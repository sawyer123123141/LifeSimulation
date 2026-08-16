using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PlaceMemoryDecayTests
    {
        private const float SamePlaceRadius = 2f;

        [Test]
        public void TimePassingLowersEveryOccupiedSlotsConfidence()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 2);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            SetSlot(store, index, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 0.8f);
            SetSlot(store, index, 1, new SimVector2(20f, 20f), ResourceKind.Water, confidence: 0.6f);

            MemorySystem.TickPlaceMemoryDecay(store, index, usableSlotCount: 2, deltaTime: 1f, confidenceDecayPerSecond: 0.1f);

            Assert.That(store.GetPlaceMemoryRefAt(index, 0).Confidence, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(store.GetPlaceMemoryRefAt(index, 1).Confidence, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ConfidenceReachingZeroClampsAndNeverGoesNegative()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 1);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            SetSlot(store, index, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 0.2f);

            MemorySystem.TickPlaceMemoryDecay(store, index, usableSlotCount: 1, deltaTime: 10f, confidenceDecayPerSecond: 0.5f);

            Assert.That(store.GetPlaceMemoryRefAt(index, 0).Confidence, Is.EqualTo(0f));
        }

        [Test]
        public void AHigherMemoryRetentionCreatureConfidenceFallsMoreSlowly()
        {
            var lowRetentionStore = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 1);
            CreatureId lowId = lowRetentionStore.Add();
            lowRetentionStore.TryGetIndex(lowId, out int lowIndex);
            SetSlot(lowRetentionStore, lowIndex, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 1f);

            var highRetentionStore = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 1);
            CreatureId highId = highRetentionStore.Add();
            highRetentionStore.TryGetIndex(highId, out int highIndex);
            SetSlot(highRetentionStore, highIndex, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 1f);

            // A high-MemoryRetention creature's phenotype yields a smaller MemoryConfidenceDecayPerSecond.
            MemorySystem.TickPlaceMemoryDecay(lowRetentionStore, lowIndex, usableSlotCount: 1, deltaTime: 1f, confidenceDecayPerSecond: 0.12f);
            MemorySystem.TickPlaceMemoryDecay(highRetentionStore, highIndex, usableSlotCount: 1, deltaTime: 1f, confidenceDecayPerSecond: 0.02f);

            float lowRetentionConfidence = lowRetentionStore.GetPlaceMemoryRefAt(lowIndex, 0).Confidence;
            float highRetentionConfidence = highRetentionStore.GetPlaceMemoryRefAt(highIndex, 0).Confidence;

            Assert.That(highRetentionConfidence, Is.GreaterThan(lowRetentionConfidence));
        }

        [Test]
        public void DecayWithZeroElapsedTimeChangesNothing()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 1);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            SetSlot(store, index, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 0.7f);

            MemorySystem.TickPlaceMemoryDecay(store, index, usableSlotCount: 1, deltaTime: 0f, confidenceDecayPerSecond: 0.5f);

            Assert.That(store.GetPlaceMemoryRefAt(index, 0).Confidence, Is.EqualTo(0.7f));
        }

        [Test]
        public void AFailedSearchAtOnePlaceOnlyDropsThatPlacesConfidence()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 2);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            SetSlot(store, index, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 0.8f);
            SetSlot(store, index, 1, new SimVector2(20f, 20f), ResourceKind.Food, confidence: 0.8f);

            MemorySystem.RecordFailedPlaceSearch(store, index, usableSlotCount: 2, new SimVector2(0.5f, 0.5f), ResourceKind.Food, SamePlaceRadius);

            Assert.That(store.GetPlaceMemoryRefAt(index, 0).Confidence, Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(store.GetPlaceMemoryRefAt(index, 1).Confidence, Is.EqualTo(0.8f));
        }

        private static void SetSlot(
            CreatureStore store,
            int index,
            int slot,
            SimVector2 position,
            ResourceKind kind,
            float confidence)
        {
            ref PlaceMemory memory = ref store.GetPlaceMemoryRefAt(index, slot);
            memory.Position = position;
            memory.Kind = kind;
            memory.LastKnownAmount = 1f;
            memory.OutcomeValue = 0.5f;
            memory.VisitCount = 1;
            memory.Confidence = confidence;
            memory.LastSeenTick = 1;
        }
    }
}
