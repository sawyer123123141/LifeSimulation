using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// One continuous stretch during which a creature held a single action, with the needs it
    /// started and finished on. The needs are what make an episode readable: "SeekFood for 14
    /// ticks, energy 0.41 to 0.33" says the trip cost more than it returned, which is invisible
    /// from an instantaneous inspector reading.
    /// </summary>
    public readonly struct CreatureActionEpisode
    {
        public CreatureActionEpisode(
            CreatureAction action,
            long startTick,
            long endTick,
            float startEnergyFraction,
            float startHydrationFraction,
            float endEnergyFraction,
            float endHydrationFraction)
        {
            Action = action;
            StartTick = startTick;
            EndTick = endTick;
            StartEnergyFraction = startEnergyFraction;
            StartHydrationFraction = startHydrationFraction;
            EndEnergyFraction = endEnergyFraction;
            EndHydrationFraction = endHydrationFraction;
        }

        public CreatureAction Action { get; }
        public long StartTick { get; }

        /// <summary>Tick of the last observation that still held this action.</summary>
        public long EndTick { get; }

        public float StartEnergyFraction { get; }
        public float StartHydrationFraction { get; }
        public float EndEnergyFraction { get; }
        public float EndHydrationFraction { get; }

        public long DurationTicks => EndTick - StartTick + 1;
        public float EnergyDelta => EndEnergyFraction - StartEnergyFraction;
        public float HydrationDelta => EndHydrationFraction - StartHydrationFraction;
    }

    /// <summary>
    /// What one creature has actually been doing, as a bounded list of action episodes plus a
    /// lifetime budget of ticks per action.
    ///
    /// <para><b>A passive observer, exactly like <c>LivenessRecorder</c>.</b> It samples the world
    /// from outside and nothing in the simulation reads it, so it adds no simulation state, appears
    /// in no hash, and cannot change a single tick of behavior. That is deliberate and load-bearing:
    /// a per-creature history held <i>inside</i> <c>SimulationWorld</c> would be future-determining
    /// state by the letter of the state-fingerprint design, and would then have to be argued about
    /// every time a fingerprint changed. Keeping it outside makes the question not arise.</para>
    ///
    /// <para>Also not gated by a <c>SimulationConfig</c> flag, for the reason given on
    /// <c>SimulationWorld.Liveness</c>: a diagnostics flag would have to be behavior-inert to be
    /// correct, and <c>FlagLivenessAnalysis</c> would then report it inert and fail the
    /// known-inert-flag assertion.</para>
    ///
    /// <para>Deterministic and Unity-free: a fixed seed observed at the same ticks yields the same
    /// history, which is what makes it testable headlessly rather than only visible in Play mode.
    /// Durations are kept in ticks; converting to seconds is the caller's business, so this type
    /// never has to know the schedule.</para>
    /// </summary>
    public sealed class CreatureActionHistory
    {
        public const int DefaultCapacity = 12;

        private readonly CreatureActionEpisode[] _episodes;
        private readonly long[] _ticksByAction;

        private CreatureId _trackedCreature;
        private bool _isTracking;

        private bool _hasOpenEpisode;
        private CreatureAction _openAction;
        private long _openStartTick;
        private float _openStartEnergy;
        private float _openStartHydration;
        private long _openEndTick;
        private float _openEndEnergy;
        private float _openEndHydration;

        private int _writeIndex;

        public CreatureActionHistory(int capacity = DefaultCapacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            _episodes = new CreatureActionEpisode[capacity];
            _ticksByAction = new long[ActionCount];
        }

        /// <summary>Number of values in <see cref="CreatureAction"/>, pinned by a test.</summary>
        public const int ActionCount = 14;

        public int Capacity => _episodes.Length;

        /// <summary>Completed episodes currently retained, oldest evicted first.</summary>
        public int EpisodeCount { get; private set; }

        public bool IsTracking => _isTracking;
        public CreatureId TrackedCreature => _trackedCreature;

        /// <summary>False once the tracked creature has left the world. Meaningless while not tracking.</summary>
        public bool IsAlive { get; private set; }

        /// <summary>Tick at which the tracked creature was last seen alive, or -1 if never observed.</summary>
        public long LastObservedTick { get; private set; } = -1;

        /// <summary>Total ticks observed for the tracked creature, across all actions.</summary>
        public long ObservedTicks { get; private set; }

        /// <summary>The episode still in progress, if any. Its <c>EndTick</c> is the latest observation.</summary>
        public bool TryGetOpenEpisode(out CreatureActionEpisode episode)
        {
            if (!_hasOpenEpisode)
            {
                episode = default;
                return false;
            }

            episode = BuildOpenEpisode();
            return true;
        }

        /// <summary>Completed episodes, index 0 being the most recent.</summary>
        public CreatureActionEpisode GetEpisodeAt(int index)
        {
            if ((uint)index >= (uint)EpisodeCount) throw new ArgumentOutOfRangeException(nameof(index));

            int slot = _writeIndex - 1 - index;
            if (slot < 0) slot += _episodes.Length;
            return _episodes[slot];
        }

        public long GetObservedTicksFor(CreatureAction action)
        {
            int actionIndex = (int)action;
            return (uint)actionIndex >= (uint)_ticksByAction.Length ? 0L : _ticksByAction[actionIndex];
        }

        /// <summary>
        /// Begin tracking a creature, discarding any previous history. Tracking the creature already
        /// tracked is a no-op, so a caller may call this every frame from a selection without
        /// destroying the history it is displaying.
        /// </summary>
        public void Track(CreatureId creature)
        {
            if (_isTracking && _trackedCreature.Equals(creature)) return;

            Clear();
            _trackedCreature = creature;
            _isTracking = true;
            IsAlive = true;
        }

        public void Clear()
        {
            Array.Clear(_episodes, 0, _episodes.Length);
            Array.Clear(_ticksByAction, 0, _ticksByAction.Length);
            _trackedCreature = default;
            _isTracking = false;
            IsAlive = false;
            _hasOpenEpisode = false;
            _writeIndex = 0;
            EpisodeCount = 0;
            LastObservedTick = -1;
            ObservedTicks = 0;
        }

        /// <summary>
        /// Sample the tracked creature once. Call after each completed <c>Step</c>; calling it more
        /// or less often changes only the resolution of the history, never the simulation.
        ///
        /// <para>Observing the same tick twice is ignored, so a caller that samples both inside its
        /// step loop and again while paused does not inflate the budget.</para>
        /// </summary>
        public void Observe(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!_isTracking || !IsAlive) return;
            if (world.CurrentTick == LastObservedTick) return;

            if (!world.TryGetCreatureIndex(_trackedCreature, out int index))
            {
                // Gone: closed by death or removal. Seal the open episode so the last thing the
                // creature was doing survives in the history rather than vanishing with it.
                CloseOpenEpisode();
                IsAlive = false;
                return;
            }

            CreatureNeeds needs = world.GetCreatureNeedsAt(index);
            Phenotype phenotype = world.Creatures.GetPhenotypeAt(index);
            CreatureAction action = world.GetCreatureDecisionAt(index).Action;

            float energyFraction = Fraction(needs.Energy, phenotype.EnergyCapacity);
            float hydrationFraction = Fraction(needs.Hydration, phenotype.HydrationCapacity);

            LastObservedTick = world.CurrentTick;
            ObservedTicks++;

            int actionIndex = (int)action;
            if ((uint)actionIndex < (uint)_ticksByAction.Length)
            {
                _ticksByAction[actionIndex]++;
            }

            if (_hasOpenEpisode && _openAction == action)
            {
                _openEndTick = world.CurrentTick;
                _openEndEnergy = energyFraction;
                _openEndHydration = hydrationFraction;
                return;
            }

            CloseOpenEpisode();

            _hasOpenEpisode = true;
            _openAction = action;
            _openStartTick = world.CurrentTick;
            _openStartEnergy = energyFraction;
            _openStartHydration = hydrationFraction;
            _openEndTick = world.CurrentTick;
            _openEndEnergy = energyFraction;
            _openEndHydration = hydrationFraction;
        }

        private void CloseOpenEpisode()
        {
            if (!_hasOpenEpisode) return;

            _episodes[_writeIndex] = BuildOpenEpisode();
            _writeIndex = (_writeIndex + 1) % _episodes.Length;
            if (EpisodeCount < _episodes.Length) EpisodeCount++;
            _hasOpenEpisode = false;
        }

        private CreatureActionEpisode BuildOpenEpisode()
        {
            return new CreatureActionEpisode(
                _openAction,
                _openStartTick,
                _openEndTick,
                _openStartEnergy,
                _openStartHydration,
                _openEndEnergy,
                _openEndHydration);
        }

        private static float Fraction(float value, float capacity)
        {
            return capacity <= 0f ? 0f : value / capacity;
        }
    }
}
