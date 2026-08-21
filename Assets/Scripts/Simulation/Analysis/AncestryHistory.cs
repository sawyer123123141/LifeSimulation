using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    public readonly struct AncestryRecord
    {
        public AncestryRecord(long birthTick, CreatureId firstParent, CreatureId secondParent, long deathTick, DeathCause deathCause)
        {
            BirthTick = birthTick;
            FirstParent = firstParent;
            SecondParent = secondParent;
            DeathTick = deathTick;
            DeathCause = deathCause;
        }

        public long BirthTick { get; }
        public CreatureId FirstParent { get; }
        public CreatureId SecondParent { get; }
        public long DeathTick { get; }
        public DeathCause DeathCause { get; }
    }

    /// <summary>External event consumer for P5 ancestry analysis. It never affects simulation state.</summary>
    public sealed class AncestryHistory
    {
        private readonly Dictionary<CreatureId, AncestryRecord> _records = new Dictionary<CreatureId, AncestryRecord>();
        private readonly Dictionary<CreatureId, List<CreatureId>> _childrenByParent = new Dictionary<CreatureId, List<CreatureId>>();
        private bool _overflowed;

        public bool HasRecordedFounders { get; private set; }
        public bool IsComplete => HasRecordedFounders && !_overflowed;
        public long CompleteThroughTick { get; private set; } = -1;

        /// <summary>Call once from the host before draining events so the event history has explicit roots.</summary>
        public void RecordFounders(long tick, CreatureStore creatures)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            for (int index = 0; index < creatures.Count; index++)
            {
                CreatureId founderId = creatures.GetIdAt(index);
                if (!_records.ContainsKey(founderId))
                {
                    _records.Add(founderId, new AncestryRecord(tick, default, default, 0, DeathCause.None));
                }
            }

            HasRecordedFounders = true;
        }

        public void Record(SimulationEvent simulationEvent)
        {
            if (simulationEvent.Kind == SimulationEventKind.Birth)
            {
                if (_records.ContainsKey(simulationEvent.Subject))
                {
                    return;
                }

                _records[simulationEvent.Subject] = new AncestryRecord(simulationEvent.Tick, simulationEvent.FirstRelated, simulationEvent.SecondRelated, 0, DeathCause.None);
                AddChild(simulationEvent.FirstRelated, simulationEvent.Subject);
                AddChild(simulationEvent.SecondRelated, simulationEvent.Subject);
                return;
            }

            if (simulationEvent.Kind == SimulationEventKind.Death && _records.TryGetValue(simulationEvent.Subject, out AncestryRecord record))
            {
                _records[simulationEvent.Subject] = new AncestryRecord(record.BirthTick, record.FirstParent, record.SecondParent, simulationEvent.Tick, simulationEvent.DeathCause);
            }
        }

        public void Record(SimulationEventBuffer events)
        {
            for (int index = 0; index < events.Count; index++)
            {
                Record(events.GetAt(index));
            }
        }

        /// <summary>Records one host-drained event batch and advances only when its ancestry evidence is complete.</summary>
        public void RecordCompleteBatch(SimulationEventBuffer events, long throughTick)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (!HasRecordedFounders) throw new InvalidOperationException("Founders must be recorded before event batches.");
            if (throughTick < 0) throw new ArgumentOutOfRangeException(nameof(throughTick));
            if (throughTick < CompleteThroughTick) throw new ArgumentOutOfRangeException(nameof(throughTick));

            long previousEventTick = -1;
            for (int index = 0; index < events.Count; index++)
            {
                SimulationEvent simulationEvent = events.GetAt(index);
                if (simulationEvent.Tick < 0) throw new ArgumentOutOfRangeException(nameof(events));
                if (simulationEvent.Tick < CompleteThroughTick) throw new ArgumentException("An event occurs before the completed ancestry interval.", nameof(events));
                if (simulationEvent.Tick == CompleteThroughTick && throughTick != CompleteThroughTick) throw new ArgumentException("A watermark-boundary event can only be replayed without advancing completeness.", nameof(events));
                if (simulationEvent.Tick > throughTick) throw new ArgumentException("An event occurs after the requested completeness watermark.", nameof(events));
                if (simulationEvent.Tick < previousEventTick) throw new ArgumentException("Events must be ordered by nondecreasing tick.", nameof(events));
                previousEventTick = simulationEvent.Tick;
            }

            Record(events);
            if (events.Overflowed)
            {
                _overflowed = true;
            }

            if (_overflowed) return;
            CompleteThroughTick = throughTick;
        }

        public bool TryGet(CreatureId creatureId, out AncestryRecord record)
        {
            return _records.TryGetValue(creatureId, out record);
        }

        public int GetChildCount(CreatureId parentId)
        {
            if (!_childrenByParent.TryGetValue(parentId, out List<CreatureId>? children) || children == null) return 0;
            return children.Count;
        }

        public CreatureId GetChildAt(CreatureId parentId, int childIndex)
        {
            if (!_childrenByParent.TryGetValue(parentId, out List<CreatureId>? children) || children == null) throw new ArgumentOutOfRangeException(nameof(parentId));
            if ((uint)childIndex >= (uint)children.Count) throw new ArgumentOutOfRangeException(nameof(childIndex));
            return children[childIndex];
        }

        private void AddChild(CreatureId parentId, CreatureId childId)
        {
            if (parentId.Value == 0) return;
            if (!_childrenByParent.TryGetValue(parentId, out List<CreatureId>? children) || children == null)
            {
                children = new List<CreatureId>();
                _childrenByParent.Add(parentId, children);
            }

            children.Add(childId);
        }
    }
}
