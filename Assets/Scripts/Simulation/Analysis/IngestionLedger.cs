#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// Per-creature lifetime ingestion, joined to the life-history record and restricted to a cohort.
    /// The numerator of every diet-versus-fitness statement in this milestone.
    /// </summary>
    public sealed class IngestionLedger
    {
        private readonly struct Totals
        {
            public Totals(double resourceAmount, double grossEnergy, double storedEnergy, double surplusEnergy, double staleActionGrossEnergy, long feedingTicks, long staleActionFeedingTicks)
            {
                ResourceAmount = resourceAmount;
                GrossEnergy = grossEnergy;
                StoredEnergy = storedEnergy;
                SurplusEnergy = surplusEnergy;
                StaleActionGrossEnergy = staleActionGrossEnergy;
                FeedingTicks = feedingTicks;
                StaleActionFeedingTicks = staleActionFeedingTicks;
            }

            public double ResourceAmount { get; }
            public double GrossEnergy { get; }
            public double StoredEnergy { get; }
            public double SurplusEnergy { get; }
            public double StaleActionGrossEnergy { get; }
            public long FeedingTicks { get; }
            public long StaleActionFeedingTicks { get; }
        }

        private readonly List<CreatureId> _members = new List<CreatureId>();
        private readonly List<LifeHistoryRecord> _lives = new List<LifeHistoryRecord>();
        private readonly List<Totals> _plant = new List<Totals>();
        private readonly List<Totals> _carcass = new List<Totals>();

        private Totals _plantTotal;
        private Totals _carcassTotal;

        private IngestionLedger()
        {
        }

        public int Count => _members.Count;

        public static IngestionLedger Join(LifeHistoryLedger ledger, IngestionRecorder recorder, FitnessCohort cohort)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (recorder == null) throw new ArgumentNullException(nameof(recorder));
            if (cohort == null) throw new ArgumentNullException(nameof(cohort));
            if (!ledger.IsComplete)
            {
                throw new InvalidOperationException(
                    "Ingestion cannot be reported against an incomplete pedigree. A smaller number is not a safer number.");
            }

            var joined = new IngestionLedger();
            double plantAmount = 0d, plantGross = 0d, plantStored = 0d, plantSurplus = 0d, plantStaleGross = 0d;
            long plantTicks = 0, plantStaleTicks = 0;
            double carcassAmount = 0d, carcassGross = 0d, carcassStored = 0d, carcassSurplus = 0d, carcassStaleGross = 0d;
            long carcassTicks = 0, carcassStaleTicks = 0;

            for (int index = 0; index < cohort.Count; index++)
            {
                CreatureId creatureId = cohort.GetIdAt(index);
                if (!ledger.TryGet(creatureId, out LifeHistoryRecord life)) continue;

                Totals plant = TotalsFor(recorder, creatureId, ResourceKind.Food);
                Totals carcass = TotalsFor(recorder, creatureId, ResourceKind.Carcass);

                joined._members.Add(creatureId);
                joined._lives.Add(life);
                joined._plant.Add(plant);
                joined._carcass.Add(carcass);

                plantAmount += plant.ResourceAmount;
                plantGross += plant.GrossEnergy;
                plantStored += plant.StoredEnergy;
                plantSurplus += plant.SurplusEnergy;
                plantStaleGross += plant.StaleActionGrossEnergy;
                plantTicks += plant.FeedingTicks;
                plantStaleTicks += plant.StaleActionFeedingTicks;

                carcassAmount += carcass.ResourceAmount;
                carcassGross += carcass.GrossEnergy;
                carcassStored += carcass.StoredEnergy;
                carcassSurplus += carcass.SurplusEnergy;
                carcassStaleGross += carcass.StaleActionGrossEnergy;
                carcassTicks += carcass.FeedingTicks;
                carcassStaleTicks += carcass.StaleActionFeedingTicks;
            }

            joined._plantTotal = new Totals(plantAmount, plantGross, plantStored, plantSurplus, plantStaleGross, plantTicks, plantStaleTicks);
            joined._carcassTotal = new Totals(carcassAmount, carcassGross, carcassStored, carcassSurplus, carcassStaleGross, carcassTicks, carcassStaleTicks);
            return joined;
        }

        public CreatureId GetIdAt(int index) => _members[Bounded(index)];

        public LifeHistoryRecord GetLifeAt(int index) => _lives[Bounded(index)];

        public double ResourceAmountAt(int index, ResourceKind kind) => MemberTotals(index, kind).ResourceAmount;

        public double GrossEnergyAt(int index, ResourceKind kind) => MemberTotals(index, kind).GrossEnergy;

        public double StoredEnergyAt(int index, ResourceKind kind) => MemberTotals(index, kind).StoredEnergy;

        public double SurplusEnergyAt(int index, ResourceKind kind) => MemberTotals(index, kind).SurplusEnergy;

        public long FeedingTicksAt(int index, ResourceKind kind) => MemberTotals(index, kind).FeedingTicks;

        public long StaleActionFeedingTicksAt(int index, ResourceKind kind) => MemberTotals(index, kind).StaleActionFeedingTicks;

        public double TotalResourceAmount(ResourceKind kind) => CohortTotals(kind).ResourceAmount;

        public double TotalGrossEnergy(ResourceKind kind) => CohortTotals(kind).GrossEnergy;

        public double TotalStoredEnergy(ResourceKind kind) => CohortTotals(kind).StoredEnergy;

        public double TotalSurplusEnergy(ResourceKind kind) => CohortTotals(kind).SurplusEnergy;

        public long TotalFeedingTicks(ResourceKind kind) => CohortTotals(kind).FeedingTicks;

        /// <summary>Share of ingested energy the capacity clamp discarded. The old proxy could not see any of it.</summary>
        public double SurplusFraction(ResourceKind kind)
        {
            Totals totals = CohortTotals(kind);
            return totals.GrossEnergy <= 0d ? 0d : totals.SurplusEnergy / totals.GrossEnergy;
        }

        /// <summary>Share of ingested energy taken while the action still read <c>Seek*</c> - defect 4, measured.</summary>
        public double StaleActionGrossEnergyFraction(ResourceKind kind)
        {
            Totals totals = CohortTotals(kind);
            return totals.GrossEnergy <= 0d ? 0d : totals.StaleActionGrossEnergy / totals.GrossEnergy;
        }

        private static Totals TotalsFor(IngestionRecorder recorder, CreatureId creatureId, ResourceKind kind)
        {
            return new Totals(
                recorder.ResourceAmount(creatureId, kind),
                recorder.GrossEnergy(creatureId, kind),
                recorder.StoredEnergy(creatureId, kind),
                recorder.SurplusEnergy(creatureId, kind),
                recorder.StaleActionGrossEnergy(creatureId, kind),
                recorder.FeedingTicks(creatureId, kind),
                recorder.StaleActionFeedingTicks(creatureId, kind));
        }

        private Totals MemberTotals(int index, ResourceKind kind)
        {
            int bounded = Bounded(index);
            if (kind == ResourceKind.Food) return _plant[bounded];
            if (kind == ResourceKind.Carcass) return _carcass[bounded];
            throw new ArgumentOutOfRangeException(nameof(kind), "Only food and carcass are ingested.");
        }

        private Totals CohortTotals(ResourceKind kind)
        {
            if (kind == ResourceKind.Food) return _plantTotal;
            if (kind == ResourceKind.Carcass) return _carcassTotal;
            throw new ArgumentOutOfRangeException(nameof(kind), "Only food and carcass are ingested.");
        }

        private int Bounded(int index)
        {
            if ((uint)index >= (uint)_members.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return index;
        }
    }

    /// <summary>
    /// The retired instrument, kept so the number it produced can be reproduced and compared rather
    /// than argued about.
    ///
    /// <para>It attributes a positive energy delta to whatever the creature's action said it was
    /// doing, exactly as <c>tools/CreatureSweep/Intake.cs</c> does. That rule loses two things it
    /// cannot recover: a bite smaller than the half-second drain that lands on the same tick, and
    /// every bite taken before the creature's next decision tick converts <c>SeekFood</c> into
    /// <c>Eat</c>. Both losses are one-directional, so this can only under-count.</para>
    /// </summary>
    public sealed class EnergyDeltaProxy
    {
        private struct CreatureTrack
        {
            public double PlantGain;
            public double MeatGain;
            public long PlantTicks;
            public long MeatTicks;
            public float LastEnergy;
            public bool Seen;
        }

        private CreatureTrack[] _tracks = new CreatureTrack[64];

        public double TotalPlantGain { get; private set; }

        public double TotalMeatGain { get; private set; }

        public long TotalPlantTicks { get; private set; }

        public long TotalMeatTicks { get; private set; }

        /// <summary>Call once per tick after <c>Step</c>, before the event buffer is drained.</summary>
        public void Observe(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            for (int index = 0; index < world.CreatureCount; index++)
            {
                ObserveCreature(
                    world.GetCreatureIdAt(index),
                    world.GetCreatureDecisionAt(index).Action,
                    world.GetCreatureNeedsAt(index).Energy);
            }
        }

        public void ObserveCreature(CreatureId creatureId, CreatureAction action, float energy)
        {
            long idValue = creatureId.Value;
            if (idValue < 0 || idValue > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(creatureId));

            int slot = (int)idValue;
            EnsureCapacity(slot + 1);
            ref CreatureTrack track = ref _tracks[slot];

            if (track.Seen)
            {
                float delta = energy - track.LastEnergy;
                if (delta > 0f)
                {
                    if (action == CreatureAction.Eat)
                    {
                        track.PlantGain += delta;
                        TotalPlantGain += delta;
                    }
                    else if (action == CreatureAction.FeedCarcass)
                    {
                        track.MeatGain += delta;
                        TotalMeatGain += delta;
                    }
                }
            }

            if (action == CreatureAction.Eat)
            {
                track.PlantTicks++;
                TotalPlantTicks++;
            }
            else if (action == CreatureAction.FeedCarcass)
            {
                track.MeatTicks++;
                TotalMeatTicks++;
            }

            track.LastEnergy = energy;
            track.Seen = true;
        }

        public double PositiveDeltaEnergy(CreatureId creatureId, ResourceKind kind)
        {
            CreatureTrack track = Read(creatureId);
            return kind == ResourceKind.Carcass ? track.MeatGain : PlantOnly(kind, track.PlantGain);
        }

        public long ActionTicks(CreatureId creatureId, ResourceKind kind)
        {
            CreatureTrack track = Read(creatureId);
            return kind == ResourceKind.Carcass ? track.MeatTicks : (long)PlantOnly(kind, track.PlantTicks);
        }

        public double TotalPositiveDeltaEnergy(ResourceKind kind)
        {
            return kind == ResourceKind.Carcass ? TotalMeatGain : PlantOnly(kind, TotalPlantGain);
        }

        public long TotalActionTicks(ResourceKind kind)
        {
            return kind == ResourceKind.Carcass ? TotalMeatTicks : (long)PlantOnly(kind, TotalPlantTicks);
        }

        private static double PlantOnly(ResourceKind kind, double value)
        {
            if (kind != ResourceKind.Food) throw new ArgumentOutOfRangeException(nameof(kind), "Only food and carcass are ingested.");
            return value;
        }

        private CreatureTrack Read(CreatureId creatureId)
        {
            long idValue = creatureId.Value;
            if (idValue < 0 || idValue > int.MaxValue || idValue >= _tracks.Length) return default;
            return _tracks[(int)idValue];
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _tracks.Length) return;
            int capacity = _tracks.Length <= 0 ? 64 : _tracks.Length;
            while (capacity < required)
            {
                capacity *= 2;
            }

            Array.Resize(ref _tracks, capacity);
        }
    }
}
