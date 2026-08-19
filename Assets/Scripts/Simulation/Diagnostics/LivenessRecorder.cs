using System;

namespace LifeSimulation.Simulation.Diagnostics
{
    /// <summary>
    /// Mechanisms whose liveness is tracked at runtime. Entries are code paths, not genes:
    /// a gene with no consumption site anywhere (the <c>NeutralMarker</c> shape) has no probe here
    /// by construction, which is why <see cref="GeneLivenessAnalysis"/> and not this recorder is
    /// the authority on whether a gene reaches behavior.
    /// </summary>
    public enum LivenessProbe : byte
    {
        PlaceMemoryObservation = 0,
        PlaceMemoryDecay = 1,
        PlaceMemoryScoring = 2,
        FailedPlaceSearch = 3,
        CommitmentBonus = 4,
        ShouldAbandon = 5,
        PlantDefenseDeterrence = 6,
        LearnedResourceOutcome = 7,
        ThreatAvoidance = 8,
    }

    /// <summary>
    /// Records, during a normal run, whether each tracked mechanism actually executed and whether
    /// its output differed from the value that would have left the simulation unchanged.
    ///
    /// Why this exists: a static caller-search reports "nothing calls this", but cannot see code
    /// that runs every tick against permanently empty data, nor code that computes a real value
    /// nothing consumes. Both shapes have already cost this project a retracted root cause.
    ///
    /// <para><b>This type must never be read by simulation logic and must never contribute to
    /// <c>SimulationWorld.ComputeStateHash</c>.</b> It is a passive observer; if a decision or a
    /// state write ever depends on it, it stops measuring the simulation and starts changing it.
    /// <c>LivenessRecorderTests</c> pins the hash-independence.</para>
    /// </summary>
    public sealed class LivenessRecorder
    {
        private static readonly int ProbeCount = Enum.GetValues(typeof(LivenessProbe)).Length;

        private readonly long[] _reached;
        private readonly long[] _effective;

        public LivenessRecorder()
        {
            _reached = new long[ProbeCount];
            _effective = new long[ProbeCount];
        }

        /// <summary>Number of times the probe's code path executed at all.</summary>
        public long ReachedCount(LivenessProbe probe) => _reached[(int)probe];

        /// <summary>
        /// Number of times the probe's output differed from the no-op value, meaning it entered
        /// creature state or moved a decision score.
        /// </summary>
        public long EffectiveCount(LivenessProbe probe) => _effective[(int)probe];

        /// <summary>Executed, but never once produced an output that changed anything.</summary>
        public bool IsInertlyExecuting(LivenessProbe probe) =>
            _reached[(int)probe] > 0 && _effective[(int)probe] == 0;

        /// <summary>Never executed at all.</summary>
        public bool IsUnreached(LivenessProbe probe) => _reached[(int)probe] == 0;

        /// <summary>Executed and demonstrably altered simulation state or a decision score.</summary>
        public bool IsLive(LivenessProbe probe) => _effective[(int)probe] > 0;

        /// <summary>Call on entry to the instrumented code path, before it can bail out.</summary>
        public void RecordReached(LivenessProbe probe) => _reached[(int)probe]++;

        /// <summary>
        /// Record an output alongside the value that would have been a no-op. Counts as effective
        /// only when the two differ, so a branch that runs every tick and always returns its
        /// identity value is reported as inert rather than live.
        /// </summary>
        public void RecordOutput(LivenessProbe probe, float produced, float noOpValue)
        {
            _reached[(int)probe]++;
            if (produced != noOpValue)
            {
                _effective[(int)probe]++;
            }
        }

        /// <summary>Record a boolean-valued mechanism; <paramref name="tookEffect"/> means it acted.</summary>
        public void RecordOutcome(LivenessProbe probe, bool tookEffect)
        {
            _reached[(int)probe]++;
            if (tookEffect)
            {
                _effective[(int)probe]++;
            }
        }

        public void Reset()
        {
            Array.Clear(_reached, 0, _reached.Length);
            Array.Clear(_effective, 0, _effective.Length);
        }

        /// <summary>One line per probe: name, reached count, effective count, verdict.</summary>
        public string Report()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("probe                     |     reached |   effective | verdict");
            foreach (LivenessProbe probe in Enum.GetValues(typeof(LivenessProbe)))
            {
                string verdict = IsUnreached(probe)
                    ? "UNREACHED"
                    : IsInertlyExecuting(probe) ? "INERT (runs, changes nothing)" : "live";
                builder.AppendLine($"{probe,-25} | {ReachedCount(probe),11} | {EffectiveCount(probe),11} | {verdict}");
            }

            return builder.ToString();
        }
    }
}
