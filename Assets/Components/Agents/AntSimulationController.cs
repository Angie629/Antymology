using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Agents
{
    public class AntSimulationController : MonoBehaviour
    {
        // List of all ants in the current generation
        private readonly List<AntAgent> ants = new List<AntAgent>();
        // List of genomes for the worker population
        private readonly List<AntGenome> population = new List<AntGenome>();
        // Random number generator for evolution and simulation
        private System.Random rng;
        // Accumulates time for simulation ticks
        private float tickAccumulator;
        // Timer for the current generation
        private float generationTimer;
        // Current generation number
        private int generationIndex;

        // Reference to the world manager
        private WorldManager world;
        // Prefab for spawning ants
        private GameObject antPrefab;
        // Reference to the queen ant
        private AntAgent queen;
        // Logger for CSV metrics
        private MetricsCsvLogger metricsLogger;

        // History lists for graphing and analysis
        private readonly List<float> genNestHistory = new List<float>();
        private readonly List<float> genBestFitnessHistory = new List<float>();
        private readonly List<float> genAvgFitnessHistory = new List<float>();
        private readonly List<float> queenHealthHistory = new List<float>();
        private readonly List<float> avgWorkerHealthHistory = new List<float>();

        // Properties for UI and stats
        public int GenerationIndex => generationIndex;
        public float TimeRemaining => Mathf.Max(0f, generationTimer);
        public int AliveCount => ants.Count;
        public float LastGenBestFitness { get; private set; }
        public float LastGenAvgFitness { get; private set; }
        public int LastGenNestBlocks { get; private set; }
        public float BestEverFitness { get; private set; }
        public float BestEverAvgFitness { get; private set; }
        public int BestEverNestBlocks { get; private set; }
        public float CurrentQueenHealth { get; private set; }
        public float CurrentAvgWorkerHealth { get; private set; }
        public int CurrentTotalMulchConsumed { get; private set; }
        public IReadOnlyList<float> GenNestHistory => genNestHistory;
        public IReadOnlyList<float> GenBestFitnessHistory => genBestFitnessHistory;
        public IReadOnlyList<float> GenAvgFitnessHistory => genAvgFitnessHistory;
        public IReadOnlyList<float> QueenHealthHistory => queenHealthHistory;
        public IReadOnlyList<float> AvgWorkerHealthHistory => avgWorkerHealthHistory;

        // Set up the simulation controller and spawn the first generation
        public void Initialize(WorldManager worldManager)
        {
            world = worldManager;
            antPrefab = worldManager.antPrefab;
            rng = new System.Random(ConfigurationManager.Instance.Seed);
            metricsLogger = new MetricsCsvLogger(
                ConfigurationManager.Instance.EnableCsvMetrics,
                ConfigurationManager.Instance.MetricsSampleInterval);
            generationIndex = 1;
            BuildInitialPopulation();
            SpawnGeneration();
        }

        // Unity update loop: handles ticking and generation timing
        private void Update()
        {
            if (world == null)
            {
                return;
            }

            generationTimer -= Time.deltaTime;
            tickAccumulator += Time.deltaTime;

            // Run simulation ticks at fixed intervals
            while (tickAccumulator >= ConfigurationManager.Instance.TickInterval)
            {
                TickSimulation();
                tickAccumulator -= ConfigurationManager.Instance.TickInterval;
            }

            // End generation if timer runs out
            if (generationTimer <= 0f)
            {
                EndGeneration();
            }
        }

        // Returns a random double between 0 and 1
        public double NextDouble()
        {
            return rng.NextDouble();
        }

        // Returns a random integer less than maxExclusive
        public int NextInt(int maxExclusive)
        {
            return rng.Next(maxExclusive);
        }

        // Checks if a position is inside the world bounds
        public bool IsWithinWorld(Vector3Int position)
        {
            return position.x >= 1 && position.z >= 1 &&
                   position.x < world.WorldSizeX - 1 &&
                   position.z < world.WorldSizeZ - 1 &&
                   position.y >= 1 &&
                   position.y < world.WorldSizeY - 1;
        }

        // Finds a walkable Y coordinate near the current Y
        public bool TryFindWalkableY(int x, int z, int currentY, out int targetY)
        {
            targetY = currentY;
            int bestDelta = int.MaxValue;

            for (int dy = -2; dy <= 2; dy++)
            {
                int candidateY = currentY + dy;
                if (candidateY <= 0 || candidateY >= world.WorldSizeY - 1)
                {
                    continue;
                }

                AbstractBlock candidate = world.GetBlock(x, candidateY, z);
                if (!(candidate is AirBlock))
                {
                    continue;
                }

                AbstractBlock below = world.GetBlock(x, candidateY - 1, z);
                if (below is AirBlock)
                {
                    continue;
                }

                int absDelta = Math.Abs(dy);
                if (absDelta < bestDelta)
                {
                    bestDelta = absDelta;
                    targetY = candidateY;
                }
            }

            return bestDelta != int.MaxValue;
        }

        // Checks if an ant can consume mulch at a position (must be alone)
        public bool CanConsumeAt(Vector3Int position, AntAgent ant, Dictionary<Vector3Int, List<AntAgent>> occupancy)
        {
            if (!occupancy.TryGetValue(position, out List<AntAgent> list))
            {
                return true;
            }

            return list.Count == 1;
        }

        // Builds the initial random population of worker genomes
        private void BuildInitialPopulation()
        {
            population.Clear();
            for (int i = 0; i < ConfigurationManager.Instance.WorkerCount; i++)
            {
                population.Add(AntGenome.CreateRandom(rng));
            }
        }

        // Spawns a new generation of ants and queen
        private void SpawnGeneration()
        {
            ClearAnts();
            world.RegenerateWorld();
            generationTimer = ConfigurationManager.Instance.EvaluationDuration;
            tickAccumulator = 0f;

            AntGenome queenGenome = population.Count > 0 ? population[0].Clone() : AntGenome.CreateRandom(rng);
            queen = SpawnAnt(queenGenome, true);

            for (int i = 0; i < population.Count; i++)
            {
                SpawnAnt(population[i], false);
            }
        }

        // Spawns an ant (queen or worker) at a random position
        private AntAgent SpawnAnt(AntGenome genome, bool isQueen)
        {
            Vector3Int spawn = FindSpawnPosition();
            GameObject antObject = CreateAntObject(isQueen);
            AntAgent agent = antObject.GetComponent<AntAgent>();
            if (agent == null)
            {
                agent = antObject.AddComponent<AntAgent>();
            }
            float maxHealth = isQueen ? ConfigurationManager.Instance.QueenMaxHealth : ConfigurationManager.Instance.WorkerMaxHealth;
            agent.Initialize(genome.Clone(), isQueen, spawn, maxHealth, this);
            ants.Add(agent);
            return agent;
        }

        // Creates the ant GameObject (uses prefab if available)
        private GameObject CreateAntObject(bool isQueen)
        {
            if (antPrefab != null)
            {
                return Instantiate(antPrefab);
            }

            PrimitiveType primitive = isQueen ? PrimitiveType.Sphere : PrimitiveType.Capsule;
            GameObject antObject = GameObject.CreatePrimitive(primitive);
            antObject.name = isQueen ? "Queen" : "Ant";
            return antObject;
        }

        // Finds a spawn position on the surface
        private Vector3Int FindSpawnPosition()
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                int x = rng.Next(1, world.WorldSizeX - 1);
                int z = rng.Next(1, world.WorldSizeZ - 1);
                int surfaceY = FindSurfaceY(x, z);
                if (surfaceY > 0)
                {
                    return new Vector3Int(x, surfaceY, z);
                }
            }

            // Fallback: center of the world
            return new Vector3Int(world.WorldSizeX / 2, world.WorldSizeY - 2, world.WorldSizeZ / 2);
        }

        // Finds the surface Y coordinate at (x, z)
        private int FindSurfaceY(int x, int z)
        {
            for (int y = world.WorldSizeY - 2; y >= 1; y--)
            {
                AbstractBlock current = world.GetBlock(x, y, z);
                if (!(current is AirBlock))
                {
                    int above = y + 1;
                    if (above < world.WorldSizeY && world.GetBlock(x, above, z) is AirBlock)
                    {
                        return above;
                    }
                }
            }

            return -1;
        }

        // Runs one simulation tick: ants act, health is shared, stats updated
        private void TickSimulation()
        {
            Dictionary<Vector3Int, List<AntAgent>> occupancy = BuildOccupancy();

            for (int i = ants.Count - 1; i >= 0; i--)
            {
                AntAgent ant = ants[i];
                ant.Tick(occupancy);
                world.DecayPheromone(ant.GridPosition);
                if (!ant.IsAlive())
                {
                    if (ant == queen)
                    {
                        queen = null;
                    }
                    Destroy(ant.gameObject);
                    ants.RemoveAt(i);
                }
            }

            ShareHealthBetweenAnts();
            UpdateAggregateStats();
            RecordHealthSample();
        }

        // Builds a dictionary of ant positions for occupancy checks
        private Dictionary<Vector3Int, List<AntAgent>> BuildOccupancy()
        {
            Dictionary<Vector3Int, List<AntAgent>> occupancy = new Dictionary<Vector3Int, List<AntAgent>>();
            for (int i = 0; i < ants.Count; i++)
            {
                Vector3Int pos = ants[i].GridPosition;
                if (!occupancy.TryGetValue(pos, out List<AntAgent> list))
                {
                    list = new List<AntAgent>();
                    occupancy[pos] = list;
                }
                list.Add(ants[i]);
            }
            return occupancy;
        }

        // Handles health sharing between ants in the same cell
        private void ShareHealthBetweenAnts()
        {
            Dictionary<Vector3Int, List<AntAgent>> occupancy = BuildOccupancy();
            foreach (KeyValuePair<Vector3Int, List<AntAgent>> entry in occupancy)
            {
                List<AntAgent> group = entry.Value;
                if (group.Count < 2)
                {
                    continue;
                }

                group.Sort((a, b) => b.Health.CompareTo(a.Health));
                AntAgent donor = group[0];
                AntAgent receiver = group[group.Count - 1];

                if (donor == receiver)
                {
                    continue;
                }

                float amount = Mathf.Min(ConfigurationManager.Instance.HealthShareAmount, donor.Health - receiver.Health);
                if (amount <= 0f)
                {
                    continue;
                }

                float given = donor.GiveHealth(amount);
                receiver.ReceiveHealth(given);
            }
        }

        // Handles end-of-generation logic: ranking, breeding, mutation, and respawn
        private void EndGeneration()
        {
            List<AntAgent> workers = ants.Where(ant => !ant.IsQueen).ToList();
            if (workers.Count == 0)
            {
                BuildInitialPopulation();
                SpawnGeneration();
                return;
            }

            workers.Sort((a, b) => b.FitnessScore.CompareTo(a.FitnessScore));

            LastGenBestFitness = workers[0].FitnessScore;
            LastGenAvgFitness = workers.Average(worker => worker.FitnessScore);
            LastGenNestBlocks = world != null ? world.NestBlockCount : 0;

            metricsLogger?.LogGenerationSummary(generationIndex, LastGenBestFitness, LastGenAvgFitness, LastGenNestBlocks);

            AppendHistory(genNestHistory, LastGenNestBlocks);
            AppendHistory(genBestFitnessHistory, LastGenBestFitness);
            AppendHistory(genAvgFitnessHistory, LastGenAvgFitness);

            BestEverFitness = Mathf.Max(BestEverFitness, LastGenBestFitness);
            BestEverAvgFitness = Mathf.Max(BestEverAvgFitness, LastGenAvgFitness);
            BestEverNestBlocks = Mathf.Max(BestEverNestBlocks, LastGenNestBlocks);

            int eliteCount = Mathf.Clamp(ConfigurationManager.Instance.EliteCount, 1, workers.Count);
            List<AntGenome> nextPopulation = new List<AntGenome>();
            for (int i = 0; i < eliteCount; i++)
            {
                nextPopulation.Add(workers[i].GetGenomeClone());
            }

            while (nextPopulation.Count < ConfigurationManager.Instance.WorkerCount)
            {
                AntGenome parentA = nextPopulation[rng.Next(nextPopulation.Count)].Clone();
                AntGenome parentB = workers[rng.Next(workers.Count)].GetGenomeClone();
                AntGenome child = parentA.Crossover(parentB, rng);
                child.Mutate(rng, ConfigurationManager.Instance.MutationRate, ConfigurationManager.Instance.MutationMagnitude);
                nextPopulation.Add(child);
            }

            population.Clear();
            population.AddRange(nextPopulation);
            generationIndex += 1;
            SpawnGeneration();
        }

        // Destroys all ant GameObjects and clears lists
        private void ClearAnts()
        {
            for (int i = 0; i < ants.Count; i++)
            {
                if (ants[i] != null)
                {
                    Destroy(ants[i].gameObject);
                }
            }
            ants.Clear();
            queen = null;
        }

        // Records health and stats for metrics logging
        private void RecordHealthSample()
        {
            AppendHistory(queenHealthHistory, CurrentQueenHealth);
            AppendHistory(avgWorkerHealthHistory, CurrentAvgWorkerHealth);

            int nestBlocks = world != null ? world.NestBlockCount : 0;
            metricsLogger?.LogHealthSample(
                generationIndex,
                TimeRemaining,
                CurrentQueenHealth,
                CurrentAvgWorkerHealth,
                AliveCount,
                nestBlocks,
                CurrentTotalMulchConsumed);
        }

        // Keeps history lists at configured length
        private void AppendHistory(List<float> history, float value)
        {
            int max = Mathf.Max(1, ConfigurationManager.Instance.GraphHistoryLength);
            history.Add(value);
            while (history.Count > max)
            {
                history.RemoveAt(0);
            }
        }

        // Updates aggregate stats for UI and logging
        private void UpdateAggregateStats()
        {
            float totalWorkerHealth = 0f;
            int workerCount = 0;
            int mulchTotal = 0;

            for (int i = 0; i < ants.Count; i++)
            {
                AntAgent ant = ants[i];
                mulchTotal += ant.MulchConsumed;
                if (ant.IsQueen)
                {
                    continue;
                }

                totalWorkerHealth += ant.Health;
                workerCount += 1;
            }

            CurrentQueenHealth = queen != null ? queen.Health : 0f;
            CurrentAvgWorkerHealth = workerCount > 0 ? totalWorkerHealth / workerCount : 0f;
            CurrentTotalMulchConsumed = mulchTotal;
        }
    }
}
