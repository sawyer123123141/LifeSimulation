using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Resources
{
    public sealed class ResourceStore
    {
        private ResourceId[] _ids;
        private ResourceKind[] _kinds;
        private SimVector2[] _positions;
        private float[] _interactionRadii;
        private float[] _amounts;
        private float[] _capacities;
        private float[] _regenerationPerSecond;
        private bool[] _isActive;
        private float[] _nutritionMultipliers;
        private readonly Dictionary<ResourceId, int> _indexById;
        private long _nextId;

        public ResourceStore(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            int capacity = Math.Max(initialCapacity, 1);
            _ids = new ResourceId[capacity];
            _kinds = new ResourceKind[capacity];
            _positions = new SimVector2[capacity];
            _interactionRadii = new float[capacity];
            _amounts = new float[capacity];
            _capacities = new float[capacity];
            _regenerationPerSecond = new float[capacity];
            _isActive = new bool[capacity];
            _nutritionMultipliers = new float[capacity];
            _indexById = new Dictionary<ResourceId, int>(initialCapacity);
            _nextId = 1;
        }

        public int Count { get; private set; }

        public ResourceId Add(
            ResourceKind kind,
            SimVector2 position,
            float interactionRadius,
            float initialAmount,
            float capacity,
            float regenerationPerSecond,
            float nutritionMultiplier = 1f)
        {
            ValidateDefinition(interactionRadius, initialAmount, capacity, regenerationPerSecond, nutritionMultiplier);
            EnsureCapacity(Count + 1);

            ResourceId id = new ResourceId(_nextId++);
            _ids[Count] = id;
            _kinds[Count] = kind;
            _positions[Count] = position;
            _interactionRadii[Count] = interactionRadius;
            _amounts[Count] = initialAmount;
            _capacities[Count] = capacity;
            _regenerationPerSecond[Count] = regenerationPerSecond;
            _isActive[Count] = true;
            _nutritionMultipliers[Count] = nutritionMultiplier;
            _indexById.Add(id, Count);
            Count++;
            return id;
        }

        public ResourceState GetAt(int index)
        {
            ValidateIndex(index);
            return new ResourceState(
                _ids[index],
                _kinds[index],
                _positions[index],
                _interactionRadii[index],
                _amounts[index],
                _capacities[index],
                _regenerationPerSecond[index],
                _isActive[index],
                _nutritionMultipliers[index]);
        }

        public bool TryGetIndex(ResourceId id, out int index)
        {
            return _indexById.TryGetValue(id, out index);
        }

        public void SetActive(ResourceId id, bool isActive)
        {
            if (!_indexById.TryGetValue(id, out int index))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            _isActive[index] = isActive;
        }

        public float ConsumeAt(int index, float requestedAmount)
        {
            ValidateIndex(index);
            if (requestedAmount < 0f || float.IsNaN(requestedAmount) || float.IsInfinity(requestedAmount))
            {
                throw new ArgumentOutOfRangeException(nameof(requestedAmount));
            }

            if (!_isActive[index])
            {
                return 0f;
            }

            float allocatedAmount = Math.Min(_amounts[index], requestedAmount);
            _amounts[index] -= allocatedAmount;
            return allocatedAmount;
        }

        public void Regenerate(float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            for (int index = 0; index < Count; index++)
            {
                if (_isActive[index])
                {
                    _amounts[index] = Math.Min(
                        _capacities[index],
                        _amounts[index] + (_regenerationPerSecond[index] * deltaTime));
                }
            }
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _ids.Length)
            {
                return;
            }

            int nextCapacity = Math.Max(required, _ids.Length * 2);
            Array.Resize(ref _ids, nextCapacity);
            Array.Resize(ref _kinds, nextCapacity);
            Array.Resize(ref _positions, nextCapacity);
            Array.Resize(ref _interactionRadii, nextCapacity);
            Array.Resize(ref _amounts, nextCapacity);
            Array.Resize(ref _capacities, nextCapacity);
            Array.Resize(ref _regenerationPerSecond, nextCapacity);
            Array.Resize(ref _isActive, nextCapacity);
            Array.Resize(ref _nutritionMultipliers, nextCapacity);
        }

        private static void ValidateDefinition(
            float interactionRadius,
            float initialAmount,
            float capacity,
            float regenerationPerSecond,
            float nutritionMultiplier)
        {
            if (interactionRadius <= 0f || float.IsNaN(interactionRadius) || float.IsInfinity(interactionRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(interactionRadius));
            }

            if (capacity < 0f || float.IsNaN(capacity) || float.IsInfinity(capacity))
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (initialAmount < 0f || initialAmount > capacity || float.IsNaN(initialAmount) || float.IsInfinity(initialAmount))
            {
                throw new ArgumentOutOfRangeException(nameof(initialAmount));
            }

            if (regenerationPerSecond < 0f || float.IsNaN(regenerationPerSecond) || float.IsInfinity(regenerationPerSecond))
            {
                throw new ArgumentOutOfRangeException(nameof(regenerationPerSecond));
            }

            if (nutritionMultiplier < 0f || float.IsNaN(nutritionMultiplier) || float.IsInfinity(nutritionMultiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(nutritionMultiplier));
            }
        }

        private void ValidateIndex(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
