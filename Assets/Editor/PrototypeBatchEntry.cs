using System;
using System.Globalization;
using System.IO;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
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
                writer.WriteLine("scenario,seed,ticks,population,births,deaths,size,speed,metabolism,vision,water_efficiency,food_efficiency,cumulative_food,cumulative_water,event_overflowed,population_cap_reached,state_hash");
                for (int seedOffset = 0; seedOffset < options.SeedCount; seedOffset++)
                {
                    int seed = options.FirstSeed + seedOffset;
                    for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                    {
                        SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(seed, options.FounderPopulation);
                        var config = new SimulationConfig(
                            seed,
                            options.FounderPopulation,
                            defaults.Schedule,
                            options.MaximumPopulation);
                        ExperimentResult result = ExperimentRunner.Run(config, scenarios[scenarioIndex], options.Ticks);
                        resultsByScenario[scenarioIndex][seedOffset] = result;
                        SimulationStatistics stats = result.FinalStatistics;
                        writer.WriteLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12:F4},{13:F4},{14},{15},{16}",
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
                            stats.CumulativeFoodConsumed,
                            stats.CumulativeWaterConsumed,
                            result.EventOverflowed,
                            result.PopulationCapReached,
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

        [MenuItem("Life Simulation/Run Prototype 3 Physiology Experiments")]
        public static void RunPrototype3Experiments()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "prototype3-physiology-scenarios.csv");
            SimulationScenario[] scenarios =
            {
                Prototype3Scenarios.PlantNutritionPoor,
                Prototype3Scenarios.PlantNutritionRich,
            };
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("scenario,seed,ticks,population,births,deaths,temperature_tolerance,fertility_investment,lifespan_tendency,cumulative_food,cumulative_water,event_overflowed,population_cap_reached,state_hash");
                for (int seedOffset = 0; seedOffset < options.SeedCount; seedOffset++)
                {
                    int seed = options.FirstSeed + seedOffset;
                    for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                    {
                        SimulationConfig defaults = SimulationConfig.CreatePrototype3Defaults(seed, options.FounderPopulation);
                        var config = new SimulationConfig(
                            seed,
                            options.FounderPopulation,
                            defaults.Schedule,
                            options.MaximumPopulation,
                            defaults.FounderProfile,
                            cognitionEnabled: true,
                            physiologyEnabled: true);
                        ExperimentResult result = ExperimentRunner.Run(config, scenarios[scenarioIndex], options.Ticks);
                        SimulationStatistics stats = result.FinalStatistics;
                        writer.WriteLine(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11},{12},{13}",
                            result.ScenarioId,
                            result.WorldSeed,
                            result.CompletedTicks,
                            stats.Population,
                            stats.BirthCount,
                            stats.DeathCount,
                            stats.MeanTemperatureToleranceGene,
                            stats.MeanFertilityInvestmentGene,
                            stats.MeanLifespanTendencyGene,
                            stats.CumulativeFoodConsumed,
                            stats.CumulativeWaterConsumed,
                            result.EventOverflowed,
                            result.PopulationCapReached,
                            result.FinalStateHash));
                    }
                }
            }

            Debug.Log($"Prototype 3 physiology experiment results saved to {outputPath}");
        }

        [MenuItem("Life Simulation/Run Prototype 4 Plant Biomass Smoke Test")]
        public static void RunPrototype4PlantBiomassSmokeTest()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "prototype4-plant-biomass-smoke.csv");
            int[] seeds = { 42, 43, 44 };

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("scenario,seed,ticks,population,plant_biomass,plant_growth,plant_consumed,dormant_patches,plant_residual,state_hash");
                for (int index = 0; index < seeds.Length; index++)
                {
                    int seed = seeds[index];
                    SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(seed, initialPopulation: 4);
                    ExperimentResult result = ExperimentRunner.Run(config, Prototype4Scenarios.PlantBackedBaseline, ticks: 2000);
                    SimulationStatistics stats = result.FinalStatistics;
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:F4},{5:F4},{6:F4},{7},{8:F6},{9}", result.ScenarioId, seed, result.CompletedTicks, stats.Population, stats.TotalPlantBiomass, stats.CumulativePlantGrowth, stats.CumulativePlantBiomassConsumed, stats.DormantPlantPatchCount, stats.PlantBiomassResidual, result.FinalStateHash));
                }
            }

            Debug.Log($"Prototype 4 plant biomass smoke results saved to {outputPath}");
        }

        [MenuItem("Life Simulation/Run Prototype 4 Plant Heredity Smoke Test")]
        public static void RunPrototype4PlantHereditySmokeTest()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "prototype4-plant-heredity-smoke.csv");
            int[] seeds = { 42, 43, 44 };
            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("seed,ticks,active_plants,plant_births,highest_generation,mean_growth,mean_nutrition,mean_defense,biomass,residual,state_hash");
                for (int index = 0; index < seeds.Length; index++)
                {
                    SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(seeds[index], 0);
                    ExperimentResult result = ExperimentRunner.Run(config, Prototype4Scenarios.PlantBackedBaseline, 4000);
                    SimulationStatistics stats = result.FinalStatistics;
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5:F4},{6:F4},{7:F4},{8:F4},{9:F6},{10}", seeds[index], result.CompletedTicks, stats.ActivePlantPatchCount, stats.PlantBirthCount, stats.HighestPlantGeneration, stats.MeanPlantGrowthGene, stats.MeanPlantNutritionGene, stats.MeanPlantDefenseGene, stats.TotalPlantBiomass, stats.PlantBiomassResidual, result.FinalStateHash));
                }
            }
            Debug.Log($"Prototype 4 plant heredity smoke results saved to {outputPath}");
        }

        [MenuItem("Life Simulation/Run Prototype 4 Plant Defense Experiment")]
        public static void RunPrototype4PlantDefenseExperiment()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "prototype4-plant-defense.csv");
            SimulationScenario[] scenarios = { Prototype4Scenarios.UndefendedPlants, Prototype4Scenarios.DefendedPlants };
            int[] seeds = { 42, 43, 44 };
            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("scenario,seed,population,births,deaths,food_efficiency,plant_defense,plant_nutrition,plant_biomass,plant_births,state_hash");
                for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                {
                    for (int seedIndex = 0; seedIndex < seeds.Length; seedIndex++)
                    {
                        SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(seeds[seedIndex], 4);
                        ExperimentResult result = ExperimentRunner.Run(config, scenarios[scenarioIndex], 4000);
                        SimulationStatistics stats = result.FinalStatistics;
                        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5:F4},{6:F4},{7:F4},{8:F4},{9},{10}", result.ScenarioId, seeds[seedIndex], stats.Population, stats.BirthCount, stats.DeathCount, stats.MeanFoodEfficiencyGene, stats.MeanPlantDefenseGene, stats.MeanPlantNutritionGene, stats.TotalPlantBiomass, stats.PlantBirthCount, result.FinalStateHash));
                    }
                }
            }
            Debug.Log($"Prototype 4 plant defense experiment results saved to {outputPath}");
        }

        [MenuItem("Life Simulation/Run Decision Policy Travel Experiments")]
        public static void RunDecisionPolicyTravelExperiments()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "decision-policy-travel-paired.csv");
            string summaryPath = Path.Combine(outputDirectory, "decision-policy-travel-summary.csv");
            SimulationScenario[] scenarios = { DecisionPolicyScenarios.NearAdequateFood, DecisionPolicyScenarios.FarRichFood };
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());
            var results = new ExperimentResult[scenarios.Length][];
            for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++) results[scenarioIndex] = new ExperimentResult[options.SeedCount];

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("scenario,seed,ticks,population,births,deaths,travel_sensitivity,urgency_exponent,risk_aversion,commitment,left_food_target_decisions,right_food_target_decisions,mean_food_target_distance,cumulative_food,cumulative_water,state_hash");
                for (int offset = 0; offset < options.SeedCount; offset++)
                {
                    int seed = options.FirstSeed + offset;
                    SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(seed, options.FounderPopulation);
                    var config = new SimulationConfig(seed, options.FounderPopulation, defaults.Schedule, options.MaximumPopulation, decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1);
                    for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                    {
                        ExperimentResult result = ExperimentRunner.Run(config, scenarios[scenarioIndex], options.Ticks);
                        results[scenarioIndex][offset] = result;
                        SimulationStatistics stats = result.FinalStatistics;
                        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10},{11},{12:F4},{13:F4},{14:F4},{15}", result.ScenarioId, result.WorldSeed, result.CompletedTicks, stats.Population, stats.BirthCount, stats.DeathCount, stats.MeanTravelSensitivityGene, stats.MeanUrgencyExponentGene, stats.MeanRiskAversionGene, stats.MeanCommitmentGene, result.LeftFoodTargetDecisions, result.RightFoodTargetDecisions, result.MeanFoodTargetDistance, stats.CumulativeFoodConsumed, stats.CumulativeWaterConsumed, result.FinalStateHash));
                    }
                }
            }

            using (var writer = new StreamWriter(summaryPath, append: false))
            {
                writer.WriteLine("control,treatment,metric,pairs,mean_treatment_minus_control,standardized_effect,direction_consistency,interval_lower,interval_upper,meets_statistical_criterion");
                ExperimentMetric[] metrics = { ExperimentMetric.TravelSensitivity, ExperimentMetric.Population, ExperimentMetric.BirthCount, ExperimentMetric.DeathCount };
                for (int index = 0; index < metrics.Length; index++)
                {
                    ExperimentMetric metric = metrics[index];
                    PairedExperimentSummary summary = PairedExperimentAnalysis.Summarize(results[0], results[1], metric);
                    PairedBootstrapInterval interval = PairedBootstrapAnalysis.EstimateMeanDifferenceInterval(results[0], results[1], metric, 1024, options.FirstSeed);
                    float effect = PairedExperimentAnalysis.CalculateStandardizedEffect(results[0], results[1], metric);
                    PairedEvolutionAssessment assessment = PairedEvolutionCriterion.Assess(summary, interval, effect);
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:F6},{5:F6},{6:F4},{7:F6},{8:F6},{9}", results[0][0].ScenarioId, results[1][0].ScenarioId, metric, summary.PairCount, summary.MeanTreatmentMinusControl, effect, summary.DirectionConsistency, interval.LowerBound, interval.UpperBound, assessment.MeetsStatisticalCriterion));
                }
            }

            Debug.Log($"Decision-policy travel results saved to {outputPath} and {summaryPath}");
        }

        [MenuItem("Life Simulation/Run Predator-Prey Control Experiments")]
        public static void RunPredatorPreyControlExperiments()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "predator-prey-control.csv");
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("condition,seed,ticks,population,births,deaths,predation_deaths,attack_hits,plant_food,carcass_food,body_size,attack,defense,aggression,diet_specialization,state_hash");
                for (int offset = 0; offset < options.SeedCount; offset++)
                {
                    int seed = options.FirstSeed + offset;
                    WritePredationControlResult(writer, "prey-only", CreatePredationExperimentConfig(seed, options, FounderProfile.Prototype1), options.Ticks);
                    WritePredationControlResult(writer, "mixed-predation", CreatePredationExperimentConfig(seed, options, FounderProfile.PredationVariation), options.Ticks);
                }
            }

            Debug.Log($"Predator-prey control results saved to {outputPath}");
        }

        private static SimulationConfig CreatePredationExperimentConfig(int seed, ExperimentBatchOptions options, FounderProfile founderProfile)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(seed, options.FounderPopulation);
            return new SimulationConfig(
                seed,
                options.FounderPopulation,
                defaults.Schedule,
                options.MaximumPopulation,
                founderProfile,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1);
        }

        private static void WritePredationControlResult(StreamWriter writer, string condition, SimulationConfig config, int ticks)
        {
            ExperimentResult result = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks);
            SimulationStatistics stats = result.FinalStatistics;
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8:F4},{9:F4},{10:F4},{11:F4},{12:F4},{13:F4},{14:F4},{15}",
                condition,
                result.WorldSeed,
                result.CompletedTicks,
                stats.Population,
                stats.BirthCount,
                stats.DeathCount,
                stats.PredationDeathCount,
                stats.AttackHitCount,
                stats.CumulativeFoodConsumed,
                stats.CumulativeCarcassConsumed,
                stats.MeanBodySizeGene,
                stats.MeanAttackGene,
                stats.MeanDefenseGene,
                stats.MeanAggressionGene,
                stats.MeanDietSpecializationGene,
                result.FinalStateHash));
        }

        [MenuItem("Life Simulation/Run Predator Removal/Reintroduction Experiments")]
        public static void RunPredatorRemovalReintroductionExperiments()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "predator-removal-reintroduction.csv");
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("condition,seed,ticks,population_at_removal,population_at_reintroduction,hunters_removed,hunters_reintroduced,final_population,births,deaths,predation_deaths,attack_hits,carcass_food,state_hash");
                for (int offset = 0; offset < options.SeedCount; offset++)
                {
                    int seed = options.FirstSeed + offset;
                    SimulationConfig config = CreatePredationExperimentConfig(seed, options, FounderProfile.PredationVariation);
                    WritePredationInterventionResult(writer, "uninterrupted", config, options.Ticks, applyIntervention: false);
                    WritePredationInterventionResult(writer, "removal-reintroduction", config, options.Ticks, applyIntervention: true);
                }
            }

            Debug.Log($"Predator removal/reintroduction results saved to {outputPath}");
        }

        private static void WritePredationInterventionResult(StreamWriter writer, string condition, SimulationConfig config, int ticks, bool applyIntervention)
        {
            PredationInterventionResult result = RunPredationIntervention(config, ticks, applyIntervention);
            SimulationStatistics stats = result.FinalStatistics;
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12:F4},{13}",
                condition,
                config.WorldSeed,
                ticks,
                result.PopulationAtRemoval,
                result.PopulationAtReintroduction,
                result.HuntersRemoved,
                result.HuntersReintroduced,
                stats.Population,
                stats.BirthCount,
                stats.DeathCount,
                stats.PredationDeathCount,
                stats.AttackHitCount,
                stats.CumulativeCarcassConsumed,
                result.FinalStateHash));
        }

        private static PredationInterventionResult RunPredationIntervention(SimulationConfig config, int ticks, bool applyIntervention)
        {
            var world = new SimulationWorld(config);
            Prototype1Scenarios.Baseline.ApplyTo(world);
            int removalTick = ticks / 2;
            int reintroductionTick = (ticks * 3) / 4;
            int populationAtRemoval = 0;
            int populationAtReintroduction = 0;
            int huntersRemoved = 0;
            int huntersReintroduced = 0;
            int reintroductionCount = Math.Max(2, config.InitialPopulation / 5);

            for (int index = 0; index < ticks; index++)
            {
                if (world.CurrentTick == removalTick)
                {
                    populationAtRemoval = world.CreatureCount;
                    if (applyIntervention)
                    {
                        huntersRemoved = RemoveViableHunters(world);
                    }
                }

                if (world.CurrentTick == reintroductionTick)
                {
                    populationAtReintroduction = world.CreatureCount;
                    if (applyIntervention)
                    {
                        for (int founder = 0; founder < reintroductionCount; founder++)
                        {
                            world.Spawn(PredationFounderFactory.Create(config.WorldSeed, 100000 + founder));
                            huntersReintroduced++;
                        }
                    }
                }

                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            return new PredationInterventionResult(
                populationAtRemoval,
                populationAtReintroduction,
                huntersRemoved,
                huntersReintroduced,
                world.Statistics,
                world.ComputeStateHash());
        }

        private static int RemoveViableHunters(SimulationWorld world)
        {
            int removed = 0;
            for (int index = 0; index < world.CreatureCount; index++)
            {
                if (!PredationSystem.HasViableHuntingStrategy(world.Creatures.GetPhenotypeAt(index)))
                {
                    continue;
                }

                world.RequestDeath(world.GetCreatureIdAt(index), DeathCause.Debug);
                removed++;
            }

            return removed;
        }

        [MenuItem("Life Simulation/Run Predator-Prey Time Series")]
        public static void RunPredatorPreyTimeSeries()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "predator-prey-timeseries.csv");
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());
            int sampleInterval = Math.Max(1, options.Ticks / 40);

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("condition,seed,tick,population,viable_hunters,non_hunters,births,deaths,predation_deaths,attack_hits,food,water,carcass_food,attack,defense,aggression,diet_specialization");
                for (int offset = 0; offset < options.SeedCount; offset++)
                {
                    int seed = options.FirstSeed + offset;
                    WritePredationTimeSeries(writer, "prey-only", CreatePredationExperimentConfig(seed, options, FounderProfile.Prototype1), options.Ticks, sampleInterval);
                    WritePredationTimeSeries(writer, "mixed-predation", CreatePredationExperimentConfig(seed, options, FounderProfile.PredationVariation), options.Ticks, sampleInterval);
                }
            }

            Debug.Log($"Predator-prey time series saved to {outputPath}");
        }

        private static void WritePredationTimeSeries(StreamWriter writer, string condition, SimulationConfig config, int ticks, int sampleInterval)
        {
            var world = new SimulationWorld(config);
            Prototype1Scenarios.Baseline.ApplyTo(world);
            for (int index = 0; index < ticks; index++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
                if (world.CurrentTick % sampleInterval == 0 || world.CurrentTick == ticks)
                {
                    WritePredationTimeSample(writer, condition, config.WorldSeed, world.Statistics);
                }
            }
        }

        private static void WritePredationTimeSample(StreamWriter writer, string condition, int seed, SimulationStatistics stats)
        {
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10:F4},{11:F4},{12:F4},{13:F4},{14:F4},{15:F4},{16:F4}",
                condition,
                seed,
                stats.Tick,
                stats.Population,
                stats.ViableHunterCount,
                stats.NonHunterCount,
                stats.BirthCount,
                stats.DeathCount,
                stats.PredationDeathCount,
                stats.AttackHitCount,
                stats.AvailableFood,
                stats.AvailableWater,
                stats.CumulativeCarcassConsumed,
                stats.MeanAttackGene,
                stats.MeanDefenseGene,
                stats.MeanAggressionGene,
                stats.MeanDietSpecializationGene));
        }

        [MenuItem("Life Simulation/Run Cognition Control Experiments")]
        public static void RunCognitionControlExperiments()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "cognition-control.csv");
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("condition,seed,ticks,population,births,deaths,food,water,memory_capacity,memory_retention,learning_rate,exploration,state_hash");
                for (int offset = 0; offset < options.SeedCount; offset++)
                {
                    int seed = options.FirstSeed + offset;
                    WriteCognitionControlResult(writer, "memory-disabled", CreateCognitionExperimentConfig(seed, options, cognitionEnabled: false), options.Ticks);
                    WriteCognitionControlResult(writer, "memory-enabled", CreateCognitionExperimentConfig(seed, options, cognitionEnabled: true), options.Ticks);
                }
            }

            Debug.Log($"Cognition control results saved to {outputPath}");
        }

        private static SimulationConfig CreateCognitionExperimentConfig(int seed, ExperimentBatchOptions options, bool cognitionEnabled)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype2Defaults(seed, options.FounderPopulation);
            return new SimulationConfig(
                seed,
                options.FounderPopulation,
                defaults.Schedule,
                options.MaximumPopulation,
                FounderProfile.CognitionVariation,
                cognitionEnabled,
                physiologyEnabled: false,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1);
        }

        private static void WriteCognitionControlResult(StreamWriter writer, string condition, SimulationConfig config, int ticks)
        {
            ExperimentResult result = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks);
            SimulationStatistics stats = result.FinalStatistics;
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12}",
                condition,
                result.WorldSeed,
                result.CompletedTicks,
                stats.Population,
                stats.BirthCount,
                stats.DeathCount,
                stats.CumulativeFoodConsumed,
                stats.CumulativeWaterConsumed,
                stats.MeanMemoryCapacityGene,
                stats.MeanMemoryRetentionGene,
                stats.MeanLearningRateGene,
                stats.MeanExplorationGene,
                result.FinalStateHash));
        }

        [MenuItem("Life Simulation/Run Cognition Relocation Experiments")]
        public static void RunCognitionRelocationExperiments()
        {
            string rootPath = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(rootPath, "ExperimentResults");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "cognition-relocation.csv");
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(Environment.GetCommandLineArgs());

            using (var writer = new StreamWriter(outputPath, append: false))
            {
                writer.WriteLine("condition,seed,ticks,population,births,deaths,food,water,memory_capacity,memory_retention,learning_rate,exploration,state_hash");
                for (int offset = 0; offset < options.SeedCount; offset++)
                {
                    int seed = options.FirstSeed + offset;
                    WriteRelocationCognitionResult(writer, "memory-disabled", CreateCognitionExperimentConfig(seed, options, cognitionEnabled: false), options.Ticks);
                    WriteRelocationCognitionResult(writer, "memory-enabled", CreateCognitionExperimentConfig(seed, options, cognitionEnabled: true), options.Ticks);
                }
            }

            Debug.Log($"Cognition relocation results saved to {outputPath}");
        }

        private static void WriteRelocationCognitionResult(StreamWriter writer, string condition, SimulationConfig config, int ticks)
        {
            ExperimentResult result = RunRelocatingCognitionExperiment(config, ticks);
            SimulationStatistics stats = result.FinalStatistics;
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6:F4},{7:F4},{8:F4},{9:F4},{10:F4},{11:F4},{12}",
                condition, result.WorldSeed, result.CompletedTicks, stats.Population, stats.BirthCount, stats.DeathCount,
                stats.CumulativeFoodConsumed, stats.CumulativeWaterConsumed, stats.MeanMemoryCapacityGene,
                stats.MeanMemoryRetentionGene, stats.MeanLearningRateGene, stats.MeanExplorationGene, result.FinalStateHash));
        }

        private static ExperimentResult RunRelocatingCognitionExperiment(SimulationConfig config, int ticks)
        {
            var world = new SimulationWorld(config);
            Prototype1Scenarios.Baseline.ApplyTo(world);
            const int relocationInterval = 400;
            RelocateOases(world, useLeftOasis: true);
            for (int founderIndex = 0; founderIndex < world.CreatureCount; founderIndex++)
            {
                world.SetCreaturePosition(world.GetCreatureIdAt(founderIndex), new SimVector2(-10f, -8f));
            }
            for (int index = 0; index < ticks; index++)
            {
                if (world.CurrentTick > 0 && world.CurrentTick % relocationInterval == 0)
                {
                    bool useLeftOasis = ((world.CurrentTick / relocationInterval) & 1) == 0;
                    RelocateOases(world, useLeftOasis);
                }

                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            return new ExperimentResult(
                "cognition-relocation",
                config.WorldSeed,
                world.CurrentTick,
                world.Statistics,
                world.ComputeStateHash(),
                eventOverflowed: false,
                populationCapReached: world.CreatureCount >= config.MaximumPopulation);
        }

        private static void RelocateOases(SimulationWorld world, bool useLeftOasis)
        {
            SimVector2 foodPosition = useLeftOasis ? new SimVector2(-12f, -8f) : new SimVector2(10f, 12f);
            SimVector2 waterPosition = useLeftOasis ? new SimVector2(-7f, -8f) : new SimVector2(5f, 12f);
            for (int index = 0; index < world.Resources.Count; index++)
            {
                ResourceState resource = world.Resources.GetAt(index);
                if (resource.Kind == ResourceKind.Food) world.Resources.SetPosition(resource.Id, foodPosition);
                else if (resource.Kind == ResourceKind.Water) world.Resources.SetPosition(resource.Id, waterPosition);
            }
        }

        private readonly struct PredationInterventionResult
        {
            public PredationInterventionResult(int populationAtRemoval, int populationAtReintroduction, int huntersRemoved, int huntersReintroduced, SimulationStatistics finalStatistics, ulong finalStateHash)
            {
                PopulationAtRemoval = populationAtRemoval;
                PopulationAtReintroduction = populationAtReintroduction;
                HuntersRemoved = huntersRemoved;
                HuntersReintroduced = huntersReintroduced;
                FinalStatistics = finalStatistics;
                FinalStateHash = finalStateHash;
            }

            public int PopulationAtRemoval { get; }
            public int PopulationAtReintroduction { get; }
            public int HuntersRemoved { get; }
            public int HuntersReintroduced { get; }
            public SimulationStatistics FinalStatistics { get; }
            public ulong FinalStateHash { get; }
        }
    }
}
