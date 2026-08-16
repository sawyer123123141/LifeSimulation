using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public class ReproductionSystemTests
    {
        private static (CreatureStore creatures, ReproductionSystem reproduction) CreateHarness()
        {
            var creatures = new CreatureStore(initialCapacity: 4);
            var arena = new ArenaBounds(-100f, 100f, -100f, 100f);
            var reproduction = new ReproductionSystem(creatures, arena, initialCapacity: 4, physiologyEnabled: false, mateSelectionEnabled: true);
            return (creatures, reproduction);
        }

        private static int AddReadyAdult(CreatureStore creatures, SimVector2 position)
        {
            CreatureId id = creatures.Add(Genome.Neutral, position);
            creatures.TryGetIndex(id, out int index);
            creatures.GetNeedsRefAt(index).Age = ReproductionSystem.AdultAgeSeconds;
            return index;
        }

        [Test]
        public void FindSeekMateTargetReturnsTargetWhenSeekingAReadyInRangePartner()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(1f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: creatures.GetIdAt(secondIndex)));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(secondIndex));
        }

        [Test]
        public void FindSeekMateTargetReturnsNegativeOneWhenNotSeekingMate()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(1f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.Wander, -1, 0f));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(-1));
            Assert.That(secondIndex, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void FindSeekMateTargetReturnsNegativeOneWhenTargetOutOfRange()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(50f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: creatures.GetIdAt(secondIndex)));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void FindSeekMateTargetReturnsNegativeOneWhenTargetNotReady()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            CreatureId secondId = creatures.Add(Genome.Neutral, new SimVector2(1f, 0f));
            creatures.TryGetIndex(secondId, out int secondIndex);
            creatures.GetNeedsRefAt(secondIndex).Age = 0f;
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: secondId));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void FindSeekMateTargetIsSufficientEvenWhenTargetIsNotSeekingBack()
        {
            (CreatureStore creatures, ReproductionSystem reproduction) = CreateHarness();
            int firstIndex = AddReadyAdult(creatures, new SimVector2(0f, 0f));
            int secondIndex = AddReadyAdult(creatures, new SimVector2(1f, 0f));
            creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.SeekMate, -1, 0.5f, targetCreatureId: creatures.GetIdAt(secondIndex)));
            creatures.SetDecisionAt(secondIndex, new CreatureDecision(CreatureAction.Wander, -1, 0f));

            int result = reproduction.FindSeekMateTargetForTest(firstIndex, creatures.Count);

            Assert.That(result, Is.EqualTo(secondIndex));
        }
    }
}
