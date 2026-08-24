using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.World;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Climbing costs a creature energy - the half of the terrain join that makes relief matter to
    /// something other than the renderer.
    ///
    /// <para>Three claims, and the third is the one that is easy to get wrong: the flag must do
    /// nothing at all when there is no elevation to climb, rather than doing something small and
    /// unexplained.</para>
    /// </summary>
    public sealed class SlopeMovementCostTests
    {
        private const int Seed = 42;
        private const int Ticks = 600;

        private static SimulationConfig Config(bool terrain, bool slopeCost)
        {
            return new SimulationConfig(
                worldSeed: Seed,
                initialPopulation: 8,
                schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PredationVariation,
                proceduralEnvironmentFieldsEnabled: terrain,
                elevationFieldEnabled: terrain,
                terrainDrivenEnvironmentEnabled: terrain,
                slopeMovementCostEnabled: slopeCost);
        }

        /// <summary>
        /// <b>Behaviour hash, not the fingerprint.</b> The fingerprint folds in the configuration
        /// hash, so it differs the moment a flag differs - which would make the liveness test below
        /// pass without the flag doing anything, and the inertness test impossible to satisfy. The
        /// behaviour hash is the config-free one, and it is the only one that answers "did these two
        /// worlds evolve the same way".
        /// </summary>
        private static ulong Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world.ComputeBehaviorHash();
        }

        /// <summary>
        /// The flag is live: with relief under them, creatures that pay for climbing do not end up
        /// where creatures that climb for free end up.
        /// </summary>
        [Test]
        public void ClimbingChangesTheWorldWhenThereIsTerrainToClimb()
        {
            Assert.That(
                Run(Config(terrain: true, slopeCost: true)),
                Is.Not.EqualTo(Run(Config(terrain: true, slopeCost: false))));
        }

        /// <summary>
        /// <b>Inert without elevation.</b> The field reports no elevation unless terrain drives it,
        /// so every climb is zero and the flag must be exactly as if it were off - not nearly.
        /// A flag that quietly perturbs a world it has no information about is worse than one that
        /// does nothing, because the perturbation looks like a result.
        /// </summary>
        [Test]
        public void WithoutElevationTheFlagDoesNothingAtAll()
        {
            Assert.That(
                Run(Config(terrain: false, slopeCost: true)),
                Is.EqualTo(Run(Config(terrain: false, slopeCost: false))));
        }

        /// <summary>
        /// Off is off. The default is false and every result on record was measured that way, so
        /// this is the guard that keeps them comparable.
        /// </summary>
        [Test]
        public void TheFlagIsOffByDefault()
        {
            Assert.That(
                new SimulationConfig(
                    worldSeed: Seed, initialPopulation: 8,
                    schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                    founderProfile: FounderProfile.PredationVariation).SlopeMovementCostEnabled,
                Is.False);
        }

        /// <summary>
        /// The exchange rate is stated in metres, and the two layers that hold it must agree - the
        /// simulation charges in metres of climb, the renderer draws in metres of height.
        /// </summary>
        [Test]
        public void TheSimulationAndTheRendererUseTheSameHeightScale()
        {
            Assert.That(PlanetTerrain.MetresPerElevationUnit, Is.EqualTo(30f));
            Assert.That(SimulationConfig.SlopeClimbCost, Is.GreaterThan(0f));
        }
    }
}
