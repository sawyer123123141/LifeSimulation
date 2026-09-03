#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// One world's relationship between a trait and a fitness quantity, plus that world's binned
    /// means. The world is the replicate; a statistic pooled across worlds is not one observation of
    /// anything.
    /// </summary>
    public sealed class PerWorldRelationship
    {
        private const int DefaultBinCount = 5;

        private readonly double[] _predictor;
        private readonly double[] _response;
        private readonly double[] _binTotal;
        private readonly int[] _binCount;

        private PerWorldRelationship(int worldSeed, double[] predictor, double[] response, double correlation, double[] binTotal, int[] binCount)
        {
            WorldSeed = worldSeed;
            _predictor = predictor;
            _response = response;
            Correlation = correlation;
            _binTotal = binTotal;
            _binCount = binCount;
        }

        public int WorldSeed { get; }

        public int CohortSize => _predictor.Length;

        /// <summary>Pearson correlation within this world. Zero when the predictor does not vary.</summary>
        public double Correlation { get; }

        public int BinCount => _binCount.Length;

        public static PerWorldRelationship ForWorld(int worldSeed, IReadOnlyList<double> predictor, IReadOnlyList<double> response, int binCount = DefaultBinCount)
        {
            if (predictor == null) throw new ArgumentNullException(nameof(predictor));
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (predictor.Count != response.Count) throw new ArgumentException("A predictor and a response must have the same length.", nameof(response));
            if (binCount <= 0) throw new ArgumentOutOfRangeException(nameof(binCount));

            var predictorValues = new double[predictor.Count];
            var responseValues = new double[response.Count];
            var binTotal = new double[binCount];
            var binMembers = new int[binCount];

            for (int index = 0; index < predictorValues.Length; index++)
            {
                predictorValues[index] = predictor[index];
                responseValues[index] = response[index];

                int bin = BinFor(predictorValues[index], binCount);
                binTotal[bin] += responseValues[index];
                binMembers[bin]++;
            }

            return new PerWorldRelationship(
                worldSeed,
                predictorValues,
                responseValues,
                CorrelationOf(predictorValues, responseValues),
                binTotal,
                binMembers);
        }

        public int BinMemberCount(int bin)
        {
            if ((uint)bin >= (uint)_binCount.Length) throw new ArgumentOutOfRangeException(nameof(bin));
            return _binCount[bin];
        }

        /// <summary>NaN for an empty bin. Reporting zero there would read as a measurement.</summary>
        public double BinMean(int bin)
        {
            if ((uint)bin >= (uint)_binCount.Length) throw new ArgumentOutOfRangeException(nameof(bin));
            if (_binCount[bin] == 0) return double.NaN;
            return _binTotal[bin] / _binCount[bin];
        }

        internal double PredictorAt(int index) => _predictor[index];

        internal double ResponseAt(int index) => _response[index];

        internal static int BinFor(double predictor, int binCount)
        {
            int bin = (int)(predictor * binCount);
            if (bin < 0) return 0;
            if (bin >= binCount) return binCount - 1;
            return bin;
        }

        internal static double CorrelationOf(double[] first, double[] second)
        {
            int count = first.Length;
            if (count < 2) return 0d;

            double firstMean = 0d;
            double secondMean = 0d;
            for (int index = 0; index < count; index++)
            {
                firstMean += first[index];
                secondMean += second[index];
            }

            firstMean /= count;
            secondMean /= count;

            double covariance = 0d;
            double firstVariance = 0d;
            double secondVariance = 0d;
            for (int index = 0; index < count; index++)
            {
                double a = first[index] - firstMean;
                double b = second[index] - secondMean;
                covariance += a * b;
                firstVariance += a * a;
                secondVariance += b * b;
            }

            double denominator = Math.Sqrt(firstVariance * secondVariance);
            return denominator <= 0d ? 0d : covariance / denominator;
        }
    }

    /// <summary>
    /// A summary <b>of the per-world values</b>, which is the honest cross-seed statement: the mean
    /// per-world correlation, how many worlds fall on each side of zero, and a bootstrap interval on
    /// that mean.
    ///
    /// <para>The pooled figure is computed and reported beside them, and is always labelled
    /// pseudo-replicated. Pooling creatures across worlds treats individuals as independent
    /// replicates when the world is the replicate, and it can carry the opposite sign to every world
    /// in it.</para>
    ///
    /// <para>The bootstrap mirrors <see cref="PairedBootstrapAnalysis.EstimateMeanDifferenceInterval"/>
    /// exactly - same resampling rule, same <see cref="RandomDomain.ExperimentSampling"/> draws, same
    /// percentile indices - and returns its <see cref="PairedBootstrapInterval"/>. It is written here
    /// rather than called because that method takes <c>ExperimentResult</c> lists and a per-world
    /// correlation is not one. No new statistical method is introduced.</para>
    /// </summary>
    public sealed class PerWorldRelationshipSummary
    {
        private PerWorldRelationshipSummary(
            int includedWorldCount,
            int excludedForSmallCohortCount,
            double meanCorrelation,
            int positiveWorldCount,
            int negativeWorldCount,
            PairedBootstrapInterval correlationInterval,
            double pooledCorrelation,
            int pooledObservationCount)
        {
            IncludedWorldCount = includedWorldCount;
            ExcludedForSmallCohortCount = excludedForSmallCohortCount;
            MeanCorrelation = meanCorrelation;
            PositiveWorldCount = positiveWorldCount;
            NegativeWorldCount = negativeWorldCount;
            CorrelationInterval = correlationInterval;
            PooledCorrelation = pooledCorrelation;
            PooledObservationCount = pooledObservationCount;
        }

        public int IncludedWorldCount { get; }

        /// <summary>Worlds left out for having too small a cohort. Reported, never dropped in silence.</summary>
        public int ExcludedForSmallCohortCount { get; }

        public double MeanCorrelation { get; }

        public int PositiveWorldCount { get; }

        public int NegativeWorldCount { get; }

        public PairedBootstrapInterval CorrelationInterval { get; }

        /// <summary>Every creature from every world in one cloud. Always pseudo-replicated.</summary>
        public double PooledCorrelation { get; }

        public int PooledObservationCount { get; }

        public bool PooledCorrelationIsPseudoReplicated => true;

        public static PerWorldRelationshipSummary Across(
            IReadOnlyList<PerWorldRelationship> worlds,
            int minimumCohortSize,
            int bootstrapResampleCount,
            int bootstrapSeed)
        {
            if (worlds == null) throw new ArgumentNullException(nameof(worlds));
            if (minimumCohortSize < 2) throw new ArgumentOutOfRangeException(nameof(minimumCohortSize));
            if (bootstrapResampleCount <= 0) throw new ArgumentOutOfRangeException(nameof(bootstrapResampleCount));

            var correlations = new List<double>();
            var pooledPredictor = new List<double>();
            var pooledResponse = new List<double>();
            int excluded = 0;
            int positive = 0;
            int negative = 0;

            for (int worldIndex = 0; worldIndex < worlds.Count; worldIndex++)
            {
                PerWorldRelationship world = worlds[worldIndex];
                if (world == null) throw new ArgumentNullException(nameof(worlds));
                if (world.CohortSize < minimumCohortSize)
                {
                    excluded++;
                    continue;
                }

                correlations.Add(world.Correlation);
                if (world.Correlation > 0d) positive++;
                else if (world.Correlation < 0d) negative++;

                for (int index = 0; index < world.CohortSize; index++)
                {
                    pooledPredictor.Add(world.PredictorAt(index));
                    pooledResponse.Add(world.ResponseAt(index));
                }
            }

            double meanCorrelation = 0d;
            for (int index = 0; index < correlations.Count; index++)
            {
                meanCorrelation += correlations[index];
            }

            meanCorrelation = correlations.Count == 0 ? 0d : meanCorrelation / correlations.Count;

            return new PerWorldRelationshipSummary(
                correlations.Count,
                excluded,
                meanCorrelation,
                positive,
                negative,
                BootstrapMeanInterval(correlations, bootstrapResampleCount, bootstrapSeed),
                PerWorldRelationship.CorrelationOf(pooledPredictor.ToArray(), pooledResponse.ToArray()),
                pooledPredictor.Count);
        }

        private static PairedBootstrapInterval BootstrapMeanInterval(List<double> values, int resampleCount, int randomSeed)
        {
            if (values.Count == 0) return new PairedBootstrapInterval(0f, 0f);

            var resampledMeans = new float[resampleCount];
            for (int resampleIndex = 0; resampleIndex < resampledMeans.Length; resampleIndex++)
            {
                double sum = 0d;
                for (int drawIndex = 0; drawIndex < values.Count; drawIndex++)
                {
                    float draw = DeterministicRandom.Float01(
                        randomSeed,
                        RandomDomain.ExperimentSampling,
                        resampleIndex,
                        drawIndex,
                        values.Count,
                        0);
                    int sampledIndex = Math.Min(values.Count - 1, (int)(draw * values.Count));
                    sum += values[sampledIndex];
                }

                resampledMeans[resampleIndex] = (float)(sum / values.Count);
            }

            Array.Sort(resampledMeans);
            int lowerIndex = (int)Math.Floor((resampledMeans.Length - 1) * 0.025d);
            int upperIndex = (int)Math.Ceiling((resampledMeans.Length - 1) * 0.975d);
            return new PairedBootstrapInterval(resampledMeans[lowerIndex], resampledMeans[upperIndex]);
        }
    }
}
