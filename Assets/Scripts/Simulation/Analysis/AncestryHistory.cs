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

        public void Record(SimulationEvent simulationEvent)
        {
            if (simulationEvent.Kind == SimulationEventKind.Birth)
            {
                _records[simulationEvent.Subject] = new AncestryRecord(simulationEvent.Tick, simulationEvent.FirstRelated, simulationEvent.SecondRelated, 0, DeathCause.None);
                return;
            }

            if (simulationEvent.Kind == SimulationEventKind.Death && _records.TryGetValue(simulationEvent.Subject, out AncestryRecord record))
            {
                _records[simulationEvent.Subject] = new AncestryRecord(record.BirthTick, record.FirstParent, record.SecondParent, simulationEvent.Tick, simulationEvent.DeathCause);
            }
        }

        public bool TryGet(CreatureId creatureId, out AncestryRecord record)
        {
            return _records.TryGetValue(creatureId, out record);
        }
    }
}
