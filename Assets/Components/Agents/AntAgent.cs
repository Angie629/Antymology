using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Agents
{
    public class AntAgent : MonoBehaviour
    {
        // The ant's current position in the grid
        public Vector3Int GridPosition { get; private set; }
        // True if this ant is the queen
        public bool IsQueen { get; private set; }
        // Current health value
        public float Health { get; private set; }
        // Maximum health value
        public float MaxHealth { get; private set; }
        // How many mulch blocks this ant has eaten
        public int MulchConsumed { get; private set; }
        // How many nest blocks the queen has built
        public int NestBuilt { get; private set; }
        // Fitness score for evolutionary ranking
        public float FitnessScore { get; private set; }

        // Genome: contains weights for movement, digging, etc.
        private AntGenome genome;
        // Reference to the simulation controller
        private AntSimulationController controller;
        // Renderer for coloring the ant
        private Renderer cachedRenderer;

        // Set up the ant with its genome, role, position, health, and controller
        public void Initialize(AntGenome assignedGenome, bool isQueen, Vector3Int spawnPosition, float maxHealth, AntSimulationController owner)
        {
            genome = assignedGenome;
            IsQueen = isQueen;
            GridPosition = spawnPosition;
            MaxHealth = maxHealth;
            Health = maxHealth;
            controller = owner;
            MulchConsumed = 0;
            NestBuilt = 0;
            FitnessScore = 0f;

            cachedRenderer = GetComponentInChildren<Renderer>();
            if (cachedRenderer != null)
            {
                // Color the queen yellow, workers brown
                cachedRenderer.material.color = isQueen ? new Color(0.95f, 0.82f, 0.2f) : new Color(0.62f, 0.34f, 0.1f);
            }

            if (isQueen)
            {
                // Make the queen bigger
                transform.localScale = Vector3.one * 1.5f;
            }

            SyncTransform();
        }

        // Is the ant alive?
        public bool IsAlive()
        {
            return Health > 0f;
        }

        // Main tick: handles health, actions, movement, pheromone, and fitness
        public void Tick(Dictionary<Vector3Int, List<AntAgent>> occupancy)
        {
            if (Health <= 0f)
            {
                return;
            }

            DrainHealth();
            if (Health <= 0f)
            {
                return;
            }

            // Try to eat mulch if possible
            bool canConsume = controller.CanConsumeAt(GridPosition, this, occupancy);
            if (canConsume && TryConsumeMulch())
            {
                UpdateFitness();
                return;
            }

            // Queen tries to build nest block
            if (IsQueen && TryBuildNest())
            {
                UpdateFitness();
                return;
            }

            // Try to dig down
            if (TryDigDown())
            {
                UpdateFitness();
                return;
            }

            // Try to move
            TryMove();
            // Drop pheromones at current position
            DepositPheromones();
            // Update fitness score
            UpdateFitness();
        }

        // Receive health from another ant
        public void ReceiveHealth(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        // Give health to another ant, returns actual amount given
        public float GiveHealth(float amount)
        {
            if (amount <= 0f || Health <= 0f)
            {
                return 0f;
            }

            float actual = Mathf.Min(amount, Health);
            Health -= actual;
            return actual;
        }

        // Increment nest block count (queen only)
        public void IncrementNestBuilt()
        {
            NestBuilt += 1;
        }

        // Clone the genome for breeding
        public AntGenome GetGenomeClone()
        {
            return genome.Clone();
        }

        // Lose health each tick, double if on acidic block
        private void DrainHealth()
        {
            float drain = ConfigurationManager.Instance.HealthDrainPerTick;
            AbstractBlock below = WorldManager.Instance.GetBlock(GridPosition.x, GridPosition.y - 1, GridPosition.z);
            if (below is AcidicBlock)
            {
                drain *= 2f;
            }

            Health -= drain;
        }

        // Try to eat mulch below, restore health
        private bool TryConsumeMulch()
        {
            AbstractBlock below = WorldManager.Instance.GetBlock(GridPosition.x, GridPosition.y - 1, GridPosition.z);
            if (!(below is MulchBlock))
            {
                return false;
            }

            if (!ShouldDo(genome.consumeWeight))
            {
                return false;
            }

            WorldManager.Instance.SetBlock(GridPosition.x, GridPosition.y - 1, GridPosition.z, new AirBlock());
            MulchConsumed += 1;
            Health = Mathf.Min(MaxHealth, Health + ConfigurationManager.Instance.MulchHealthGain);
            return true;
        }

        // Queen tries to build nest block in adjacent air cell
        private bool TryBuildNest()
        {
            if (!ShouldDo(genome.buildWeight))
            {
                return false;
            }

            float cost = MaxHealth / 3f;
            if (Health < cost)
            {
                return false;
            }

            Vector3Int target;
            if (!FindAdjacentAirCell(out target))
            {
                return false;
            }

            WorldManager.Instance.SetBlock(target.x, target.y, target.z, new NestBlock());
            Health -= cost;
            WorldManager.Instance.RegisterNestBlock();
            NestBuilt += 1;
            return true;
        }

        // Try to dig down if possible
        private bool TryDigDown()
        {
            if (!ShouldDo(genome.digWeight))
            {
                return false;
            }

            int belowY = GridPosition.y - 1;
            AbstractBlock below = WorldManager.Instance.GetBlock(GridPosition.x, belowY, GridPosition.z);
            if (below is AirBlock || below is ContainerBlock)
            {
                return false;
            }

            WorldManager.Instance.SetBlock(GridPosition.x, belowY, GridPosition.z, new AirBlock());
            GridPosition = new Vector3Int(GridPosition.x, belowY, GridPosition.z);
            SyncTransform();
            return true;
        }

        // Try to move in a direction based on genome and pheromone
        private void TryMove()
        {
            Vector2Int direction = ChooseMoveDirection();
            if (direction == Vector2Int.zero)
            {
                return;
            }

            int targetX = GridPosition.x + direction.x;
            int targetZ = GridPosition.z + direction.y;
            int targetY;
            if (!controller.TryFindWalkableY(targetX, targetZ, GridPosition.y, out targetY))
            {
                return;
            }

            GridPosition = new Vector3Int(targetX, targetY, targetZ);
            SyncTransform();
        }

        // Decide move direction based on weighted random and pheromone
        private Vector2Int ChooseMoveDirection()
        {
            float north = GetMoveWeight(new Vector3Int(0, 0, 1), genome.moveNorthWeight);
            float south = GetMoveWeight(new Vector3Int(0, 0, -1), genome.moveSouthWeight);
            float east = GetMoveWeight(new Vector3Int(1, 0, 0), genome.moveEastWeight);
            float west = GetMoveWeight(new Vector3Int(-1, 0, 0), genome.moveWestWeight);
            float total = north + south + east + west;
            float roll = (float)controller.NextDouble() * total;

            if (roll < north)
            {
                return new Vector2Int(0, 1);
            }
            roll -= north;
            if (roll < south)
            {
                return new Vector2Int(0, -1);
            }
            roll -= south;
            if (roll < east)
            {
                return new Vector2Int(1, 0);
            }
            return new Vector2Int(-1, 0);
        }

        // Calculate move weight, factoring in pheromone at target cell
        private float GetMoveWeight(Vector3Int offset, float baseWeight)
        {
            float weight = Mathf.Max(0.01f, baseWeight);
            Vector3Int check = GridPosition + offset;
            if (!controller.IsWithinWorld(check))
            {
                return weight * 0.25f;
            }

            float pheromone = IsQueen
                ? WorldManager.Instance.GetNestPheromone(check)
                : WorldManager.Instance.GetFoodPheromone(check);
            return weight * (1f + pheromone * ConfigurationManager.Instance.PheromoneBias);
        }

        // Find an adjacent air cell for nest building
        private bool FindAdjacentAirCell(out Vector3Int target)
        {
            Vector3Int[] offsets =
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1),
                new Vector3Int(0, -1, 0),
                new Vector3Int(0, 1, 0)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                int index = controller.NextInt(offsets.Length);
                Vector3Int candidate = GridPosition + offsets[index];
                if (!controller.IsWithinWorld(candidate))
                {
                    continue;
                }

                AbstractBlock block = WorldManager.Instance.GetBlock(candidate.x, candidate.y, candidate.z);
                if (block is AirBlock)
                {
                    target = candidate;
                    return true;
                }
            }

            target = GridPosition;
            return false;
        }

        // Decide if an action should be performed based on weight
        private bool ShouldDo(float weight)
        {
            float normalized = Mathf.Clamp01(weight / 2f);
            return controller.NextDouble() < normalized;
        }

        // Sync the ant's transform to its grid position
        private void SyncTransform()
        {
            transform.position = new Vector3(GridPosition.x, GridPosition.y, GridPosition.z);
        }

        // Update the fitness score based on health, mulch, and nest built
        private void UpdateFitness()
        {
            FitnessScore = Health + MulchConsumed * ConfigurationManager.Instance.MulchFitnessBonus;
            if (IsQueen)
            {
                FitnessScore += NestBuilt * ConfigurationManager.Instance.NestFitnessBonus;
            }
        }

        // Drop pheromones at current position (nest for queen, food for worker)
        private void DepositPheromones()
        {
            if (IsQueen)
            {
                WorldManager.Instance.DepositNestPheromone(GridPosition);
            }
            else
            {
                WorldManager.Instance.DepositFoodPheromone(GridPosition);
            }
        }
    }
}
