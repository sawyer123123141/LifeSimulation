using LifeSimulation.Simulation.Analysis;
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
    }
}
