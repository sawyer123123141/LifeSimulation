using System.Globalization;
using System.IO;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using UnityEditor;
using UnityEngine;

namespace LifeSimulation.EditorTools
{
    public static class PrototypeBatchEntry
    {
        public static void RunPrototype1Benchmarks()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "BenchmarkResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "prototype1-benchmark.csv");
            int[] populations = { 100, 500, 1000 };

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("scenario,population,warmup_ticks,measured_ticks,total_ms,average_step_ms,final_population");
                for (int index = 0; index < populations.Length; index++)
                {
                    int population = populations[index];
                    SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: population);
                    SimulationBenchmarkResult result = SimulationBenchmark.Run(
                        config,
                        Prototype1Scenarios.Baseline,
                        warmupTicks: 200,
                        measuredTicks: 2000);
                    writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "baseline,{0},200,2000,{1:F4},{2:F6},{3}",
                        population,
                        result.TotalMilliseconds,
                        result.AverageStepMilliseconds,
                        result.FinalPopulation));
                }
            }

            Debug.Log($"Prototype 1 benchmark results saved to {outputPath}");
        }

        public static void RunPrototype1Experiments()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "prototype1-paired-scenarios.csv");
            SimulationScenario[] scenarios =
            {
                Prototype1Scenarios.Baseline,
                Prototype1Scenarios.Drought,
                Prototype1Scenarios.FoodScarcity,
            };

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("scenario,seed,ticks,population,births,deaths,size,speed,metabolism,vision,water_efficiency,food_efficiency,state_hash");
                for (int seed = 42; seed < 47; seed++)
                {
                    for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                    {
                        SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(seed, initialPopulation: 50);
                        ExperimentResult result = ExperimentRunner.Run(config, scenarios[scenarioIndex], ticks: 20000);
                        SimulationStatistics stats = result.FinalStatistics;
                        writer.WriteLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12}",
                            result.ScenarioId,
                            result.WorldSeed,
                            result.CompletedTicks,
                            stats.Population,
                            stats.BirthCount,
                            stats.DeathCount,
                            stats.MeanBodySizeGene,
                            stats.MeanMovementSpeedGene,
                            stats.MeanMetabolicPaceGene,
                            stats.MeanVisionRangeGene,
                            stats.MeanWaterEfficiencyGene,
                            stats.MeanFoodEfficiencyGene,
                            result.FinalStateHash));
                    }
                }
            }

            Debug.Log($"Prototype 1 paired experiment results saved to {outputPath}");
        }
    }
}
