using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// Presenter-owned P5 analysis session. <see cref="SimulationWorld"/> never constructs or reads this type;
    /// the presenter advances it after stepping and before clearing the host event buffer.
    /// </summary>
    public sealed class P5HistoryPanelSession
    {
        public const int ObservationIntervalTicks = 300;
        public const float GeneticThreshold = .25f;
        public const int DefaultOutputCapacity = 64;

        private static readonly ClusterHistoryPolicy DefaultPolicy = new ClusterHistoryPolicy(
            minimumSupportedCurrentMembers: 1,
            minimumCurrentSupportFraction: .5f,
            minimumSupportingPreviousMembers: 1,
            minimumPreviousSupportFraction: .5f,
            maximumAncestorGenerations: 3,
            requiredSuccessorObservations: 1,
            requiredAbsentObservations: 2);

        private readonly SimulationWorld _world;
        private readonly AncestryHistory _ancestry;
        private readonly GeneticClusterHistory _history;
        private readonly ClusterHistoryEventBuffer _output;
        private readonly ClusterHistoryEvent[] _displayEvents;
        private int _copiedOutputEventCount;
        private bool _observationCadenceWasMissed;

        private P5HistoryPanelSession(SimulationWorld world, int outputCapacity)
        {
            _world = world;
            _ancestry = new AncestryHistory();
            _ancestry.RecordFounders(world.CurrentTick, world.Creatures);
            _output = new ClusterHistoryEventBuffer(outputCapacity);
            _history = new GeneticClusterHistory(DefaultPolicy, _output);
            _displayEvents = new ClusterHistoryEvent[outputCapacity];
            NextObservationTick = NextCadenceTickAfter(world.CurrentTick);
        }

        public int DisplayEventCount { get; private set; }
        public long NextObservationTick { get; private set; }
        public long AncestryCompleteThroughTick => _ancestry.CompleteThroughTick;
        public bool OutputOverflowed => _output.Overflowed;
        public int ObservationCount { get; private set; }
        public bool LastObservationWasSampled { get; private set; }
        public bool ObservationCadenceWasMissed => _observationCadenceWasMissed;
        public bool AncestryIsComplete => _ancestry.IsComplete;
        public int OutputCapacity => _output.Capacity;
        public ClusterHistoryPolicy Policy => _history.Policy;

        public string StatusText
        {
            get
            {
                if (OutputOverflowed) return "P5 analysis output overflowed; records were dropped.";
                if (ObservationCadenceWasMissed) return "P5 observation cadence was missed; call Advance after every simulation step.";
                if (!AncestryIsComplete) return "P5 ancestry evidence is incomplete.";
                if (ObservationCount == 0) return $"Awaiting first full P5 observation at tick {NextObservationTick}.";
                if (DisplayEventCount == 0) return "No P5 history evidence records have been produced.";
                return "P5 history evidence is available.";
            }
        }

        public static P5HistoryPanelSession CreateForWorld(SimulationWorld world)
        {
            return CreateForWorld(world, DefaultOutputCapacity);
        }

        /// <summary>Allows EditMode tests to exercise bounded-output reporting without changing the presenter default.</summary>
        public static P5HistoryPanelSession CreateForWorld(SimulationWorld world, int outputCapacity)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (outputCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(outputCapacity));
            return new P5HistoryPanelSession(world, outputCapacity);
        }

        public void Advance(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!ReferenceEquals(world, _world)) throw new ArgumentException("The session must advance its originating world.", nameof(world));

            _ancestry.RecordCompleteBatch(world.Events, world.CurrentTick);
            SkipMissedCadenceTicks(world.CurrentTick);
            if (world.CurrentTick == NextObservationTick)
            {
                PopulationGenomeSnapshot snapshot = PopulationGenomeSnapshot.Capture(world.CurrentTick, world.Creatures);
                GeneticClusterObservation observation = GeneticClusterObservation.Create(snapshot, GeneticThreshold);
                _history.Record(observation, _ancestry);
                ObservationCount++;
                LastObservationWasSampled = observation.IsSampled;
                NextObservationTick += ObservationIntervalTicks;
            }

            CopyOutputEvents();
        }

        public ClusterHistoryEvent GetEventAt(int index)
        {
            if ((uint)index >= (uint)DisplayEventCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _displayEvents[index];
        }

        private void CopyOutputEvents()
        {
            for (int index = _copiedOutputEventCount; index < _output.Count; index++)
            {
                _displayEvents[DisplayEventCount] = _output.GetAt(index);
                DisplayEventCount++;
            }

            _copiedOutputEventCount = _output.Count;
        }

        private void SkipMissedCadenceTicks(long currentTick)
        {
            if (NextObservationTick < currentTick)
            {
                _observationCadenceWasMissed = true;
            }

            while (NextObservationTick < currentTick)
            {
                NextObservationTick += ObservationIntervalTicks;
            }
        }

        private static long NextCadenceTickAfter(long currentTick)
        {
            long completedIntervals = currentTick / ObservationIntervalTicks;
            return (completedIntervals + 1) * ObservationIntervalTicks;
        }
    }
}
