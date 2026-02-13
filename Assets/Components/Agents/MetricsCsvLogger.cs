using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Antymology.Agents
{
    public class MetricsCsvLogger
    {
        // Directory where metrics files are written
        private readonly string outputDirectory;
        // Path for health metrics CSV
        private readonly string healthFilePath;
        // Path for generation summary CSV
        private readonly string generationFilePath;
        // Use invariant culture for formatting numbers
        private readonly CultureInfo invariantCulture = CultureInfo.InvariantCulture;
        // How often to sample health metrics (in ticks)
        private readonly int sampleInterval;
        // Whether logging is enabled
        private readonly bool isEnabled;
        // Current tick index for health samples
        private int tickIndex;

        // Set up logger, create directories and files if enabled
        public MetricsCsvLogger(bool enableLogging, int sampleIntervalTicks)
        {
            isEnabled = enableLogging;
            sampleInterval = Mathf.Max(1, sampleIntervalTicks);

            string baseDirectory = Path.Combine(Application.persistentDataPath, "metrics");
            string runFolder = "run_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", invariantCulture);
            outputDirectory = Path.Combine(baseDirectory, runFolder);
            healthFilePath = Path.Combine(outputDirectory, "health_metrics.csv");
            generationFilePath = Path.Combine(outputDirectory, "generation_metrics.csv");

            if (!isEnabled)
            {
                return;
            }

            Directory.CreateDirectory(baseDirectory);
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(baseDirectory, "latest.txt"), outputDirectory);
            EnsureHeader(healthFilePath, "step,generation,time_remaining,queen_health,avg_worker_health,alive_count,nest_blocks,mulch_consumed");
            EnsureHeader(generationFilePath, "generation,best_fitness,avg_fitness,nest_blocks");
        }

        // Log a health sample to CSV (only every sampleInterval ticks)
        public void LogHealthSample(
            int generation,
            float timeRemaining,
            float queenHealth,
            float avgWorkerHealth,
            int aliveCount,
            int nestBlocks,
            int mulchConsumed)
        {
            if (!isEnabled)
            {
                return;
            }

            tickIndex += 1;
            if ((tickIndex - 1) % sampleInterval != 0)
            {
                return;
            }

            string line = string.Join(",",
                tickIndex.ToString(invariantCulture),
                generation.ToString(invariantCulture),
                timeRemaining.ToString("F3", invariantCulture),
                queenHealth.ToString("F3", invariantCulture),
                avgWorkerHealth.ToString("F3", invariantCulture),
                aliveCount.ToString(invariantCulture),
                nestBlocks.ToString(invariantCulture),
                mulchConsumed.ToString(invariantCulture));

            AppendLine(healthFilePath, line);
        }

        // Log a generation summary to CSV
        public void LogGenerationSummary(int generation, float bestFitness, float avgFitness, int nestBlocks)
        {
            if (!isEnabled)
            {
                return;
            }

            string line = string.Join(",",
                generation.ToString(invariantCulture),
                bestFitness.ToString("F3", invariantCulture),
                avgFitness.ToString("F3", invariantCulture),
                nestBlocks.ToString(invariantCulture));

            AppendLine(generationFilePath, line);
        }

        // Make sure the CSV file has a header row
        private void EnsureHeader(string filePath, string header)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Length > 0)
                {
                    return;
                }
            }

            AppendLine(filePath, header);
        }

        // Append a line to the CSV file
        private void AppendLine(string filePath, string line)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine(line);
            }
        }
    }
}
