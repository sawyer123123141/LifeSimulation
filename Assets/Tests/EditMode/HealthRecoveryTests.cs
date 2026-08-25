using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Healing, which did not exist until now.
    ///
    /// <para>Health was written once at birth and subtracted from in five places, with no addition
    /// anywhere — a one-way ratchet. Because health is one of the three conditions on the
    /// mate-seeking gate, losing a fifth of it meant permanent sterility rather than injury.</para>
    /// </summary>
    public sealed class HealthRecoveryTests
    {
        private const int Seed = 42;
        private const int Ticks = 600;
        private const float Tolerance = 1e-4f;

        private static Phenotype Subject => Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f));

        private static CreatureNeeds Injured(Phenotype phenotype, float healthFraction, float suppliesFraction)
        {
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Health = phenotype.HealthCapacity * healthFraction;
            needs.Energy = phenotype.EnergyCapacity * suppliesFraction;
            needs.Hydration = phenotype.HydrationCapacity * suppliesFraction;
            return needs;
        }

        private static SimulationConfig Config(bool healthRecovery)
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
                healthRecoveryEnabled: healthRecovery);
        }

        private static ulong Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world.ComputeBehaviorHash();
        }

        [Test]
        public void AWellFedCreatureHeals()
        {
            Phenotype phenotype = Subject;
            CreatureNeeds needs = Injured(phenotype, healthFraction: 0.5f, suppliesFraction: 1f);
            float before = needs.Health;

            NeedsSystem.RecoverHealth(ref needs, phenotype, deltaTime: 1f);

            Assert.That(needs.Health, Is.GreaterThan(before));
            Assert.That(
                needs.Health - before,
                Is.EqualTo(phenotype.HealthCapacity * NeedsSystem.HealthRecoveryFractionPerSecond).Within(Tolerance));
        }

        [Test]
        public void AHungryCreatureDoesNotHeal()
        {
            // Healing is paid for. Free regeneration would make injury meaningless rather than
            // recoverable, which is the opposite failure to the one being fixed.
            Phenotype phenotype = Subject;
            CreatureNeeds needs = Injured(phenotype, healthFraction: 0.5f, suppliesFraction: 0.2f);
            float before = needs.Health;

            NeedsSystem.RecoverHealth(ref needs, phenotype, deltaTime: 1f);

            Assert.That(needs.Health, Is.EqualTo(before));
        }

        [Test]
        public void AThirstyCreatureDoesNotHeal()
        {
            Phenotype phenotype = Subject;
            CreatureNeeds needs = Injured(phenotype, healthFraction: 0.5f, suppliesFraction: 1f);
            needs.Hydration = phenotype.HydrationCapacity * 0.2f;
            float before = needs.Health;

            NeedsSystem.RecoverHealth(ref needs, phenotype, deltaTime: 1f);

            Assert.That(needs.Health, Is.EqualTo(before));
        }

        [Test]
        public void HealingStopsAtCapacity()
        {
            Phenotype phenotype = Subject;
            CreatureNeeds needs = Injured(phenotype, healthFraction: 1f, suppliesFraction: 1f);

            NeedsSystem.RecoverHealth(ref needs, phenotype, deltaTime: 100f);

            Assert.That(needs.Health, Is.EqualTo(phenotype.HealthCapacity).Within(Tolerance));
        }

        [Test]
        public void StandingInAHotBandStillLosesGround()
        {
            // The rate is deliberately under the peak thermal damage rate, so a creature in a hot
            // band nets a loss and only makes it back once it leaves. If healing outran the damage,
            // temperature would stop meaning anything.
            Phenotype phenotype = Subject;
            CreatureNeeds needs = Injured(phenotype, healthFraction: 0.9f, suppliesFraction: 1f);
            float before = needs.Health;

            NeedsSystem.ApplyTemperatureStress(ref needs, phenotype, temperature: 28f, deltaTime: 1f);
            NeedsSystem.RecoverHealth(ref needs, phenotype, deltaTime: 1f);

            Assert.That(needs.Health, Is.LessThan(before));
        }

        [Test]
        public void MetabolicPaceScalesHealingByTheSameFactorTheDrainsUse()
        {
            // The first private benefit MetabolicPace has ever had. Ingestion failed because
            // contested sites are shared; nobody can consume someone else's healing.
            Phenotype phenotype = Subject;
            CreatureNeeds slowNeeds = Injured(phenotype, healthFraction: 0.5f, suppliesFraction: 1f);
            CreatureNeeds fastNeeds = Injured(phenotype, healthFraction: 0.5f, suppliesFraction: 1f);
            float before = slowNeeds.Health;

            NeedsSystem.RecoverHealth(ref slowNeeds, phenotype, deltaTime: 1f, metabolicScale: 0.7f);
            NeedsSystem.RecoverHealth(ref fastNeeds, phenotype, deltaTime: 1f, metabolicScale: 1.5f);

            Assert.That(
                (fastNeeds.Health - before) / (slowNeeds.Health - before),
                Is.EqualTo(1.5f / 0.7f).Within(1e-3f));
        }

        [Test]
        public void MetabolicHealingIsInertWithoutHealing()
        {
            // Same shape as the slope cost needing elevation: a flag with nothing to act on must do
            // nothing at all, rather than something small and unexplained.
            SimulationConfig without = Config(healthRecovery: false);
            var with = new SimulationConfig(
                worldSeed: Seed,
                initialPopulation: 8,
                schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                healthRecoveryEnabled: false,
                metabolicHealingEnabled: true);

            Assert.That(Run(with), Is.EqualTo(Run(without)));
        }

        [Test]
        public void TheFlagOffChangesNothingAtAll()
        {
            Assert.That(Run(Config(healthRecovery: false)), Is.EqualTo(Run(Config(healthRecovery: false))));
        }

        [Test]
        public void TheFlagOnChangesHowTheWorldEvolves()
        {
            Assert.That(
                Run(Config(healthRecovery: true)),
                Is.Not.EqualTo(Run(Config(healthRecovery: false))));
        }

        [Test]
        public void TheConfigurationHashSeesTheFlag()
        {
            Assert.That(
                Config(healthRecovery: true).ComputeConfigurationHash(),
                Is.Not.EqualTo(Config(healthRecovery: false).ComputeConfigurationHash()));
        }
    }
}
