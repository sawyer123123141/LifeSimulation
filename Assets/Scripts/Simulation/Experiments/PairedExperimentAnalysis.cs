using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Experiments
{
    public enum ExperimentMetric : byte
    {
        Population = 0,
        BirthCount = 1,
        DeathCount = 2,
        BodySize = 3,
        MovementSpeed = 4,
        MetabolicPace = 5,
        VisionRange = 6,
        WaterEfficiency = 7,
        FoodEfficiency = 8,
        TemperatureTolerance = 9,
        FertilityInvestment = 10,
        LifespanTendency = 11,
    }

    public readonly struct PairedExperimentSummary
    {
        public PairedExperimentSummary(
            int pairCount,
            float meanTreatmentMinusControl,
            int positiveDifferenceCount,
            int negativeDifferenceCount)
        {
            PairCount = pairCount;
            MeanTreatmentMinusControl = meanTreatmentMinusControl;
            PositiveDifferenceCount = positiveDifferenceCount;
            NegativeDifferenceCount = negativeDifferenceCount;
        }

        public int PairCount { get; }
        public float MeanTreatmentMinusControl { get; }
        public int PositiveDifferenceCount { get; }
        public int NegativeDifferenceCount { get; }
        public float DirectionConsistency => PairCount == 0
            ? 0f
            : Math.Max(PositiveDifferenceCount, NegativeDifferenceCount) / (float)PairCount;
    }

    public static class PairedExperimentAnalysis
    {
        public static PairedExperimentSummary Summarize(
            IReadOnlyList<ExperimentResult> control,
            IReadOnlyList<ExperimentResult> treatment,
            ExperimentMetric metric)
        {
            float[] differences = CalculateDifferences(control, treatment, metric);
            double totalDifference = 0d;
            int positiveCount = 0;
            int negativeCount = 0;
            for (int index = 0; index < differences.Length; index++)
            {
                float difference = differences[index];
                totalDifference += difference;
                if (difference > 0f)
                {
                    positiveCount++;
                }
                else if (difference < 0f)
                {
                    negativeCount++;
                }
            }

            return new PairedExperimentSummary(
                differences.Length,
                (float)(totalDifference / differences.Length),
                positiveCount,
                negativeCount);
        }

        public static float[] CalculateDifferences(
            IReadOnlyList<ExperimentResult> control,
            IReadOnlyList<ExperimentResult> treatment,
            ExperimentMetric metric)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            if (treatment == null)
            {
                throw new ArgumentNullException(nameof(treatment));
            }

            if (control.Count == 0 || control.Count != treatment.Count)
            {
                throw new ArgumentException("Paired experiment groups must have the same non-zero count.");
            }

            var differences = new float[control.Count];
            for (int index = 0; index < control.Count; index++)
            {
                if (control[index].WorldSeed != treatment[index].WorldSeed)
                {
                    throw new ArgumentException("Paired experiment groups must be ordered by the same world seed.");
                }

                differences[index] = GetMetric(treatment[index], metric) - GetMetric(control[index], metric);
            }

            return differences;
        }

        public static float CalculateStandardizedEffect(
            IReadOnlyList<ExperimentResult> control,
            IReadOnlyList<ExperimentResult> treatment,
            ExperimentMetric metric)
        {
            float[] differences = CalculateDifferences(control, treatment, metric);
            double sum = 0d;
            for (int index = 0; index < differences.Length; index++)
            {
                sum += differences[index];
            }

            double mean = sum / differences.Length;
            if (differences.Length == 1)
            {
                return mean == 0d ? 0f : (mean > 0d ? float.PositiveInfinity : float.NegativeInfinity);
            }

            double sumOfSquaredDeviation = 0d;
            for (int index = 0; index < differences.Length; index++)
            {
                double deviation = differences[index] - mean;
                sumOfSquaredDeviation += deviation * deviation;
            }

            double sampleDeviation = Math.Sqrt(sumOfSquaredDeviation / (differences.Length - 1));
            if (sampleDeviation == 0d)
            {
                return mean == 0d ? 0f : (mean > 0d ? float.PositiveInfinity : float.NegativeInfinity);
            }

            return (float)(mean / sampleDeviation);
        }

        private static float GetMetric(ExperimentResult result, ExperimentMetric metric)
        {
            switch (metric)
            {
                case ExperimentMetric.Population:
                    return result.FinalStatistics.Population;
                case ExperimentMetric.BirthCount:
                    return result.FinalStatistics.BirthCount;
                case ExperimentMetric.DeathCount:
                    return result.FinalStatistics.DeathCount;
                case ExperimentMetric.BodySize:
                    return result.FinalStatistics.MeanBodySizeGene;
                case ExperimentMetric.MovementSpeed:
                    return result.FinalStatistics.MeanMovementSpeedGene;
                case ExperimentMetric.MetabolicPace:
                    return result.FinalStatistics.MeanMetabolicPaceGene;
                case ExperimentMetric.VisionRange:
                    return result.FinalStatistics.MeanVisionRangeGene;
                case ExperimentMetric.WaterEfficiency:
                    return result.FinalStatistics.MeanWaterEfficiencyGene;
                case ExperimentMetric.FoodEfficiency:
                    return result.FinalStatistics.MeanFoodEfficiencyGene;
                case ExperimentMetric.TemperatureTolerance:
                    return result.FinalStatistics.MeanTemperatureToleranceGene;
                case ExperimentMetric.FertilityInvestment:
                    return result.FinalStatistics.MeanFertilityInvestmentGene;
                case ExperimentMetric.LifespanTendency:
                    return result.FinalStatistics.MeanLifespanTendencyGene;
                default:
                    throw new ArgumentOutOfRangeException(nameof(metric));
            }
        }
    }

    public readonly struct PairedBootstrapInterval
    {
        public PairedBootstrapInterval(float lowerBound, float upperBound)
        {
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        public float LowerBound { get; }
        public float UpperBound { get; }
        public bool ExcludesZero => LowerBound > 0f || UpperBound < 0f;
    }

    public static class PairedBootstrapAnalysis
    {
        public static PairedBootstrapInterval EstimateMeanDifferenceInterval(
            IReadOnlyList<ExperimentResult> control,
            IReadOnlyList<ExperimentResult> treatment,
            ExperimentMetric metric,
            int resampleCount,
            int randomSeed)
        {
            if (resampleCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resampleCount));
            }

            float[] differences = PairedExperimentAnalysis.CalculateDifferences(control, treatment, metric);
            var resampledMeans = new float[resampleCount];
            for (int resampleIndex = 0; resampleIndex < resampledMeans.Length; resampleIndex++)
            {
                double sum = 0d;
                for (int drawIndex = 0; drawIndex < differences.Length; drawIndex++)
                {
                    float draw = DeterministicRandom.Float01(
                        randomSeed,
                        RandomDomain.ExperimentSampling,
                        resampleIndex,
                        drawIndex,
                        differences.Length,
                        0);
                    int sampledIndex = Math.Min(differences.Length - 1, (int)(draw * differences.Length));
                    sum += differences[sampledIndex];
                }

                resampledMeans[resampleIndex] = (float)(sum / differences.Length);
            }

            Array.Sort(resampledMeans);
            int lowerIndex = (int)Math.Floor((resampledMeans.Length - 1) * 0.025d);
            int upperIndex = (int)Math.Ceiling((resampledMeans.Length - 1) * 0.975d);
            return new PairedBootstrapInterval(resampledMeans[lowerIndex], resampledMeans[upperIndex]);
        }
    }

    public readonly struct PairedEvolutionAssessment
    {
        public PairedEvolutionAssessment(
            bool intervalExcludesZero,
            bool meetsMinimumEffectSize,
            bool meetsDirectionConsistency,
            float standardizedEffect)
        {
            IntervalExcludesZero = intervalExcludesZero;
            MeetsMinimumEffectSize = meetsMinimumEffectSize;
            MeetsDirectionConsistency = meetsDirectionConsistency;
            StandardizedEffect = standardizedEffect;
        }

        public bool IntervalExcludesZero { get; }
        public bool MeetsMinimumEffectSize { get; }
        public bool MeetsDirectionConsistency { get; }
        public float StandardizedEffect { get; }
        public bool MeetsStatisticalCriterion => IntervalExcludesZero
            && MeetsMinimumEffectSize
            && MeetsDirectionConsistency;
        public bool RequiresMechanismEvidence => true;
    }

    public static class PairedEvolutionCriterion
    {
        public const float MinimumStandardizedEffect = 0.5f;
        public const float MinimumDirectionConsistency = 0.75f;

        public static PairedEvolutionAssessment Assess(
            PairedExperimentSummary summary,
            PairedBootstrapInterval interval,
            float standardizedEffect)
        {
            if (float.IsNaN(standardizedEffect))
            {
                throw new ArgumentOutOfRangeException(nameof(standardizedEffect));
            }

            return new PairedEvolutionAssessment(
                interval.ExcludesZero,
                Math.Abs(standardizedEffect) >= MinimumStandardizedEffect,
                summary.DirectionConsistency >= MinimumDirectionConsistency,
                standardizedEffect);
        }
    }
}
