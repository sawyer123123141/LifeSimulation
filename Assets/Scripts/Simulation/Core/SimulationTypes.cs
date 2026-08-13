using System;

namespace LifeSimulation.Simulation.Core
{
    public readonly struct CreatureId : IEquatable<CreatureId>
    {
        public CreatureId(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public bool Equals(CreatureId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CreatureId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public readonly struct SimVector2
    {
        public SimVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public static float Distance(SimVector2 left, SimVector2 right)
        {
            float x = right.X - left.X;
            float y = right.Y - left.Y;
            return (float)Math.Sqrt((x * x) + (y * y));
        }
    }

    public readonly struct CreatureLineage
    {
        public CreatureLineage(CreatureId lineageId, CreatureId firstParent, CreatureId secondParent, int generation)
        {
            LineageId = lineageId;
            FirstParent = firstParent;
            SecondParent = secondParent;
            Generation = generation;
        }

        public CreatureId LineageId { get; }
        public CreatureId FirstParent { get; }
        public CreatureId SecondParent { get; }
        public int Generation { get; }
    }

    public readonly struct SimulationStatistics
    {
        public SimulationStatistics(
            long tick,
            int population,
            int highestGeneration,
            float meanBodySizeGene,
            float meanEnergyFraction,
            float meanHydrationFraction,
            float availableFood,
            float availableWater)
        {
            Tick = tick;
            Population = population;
            HighestGeneration = highestGeneration;
            MeanBodySizeGene = meanBodySizeGene;
            MeanEnergyFraction = meanEnergyFraction;
            MeanHydrationFraction = meanHydrationFraction;
            AvailableFood = availableFood;
            AvailableWater = availableWater;
        }

        public long Tick { get; }
        public int Population { get; }
        public int HighestGeneration { get; }
        public float MeanBodySizeGene { get; }
        public float MeanEnergyFraction { get; }
        public float MeanHydrationFraction { get; }
        public float AvailableFood { get; }
        public float AvailableWater { get; }
    }

    public struct ReproductionState
    {
        public float CooldownRemaining;
    }

    public enum RandomDomain : ulong
    {
        Wander = 1,
        Crossover = 2,
        Mutation = 3,
        BirthPlacement = 4,
        ExperimentSampling = 5,
        Exploration = 6
    }

    public enum DeathCause : byte
    {
        Debug = 0,
        Starvation = 1,
        Dehydration = 2,
        Age = 3,
        Health = 4
    }
}
