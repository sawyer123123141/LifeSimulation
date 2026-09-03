#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// One creature's life as an outside observer can know it: the pedigree facts
    /// <see cref="AncestryHistory"/> already records, joined to the genome it carried.
    /// </summary>
    public readonly struct LifeHistoryRecord
    {
        public LifeHistoryRecord(CreatureId creatureId, AncestryRecord ancestry, bool hasGenome, Genome genome, int offspringCredited)
        {
            CreatureId = creatureId;
            BirthTick = ancestry.BirthTick;
            FirstParent = ancestry.FirstParent;
            SecondParent = ancestry.SecondParent;
            DeathTick = ancestry.DeathTick;
            DeathCause = ancestry.DeathCause;
            HasGenome = hasGenome;
            Genome = genome;
            OffspringCredited = offspringCredited;
        }

        public CreatureId CreatureId { get; }
        public long BirthTick { get; }
        public CreatureId FirstParent { get; }
        public CreatureId SecondParent { get; }

        /// <summary>Zero while the creature is alive - see <see cref="IsAlive"/>.</summary>
        public long DeathTick { get; }
        public DeathCause DeathCause { get; }

        /// <summary>False for a creature that was born and died between two <see cref="LifeHistoryLedger.Observe(CreatureStore)"/> calls.</summary>
        public bool HasGenome { get; }
        public Genome Genome { get; }

        /// <summary>
        /// Births this creature was named a parent of. Both parents are credited for the same birth,
        /// so a population at replacement averages about two, not one.
        /// </summary>
        public int OffspringCredited { get; }

        /// <summary>
        /// <c>deathTick == 0 &amp;&amp; deathCause == None</c> is <see cref="AncestryHistory"/>'s
        /// sentinel for "has not died". A death at tick 0 would be indistinguishable from it, and is
        /// unreachable because deaths only ever arrive as drained events and no event precedes the
        /// first <c>Step</c>.
        /// </summary>
        public bool IsAlive => DeathTick == 0 && DeathCause == DeathCause.None;

        public long LifespanTicks => IsAlive ? 0 : DeathTick - BirthTick;
    }

    /// <summary>
    /// A genome companion to <see cref="AncestryHistory"/>. It wraps rather than replaces the
    /// pedigree so the completeness watermark and permanent-overflow semantics stay in one place,
    /// and it adds the one thing that ledger does not carry: what each creature's genome was.
    ///
    /// <para>Genomes are captured by observation, so <see cref="Observe(CreatureStore)"/> must be
    /// called at least as often as creatures are born and die - once per tick in the sweep driver.
    /// A creature never observed alive keeps its pedigree record and reports
    /// <see cref="LifeHistoryRecord.HasGenome"/> false rather than a fabricated genome.</para>
    ///
    /// <para>Purely an outside observer: it reads the world between steps and writes nothing.</para>
    /// </summary>
    public sealed class LifeHistoryLedger
    {
        private readonly AncestryHistory _ancestry = new AncestryHistory();
        private readonly Dictionary<CreatureId, Genome> _genomes = new Dictionary<CreatureId, Genome>();
        private readonly List<CreatureId> _observationOrder = new List<CreatureId>();

        /// <summary>Mirrors <see cref="AncestryHistory.IsComplete"/>. Analyses must refuse to report when this is false.</summary>
        public bool IsComplete => _ancestry.IsComplete;

        public long CompleteThroughTick => _ancestry.CompleteThroughTick;

        /// <summary>Creatures whose genome has been captured, in first-observation order.</summary>
        public int Count => _observationOrder.Count;

        /// <summary>Call once before the first drain, or founders are absent from the pedigree entirely.</summary>
        public void RecordFounders(long tick, CreatureStore creatures)
        {
            _ancestry.RecordFounders(tick, creatures);
        }

        public void RecordCompleteBatch(SimulationEventBuffer events, long throughTick)
        {
            _ancestry.RecordCompleteBatch(events, throughTick);
        }

        /// <summary>Captures the genome of every creature not yet seen. Idempotent within a tick.</summary>
        public void Observe(CreatureStore creatures)
        {
            if (creatures == null) throw new ArgumentNullException(nameof(creatures));
            for (int index = 0; index < creatures.Count; index++)
            {
                CreatureId creatureId = creatures.GetIdAt(index);
                if (_genomes.ContainsKey(creatureId)) continue;
                _genomes.Add(creatureId, creatures.GetGenomeAt(index));
                _observationOrder.Add(creatureId);
            }
        }

        public void Observe(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            Observe(world.Creatures);
        }

        /// <summary>First-observation order, which is deterministic and does not depend on dictionary iteration.</summary>
        public CreatureId GetIdAt(int index)
        {
            if ((uint)index >= (uint)_observationOrder.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _observationOrder[index];
        }

        public bool TryGet(CreatureId creatureId, out LifeHistoryRecord record)
        {
            if (!_ancestry.TryGet(creatureId, out AncestryRecord ancestry))
            {
                record = default;
                return false;
            }

            bool hasGenome = _genomes.TryGetValue(creatureId, out Genome genome);
            record = new LifeHistoryRecord(creatureId, ancestry, hasGenome, genome, _ancestry.GetChildCount(creatureId));
            return true;
        }

        /// <summary>Read straight from the pedigree's child index; this type keeps no birth counter of its own.</summary>
        public int OffspringCredited(CreatureId parentId)
        {
            return _ancestry.GetChildCount(parentId);
        }
    }
}
