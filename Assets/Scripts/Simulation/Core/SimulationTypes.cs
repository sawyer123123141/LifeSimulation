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
            float meanMovementSpeedGene,
            float meanMetabolicPaceGene,
            float meanVisionRangeGene,
            float meanWaterEfficiencyGene,
            float meanFoodEfficiencyGene,
            float meanEnergyFraction,
            float meanHydrationFraction,
            float availableFood,
            float availableWater,
            float cumulativeFoodConsumed,
            float cumulativeWaterConsumed,
            int birthCount,
            int deathCount)
        {
            Tick = tick;
            Population = population;
            HighestGeneration = highestGeneration;
            MeanBodySizeGene = meanBodySizeGene;
            MeanMovementSpeedGene = meanMovementSpeedGene;
            MeanMetabolicPaceGene = meanMetabolicPaceGene;
            MeanVisionRangeGene = meanVisionRangeGene;
            MeanWaterEfficiencyGene = meanWaterEfficiencyGene;
            MeanFoodEfficiencyGene = meanFoodEfficiencyGene;
            MeanEnergyFraction = meanEnergyFraction;
            MeanHydrationFraction = meanHydrationFraction;
            AvailableFood = availableFood;
            AvailableWater = availableWater;
            CumulativeFoodConsumed = cumulativeFoodConsumed;
            CumulativeWaterConsumed = cumulativeWaterConsumed;
            BirthCount = birthCount;
            DeathCount = deathCount;
        }

        public long Tick { get; }
        public int Population { get; }
        public int HighestGeneration { get; }
        public float MeanBodySizeGene { get; }
        public float MeanMovementSpeedGene { get; }
        public float MeanMetabolicPaceGene { get; }
        public float MeanVisionRangeGene { get; }
        public float MeanWaterEfficiencyGene { get; }
        public float MeanFoodEfficiencyGene { get; }
        public float MeanEnergyFraction { get; }
        public float MeanHydrationFraction { get; }
        public float AvailableFood { get; }
        public float AvailableWater { get; }
        public float CumulativeFoodConsumed { get; }
        public float CumulativeWaterConsumed { get; }
        public int BirthCount { get; }
        public int DeathCount { get; }
    }

    public struct ReproductionState
    {
        public float CooldownRemaining;
    }

    public struct CombatState
    {
        public float WoundSeverity;
        public float AttackRecoveryRemaining;
    }

    public enum RandomDomain : ulong
    {
        Wander = 1,
        Crossover = 2,
        Mutation = 3,
        BirthPlacement = 4,
        ExperimentSampling = 5,
        Exploration = 6,
        FounderGenome = 7,
        AttackResolution = 8
    }

    public enum DeathCause : byte
    {
        None = 0,
        Debug = 1,
        Starvation = 2,
        Dehydration = 3,
        Age = 4,
        Health = 5,
        Predation = 6
    }

    public enum SimulationEventKind : byte
    {
        Birth = 0,
        Death = 1
    }

    public readonly struct SimulationEvent
    {
        public SimulationEvent(
            long tick,
            SimulationEventKind kind,
            CreatureId subject,
            CreatureId firstRelated,
            CreatureId secondRelated,
            DeathCause deathCause)
        {
            Tick = tick;
            Kind = kind;
            Subject = subject;
            FirstRelated = firstRelated;
            SecondRelated = secondRelated;
            DeathCause = deathCause;
        }

        public long Tick { get; }
        public SimulationEventKind Kind { get; }
        public CreatureId Subject { get; }
        public CreatureId FirstRelated { get; }
        public CreatureId SecondRelated { get; }
        public DeathCause DeathCause { get; }
    }
}
