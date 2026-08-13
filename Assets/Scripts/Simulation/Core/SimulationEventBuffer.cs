using System;

namespace LifeSimulation.Simulation.Core
{
    public sealed class SimulationEventBuffer
    {
        private readonly SimulationEvent[] _events;

        public SimulationEventBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _events = new SimulationEvent[capacity];
        }

        public int Count { get; private set; }
        public bool Overflowed { get; private set; }

        public bool TryWrite(SimulationEvent simulationEvent)
        {
            if (Count >= _events.Length)
            {
                Overflowed = true;
                return false;
            }

            _events[Count++] = simulationEvent;
            return true;
        }

        public SimulationEvent GetAt(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _events[index];
        }

        public void Clear()
        {
            Count = 0;
            Overflowed = false;
        }
    }
}
