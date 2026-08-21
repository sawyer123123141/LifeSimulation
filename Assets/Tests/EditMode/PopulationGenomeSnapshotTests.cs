using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PopulationGenomeSnapshotTests
    {
        [Test]
        public void SnapshotCopiesCurrentCreatureIdsAndGenomesAtTheRequestedTick()
        {
            var creatures = new CreatureStore(2);
            CreatureId first = creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId second = creatures.Add(Genome.Neutral.WithBodySize(.8f));

            PopulationGenomeSnapshot snapshot = PopulationGenomeSnapshot.Capture(17, creatures);

            Assert.That(snapshot.Tick, Is.EqualTo(17));
            Assert.That(snapshot.Count, Is.EqualTo(2));
            Assert.That(snapshot.GetIdAt(0), Is.EqualTo(first));
            Assert.That(snapshot.GetGenomeAt(1).BodySize, Is.EqualTo(.8f));
            Assert.That(snapshot.GetIdAt(1), Is.EqualTo(second));
        }
    }
}
