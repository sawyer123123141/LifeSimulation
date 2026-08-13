using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

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
        SeekPrey,
        Attack,
        Flee,
        SeekCarcass,
        FeedCarcass,
        SeekThermalComfort,
    }

    public enum CreatureIntent : byte
    {
        Wander = 0,
        SeekFood = 1,
        SeekWater = 2,
        SeekPrey = 3,
        Flee = 4,
        SeekCarcass = 5,
        SeekThermalComfort = 6,
    }

    public readonly struct DecisionCandidate
    {
        public DecisionCandidate(CreatureIntent intent, int targetResourceIndex, CreatureId targetCreatureId, float score)
        {
            Intent = intent;
            TargetResourceIndex = targetResourceIndex;
            TargetCreatureId = targetCreatureId;
            Score = score;
        }

        public CreatureIntent Intent { get; }
        public int TargetResourceIndex { get; }
        public CreatureId TargetCreatureId { get; }
        public float Score { get; }
    }

    public struct DecisionCandidateBuffer
    {
        public const int Capacity = 8;

        private DecisionCandidate _candidate0;
        private DecisionCandidate _candidate1;
        private DecisionCandidate _candidate2;
        private DecisionCandidate _candidate3;
        private DecisionCandidate _candidate4;
        private DecisionCandidate _candidate5;
        private DecisionCandidate _candidate6;
        private DecisionCandidate _candidate7;
        private int _count;

        public int Count => _count;

        public bool TryAdd(DecisionCandidate candidate)
        {
            if (_count >= Capacity)
            {
                return false;
            }

            SetAt(_count++, candidate);
            return true;
        }

        public bool TryGetBest(out DecisionCandidate best)
        {
            if (_count == 0)
            {
                best = default;
                return false;
            }

            best = GetAt(0);
            for (int index = 1; index < _count; index++)
            {
                DecisionCandidate candidate = GetAt(index);
                if (IsBetter(candidate, best))
                {
                    best = candidate;
                }
            }

            return true;
        }

        private static bool IsBetter(DecisionCandidate candidate, DecisionCandidate currentBest)
        {
            if (candidate.Score != currentBest.Score)
            {
                return candidate.Score > currentBest.Score;
            }

            if (candidate.Intent != currentBest.Intent)
            {
                return candidate.Intent < currentBest.Intent;
            }

            if (candidate.TargetResourceIndex != currentBest.TargetResourceIndex)
            {
                return candidate.TargetResourceIndex < currentBest.TargetResourceIndex;
            }

            return candidate.TargetCreatureId.Value < currentBest.TargetCreatureId.Value;
        }

        private DecisionCandidate GetAt(int index)
        {
            switch (index)
            {
                case 0: return _candidate0;
                case 1: return _candidate1;
                case 2: return _candidate2;
                case 3: return _candidate3;
                case 4: return _candidate4;
                case 5: return _candidate5;
                case 6: return _candidate6;
                default: return _candidate7;
            }
        }

        private void SetAt(int index, DecisionCandidate candidate)
        {
            switch (index)
            {
                case 0: _candidate0 = candidate; break;
                case 1: _candidate1 = candidate; break;
                case 2: _candidate2 = candidate; break;
                case 3: _candidate3 = candidate; break;
                case 4: _candidate4 = candidate; break;
                case 5: _candidate5 = candidate; break;
                case 6: _candidate6 = candidate; break;
                default: _candidate7 = candidate; break;
            }
        }
    }

    public readonly struct CreatureDecision
    {
        public CreatureDecision(
            CreatureAction action,
            int targetResourceIndex,
            float score,
            long decisionTick = -1,
            CreatureId targetCreatureId = default)
        {
            Action = action;
            TargetResourceIndex = targetResourceIndex;
            Score = score;
            DecisionTick = decisionTick;
            TargetCreatureId = targetCreatureId;
        }

        public CreatureAction Action { get; }
        public int TargetResourceIndex { get; }
        public float Score { get; }
        public long DecisionTick { get; }
        public CreatureId TargetCreatureId { get; }
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

        public static CreatureDecision DecideIntentUtilityV1(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            CreatureObservation threat,
            float threatIntensity,
            out DecisionDiagnostics diagnostics)
        {
            var candidates = new DecisionCandidateBuffer();
            float bestFoodScore = -1f;
            float bestWaterScore = -1f;

            ScoreResourceCandidates(CreatureIntent.SeekFood, needs, genome, phenotype, resources, foodCandidates, threat, threatIntensity, ref candidates, ref bestFoodScore);
            ScoreResourceCandidates(CreatureIntent.SeekWater, needs, genome, phenotype, resources, waterCandidates, threat, threatIntensity, ref candidates, ref bestWaterScore);
            diagnostics = new DecisionDiagnostics(bestFoodScore, bestWaterScore, foodCandidates.Count > 0, waterCandidates.Count > 0);
            if (!candidates.TryGetBest(out DecisionCandidate best) || best.Score < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            return new CreatureDecision(
                best.Intent == CreatureIntent.SeekWater ? CreatureAction.SeekWater : CreatureAction.SeekFood,
                best.TargetResourceIndex,
                best.Score);
        }

        private static void ScoreResourceCandidates(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            ResourceCandidateBuffer observations,
            CreatureObservation threat,
            float threatIntensity,
            ref DecisionCandidateBuffer candidates,
            ref float bestScore)
        {
            for (int index = 0; index < observations.Count; index++)
            {
                ResourceObservation observation = observations.GetAt(index);
                ResourceState resource = resources.GetAt(observation.ResourceIndex);
                float score = ResourceUtility(intent, needs, genome, phenotype, resource, observation.Distance, threat, threatIntensity);
                if (score > bestScore)
                {
                    bestScore = score;
                }

                candidates.TryAdd(new DecisionCandidate(intent, observation.ResourceIndex, default, score));
            }
        }

        private static float ResourceUtility(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceState resource,
            float distance,
            CreatureObservation threat,
            float threatIntensity)
        {
            bool seekingWater = intent == CreatureIntent.SeekWater;
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float missing = Math.Max(0.0001f, capacity - current);
            float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
            float perUnitGain = seekingWater ? 20f : 20f * phenotype.FoodYield * resource.NutritionMultiplier;
            float needGain = Math.Min(1f, (resource.Amount * perUnitGain) / missing);
            float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
            float dangerPenalty = threat.IsValid ? Math.Max(0f, threatIntensity) * genome.RiskAversion * (distance / Math.Max(0.01f, phenotype.MaximumSpeed)) : 0f;
            return Math.Max(0f, (urgency * needGain) - travelBurden - dangerPenalty);
        }

        private static float EstimateTravelBurden(float distance, Phenotype phenotype)
        {
            float safeDistance = Math.Max(0f, distance);
            float travelTime = safeDistance / Math.Max(0.01f, phenotype.MaximumSpeed);
            float energyCost = (travelTime * phenotype.BasalEnergyCostMultiplier) + (safeDistance * phenotype.BodyMass * 0.5f);
            float hydrationCost = travelTime * phenotype.BodyMass * phenotype.DigestionRate * phenotype.WaterLossMultiplier * 0.75f;
            return 0.5f * ((energyCost / Math.Max(0.01f, phenotype.EnergyCapacity)) + (hydrationCost / Math.Max(0.01f, phenotype.HydrationCapacity)));
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation food,
            ResourceObservation water)
        {
            return Decide(needs, phenotype, food, water, out _);
        }

        public static CreatureDecision PreferRememberedResource(
            CreatureNeeds needs,
            Phenotype phenotype,
            MemoryState memory,
            CreatureDecision currentDecision,
            out SimVector2 rememberedTarget)
        {
            rememberedTarget = default;
            if (currentDecision.Action != CreatureAction.Wander)
            {
                return currentDecision;
            }

            float foodValue = KnownOutcomeOrCuriosity(memory.FoodOutcomeValue, memory.FoodExperienceCount, phenotype.Exploration);
            float waterValue = KnownOutcomeOrCuriosity(memory.WaterOutcomeValue, memory.WaterExperienceCount, phenotype.Exploration);
            float foodScore = Urgency(needs.Energy, phenotype.EnergyCapacity) * memory.FoodConfidence * foodValue;
            float waterScore = Urgency(needs.Hydration, phenotype.HydrationCapacity) * memory.WaterConfidence * waterValue;
            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return currentDecision;
            }

            if (waterScore > foodScore)
            {
                rememberedTarget = memory.WaterPosition;
                return new CreatureDecision(CreatureAction.SeekWater, -1, waterScore);
            }

            rememberedTarget = memory.FoodPosition;
            return new CreatureDecision(CreatureAction.SeekFood, -1, foodScore);
        }

        public static CreatureDecision DecideFromLearnedOutcomes(
            CreatureNeeds needs,
            Phenotype phenotype,
            MemoryState memory,
            ResourceObservation food,
            ResourceObservation water,
            out DecisionDiagnostics diagnostics)
        {
            float foodValue = KnownOutcomeOrCuriosity(memory.FoodOutcomeValue, memory.FoodExperienceCount, phenotype.Exploration);
            float waterValue = KnownOutcomeOrCuriosity(memory.WaterOutcomeValue, memory.WaterExperienceCount, phenotype.Exploration);
            float foodScore = food.IsValid ? Urgency(needs.Energy, phenotype.EnergyCapacity) * Availability(food.Distance) * foodValue : -1f;
            float waterScore = water.IsValid ? Urgency(needs.Hydration, phenotype.HydrationCapacity) * Availability(water.Distance) * waterValue : -1f;
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

        private static float KnownOutcomeOrCuriosity(float learnedValue, int experienceCount, float exploration)
        {
            return experienceCount > 0 && learnedValue >= 0.05f
                ? learnedValue
                : 0.5f + (0.5f * exploration);
        }
    }
}
