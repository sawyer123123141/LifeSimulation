using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The one place a defender's decision reaches combat resolution.
    ///
    /// <para>Combat consulted the defender's stats and never its choice, so a creature grazing
    /// obliviously was hit exactly as often as one running for its life. Across twenty-two powered
    /// predation cells the passive <c>Defense</c> gene crossed |t| = 2 in <b>22 of 22</b>.</para>
    ///
    /// <para><b>The flag works and does not achieve its purpose</b>, which is why it is default
    /// false: the flee knob <c>RiskAversion</c> stays strongly negatively selected at every evasion
    /// strength tested, because the same gene also governs avoiding food near threats and starvation
    /// outweighs predation five to one. See
    /// <c>docs/emergent-behaviour-fleeing-is-selected-against-2026-08-29.md</c>. These tests pin the
    /// mechanism, not the outcome.</para>
    /// </summary>
    public sealed class EvasiveFleeingTests
    {
        private const int Seed = 42;

        /// <summary>
        /// Long enough for predation to actually occur. Combat needs founders that hunt, an
        /// engagement inside 1.1 units and an attack recovery of 0.75s between blows; a short run
        /// reaches the end before any of that and would report the flag inert by construction, which
        /// is the trap <c>GradedFertilityTests</c> records falling into.
        /// </summary>
        private const int Ticks = 4000;
        private const float Tolerance = 1e-4f;

        private static SimulationConfig Config(bool evasiveFleeing, float strength = SimulationConfig.DefaultEvasiveFleeingStrength)
        {
            return new SimulationConfig(
                worldSeed: Seed,
                initialPopulation: 8,
                schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PredationVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                evasiveFleeingEnabled: evasiveFleeing,
                evasiveFleeingStrength: strength);
        }

        private static ulong Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world.ComputeBehaviorHash();
        }

        private static Phenotype Defender(float maneuverability)
        {
            return Phenotype.FromGenome(new Genome(
                bodySize: 0.5f,
                movementSpeed: 0.5f,
                metabolicPace: 0.5f,
                visionRange: 0.5f,
                waterEfficiency: 0.5f,
                foodEfficiency: 0.5f,
                maneuverability: maneuverability));
        }

        [Test]
        public void StandingStillIsUnchangedByTheFlag()
        {
            // The multiplier is only ever applied to a defender whose action is Flee. A strength of
            // zero must also be exactly the old arithmetic, so the parameter can be swept down to
            // the original behaviour without a separate branch.
            Assert.That(PredationSystem.FleeEvasionMultiplier(Defender(0.5f), 0f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void FleeingAlwaysHelpsEvenWithNoAgility()
        {
            // Phenotype.Maneuverability is 1 + 2*gene, so its minimum is 1.0 and the least agile
            // creature alive still halves its attacker's chance at strength 1. Fleeing must never be
            // worthless to exactly the creatures most in need of it.
            //
            // This assertion is why the shipped formula has no floor term: the first draft added one
            // on the assumption of a 0-1 gene and over-rewarded evasion by about twenty points.
            float multiplier = PredationSystem.FleeEvasionMultiplier(Defender(0f), 1f);
            Assert.That(multiplier, Is.LessThan(1f));
            Assert.That(multiplier, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void TheDefaultStrengthRoughlyHalvesTheHitChance()
        {
            // Calibration, pinned. A founder-average defender sits near gene 0.44, phenotype 1.88.
            float multiplier = PredationSystem.FleeEvasionMultiplier(
                Defender(0.44f), SimulationConfig.DefaultEvasiveFleeingStrength);
            Assert.That(multiplier, Is.EqualTo(0.515f).Within(0.01f));
        }

        [Test]
        public void AgilityPaysOnlyWhenItIsUsed()
        {
            // The point of the whole flag: Maneuverability already sits passively in the resistance
            // denominator, and this term is the one that requires a decision behind it. More agility
            // must buy more evasion.
            float sluggish = PredationSystem.FleeEvasionMultiplier(Defender(0.1f), 1f);
            float agile = PredationSystem.FleeEvasionMultiplier(Defender(0.9f), 1f);
            Assert.That(agile, Is.LessThan(sluggish));
        }

        [Test]
        public void StrongerEvasionIsMonotone()
        {
            Phenotype defender = Defender(0.5f);
            float weak = PredationSystem.FleeEvasionMultiplier(defender, 0.5f);
            float strong = PredationSystem.FleeEvasionMultiplier(defender, 4f);
            Assert.That(strong, Is.LessThan(weak));
            Assert.That(strong, Is.GreaterThan(0f), "a multiplier must never drive the hit chance negative");
        }

        [Test]
        public void FlagOffIsByteIdenticalToTheRecordedBehaviour()
        {
            // The project's standing requirement for a new mechanism. The strength must not matter
            // while the flag is off, or the value would silently re-baseline every recorded run.
            ulong baseline = Run(Config(evasiveFleeing: false));
            ulong withStrengthMoved = Run(Config(evasiveFleeing: false, strength: 7f));
            Assert.That(withStrengthMoved, Is.EqualTo(baseline));
        }

        [Test]
        public void FlagOnChangesBehaviour()
        {
            // The liveness half. A flag that cannot be shown to move the world is indistinguishable
            // from an unwired one, which is the failure mode KnownInertFlags exists to catch.
            ulong off = Run(Config(evasiveFleeing: false));
            ulong on = Run(Config(evasiveFleeing: true));
            Assert.That(on, Is.Not.EqualTo(off));
        }

        [Test]
        public void ConfigurationHashCarriesBothTheFlagAndTheStrength()
        {
            // V2 carries configuration, so two worlds differing only in evasion must not collide.
            ulong off = Config(evasiveFleeing: false).ComputeConfigurationHash();
            ulong on = Config(evasiveFleeing: true).ComputeConfigurationHash();
            ulong stronger = Config(evasiveFleeing: true, strength: 4f).ComputeConfigurationHash();
            Assert.That(on, Is.Not.EqualTo(off));
            Assert.That(stronger, Is.Not.EqualTo(on));
        }
    }
}
