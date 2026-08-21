using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    public readonly struct LifecycleTimelineEntry
    {
        public LifecycleTimelineEntry(long tick, int birthCount, int deathCount, int starvationDeathCount)
        {
            Tick = tick; BirthCount = birthCount; DeathCount = deathCount; StarvationDeathCount = starvationDeathCount;
        }
        public long Tick { get; }
        public int BirthCount { get; }
        public int DeathCount { get; }
        public int StarvationDeathCount { get; }
    }

    /// <summary>Analysis-only literal lifecycle counts; it does not infer ecological meaning.</summary>
    public sealed class LifecycleTimeline
    {
        private readonly List<LifecycleTimelineEntry> _entries = new List<LifecycleTimelineEntry>();
        public int Count => _entries.Count;

        public void Record(SimulationEvent simulationEvent)
        {
            int index = _entries.Count - 1;
            LifecycleTimelineEntry entry = index >= 0 && _entries[index].Tick == simulationEvent.Tick
                ? _entries[index] : new LifecycleTimelineEntry(simulationEvent.Tick, 0, 0, 0);
            int births = entry.BirthCount + (simulationEvent.Kind == SimulationEventKind.Birth ? 1 : 0);
            int deaths = entry.DeathCount + (simulationEvent.Kind == SimulationEventKind.Death ? 1 : 0);
            int starvation = entry.StarvationDeathCount + (simulationEvent.Kind == SimulationEventKind.Death && simulationEvent.DeathCause == DeathCause.Starvation ? 1 : 0);
            var updated = new LifecycleTimelineEntry(entry.Tick, births, deaths, starvation);
            if (index >= 0 && _entries[index].Tick == simulationEvent.Tick) _entries[index] = updated; else _entries.Add(updated);
        }

        public LifecycleTimelineEntry GetAt(int index) => _entries[index];
    }
}
