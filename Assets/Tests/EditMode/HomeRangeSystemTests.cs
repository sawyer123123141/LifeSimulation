using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class HomeRangeSystemTests
    {
        [Test]
        public void RecordSuccessMovesCentreTowardPositionAndRaisesFamiliarity()
        {
            var state = new HomeRangeState
            {
                Centre = new SimVector2(2f, -2f),
                Familiarity = 0.5f,
            };

            HomeRangeSystem.RecordSuccess(ref state, new SimVector2(10f, 6f));

            Assert.That(state.Centre.X, Is.EqualTo(4f));
            Assert.That(state.Centre.Y, Is.EqualTo(0f));
            Assert.That(state.Familiarity, Is.EqualTo(0.75f));
        }

        [Test]
        public void TickDecayClampsFamiliarityToZeroWithoutMovingCentre()
        {
            var state = new HomeRangeState
            {
                Centre = new SimVector2(3f, -4f),
                Familiarity = 0.005f,
            };

            HomeRangeSystem.TickDecay(ref state, 1f);

            Assert.That(state.Centre.X, Is.EqualTo(3f));
            Assert.That(state.Centre.Y, Is.EqualTo(-4f));
            Assert.That(state.Familiarity, Is.EqualTo(0f));
        }

        [Test]
        public void GetCandidateBonusRewardsNearCandidatesWithoutExceedingTheMaximum()
        {
            var state = new HomeRangeState
            {
                Centre = new SimVector2(0f, 0f),
                Familiarity = 0.8f,
            };

            float bonus = HomeRangeSystem.GetCandidateBonus(state, new SimVector2(2f, 0f));

            Assert.That(bonus, Is.EqualTo(0.064f));
            Assert.That(bonus, Is.GreaterThan(0f));
            Assert.That(bonus, Is.LessThanOrEqualTo(0.1f));
        }

        [Test]
        public void GetCandidateBonusReturnsZeroForBlankState()
        {
            float bonus = HomeRangeSystem.GetCandidateBonus(default, new SimVector2(0f, 0f));

            Assert.That(bonus, Is.EqualTo(0f));
        }

        [Test]
        public void CreatureStoreDefaultsNewbornReplacementAndSwappedStateWithoutInheritance()
        {
            var store = new CreatureStore(initialCapacity: 2);
            CreatureId firstParent = store.Add();
            CreatureId secondParent = store.Add();
            store.TryGetIndex(firstParent, out int firstParentIndex);
            store.TryGetIndex(secondParent, out int secondParentIndex);
            store.GetHomeRangeRefAt(firstParentIndex) = new HomeRangeState
            {
                Centre = new SimVector2(7f, 8f),
                Familiarity = 1f,
            };
            store.GetHomeRangeRefAt(secondParentIndex) = new HomeRangeState
            {
                Centre = new SimVector2(-5f, -6f),
                Familiarity = 0.5f,
            };

            CreatureId child = store.AddChild(Genome.Neutral, new SimVector2(0f, 0f), firstParent, secondParent);
            Assert.That(store.TryGetIndex(child, out int childIndex), Is.True);
            AssertBlank(store.GetHomeRangeRefAt(childIndex));

            Assert.That(store.Remove(firstParent), Is.True);
            Assert.That(store.TryGetIndex(child, out childIndex), Is.True);
            AssertBlank(store.GetHomeRangeRefAt(childIndex));
            Assert.That(store.TryGetIndex(secondParent, out int survivingParentIndex), Is.True);
            Assert.That(store.GetHomeRangeRefAt(survivingParentIndex).Centre.X, Is.EqualTo(-5f));
            Assert.That(store.GetHomeRangeRefAt(survivingParentIndex).Centre.Y, Is.EqualTo(-6f));
            Assert.That(store.GetHomeRangeRefAt(survivingParentIndex).Familiarity, Is.EqualTo(0.5f));

            CreatureId replacement = store.Add();
            Assert.That(store.TryGetIndex(replacement, out int replacementIndex), Is.True);
            AssertBlank(store.GetHomeRangeRefAt(replacementIndex));
        }

        [Test]
        public void SimulationConfigKeepsHomeRangeAffinityDisabledByDefault()
        {
            var schedule = new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1);

            var defaultConfig = new SimulationConfig(worldSeed: 1, initialPopulation: 1, schedule: schedule);
            var enabledConfig = new SimulationConfig(worldSeed: 1, initialPopulation: 1, schedule: schedule, homeRangeAffinityEnabled: true);

            Assert.That(defaultConfig.HomeRangeAffinityEnabled, Is.False);
            Assert.That(enabledConfig.HomeRangeAffinityEnabled, Is.True);
        }

        private static void AssertBlank(HomeRangeState state)
        {
            Assert.That(state.Centre.X, Is.EqualTo(0f));
            Assert.That(state.Centre.Y, Is.EqualTo(0f));
            Assert.That(state.Familiarity, Is.EqualTo(0f));
        }
    }
}
