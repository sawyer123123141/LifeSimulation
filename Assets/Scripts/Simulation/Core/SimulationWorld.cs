using System;

namespace LifeSimulation.Simulation.Core
{
    public sealed class SimulationWorld
    {
        private CreatureId[] _pendingDeaths;
        private int _pendingDeathCount;

        public SimulationWorld(SimulationConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Config.Validate();
            Creatures = new CreatureStore(Config.InitialPopulation);
            _pendingDeaths = new CreatureId[Math.Max(Config.InitialPopulation, 1)];

            for (int index = 0; index < Config.InitialPopulation; index++)
            {
                Creatures.Add();
            }
        }

        public SimulationConfig Config { get; }
        public CreatureStore Creatures { get; }
        public int CreatureCount => Creatures.Count;
        public long CurrentTick { get; private set; }

        public CreatureId GetCreatureIdAt(int index)
        {
            return Creatures.GetIdAt(index);
        }

        public bool TryGetCreatureIndex(CreatureId id, out int index)
        {
            return Creatures.TryGetIndex(id, out index);
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

            for (int index = 0; index < _pendingDeathCount; index++)
            {
                Creatures.Remove(_pendingDeaths[index]);
            }

            _pendingDeathCount = 0;
            CurrentTick++;
        }

        private void EnsurePendingDeathCapacity(int required)
        {
            if (required <= _pendingDeaths.Length)
            {
                return;
            }

            Array.Resize(ref _pendingDeaths, Math.Max(required, _pendingDeaths.Length * 2));
        }
    }
}
