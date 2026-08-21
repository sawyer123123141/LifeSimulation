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

        public void Record(SimulationEvent simulationEvent)
        {
            if (simulationEvent.Kind == SimulationEventKind.Birth)
            {
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
