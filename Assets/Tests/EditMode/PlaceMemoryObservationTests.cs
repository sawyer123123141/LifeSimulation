using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PlaceMemoryObservationTests
    {
        private const float SamePlaceRadius = 2f;

        [Test]
        public void ObservingANewPlaceWithAFreeSlotOccupiesItAndLeavesOthersUntouched()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 3);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            ref PlaceMemory occupied = ref store.GetPlaceMemoryRefAt(index, 0);
            occupied.Position = new SimVector2(50f, 50f);
            occupied.Kind = ResourceKind.Water;
            occupied.VisitCount = 4;
            occupied.Confidence = 0.5f;
            occupied.OutcomeValue = 0.5f;
            occupied.LastSeenTick = 10;

            MemorySystem.ObservePlace(store, index, usableSlotCount: 3, new SimVector2(1f, 1f), ResourceKind.Food, amount: 8f, tick: 20, SamePlaceRadius);

            PlaceMemory newSlot = store.GetPlaceMemoryRefAt(index, 1);
            Assert.That(newSlot.Position.X, Is.EqualTo(1f));
            Assert.That(newSlot.Position.Y, Is.EqualTo(1f));
            Assert.That(newSlot.Kind, Is.EqualTo(ResourceKind.Food));
            Assert.That(newSlot.LastKnownAmount, Is.EqualTo(8f));
            Assert.That(newSlot.VisitCount, Is.EqualTo(1));
            Assert.That(newSlot.Confidence, Is.EqualTo(1f));
            Assert.That(newSlot.LastSeenTick, Is.EqualTo(20L));

            PlaceMemory untouchedFreeSlot = store.GetPlaceMemoryRefAt(index, 2);
            Assert.That(untouchedFreeSlot.VisitCount, Is.EqualTo(0));

            PlaceMemory stillOccupied = store.GetPlaceMemoryRefAt(index, 0);
            Assert.That(stillOccupied.Kind, Is.EqualTo(ResourceKind.Water));
            Assert.That(stillOccupied.VisitCount, Is.EqualTo(4));
        }

        [Test]
        public void ReObservingAKnownPlaceUpdatesItInsteadOfCreatingADuplicate()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 3);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            ref PlaceMemory known = ref store.GetPlaceMemoryRefAt(index, 0);
            known.Position = new SimVector2(10f, 10f);
            known.Kind = ResourceKind.Food;
            known.LastKnownAmount = 3f;
            known.OutcomeValue = 0.7f;
            known.VisitCount = 2;
            known.Confidence = 0.4f;
            known.LastSeenTick = 5;

            MemorySystem.ObservePlace(store, index, usableSlotCount: 3, new SimVector2(10.5f, 10.5f), ResourceKind.Food, amount: 9f, tick: 30, SamePlaceRadius);

            PlaceMemory updated = store.GetPlaceMemoryRefAt(index, 0);
            Assert.That(updated.Position.X, Is.EqualTo(10.5f));
            Assert.That(updated.Position.Y, Is.EqualTo(10.5f));
            Assert.That(updated.LastKnownAmount, Is.EqualTo(9f));
            Assert.That(updated.VisitCount, Is.EqualTo(3));
            Assert.That(updated.Confidence, Is.EqualTo(1f));
            Assert.That(updated.LastSeenTick, Is.EqualTo(30L));
            Assert.That(updated.OutcomeValue, Is.EqualTo(0.7f), "re-observing must not clobber the learned outcome value");

            PlaceMemory otherSlot = store.GetPlaceMemoryRefAt(index, 1);
            Assert.That(otherSlot.VisitCount, Is.EqualTo(0));
        }

        [Test]
        public void ObservingWithAllSlotsFullEvictsTheLowestConfidenceTimesOutcomeValueEntry()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 3);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            SetSlot(store, index, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 0.8f, outcomeValue: 0.8f, lastSeenTick: 10); // product 0.64
            SetSlot(store, index, 1, new SimVector2(20f, 20f), ResourceKind.Water, confidence: 0.2f, outcomeValue: 0.3f, lastSeenTick: 15); // product 0.06 - lowest
            SetSlot(store, index, 2, new SimVector2(40f, 40f), ResourceKind.Food, confidence: 0.9f, outcomeValue: 0.9f, lastSeenTick: 20); // product 0.81

            MemorySystem.ObservePlace(store, index, usableSlotCount: 3, new SimVector2(99f, 99f), ResourceKind.Water, amount: 5f, tick: 99, SamePlaceRadius);

            PlaceMemory evicted = store.GetPlaceMemoryRefAt(index, 1);
            Assert.That(evicted.Position.X, Is.EqualTo(99f));
            Assert.That(evicted.Kind, Is.EqualTo(ResourceKind.Water));
            Assert.That(evicted.LastSeenTick, Is.EqualTo(99L));
            Assert.That(evicted.OutcomeValue, Is.EqualTo(0f), "the evicted slot's stale learned value must not survive");

            PlaceMemory untouchedLow = store.GetPlaceMemoryRefAt(index, 0);
            Assert.That(untouchedLow.LastSeenTick, Is.EqualTo(10L));

            PlaceMemory untouchedHigh = store.GetPlaceMemoryRefAt(index, 2);
            Assert.That(untouchedHigh.LastSeenTick, Is.EqualTo(20L));
        }

        [Test]
        public void TwoEntriesTiedOnConfidenceTimesOutcomeValueEvictTheOneWithTheOlderLastSeenTick()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 2);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            SetSlot(store, index, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 0.5f, outcomeValue: 0.4f, lastSeenTick: 50); // product 0.2, newer
            SetSlot(store, index, 1, new SimVector2(20f, 20f), ResourceKind.Water, confidence: 0.4f, outcomeValue: 0.5f, lastSeenTick: 10); // product 0.2, older - evicted

            MemorySystem.ObservePlace(store, index, usableSlotCount: 2, new SimVector2(5f, 5f), ResourceKind.Food, amount: 1f, tick: 100, SamePlaceRadius);

            PlaceMemory evicted = store.GetPlaceMemoryRefAt(index, 1);
            Assert.That(evicted.LastSeenTick, Is.EqualTo(100L));

            PlaceMemory kept = store.GetPlaceMemoryRefAt(index, 0);
            Assert.That(kept.LastSeenTick, Is.EqualTo(50L));
        }

        [Test]
        public void ReplayingTheSameObservationSequenceProducesByteIdenticalSlotContents()
        {
            var firstStore = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 3);
            CreatureId firstId = firstStore.Add();
            firstStore.TryGetIndex(firstId, out int firstIndex);

            var secondStore = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 3);
            CreatureId secondId = secondStore.Add();
            secondStore.TryGetIndex(secondId, out int secondIndex);

            (SimVector2 position, ResourceKind kind, float amount, long tick)[] sequence =
            {
                (new SimVector2(1f, 1f), ResourceKind.Food, 4f, 1L),
                (new SimVector2(30f, 30f), ResourceKind.Water, 6f, 2L),
                (new SimVector2(60f, 60f), ResourceKind.Food, 2f, 3L),
                (new SimVector2(1.2f, 1.1f), ResourceKind.Food, 5f, 4L),
                (new SimVector2(90f, 90f), ResourceKind.Water, 1f, 5L),
            };

            foreach (var observation in sequence)
            {
                MemorySystem.ObservePlace(firstStore, firstIndex, usableSlotCount: 3, observation.position, observation.kind, observation.amount, observation.tick, SamePlaceRadius);
                MemorySystem.ObservePlace(secondStore, secondIndex, usableSlotCount: 3, observation.position, observation.kind, observation.amount, observation.tick, SamePlaceRadius);
            }

            for (int slot = 0; slot < 3; slot++)
            {
                PlaceMemory first = firstStore.GetPlaceMemoryRefAt(firstIndex, slot);
                PlaceMemory second = secondStore.GetPlaceMemoryRefAt(secondIndex, slot);

                Assert.That(second.Position.X, Is.EqualTo(first.Position.X));
                Assert.That(second.Position.Y, Is.EqualTo(first.Position.Y));
                Assert.That(second.Kind, Is.EqualTo(first.Kind));
                Assert.That(second.LastKnownAmount, Is.EqualTo(first.LastKnownAmount));
                Assert.That(second.OutcomeValue, Is.EqualTo(first.OutcomeValue));
                Assert.That(second.VisitCount, Is.EqualTo(first.VisitCount));
                Assert.That(second.Confidence, Is.EqualTo(first.Confidence));
                Assert.That(second.LastSeenTick, Is.EqualTo(first.LastSeenTick));
            }
        }

        [Test]
        public void AHighValueEvictionCandidateIsNotEvictedEvenIfItIsTheOldest()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 2);
            CreatureId id = store.Add();
            store.TryGetIndex(id, out int index);

            SetSlot(store, index, 0, new SimVector2(0f, 0f), ResourceKind.Food, confidence: 1f, outcomeValue: 1f, lastSeenTick: 1); // oldest but highest value, product 1.0
            SetSlot(store, index, 1, new SimVector2(20f, 20f), ResourceKind.Water, confidence: 0.5f, outcomeValue: 0.5f, lastSeenTick: 100); // newer but lower value, product 0.25 - evicted

            MemorySystem.ObservePlace(store, index, usableSlotCount: 2, new SimVector2(5f, 5f), ResourceKind.Food, amount: 1f, tick: 200, SamePlaceRadius);

            PlaceMemory highValueUntouched = store.GetPlaceMemoryRefAt(index, 0);
            Assert.That(highValueUntouched.LastSeenTick, Is.EqualTo(1L));
            Assert.That(highValueUntouched.OutcomeValue, Is.EqualTo(1f));

            PlaceMemory evicted = store.GetPlaceMemoryRefAt(index, 1);
            Assert.That(evicted.LastSeenTick, Is.EqualTo(200L));
        }

        private static void SetSlot(
            CreatureStore store,
            int index,
            int slot,
            SimVector2 position,
            ResourceKind kind,
            float confidence,
            float outcomeValue,
            long lastSeenTick)
        {
            ref PlaceMemory memory = ref store.GetPlaceMemoryRefAt(index, slot);
            memory.Position = position;
            memory.Kind = kind;
            memory.LastKnownAmount = 1f;
            memory.OutcomeValue = outcomeValue;
            memory.VisitCount = 1;
            memory.Confidence = confidence;
            memory.LastSeenTick = lastSeenTick;
        }
    }
}
