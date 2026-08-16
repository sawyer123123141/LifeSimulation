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

        /// <summary>
        /// Converts a raw intake rate into a [0, 1] learning-signal outcome by measuring it against
        /// <paramref name="expectedIntakeRate"/> - the rate a fully-fed creature at an ideal place would
        /// achieve - instead of clamping a raw nutrition amount to 1.0. A typical feeding rarely reaches
        /// that ceiling, so the result lands strictly below 1.0 and carries real information about how
        /// good the place was, rather than saturating on nearly every feeding event.
        /// </summary>
        public static float ComputeIntakeOutcome(float actualIntakeRate, float expectedIntakeRate)
        {
            if (actualIntakeRate < 0f || float.IsNaN(actualIntakeRate) || float.IsInfinity(actualIntakeRate))
            {
                throw new ArgumentOutOfRangeException(nameof(actualIntakeRate));
            }

            if (expectedIntakeRate <= 0f || float.IsNaN(expectedIntakeRate) || float.IsInfinity(expectedIntakeRate))
            {
                throw new ArgumentOutOfRangeException(nameof(expectedIntakeRate));
            }

            return Math.Min(1f, actualIntakeRate / expectedIntakeRate);
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

        /// <summary>
        /// Records an observed place into a creature's place-memory slots. If the place is already
        /// remembered - same <paramref name="kind"/>, within <paramref name="samePlaceRadius"/> of an
        /// occupied slot - that slot is refreshed in place. Otherwise the observation fills a free slot,
        /// or, if every usable slot is occupied, evicts the slot with the lowest <c>Confidence x OutcomeValue</c>
        /// (ties broken by lowest <c>LastSeenTick</c>, then lowest slot index).
        /// </summary>
        public static void ObservePlace(
            CreatureStore store,
            int creatureIndex,
            int usableSlotCount,
            SimVector2 position,
            ResourceKind kind,
            float amount,
            long tick,
            float samePlaceRadius)
        {
            if (usableSlotCount < 0 || usableSlotCount > store.MaximumMemorySlots)
            {
                throw new ArgumentOutOfRangeException(nameof(usableSlotCount));
            }

            if (samePlaceRadius < 0f || float.IsNaN(samePlaceRadius) || float.IsInfinity(samePlaceRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(samePlaceRadius));
            }

            for (int slot = 0; slot < usableSlotCount; slot++)
            {
                ref PlaceMemory candidate = ref store.GetPlaceMemoryRefAt(creatureIndex, slot);
                if (candidate.VisitCount > 0
                    && candidate.Kind == kind
                    && SimVector2.Distance(candidate.Position, position) <= samePlaceRadius)
                {
                    RefreshSlot(ref candidate, position, amount, tick);
                    return;
                }
            }

            for (int slot = 0; slot < usableSlotCount; slot++)
            {
                ref PlaceMemory candidate = ref store.GetPlaceMemoryRefAt(creatureIndex, slot);
                if (candidate.VisitCount == 0)
                {
                    OccupySlot(ref candidate, position, kind, amount, tick);
                    return;
                }
            }

            if (usableSlotCount == 0)
            {
                return;
            }

            int evictionSlot = FindEvictionSlot(store, creatureIndex, usableSlotCount);
            ref PlaceMemory evicted = ref store.GetPlaceMemoryRefAt(creatureIndex, evictionSlot);
            OccupySlot(ref evicted, position, kind, amount, tick);
        }

        private static void RefreshSlot(ref PlaceMemory memory, SimVector2 position, float amount, long tick)
        {
            memory.Position = position;
            memory.LastKnownAmount = amount;
            memory.VisitCount++;
            memory.Confidence = 1f;
            memory.LastSeenTick = tick;
        }

        private static void OccupySlot(ref PlaceMemory memory, SimVector2 position, ResourceKind kind, float amount, long tick)
        {
            memory.Position = position;
            memory.Kind = kind;
            memory.LastKnownAmount = amount;
            memory.OutcomeValue = 0f;
            memory.VisitCount = 1;
            memory.Confidence = 1f;
            memory.LastSeenTick = tick;
        }

        /// <summary>
        /// Reduces every place-memory slot's <c>Confidence</c> by <paramref name="deltaTime"/> multiplied by
        /// <paramref name="confidenceDecayPerSecond"/>, clamped at zero. A creature's decay rate already
        /// reflects its <c>MemoryRetention</c> - a higher-retention creature is passed a smaller rate, so its
        /// confidences fall more slowly.
        /// </summary>
        public static void TickPlaceMemoryDecay(
            CreatureStore store,
            int creatureIndex,
            int usableSlotCount,
            float deltaTime,
            float confidenceDecayPerSecond)
        {
            if (usableSlotCount < 0 || usableSlotCount > store.MaximumMemorySlots)
            {
                throw new ArgumentOutOfRangeException(nameof(usableSlotCount));
            }

            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (confidenceDecayPerSecond < 0f || float.IsNaN(confidenceDecayPerSecond) || float.IsInfinity(confidenceDecayPerSecond))
            {
                throw new ArgumentOutOfRangeException(nameof(confidenceDecayPerSecond));
            }

            float confidenceLoss = deltaTime * confidenceDecayPerSecond;

            for (int slot = 0; slot < usableSlotCount; slot++)
            {
                ref PlaceMemory memory = ref store.GetPlaceMemoryRefAt(creatureIndex, slot);
                memory.Confidence = Math.Max(0f, memory.Confidence - confidenceLoss);
            }
        }

        /// <summary>
        /// Sharply cuts the <c>Confidence</c> of the single occupied place-memory slot matching
        /// <paramref name="kind"/> within <paramref name="samePlaceRadius"/> of <paramref name="position"/> -
        /// the specific place a creature travelled to and found empty. Other slots, including ones for the
        /// same resource kind at other places, are left untouched. If no matching slot is found, this is a no-op.
        /// </summary>
        public static void RecordFailedPlaceSearch(
            CreatureStore store,
            int creatureIndex,
            int usableSlotCount,
            SimVector2 position,
            ResourceKind kind,
            float samePlaceRadius)
        {
            if (usableSlotCount < 0 || usableSlotCount > store.MaximumMemorySlots)
            {
                throw new ArgumentOutOfRangeException(nameof(usableSlotCount));
            }

            if (samePlaceRadius < 0f || float.IsNaN(samePlaceRadius) || float.IsInfinity(samePlaceRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(samePlaceRadius));
            }

            for (int slot = 0; slot < usableSlotCount; slot++)
            {
                ref PlaceMemory candidate = ref store.GetPlaceMemoryRefAt(creatureIndex, slot);
                if (candidate.VisitCount > 0
                    && candidate.Kind == kind
                    && SimVector2.Distance(candidate.Position, position) <= samePlaceRadius)
                {
                    candidate.Confidence *= 0.35f;
                    return;
                }
            }
        }

        private static int FindEvictionSlot(CreatureStore store, int creatureIndex, int usableSlotCount)
        {
            int bestSlot = 0;
            PlaceMemory best = store.GetPlaceMemoryRefAt(creatureIndex, 0);
            float bestValue = best.Confidence * best.OutcomeValue;

            for (int slot = 1; slot < usableSlotCount; slot++)
            {
                PlaceMemory current = store.GetPlaceMemoryRefAt(creatureIndex, slot);
                float currentValue = current.Confidence * current.OutcomeValue;

                if (currentValue < bestValue
                    || (currentValue == bestValue && current.LastSeenTick < best.LastSeenTick))
                {
                    bestSlot = slot;
                    best = current;
                    bestValue = currentValue;
                }
            }

            return bestSlot;
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
