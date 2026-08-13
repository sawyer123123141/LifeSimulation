using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Resources
{
    public enum ResourceKind
    {
        Food,
        Water,
        Carcass,
    }

    public readonly struct ResourceId : IEquatable<ResourceId>
    {
        public ResourceId(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public bool Equals(ResourceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public readonly struct ResourceState
    {
        public ResourceState(
            ResourceId id,
            ResourceKind kind,
            SimVector2 position,
            float interactionRadius,
            float amount,
            float capacity,
            float regenerationPerSecond,
            bool isActive)
        {
            Id = id;
            Kind = kind;
            Position = position;
            InteractionRadius = interactionRadius;
            Amount = amount;
            Capacity = capacity;
            RegenerationPerSecond = regenerationPerSecond;
            IsActive = isActive;
        }

        public ResourceId Id { get; }
        public ResourceKind Kind { get; }
        public SimVector2 Position { get; }
        public float InteractionRadius { get; }
        public float Amount { get; }
        public float Capacity { get; }
        public float RegenerationPerSecond { get; }
        public bool IsActive { get; }
    }
}
