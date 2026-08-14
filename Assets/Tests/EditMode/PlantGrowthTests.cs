using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PlantGrowthTests
    {
        [Test]
        public void LogisticGrowthIsLimitedByTheEnvironmentAndCapacity()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 0f, 1f, 0f);

            float added = PlantGrowthSystem.Step(patches, new EnvironmentField(), 1f);

            float expectedGrowth = 1.6f * PlantPhenotype.FromGenome(PlantGenome.Neutral).GrowthRateMultiplier;
            Assert.That(added, Is.EqualTo(expectedGrowth).Within(.0001f));
            Assert.That(patches.GetAt(0).Biomass, Is.EqualTo(2f + expectedGrowth).Within(.0001f));
        }

        [Test]
        public void ZeroMoisturePreventsPlantGrowth()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 0f, 1f, 0f);

            Assert.That(PlantGrowthSystem.Step(patches, new EnvironmentField(moisture: 0f), 1f), Is.EqualTo(0f));
            Assert.That(patches.GetAt(0).Biomass, Is.EqualTo(2f));
        }

        [Test]
        public void PlantDefenseTradesProjectedFoodNutritionForGrowth()
        {
            PlantGenome lowDefense = new PlantGenome(.5f, .5f, .5f, .8f, 0f, .5f, .5f, .5f);
            PlantGenome highDefense = new PlantGenome(.5f, .5f, .5f, .8f, 1f, .5f, .5f, .5f);

            Assert.That(PlantPhenotype.FromGenome(highDefense).NutritionMultiplier, Is.LessThan(PlantPhenotype.FromGenome(lowDefense).NutritionMultiplier));
            Assert.That(PlantPhenotype.FromGenome(highDefense).GrowthRateMultiplier, Is.LessThan(PlantPhenotype.FromGenome(lowDefense).GrowthRateMultiplier));
        }
    }
}
