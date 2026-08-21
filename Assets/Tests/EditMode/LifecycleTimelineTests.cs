using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class LifecycleTimelineTests
    {
        [Test]
        public void TimelineGroupsBirthsAndDeathsAtTheSameTick()
        {
            var timeline = new LifecycleTimeline();
            timeline.Record(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(3), new CreatureId(1), new CreatureId(2), DeathCause.None));
            timeline.Record(new SimulationEvent(10, SimulationEventKind.Death, new CreatureId(4), default, default, DeathCause.Starvation));

            Assert.That(timeline.Count, Is.EqualTo(1));
            LifecycleTimelineEntry entry = timeline.GetAt(0);
            Assert.That(entry.Tick, Is.EqualTo(10));
            Assert.That(entry.BirthCount, Is.EqualTo(1));
            Assert.That(entry.DeathCount, Is.EqualTo(1));
            Assert.That(entry.StarvationDeathCount, Is.EqualTo(1));
        }

        [Test]
        public void TimelineCanReadAnEventBufferWithoutClearingIt()
        {
            var events = new SimulationEventBuffer(2);
            events.TryWrite(new SimulationEvent(10, SimulationEventKind.Birth, new CreatureId(3), new CreatureId(1), new CreatureId(2), DeathCause.None));
            var timeline = new LifecycleTimeline();

            timeline.Record(events);

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(timeline.Count, Is.EqualTo(1));
            Assert.That(timeline.GetAt(0).BirthCount, Is.EqualTo(1));
        }
    }
}
