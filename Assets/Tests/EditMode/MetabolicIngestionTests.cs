using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Giving <see cref="Genome.MetabolicPace"/> the benefit it never had.
    ///
    /// <para>The gene raises two drains 2.14x across its range and had no third reader at all, so it
    /// was a pure cost the population was steadily selling. These pin that the flag off changes
    /// nothing, that the flag on scales ingestion by exactly the factor the drains already use, and
    /// that it actually reaches behaviour.</para>
    /// </summary>
    public sealed class MetabolicIngestionTests
    {
        private const int Seed = 42;
        private const int Ticks = 600;
        private const float Tolerance = 1e-5f;

        private static Genome Pace(float pace)
        {
            return new Genome(0.5f, 0.5f, pace, 0.5f, 0.5f, 0.5f);
        }

        private static SimulationConfig Config(bool metabolicIngestion)
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
                metabolicIngestionEnabled: metabolicIngestion);
        }

        /// <summary>
        /// Behaviour hash, not the fingerprint - the fingerprint folds in the config hash and would
        /// differ even if the flag did nothing.
        ///
        /// <para><b>The scenario is not optional here.</b> A bare world has no resources, so nothing
        /// is ever ingested and a flag that only changes ingestion is inert by construction. The
        /// first version of this test omitted it and the liveness check failed with identical
        /// hashes - correctly.</para>
        /// </summary>
        private static ulong Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world.ComputeBehaviorHash();
        }

        [Test]
        public void WithoutTheFlagPaceDoesNotTouchIngestionAtAll()
        {
            // The defect being fixed, stated as a test: this is what "pure cost" means.
            float slow = Phenotype.FromGenome(Pace(0f)).IngestionRate;
            float fast = Phenotype.FromGenome(Pace(1f)).IngestionRate;

            Assert.That(fast, Is.EqualTo(slow).Within(Tolerance));
        }

        [Test]
        public void WithTheFlagIngestionScalesByTheSameFactorTheDrainsUse()
        {
            // 0.7 + 0.8*pace, the identical factor applied to BasalEnergyCostMultiplier and
            // DigestionRate. Using a different curve here would make the trade-off arbitrary.
            float slow = Phenotype.FromGenome(Pace(0f), metabolicIngestionEnabled: true).IngestionRate;
            float fast = Phenotype.FromGenome(Pace(1f), metabolicIngestionEnabled: true).IngestionRate;
            float middle = Phenotype.FromGenome(Pace(0.5f), metabolicIngestionEnabled: true).IngestionRate;

            Assert.That(slow, Is.EqualTo((1.25f - 0.15f) * 0.7f).Within(Tolerance));
            Assert.That(fast, Is.EqualTo((1.25f - 0.15f) * 1.5f).Within(Tolerance));
            Assert.That(fast / slow, Is.EqualTo(1.5f / 0.7f).Within(Tolerance));
            Assert.That(middle, Is.EqualTo((1.25f - 0.15f) * 1.1f).Within(Tolerance));
        }

        [Test]
        public void TheDrainsAreUnchangedByTheFlag()
        {
            // The flag adds a benefit. If it also moved a cost, the two could not be told apart.
            Phenotype without = Phenotype.FromGenome(Pace(0.8f));
            Phenotype with = Phenotype.FromGenome(Pace(0.8f), metabolicIngestionEnabled: true);

            Assert.That(with.BasalEnergyCostMultiplier, Is.EqualTo(without.BasalEnergyCostMultiplier).Within(Tolerance));
            Assert.That(with.DigestionRate, Is.EqualTo(without.DigestionRate).Within(Tolerance));
            Assert.That(with.BodyMass, Is.EqualTo(without.BodyMass).Within(Tolerance));
            Assert.That(with.FoodYield, Is.EqualTo(without.FoodYield).Within(Tolerance));
        }

        [Test]
        public void FoodEfficiencyKeepsItsOwnTradeOff()
        {
            // Efficient eaters already eat slower and extract more. Pace multiplies that curve rather
            // than replacing it, so the two genes stay separable.
            float efficientSlow = Phenotype.FromGenome(
                new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 1f), metabolicIngestionEnabled: true).IngestionRate;
            float inefficientFast = Phenotype.FromGenome(
                new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0f), metabolicIngestionEnabled: true).IngestionRate;

            Assert.That(efficientSlow, Is.LessThan(inefficientFast));
        }

        [Test]
        public void TheFlagOffChangesNothingAtAll()
        {
            Assert.That(Run(Config(metabolicIngestion: false)), Is.EqualTo(Run(Config(metabolicIngestion: false))));
        }

        [Test]
        public void TheFlagOnChangesHowTheWorldEvolves()
        {
            Assert.That(
                Run(Config(metabolicIngestion: true)),
                Is.Not.EqualTo(Run(Config(metabolicIngestion: false))));
        }

        [Test]
        public void TheConfigurationHashSeesTheFlag()
        {
            Assert.That(
                Config(metabolicIngestion: true).ComputeConfigurationHash(),
                Is.Not.EqualTo(Config(metabolicIngestion: false).ComputeConfigurationHash()));
        }
    }
}
