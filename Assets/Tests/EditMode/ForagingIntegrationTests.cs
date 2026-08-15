using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
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

        [Test]
        public void SecondsInCurrentActionGrowsByTheElapsedTimeWhileTheActionIsUnchanged()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var config = new SimulationConfig(42, 1, defaults.Schedule, foragingEconomicsEnabled: true);
            var world = new SimulationWorld(config);

            for (int tick = 0; tick < 10; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Wander));
            ForagingState state = world.Creatures.GetForagingRefAt(0);
            Assert.That(state.SecondsInCurrentAction, Is.EqualTo(10 * config.FixedDeltaTime).Within(0.0001f));
        }

        [Test]
        public void SecondsInCurrentActionResetsToZeroOnTheTickTheActionChanges()
        {
            var schedule = new SimulationSchedule(20, 20, 4, 2, 20, 1, 1, 1);
            var config = new SimulationConfig(42, 1, schedule, foragingEconomicsEnabled: true);
            var world = new SimulationWorld(config);
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 10f, 10f, 0f);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;

            // PerceptionHz is 4 (interval 5 ticks at base 20Hz), so the resource
            // grid only sees the food once that perception tick lands.
            for (int tick = 0; tick < 5; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.Not.EqualTo(CreatureAction.Wander));
            ForagingState state = world.Creatures.GetForagingRefAt(0);
            Assert.That(state.SecondsInCurrentAction, Is.EqualTo(0f));
        }

        [Test]
        public void RecentIntakeRateRisesWhileACreatureEats()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var config = new SimulationConfig(42, 1, defaults.Schedule, foragingEconomicsEnabled: true);
            var world = new SimulationWorld(config);
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 1000f, 1000f, 0f);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;

            for (int tick = 0; tick < 40; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            ForagingState state = world.Creatures.GetForagingRefAt(0);
            Assert.That(state.RecentIntakeRate, Is.GreaterThan(0f));
        }

        [Test]
        public void RecentIntakeRateDecaysTowardZeroAfterACreatureStopsEating()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var config = new SimulationConfig(42, 1, defaults.Schedule, foragingEconomicsEnabled: true);
            var world = new SimulationWorld(config);
            ResourceId foodId = world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 1000f, 1000f, 0f);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;

            for (int tick = 0; tick < 20; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            float rateWhileEating = world.Creatures.GetForagingRefAt(0).RecentIntakeRate;
            Assert.That(rateWhileEating, Is.GreaterThan(0f));

            // Take the food away so ResolveResourceInteractions can no longer grant it,
            // regardless of what the creature's stale decision still targets.
            world.Resources.SetActive(foodId, false);

            for (int tick = 0; tick < 20; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            float rateAfterStopping = world.Creatures.GetForagingRefAt(0).RecentIntakeRate;
            Assert.That(rateAfterStopping, Is.LessThan(rateWhileEating));
        }

        [Test]
        public void ValidateRejectsAnyOfTheFiveForagingConstantsAtZeroOrBelow()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 1);
            SimulationSchedule schedule = defaults.Schedule;

            Assert.That(() => new SimulationConfig(42, 1, schedule, handlingSeconds: 0f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, handlingSeconds: -1f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, referenceGain: 0f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, referenceGain: -1f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, commitmentStrength: 0f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, commitmentStrength: -1f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, commitmentHalfLifeSeconds: 0f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, commitmentHalfLifeSeconds: -1f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, giveUpSensitivity: 0f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SimulationConfig(42, 1, schedule, giveUpSensitivity: -1f).Validate(), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ForagingStateStaysAtZeroWhenTheFlagIsOff()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var world = new SimulationWorld(config);
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 1000f, 1000f, 0f);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;

            Assert.That(config.ForagingEconomicsEnabled, Is.False);

            for (int tick = 0; tick < 40; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            ForagingState state = world.Creatures.GetForagingRefAt(0);
            Assert.That(state.SecondsInCurrentAction, Is.EqualTo(0f));
            Assert.That(state.RecentIntakeRate, Is.EqualTo(0f));
        }
    }
}
