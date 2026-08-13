using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Behavior
{
    public static class MemorySystem
    {
        public static void RememberResource(ref MemoryState memory, ResourceKind kind, SimVector2 position)
        {
            switch (kind)
            {
                case ResourceKind.Food:
                    memory.FoodPosition = position;
                    memory.FoodConfidence = 1f;
                    memory.FoodAge = 0f;
                    break;
                case ResourceKind.Water:
                    memory.WaterPosition = position;
                    memory.WaterConfidence = 1f;
                    memory.WaterAge = 0f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), "Only renewable survival resources can be stored in this memory slot.");
            }
        }

        public static void RememberThreat(ref MemoryState memory, SimVector2 position)
        {
            memory.ThreatPosition = position;
            memory.ThreatConfidence = 1f;
            memory.ThreatAge = 0f;
        }

        public static void LearnResourceOutcome(ref MemoryState memory, ResourceKind kind, float outcome, float learningRate)
        {
            if (outcome < 0f || float.IsNaN(outcome) || float.IsInfinity(outcome))
            {
                throw new ArgumentOutOfRangeException(nameof(outcome));
            }

            if (learningRate < 0f || learningRate > 1f || float.IsNaN(learningRate) || float.IsInfinity(learningRate))
            {
                throw new ArgumentOutOfRangeException(nameof(learningRate));
            }

            if (kind == ResourceKind.Food)
            {
                memory.FoodOutcomeValue += (outcome - memory.FoodOutcomeValue) * learningRate;
                memory.FoodExperienceCount++;
                return;
            }

            if (kind == ResourceKind.Water)
            {
                memory.WaterOutcomeValue += (outcome - memory.WaterOutcomeValue) * learningRate;
                memory.WaterExperienceCount++;
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        public static void RecordFailedSearch(ref MemoryState memory, ResourceKind kind)
        {
            if (kind == ResourceKind.Food)
            {
                memory.FoodConfidence *= 0.35f;
                return;
            }

            if (kind == ResourceKind.Water)
            {
                memory.WaterConfidence *= 0.35f;
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        public static void TickDecay(ref MemoryState memory, float deltaTime, float confidenceDecayPerSecond)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (confidenceDecayPerSecond < 0f || float.IsNaN(confidenceDecayPerSecond) || float.IsInfinity(confidenceDecayPerSecond))
            {
                throw new ArgumentOutOfRangeException(nameof(confidenceDecayPerSecond));
            }

            float confidenceLoss = deltaTime * confidenceDecayPerSecond;
            memory.FoodConfidence = Math.Max(0f, memory.FoodConfidence - confidenceLoss);
            memory.WaterConfidence = Math.Max(0f, memory.WaterConfidence - confidenceLoss);
            memory.ThreatConfidence = Math.Max(0f, memory.ThreatConfidence - confidenceLoss);
            memory.FoodAge += deltaTime;
            memory.WaterAge += deltaTime;
            memory.ThreatAge += deltaTime;
        }
    }
}
