#nullable enable annotations

using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Diagnostics
{
    /// <summary>
    /// An optional, passive sink for what creatures actually ingested, written at the allocation site
    /// and read by nobody inside the simulation.
    ///
    /// <para><b>Why it has to live here rather than outside the world.</b> Every energy-delta proxy is
    /// unrepairable in principle, for two reasons the tick schedule creates. Drains land in a
    /// half-second lump once per ten ticks while ingestion happens every tick, so on a needs tick the
    /// drain usually exceeds the bite and the whole tick's ingestion disappears into a negative delta.
    /// And ingestion fires under <c>SeekFood</c> / <c>SeekCarcass</c> as well as <c>Eat</c> /
    /// <c>FeedCarcass</c>, so every tick between entering the interaction radius and the creature's
    /// next decision tick is invisible to an observer keyed on the action. The capacity clamp hides a
    /// third quantity outright: a bite taken while nearly full stores nothing and leaves no trace.</para>
    ///
    /// <para>Follows the <c>LivenessRecorder</c> precedent exactly: null by default on
    /// <see cref="SimulationWorld.Recorder"/>, never read by simulation logic, absent from every hash
    /// by construction, and pinned hash-inert by a committed test.</para>
    ///
    /// <para>Accumulators are keyed by <c>CreatureId.Value</c>, never by creature index, because
    /// <c>CreatureStore.Remove</c> swap-removes and indices are not stable across a death.</para>
    /// </summary>
    public sealed class IngestionRecorder
    {
        private struct SourceTotals
        {
            public double ResourceAmount;
            public double GrossEnergy;
            public double StoredEnergy;
            public double SurplusEnergy;
            public double StaleActionGrossEnergy;
            public long FeedingTicks;
            public long StaleActionFeedingTicks;
        }

        private SourceTotals[] _plant = new SourceTotals[64];
        private SourceTotals[] _carcass = new SourceTotals[64];

        private SourceTotals _plantTotal;
        private SourceTotals _carcassTotal;

        /// <summary>One past the highest <c>CreatureId.Value</c> that has ever fed.</summary>
        public long HighestRecordedCreatureIdValue { get; private set; }

        /// <summary>
        /// <paramref name="underStaleSeekAction"/> marks a bite taken while the creature's decision
        /// still read <c>SeekFood</c> or <c>SeekCarcass</c> - the ticks the retired delta proxy could
        /// not see.
        /// </summary>
        public void Record(
            CreatureId creatureId,
            ResourceKind kind,
            bool underStaleSeekAction,
            float resourceAmount,
            float grossEnergy,
            float storedEnergy)
        {
            long idValue = creatureId.Value;
            if (idValue < 0) throw new ArgumentOutOfRangeException(nameof(creatureId));
            if (idValue > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(creatureId));

            int slot = (int)idValue;
            SourceTotals[] totals = ArrayFor(kind);
            EnsureCapacity(kind, slot + 1);
            totals = ArrayFor(kind);

            // Non-negative by definition: ConsumeFood clamps, so stored can never exceed gross. The
            // Max only absorbs float representation error.
            double surplus = Math.Max(0d, grossEnergy - (double)storedEnergy);

            Accumulate(ref totals[slot], underStaleSeekAction, resourceAmount, grossEnergy, storedEnergy, surplus);
            Accumulate(ref TotalFor(kind), underStaleSeekAction, resourceAmount, grossEnergy, storedEnergy, surplus);

            if (idValue > HighestRecordedCreatureIdValue) HighestRecordedCreatureIdValue = idValue;
        }

        public double ResourceAmount(CreatureId creatureId, ResourceKind kind) => Read(creatureId, kind).ResourceAmount;

        public double GrossEnergy(CreatureId creatureId, ResourceKind kind) => Read(creatureId, kind).GrossEnergy;

        public double StoredEnergy(CreatureId creatureId, ResourceKind kind) => Read(creatureId, kind).StoredEnergy;

        /// <summary>Gross energy the capacity clamp discarded. The quantity the old proxy could not see at all.</summary>
        public double SurplusEnergy(CreatureId creatureId, ResourceKind kind) => Read(creatureId, kind).SurplusEnergy;

        public long FeedingTicks(CreatureId creatureId, ResourceKind kind) => Read(creatureId, kind).FeedingTicks;

        public long StaleActionFeedingTicks(CreatureId creatureId, ResourceKind kind) => Read(creatureId, kind).StaleActionFeedingTicks;

        public double StaleActionGrossEnergy(CreatureId creatureId, ResourceKind kind) => Read(creatureId, kind).StaleActionGrossEnergy;

        public double TotalResourceAmount(ResourceKind kind) => TotalFor(kind).ResourceAmount;

        public double TotalGrossEnergy(ResourceKind kind) => TotalFor(kind).GrossEnergy;

        public double TotalStoredEnergy(ResourceKind kind) => TotalFor(kind).StoredEnergy;

        public double TotalSurplusEnergy(ResourceKind kind) => TotalFor(kind).SurplusEnergy;

        public long TotalFeedingTicks(ResourceKind kind) => TotalFor(kind).FeedingTicks;

        public long TotalStaleActionFeedingTicks(ResourceKind kind) => TotalFor(kind).StaleActionFeedingTicks;

        public double TotalStaleActionGrossEnergy(ResourceKind kind) => TotalFor(kind).StaleActionGrossEnergy;

        private static void Accumulate(
            ref SourceTotals totals,
            bool underStaleSeekAction,
            float resourceAmount,
            float grossEnergy,
            float storedEnergy,
            double surplus)
        {
            totals.ResourceAmount += resourceAmount;
            totals.GrossEnergy += grossEnergy;
            totals.StoredEnergy += storedEnergy;
            totals.SurplusEnergy += surplus;
            totals.FeedingTicks++;
            if (!underStaleSeekAction) return;
            totals.StaleActionFeedingTicks++;
            totals.StaleActionGrossEnergy += grossEnergy;
        }

        private SourceTotals Read(CreatureId creatureId, ResourceKind kind)
        {
            long idValue = creatureId.Value;
            if (idValue < 0 || idValue > int.MaxValue) return default;
            SourceTotals[] totals = ArrayFor(kind);
            int slot = (int)idValue;
            if (slot >= totals.Length) return default;
            return totals[slot];
        }

        private SourceTotals[] ArrayFor(ResourceKind kind)
        {
            if (kind == ResourceKind.Food) return _plant;
            if (kind == ResourceKind.Carcass) return _carcass;
            throw new ArgumentOutOfRangeException(nameof(kind), "Only food and carcass are ingested.");
        }

        private ref SourceTotals TotalFor(ResourceKind kind)
        {
            if (kind == ResourceKind.Food) return ref _plantTotal;
            if (kind == ResourceKind.Carcass) return ref _carcassTotal;
            throw new ArgumentOutOfRangeException(nameof(kind), "Only food and carcass are ingested.");
        }

        /// <summary>Amortised growth, in the manner of the store's own <c>EnsureCapacity</c>.</summary>
        private void EnsureCapacity(ResourceKind kind, int required)
        {
            if (kind == ResourceKind.Food)
            {
                if (required <= _plant.Length) return;
                Array.Resize(ref _plant, NextCapacity(_plant.Length, required));
                return;
            }

            if (required <= _carcass.Length) return;
            Array.Resize(ref _carcass, NextCapacity(_carcass.Length, required));
        }

        private static int NextCapacity(int current, int required)
        {
            int capacity = current <= 0 ? 64 : current;
            while (capacity < required)
            {
                capacity *= 2;
            }

            return capacity;
        }
    }
}
