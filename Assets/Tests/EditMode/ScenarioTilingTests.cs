using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Tiling a scenario to fill a larger arena.
    ///
    /// <para>Written after a hand-built copy of the `Y` habitat killed every world it was run in,
    /// including the control arm that should have reproduced a population of 96. The copy had the six
    /// active food and water sites and was missing the twenty dormant sites plant dispersal
    /// re-establishes into, and the founder placement. <b>A scenario is not its visible resources</b>,
    /// so deriving one belongs here next to <c>Scaled</c> and <c>WithRegeneration</c> rather than in
    /// whatever is calling it.</para>
    /// </summary>
    [TestFixture]
    public sealed class ScenarioTilingTests
    {
        [Test]
        public void OneTileIsTheSameScenario()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            SimulationScenario tiled = source.Tiled("one", tiles: 1, spacing: 50f);

            Assert.That(tiled.ResourceCount, Is.EqualTo(source.ResourceCount));
            Assert.That(
                tiled.ComputeLayoutFingerprint(),
                Is.EqualTo(source.ComputeLayoutFingerprint()),
                "a single tile must be the original layout, or the control arm is not a control");
        }

        [Test]
        public void EveryResourceIsCopiedIntoEveryTile()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            SimulationScenario tiled = source.Tiled("four", tiles: 2, spacing: 50f);

            Assert.That(tiled.ResourceCount, Is.EqualTo(source.ResourceCount * 4));
        }

        /// <summary>
        /// The dormant sites are the ones a hand-written copy forgets, and without them plant
        /// mortality has nowhere to re-establish into and the food never comes back.
        /// </summary>
        [Test]
        public void DormantSitesAreTiledToo()
        {
            var genome = new PlantGenome(.55f, .5f, .5f, .65f, .3f, .5f, .5f, .5f);
            var scenario = new SimulationScenario("probe", new[]
            {
                new ResourceDefinition(ResourceKind.Food, new SimVector2(1f, 2f), 1.5f, 24f, 24f, 12f, nutritionMultiplier: 1f, plantGenome: genome),
                new ResourceDefinition(ResourceKind.Food, new SimVector2(3f, 4f), 1.5f, 0f, 24f, 0f, isActive: false),
            });

            Assert.That(scenario.Tiled("t", tiles: 3, spacing: 50f).ResourceCount, Is.EqualTo(2 * 9));
        }

        [Test]
        public void TilesAreCentredOnTheOrigin()
        {
            var scenario = new SimulationScenario("probe", new[]
            {
                new ResourceDefinition(ResourceKind.Water, new SimVector2(0f, 0f), 1.5f, 12f, 12f, 1f),
            });

            // Two tiles at spacing 50 put their copies at -25 and +25 on each axis, so the block is
            // centred rather than growing off one corner - which would move the habitat away from
            // where the founders are placed.
            var expected = new SimulationScenario("expected", new[]
            {
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-25f, -25f), 1.5f, 12f, 12f, 1f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(-25f, 25f), 1.5f, 12f, 12f, 1f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(25f, -25f), 1.5f, 12f, 12f, 1f),
                new ResourceDefinition(ResourceKind.Water, new SimVector2(25f, 25f), 1.5f, 12f, 12f, 1f),
            });

            SimulationScenario tiled = scenario.Tiled("t", tiles: 2, spacing: 50f);
            Assert.That(tiled.ResourceCount, Is.EqualTo(4));
            Assert.That(tiled.ComputeLayoutFingerprint(), Is.EqualTo(expected.ComputeLayoutFingerprint()));
        }

        /// <summary>
        /// The founder placement is a point on a resource site, so it has to move with the sites.
        /// Leaving it where it was put four founders in empty ground between habitats and killed
        /// half the worlds.
        /// </summary>
        [Test]
        public void FoundersAreMovedIntoATileRatherThanLeftInTheGap()
        {
            var scenario = new SimulationScenario(
                "probe",
                new[] { new ResourceDefinition(ResourceKind.Food, new SimVector2(-12f, -8f), 1.5f, 24f, 24f, 12f) },
                founderPlacement: new SimVector2(-12f, -8f));

            SimulationScenario tiled = scenario.Tiled("t", tiles: 2, spacing: 50f);
            Assert.That(tiled.FounderPlacement.HasValue, Is.True, "a tiled scenario must still place its founders");

            SimVector2 placement = tiled.FounderPlacement.Value;
            bool onASite = false;
            foreach ((float x, float y) in new[] { (-37f, -33f), (-37f, 17f), (13f, -33f), (13f, 17f) })
            {
                if (Math.Abs(placement.X - x) < 0.001f && Math.Abs(placement.Y - y) < 0.001f) onASite = true;
            }

            Assert.That(onASite, Is.True, $"founders landed at ({placement.X}, {placement.Y}), which is not a site");
        }

        /// <summary>An odd tile count has a true middle tile, so the placement should not move.</summary>
        [Test]
        public void AnOddTileCountLeavesTheFoundersWhereTheyWere()
        {
            var scenario = new SimulationScenario(
                "probe",
                new[] { new ResourceDefinition(ResourceKind.Food, new SimVector2(4f, 6f), 1.5f, 24f, 24f, 12f) },
                founderPlacement: new SimVector2(4f, 6f));

            SimVector2 placement = scenario.Tiled("t", tiles: 3, spacing: 50f).FounderPlacement.Value;
            Assert.That(placement.X, Is.EqualTo(4f).Within(0.001f));
            Assert.That(placement.Y, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void RejectsNonsenseArguments()
        {
            SimulationScenario source = Prototype4Scenarios.ConsumerDefenseCalibrationModerate;
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Tiled("t", tiles: 0, spacing: 50f));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Tiled("t", tiles: 2, spacing: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Tiled("t", tiles: 2, spacing: float.NaN));
        }
    }
}
