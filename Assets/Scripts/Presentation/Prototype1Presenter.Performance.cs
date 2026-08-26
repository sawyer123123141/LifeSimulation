using System;
using System.IO;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// Writes what the renderer is actually costing to <c>Logs/performance.txt</c>, every few
    /// seconds, without anybody having to open a window.
    ///
    /// <para><b>Why.</b> Nothing rendered by this project has ever been observed running. The
    /// external terrain brief's top recommendation was "profile first, terrain is probably not the
    /// bottleneck", and it is right — but the first attempt at acting on it asked a human to read
    /// three figures out of the Profiler, which puts the measurement behind a GUI, leaves no
    /// artefact, and cannot be re-checked later. A file can be read, diffed and committed.</para>
    /// </summary>
    public sealed partial class Prototype1Presenter
    {
        /// <summary>Long enough to be past the first-frame spike and short enough to not need patience.</summary>
        private const float PerformanceReportInterval = 5f;

        private readonly FrameTimeLog _frameTimes = new FrameTimeLog();
        private float _sincePerformanceReport;
        private string _performancePath;

        /// <summary>
        /// Called once per frame from <c>Update</c>. Uses <c>unscaledDeltaTime</c> deliberately: this
        /// measures what the renderer costs, which the simulation speed multiplier must not distort.
        /// </summary>
        private void SamplePerformance()
        {
            _frameTimes.Add(Time.unscaledDeltaTime * 1000f);
            _sincePerformanceReport += Time.unscaledDeltaTime;
            if (_sincePerformanceReport < PerformanceReportInterval) return;

            _sincePerformanceReport = 0f;
            WritePerformanceReport();
            _frameTimes.Clear();
            _sectionWorstMilliseconds.Clear();
        }

        /// <summary>
        /// The worst time any named section has taken since the last report.
        ///
        /// <para>A frame-time percentile says a stutter happened; it cannot say what did it. The
        /// first real reading had a median of 3.02 ms and a worst frame of 197.52 ms, which is a
        /// hitch rather than a throughput problem, and hitches are always some periodic job running
        /// on the main thread. Naming the job is the difference between a fix and a guess.</para>
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<string, double> _sectionWorstMilliseconds =
            new System.Collections.Generic.Dictionary<string, double>();

        private void RecordSection(string section, double milliseconds)
        {
            if (milliseconds <= 0d) return;
            _sectionWorstMilliseconds.TryGetValue(section, out double worst);
            if (milliseconds > worst) _sectionWorstMilliseconds[section] = milliseconds;
        }

        private string DescribeSections()
        {
            if (_sectionWorstMilliseconds.Count == 0) return string.Empty;

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("  worst single call, by section");
            foreach (System.Collections.Generic.KeyValuePair<string, double> pair in _sectionWorstMilliseconds)
            {
                builder.AppendLine("    " + pair.Key.PadRight(16) + pair.Value.ToString("0.00") + " ms");
            }

            return builder.ToString();
        }

        private void WritePerformanceReport()
        {
            try
            {
                if (_performancePath == null)
                {
                    string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                    Directory.CreateDirectory(directory);
                    _performancePath = Path.Combine(directory, "performance.txt");
                }

                CountRenderedGeometry(out int renderers, out int triangles);
                File.WriteAllText(
                    _performancePath,
                    _frameTimes.Describe(
                        label: _scenarioId ?? "unknown scenario",
                        creatures: _world?.CreatureCount ?? 0,
                        renderers: renderers,
                        triangles: triangles,
                        drawCalls: DrawCalls())
                    + DescribeSections());
            }
            catch (IOException)
            {
                // A profiling readout must never be able to take the run down with it.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>
        /// Every enabled renderer in the scene and the triangles behind it.
        ///
        /// <para>This is the number the terrain review flagged and nobody has ever looked at: the
        /// chunked planet builds one <c>Mesh</c> and one <c>MeshRenderer</c> per chunk, and 908 of
        /// them were counted at ground level by reading the tree rather than by measuring.</para>
        /// </summary>
        private static void CountRenderedGeometry(out int renderers, out int triangles)
        {
            renderers = 0;
            triangles = 0;

            foreach (MeshRenderer renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;

                renderers++;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh != null) triangles += mesh.triangles.Length / 3;
            }
        }

        /// <summary>Draw calls, which Unity only exposes to the editor. Negative means unavailable.</summary>
        private static int DrawCalls()
        {
#if UNITY_EDITOR
            return UnityEditor.UnityStats.drawCalls;
#else
            return -1;
#endif
        }
    }
}
