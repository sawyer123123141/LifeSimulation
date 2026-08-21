using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class AncestryHistoryTests
    {
        [Test]
        public void HistoryRetainsBirthParentsAndLaterDeathWithoutReadingSimulationState()
        {
            var history = new AncestryHistory();
            var child = new CreatureId(3);
            history.Record(new SimulationEvent(10, SimulationEventKind.Birth, child, new CreatureId(1), new CreatureId(2), DeathCause.None));
            history.Record(new SimulationEvent(25, SimulationEventKind.Death, child, default, default, DeathCause.Predation));

            Assert.That(history.TryGet(child, out AncestryRecord record), Is.True);
            Assert.That(record.BirthTick, Is.EqualTo(10));
            Assert.That(record.FirstParent, Is.EqualTo(new CreatureId(1)));
            Assert.That(record.SecondParent, Is.EqualTo(new CreatureId(2)));
            Assert.That(record.DeathTick, Is.EqualTo(25));
            Assert.That(record.DeathCause, Is.EqualTo(DeathCause.Predation));
        }

        [Test]
        public void HistoryCanReadAnEventBufferWithoutClearingTheHostBuffer()
        {
            var events = new SimulationEventBuffer(2);
            events.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(3), new CreatureId(1), new CreatureId(2), DeathCause.None));
            var history = new AncestryHistory();

            history.Record(events);

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(history.TryGet(new CreatureId(3), out _), Is.True);
        }

        [Test]
        public void HistoryIndexesKnownDescendantsByParent()
        {
            var history = new AncestryHistory();
            var firstChild = new CreatureId(3);
            var secondChild = new CreatureId(4);
            var parent = new CreatureId(1);
            history.Record(new SimulationEvent(10, SimulationEventKind.Birth, firstChild, parent, new CreatureId(2), DeathCause.None));
            history.Record(new SimulationEvent(20, SimulationEventKind.Birth, secondChild, parent, default, DeathCause.None));

            Assert.That(history.GetChildCount(parent), Is.EqualTo(2));
            Assert.That(history.GetChildAt(parent, 0), Is.EqualTo(firstChild));
            Assert.That(history.GetChildAt(parent, 1), Is.EqualTo(secondChild));
        }

        [Test]
        public void HistoryCanRecordFoundersBeforeItConsumesBirthEvents()
        {
            var creatures = new CreatureStore(2);
            CreatureId firstFounder = creatures.Add(Genome.Neutral);
            CreatureId secondFounder = creatures.Add(Genome.Neutral);
            var history = new AncestryHistory();

            history.RecordFounders(0, creatures);

            Assert.That(history.TryGet(firstFounder, out AncestryRecord firstRecord), Is.True);
            Assert.That(firstRecord.BirthTick, Is.EqualTo(0));
            Assert.That(firstRecord.FirstParent, Is.EqualTo(default(CreatureId)));
            Assert.That(history.TryGet(secondFounder, out AncestryRecord secondRecord), Is.True);
            Assert.That(secondRecord.SecondParent, Is.EqualTo(default(CreatureId)));
        }

        [Test]
        public void ReplayingTheSameBirthDoesNotDuplicateTheParentChildIndex()
        {
            var history = new AncestryHistory();
            var parent = new CreatureId(1);
            var birth = new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(3), parent, new CreatureId(2), DeathCause.None);

            history.Record(birth);
            history.Record(birth);

            Assert.That(history.GetChildCount(parent), Is.EqualTo(1));
        }

        [Test]
        public void CompleteBatchAdvancesAnEmptyOrderedEventRangeAfterFoundersAreRecorded()
        {
            var history = new AncestryHistory();
            history.RecordFounders(0, new CreatureStore(1));
            var events = new SimulationEventBuffer(1);

            history.RecordCompleteBatch(events, 10);

            Assert.That(history.HasRecordedFounders, Is.True);
            Assert.That(history.IsComplete, Is.True);
            Assert.That(history.CompleteThroughTick, Is.EqualTo(10));
            Assert.That(events.Count, Is.EqualTo(0));
            Assert.That(events.Overflowed, Is.False);
        }

        [Test]
        public void CompleteBatchRecordsBirthBeforeItAdvancesTheCompletenessWatermark()
        {
            var history = new AncestryHistory();
            history.RecordFounders(0, new CreatureStore(1));
            var child = new CreatureId(3);
            var events = new SimulationEventBuffer(1);
            events.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, child, new CreatureId(1), new CreatureId(2), DeathCause.None));

            history.RecordCompleteBatch(events, 10);

            Assert.That(history.TryGet(child, out AncestryRecord record), Is.True);
            Assert.That(record.BirthTick, Is.EqualTo(10));
            Assert.That(history.CompleteThroughTick, Is.EqualTo(10));
        }

        [Test]
        public void CompleteBatchRejectsADecreasingThroughTick()
        {
            var history = new AncestryHistory();
            history.RecordFounders(0, new CreatureStore(1));
            var events = new SimulationEventBuffer(1);
            history.RecordCompleteBatch(events, 10);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => history.RecordCompleteBatch(events, 9));

            Assert.That(history.CompleteThroughTick, Is.EqualTo(10));
            Assert.That(history.IsComplete, Is.True);
        }

        [Test]
        public void CompleteBatchRejectsEventsOlderThanItsCompletedWatermarkWithoutMutatingHistory()
        {
            var history = new AncestryHistory();
            history.RecordFounders(0, new CreatureStore(1));
            history.RecordCompleteBatch(new SimulationEventBuffer(1), 20);
            var lateChild = new CreatureId(3);
            var events = new SimulationEventBuffer(1);
            events.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, lateChild, new CreatureId(1), new CreatureId(2), DeathCause.None));

            Assert.Throws<System.ArgumentException>(() => history.RecordCompleteBatch(events, 30));

            Assert.That(history.TryGet(lateChild, out _), Is.False);
            Assert.That(history.CompleteThroughTick, Is.EqualTo(20));
            Assert.That(history.IsComplete, Is.True);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.GetAt(0).Subject, Is.EqualTo(lateChild));
        }

        [Test]
        public void CompleteBatchAllowsAnEqualTickReplayWithoutDuplicatingAncestry()
        {
            var history = new AncestryHistory();
            history.RecordFounders(0, new CreatureStore(1));
            var parent = new CreatureId(1);
            var child = new CreatureId(3);
            var events = new SimulationEventBuffer(1);
            events.TryWrite(new SimulationEvent(20, SimulationEventKind.Birth, child, parent, new CreatureId(2), DeathCause.None));

            history.RecordCompleteBatch(events, 20);
            history.RecordCompleteBatch(events, 20);

            Assert.Throws<System.ArgumentException>(() => history.RecordCompleteBatch(events, 30));

            Assert.That(history.TryGet(child, out _), Is.True);
            Assert.That(history.GetChildCount(parent), Is.EqualTo(1));
            Assert.That(history.CompleteThroughTick, Is.EqualTo(20));
            Assert.That(history.IsComplete, Is.True);
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        public void OverflowedBatchLeavesTheHostBufferUntouchedAndPermanentlyMakesHistoryIncomplete()
        {
            var history = new AncestryHistory();
            history.RecordFounders(0, new CreatureStore(1));
            history.RecordCompleteBatch(new SimulationEventBuffer(1), 5);
            var firstChild = new CreatureId(3);
            var events = new SimulationEventBuffer(1);
            events.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, firstChild, new CreatureId(1), new CreatureId(2), DeathCause.None));
            bool secondWriteSucceeded = events.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(4), new CreatureId(1), new CreatureId(2), DeathCause.None));

            history.RecordCompleteBatch(events, 10);

            Assert.That(secondWriteSucceeded, Is.False);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events.Overflowed, Is.True);
            Assert.That(events.GetAt(0).Subject, Is.EqualTo(firstChild));
            Assert.That(history.TryGet(firstChild, out _), Is.True);
            Assert.That(history.IsComplete, Is.False);
            Assert.That(history.CompleteThroughTick, Is.EqualTo(5));

            history.RecordCompleteBatch(new SimulationEventBuffer(1), 20);

            Assert.That(history.IsComplete, Is.False);
            Assert.That(history.CompleteThroughTick, Is.EqualTo(5));
        }
    }
}
