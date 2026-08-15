using System;

namespace LifeSimulation.Simulation.Environment
{
    public sealed class PlantSiteRegistry
    {
        private int[] _resourceIndices;

        public PlantSiteRegistry(int initialCapacity)
        {
            int capacity = Math.Max(1, initialCapacity);
            _resourceIndices = new int[capacity];
        }

        public int Count { get; private set; }

        public void Register(int resourceIndex)
        {
            EnsureCapacity(Count + 1);
            _resourceIndices[Count++] = resourceIndex;
        }

        public int GetResourceIndexAt(int slot)
        {
            return _resourceIndices[slot];
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _resourceIndices.Length) return;
            int capacity = Math.Max(required, _resourceIndices.Length * 2);
            Array.Resize(ref _resourceIndices, capacity);
        }
    }
}
