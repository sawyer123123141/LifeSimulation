using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class GeneticClusteringTests
    {
        [Test]
        public void ExplicitThresholdSeparatesDistantGenomeSamples()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.FromTraits(new float[Genome.TraitCount]));
            var highTraits = new float[Genome.TraitCount];
            for (int index = 0; index < highTraits.Length; index++) highTraits[index] = 1f;
            creatures.Add(Genome.FromTraits(highTraits));

            GeneticClusters clusters = GeneticClusters.From(PopulationGenomeSnapshot.Capture(10, creatures), threshold: .5f);

            Assert.That(clusters.Count, Is.EqualTo(2));
        }

        [Test]
        public void IntermediateGenomeConnectsSamplesInsideTheExplicitThreshold()
        {
            var creatures = new CreatureStore(3);
            creatures.Add(Genome.FromTraits(new float[Genome.TraitCount]));
            var middleTraits = new float[Genome.TraitCount];
            for (int index = 0; index < middleTraits.Length; index++) middleTraits[index] = .5f;
            creatures.Add(Genome.FromTraits(middleTraits));
            var highTraits = new float[Genome.TraitCount];
            for (int index = 0; index < highTraits.Length; index++) highTraits[index] = 1f;
            creatures.Add(Genome.FromTraits(highTraits));

            GeneticClusters clusters = GeneticClusters.From(PopulationGenomeSnapshot.Capture(10, creatures), threshold: .5f);

            Assert.That(clusters.Count, Is.EqualTo(1));
        }

        [Test]
        public void ClusterMembershipKeepsConnectedSamplesTogether()
        {
            var creatures = new CreatureStore(3);
            creatures.Add(Genome.FromTraits(new float[Genome.TraitCount]));
            var nearbyTraits = new float[Genome.TraitCount];
            for (int index = 0; index < nearbyTraits.Length; index++) nearbyTraits[index] = .25f;
            creatures.Add(Genome.FromTraits(nearbyTraits));
            var distantTraits = new float[Genome.TraitCount];
            for (int index = 0; index < distantTraits.Length; index++) distantTraits[index] = 1f;
            creatures.Add(Genome.FromTraits(distantTraits));

            GeneticClusters clusters = GeneticClusters.From(PopulationGenomeSnapshot.Capture(10, creatures), threshold: .5f);

            Assert.That(clusters.GetClusterIndexAt(0), Is.EqualTo(clusters.GetClusterIndexAt(1)));
            Assert.That(clusters.GetClusterIndexAt(2), Is.Not.EqualTo(clusters.GetClusterIndexAt(0)));
        }
    }
}
