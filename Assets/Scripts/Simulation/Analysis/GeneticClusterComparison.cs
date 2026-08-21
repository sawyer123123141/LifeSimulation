using System;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Analysis-only comparison that makes sample-induced cluster changes explicit.</summary>
    public sealed class GeneticClusterComparison
    {
        private GeneticClusterComparison(float threshold, int fullPopulationClusterCount, int sampleClusterCount)
        {
            Threshold = threshold;
            FullPopulationClusterCount = fullPopulationClusterCount;
            SampleClusterCount = sampleClusterCount;
        }

        public float Threshold { get; }
        public int FullPopulationClusterCount { get; }
        public int SampleClusterCount { get; }

        public static GeneticClusterComparison Analyze(PopulationGenomeSnapshot fullPopulation, PopulationGenomeSnapshot sample, float threshold)
        {
            if (fullPopulation == null) throw new ArgumentNullException(nameof(fullPopulation));
            if (sample == null) throw new ArgumentNullException(nameof(sample));
            if (fullPopulation.Tick != sample.Tick) throw new ArgumentException("Snapshots must represent the same tick.", nameof(sample));

            return new GeneticClusterComparison(
                threshold,
                GeneticClusters.From(fullPopulation, threshold).Count,
                GeneticClusters.From(sample, threshold).Count);
        }
    }
}
