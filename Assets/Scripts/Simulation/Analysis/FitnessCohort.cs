#nullable enable annotations

using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    public enum FitnessCohortKind : byte
    {
        /// <summary>Born early enough that no genotype could still have been alive at the run's end.</summary>
        CompleteLife = 0,

        /// <summary>Born early enough that every one of its offspring had time to reach adult age as well.</summary>
        OffspringToAdulthood = 1
    }

    /// <summary>
    /// Which creatures a lifetime-fitness statistic may be computed over, and why the rest were left
    /// out.
    ///
    /// <para>The censoring horizon is a property of the <b>phenotype map</b>, not of the observed
    /// population: it is the longest life any genotype reachable by mutation could have, derived from
    /// <see cref="Phenotype.MaximumAgeSeconds"/> at the top of <see cref="Genome.LifespanTendency"/>'s
    /// clamp. A creature is therefore admitted or excluded on its <b>birth tick alone</b> and its own
    /// lifespan genotype is never consulted, which is what keeps the filter from selecting on the very
    /// trait a fitness statistic is about.</para>
    ///
    /// <para>No survival-analysis machinery is needed or wanted here. A longer run plus a
    /// genotype-independent horizon removes the censoring rather than modelling it.</para>
    /// </summary>
    public sealed class FitnessCohort
    {
        private readonly List<CreatureId> _members;
        private readonly HashSet<CreatureId> _membership;

        private FitnessCohort(
            List<CreatureId> members,
            HashSet<CreatureId> membership,
            long latestAdmittedBirthTick,
            int excludedByHorizon,
            int excludedAsFounder,
            int excludedByBirthWindow,
            int excludedWithoutGenome)
        {
            _members = members;
            _membership = membership;
            LatestAdmittedBirthTick = latestAdmittedBirthTick;
            ExcludedByHorizon = excludedByHorizon;
            ExcludedAsFounder = excludedAsFounder;
            ExcludedByBirthWindow = excludedByBirthWindow;
            ExcludedWithoutGenome = excludedWithoutGenome;
        }

        public int Count => _members.Count;

        /// <summary>The last birth tick this cohort admits. Nothing born after it is included.</summary>
        public long LatestAdmittedBirthTick { get; }

        public int ExcludedByHorizon { get; }
        public int ExcludedAsFounder { get; }
        public int ExcludedByBirthWindow { get; }
        public int ExcludedWithoutGenome { get; }

        /// <summary>A constant, identical for every genotype: <c>AdultAgeSeconds</c> in base ticks.</summary>
        public static long AdultAgeTicks(SimulationSchedule schedule)
        {
            return TicksFor(ReproductionSystem.AdultAgeSeconds, schedule);
        }

        /// <summary>
        /// The longest life reachable by any genotype, in base ticks. Derived by evaluating the
        /// phenotype map at the top of the lifespan clamp rather than by writing the number down.
        /// </summary>
        public static long MaximumLifespanTicks(SimulationSchedule schedule)
        {
            float longestLifeSeconds = Phenotype.FromGenome(LongestLivedReachableGenome).MaximumAgeSeconds;
            return TicksFor(longestLifeSeconds, schedule);
        }

        public static FitnessCohort Select(
            LifeHistoryLedger ledger,
            SimulationSchedule schedule,
            long endTick,
            FitnessCohortKind kind,
            bool excludeFounders = false,
            long birthWindowStartTick = 0,
            long birthWindowEndTick = long.MaxValue)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (endTick < 0) throw new ArgumentOutOfRangeException(nameof(endTick));
            if (birthWindowStartTick < 0) throw new ArgumentOutOfRangeException(nameof(birthWindowStartTick));
            if (birthWindowEndTick < birthWindowStartTick) throw new ArgumentOutOfRangeException(nameof(birthWindowEndTick));
            if (!ledger.IsComplete)
            {
                throw new InvalidOperationException(
                    "A fitness cohort cannot be selected from an incomplete pedigree. Reporting a smaller number instead of refusing would be a silent censoring bias.");
            }

            long latestAdmittedBirthTick = endTick - RequiredObservationTicks(schedule, kind);
            var members = new List<CreatureId>();
            var membership = new HashSet<CreatureId>();
            int excludedByHorizon = 0;
            int excludedAsFounder = 0;
            int excludedByBirthWindow = 0;
            int excludedWithoutGenome = 0;

            for (int index = 0; index < ledger.Count; index++)
            {
                CreatureId creatureId = ledger.GetIdAt(index);
                if (!ledger.TryGet(creatureId, out LifeHistoryRecord record)) continue;

                if (record.BirthTick > latestAdmittedBirthTick)
                {
                    excludedByHorizon++;
                    continue;
                }

                if (excludeFounders && IsFounder(record))
                {
                    excludedAsFounder++;
                    continue;
                }

                if (record.BirthTick < birthWindowStartTick || record.BirthTick > birthWindowEndTick)
                {
                    excludedByBirthWindow++;
                    continue;
                }

                members.Add(creatureId);
                membership.Add(creatureId);
            }

            // Creatures the pedigree knows about but observation never caught alive. Counted here so
            // a driver that observes too rarely reports the gap rather than hiding it.
            excludedWithoutGenome = CountPedigreedWithoutGenome(ledger, latestAdmittedBirthTick, excludeFounders, birthWindowStartTick, birthWindowEndTick);

            return new FitnessCohort(
                members,
                membership,
                latestAdmittedBirthTick,
                excludedByHorizon,
                excludedAsFounder,
                excludedByBirthWindow,
                excludedWithoutGenome);
        }

        public CreatureId GetIdAt(int index)
        {
            if ((uint)index >= (uint)_members.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _members[index];
        }

        public bool Contains(CreatureId creatureId)
        {
            return _membership.Contains(creatureId);
        }

        /// <summary>Parentlessness, which is genotype-independent and therefore a safe filter.</summary>
        public static bool IsFounder(LifeHistoryRecord record)
        {
            return record.FirstParent.Value == 0 && record.SecondParent.Value == 0;
        }

        private static Genome LongestLivedReachableGenome =>
            new Genome(.5f, .5f, .5f, .5f, .5f, .5f, lifespanTendency: 1f);

        private static long RequiredObservationTicks(SimulationSchedule schedule, FitnessCohortKind kind)
        {
            long lifespan = MaximumLifespanTicks(schedule);
            if (kind == FitnessCohortKind.CompleteLife) return lifespan;
            if (kind == FitnessCohortKind.OffspringToAdulthood) return lifespan + AdultAgeTicks(schedule);
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        private static long TicksFor(float seconds, SimulationSchedule schedule)
        {
            if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (schedule.BaseFrequencyHz <= 0) throw new ArgumentOutOfRangeException(nameof(schedule));
            return (long)Math.Ceiling(seconds * (double)schedule.BaseFrequencyHz);
        }

        private static int CountPedigreedWithoutGenome(
            LifeHistoryLedger ledger,
            long latestAdmittedBirthTick,
            bool excludeFounders,
            long birthWindowStartTick,
            long birthWindowEndTick)
        {
            int count = 0;
            for (int index = 0; index < ledger.PedigreeCount; index++)
            {
                CreatureId creatureId = ledger.GetPedigreeIdAt(index);
                if (!ledger.TryGet(creatureId, out LifeHistoryRecord record)) continue;
                if (record.HasGenome) continue;
                if (record.BirthTick > latestAdmittedBirthTick) continue;
                if (excludeFounders && IsFounder(record)) continue;
                if (record.BirthTick < birthWindowStartTick || record.BirthTick > birthWindowEndTick) continue;
                count++;
            }

            return count;
        }
    }
}
