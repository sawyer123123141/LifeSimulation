using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The density-dependent brake the model did not have.
    ///
    /// <para>Births were gated by step functions, so the population bred at full rate until the
    /// forage was stripped and then starved together — which is why the same ecology survived 23 of
    /// 24 runs at a cap of 250 and 3 of 20 at a cap of 500. The cap was supplying the regulation.
    /// See <c>docs/experiments/p6-the-cap-is-the-stabiliser-2026-08-24.md</c>.</para>
    /// </summary>
    public sealed class GradedFertilityTests
    {
        private const int Seed = 42;
        /// <summary>
        /// Long enough to matter. <c>AdultAgeSeconds</c> is 20 - 1,200 ticks - and the cooldown this
        /// flag scales is around 12 seconds on top of that, so a 600-tick run reaches the end before
        /// anything has bred once and the flag is inert by construction. The first version of this
        /// test used 600 and the liveness check failed with identical hashes, correctly.
        /// </summary>
        private const int Ticks = 4000;
        private const float Tolerance = 1e-4f;
        private const float Gate = SimulationConfig.DefaultReproductionNeedFraction;

        private static SimulationConfig Config(bool gradedFertility)
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
                foragingEconomicsEnabled: true,
                gradedFertilityEnabled: gradedFertility);
        }

        private static ulong Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world.ComputeBehaviorHash();
        }

        [Test]
        public void AFullyProvisionedCreatureIsNotSlowedAtAll()
        {
            // The brake must cost nothing at the top, or it is a fertility nerf rather than a
            // density-dependent response.
            Assert.That(ReproductionSystem.CooldownMultiplier(1f, Gate), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void ACreatureExactlyAtTheGateWaitsTheFullMultiple()
        {
            Assert.That(
                ReproductionSystem.CooldownMultiplier(Gate, Gate),
                Is.EqualTo(1f + SimulationConfig.GradedFertilityStrength).Within(Tolerance));
        }

        [Test]
        public void BelowTheGateItClampsRatherThanRunningAway()
        {
            // Below the gate the creature cannot breed at all, so the multiplier has nothing to do.
            // Letting it grow without bound would put an arbitrary number into the cooldown of any
            // creature that recovers.
            Assert.That(
                ReproductionSystem.CooldownMultiplier(0f, Gate),
                Is.EqualTo(1f + SimulationConfig.GradedFertilityStrength).Within(Tolerance));
        }

        [Test]
        public void TheBrakeTightensMonotonicallyAsConditionFalls()
        {
            float previous = 0f;
            for (int step = 20; step >= 0; step--)
            {
                float multiplier = ReproductionSystem.CooldownMultiplier(step / 20f, Gate);
                Assert.That(multiplier, Is.GreaterThanOrEqualTo(previous - Tolerance));
                previous = multiplier;
            }
        }

        [Test]
        public void TheCurveIsMeasuredAgainstTheGateNotAgainstZero()
        {
            // A creature is not "half fed" - it is some fraction of the way from the threshold that
            // lets it breed at all up to full. Measuring from zero would make the brake almost
            // fully applied everywhere, since nothing breeds below the gate anyway.
            float halfway = Gate + ((1f - Gate) / 2f);

            Assert.That(
                ReproductionSystem.CooldownMultiplier(halfway, Gate),
                Is.EqualTo(1f + (SimulationConfig.GradedFertilityStrength / 2f)).Within(Tolerance));
        }

        [Test]
        public void ASlackerGateStretchesTheCurveRatherThanMovingIt()
        {
            // The brake is defined relative to whatever the gate is, so the two knobs stay separable.
            Assert.That(ReproductionSystem.CooldownMultiplier(0.45f, 0.45f), Is.EqualTo(ReproductionSystem.CooldownMultiplier(0.7f, 0.7f)).Within(Tolerance));
            Assert.That(ReproductionSystem.CooldownMultiplier(1f, 0.45f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void TheFlagOffChangesNothingAtAll()
        {
            Assert.That(Run(Config(gradedFertility: false)), Is.EqualTo(Run(Config(gradedFertility: false))));
        }

        [Test]
        public void TheFlagOnChangesHowTheWorldEvolves()
        {
            Assert.That(
                Run(Config(gradedFertility: true)),
                Is.Not.EqualTo(Run(Config(gradedFertility: false))));
        }

        [Test]
        public void TheConfigurationHashSeesTheFlag()
        {
            Assert.That(
                Config(gradedFertility: true).ComputeConfigurationHash(),
                Is.Not.EqualTo(Config(gradedFertility: false).ComputeConfigurationHash()));
        }
    }
}
