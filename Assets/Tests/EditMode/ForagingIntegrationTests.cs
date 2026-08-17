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
        public void ConstructingAGenomeWithoutSpecifyingPersistenceDefaultsToOneHalf()
        {
            // Persistence's default moved from 0f to 0.5f to match its sibling behavioral
            // genes (urgencyExponent, travelSensitivity, riskAversion, commitment) and to
            // avoid the clamp-boundary drift a gene pinned at exactly 0 would show under
            // symmetric mutation.
            Genome genome = Genome.Neutral;

            Assert.That(genome.Persistence, Is.EqualTo(0.5f));
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
        public void CreatureOnAPatchDrainingBelowItsRecentAverageStopsTargetingItWithinAFewDecisionTicks()
        {
            var schedule = new SimulationSchedule(20, 20, 4, 20, 20, 20, 1, 1);
            // A tiny referenceGain keeps PatchScore saturated near its maximum for any
            // positive remaining amount, so a merely-thin patch never gets deselected by
            // ordinary scoring alone; only ShouldAbandon (driven by the realized intake
            // rate, not the score) can make this creature stop targeting it.
            var config = new SimulationConfig(42, 1, schedule, foragingEconomicsEnabled: true, referenceGain: 0.001f);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));

            // Pinned at zero every tick so urgency stays maxed throughout: the point of
            // this test is a draining patch, not a creature that got full and wandered
            // off for an unrelated reason. Position is re-pinned too, so a Wander tick
            // before the resource grid first sees the patch can't drift it away.
            //
            // Capacity is tiny and regen is fast: the creature's per-tick request
            // (~0.055) always exceeds the 0.02 available, so it only ever gets a
            // trickle, and regen tops the patch straight back up to that same 0.02
            // before the next tick. The patch never actually empties, so PatchScore
            // (saturated by the tiny referenceGain above) rates it as excellent forever;
            // only ShouldAbandon, watching the realized trickle, can make the creature
            // stop targeting it.
            ResourceId foodId = world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 0.02f, 0.02f, 10f);
            for (int tick = 0; tick < 10; tick++)
            {
                world.Creatures.GetNeedsRefAt(0).Energy = 0f;
                world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Eat));

            // Simulate "was recently doing well" against the trickle already running
            // underneath (which never gave it anywhere near this rate).
            world.Creatures.GetForagingRefAt(0).RecentIntakeRate = 5f;

            bool stoppedTargetingThePatch = false;
            for (int tick = 0; tick < 5; tick++)
            {
                world.Creatures.GetNeedsRefAt(0).Energy = 0f;
                world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
                world.Step(config.FixedDeltaTime);
                CreatureDecision decision = world.Creatures.GetDecisionAt(0);
                if (decision.TargetResourceIndex < 0)
                {
                    stoppedTargetingThePatch = true;
                    break;
                }
            }

            Assert.That(stoppedTargetingThePatch, Is.True);
        }

        [Test]
        public void TwoCreaturesDifferingOnlyInPersistenceTheHigherPersistenceOneAbandonsLater()
        {
            var schedule = new SimulationSchedule(20, 20, 4, 20, 20, 20, 1, 1);
            var lowPersistenceGenome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0f);
            var highPersistenceGenome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, persistence: 0.9f);
            // A tiny referenceGain keeps PatchScore saturated for any positive remaining
            // amount (see the comment in the single-creature test above). A near-zero
            // commitmentStrength neutralizes Task 5's CommitmentBonus, which also scales
            // with Persistence and would otherwise confound "who keeps targeting longer"
            // with a second, unrelated mechanism.
            var config = new SimulationConfig(42, 0, schedule, foragingEconomicsEnabled: true, referenceGain: 0.001f, commitmentStrength: 0.0001f);
            var world = new SimulationWorld(config);
            CreatureId lowId = world.Spawn(lowPersistenceGenome);
            CreatureId highId = world.Spawn(highPersistenceGenome);
            ResourceId lowFoodId = world.Resources.Add(ResourceKind.Food, new SimVector2(-15f, 0f), 1f, 1000f, 1000f, 0f);
            ResourceId highFoodId = world.Resources.Add(ResourceKind.Food, new SimVector2(15f, 0f), 1f, 1000f, 1000f, 0f);
            world.SetCreaturePosition(lowId, new SimVector2(-15f, 0f));
            world.SetCreaturePosition(highId, new SimVector2(15f, 0f));

            // Pinned at zero every tick so urgency stays maxed throughout for both
            // creatures: only the intake-rate/persistence comparison should decide who
            // abandons first, not one of them getting full first.
            for (int tick = 0; tick < 10; tick++)
            {
                world.Creatures.GetNeedsRefAt(0).Energy = 0f;
                world.Creatures.GetNeedsRefAt(1).Energy = 0f;
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Eat));
            Assert.That(world.Creatures.GetDecisionAt(1).Action, Is.EqualTo(CreatureAction.Eat));

            // Both start from the same artificially high "recently was doing great"
            // baseline, then both patches collapse to the same small trickle in the
            // same tick. The low-persistence creature's give-up threshold is high
            // enough to trip on that first trickle; the high-persistence one's is not,
            // so it keeps targeting until the patch is truly empty a tick later.
            world.Creatures.GetForagingRefAt(0).RecentIntakeRate = 5f;
            world.Creatures.GetForagingRefAt(1).RecentIntakeRate = 5f;
            world.Resources.SetFoodProjection(lowFoodId, 0.05f, 1f);
            world.Resources.SetFoodProjection(highFoodId, 0.05f, 1f);

            int lowAbandonTick = -1;
            int highAbandonTick = -1;
            for (int tick = 0; tick < 10 && (lowAbandonTick < 0 || highAbandonTick < 0); tick++)
            {
                world.Creatures.GetNeedsRefAt(0).Energy = 0f;
                world.Creatures.GetNeedsRefAt(1).Energy = 0f;
                world.Step(config.FixedDeltaTime);
                if (lowAbandonTick < 0 && world.Creatures.GetDecisionAt(0).TargetResourceIndex < 0)
                {
                    lowAbandonTick = tick;
                }
                if (highAbandonTick < 0 && world.Creatures.GetDecisionAt(1).TargetResourceIndex < 0)
                {
                    highAbandonTick = tick;
                }
            }

            Assert.That(lowAbandonTick, Is.GreaterThanOrEqualTo(0));
            Assert.That(highAbandonTick, Is.GreaterThanOrEqualTo(0));
            Assert.That(highAbandonTick, Is.GreaterThan(lowAbandonTick));
        }

        [Test]
        public void CreatureOnARichPatchDoesNotAbandon()
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

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Eat));
        }

        [Test]
        public void FewerStarvationDeathsOccurNearADepletedResourceWithTheFlagOnThanOff()
        {
            // Five creatures start starving, colocated with a food patch (A) that is
            // pinned to a near-zero amount every tick for the whole run -- it never
            // recovers, but it also never reads as literally empty (Amount stays
            // above zero), so the legacy nearest-available search (flag off) keeps
            // treating it as the best -- indeed the only -- option it ever considers,
            // since legacy never compares alternatives. A second, ample patch (B) sits
            // two units away, an easy net-positive trip. With the flag on, scoring
            // recognises A's near-zero remaining amount is worthless next to B's and
            // redirects every creature to B before health decay (needs ~560 ticks from
            // full health once Energy hits zero) runs out; with the flag off, nobody
            // ever looks past A and all five starve.
            int off = RunToStarvationCount(foragingEconomicsEnabled: false);
            int on = RunToStarvationCount(foragingEconomicsEnabled: true);

            Assert.That(off, Is.EqualTo(5), "expected the flag-off run to lose the whole population to starvation");
            Assert.That(on, Is.LessThan(off));
        }

        private static int RunToStarvationCount(bool foragingEconomicsEnabled)
        {
            const int population = 5;
            var schedule = new SimulationSchedule(20, 20, 4, 20, 20, 20, 1, 1);
            var config = new SimulationConfig(42, 0, schedule, foragingEconomicsEnabled: foragingEconomicsEnabled);
            var world = new SimulationWorld(config);

            for (int i = 0; i < population; i++)
            {
                CreatureId id = world.Spawn();
                world.SetCreaturePosition(id, new SimVector2(0f, 0f));
                world.Creatures.GetNeedsRefAt(i).Energy = 0f;
            }

            ResourceId depletedFood = world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 1000f, 1000f, 0f);
            world.Resources.Add(ResourceKind.Water, new SimVector2(0f, 0f), 1f, 10000f, 10000f, 1000f);
            world.Resources.Add(ResourceKind.Food, new SimVector2(2f, 0f), 1f, 1000f, 1000f, 0f);

            for (int tick = 0; tick < 900; tick++)
            {
                world.Resources.SetFoodProjection(depletedFood, 0.002f, 1f);
                world.Step(config.FixedDeltaTime);
            }

            return world.Statistics.StarvationDeathCount;
        }

        [Test]
        public void WithForagingEconomicsDisabledADepletedPatchNeverConsultsRecentIntakeRate()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(42, 1);
            var config = new SimulationConfig(42, 1, defaults.Schedule);
            var world = new SimulationWorld(config);
            world.Resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 1000f, 1000f, 0f);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            ref CreatureNeeds needs = ref world.Creatures.GetNeedsRefAt(0);
            needs.Energy = 0f;

            for (int tick = 0; tick < 40; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Eat));

            // With the flag off, ForagingState is never touched by SimulationWorld, but
            // nothing stops a test (or future caller) from setting it directly. Even so,
            // ShouldAbandon must never be consulted while the flag is off: an elevated
            // RecentIntakeRate that would trigger abandonment if the flag were on must
            // have no effect while the patch itself remains rich.
            world.Creatures.GetForagingRefAt(0).RecentIntakeRate = 1000f;

            for (int tick = 0; tick < 10; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Eat));
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
