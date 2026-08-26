using System;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// Frame times in, a readable report out. No Unity types, so it is tested headlessly.
    ///
    /// <para><b>Why this exists.</b> Nothing rendered by this project has ever been observed running —
    /// the camera, the planet view, the chunked surface and the tuning-drag fix are all verified by
    /// compile and offline capture only, and <b>908 chunks means 908 renderers, never profiled</b>.
    /// Every performance suggestion on record, including an external design brief's, is unranked
    /// until a number exists.</para>
    ///
    /// <para>Asking a human to read the Profiler window and report three figures was the wrong
    /// instrument: it puts the measurement behind a GUI, produces no artefact, and cannot be
    /// re-checked. This writes a file instead.</para>
    ///
    /// <para><b>Percentiles, not an average.</b> A mean frame time hides exactly the thing that
    /// matters — the occasional 80 ms frame that a viewer sees as a stutter. The 99th percentile and
    /// the worst frame are the numbers that decide whether something is smooth.</para>
    /// </summary>
    public sealed class FrameTimeLog
    {
        private readonly double[] _milliseconds;
        private int _count;
        private int _next;

        public FrameTimeLog(int capacity = 2048)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _milliseconds = new double[capacity];
        }

        /// <summary>Frames recorded, capped at the ring's capacity.</summary>
        public int Count => _count;

        public void Add(double frameMilliseconds)
        {
            if (frameMilliseconds < 0d || double.IsNaN(frameMilliseconds) || double.IsInfinity(frameMilliseconds))
            {
                throw new ArgumentOutOfRangeException(nameof(frameMilliseconds));
            }

            _milliseconds[_next] = frameMilliseconds;
            _next = (_next + 1) % _milliseconds.Length;
            if (_count < _milliseconds.Length) _count++;
        }

        public void Clear()
        {
            _count = 0;
            _next = 0;
        }

        /// <summary>Milliseconds at the given fraction, 0 being the fastest frame and 1 the worst.</summary>
        public double Percentile(double fraction)
        {
            if (_count == 0) return double.NaN;

            var sorted = new double[_count];
            Array.Copy(_milliseconds, sorted, _count);
            Array.Sort(sorted);

            double clamped = fraction < 0d ? 0d : fraction > 1d ? 1d : fraction;
            int rank = (int)Math.Round(clamped * (sorted.Length - 1));
            return sorted[rank];
        }

        public double Median => Percentile(0.5d);

        /// <summary>Frames per second implied by the median frame, which is the honest headline.</summary>
        public double MedianFramesPerSecond
        {
            get
            {
                double median = Median;
                return median <= 0d ? double.NaN : 1000d / median;
            }
        }

        /// <summary>
        /// The report written to disk. Deliberately one screen, in a fixed order, so two runs can be
        /// diffed by eye.
        /// </summary>
        public string Describe(string label, int creatures, int renderers, int triangles, int drawCalls)
        {
            return string.Join(
                Environment.NewLine,
                "performance — " + label,
                "  frames sampled   " + _count,
                "  median           " + Format(Median) + " ms   (" + Format(MedianFramesPerSecond) + " fps)",
                "  p90              " + Format(Percentile(0.90d)) + " ms",
                "  p99              " + Format(Percentile(0.99d)) + " ms",
                "  worst frame      " + Format(Percentile(1d)) + " ms",
                "  best frame       " + Format(Percentile(0d)) + " ms",
                "",
                "  creatures        " + creatures,
                "  renderers        " + renderers,
                "  triangles        " + triangles,
                "  draw calls       " + (drawCalls < 0 ? "unavailable outside the editor" : drawCalls.ToString()),
                "");
        }

        private static string Format(double value)
        {
            return double.IsNaN(value) ? "n/a" : value.ToString("0.00");
        }
    }
}
