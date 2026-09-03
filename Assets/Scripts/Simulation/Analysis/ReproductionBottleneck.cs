#nullable enable annotations

using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    public enum ReproductiveNeed : byte
    {
        Energy = 0,
        Hydration = 1,
        Health = 2
    }

    /// <summary>
    /// Which of the three reproduction needs is actually holding the population back, counted at
    /// reproduction-relevant moments.
    ///
    /// <para>An outside observer: it calls the production predicates
    /// <see cref="ReproductionSystem.CanReproduce"/> and <see cref="ReproductionSystem.CanSeekMate"/>
    /// rather than a copy of their logic, and it writes nothing to the world.</para>
    ///
    /// <para>Samples are taken only on reproduction ticks and only for creatures at or past adult
    /// age. Sampling every tick would weight the answer by lifespan; sampling juveniles would weight
    /// it by the time before a creature can breed at all, and age is not a need.</para>
    /// </summary>
    public sealed class ReproductionBottleneck
    {
        private const int NeedCount = 3;

        private readonly float _needFraction;
        private readonly long[] _minimumNeed = new long[NeedCount];
        private readonly long[] _blockingNeed = new long[NeedCount];
        private readonly long[] _blockedMinimumNeed = new long[NeedCount];

        public ReproductionBottleneck(float needFraction)
        {
            if (needFraction < 0f || needFraction > 1f) throw new ArgumentOutOfRangeException(nameof(needFraction));
            _needFraction = needFraction;
        }

        /// <summary>Adult creature-samples taken. The denominator for the unconditional distribution.</summary>
        public long AdultSamples { get; private set; }

        /// <summary>Adult samples that failed <see cref="ReproductionSystem.CanReproduce"/> for any reason.</summary>
        public long BlockedSamples { get; private set; }

        /// <summary>Adult samples blocked only because a cooldown was still running. Not a need.</summary>
        public long CooldownBlockedSamples { get; private set; }

        /// <summary>Adult samples that passed the breeding gate but failed the higher mate-seeking gate.</summary>
        public long MateSeekingBlockedSamples { get; private set; }

        /// <summary>Creatures below adult age, which are counted out rather than counted in as blocked.</summary>
        public long JuvenileSamplesSkipped { get; private set; }

        /// <summary>How often this need was the lowest of the three, over all adult samples.</summary>
        public long MinimumNeedCount(ReproductiveNeed need) => _minimumNeed[IndexOf(need)];

        /// <summary>How often this need was itself below the gate. Several needs can block one sample.</summary>
        public long BlockingNeedCount(ReproductiveNeed need) => _blockingNeed[IndexOf(need)];

        /// <summary>
        /// How often this need was the lowest of the three, restricted to samples that were blocked.
        /// This is the conditioning that says whether an energy-side trait can reach fitness at all.
        /// </summary>
        public long BlockedMinimumNeedCount(ReproductiveNeed need) => _blockedMinimumNeed[IndexOf(need)];

        public static bool IsReproductionTick(SimulationSchedule schedule, long tick)
        {
            if (schedule.ReproductionHz <= 0) throw new ArgumentOutOfRangeException(nameof(schedule));
            int interval = schedule.BaseFrequencyHz / schedule.ReproductionHz;
            if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(schedule));
            return tick % interval == 0;
        }

        /// <summary>Samples every adult in the world, but only on a reproduction tick.</summary>
        public bool SampleWorld(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!IsReproductionTick(world.Config.Schedule, world.CurrentTick)) return false;

            CreatureStore creatures = world.Creatures;
            for (int index = 0; index < creatures.Count; index++)
            {
                Sample(creatures.GetNeedsAt(index), creatures.GetPhenotypeAt(index), creatures.GetReproductionRefAt(index));
            }

            return true;
        }

        /// <summary>Returns false for a juvenile, which is recorded as skipped rather than as blocked.</summary>
        public bool Sample(CreatureNeeds needs, Phenotype phenotype, ReproductionState reproduction)
        {
            if (needs.Age < ReproductionSystem.AdultAgeSeconds)
            {
                JuvenileSamplesSkipped++;
                return false;
            }

            AdultSamples++;

            float energyRatio = Ratio(needs.Energy, phenotype.EnergyCapacity);
            float hydrationRatio = Ratio(needs.Hydration, phenotype.HydrationCapacity);
            float healthRatio = Ratio(needs.Health, phenotype.HealthCapacity);
            int minimumIndex = LowestOf(energyRatio, hydrationRatio, healthRatio);
            _minimumNeed[minimumIndex]++;

            bool canReproduce = ReproductionSystem.CanReproduce(needs, phenotype, reproduction, _needFraction);
            if (!canReproduce)
            {
                BlockedSamples++;
                _blockedMinimumNeed[minimumIndex]++;

                if (energyRatio < _needFraction) _blockingNeed[IndexOf(ReproductiveNeed.Energy)]++;
                if (hydrationRatio < _needFraction) _blockingNeed[IndexOf(ReproductiveNeed.Hydration)]++;
                if (healthRatio < _needFraction) _blockingNeed[IndexOf(ReproductiveNeed.Health)]++;

                // Age is excluded by construction above, so a cooldown is the only non-need blocker
                // that can reach this point.
                if (reproduction.CooldownRemaining > 0f) CooldownBlockedSamples++;
            }
            else if (!ReproductionSystem.CanSeekMate(needs, phenotype, reproduction, _needFraction))
            {
                MateSeekingBlockedSamples++;
            }

            return true;
        }

        private static float Ratio(float value, float capacity)
        {
            // A zero capacity is not reachable from the phenotype map, but dividing by it would put a
            // NaN into a count that is supposed to be a census.
            if (capacity <= 0f) return 0f;
            return value / capacity;
        }

        private static int LowestOf(float energy, float hydration, float health)
        {
            if (energy <= hydration && energy <= health) return IndexOf(ReproductiveNeed.Energy);
            if (hydration <= health) return IndexOf(ReproductiveNeed.Hydration);
            return IndexOf(ReproductiveNeed.Health);
        }

        private static int IndexOf(ReproductiveNeed need)
        {
            if ((uint)need > (uint)ReproductiveNeed.Health) throw new ArgumentOutOfRangeException(nameof(need));
            return (int)need;
        }
    }
}
