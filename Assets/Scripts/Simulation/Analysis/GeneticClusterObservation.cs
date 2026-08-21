using System;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Immutable snapshot, threshold, and derived genetic clusters for one analytical observation.</summary>
    public sealed class GeneticClusterObservation
    {
        private readonly GeneticClusters _clusters;

        private GeneticClusterObservation(PopulationGenomeSnapshot snapshot, float threshold, GeneticClusters clusters)
        {
            Snapshot = snapshot;
            Threshold = threshold;
            _clusters = clusters;
        }

        public PopulationGenomeSnapshot Snapshot { get; }
        public long Tick => Snapshot.Tick;
        public float Threshold { get; }
        public bool IsSampled => Snapshot.IsSampled;
        public int SourcePopulationCount => Snapshot.SourcePopulationCount;
        public int SampleLimit => Snapshot.SampleLimit;
        public int ClusterCount => _clusters.Count;

        public static GeneticClusterObservation Create(PopulationGenomeSnapshot snapshot, float threshold)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!(threshold >= 0f && threshold <= 1f)) throw new ArgumentOutOfRangeException(nameof(threshold));
            return new GeneticClusterObservation(snapshot, threshold, GeneticClusters.From(snapshot, threshold));
        }

        public int GetClusterIndexAt(int sampleIndex) => _clusters.GetClusterIndexAt(sampleIndex);
    }
}
