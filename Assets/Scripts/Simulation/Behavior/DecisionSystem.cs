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
        public CreatureDecision(CreatureAction action, int targetResourceIndex, float score)
        {
            Action = action;
            TargetResourceIndex = targetResourceIndex;
            Score = score;
        }

        public CreatureAction Action { get; }
        public int TargetResourceIndex { get; }
        public float Score { get; }
    }

    public static class DecisionSystem
    {
        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation food,
            ResourceObservation water)
        {
            float foodScore = food.IsValid
                ? Urgency(needs.Energy, phenotype.EnergyCapacity) * Availability(food.Distance)
                : -1f;
            float waterScore = water.IsValid
                ? Urgency(needs.Hydration, phenotype.HydrationCapacity) * Availability(water.Distance)
                : -1f;

            if (waterScore > foodScore && waterScore >= 0f)
            {
                return new CreatureDecision(CreatureAction.SeekWater, water.ResourceIndex, waterScore);
            }

            if (foodScore >= 0f)
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
