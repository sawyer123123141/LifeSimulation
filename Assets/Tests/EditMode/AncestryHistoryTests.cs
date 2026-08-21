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
    }
}
