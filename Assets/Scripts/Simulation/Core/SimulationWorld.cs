using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Core
{
    public sealed class SimulationWorld
    {
        private CreatureId[] _pendingDeaths;
        private int _pendingDeathCount;
        private long _spawnOrdinal;

        public SimulationWorld(SimulationConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Config.Validate();
            Creatures = new CreatureStore(Config.InitialPopulation);
            Resources = new ResourceStore(initialCapacity: 8);
            Arena = new ArenaBounds(-25f, 25f, -25f, 25f);
            _pendingDeaths = new CreatureId[Math.Max(Config.InitialPopulation, 1)];

            for (int index = 0; index < Config.InitialPopulation; index++)
            {
                Spawn();
            }
        }

        public SimulationConfig Config { get; }
        public CreatureStore Creatures { get; }
        public ResourceStore Resources { get; }
        public ArenaBounds Arena { get; }
        public int CreatureCount => Creatures.Count;
        public long CurrentTick { get; private set; }

        public CreatureId GetCreatureIdAt(int index)
        {
            return Creatures.GetIdAt(index);
        }

        public CreatureId Spawn()
        {
            return Spawn(Genome.Neutral);
        }

        public CreatureId Spawn(Genome genome)
        {
            long spawnOrdinal = _spawnOrdinal++;
            return Creatures.Add(genome, new SimVector2(
                Lerp(Arena.MinimumX, Arena.MaximumX, DeterministicRandom.Float01(Config.WorldSeed, RandomDomain.BirthPlacement, spawnOrdinal, 0, 0, 0)),
                Lerp(Arena.MinimumY, Arena.MaximumY, DeterministicRandom.Float01(Config.WorldSeed, RandomDomain.BirthPlacement, spawnOrdinal, 0, 0, 1))));
        }

        public bool TryGetCreatureIndex(CreatureId id, out int index)
        {
            return Creatures.TryGetIndex(id, out index);
        }

        public CreatureNeeds GetCreatureNeedsAt(int index)
        {
            return Creatures.GetNeedsAt(index);
        }

        public MovementState GetCreatureMovementAt(int index)
        {
            return Creatures.GetMovementAt(index);
        }

        public void RequestDeath(CreatureId id, DeathCause cause)
        {
            if (!Creatures.TryGetIndex(id, out _))
            {
                return;
            }

            for (int index = 0; index < _pendingDeathCount; index++)
            {
                if (_pendingDeaths[index].Equals(id))
                {
                    return;
                }
            }

            EnsurePendingDeathCapacity(_pendingDeathCount + 1);
            _pendingDeaths[_pendingDeathCount++] = id;
        }

        public void Step(float fixedDeltaTime)
        {
            if (Math.Abs(fixedDeltaTime - Config.FixedDeltaTime) > 0.000001f)
            {
                throw new ArgumentException("Simulation steps must use the configured fixed delta.", nameof(fixedDeltaTime));
            }

            long nextTick = CurrentTick + 1;
            TickMovement(nextTick);
            if (IsDue(nextTick, Config.Schedule.NeedsHz))
            {
                TickNeeds();
            }

            if (IsDue(nextTick, Config.Schedule.ResourcesHz))
            {
                Resources.Regenerate(1f / Config.Schedule.ResourcesHz);
            }

            for (int index = 0; index < _pendingDeathCount; index++)
            {
                Creatures.Remove(_pendingDeaths[index]);
            }

            _pendingDeathCount = 0;
            CurrentTick = nextTick;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = 14695981039346656037UL;
            hash = Hash(hash, unchecked((ulong)Config.WorldSeed));
            hash = Hash(hash, unchecked((ulong)CurrentTick));
            hash = Hash(hash, unchecked((ulong)CreatureCount));

            for (int index = 0; index < CreatureCount; index++)
            {
                hash = Hash(hash, unchecked((ulong)GetCreatureIdAt(index).Value));
            }

            return hash;
        }

        private void EnsurePendingDeathCapacity(int required)
        {
            if (required <= _pendingDeaths.Length)
            {
                return;
            }

            Array.Resize(ref _pendingDeaths, Math.Max(required, _pendingDeaths.Length * 2));
        }

        private bool IsDue(long tick, int frequencyHz)
        {
            int interval = Config.Schedule.BaseFrequencyHz / frequencyHz;
            return tick % interval == 0;
        }

        private void TickNeeds()
        {
            float deltaTime = 1f / Config.Schedule.NeedsHz;
            for (int index = 0; index < Creatures.Count; index++)
            {
                ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(index);
                ref MovementState movement = ref Creatures.GetMovementRefAt(index);
                NeedsSystem.Tick(ref needs, Creatures.GetPhenotypeAt(index), deltaTime, movement.DistanceSinceLastNeeds);
                movement.DistanceSinceLastNeeds = 0f;
            }
        }

        private void TickMovement(long nextTick)
        {
            for (int index = 0; index < Creatures.Count; index++)
            {
                CreatureId id = Creatures.GetIdAt(index);
                float angle = DeterministicRandom.Float01(
                    Config.WorldSeed,
                    RandomDomain.Wander,
                    nextTick,
                    id.Value,
                    0,
                    0) * ((float)Math.PI * 2f);
                ref MovementState movement = ref Creatures.GetMovementRefAt(index);
                SimVector2 target = new SimVector2(
                    movement.Position.X + (float)Math.Cos(angle),
                    movement.Position.Y + (float)Math.Sin(angle));
                MovementSystem.MoveToward(
                    ref movement,
                    target,
                    Creatures.GetPhenotypeAt(index).MaximumSpeed,
                    Config.FixedDeltaTime,
                    Arena);
            }
        }

        private static float Lerp(float minimum, float maximum, float t)
        {
            return minimum + ((maximum - minimum) * t);
        }

        private static ulong Hash(ulong hash, ulong value)
        {
            return (hash ^ value) * 1099511628211UL;
        }
    }
}
