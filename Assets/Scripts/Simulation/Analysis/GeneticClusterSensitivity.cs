using System;

namespace LifeSimulation.Simulation.Analysis
{
    public readonly struct GeneticClusterSensitivityEntry
    {
        public GeneticClusterSensitivityEntry(float threshold, int clusterCount)
        {
            Threshold = threshold;
            ClusterCount = clusterCount;
        }

        public float Threshold { get; }
        public int ClusterCount { get; }
    }

    /// <summary>Analysis-only threshold sweep; it exposes clustering uncertainty without selecting a species threshold.</summary>
    public sealed class GeneticClusterSensitivity
    {
        private readonly GeneticClusterSensitivityEntry[] _entries;

        private GeneticClusterSensitivity(GeneticClusterSensitivityEntry[] entries)
        {
            _entries = entries;
        }

        public int Count => _entries.Length;

        public static GeneticClusterSensitivity Analyze(PopulationGenomeSnapshot snapshot, float[] thresholds)
        {
            if (thresholds == null) throw new ArgumentNullException(nameof(thresholds));
            var entries = new GeneticClusterSensitivityEntry[thresholds.Length];
            for (int index = 0; index < thresholds.Length; index++)
            {
                float threshold = thresholds[index];
                entries[index] = new GeneticClusterSensitivityEntry(threshold, GeneticClusters.From(snapshot, threshold).Count);
            }

            return new GeneticClusterSensitivity(entries);
        }

        public GeneticClusterSensitivityEntry GetAt(int index)
        {
            if ((uint)index >= (uint)_entries.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return _entries[index];
        }
    }
}
