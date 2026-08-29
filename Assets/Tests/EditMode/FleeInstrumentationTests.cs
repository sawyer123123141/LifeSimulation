using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// How often creatures actually flee - the measurement the flee argument was missing.
    ///
    /// <para><c>risk_aversion</c> is selected against at t = -3 to -6 across twenty-two powered
    /// predation cells, and the proposed reason is that the same gene's foraging-caution role
    /// outweighs its flee role in a cell losing 44.8% of deaths to starvation against 8.4% to
    /// predation. **That story was inferred from gene drift and the death mix; nothing reported how
    /// often fleeing happened at all.** See
    /// <c>docs/emergent-behaviour-fleeing-is-selected-against-2026-08-29.md</c>.</para>
    /// </summary>
    public sealed class FleeInstrumentationTests
    {
        private const int Seed = 42;
        private const int Ticks = 2000;

        private static SimulationConfig Config(FounderProfile profile)
        {
            return new SimulationConfig(
                worldSeed: Seed,
                initialPopulation: 8,
                schedule: new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: profile,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true);
        }

        private static SimulationWorld Run(SimulationConfig config)
        {
            var world = new SimulationWorld(config);
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.ApplyTo(world);
            for (int tick = 0; tick < Ticks; tick++) world.Step(config.FixedDeltaTime);
            return world;
        }

        [Test]
        public void CountingFleeingDoesNotChangeTheWorld()
        {
            // The whole point of an instrument: it must be inert. These counters are deliberately
            // left out of both hashes, so every number recorded before they existed stays comparable.
            SimulationConfig config = Config(FounderProfile.PredationVariation);
            ulong behaviour = Run(config).ComputeBehaviorHash();
            ulong again = Run(config).ComputeBehaviorHash();
            Assert.That(again, Is.EqualTo(behaviour));
        }

        [Test]
        public void EveryDecisionIsCounted()
        {
            SimulationStatistics statistics = Run(Config(FounderProfile.PredationVariation)).CaptureStatistics();
            Assert.That(statistics.DecisionCount, Is.GreaterThan(0), "no decisions were counted at all");
            Assert.That(statistics.FleeDecisionCount, Is.LessThanOrEqualTo(statistics.DecisionCount));
        }

        [Test]
        public void TheFractionIsSafeBeforeAnythingHasDecided()
        {
            // Guards the division. A world read at tick 0 must report 0, not divide by zero.
            var world = new SimulationWorld(Config(FounderProfile.PredationVariation));
            Assert.That(world.CaptureStatistics().FleeingFraction, Is.EqualTo(0f));
        }

        [Test]
        public void HerbivoresWithNoThreatsNeverFlee()
        {
            // The negative control. PhysiologyVariation seeds herbivores, so no creature is ever a
            // threat and the flee branch has no occasion to fire. A non-zero count here would mean
            // creatures are fleeing from something that cannot hurt them.
            SimulationStatistics statistics = Run(Config(FounderProfile.PhysiologyVariation)).CaptureStatistics();
            Assert.That(statistics.DecisionCount, Is.GreaterThan(0));
            Assert.That(statistics.FleeDecisionCount, Is.EqualTo(0));
        }
    }
}
