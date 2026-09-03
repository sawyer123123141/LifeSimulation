using System;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PerWorldRelationshipTests
    {
        [Test]
        public void TwoWorldsWithNegativeSlopesThatPoolPositiveAreReportedAsNegative()
        {
            // Simpson's paradox, built deliberately: within each world the relationship is negative,
            // and the pooled cloud slopes the other way because the worlds sit at different levels.
            var first = PerWorldRelationship.ForWorld(
                worldSeed: 1,
                predictor: new double[] { 0.0, 0.1, 0.2, 0.3 },
                response: new double[] { 3.0, 2.9, 2.8, 2.7 });
            var second = PerWorldRelationship.ForWorld(
                worldSeed: 2,
                predictor: new double[] { 0.7, 0.8, 0.9, 1.0 },
                response: new double[] { 9.0, 8.9, 8.8, 8.7 });

            var summary = PerWorldRelationshipSummary.Across(new[] { first, second }, minimumCohortSize: 2, bootstrapResampleCount: 512, bootstrapSeed: 7);

            Assert.That(first.Correlation, Is.LessThan(0d));
            Assert.That(second.Correlation, Is.LessThan(0d));
            Assert.That(summary.MeanCorrelation, Is.LessThan(0d));
            Assert.That(summary.NegativeWorldCount, Is.EqualTo(2));
            Assert.That(summary.PositiveWorldCount, Is.EqualTo(0));
            Assert.That(summary.PooledCorrelation, Is.GreaterThan(0d));
            Assert.That(summary.PooledCorrelationIsPseudoReplicated, Is.True);
        }

        [Test]
        public void TheCrossSeedSummaryReportsSignCountsAndABootstrapInterval()
        {
            PerWorldRelationship[] worlds =
            {
                Linear(1, slope: 1d),
                Linear(2, slope: 1d),
                Linear(3, slope: 1d),
                Linear(4, slope: -1d),
            };

            var summary = PerWorldRelationshipSummary.Across(worlds, minimumCohortSize: 2, bootstrapResampleCount: 1_024, bootstrapSeed: 11);

            Assert.That(summary.IncludedWorldCount, Is.EqualTo(4));
            Assert.That(summary.PositiveWorldCount, Is.EqualTo(3));
            Assert.That(summary.NegativeWorldCount, Is.EqualTo(1));
            Assert.That(summary.MeanCorrelation, Is.EqualTo(.5d).Within(1e-6));
            Assert.That(summary.CorrelationInterval.LowerBound, Is.LessThanOrEqualTo(summary.CorrelationInterval.UpperBound));
        }

        [Test]
        public void TheBootstrapIsDeterministicForTheSameSeed()
        {
            PerWorldRelationship[] worlds = { Linear(1, 1d), Linear(2, -1d), Linear(3, 1d) };

            PairedBootstrapInterval first = PerWorldRelationshipSummary.Across(worlds, 2, 512, 5).CorrelationInterval;
            PairedBootstrapInterval second = PerWorldRelationshipSummary.Across(worlds, 2, 512, 5).CorrelationInterval;

            Assert.That(first.LowerBound, Is.EqualTo(second.LowerBound));
            Assert.That(first.UpperBound, Is.EqualTo(second.UpperBound));
        }

        [Test]
        public void WorldsBelowTheMinimumCohortSizeAreReportedRatherThanSilentlyDropped()
        {
            PerWorldRelationship[] worlds =
            {
                Linear(1, 1d),
                PerWorldRelationship.ForWorld(2, new double[] { 0.1, 0.2 }, new double[] { 1.0, 2.0 }),
            };

            var summary = PerWorldRelationshipSummary.Across(worlds, minimumCohortSize: 4, bootstrapResampleCount: 256, bootstrapSeed: 3);

            Assert.That(summary.IncludedWorldCount, Is.EqualTo(1));
            Assert.That(summary.ExcludedForSmallCohortCount, Is.EqualTo(1));
        }

        [Test]
        public void DietBinMeansAreComputedPerWorld()
        {
            var world = PerWorldRelationship.ForWorld(
                worldSeed: 1,
                predictor: new double[] { 0.05, 0.15, 0.85, 0.95 },
                response: new double[] { 1.0, 3.0, 10.0, 20.0 },
                binCount: 5);

            Assert.That(world.BinCount, Is.EqualTo(5));
            Assert.That(world.BinMemberCount(0), Is.EqualTo(2));
            Assert.That(world.BinMean(0), Is.EqualTo(2d).Within(1e-6));
            Assert.That(world.BinMemberCount(4), Is.EqualTo(2));
            Assert.That(world.BinMean(4), Is.EqualTo(15d).Within(1e-6));
            Assert.That(world.BinMemberCount(2), Is.EqualTo(0));
            Assert.That(double.IsNaN(world.BinMean(2)), Is.True, "An empty bin has no mean, and reporting zero would read as a measurement.");
        }

        [Test]
        public void AWorldWithNoVarianceInThePredictorHasNoCorrelationRatherThanAFabricatedOne()
        {
            var world = PerWorldRelationship.ForWorld(1, new double[] { .5, .5, .5 }, new double[] { 1.0, 2.0, 3.0 });

            Assert.That(world.Correlation, Is.EqualTo(0d));
            Assert.That(world.CohortSize, Is.EqualTo(3));
        }

        [Test]
        public void MismatchedPredictorAndResponseLengthsAreRefused()
        {
            Assert.Throws<ArgumentException>(() => PerWorldRelationship.ForWorld(1, new double[] { 1, 2 }, new double[] { 1 }));
        }

        private static PerWorldRelationship Linear(int worldSeed, double slope)
        {
            var predictor = new double[8];
            var response = new double[8];
            for (int index = 0; index < predictor.Length; index++)
            {
                predictor[index] = index / 8d;
                response[index] = slope * predictor[index];
            }

            return PerWorldRelationship.ForWorld(worldSeed, predictor, response);
        }
    }
}
