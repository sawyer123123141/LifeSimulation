using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ResourceExperimentTests
    {
        [Test]
        public void ResourceNutritionMultiplierChangesEnergyRecoveredWithoutChangingBiomassConsumed()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype3Defaults(42, 0);
            var poorWorld = new SimulationWorld(config);
            var richWorld = new SimulationWorld(config);
            poorWorld.Spawn();
            richWorld.Spawn();
            poorWorld.SetCreaturePosition(poorWorld.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            richWorld.SetCreaturePosition(richWorld.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            poorWorld.Creatures.GetNeedsRefAt(0).Energy = 0f;
            richWorld.Creatures.GetNeedsRefAt(0).Energy = 0f;
            poorWorld.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 10f, 10f, 0f, nutritionMultiplier: 0.5f);
            richWorld.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 2f, 10f, 10f, 0f, nutritionMultiplier: 1.5f);

            for (int index = 0; index < 10; index++)
            {
                poorWorld.Step(config.FixedDeltaTime);
                richWorld.Step(config.FixedDeltaTime);
            }

            Assert.That(richWorld.GetCreatureNeedsAt(0).Energy, Is.GreaterThan(poorWorld.GetCreatureNeedsAt(0).Energy));
            Assert.That(richWorld.Resources.GetAt(0).Amount, Is.EqualTo(poorWorld.Resources.GetAt(0).Amount).Within(0.0001f));
        }

        [Test]
        public void DroughtChangesOnlyWaterAvailabilityForPairedFounders()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 4);
            var baselineWorld = new SimulationWorld(config);
            var droughtWorld = new SimulationWorld(config);

            Prototype1Scenarios.Baseline.ApplyTo(baselineWorld);
            Prototype1Scenarios.Drought.ApplyTo(droughtWorld);
            ConsumeAllOfKind(baselineWorld, ResourceKind.Water, 1f);
            ConsumeAllOfKind(droughtWorld, ResourceKind.Water, 1f);
            baselineWorld.Resources.Regenerate(1f);
            droughtWorld.Resources.Regenerate(1f);

            Assert.That(droughtWorld.Creatures.GetGenomeAt(0).WaterEfficiency,
                Is.EqualTo(baselineWorld.Creatures.GetGenomeAt(0).WaterEfficiency));
            Assert.That(TotalAvailable(baselineWorld, ResourceKind.Food),
                Is.EqualTo(TotalAvailable(droughtWorld, ResourceKind.Food)));
            Assert.That(TotalAvailable(droughtWorld, ResourceKind.Water),
                Is.LessThan(TotalAvailable(baselineWorld, ResourceKind.Water)));
        }

        [Test]
        public void FoodScarcityChangesOnlyFoodAvailabilityForPairedFounders()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 4);
            var baselineWorld = new SimulationWorld(config);
            var scarcityWorld = new SimulationWorld(config);

            Prototype1Scenarios.Baseline.ApplyTo(baselineWorld);
            Prototype1Scenarios.FoodScarcity.ApplyTo(scarcityWorld);
            ConsumeAllOfKind(baselineWorld, ResourceKind.Food, 1f);
            ConsumeAllOfKind(scarcityWorld, ResourceKind.Food, 1f);
            baselineWorld.Resources.Regenerate(1f);
            scarcityWorld.Resources.Regenerate(1f);

            Assert.That(scarcityWorld.Creatures.GetGenomeAt(2).FoodEfficiency,
                Is.EqualTo(baselineWorld.Creatures.GetGenomeAt(2).FoodEfficiency));
            Assert.That(TotalAvailable(baselineWorld, ResourceKind.Water),
                Is.EqualTo(TotalAvailable(scarcityWorld, ResourceKind.Water)));
            Assert.That(TotalAvailable(scarcityWorld, ResourceKind.Food),
                Is.LessThan(TotalAvailable(baselineWorld, ResourceKind.Food)));
        }

        [Test]
        public void SameExperimentInputsProduceTheSameRecordedResult()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 4);
            ExperimentResult first = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks: 120);
            ExperimentResult second = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks: 120);

            Assert.That(first.ScenarioId, Is.EqualTo("baseline"));
            Assert.That(first.WorldSeed, Is.EqualTo(42));
            Assert.That(first.CompletedTicks, Is.EqualTo(120));
            Assert.That(second.FinalStateHash, Is.EqualTo(first.FinalStateHash));
            Assert.That(second.FinalStatistics.Population, Is.EqualTo(first.FinalStatistics.Population));
        }

        [Test]
        public void ScenarioCanPlaceAllFoundersAtAControlledLocation()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 3);
            var world = new SimulationWorld(config);
            var scenario = new SimulationScenario(
                "controlled-founders",
                new ResourceDefinition[0],
                founderPlacement: new SimVector2(2f, -3f));

            scenario.ApplyTo(world);

            for (int index = 0; index < world.CreatureCount; index++)
            {
                MovementState movement = world.GetCreatureMovementAt(index);
                Assert.That(movement.Position.X, Is.EqualTo(2f));
                Assert.That(movement.Position.Y, Is.EqualTo(-3f));
            }
        }

        [Test]
        public void Prototype4DefenseCalibrationChangesOnlyPlantDefenseBetweenPairedConditions()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 12);
            var controlWorld = new SimulationWorld(config);
            var defendedWorld = new SimulationWorld(config);

            Prototype4Scenarios.ConsumerDefenseCalibrationControl.ApplyTo(controlWorld);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(defendedWorld);

            Assert.That(controlWorld.Resources.Count, Is.EqualTo(defendedWorld.Resources.Count));
            Assert.That(controlWorld.Plants.Count, Is.EqualTo(defendedWorld.Plants.Count));
            Assert.That(controlWorld.Resources.GetAt(0).Position.X, Is.EqualTo(controlWorld.Resources.GetAt(1).Position.X));
            Assert.That(controlWorld.Resources.GetAt(0).Position.Y, Is.EqualTo(controlWorld.Resources.GetAt(1).Position.Y));
            Assert.That(controlWorld.Resources.GetAt(2).Position.X, Is.EqualTo(controlWorld.Resources.GetAt(3).Position.X));
            Assert.That(controlWorld.Resources.GetAt(2).Position.Y, Is.EqualTo(controlWorld.Resources.GetAt(3).Position.Y));
            for (int index = 0; index < controlWorld.CreatureCount; index++)
            {
                Assert.That(controlWorld.GetCreatureMovementAt(index).Position.X, Is.EqualTo(defendedWorld.GetCreatureMovementAt(index).Position.X));
                Assert.That(controlWorld.GetCreatureMovementAt(index).Position.Y, Is.EqualTo(defendedWorld.GetCreatureMovementAt(index).Position.Y));
            }

            for (int index = 0; index < controlWorld.Plants.Count; index++)
            {
                PlantPatchState control = controlWorld.Plants.GetAt(index);
                PlantPatchState defended = defendedWorld.Plants.GetAt(index);
                Assert.That(control.Biomass, Is.EqualTo(defended.Biomass));
                Assert.That(control.Capacity, Is.EqualTo(defended.Capacity));
                Assert.That(control.GrowthRate, Is.EqualTo(defended.GrowthRate));
                Assert.That(control.Genome.Defense, Is.EqualTo(0f));
                Assert.That(defended.Genome.Defense, Is.EqualTo(0.3f));
            }
        }

        [Test]
        public void Prototype4WatchableStarterHabitatClustersFoundersAtRenewablePairedResources()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(42, 4);
            var world = new SimulationWorld(config);

            Prototype4Scenarios.WatchableStarterHabitat.ApplyTo(world);

            Assert.That(world.Resources.Count, Is.EqualTo(4));
            Assert.That(world.Plants.Count, Is.EqualTo(2));
            Assert.That(world.Resources.GetAt(0).Capacity, Is.EqualTo(600f));
            Assert.That(world.Resources.GetAt(0).RegenerationPerSecond, Is.EqualTo(30f));
            Assert.That(world.Resources.GetAt(0).Position.X, Is.EqualTo(world.Resources.GetAt(1).Position.X));
            Assert.That(world.Resources.GetAt(0).Position.Y, Is.EqualTo(world.Resources.GetAt(1).Position.Y));
            for (int index = 0; index < world.CreatureCount; index++)
            {
                MovementState movement = world.GetCreatureMovementAt(index);
                Assert.That(movement.Position.X, Is.EqualTo(-12f));
                Assert.That(movement.Position.Y, Is.EqualTo(-8f));
            }
        }

        [Test]
        public void ResourceStoreCanRelocateAnExistingResourceWithoutChangingItsBudget()
        {
            var resources = new ResourceStore(initialCapacity: 1);
            ResourceId id = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 10f, 0.5f);

            resources.SetPosition(id, new SimVector2(7f, -3f));

            ResourceState moved = resources.GetAt(0);
            Assert.That(moved.Position.X, Is.EqualTo(7f));
            Assert.That(moved.Position.Y, Is.EqualTo(-3f));
            Assert.That(moved.Amount, Is.EqualTo(5f));
            Assert.That(moved.Capacity, Is.EqualTo(10f));
        }

        [Test]
        public void HeadlessExperimentDrainsLifecycleEventsWithoutInvalidatingTheRun()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 20);

            ExperimentResult result = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks: 2000);

            Assert.That(result.EventOverflowed, Is.False);
            Assert.That(result.PopulationCapReached, Is.False);
        }

        [Test]
        public void ExperimentStatisticsReportCumulativeResourceConsumption()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 20);

            ExperimentResult result = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks: 1000);

            Assert.That(result.FinalStatistics.CumulativeFoodConsumed, Is.GreaterThan(0f));
            Assert.That(result.FinalStatistics.CumulativeWaterConsumed, Is.GreaterThan(0f));
        }

        [Test]
        public void BaselineScenarioSustainsFoundersLongEnoughForTheFirstMatureCycle()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 20);

            ExperimentResult result = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks: 400);

            Assert.That(result.FinalStatistics.Population, Is.GreaterThan(0));
        }

        [Test]
        public void BaselineScenarioProducesAnOffspringWithinTheFirstExperimentWindow()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 20);

            ExperimentResult result = ExperimentRunner.Run(config, Prototype1Scenarios.Baseline, ticks: 1000);

            Assert.That(result.FinalStatistics.BirthCount, Is.GreaterThan(0));
        }

        [Test]
        public void BenchmarkReportsTheExactMeasuredTickCount()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 20);

            SimulationBenchmarkResult result = SimulationBenchmark.Run(
                config,
                Prototype1Scenarios.Baseline,
                warmupTicks: 10,
                measuredTicks: 100);

            Assert.That(result.MeasuredTicks, Is.EqualTo(100));
            Assert.That(result.FinalPopulation, Is.GreaterThan(0));
            Assert.That(result.AverageStepMilliseconds, Is.GreaterThanOrEqualTo(0d));
            Assert.That(result.P95StepMilliseconds, Is.GreaterThanOrEqualTo(0d));
        }

        [Test]
        public void ScenarioResourceBudgetScalesWithTheFounderPopulation()
        {
            var smallWorld = new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(42, 4));
            var largeWorld = new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(42, 20));

            Prototype1Scenarios.Baseline.ApplyTo(smallWorld);
            Prototype1Scenarios.Baseline.ApplyTo(largeWorld);

            Assert.That(TotalAvailable(largeWorld, ResourceKind.Water),
                Is.EqualTo(TotalAvailable(smallWorld, ResourceKind.Water) * 5f));
        }

        [Test]
        public void PairedAnalysisReportsTreatmentShiftAndDirectionConsistency()
        {
            ExperimentResult[] baseline =
            {
                CreateResult("baseline", 42, waterEfficiency: 0.40f),
                CreateResult("baseline", 43, waterEfficiency: 0.60f),
            };
            ExperimentResult[] drought =
            {
                CreateResult("drought", 42, waterEfficiency: 0.70f),
                CreateResult("drought", 43, waterEfficiency: 0.70f),
            };

            PairedExperimentSummary summary = PairedExperimentAnalysis.Summarize(
                baseline,
                drought,
                ExperimentMetric.WaterEfficiency);

            Assert.That(summary.PairCount, Is.EqualTo(2));
            Assert.That(summary.MeanTreatmentMinusControl, Is.EqualTo(0.20f).Within(0.0001f));
            Assert.That(summary.PositiveDifferenceCount, Is.EqualTo(2));
            Assert.That(summary.DirectionConsistency, Is.EqualTo(1f));
        }

        [Test]
        public void PairedAnalysisExposesPhysiologyTraitMetrics()
        {
            ExperimentResult[] control = { CreateResult("p3-control", 42, waterEfficiency: 0.5f, temperatureTolerance: 0.3f) };
            ExperimentResult[] treatment = { CreateResult("p3-treatment", 42, waterEfficiency: 0.5f, temperatureTolerance: 0.7f) };

            PairedExperimentSummary summary = PairedExperimentAnalysis.Summarize(
                control,
                treatment,
                ExperimentMetric.TemperatureTolerance);

            Assert.That(summary.MeanTreatmentMinusControl, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void BootstrapAnalysisKeepsAUniformPositivePairedEffectAboveZero()
        {
            ExperimentResult[] baseline =
            {
                CreateResult("baseline", 42, waterEfficiency: 0.40f),
                CreateResult("baseline", 43, waterEfficiency: 0.40f),
                CreateResult("baseline", 44, waterEfficiency: 0.40f),
            };
            ExperimentResult[] drought =
            {
                CreateResult("drought", 42, waterEfficiency: 0.70f),
                CreateResult("drought", 43, waterEfficiency: 0.70f),
                CreateResult("drought", 44, waterEfficiency: 0.70f),
            };

            PairedBootstrapInterval interval = PairedBootstrapAnalysis.EstimateMeanDifferenceInterval(
                baseline,
                drought,
                ExperimentMetric.WaterEfficiency,
                resampleCount: 128,
                randomSeed: 42);

            Assert.That(interval.LowerBound, Is.GreaterThan(0f));
            Assert.That(interval.UpperBound, Is.EqualTo(0.30f).Within(0.0001f));
        }

        [Test]
        public void EvolutionCriterionRequiresIntervalEffectSizeAndConsistentDirection()
        {
            var summary = new PairedExperimentSummary(
                pairCount: 20,
                meanTreatmentMinusControl: 0.12f,
                positiveDifferenceCount: 16,
                negativeDifferenceCount: 2);
            var interval = new PairedBootstrapInterval(0.03f, 0.20f);

            PairedEvolutionAssessment assessment = PairedEvolutionCriterion.Assess(
                summary,
                interval,
                standardizedEffect: 0.60f);

            Assert.That(assessment.MeetsStatisticalCriterion, Is.True);
            Assert.That(assessment.RequiresMechanismEvidence, Is.True);
        }

        [Test]
        public void PairedAnalysisCalculatesAStandardizedWithinPairEffect()
        {
            ExperimentResult[] baseline =
            {
                CreateResult("baseline", 42, waterEfficiency: 0.20f),
                CreateResult("baseline", 43, waterEfficiency: 0.30f),
                CreateResult("baseline", 44, waterEfficiency: 0.40f),
            };
            ExperimentResult[] drought =
            {
                CreateResult("drought", 42, waterEfficiency: 0.30f),
                CreateResult("drought", 43, waterEfficiency: 0.50f),
                CreateResult("drought", 44, waterEfficiency: 0.70f),
            };

            float effect = PairedExperimentAnalysis.CalculateStandardizedEffect(
                baseline,
                drought,
                ExperimentMetric.WaterEfficiency);

            Assert.That(effect, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void ExperimentBatchOptionsAcceptsAReproducibleLargeSweep()
        {
            ExperimentBatchOptions options = ExperimentBatchOptions.Parse(new[]
            {
                "-lifeSimFirstSeed", "100",
                "-lifeSimSeedCount", "20",
                "-lifeSimFounders", "80",
                "-lifeSimMaximumPopulation", "1500",
                "-lifeSimTicks", "50000",
            });

            Assert.That(options.FirstSeed, Is.EqualTo(100));
            Assert.That(options.SeedCount, Is.EqualTo(20));
            Assert.That(options.FounderPopulation, Is.EqualTo(80));
            Assert.That(options.MaximumPopulation, Is.EqualTo(1500));
            Assert.That(options.Ticks, Is.EqualTo(50000));
        }

        private static float TotalAvailable(SimulationWorld world, ResourceKind kind)
        {
            float total = 0f;
            for (int index = 0; index < world.Resources.Count; index++)
            {
                ResourceState resource = world.Resources.GetAt(index);
                if (resource.Kind == kind && resource.IsActive)
                {
                    total += resource.Amount;
                }
            }

            return total;
        }

        private static void ConsumeAllOfKind(SimulationWorld world, ResourceKind kind, float amountPerResource)
        {
            for (int index = 0; index < world.Resources.Count; index++)
            {
                if (world.Resources.GetAt(index).Kind == kind)
                {
                    world.Resources.ConsumeAt(index, amountPerResource);
                }
            }
        }

        private static ExperimentResult CreateResult(string scenarioId, int seed, float waterEfficiency, float temperatureTolerance = 0f)
        {
            var statistics = new SimulationStatistics(
                tick: 100,
                population: 10,
                highestGeneration: 1,
                meanBodySizeGene: 0.5f,
                meanMovementSpeedGene: 0.5f,
                meanMetabolicPaceGene: 0.5f,
                meanVisionRangeGene: 0.5f,
                meanWaterEfficiencyGene: waterEfficiency,
                meanFoodEfficiencyGene: 0.5f,
                meanEnergyFraction: 0.5f,
                meanHydrationFraction: 0.5f,
                availableFood: 20f,
                availableWater: 20f,
                cumulativeFoodConsumed: 0f,
                cumulativeWaterConsumed: 0f,
                birthCount: 5,
                deathCount: 2,
                meanTemperatureToleranceGene: temperatureTolerance);
            return new ExperimentResult(
                scenarioId,
                seed,
                100,
                statistics,
                finalStateHash: 0UL,
                eventOverflowed: false,
                populationCapReached: false);
        }
    }
}
