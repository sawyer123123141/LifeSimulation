using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
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

            float expectedGrowth = 1.68f * PlantPhenotype.FromGenome(PlantGenome.Neutral).GrowthRateMultiplier;
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
        public void ZeroBiomassPatchStillProducesNonzeroGrowth()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 0f, 10f, 1f, 1f, 0f);

            float added = PlantGrowthSystem.Step(patches, new EnvironmentField(), 1f);

            Assert.That(added, Is.GreaterThan(0f));
            Assert.That(patches.GetAt(0).Biomass, Is.GreaterThan(0f));
        }

        [Test]
        public void SproutFloorContributionIsSmallRelativeToNormalGrowthAtHalfCapacity()
        {
            var patches = new PlantPatchStore(1);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 5f, 10f, 1f, 1f, 0f);

            float addedWithFloor = PlantGrowthSystem.Step(patches, new EnvironmentField(), 1f);

            float mult = PlantPhenotype.FromGenome(PlantGenome.Neutral).GrowthRateMultiplier;
            float growthWithoutFloor = 1f * mult * 5f * (1f - (5f / 10f)) * 1f * 1f;

            float relativeDifference = (addedWithFloor - growthWithoutFloor) / growthWithoutFloor;
            Assert.That(relativeDifference, Is.LessThan(0.05f));
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

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal);

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
            // A valid inactive Food resource exists in the store, but it is never registered in
            // the PlantSiteRegistry. This distinguishes "FindSite searches only the registry"
            // (0 births, correct) from a whole-store scan (which would find this resource and
            // produce 1 birth), pinning the Task 3 registry-scoped search behaviour.
            var resources = new ResourceStore(1);
            ResourceId unregisteredSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
            resources.SetActive(unregisteredSite, false);
            var sites = new PlantSiteRegistry(1);
            var patches = new PlantPatchStore(2);
            patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            Assert.That(() => PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal), Throws.Nothing);
            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(patches.Count, Is.EqualTo(1));
        }

        [Test]
        public void SiteJustInsideDispersalRangeWithNearZeroEstablishmentProbabilityUsuallyFailsTheRoll()
        {
            // A patch created via PlantPatchStore.Add starts with PlantGenome.Neutral
            // (Dispersal=.5), giving DispersalRange = 4 + 20*.5 = 14 (see
            // PlantPhenotype.FromGenome). Placing the site at 0.99 * range means
            // EstablishmentSuccessProbability is ~0.01, so the establishment roll fails on
            // every attempt for this seed/tick (verified by brute-force replay of
            // DeterministicRandom's output), producing 0 births. If the roll-against-probability
            // check in FindSite were deleted, this site (in range and inactive Food) would
            // always be accepted and this test would go red (0 -> 1 births), so it pins the
            // establishment roll to actually gating FindSite.
            const float range = 14f;
            const float distance = 0.99f * range;
            var resources = new ResourceStore(1);
            ResourceId site = resources.Add(ResourceKind.Food, new SimVector2(distance, 0f), 1f, 0f, 12f, 0f);
            resources.SetActive(site, false);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 1, 1, 1f, ref ordinal);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(resources.GetAt(0).IsActive, Is.False);
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

            int births = PlantReproductionSystem.Step(patches, resources, sites, 4, 1, 1f, ref ordinal);

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

                int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal);
                float childBiomass = patches.Count > 1 ? patches.GetAt(1).Biomass : -1f;
                return (births, ordinal, childBiomass);
            }

            var first = RunOnce();
            var second = RunOnce();

            Assert.That(second.Births, Is.EqualTo(first.Births));
            Assert.That(second.OrdinalAfter, Is.EqualTo(first.OrdinalAfter));
            Assert.That(second.ChildBiomass, Is.EqualTo(first.ChildBiomass));
        }

        // PlantReproductionSystem.ReproductionCooldownSeconds is private, matching this file's
        // existing constant convention (MaturityFraction etc. are also private), so this value
        // is hardcoded here and must be kept in sync with that constant (currently 20f).
        private const float ReproductionCooldownSeconds = 20f;

        [Test]
        public void SuccessfulEstablishmentStartsAReproductionCooldownOnTheParent()
        {
            var resources = new ResourceStore(1);
            ResourceId childSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
            resources.SetActive(childSite, false);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal);

            Assert.That(patches.GetAt(parentIndex).ReproductionCooldownRemaining, Is.EqualTo(ReproductionCooldownSeconds));
        }

        [Test]
        public void NewlyCreatedPatchHasNoReproductionCooldown()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 2f, 10f, 1f, 1f, 0f);

            Assert.That(patches.GetAt(index).ReproductionCooldownRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void ParentOnCooldownIsSkippedAndProducesNoSecondBirthOnImmediateReStep()
        {
            var resources = new ResourceStore(2);
            ResourceId firstSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
            resources.SetActive(firstSite, false);
            ResourceId secondSite = resources.Add(ResourceKind.Food, new SimVector2(-1f, 0f), 1f, 0f, 12f, 0f);
            resources.SetActive(secondSite, false);
            var sites = new PlantSiteRegistry(2);
            sites.Register(0);
            sites.Register(1);
            var patches = new PlantPatchStore(3);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            int firstStepBirths = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal);
            Assert.That(firstStepBirths, Is.EqualTo(1));
            Assert.That(patches.GetAt(parentIndex).ReproductionCooldownRemaining, Is.EqualTo(ReproductionCooldownSeconds));

            int patchCountAfterFirstBirth = patches.Count;
            int secondStepBirths = PlantReproductionSystem.Step(patches, resources, sites, 42, 21, 1f, ref ordinal);

            Assert.That(secondStepBirths, Is.EqualTo(0));
            Assert.That(patches.Count, Is.EqualTo(patchCountAfterFirstBirth));
        }

        [Test]
        public void CooldownDecaysToZeroAfterEnoughCumulativeDeltaTimeAndParentBecomesEligibleAgain()
        {
            // The resource at index 0 is never deactivated, so PlantSiteRegistry's candidate is
            // always rejected by the IsActive check in FindSite and the parent can never
            // successfully reproduce again in this test; that isolates the assertion to the
            // cooldown's decay-to-zero mechanics (and the fact that a cooldown of exactly 0 no
            // longer causes Step to skip the parent) without depending on the RNG-driven
            // site-establishment outcome.
            var resources = new ResourceStore(1);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            patches.SetReproductionCooldown(parentIndex, ReproductionCooldownSeconds);
            long ordinal = 0;

            for (int i = 0; i < 19; i++)
            {
                PlantReproductionSystem.Step(patches, resources, sites, 42, 20 + i, 1f, ref ordinal);
                Assert.That(patches.GetAt(parentIndex).ReproductionCooldownRemaining, Is.GreaterThan(0f));
            }

            PlantReproductionSystem.Step(patches, resources, sites, 42, 39, 1f, ref ordinal);

            Assert.That(patches.GetAt(parentIndex).ReproductionCooldownRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void FailedReproductionAttemptDoesNotStartACooldown()
        {
            var resources = new ResourceStore(1);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 0f, 12f, 0f);
            var sites = new PlantSiteRegistry(1);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(patches.GetAt(parentIndex).ReproductionCooldownRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void PlantSiteCompetitionEnabledDefaultsToFalse()
        {
            var config = SimulationConfig.CreatePrototype4Defaults(42, 4);
            Assert.That(config.PlantSiteCompetitionEnabled, Is.False);
        }

        [Test]
        public void PlantSiteCompetitionEnabledCanBeSetTrue()
        {
            var defaults = SimulationConfig.CreatePrototype4Defaults(42, 4);
            var config = new SimulationConfig(
                42,
                4,
                defaults.Schedule,
                defaults.MaximumPopulation,
                defaults.FounderProfile,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                plantSiteCompetitionEnabled: true);

            Assert.That(config.PlantSiteCompetitionEnabled, Is.True);
        }

        [Test]
        public void ReplaceAtOverwritesTraitsAndGenomeButPreservesSiteIdentity()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(7), new SimVector2(3f, 4f), 5f, 20f, .1f, 1f, 0f);
            PlantPatchId originalId = patches.GetAt(index).Id;
            var newGenome = new PlantGenome(.9f, .1f, .2f, .3f, .4f, .5f, .6f, .7f);
            var newLineage = new PlantLineage(originalId, new PlantPatchId(11), 3);

            patches.ReplaceAt(index, newGenome, newLineage, biomass: 15f, growthRate: .3f, nutrition: .6f, defense: .2f);

            PlantPatchState result = patches.GetAt(index);
            Assert.That(result.Id, Is.EqualTo(originalId));
            Assert.That(result.FoodResourceId, Is.EqualTo(new ResourceId(7)));
            Assert.That(result.Position.X, Is.EqualTo(3f));
            Assert.That(result.Position.Y, Is.EqualTo(4f));
            Assert.That(result.Capacity, Is.EqualTo(20f));
            Assert.That(result.Biomass, Is.EqualTo(15f));
            Assert.That(result.GrowthRate, Is.EqualTo(.3f));
            Assert.That(result.Nutrition, Is.EqualTo(.6f));
            Assert.That(result.Defense, Is.EqualTo(.2f));
            Assert.That(result.Genome.Dispersal, Is.EqualTo(newGenome.Dispersal));
            Assert.That(result.Lineage.Generation, Is.EqualTo(3));
            Assert.That(result.ReproductionCooldownRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void ReplaceAtResetsAgeSoATakeoverStartsANewPatchLife()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(7), new SimVector2(0f, 0f), 5f, 20f, .1f, 1f, 0f);
            patches.AdvanceAge(index, 80f);
            Assert.That(patches.GetAt(index).Age, Is.EqualTo(80f), "the incumbent must actually be old before the takeover");

            patches.ReplaceAt(index, PlantGenome.Neutral, new PlantLineage(patches.GetAt(index).Id, new PlantPatchId(11), 3), biomass: 2f, growthRate: .1f, nutrition: 1f, defense: 0f);

            Assert.That(patches.GetAt(index).Age, Is.EqualTo(0f),
                "a takeover installs a new seedling; inheriting the incumbent's age makes it die on someone else's clock");
        }

        [Test]
        public void ATakenOverSeedlingIsNotKilledByTheIncumbentsAccumulatedAge()
        {
            var resources = new ResourceStore(1);
            ResourceId siteId = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 20f, 0f);
            var patches = new PlantPatchStore(1);
            int index = patches.Add(siteId, new SimVector2(0f, 0f), 5f, 20f, .1f, 1f, 0f);

            // Age the incumbent to just under its lifespan, then let a seedling take the site.
            float lifespan = PlantPhenotype.FromGenome(PlantGenome.Neutral).LifespanSeconds;
            patches.AdvanceAge(index, lifespan - 1f);
            patches.ReplaceAt(index, PlantGenome.Neutral, new PlantLineage(patches.GetAt(index).Id, new PlantPatchId(11), 3), biomass: 2f, growthRate: .1f, nutrition: 1f, defense: 0f);

            PlantMortalitySystem.Step(patches, resources, deltaTime: 2f);

            Assert.That(patches.Count, Is.EqualTo(1),
                "the replacement seedling is two seconds old and must not be aged out by the patch it replaced");
            Assert.That(resources.GetAt(0).IsActive, Is.True);
        }

        [Test]
        public void ReplaceAtClampsBiomassToCapacity()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(7), new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);

            patches.ReplaceAt(index, PlantGenome.Neutral, new PlantLineage(patches.GetAt(index).Id, default, 1), biomass: 999f, growthRate: .1f, nutrition: 1f, defense: 0f);

            Assert.That(patches.GetAt(index).Biomass, Is.EqualTo(10f));
        }

        [Test]
        public void CompetitionDisabledNeverConsidersAnOccupiedCandidateEvenIfVulnerable()
        {
            var resources = new ResourceStore(1);
            ResourceId occupiedSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 1f, 20f, 0f);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            int occupantIndex = patches.Add(occupiedSite, new SimVector2(1f, 0f), 1f, 20f, .2f, .8f, .1f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal, competitionEnabled: false);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(patches.Count, Is.EqualTo(2));
            Assert.That(patches.GetAt(occupantIndex).Biomass, Is.EqualTo(1f));
            Assert.That(patches.GetAt(parentIndex).Biomass, Is.EqualTo(10f));
        }

        [Test]
        public void CompetitionEnabledLetsAVulnerableOccupiedSiteBeTakenOverByADisperser()
        {
            var resources = new ResourceStore(1);
            ResourceId occupiedSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 1f, 20f, 0f);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            int occupantIndex = patches.Add(occupiedSite, new SimVector2(1f, 0f), 1f, 20f, .2f, .8f, .1f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal, competitionEnabled: true);

            Assert.That(births, Is.EqualTo(1));
            Assert.That(patches.Count, Is.EqualTo(2));
            PlantPatchState takenOver = patches.GetAt(occupantIndex);
            Assert.That(takenOver.GrowthRate, Is.EqualTo(.1f));
            Assert.That(takenOver.Nutrition, Is.EqualTo(1f));
            Assert.That(takenOver.Defense, Is.EqualTo(0f));
            Assert.That(takenOver.Lineage.Generation, Is.EqualTo(patches.GetAt(parentIndex).Lineage.Generation + 1));
            Assert.That(takenOver.ReproductionCooldownRemaining, Is.EqualTo(0f));
            // Biomass conservation: nothing created or destroyed, only moved.
            float totalAfter = patches.GetAt(parentIndex).Biomass + takenOver.Biomass;
            Assert.That(totalAfter, Is.EqualTo(10f + 1f).Within(.0001f));
        }

        [Test]
        public void CompetitionEnabledNeverDisplacesANonVulnerableOccupiedSite()
        {
            var resources = new ResourceStore(1);
            ResourceId occupiedSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 5f, 20f, 0f);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            int occupantIndex = patches.Add(occupiedSite, new SimVector2(1f, 0f), 5f, 20f, .2f, .8f, .1f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal, competitionEnabled: true);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(patches.Count, Is.EqualTo(2));
            Assert.That(patches.GetAt(occupantIndex).Biomass, Is.EqualTo(5f));
            Assert.That(patches.GetAt(occupantIndex).GrowthRate, Is.EqualTo(.2f));
            Assert.That(patches.GetAt(parentIndex).Biomass, Is.EqualTo(10f));
        }

        [Test]
        public void RemoveAtSwapsTheLastPatchIntoTheVacatedSlotWithEveryFieldIntact()
        {
            var patches = new PlantPatchStore(3);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 1f, 10f, .1f, 1f, 0f);
            patches.Add(new ResourceId(2), new SimVector2(1f, 1f), 2f, 20f, .2f, .9f, .1f);
            int lastIndex = patches.Add(new ResourceId(3), new SimVector2(3f, 4f), 7f, 30f, .3f, .8f, .2f);
            var lastGenome = new PlantGenome(.9f, .1f, .2f, .3f, .4f, .5f, .6f, .7f);
            patches.SetGenomeAndLineage(lastIndex, lastGenome, new PlantLineage(patches.GetAt(lastIndex).Id, new PlantPatchId(42), 5));
            PlantPatchId survivingId = patches.GetAt(lastIndex).Id;

            patches.RemoveAt(1);

            Assert.That(patches.Count, Is.EqualTo(2));
            PlantPatchState moved = patches.GetAt(1);
            Assert.That(moved.Id, Is.EqualTo(survivingId));
            Assert.That(moved.FoodResourceId, Is.EqualTo(new ResourceId(3)));
            Assert.That(moved.Position.X, Is.EqualTo(3f));
            Assert.That(moved.Position.Y, Is.EqualTo(4f));
            Assert.That(moved.Biomass, Is.EqualTo(7f));
            Assert.That(moved.Capacity, Is.EqualTo(30f));
            Assert.That(moved.GrowthRate, Is.EqualTo(.3f));
            Assert.That(moved.Nutrition, Is.EqualTo(.8f));
            Assert.That(moved.Defense, Is.EqualTo(.2f));
            Assert.That(moved.Genome.Growth, Is.EqualTo(.9f));
            Assert.That(moved.Lineage.Generation, Is.EqualTo(5));
            Assert.That(patches.FindIndex(new ResourceId(1)), Is.EqualTo(0));
            Assert.That(patches.FindIndex(new ResourceId(3)), Is.EqualTo(1));
            Assert.That(patches.FindIndex(new ResourceId(2)), Is.EqualTo(-1));
        }

        [Test]
        public void AdvanceAgeAccumulatesElapsedTimeOnAPatch()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 1f, 10f, .1f, 1f, 0f);

            Assert.That(patches.GetAt(index).Age, Is.EqualTo(0f));
            patches.AdvanceAge(index, 1.5f);
            patches.AdvanceAge(index, 2.5f);

            Assert.That(patches.GetAt(index).Age, Is.EqualTo(4f).Within(.0001f));
        }

        [Test]
        public void SlowestGrowerLivesExactlyTwiceAsLongAsFastestGrower()
        {
            var slow = new PlantGenome(0f, .5f, .5f, .5f, .5f, .5f, .5f, .5f);
            var fast = new PlantGenome(1f, .5f, .5f, .5f, .5f, .5f, .5f, .5f);

            float slowLifespan = PlantPhenotype.FromGenome(slow).LifespanSeconds;
            float fastLifespan = PlantPhenotype.FromGenome(fast).LifespanSeconds;

            Assert.That(slowLifespan, Is.EqualTo(fastLifespan * 2f).Within(.0001f));
            Assert.That(slowLifespan, Is.EqualTo(PlantPhenotype.BaseLifespanSeconds * 1.5f).Within(.0001f));
            Assert.That(fastLifespan, Is.EqualTo(PlantPhenotype.BaseLifespanSeconds * .75f).Within(.0001f));
        }

        [Test]
        public void PatchIsRemovedOnTheStepItsAgeReachesItsLifespanAndNotBefore()
        {
            var resources = new ResourceStore(1);
            ResourceId site = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 10f, 0f);
            var patches = new PlantPatchStore(1);
            int index = patches.Add(site, new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);
            float lifespan = PlantPhenotype.FromGenome(patches.GetAt(index).Genome).LifespanSeconds;

            float elapsed = 0f;
            while (elapsed + 1f < lifespan)
            {
                PlantMortalitySystem.Step(patches, resources, 1f);
                elapsed += 1f;
                Assert.That(patches.Count, Is.EqualTo(1), $"patch died early at age {elapsed}");
            }

            float removedBiomass = PlantMortalitySystem.Step(patches, resources, 1f);

            Assert.That(patches.Count, Is.EqualTo(0));
            Assert.That(removedBiomass, Is.EqualTo(5f).Within(.0001f));
        }

        [Test]
        public void DyingPatchClearsItsFoodProjectionAndFreesItsSite()
        {
            var resources = new ResourceStore(1);
            ResourceId site = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 10f, 0f);
            var patches = new PlantPatchStore(1);
            patches.Add(site, new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);

            PlantMortalitySystem.Step(patches, resources, 10000f);

            Assert.That(patches.Count, Is.EqualTo(0));
            Assert.That(resources.GetAt(0).IsActive, Is.False);
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void FastGrowingPatchDiesBeforeSlowGrowingPatchCreatedAtTheSameTime()
        {
            var resources = new ResourceStore(2);
            ResourceId fastSite = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 10f, 0f);
            ResourceId slowSite = resources.Add(ResourceKind.Food, new SimVector2(5f, 0f), 1f, 5f, 10f, 0f);
            var patches = new PlantPatchStore(2);
            int fast = patches.Add(fastSite, new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);
            patches.SetGenomeAndLineage(fast, new PlantGenome(1f, .5f, .5f, .5f, .5f, .5f, .5f, .5f), patches.GetAt(fast).Lineage);
            int slow = patches.Add(slowSite, new SimVector2(5f, 0f), 5f, 10f, .1f, 1f, 0f);
            patches.SetGenomeAndLineage(slow, new PlantGenome(0f, .5f, .5f, .5f, .5f, .5f, .5f, .5f), patches.GetAt(slow).Lineage);

            int stepsUntilFirstDeath = 0;
            while (patches.Count == 2 && stepsUntilFirstDeath < 10000)
            {
                PlantMortalitySystem.Step(patches, resources, 1f);
                stepsUntilFirstDeath++;
            }

            Assert.That(patches.Count, Is.EqualTo(1));
            Assert.That(patches.GetAt(0).Genome.Growth, Is.EqualTo(0f), "the surviving patch should be the slow grower");
        }

        [Test]
        public void BiomassRemovedByMortalityIsReportedSoTheResidualStaysBalanced()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(42, 4);
            var config = new SimulationConfig(
                42, 4, defaults.Schedule, defaults.MaximumPopulation, defaults.FounderProfile,
                cognitionEnabled: true, physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true, plantMortalityEnabled: true);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.PlantBackedBaseline.ApplyTo(world);

            for (int tick = 0; tick < 6000; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            SimulationStatistics stats = world.Statistics;
            Assert.That(stats.CumulativePlantBiomassLostToMortality, Is.GreaterThan(0f), "no patch died, so this test proves nothing");

            // Conservation is asserted relative to total biomass throughput rather than as a fixed
            // absolute epsilon: the residual is float32 accumulation drift that grows with run
            // length, and measured at ~3-6e-6 relative both with AND without mortality enabled
            // (it is in fact larger with mortality off), so a fixed .0001 would fail on
            // pre-existing behaviour at this tick count.
            float throughput = stats.CumulativePlantGrowth + stats.CumulativePlantBiomassConsumed + stats.CumulativePlantBiomassLostToMortality;
            Assert.That(throughput, Is.GreaterThan(0f));
            Assert.That(Math.Abs(stats.PlantBiomassResidual) / throughput, Is.LessThan(1e-4f));
        }

        [Test]
        public void PlantMortalityFlagOffLeavesTheStandardHashScenarioUnchanged()
        {
            var config = new SimulationConfig(
                99,
                2,
                new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);

            for (int i = 0; i < 50; i++)
            {
                world.Step(config.FixedDeltaTime);
            }

            // Rederived 2026-08-26 with the predation founder fix; see CoreSimulationTests.
            Assert.That(world.ComputeStateHash(), Is.EqualTo(14531405954382358740UL));
        }
    }
}
