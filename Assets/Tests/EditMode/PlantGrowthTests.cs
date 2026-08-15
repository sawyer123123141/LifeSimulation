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
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 1f, 0f);

            float added = PlantGrowthSystem.Step(patches, new EnvironmentField(), 1f);

            float expectedGrowth = 1.6f * PlantPhenotype.FromGenome(PlantGenome.Neutral).GrowthRateMultiplier;
            Assert.That(added, Is.EqualTo(expectedGrowth).Within(.0001f));
            Assert.That(patches.GetAt(0).Biomass, Is.EqualTo(2f + expectedGrowth).Within(.0001f));
        }

        [Test]
        public void ZeroMoisturePreventsPlantGrowth()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 1f, 0f);

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

        [Test]
        public void MaturePlantTransfersBiomassToADeterministicClonalSeedling()
        {
            var resources = new ResourceStore(1);
            ResourceId childSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
            resources.SetActive(childSite, false);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, ref ordinal);

            Assert.That(births, Is.EqualTo(1));
            Assert.That(resources.GetAt(0).IsActive, Is.True);
            Assert.That(patches.Count, Is.EqualTo(2));
            Assert.That(patches.GetAt(0).Biomass + patches.GetAt(1).Biomass, Is.EqualTo(10f).Within(.0001f));
            Assert.That(patches.GetAt(1).Lineage.Generation, Is.EqualTo(patches.GetAt(parentIndex).Lineage.Generation + 1));
        }

        [Test]
        public void EstablishmentSuccessProbabilityFallsLinearlyWithDistanceAcrossDispersalRange()
        {
            Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(0f, 10f), Is.EqualTo(1f));
            Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(10f, 10f), Is.EqualTo(0f));
            Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(5f, 10f), Is.EqualTo(.5f).Within(.0001f));
            Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(15f, 10f), Is.EqualTo(0f));
        }

        [Test]
        public void EstablishmentSuccessProbabilityDoesNotDivideByZeroWhenDispersalRangeIsZero()
        {
            Assert.That(PlantReproductionSystem.EstablishmentSuccessProbability(1f, 0f), Is.EqualTo(0f));
            Assert.That(() => PlantReproductionSystem.EstablishmentSuccessProbability(1f, 0f), Throws.Nothing);
        }

        [Test]
        public void StepProducesNoBirthsWhenTheRegistryHasNoSites()
        {
            var resources = new ResourceStore(1);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
            var sites = new PlantSiteRegistry(1);
            var patches = new PlantPatchStore(2);
            patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            Assert.That(() => PlantReproductionSystem.Step(patches, resources, sites, 42, 20, ref ordinal), Throws.Nothing);
            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, ref ordinal);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(patches.Count, Is.EqualTo(1));
        }

        [Test]
        public void SiteWithinRangeThatFailsItsEstablishmentRollLetsStepRetryTheNextAttempt()
        {
            // With worldSeed=4, tick=1, this parent's (auto-assigned Id 1) first establishment
            // roll (attempt 0) is ~0.900, which fails against the 0.5 success probability at
            // distance 2 of a 4-unit dispersal range (default Dispersal=0 => range=4). The
            // second attempt's roll (~0.144) succeeds, so the site is only reached via retry.
            var resources = new ResourceStore(1);
            ResourceId site = resources.Add(ResourceKind.Food, new SimVector2(2f, 0f), 1f, 0f, 12f, 0f);
            resources.SetActive(site, false);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 4, 1, ref ordinal);

            Assert.That(births, Is.EqualTo(1));
            Assert.That(resources.GetAt(0).IsActive, Is.True);
            Assert.That(patches.Count, Is.EqualTo(2));
        }

        [Test]
        public void SameSeedTickAndOrdinalProduceTheSameEstablishmentOutcome()
        {
            (int Births, long OrdinalAfter, float ChildBiomass) RunOnce()
            {
                var resources = new ResourceStore(1);
                ResourceId childSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
                resources.SetActive(childSite, false);
                var sites = new PlantSiteRegistry(1);
                sites.Register(0);
                var patches = new PlantPatchStore(2);
                patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
                long ordinal = 0;

                int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, ref ordinal);
                float childBiomass = patches.Count > 1 ? patches.GetAt(1).Biomass : -1f;
                return (births, ordinal, childBiomass);
            }

            var first = RunOnce();
            var second = RunOnce();

            Assert.That(second.Births, Is.EqualTo(first.Births));
            Assert.That(second.OrdinalAfter, Is.EqualTo(first.OrdinalAfter));
            Assert.That(second.ChildBiomass, Is.EqualTo(first.ChildBiomass));
        }
    }
}
