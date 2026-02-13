using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all configuration parameters for the simulation, including world size, ant properties, evolutionary algorithm settings, and UI/metrics options.
/// Singleton pattern ensures a single global instance.
/// </summary>
public class ConfigurationManager : Singleton<ConfigurationManager>
{

    /// <summary>
    /// The seed for world generation.
    /// </summary>
    public int Seed = 1337;

    /// <summary>
    /// The number of chunks in the x and z dimension of the world.
    /// </summary>
    public int World_Diameter = 16;

    /// <summary>
    /// The number of chunks in the y dimension of the world.
    /// </summary>
    public int World_Height = 4;

    /// <summary>
    /// The number of blocks in any dimension of a chunk.
    /// </summary>
    public int Chunk_Diameter = 8;

    /// <summary>
    /// How much of the tile map does each tile take up.
    /// </summary>
    public float Tile_Map_Unit_Ratio = 0.25f;

    /// <summary>
    /// The number of acidic regions on the map.
    /// </summary>
    public int Number_Of_Acidic_Regions = 10;

    /// <summary>
    /// The radius of each acidic region
    /// </summary>
    public int Acidic_Region_Radius = 5;

    /// <summary>
    /// The number of acidic regions on the map.
    /// </summary>
    public int Number_Of_Conatiner_Spheres = 5;

    /// <summary>
    /// The radius of each acidic region
    /// </summary>
    public int Conatiner_Sphere_Radius = 20;

    /// <summary>
    /// Number of worker ants per generation.
    /// </summary>
    public int WorkerCount = 30;

    /// <summary>
    /// Maximum health for worker ants.
    /// </summary>
    public float WorkerMaxHealth = 40f;

    /// <summary>
    /// Maximum health for the queen ant.
    /// </summary>
    public float QueenMaxHealth = 120f;

    /// <summary>
    /// Health drained per tick.
    /// </summary>
    public float HealthDrainPerTick = 0.4f;

    /// <summary>
    /// Health gained by consuming a mulch block.
    /// </summary>
    public float MulchHealthGain = 15f;

    /// <summary>
    /// Seconds per simulation tick.
    /// </summary>
    public float TickInterval = 0.25f;

    /// <summary>
    /// Seconds per generation evaluation.
    /// </summary>
    public float EvaluationDuration = 60f;

    /// <summary>
    /// Number of elite genomes kept each generation.
    /// </summary>
    public int EliteCount = 12;

    /// <summary>
    /// Mutation probability per genome value.
    /// </summary>
    public float MutationRate = 0.15f;

    /// <summary>
    /// Maximum absolute mutation delta.
    /// </summary>
    public float MutationMagnitude = 0.4f;

    /// <summary>
    /// Health shared per tick when ants overlap.
    /// </summary>
    public float HealthShareAmount = 2f;

    /// <summary>
    /// Fitness weight for mulch collection.
    /// </summary>
    public float MulchFitnessBonus = 2f;

    /// <summary>
    /// Fitness weight for nest blocks created.
    /// </summary>
    public float NestFitnessBonus = 12f;

    /// <summary>
    /// Amount of pheromone deposited per tick.
    /// </summary>
    public float PheromoneDeposit = 0.2f;

    /// <summary>
    /// Maximum pheromone concentration per cell.
    /// </summary>
    public float PheromoneMax = 2.5f;

    /// <summary>
    /// Pheromone decay per tick.
    /// </summary>
    public float PheromoneDecay = 0.05f;

    /// <summary>
    /// How strongly pheromones influence movement.
    /// </summary>
    public float PheromoneBias = 1.2f;

    /// <summary>
    /// Number of history samples retained for HUD graphs.
    /// </summary>
    public int GraphHistoryLength = 50;

    /// <summary>
    /// Enable writing metrics CSVs for TensorBoard logging.
    /// </summary>
    /// <summary>
    /// If true, enables writing simulation metrics to CSV files for TensorBoard or analysis.
    /// </summary>
    public bool EnableCsvMetrics = true;

    /// <summary>
    /// Log every N ticks to the health metrics CSV.
    /// </summary>
    public int MetricsSampleInterval = 1;
}
