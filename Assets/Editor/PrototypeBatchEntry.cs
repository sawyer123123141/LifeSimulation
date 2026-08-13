using System;
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
                writer.WriteLine("scenario,population,warmup_ticks,measured_ticks,total_ms,average_step_ms,p95_step_ms,final_population");
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
                        "baseline,{0},200,2000,{1:F4},{2:F6},{3:F6},{4}",
                        population,
                        result.TotalMilliseconds,
                        result.AverageStepMilliseconds,
                        result.P95StepMilliseconds,
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
            string summaryPath = Path.Combine(outputDirectory, "prototype1-paired-summary.csv");
            SimulationScenario[] scenarios =
            {
                Prototype1Scenarios.Baseline,
                Prototype1Scenarios.Drought,
                Prototype1Scenarios.FoodScarcity,
            };
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());
            var resultsByScenario = new ExperimentResult[scenarios.Length][];
            for (int scenarioIndex = 0; scenarioIndex < resultsByScenario.Length; scenarioIndex++)
            {
                resultsByScenario[scenarioIndex] = new ExperimentResult[options.SeedCount];
            }

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("scenario,seed,ticks,population,births,deaths,size,speed,metabolism,vision,water_efficiency,food_efficiency,event_overflowed,state_hash");
                for (int seedOffset = 0; seedOffset < options.SeedCount; seedOffset++)
                {
                    int seed = options.FirstSeed + seedOffset;
                    for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                    {
                        SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(seed, options.FounderPopulation);
                        ExperimentResult result = ExperimentRunner.Run(config, scenarios[scenarioIndex], options.Ticks);
                        resultsByScenario[scenarioIndex][seedOffset] = result;
                        SimulationStatistics stats = result.FinalStatistics;
                        writer.WriteLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12},{13}",
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
                            result.EventOverflowed,
                            result.FinalStateHash));
                    }
                }
            }

            using (var writer = new StreamWriter(summaryPath, append: false))
            {
                writer.WriteLine("control,treatment,metric,pairs,mean_treatment_minus_control,standardized_effect,direction_consistency,interval_lower,interval_upper,meets_statistical_criterion,requires_mechanism_evidence");
                Array metrics = Enum.GetValues(typeof(ExperimentMetric));
                for (int treatmentIndex = 1; treatmentIndex < scenarios.Length; treatmentIndex++)
                {
                    for (int metricIndex = 0; metricIndex < metrics.Length; metricIndex++)
                    {
                        var metric = (ExperimentMetric)metrics.GetValue(metricIndex);
                        PairedExperimentSummary summary = PairedExperimentAnalysis.Summarize(
                            resultsByScenario[0],
                            resultsByScenario[treatmentIndex],
                            metric);
                        PairedBootstrapInterval interval = PairedBootstrapAnalysis.EstimateMeanDifferenceInterval(
                            resultsByScenario[0],
                            resultsByScenario[treatmentIndex],
                            metric,
                            resampleCount: 1024,
                            randomSeed: options.FirstSeed);
                        float effect = PairedExperimentAnalysis.CalculateStandardizedEffect(
                            resultsByScenario[0],
                            resultsByScenario[treatmentIndex],
                            metric);
                        PairedEvolutionAssessment assessment = PairedEvolutionCriterion.Assess(summary, interval, effect);
                        writer.WriteLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4:F6},{5:F6},{6:F4},{7:F6},{8:F6},{9},{10}",
                            resultsByScenario[0][0].ScenarioId,
                            resultsByScenario[treatmentIndex][0].ScenarioId,
                            metric,
                            summary.PairCount,
                            summary.MeanTreatmentMinusControl,
                            effect,
                            summary.DirectionConsistency,
                            interval.LowerBound,
                            interval.UpperBound,
                            assessment.MeetsStatisticalCriterion,
                            assessment.RequiresMechanismEvidence));
                    }
                }
            }

            Debug.Log($"Prototype 1 paired experiment results saved to {outputPath} and {summaryPath}");
        }
    }
}
