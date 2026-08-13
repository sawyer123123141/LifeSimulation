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
    }
}
