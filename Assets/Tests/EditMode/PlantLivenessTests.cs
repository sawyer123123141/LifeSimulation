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
    }
}
