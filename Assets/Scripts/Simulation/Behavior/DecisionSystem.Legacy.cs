using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Behavior
{
    public static partial class DecisionSystem
    {

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation food,
            ResourceObservation water)
        {
            return Decide(needs, phenotype, food, water, out _);
        }

        public static CreatureDecision DecideFromLearnedOutcomes(
            CreatureNeeds needs,
            Phenotype phenotype,
            MemoryState memory,
            ResourceObservation food,
            ResourceObservation water,
            ResourceStore resources,
            out DecisionDiagnostics diagnostics,
            bool learnedResourceQualityEnabled = false)
        {
            float foodValue = KnownOutcomeOrCuriosity(memory.FoodOutcomeValue, memory.FoodExperienceCount, phenotype.Exploration);
            float waterValue = KnownOutcomeOrCuriosity(memory.WaterOutcomeValue, memory.WaterExperienceCount, phenotype.Exploration);
            float foodNeedGain = learnedResourceQualityEnabled && food.IsValid
                ? ComputeNeedGain(false, needs, phenotype, resources.GetAt(food.ResourceIndex))
                : 1f;
            float waterNeedGain = learnedResourceQualityEnabled && water.IsValid
                ? ComputeNeedGain(true, needs, phenotype, resources.GetAt(water.ResourceIndex))
                : 1f;
            float foodScore = food.IsValid ? Urgency(needs.Energy, phenotype.EnergyCapacity) * Availability(food.Distance) * foodValue * foodNeedGain : -1f;
            float waterScore = water.IsValid ? Urgency(needs.Hydration, phenotype.HydrationCapacity) * Availability(water.Distance) * waterValue * waterNeedGain : -1f;
            diagnostics = new DecisionDiagnostics(foodScore, waterScore, food.IsValid, water.IsValid);
            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            return waterScore > foodScore
                ? new CreatureDecision(CreatureAction.SeekWater, water.ResourceIndex, waterScore)
                : new CreatureDecision(CreatureAction.SeekFood, food.ResourceIndex, foodScore);
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            CreatureAction currentAction,
            float secondsInCurrentAction,
            float handlingSeconds,
            float referenceGain,
            float commitmentStrength,
            float commitmentHalfLifeSeconds,
            out DecisionDiagnostics diagnostics)
        {
            float foodScore = BestPatchScore(needs.Energy, phenotype.EnergyCapacity, phenotype, foodCandidates, handlingSeconds, referenceGain, out int foodResourceIndex);
            float waterScore = BestPatchScore(needs.Hydration, phenotype.HydrationCapacity, phenotype, waterCandidates, handlingSeconds, referenceGain, out int waterResourceIndex);

            if (foodResourceIndex >= 0 && (currentAction == CreatureAction.SeekFood || currentAction == CreatureAction.Eat))
            {
                foodScore += ForagingEconomics.CommitmentBonus(secondsInCurrentAction, phenotype.Persistence, commitmentStrength, commitmentHalfLifeSeconds);
            }
            else if (waterResourceIndex >= 0 && (currentAction == CreatureAction.SeekWater || currentAction == CreatureAction.Drink))
            {
                waterScore += ForagingEconomics.CommitmentBonus(secondsInCurrentAction, phenotype.Persistence, commitmentStrength, commitmentHalfLifeSeconds);
            }

            diagnostics = new DecisionDiagnostics(foodScore, waterScore, foodCandidates.Count > 0, waterCandidates.Count > 0);

            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            if (waterScore > foodScore && waterScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekWater, waterResourceIndex, waterScore);
            }

            if (foodScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekFood, foodResourceIndex, foodScore);
            }

            return new CreatureDecision(CreatureAction.Wander, -1, 0f);
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
    }
}
