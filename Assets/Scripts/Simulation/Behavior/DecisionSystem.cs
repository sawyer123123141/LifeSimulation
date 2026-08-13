using System;
using LifeSimulation.Simulation.Biology;

namespace LifeSimulation.Simulation.Behavior
{
    public enum CreatureAction : byte
    {
        Wander,
        SeekFood,
        Eat,
        SeekWater,
        Drink,
        Rest,
        SeekMate,
        Reproduce,
    }

    public readonly struct CreatureDecision
    {
        public CreatureDecision(CreatureAction action, int targetResourceIndex, float score, long decisionTick = -1)
        {
            Action = action;
            TargetResourceIndex = targetResourceIndex;
            Score = score;
            DecisionTick = decisionTick;
        }

        public CreatureAction Action { get; }
        public int TargetResourceIndex { get; }
        public float Score { get; }
        public long DecisionTick { get; }
    }

    public readonly struct DecisionDiagnostics
    {
        public DecisionDiagnostics(float foodScore, float waterScore, bool foodVisible, bool waterVisible)
        {
            FoodScore = foodScore;
            WaterScore = waterScore;
            FoodVisible = foodVisible;
            WaterVisible = waterVisible;
        }

        public float FoodScore { get; }
        public float WaterScore { get; }
        public bool FoodVisible { get; }
        public bool WaterVisible { get; }
    }

    public static class DecisionSystem
    {
        private const float MinimumUrgencyToSeekResource = 0.05f;

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation food,
            ResourceObservation water)
        {
            return Decide(needs, phenotype, food, water, out _);
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation food,
            ResourceObservation water,
            out DecisionDiagnostics diagnostics)
        {
            float foodScore = food.IsValid
                ? Urgency(needs.Energy, phenotype.EnergyCapacity) * Availability(food.Distance)
                : -1f;
            float waterScore = water.IsValid
                ? Urgency(needs.Hydration, phenotype.HydrationCapacity) * Availability(water.Distance)
                : -1f;
            diagnostics = new DecisionDiagnostics(foodScore, waterScore, food.IsValid, water.IsValid);

            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            if (waterScore > foodScore && waterScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekWater, water.ResourceIndex, waterScore);
            }

            if (foodScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekFood, food.ResourceIndex, foodScore);
            }

            return new CreatureDecision(CreatureAction.Wander, -1, 0f);
        }

        private static float Urgency(float current, float capacity)
        {
            if (capacity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            return Math.Max(0f, Math.Min(1f, 1f - (current / capacity)));
        }

        private static float Availability(float distance)
        {
            return 1f / (1f + Math.Max(0f, distance));
        }
    }
}
