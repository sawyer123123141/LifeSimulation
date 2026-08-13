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
