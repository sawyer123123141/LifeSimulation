using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ResourceExperimentTests
    {
        [Test]
        public void DroughtChangesOnlyWaterAvailabilityForPairedFounders()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 4);
            var baselineWorld = new SimulationWorld(config);
            var droughtWorld = new SimulationWorld(config);

            Prototype1Scenarios.Baseline.ApplyTo(baselineWorld);
            Prototype1Scenarios.Drought.ApplyTo(droughtWorld);

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

        private static ExperimentResult CreateResult(string scenarioId, int seed, float waterEfficiency)
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
                birthCount: 5,
                deathCount: 2);
            return new ExperimentResult(scenarioId, seed, 100, statistics, finalStateHash: 0UL);
        }
    }
}
