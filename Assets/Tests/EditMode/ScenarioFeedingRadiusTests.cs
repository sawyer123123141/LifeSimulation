using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Widening the disc a creature can feed from.
    ///
    /// <para>Measured cause of the herd reading as a pile: `Y` has six food sites of
    /// <c>InteractionRadius</c> 1.5 for 96 creatures, so sixteen animals share a disc three units
    /// across while a creature model is about one unit wide. They overlap by construction, and both a
    /// movement change and a four-times-larger world were measured and left the number close to where
    /// it started. The radius is the geometry that actually sets it.</para>
    /// </summary>
    [TestFixture]
    public sealed class ScenarioFeedingRadiusTests
    {
        [Test]
        public void AFactorOfOneChangesNothing()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            SimulationScenario same = source.WithFeedingRadius("same", 1f);

            Assert.That(
                same.ComputeLayoutFingerprint(),
                Is.EqualTo(source.ComputeLayoutFingerprint()),
                "a factor of one must be the original layout, or a control arm is not a control");
        }

        [Test]
        public void TheRadiusScalesAndNothingElseDoes()
        {
            var genome = new PlantGenome(.55f, .5f, .5f, .65f, .3f, .5f, .5f, .5f);
            var scenario = new SimulationScenario("probe", new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(1f, 2f), 1.5f, 24f, 20f, 12f, nutritionMultiplier: 1.25f, plantGenome: genome),
            });

            var expected = new SimulationScenario("expected", new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(1f, 2f), 3f, 24f, 20f, 12f, nutritionMultiplier: 1.25f, plantGenome: genome),
            });

            Assert.That(
                scenario.WithFeedingRadius("wider", 2f).ComputeLayoutFingerprint(),
                Is.EqualTo(expected.ComputeLayoutFingerprint()),
                "position, amount, capacity, regeneration, nutrition and genome must all be untouched");
        }

        /// <summary>
        /// Dormant sites are what plant dispersal re-establishes into. Missing them once already
        /// killed every world in a probe, so a transform that skipped them would be the same bug.
        /// </summary>
        [Test]
        public void DormantSitesAreWidenedToo()
        {
            var scenario = new SimulationScenario("probe", new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(3f, 4f), 1.5f, 0f, 24f, 0f, isActive: false),
            });

            Assert.That(scenario.WithFeedingRadius("wider", 2f).ResourceCount, Is.EqualTo(1));
        }

        [Test]
        public void FounderPlacementIsCarriedThrough()
        {
            var scenario = new SimulationScenario(
                "probe",
                new[] { new ResourceDefinition(ResourceKind.Food, new SimVector2(1f, 1f), 1.5f, 24f, 24f, 12f) },
                founderPlacement: new SimVector2(1f, 1f));

            SimulationScenario wider = scenario.WithFeedingRadius("wider", 2f);
            Assert.That(wider.FounderPlacement.HasValue, Is.True);
            Assert.That(wider.FounderPlacement.Value.X, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void RejectsNonsenseFactors()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            Assert.Throws<ArgumentOutOfRangeException>(() => source.WithFeedingRadius("t", 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.WithFeedingRadius("t", -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.WithFeedingRadius("t", float.NaN));
        }
    }
}
