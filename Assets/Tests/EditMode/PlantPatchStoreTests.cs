using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PlantPatchStoreTests
    {
        [Test]
        public void PlantPatchUsesStablePairedFoodResourceAndClampsConsumption()
        {
            var store = new PlantPatchStore(2);
            int index = store.Add(new ResourceId(7), new SimVector2(2f, 3f), 3f, 10f, .5f, .2f, 1.1f, .1f);

            Assert.That(store.GetAt(index).FoodResourceId.Value, Is.EqualTo(7));
            Assert.That(store.ConsumeAt(index, 9f), Is.EqualTo(3f));
            Assert.That(store.GetAt(index).Biomass, Is.EqualTo(0f));
        }

        [Test]
        public void ProjectionUsesPlantBiomassAndNutritionWithoutChangingTheResourceId()
        {
            var resources = new ResourceStore(1);
            ResourceId resourceId = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 10f, 10f, 1f);
            var patches = new PlantPatchStore(1);
            patches.Add(resourceId, new SimVector2(0f, 0f), 3f, 10f, .5f, .2f, 1.25f, 0f);

            PlantGrowthSystem.ProjectFoodResources(patches, resources);

            Assert.That(resources.GetAt(0).Id, Is.EqualTo(resourceId));
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(3f));
            Assert.That(resources.GetAt(0).NutritionMultiplier, Is.EqualTo(1.25f * PlantPhenotype.FromGenome(PlantGenome.Neutral).NutritionMultiplier));
        }

        [Test]
        public void PlantCloneMutationAndLineageAreDeterministic()
        {
            PlantGenome parent = new PlantGenome(.4f, .5f, .6f, .7f, .8f, .3f, .2f, .1f);
            PlantGenome first = PlantGenome.CloneMutated(parent, 42, 5, .03f);
            PlantGenome second = PlantGenome.CloneMutated(parent, 42, 5, .03f);
            var store = new PlantPatchStore(1);
            int index = store.Add(new ResourceId(3), new SimVector2(0f, 0f), 1f, 2f, .1f, 0f, 1f, 0f);
            store.SetGenomeAndLineage(index, first, new PlantLineage(new PlantPatchId(9), new PlantPatchId(2), 3));

            Assert.That(second.Growth, Is.EqualTo(first.Growth));
            Assert.That(store.GetAt(index).Lineage.Generation, Is.EqualTo(3));
            Assert.That(store.GetAt(index).Lineage.ParentId.Value, Is.EqualTo(2));
        }
    }
}
