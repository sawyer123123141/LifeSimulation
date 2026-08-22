using System;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>Fixed-capacity host-drained output for immutable cluster-history evidence.</summary>
    public sealed class ClusterHistoryEventBuffer
    {
        private readonly ClusterHistoryEvent[] _events;

        public ClusterHistoryEventBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _events = new ClusterHistoryEvent[capacity];
        }

        public int Capacity => _events.Length;
        public int Count { get; private set; }
        public bool Overflowed { get; private set; }

        public bool TryWrite(ClusterHistoryEvent historyEvent)
        {
            if (Count >= _events.Length)
            {
                Overflowed = true;
                return false;
            }

            _events[Count++] = historyEvent;
            return true;
        }

        public ClusterHistoryEvent GetAt(int index)
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _events[index];
        }

        public void Clear()
        {
            Count = 0;
            Overflowed = false;
        }
    }
}
