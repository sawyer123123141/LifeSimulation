using System;
using LifeSimulation.Presentation;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The frame-time report, tested where it is still arithmetic.
    ///
    /// <para>Nothing rendered by this project has ever been observed running, so the first number
    /// anybody sees will come out of this. It should not be the first time it has been checked.</para>
    /// </summary>
    public sealed class FrameTimeLogTests
    {
        private static FrameTimeLog WithFrames(params double[] frames)
        {
            var log = new FrameTimeLog();
            foreach (double frame in frames) log.Add(frame);
            return log;
        }

        [Test]
        public void AnEmptyLogReportsNothingRatherThanZero()
        {
            // Zero milliseconds would read as an infinitely fast game.
            var log = new FrameTimeLog();

            Assert.That(log.Count, Is.EqualTo(0));
            Assert.That(double.IsNaN(log.Median), Is.True);
            Assert.That(double.IsNaN(log.MedianFramesPerSecond), Is.True);
        }

        [Test]
        public void ThePercentilesAreTheOrderedFrames()
        {
            FrameTimeLog log = WithFrames(10d, 50d, 20d, 40d, 30d);

            Assert.That(log.Percentile(0d), Is.EqualTo(10d));
            Assert.That(log.Median, Is.EqualTo(30d));
            Assert.That(log.Percentile(1d), Is.EqualTo(50d));
        }

        [Test]
        public void OneBadFrameMovesTheWorstButNotTheMedian()
        {
            // The whole reason for reporting percentiles: a mean would smear a stutter across the
            // report and a median alone would hide it.
            FrameTimeLog steady = WithFrames(16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d);
            FrameTimeLog stutter = WithFrames(16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d, 250d);

            Assert.That(stutter.Median, Is.EqualTo(steady.Median));
            Assert.That(stutter.Percentile(1d), Is.EqualTo(250d));
        }

        [Test]
        public void FramesPerSecondComesFromTheMedianFrame()
        {
            FrameTimeLog log = WithFrames(20d, 20d, 20d);

            Assert.That(log.MedianFramesPerSecond, Is.EqualTo(50d).Within(1e-9d));
        }

        [Test]
        public void TheRingKeepsTheMostRecentFramesOnly()
        {
            // A long session must not report the frame times of a minute ago as though they were now.
            var log = new FrameTimeLog(capacity: 4);
            foreach (double frame in new[] { 100d, 100d, 100d, 100d, 8d, 8d, 8d, 8d }) log.Add(frame);

            Assert.That(log.Count, Is.EqualTo(4));
            Assert.That(log.Median, Is.EqualTo(8d));
        }

        [Test]
        public void ClearingResetsIt()
        {
            FrameTimeLog log = WithFrames(10d, 20d);
            log.Clear();

            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void ANonsenseFrameIsRejectedRatherThanRecorded()
        {
            var log = new FrameTimeLog();

            Assert.Throws<ArgumentOutOfRangeException>(() => log.Add(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => log.Add(-1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => log.Add(double.PositiveInfinity));
        }

        [Test]
        public void TheReportNamesEveryFigureItPrints()
        {
            string report = WithFrames(16d, 16d, 33d).Describe("Y playtest", creatures: 42, renderers: 908, triangles: 232000, drawCalls: -1);

            Assert.That(report, Does.Contain("Y playtest"));
            Assert.That(report, Does.Contain("median"));
            Assert.That(report, Does.Contain("p99"));
            Assert.That(report, Does.Contain("renderers"));
            Assert.That(report, Does.Contain("908"));
            Assert.That(report, Does.Contain("232000"));

            // Outside the editor Unity does not expose draw calls, and a silent 0 would read as
            // "nothing is being drawn" rather than "this was not measurable".
            Assert.That(report, Does.Contain("unavailable outside the editor"));
        }
    }
}
