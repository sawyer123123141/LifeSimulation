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

        [Test]
        public void SampleUsesEvenlySpacedCreatureIdsIncludingBothEnds()
        {
            var creatures = new CreatureStore(5);
            CreatureId first = creatures.Add(Genome.Neutral.WithBodySize(.1f));
            creatures.Add(Genome.Neutral.WithBodySize(.2f));
            CreatureId middle = creatures.Add(Genome.Neutral.WithBodySize(.3f));
            creatures.Add(Genome.Neutral.WithBodySize(.4f));
            CreatureId last = creatures.Add(Genome.Neutral.WithBodySize(.5f));

            PopulationGenomeSnapshot sample = PopulationGenomeSnapshot.CaptureSample(17, creatures, maximumCount: 3);

            Assert.That(sample.Count, Is.EqualTo(3));
            Assert.That(sample.GetIdAt(0), Is.EqualTo(first));
            Assert.That(sample.GetIdAt(1), Is.EqualTo(middle));
            Assert.That(sample.GetIdAt(2), Is.EqualTo(last));
        }
    }
}
