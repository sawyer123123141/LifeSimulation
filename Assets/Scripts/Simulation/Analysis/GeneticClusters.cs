using System;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Analysis-only connected genetic groups; callers must state their threshold explicitly.</summary>
    public sealed class GeneticClusters
    {
        private readonly int[] _clusterIndices;

        private GeneticClusters(int count, int[] clusterIndices)
        {
            Count = count;
            _clusterIndices = clusterIndices;
        }

        public int Count { get; }

        public static GeneticClusters From(PopulationGenomeSnapshot snapshot, float threshold)
        {
            if (threshold < 0f || threshold > 1f) throw new ArgumentOutOfRangeException(nameof(threshold));
            var parents = new int[snapshot.Count];
            for (int index = 0; index < parents.Length; index++) parents[index] = index;
            for (int first = 0; first < snapshot.Count; first++)
            {
                for (int second = first + 1; second < snapshot.Count; second++)
                {
                    if (GeneticDistance.Between(snapshot.GetGenomeAt(first), snapshot.GetGenomeAt(second)) <= threshold) Union(parents, first, second);
                }
            }
            var clusterIndices = new int[snapshot.Count];
            int count = 0;
            for (int index = 0; index < parents.Length; index++)
            {
                int root = Find(parents, index);
                int clusterIndex = 0;
                while (clusterIndex < index && Find(parents, clusterIndex) != root) clusterIndex++;
                if (clusterIndex == index) count++;
                clusterIndices[index] = clusterIndex;
            }

            return new GeneticClusters(count, clusterIndices);
        }

        public int GetClusterIndexAt(int sampleIndex)
        {
            if ((uint)sampleIndex >= (uint)_clusterIndices.Length) throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            return _clusterIndices[sampleIndex];
        }

        private static int Find(int[] parents, int index)
        {
            while (parents[index] != index) { parents[index] = parents[parents[index]]; index = parents[index]; }
            return index;
        }
        private static void Union(int[] parents, int first, int second)
        {
            first = Find(parents, first); second = Find(parents, second);
            if (first != second) parents[second] = first;
        }
    }
}
