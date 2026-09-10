using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The all-or-nothing plant seeding threshold, made a ramp.
    ///
    /// <para>A patch below <c>MaturityFraction</c> of capacity produces no seed at all, while every
    /// patch dies of age on a 34-135 second clock whether or not anything eats it. Measured
    /// 2026-09-06 across eleven cells, that age mortality is <b>78% of gross plant growth in the
    /// ungrazed phase</b>, so recruitment is the only inflow opposing a large constant outflow and
    /// this threshold is its only valve. See
    /// <c>docs/experiments/p6-what-limits-the-peak-2026-09-06.md</c> and the arm predeclared in
    /// <c>docs/experiments/p6-graded-seeding-arm-2026-09-06.md</c>.</para>
    /// </summary>
    public sealed class PlantGradedSeedingTests
    {
        private const int Seed = 42;

        /// <summary>
        /// Long enough for a patch to be born and then be grazed below the threshold. A run that ends
        /// while every patch is still above it leaves the flag inert by construction, which is a
        /// property of the run rather than of the flag.
        /// </summary>
        private const int Ticks = 4000;

        private const float Tolerance = 1e-4f;

        private static SimulationConfig Config(bool gradedSeeding)
        {
            return new SimulationConfig(
                worldSeed: Seed,
                initialPopulation: 8,
                schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                plantMortalityEnabled: true,
                plantSiteCompetitionEnabled: true,
                plantGradedSeedingEnabled: gradedSeeding);
        }

        private static ulong Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world.ComputeBehaviorHash();
        }

        [Test]
        public void AMaturePatchSeedsExactlyAsItAlwaysDid()
        {
            // The arm must change nothing above the threshold, or it is a seeding buff rather than
            // the removal of a step.
            Assert.That(PlantReproductionSystem.SeedMaturityScale(75f, 100f), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(PlantReproductionSystem.SeedMaturityScale(100f, 100f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void TheRampIsLinearFromEmptyToTheThreshold()
        {
            // Half of the way to the threshold is half the rate, applied to half the standing stock,
            // so a depleted patch is not merely slower to seed - it is quadratically slower.
            float half = PlantReproductionSystem.MaturityFraction * 0.5f * 100f;
            Assert.That(PlantReproductionSystem.SeedMaturityScale(half, 100f), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(PlantReproductionSystem.SeedMaturityScale(0f, 100f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void AnEmptyCapacityScalesToZeroRatherThanDividingByIt()
        {
            Assert.That(PlantReproductionSystem.SeedMaturityScale(10f, 0f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TheFlagIsLiveOnTheProductionPath()
        {
            Assert.That(Run(Config(gradedSeeding: true)), Is.Not.EqualTo(Run(Config(gradedSeeding: false))),
                "graded seeding produced a bit-identical run, so it is reaching no patch below the threshold");
        }

        [Test]
        public void TheFlagOffPathIsUnchanged()
        {
            // The multiply by the scale is unconditional in Step, so this pins that multiplying by
            // exactly 1f leaves the recorded path alone.
            Assert.That(Run(Config(gradedSeeding: false)), Is.EqualTo(Run(Config(gradedSeeding: false))));
            Assert.That(Config(gradedSeeding: true).ComputeConfigurationHash(),
                Is.Not.EqualTo(Config(gradedSeeding: false).ComputeConfigurationHash()));
        }
    }
}
