using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class PlaceMemoryStorageTests
    {
        private static void FillAllSlotsWithMemories(CreatureStore store, int index)
        {
            for (int slot = 0; slot < store.MaximumMemorySlots; slot++)
            {
                ref PlaceMemory memory = ref store.GetPlaceMemoryRefAt(index, slot);
                memory.Position = new SimVector2(slot + 1f, slot + 2f);
                memory.Kind = ResourceKind.Food;
                memory.LastKnownAmount = 10f;
                memory.OutcomeValue = 0.8f;
                memory.VisitCount = 4;
                memory.Confidence = 0.9f;
                memory.LastSeenTick = 100 + slot;
            }
        }

        [Test]
        public void ANewbornFromTwoParentsWithRichMemoriesHasZeroRememberedPlaces()
        {
            var store = new CreatureStore(initialCapacity: 2, maximumMemorySlots: 3);
            Genome parentGenome = new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                memoryCapacity: 1f, memoryRetention: 1f, learningRate: 1f, exploration: 1f);
            CreatureId firstParent = store.Add(parentGenome);
            CreatureId secondParent = store.Add(parentGenome);
            store.TryGetIndex(firstParent, out int firstIndex);
            store.TryGetIndex(secondParent, out int secondIndex);
            FillAllSlotsWithMemories(store, firstIndex);
            FillAllSlotsWithMemories(store, secondIndex);

            Genome childGenome = GenomeInheritance.CreateChild(parentGenome, parentGenome, worldSeed: 42, birthOrdinal: 0, mutationStandardDeviation: 0f);
            CreatureId child = store.AddChild(childGenome, new SimVector2(0f, 0f), firstParent, secondParent);

            Assert.That(store.TryGetIndex(child, out int childIndex), Is.True);
            for (int slot = 0; slot < store.MaximumMemorySlots; slot++)
            {
                PlaceMemory memory = store.GetPlaceMemoryRefAt(childIndex, slot);
                Assert.That(memory.VisitCount, Is.EqualTo(0), $"slot {slot} should be empty for a newborn");
                Assert.That(memory.Confidence, Is.EqualTo(0f), $"slot {slot} should be empty for a newborn");
                Assert.That(memory.LastSeenTick, Is.EqualTo(0L), $"slot {slot} should be empty for a newborn");
            }
        }

        [Test]
        public void ANewbornInheritsCognitionGenesFromCrossoverButNotItsParentsMemories()
        {
            var firstParent = new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                memoryCapacity: 0f, memoryRetention: 0f, learningRate: 0f, exploration: 0f);
            var secondParent = new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                memoryCapacity: 1f, memoryRetention: 1f, learningRate: 1f, exploration: 1f);

            Genome expectedChild = GenomeInheritance.CreateChild(firstParent, secondParent, worldSeed: 7, birthOrdinal: 3, mutationStandardDeviation: 0f);
            Genome actualChild = GenomeInheritance.CreateChild(firstParent, secondParent, worldSeed: 7, birthOrdinal: 3, mutationStandardDeviation: 0f);

            Assert.That(actualChild.MemoryCapacity, Is.EqualTo(expectedChild.MemoryCapacity));
            Assert.That(actualChild.MemoryRetention, Is.EqualTo(expectedChild.MemoryRetention));
            Assert.That(actualChild.LearningRate, Is.EqualTo(expectedChild.LearningRate));
            Assert.That(actualChild.Exploration, Is.EqualTo(expectedChild.Exploration));
            Assert.That(actualChild.MemoryCapacity, Is.EqualTo(firstParent.MemoryCapacity).Or.EqualTo(secondParent.MemoryCapacity));
            Assert.That(actualChild.MemoryRetention, Is.EqualTo(firstParent.MemoryRetention).Or.EqualTo(secondParent.MemoryRetention));
            Assert.That(actualChild.LearningRate, Is.EqualTo(firstParent.LearningRate).Or.EqualTo(secondParent.LearningRate));
            Assert.That(actualChild.Exploration, Is.EqualTo(firstParent.Exploration).Or.EqualTo(secondParent.Exploration));
        }

        [Test]
        public void ACreatureReusingADeadCreaturesStoreSlotCarriesOverNoRememberedPlaces()
        {
            var store = new CreatureStore(initialCapacity: 2, maximumMemorySlots: 3);
            Genome genome = new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                memoryCapacity: 1f, memoryRetention: 1f, learningRate: 1f, exploration: 1f);
            CreatureId doomed = store.Add(genome);
            CreatureId survivor = store.Add(genome);
            store.TryGetIndex(doomed, out int doomedIndex);
            FillAllSlotsWithMemories(store, doomedIndex);

            Assert.That(store.Remove(doomed), Is.True);
            store.TryGetIndex(survivor, out int survivorIndex);
            Genome childGenome = GenomeInheritance.CreateChild(genome, genome, worldSeed: 11, birthOrdinal: 1, mutationStandardDeviation: 0f);
            CreatureId replacement = store.AddChild(childGenome, new SimVector2(0f, 0f), survivor, survivor);

            Assert.That(store.TryGetIndex(replacement, out int replacementIndex), Is.True);
            for (int slot = 0; slot < store.MaximumMemorySlots; slot++)
            {
                PlaceMemory memory = store.GetPlaceMemoryRefAt(replacementIndex, slot);
                Assert.That(memory.VisitCount, Is.EqualTo(0), $"slot {slot} should not carry over the dead creature's memories");
                Assert.That(memory.Confidence, Is.EqualTo(0f), $"slot {slot} should not carry over the dead creature's memories");
                Assert.That(memory.LastSeenTick, Is.EqualTo(0L), $"slot {slot} should not carry over the dead creature's memories");
            }
        }

        [Test]
        public void NewlySpawnedCreatureSlotsAreClearedNotInheritedFromAPreviousOccupant()
        {
            var store = new CreatureStore(initialCapacity: 2, maximumMemorySlots: 4);
            CreatureId first = store.Add();
            CreatureId second = store.Add();

            ref PlaceMemory stale = ref store.GetPlaceMemoryRefAt(1, 2);
            stale.Position = new SimVector2(7f, 9f);
            stale.Kind = ResourceKind.Water;
            stale.VisitCount = 5;
            stale.Confidence = 0.9f;
            stale.LastSeenTick = 123;

            Assert.That(store.Remove(first), Is.True);
            CreatureId replacement = store.Add();

            Assert.That(store.TryGetIndex(replacement, out int replacementIndex), Is.True);
            for (int slot = 0; slot < store.MaximumMemorySlots; slot++)
            {
                PlaceMemory memory = store.GetPlaceMemoryRefAt(replacementIndex, slot);
                Assert.That(memory.VisitCount, Is.EqualTo(0), $"slot {slot} should be cleared");
                Assert.That(memory.Confidence, Is.EqualTo(0f), $"slot {slot} should be cleared");
                Assert.That(memory.LastSeenTick, Is.EqualTo(0L), $"slot {slot} should be cleared");
            }
        }

        [Test]
        public void HigherMemoryCapacityGeneYieldsMoreUsableSlotsThanALowerOne()
        {
            int lowSlotCount = SimulationConfig.ComputeMemorySlotCount(
                SimulationConfig.DefaultMinimumMemorySlots,
                SimulationConfig.DefaultAdditionalMemorySlots,
                memoryCapacityGene: 0f);
            int highSlotCount = SimulationConfig.ComputeMemorySlotCount(
                SimulationConfig.DefaultMinimumMemorySlots,
                SimulationConfig.DefaultAdditionalMemorySlots,
                memoryCapacityGene: 1f);

            Assert.That(lowSlotCount, Is.EqualTo(SimulationConfig.DefaultMinimumMemorySlots));
            Assert.That(highSlotCount, Is.EqualTo(SimulationConfig.DefaultMinimumMemorySlots + SimulationConfig.DefaultAdditionalMemorySlots));
            Assert.That(highSlotCount, Is.GreaterThan(lowSlotCount));
        }

        [Test]
        public void RemovingACreatureLeavesTheSurvivorsSlotsIntactAndOwnedByIt()
        {
            var store = new CreatureStore(initialCapacity: 2, maximumMemorySlots: 3);
            CreatureId first = store.Add();
            CreatureId moved = store.Add();

            ref PlaceMemory movedSlot = ref store.GetPlaceMemoryRefAt(1, 1);
            movedSlot.Position = new SimVector2(2f, -6f);
            movedSlot.Kind = ResourceKind.Food;
            movedSlot.LastKnownAmount = 12f;
            movedSlot.OutcomeValue = 0.6f;
            movedSlot.VisitCount = 3;
            movedSlot.Confidence = 0.75f;
            movedSlot.LastSeenTick = 42;

            Assert.That(store.Remove(first), Is.True);
            Assert.That(store.TryGetIndex(moved, out int movedIndex), Is.True);

            PlaceMemory survived = store.GetPlaceMemoryRefAt(movedIndex, 1);
            Assert.That(survived.Position.X, Is.EqualTo(2f));
            Assert.That(survived.Position.Y, Is.EqualTo(-6f));
            Assert.That(survived.Kind, Is.EqualTo(ResourceKind.Food));
            Assert.That(survived.LastKnownAmount, Is.EqualTo(12f));
            Assert.That(survived.OutcomeValue, Is.EqualTo(0.6f));
            Assert.That(survived.VisitCount, Is.EqualTo(3));
            Assert.That(survived.Confidence, Is.EqualTo(0.75f));
            Assert.That(survived.LastSeenTick, Is.EqualTo(42L));
        }

        [Test]
        public void GrowingPastInitialCapacityLosesNoSlotDataAndDoesNotShiftItBetweenCreatures()
        {
            var store = new CreatureStore(initialCapacity: 1, maximumMemorySlots: 2);
            CreatureId first = store.Add();
            ref PlaceMemory firstSlot = ref store.GetPlaceMemoryRefAt(0, 0);
            firstSlot.Position = new SimVector2(1f, 1f);
            firstSlot.VisitCount = 1;

            // Force EnsureCapacity to grow the backing arrays beyond their initial size.
            CreatureId second = store.Add();
            CreatureId third = store.Add();
            ref PlaceMemory thirdSlot = ref store.GetPlaceMemoryRefAt(2, 1);
            thirdSlot.Position = new SimVector2(9f, 9f);
            thirdSlot.VisitCount = 9;

            Assert.That(store.TryGetIndex(first, out int firstIndex), Is.True);
            Assert.That(store.TryGetIndex(second, out int secondIndex), Is.True);
            Assert.That(store.TryGetIndex(third, out int thirdIndex), Is.True);

            PlaceMemory firstAfterGrowth = store.GetPlaceMemoryRefAt(firstIndex, 0);
            Assert.That(firstAfterGrowth.Position.X, Is.EqualTo(1f));
            Assert.That(firstAfterGrowth.VisitCount, Is.EqualTo(1));

            PlaceMemory secondUntouched = store.GetPlaceMemoryRefAt(secondIndex, 0);
            Assert.That(secondUntouched.VisitCount, Is.EqualTo(0));

            PlaceMemory thirdAfterGrowth = store.GetPlaceMemoryRefAt(thirdIndex, 1);
            Assert.That(thirdAfterGrowth.Position.X, Is.EqualTo(9f));
            Assert.That(thirdAfterGrowth.VisitCount, Is.EqualTo(9));
        }
    }
}
