using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class GeneticClusterObservationTests
    {
        [Test]
        public void ObservationKeepsSnapshotProvenanceAndDerivedClusterOrdinals()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.Neutral.WithBodySize(.2f));
            creatures.Add(Genome.Neutral.WithBodySize(.3f));
            PopulationGenomeSnapshot snapshot = PopulationGenomeSnapshot.Capture(10, creatures);

            GeneticClusterObservation observation = GeneticClusterObservation.Create(snapshot, threshold: .5f);

            Assert.That(observation.Snapshot, Is.SameAs(snapshot));
            Assert.That(observation.Tick, Is.EqualTo(10));
            Assert.That(observation.Threshold, Is.EqualTo(.5f));
            Assert.That(observation.IsSampled, Is.False);
            Assert.That(observation.SourcePopulationCount, Is.EqualTo(2));
            Assert.That(observation.SampleLimit, Is.EqualTo(0));
            Assert.That(observation.ClusterCount, Is.EqualTo(1));
            Assert.That(observation.GetClusterIndexAt(0), Is.EqualTo(observation.GetClusterIndexAt(1)));
        }

        [Test]
        public void ObservationRejectsNullSnapshotAndThresholdOutsideUnitRange()
        {
            Assert.That(() => GeneticClusterObservation.Create(null!, threshold: .5f), Throws.ArgumentNullException);

            var creatures = new CreatureStore(1);
            creatures.Add(Genome.Neutral);
            PopulationGenomeSnapshot snapshot = PopulationGenomeSnapshot.Capture(10, creatures);

            Assert.That(() => GeneticClusterObservation.Create(snapshot, threshold: -.01f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => GeneticClusterObservation.Create(snapshot, threshold: 1.01f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => GeneticClusterObservation.Create(snapshot, threshold: float.NaN), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void PolicyRejectsNonPositiveCountsDepthsAndWindows()
        {
            Assert.That(() => new ClusterHistoryPolicy(0, .5f, 1, .5f, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, .5f, 0, .5f, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, .5f, 1, .5f, 0, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, .5f, 1, .5f, 1, 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, .5f, 1, .5f, 1, 1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void PolicyRejectsFractionsOutsideTheOpenClosedUnitRange()
        {
            Assert.That(() => new ClusterHistoryPolicy(1, 0f, 1, .5f, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, 1.01f, 1, .5f, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, float.NaN, 1, .5f, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, .5f, 1, 0f, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, .5f, 1, 1.01f, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ClusterHistoryPolicy(1, .5f, 1, float.NaN, 1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void EqualPoliciesCompareByEveryConfiguredValue()
        {
            var first = new ClusterHistoryPolicy(2, .6f, 3, .7f, 4, 5, 6);
            var same = new ClusterHistoryPolicy(2, .6f, 3, .7f, 4, 5, 6);
            var different = new ClusterHistoryPolicy(2, .6f, 3, .7f, 4, 5, 7);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(different));
        }
    }
}
