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
        public void TemperatureToleranceCannotHelpThePlantEvenWhenTemperatureIsLimiting()
        {
            // Characterization of a known gap, not an endorsement of it.
            //
            // PlantGrowthSystem computes:
            //   limit = min(moistureAdaptation, min(sample.Fertility, sample.Temperature))
            // Temperature enters as a RAW limit with no genome modulation, unlike moisture which has
            // an adaptation term. So plant TemperatureTolerance pays -.10f growth in PlantPhenotype
            // and can never earn it back, under any environment.
            //
            // This is why richer terrain fields alone will NOT make the gene meaningful: a
            // temperatureAdaptation term mirroring moistureAdaptation is also required. Both halves
            // or neither.
            //
            // When that lands, this test should FAIL and be replaced by the moisture-shaped
            // assertion above.
            var field = new EnvironmentField(moisture: 1f, fertility: 1f, temperature: 0.2f);

            float intolerant = PlantGrowthSystem.Step(StoreWithTolerance(0f, 0f), field, 1f);
            float tolerant = PlantGrowthSystem.Step(StoreWithTolerance(0f, 1f), field, 1f);

            Assert.That(tolerant, Is.LessThan(intolerant),
                "expected TemperatureTolerance to be a pure cost: it charges growth and has no adaptation term to earn it back");
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
