using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Experiments
{
    public enum DecisionInvalidationReason : byte
    {
        None = 0,
        PreviousResourceUnavailable = 1,
        HigherScoredIntent = 2,
        ExecutionTransition = 3,
    }

    public readonly struct DecisionTraceEntry
    {
        public DecisionTraceEntry(long tick, CreatureId creatureId, CreatureDecision previous, CreatureDecision winner, DecisionDiagnostics diagnostics, DecisionInvalidationReason invalidationReason)
        {
            Tick = tick;
            CreatureId = creatureId;
            Previous = previous;
            Winner = winner;
            FoodScore = diagnostics.FoodScore;
            WaterScore = diagnostics.WaterScore;
            FoodVisible = diagnostics.FoodVisible;
            WaterVisible = diagnostics.WaterVisible;
            InvalidationReason = invalidationReason;
        }

        public long Tick { get; }
        public CreatureId CreatureId { get; }
        public CreatureDecision Previous { get; }
        public CreatureDecision Winner { get; }
        public float FoodScore { get; }
        public float WaterScore { get; }
        public bool FoodVisible { get; }
        public bool WaterVisible { get; }
        public DecisionInvalidationReason InvalidationReason { get; }
        public bool Switched => Previous.Action != Winner.Action || Previous.TargetResourceIndex != Winner.TargetResourceIndex || !Previous.TargetCreatureId.Equals(Winner.TargetCreatureId);
    }

    public sealed class DecisionTraceRecorder
    {
        private readonly DecisionTraceEntry[] _entries;

        public DecisionTraceRecorder(CreatureId sampledCreatureId, int capacity)
        {
            SampledCreatureId = sampledCreatureId;
            _entries = new DecisionTraceEntry[capacity < 1 ? 1 : capacity];
        }

        public CreatureId SampledCreatureId { get; }
        public int Count { get; private set; }
        public bool Overflowed { get; private set; }

        public void Record(DecisionTraceEntry entry)
        {
            if (!entry.CreatureId.Equals(SampledCreatureId))
            {
                return;
            }

            if (Count >= _entries.Length)
            {
                Overflowed = true;
                return;
            }

            _entries[Count++] = entry;
        }

        public DecisionTraceEntry GetAt(int index)
        {
            return _entries[index];
        }
    }
}
