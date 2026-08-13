using System;
using System.Collections.Generic;

namespace LifeSimulation.Simulation.Core
{
    public sealed class CreatureStore
    {
        private CreatureId[] _identities;
        private readonly Dictionary<CreatureId, int> _indexById;
        private long _nextId;

        public CreatureStore(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            _identities = new CreatureId[Math.Max(initialCapacity, 1)];
            _indexById = new Dictionary<CreatureId, int>(initialCapacity);
            _nextId = 1;
        }

        public int Count { get; private set; }

        public CreatureId Add()
        {
            EnsureCapacity(Count + 1);

            var id = new CreatureId(_nextId++);
            _identities[Count] = id;
            _indexById.Add(id, Count);
            Count++;
            return id;
        }

        public bool TryGetIndex(CreatureId id, out int index)
        {
            return _indexById.TryGetValue(id, out index);
        }

        public bool Remove(CreatureId id)
        {
            if (!_indexById.TryGetValue(id, out int removedIndex))
            {
                return false;
            }

            int lastIndex = Count - 1;
            CreatureId movedId = _identities[lastIndex];

            _indexById.Remove(id);
            if (removedIndex != lastIndex)
            {
                _identities[removedIndex] = movedId;
                _indexById[movedId] = removedIndex;
            }

            Count--;
            return true;
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _identities.Length)
            {
                return;
            }

            int nextCapacity = Math.Max(required, _identities.Length * 2);
            Array.Resize(ref _identities, nextCapacity);
        }
    }
}
