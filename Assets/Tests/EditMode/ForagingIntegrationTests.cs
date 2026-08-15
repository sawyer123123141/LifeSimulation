using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ForagingIntegrationTests
    {
        [Test]
        public void ConstructingAGenomeWithoutSpecifyingPersistenceDefaultsToZero()
        {
            Genome genome = Genome.Neutral;

            Assert.That(genome.Persistence, Is.EqualTo(0f));
        }

        [Test]
        public void PersistenceAboveOneIsClampedToOne()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 1.5f);

            Assert.That(genome.Persistence, Is.EqualTo(1f));
        }

        [Test]
        public void PersistenceBelowZeroIsClampedToZero()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: -0.5f);

            Assert.That(genome.Persistence, Is.EqualTo(0f));
        }

        [Test]
        public void PhenotypeFromGenomePersistenceEqualsTheGenomeValue()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0.7f);

            Phenotype phenotype = Phenotype.FromGenome(genome);

            Assert.That(phenotype.Persistence, Is.EqualTo(0.7f));
        }

        [Test]
        public void TwoGenomesDifferingOnlyInPersistenceProduceDifferentBasalEnergyCostMultiplier()
        {
            Genome lowPersistence = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0f);
            Genome highPersistence = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 1f);

            Phenotype lowPhenotype = Phenotype.FromGenome(lowPersistence);
            Phenotype highPhenotype = Phenotype.FromGenome(highPersistence);

            Assert.That(highPhenotype.BasalEnergyCostMultiplier, Is.Not.EqualTo(lowPhenotype.BasalEnergyCostMultiplier));
        }

        [Test]
        public void WithBodySizePreservesPersistence()
        {
            Genome genome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0.8f);

            Genome resized = genome.WithBodySize(0.9f);

            Assert.That(resized.Persistence, Is.EqualTo(0.8f));
        }

        [Test]
        public void NewlySpawnedCreatureHasZeroForagingState()
        {
            var store = new CreatureStore(initialCapacity: 1);
            CreatureId id = store.Add();

            Assert.That(store.TryGetIndex(id, out int index), Is.True);
            ForagingState state = store.GetForagingRefAt(index);
            Assert.That(state.SecondsInCurrentAction, Is.EqualTo(0f));
            Assert.That(state.RecentIntakeRate, Is.EqualTo(0f));
        }

        [Test]
        public void MutatingForagingStateThroughTheRefAccessorPersists()
        {
            var store = new CreatureStore(initialCapacity: 1);
            CreatureId id = store.Add();
            Assert.That(store.TryGetIndex(id, out int index), Is.True);

            ref ForagingState state = ref store.GetForagingRefAt(index);
            state.SecondsInCurrentAction = 12f;
            state.RecentIntakeRate = 3.5f;

            Assert.That(store.GetForagingRefAt(index).SecondsInCurrentAction, Is.EqualTo(12f));
            Assert.That(store.GetForagingRefAt(index).RecentIntakeRate, Is.EqualTo(3.5f));
        }

        [Test]
        public void SwapBackRemovalKeepsTheForagingSidecarAlignedWithTheMovedCreature()
        {
            var store = new CreatureStore(initialCapacity: 2);
            CreatureId first = store.Add();
            CreatureId moved = store.Add();
            ref ForagingState movedState = ref store.GetForagingRefAt(1);
            movedState.SecondsInCurrentAction = 7f;
            movedState.RecentIntakeRate = 2f;

            Assert.That(store.Remove(first), Is.True);
            Assert.That(store.TryGetIndex(moved, out int movedIndex), Is.True);
            Assert.That(store.GetForagingRefAt(movedIndex).SecondsInCurrentAction, Is.EqualTo(7f));
            Assert.That(store.GetForagingRefAt(movedIndex).RecentIntakeRate, Is.EqualTo(2f));
        }

        [Test]
        public void ForagingStateSurvivesGrowingPastInitialCapacity()
        {
            var store = new CreatureStore(initialCapacity: 1);
            _ = store.Add();
            _ = store.Add();
            CreatureId third = store.Add();

            Assert.That(store.TryGetIndex(third, out int thirdIndex), Is.True);
            ref ForagingState thirdState = ref store.GetForagingRefAt(thirdIndex);
            thirdState.SecondsInCurrentAction = 4f;
            thirdState.RecentIntakeRate = 1.2f;

            CreatureId fourth = store.Add();

            Assert.That(store.TryGetIndex(third, out thirdIndex), Is.True);
            Assert.That(store.GetForagingRefAt(thirdIndex).SecondsInCurrentAction, Is.EqualTo(4f));
            Assert.That(store.GetForagingRefAt(thirdIndex).RecentIntakeRate, Is.EqualTo(1.2f));

            Assert.That(store.TryGetIndex(fourth, out int fourthIndex), Is.True);
            Assert.That(store.GetForagingRefAt(fourthIndex).SecondsInCurrentAction, Is.EqualTo(0f));
            Assert.That(store.GetForagingRefAt(fourthIndex).RecentIntakeRate, Is.EqualTo(0f));
        }
    }
}
