using System.Collections.Generic;
using System.Text;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// Where any presentation component reports what a named piece of per-frame work cost.
    ///
    /// <para><b>Why it is static.</b> The work worth blaming for a stutter is spread across
    /// components that do not know about each other — the presenter rebuilds a heatmap, and
    /// <c>PlanetChunkedSurface</c> builds chunks from its own <c>LateUpdate</c>. Threading a
    /// reference between them to collect timings would be a larger change than the measurement is
    /// worth, and this is a diagnostic that writes to a text file.</para>
    ///
    /// <para><b>Totals as well as worst.</b> A single 20 ms call is a visible hitch; forty 5 ms calls
    /// in the same second are a worse problem that a worst-call figure alone would rank lower. Both
    /// are reported, with the count.</para>
    /// </summary>
    public static class PerformanceSections
    {
        private sealed class Section
        {
            public double WorstMilliseconds;
            public double TotalMilliseconds;
            public int Calls;
        }

        private static readonly Dictionary<string, Section> Sections = new Dictionary<string, Section>();

        public static void Record(string name, double milliseconds)
        {
            if (milliseconds < 0d) return;

            if (!Sections.TryGetValue(name, out Section section))
            {
                section = new Section();
                Sections[name] = section;
            }

            section.Calls++;
            section.TotalMilliseconds += milliseconds;
            if (milliseconds > section.WorstMilliseconds) section.WorstMilliseconds = milliseconds;
        }

        public static void Clear()
        {
            Sections.Clear();
        }

        public static string Describe()
        {
            if (Sections.Count == 0) return string.Empty;

            var builder = new StringBuilder();
            builder.AppendLine("  section              calls    worst ms    total ms");
            foreach (KeyValuePair<string, Section> pair in Sections)
            {
                builder.AppendLine(
                    "    " + pair.Key.PadRight(18)
                    + pair.Value.Calls.ToString().PadLeft(5)
                    + pair.Value.WorstMilliseconds.ToString("0.00").PadLeft(12)
                    + pair.Value.TotalMilliseconds.ToString("0.00").PadLeft(12));
            }

            return builder.ToString();
        }
    }
}
