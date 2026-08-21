using System.Linq;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Plant-side counterpart of <see cref="LivenessTests"/>, plus characterization of which plant
    /// traits actually have a fitness TRADE-OFF rather than merely reaching behavior.
    ///
    /// The distinction matters and cost a wrong prediction to learn: a pure-cost gene passes a
    /// liveness test. Perturbing it changes growth, so the behavior hash moves, so it reads "live" —
    /// while being unable to benefit its carrier under any environment.
    /// </summary>
    public sealed class PlantLivenessTests
    {
        // ---- Genome integrity -------------------------------------------------------------

        [Test]
        public void EveryPlantTraitSurvivesTheTraitArrayRoundTrip()
        {
            float[] traits = Enumerable.Range(0, PlantGenome.TraitCount)
                .Select(index => (index + 1) / (float)(PlantGenome.TraitCount + 1))
                .ToArray();

            Assert.That(PlantGenome.FromTraits(traits).ToTraits(), Is.EqualTo(traits));
        }

        [Test]
        public void PlantTraitNamesAreDistinctAndCoverEveryTrait()
        {
            var names = Enumerable.Range(0, PlantGenome.TraitCount).Select(PlantGenome.TraitName).ToArray();

            Assert.That(names.Distinct().Count(), Is.EqualTo(PlantGenome.TraitCount));
            Assert.That(names, Has.None.Null.Or.Empty);
        }

        [Test]
        public void WithTraitChangesOnlyTheNamedPlantTrait()
        {
            PlantGenome original = PlantGenome.FromTraits(
                Enumerable.Repeat(0.5f, PlantGenome.TraitCount).ToArray());

            for (int traitIndex = 0; traitIndex < PlantGenome.TraitCount; traitIndex++)
            {
                float[] changed = original.WithTrait(traitIndex, 0.25f).ToTraits();
                for (int other = 0; other < PlantGenome.TraitCount; other++)
                {
                    Assert.That(changed[other], Is.EqualTo(other == traitIndex ? 0.25f : 0.5f),
                        $"WithTrait({traitIndex}) altered {PlantGenome.TraitName(other)}");
                }
            }
        }

        // ---- Liveness ---------------------------------------------------------------------

        [Test]
        public void EveryPlantGeneReachesBehaviour()
        {
            PlantGeneLivenessResult[] results = PlantGeneLivenessAnalysis.Analyze(
                () => SimulationConfig.CreateFullEcosystemDefaults(42, 12),
                Prototype4Scenarios.ConsumerDefenseCalibrationModerate,
                ticks: 2000);

            string[] dead = results.Where(r => !r.ReachesBehavior).Select(r => r.TraitName).ToArray();

            Assert.That(dead, Is.Empty, $"plant gene liveness changed.\n{PlantGeneLivenessAnalysis.Report(results)}");
        }

        [Test]
        public void EveryPlantTraitTransmitsThroughCloneMutated()
        {
            // The animal genome once passed 23 of 24 positional arguments and nothing failed, so
            // persistence took its default for every creature ever born. PlantGenome's ninth
            // parameter HAS a default, which is exactly that trap, so pin it: at a zero mutation
            // standard deviation a clone must reproduce every trait exactly, and the values are
            // pairwise distinct so a dropped or swapped argument cannot pass.
            var traits = new float[PlantGenome.TraitCount];
            for (int index = 0; index < traits.Length; index++)
            {
                traits[index] = (index + 1) / (float)(PlantGenome.TraitCount + 1);
            }

            PlantGenome parent = PlantGenome.FromTraits(traits);
            PlantGenome child = PlantGenome.CloneMutated(parent, worldSeed: 7, ordinal: 3, mutationStandardDeviation: 0f);

            Assert.That(child.ToTraits(), Is.EqualTo(traits),
                "a plant trait stopped transmitting through CloneMutated");
        }

        // ---- Fertility adaptation ----------------------------------------------------------

        private static PlantPatchStore StoreWithUptake(float nutrientUptake)
        {
            var store = new PlantPatchStore(4);
            int index = store.Add(new ResourceId(1), new SimVector2(0f, 0f), biomass: 5f, capacity: 100f,
                growthRate: 1f, nutrition: 1f, defense: 0f);
            PlantGenome genome = new PlantGenome(.5f, .5f, .5f, .5f, 0f, .5f, 0f, 0f, nutrientUptake);
            store.SetGenomeAndLineage(index, genome, default);
            return store;
        }

        [Test]
        public void NutrientUptakeIsInertWhenFertilityAdaptationIsDisabled()
        {
            // Flag-off must be byte-identical to the world before the gene existed, which means the
            // -.10f charge has to be gated alongside the benefit. An unconditional cost would make
            // merely ADDING the gene change every plant run.
            var field = new EnvironmentField(moisture: 1f, fertility: 0.2f, temperature: 1f);

            float without = PlantGrowthSystem.Step(StoreWithUptake(0f), field, 1f);
            float with = PlantGrowthSystem.Step(StoreWithUptake(1f), field, 1f);

            Assert.That(with, Is.EqualTo(without),
                "with fertility adaptation disabled, NutrientUptake must not affect growth at all");
        }

        [Test]
        public void NutrientUptakeHelpsThePlantWhenFertilityIsPoor()
        {
            var field = new EnvironmentField(moisture: 1f, fertility: 0.2f, temperature: 1f);

            float without = PlantGrowthSystem.Step(StoreWithUptake(0f), field, 1f, fertilityAdaptationEnabled: true);
            float with = PlantGrowthSystem.Step(StoreWithUptake(1f), field, 1f, fertilityAdaptationEnabled: true);

            Assert.That(with, Is.GreaterThan(without),
                "NutrientUptake should buy real growth on poor soil");
        }

        [Test]
        public void NutrientUptakeIsAPureCostWhereFertilityDoesNotBind()
        {
            // The self-defeating half of the Min structure, pinned deliberately: an adaptation term
            // lifts its own channel out of contention, so once fertility is no longer the smallest
            // channel the gene buys nothing and still pays. That is what should make the trait settle
            // at an interior equilibrium instead of ramping to 1.
            var field = new EnvironmentField(moisture: 1f, fertility: 1f, temperature: 1f);

            float without = PlantGrowthSystem.Step(StoreWithUptake(0f), field, 1f, fertilityAdaptationEnabled: true);
            float with = PlantGrowthSystem.Step(StoreWithUptake(1f), field, 1f, fertilityAdaptationEnabled: true);

            Assert.That(with, Is.LessThan(without),
                "where fertility does not bind, NutrientUptake should be a pure cost");
        }

        // ---- Trade-off characterization ---------------------------------------------------

        private static PlantPatchStore StoreWithTolerance(float moistureTolerance, float temperatureTolerance)
        {
            var store = new PlantPatchStore(4);
            int index = store.Add(new ResourceId(1), new SimVector2(0f, 0f), biomass: 5f, capacity: 100f,
                growthRate: 1f, nutrition: 1f, defense: 0f);
            PlantGenome genome = new PlantGenome(.5f, .5f, .5f, .5f, 0f, .5f, moistureTolerance, temperatureTolerance);
            store.SetGenomeAndLineage(index, genome, default);
            return store;
        }

        [Test]
        public void MoistureToleranceHelpsThePlantWhenMoistureIsScarce()
        {
            // The working pattern: PlantGrowthSystem folds MoistureTolerance into moistureAdaptation,
            // so carrying it buys real growth in a dry place. This is what a trait with a genuine
            // trade-off looks like, and it is the reference the temperature test below fails against.
            var field = new EnvironmentField(moisture: 0.2f, fertility: 1f, temperature: 1f);

            float intolerant = PlantGrowthSystem.Step(StoreWithTolerance(0f, 0f), field, 1f);
            float tolerant = PlantGrowthSystem.Step(StoreWithTolerance(1f, 0f), field, 1f);

            Assert.That(tolerant, Is.GreaterThan(intolerant),
                "MoistureTolerance should improve growth under scarce moisture");
        }

        [Test]
        public void TemperatureToleranceIsAPureCostWhenAdaptationIsDisabled()
        {
            // Flag-off behavior, kept as the regression guard for the old path.
            //
            // limit = min(moistureAdaptation, min(sample.Fertility, sample.Temperature)) - temperature
            // enters as a RAW limit with no genome modulation, so TemperatureTolerance pays -.10f
            // growth in PlantPhenotype and can never earn it back.
            var field = new EnvironmentField(moisture: 1f, fertility: 1f, temperature: 0.2f);

            float intolerant = PlantGrowthSystem.Step(StoreWithTolerance(0f, 0f), field, 1f);
            float tolerant = PlantGrowthSystem.Step(StoreWithTolerance(0f, 1f), field, 1f);

            Assert.That(tolerant, Is.LessThan(intolerant),
                "with adaptation disabled, TemperatureTolerance should remain a pure cost");
        }

        [Test]
        public void TemperatureToleranceHelpsThePlantWhenAdaptationIsEnabled()
        {
            // The fix: temperature now mirrors the moisture pattern, so tolerance buys real growth
            // at a limiting site instead of only charging for itself.
            var field = new EnvironmentField(moisture: 1f, fertility: 1f, temperature: 0.2f);

            float intolerant = PlantGrowthSystem.Step(StoreWithTolerance(0f, 0f), field, 1f, temperatureAdaptationEnabled: true);
            float tolerant = PlantGrowthSystem.Step(StoreWithTolerance(0f, 1f), field, 1f, temperatureAdaptationEnabled: true);

            Assert.That(tolerant, Is.GreaterThan(intolerant),
                "TemperatureTolerance should improve growth under a limiting temperature once adaptation is enabled");
        }

        [Test]
        public void TemperatureAdaptationIsByteIdenticalWhereTemperatureIsUnlimiting()
        {
            // Why the flag reads inert in production: at temperature 1 the adaptation expression
            // collapses to the raw value, so enabling it changes nothing until the environment
            // actually varies. This is the measured basis for its entry in KnownInertFlags.
            var field = new EnvironmentField(moisture: 1f, fertility: 1f, temperature: 1f);

            float off = PlantGrowthSystem.Step(StoreWithTolerance(0f, 1f), field, 1f);
            float on = PlantGrowthSystem.Step(StoreWithTolerance(0f, 1f), field, 1f, temperatureAdaptationEnabled: true);

            Assert.That(on, Is.EqualTo(off));
        }

        [Test]
        public void FertilityIsPinnedAtOneOnEveryProductionPath()
        {
            // Fertility is a real term in the growth limit, and it is constant in production, so it
            // never constrains anything. Recorded so terrain work knows it is an unused channel
            // rather than assuming it already varies.
            SimulationConfig cohorts = SimulationConfig.CreatePrototype4Defaults(42, 12);
            var world = new SimulationWorld(cohorts);

            foreach (var position in new[] { new SimVector2(-24f, -24f), new SimVector2(0f, 0f), new SimVector2(24f, 24f) })
            {
                EnvironmentSample sample = world.Environment.Sample(position);
                Assert.That(sample.Fertility, Is.EqualTo(1f), $"fertility varied at {position.X},{position.Y}");
                Assert.That(sample.Temperature, Is.EqualTo(1f), $"plant-facing temperature varied at {position.X},{position.Y}");
            }
        }

        // ---- The seed-production route (live, and a MEASURED NULL) ---------------------------

        [Test]
        public void SeedProductionRateChangesNothingUntilItsRouteIsEnabled()
        {
            PlantGenome fast = PlantGenome.Neutral.WithTrait(10, 1f);
            PlantGenome slow = PlantGenome.Neutral.WithTrait(10, 0f);

            PlantPhenotype fastOff = PlantPhenotype.FromGenome(fast);
            PlantPhenotype slowOff = PlantPhenotype.FromGenome(slow);

            Assert.That(fastOff.ReproductionCooldownSeconds, Is.EqualTo(slowOff.ReproductionCooldownSeconds));
            Assert.That(fastOff.DispersalRange, Is.EqualTo(slowOff.DispersalRange));
            Assert.That(fastOff.ReproductionCooldownSeconds, Is.EqualTo(PlantReproductionSystem.ReproductionCooldownSeconds));
        }

        [Test]
        public void SeedProductionRateShortensTheCooldownAndChargesDispersalWhenEnabled()
        {
            // Both halves move together so flag-off stays byte-identical, the same shape as
            // NutrientUptake and SeedlingResilience.
            PlantPhenotype fast = PlantPhenotype.FromGenome(
                PlantGenome.Neutral.WithTrait(10, 1f), false, false, seedProductionRateDispersalCharge: 2f, seedProductionRateEnabled: true);
            PlantPhenotype slow = PlantPhenotype.FromGenome(
                PlantGenome.Neutral.WithTrait(10, 0f), false, false, seedProductionRateDispersalCharge: 2f, seedProductionRateEnabled: true);

            Assert.That(fast.ReproductionCooldownSeconds, Is.LessThan(slow.ReproductionCooldownSeconds));
            Assert.That(fast.DispersalRange, Is.EqualTo(slow.DispersalRange - 2f));
        }

        [Test]
        public void TheSeedProductionCooldownStillSpansTwoFoldSoTheMeasuredNullStillApplies()
        {
            // Pins WHY this route is a null, so nobody spends another session re-testing it. A
            // patch lives 95.8s: 6.7s growing to maturity, 30.4s on cooldown, and 58.7s ALREADY
            // mature, off cooldown, and failing to find a free site at 91% occupancy. Freeing
            // cooldown time only adds to a pool of time that is already being wasted. Measured
            // over seeds 42-71: births move 203.7 -> 221.8 across the gene's full span, under 10%,
            // and plant generations do not rise at all.
            // docs/experiments/p4-seed-production-rate-is-not-the-constraint-2026-08-20.md
            float slow = PlantPhenotype.FromGenome(
                PlantGenome.Neutral.WithTrait(10, 0f), false, false, 0f, true).ReproductionCooldownSeconds;
            float fast = PlantPhenotype.FromGenome(
                PlantGenome.Neutral.WithTrait(10, 1f), false, false, 0f, true).ReproductionCooldownSeconds;

            Assert.That(slow / fast, Is.EqualTo(2f).Within(.001f),
                "if the span changes, the measured sub-10% birth swing no longer applies");
        }

        // ---- The establishment contest ------------------------------------------------------

        [Test]
        public void SeedlingResilienceCostsDispersalRangeOnlyWhenTheContestIsEnabled()
        {
            // Charged against dispersal rather than growth rate on purpose: a growth-rate charge is
            // multiplied by (1 - Biomass/Capacity), measured mean 0.1711, so it is almost free.
            // Dispersal is the strongest measured fitness channel, so this trade-off actually bites.
            PlantGenome tough = PlantGenome.Neutral.WithTrait(9, 1f);

            float off = PlantPhenotype.FromGenome(tough).DispersalRange;
            float on = PlantPhenotype.FromGenome(tough, fertilityAdaptationEnabled: false, establishmentContestEnabled: true).DispersalRange;

            Assert.That(off, Is.EqualTo(4f + (20f * .5f)));
            Assert.That(on, Is.EqualTo(off - 2f));
        }

        [Test]
        public void SeedlingResilienceDecidesTheTakeoverOnlyWhenTheContestIsEnabled()
        {
            // With the contest off, a maximally resilient seedling is overwritten exactly like a
            // defenceless one - which is the world as measured on 2026-08-20, where 34% of every
            // patch ever born dies this way inside a median two seconds and no gene correlates
            // with the outcome above |r| = 0.10.
            Assert.That(TakeoversOf(resilience: 0f, contestEnabled: false), Is.EqualTo(1));
            Assert.That(TakeoversOf(resilience: 1f, contestEnabled: false), Is.EqualTo(1));

            Assert.That(TakeoversOf(resilience: 0f, contestEnabled: true), Is.EqualTo(1),
                "a seedling with no resilience must still lose its site, or the contest is just a block");
            Assert.That(TakeoversOf(resilience: 1f, contestEnabled: true), Is.EqualTo(0),
                "a maximally resilient seedling must hold its site");
        }

        [Test]
        public void InvaderResilienceCanOvercomeAnEqualIncumbentOnlyWhenItsContestFlagIsEnabled()
        {
            Assert.That(TakeoversOf(resilience: 1f, invaderResilience: 1f, contestEnabled: true, invaderContestEnabled: false), Is.EqualTo(0));
            Assert.That(TakeoversOf(resilience: 1f, invaderResilience: 1f, contestEnabled: true, invaderContestEnabled: true), Is.EqualTo(1));
        }

        /// <summary>
        /// One takeover attempt against a seedling sitting below VulnerabilityFraction. Seed 4 and
        /// tick 1 are the pair whose second establishment attempt succeeds (see
        /// PlantGrowthTests.SiteWithinRangeThatFailsItsEstablishmentRollLetsStepRetryTheNextAttempt),
        /// so the resilience-0 arm asserting one birth is what pins the roll as reachable at all.
        /// </summary>
        private static int TakeoversOf(float resilience, bool contestEnabled, float invaderResilience = 0f, bool invaderContestEnabled = false)
        {
            var resources = new ResourceStore(2);
            ResourceId parentSite = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 10f, 12f, 0f);
            ResourceId contested = resources.Add(ResourceKind.Food, new SimVector2(2f, 0f), 1f, 1f, 12f, 0f);
            resources.SetActive(contested, true);
            var sites = new PlantSiteRegistry(1);
            sites.Register(1);

            var patches = new PlantPatchStore(4);
            int parentIndex = patches.Add(parentSite, new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            patches.SetGenomeAndLineage(parentIndex, PlantGenome.Neutral.WithTrait(9, invaderResilience), patches.GetAt(parentIndex).Lineage);
            int seedlingIndex = patches.Add(contested, new SimVector2(2f, 0f), 1f, 10f, .1f, 1f, 0f);
            patches.SetGenomeAndLineage(seedlingIndex, PlantGenome.Neutral.WithTrait(9, resilience), patches.GetAt(seedlingIndex).Lineage);

            long ordinal = 0;
            return PlantReproductionSystem.Step(patches, resources, sites, 4, 1, 1f, ref ordinal, competitionEnabled: true, establishmentContestEnabled: contestEnabled, invaderEstablishmentContestEnabled: invaderContestEnabled);
        }
    }
}
