using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// Covers Task 5 of the place-memory plan: remembered places (the multi-slot <see cref="PlaceMemory"/>
    /// sidecar) competing with visible ones in <see cref="SimulationWorld"/>'s decision path. Before this
    /// task, nothing in <see cref="SimulationWorld"/> ever read a place-memory slot; a creature could only
    /// fall back to the single-slot <see cref="MemoryState"/> food/water position. These tests exercise the
    /// real per-tick <see cref="SimulationWorld.Step"/> path with slots seeded directly (matching the style
    /// of <see cref="PlaceMemoryObservationTests"/> and <see cref="PlaceMemoryDecayTests"/>), since observing
    /// places during perception is not part of this task.
    /// </summary>
    public sealed class PlaceMemoryDecisionTests
    {
        private static readonly SimulationSchedule ImmediateSchedule = new SimulationSchedule(20, 20, 20, 20, 20, 20, 1, 1);

        [Test]
        public void TwoRememberedPlacesEqualDistanceDifferentValueTravelsToTheHigherValueOne()
        {
            var config = new SimulationConfig(42, 1, ImmediateSchedule, cognitionEnabled: true);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 0f;

            SeedPlace(world, 0, new SimVector2(1f, 0f), ResourceKind.Food, lastKnownAmount: 1f, confidence: 1f);
            SeedPlace(world, 1, new SimVector2(0f, 1f), ResourceKind.Food, lastKnownAmount: 10f, confidence: 1f);

            world.Step(config.FixedDeltaTime);

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(world.Creatures.GetDecisionAt(0).TargetResourceIndex, Is.LessThan(0));
            MemoryState memory = world.Creatures.GetMemoryRefAt(0);
            Assert.That(memory.HasActiveRememberedTarget, Is.True);
            Assert.That(memory.ActiveRememberedTarget.X, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(memory.ActiveRememberedTarget.Y, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void AVisiblePatchAndARememberedOneOfEqualQualityPrefersTheVisiblePatch()
        {
            var config = new SimulationConfig(42, 1, ImmediateSchedule, cognitionEnabled: true);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 0f;
            ResourceId visibleFoodId = world.Resources.Add(ResourceKind.Food, new SimVector2(2f, 0f), 1f, 10f, 10f, 0f);

            // A remembered place with a far larger LastKnownAmount than the visible patch actually
            // has - if it were allowed to compete, it would win on score alone. It must not even be
            // considered while a visible option exists.
            SeedPlace(world, 0, new SimVector2(0f, -1f), ResourceKind.Food, lastKnownAmount: 1000f, confidence: 1f);

            for (int tick = 0; tick < 3; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Resources.TryGetIndex(visibleFoodId, out int visibleFoodIndex), Is.True);
            CreatureDecision decision = world.Creatures.GetDecisionAt(0);
            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(visibleFoodIndex));
            Assert.That(world.Creatures.GetMemoryRefAt(0).HasActiveRememberedTarget, Is.False);
        }

        [Test]
        public void ARememberedPlaceWhoseConfidenceHasDecayedNearZeroIsNotPursued()
        {
            var config = new SimulationConfig(42, 1, ImmediateSchedule, cognitionEnabled: true);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 0f;

            SeedPlace(world, 0, new SimVector2(1f, 0f), ResourceKind.Food, lastKnownAmount: 10f, confidence: 0.001f);

            world.Step(config.FixedDeltaTime);

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Wander));
            Assert.That(world.Creatures.GetMemoryRefAt(0).HasActiveRememberedTarget, Is.False);
        }

        [Test]
        public void TravellingToARememberedPlaceThatIsNowEmptyArrivesFindsNothingAndItsConfidenceDrops()
        {
            var config = new SimulationConfig(42, 1, ImmediateSchedule, cognitionEnabled: true);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));

            // No resource exists at (1, 0): the remembered place is stale. MaximumSpeed for a neutral
            // genome is 2.5 units/second at a 20 Hz fixed step, so this one-unit trip takes a handful
            // of ticks; needs.Energy is pinned to 0 every tick so urgency (and therefore pursuit) never
            // lapses for an unrelated reason before arrival.
            SeedPlace(world, 0, new SimVector2(1f, 0f), ResourceKind.Food, lastKnownAmount: 10f, confidence: 1f);

            float confidenceBeforeArrival = 1f;
            bool droppedAfterArrival = false;
            for (int tick = 0; tick < 40 && !droppedAfterArrival; tick++)
            {
                world.Creatures.GetNeedsRefAt(0).Energy = 0f;
                world.Step(config.FixedDeltaTime);
                float currentConfidence = world.Creatures.GetPlaceMemoryRefAt(0, 0).Confidence;
                if (currentConfidence < confidenceBeforeArrival)
                {
                    droppedAfterArrival = true;
                }
            }

            Assert.That(droppedAfterArrival, Is.True, "the specific remembered place's confidence must drop once the creature arrives and finds nothing");
        }

        [Test]
        public void WithCognitionDisabledRememberedPlacesNeverInfluenceTheDecision()
        {
            var config = new SimulationConfig(42, 1, ImmediateSchedule, cognitionEnabled: false);
            var world = new SimulationWorld(config);
            world.SetCreaturePosition(world.GetCreatureIdAt(0), new SimVector2(0f, 0f));
            world.Creatures.GetNeedsRefAt(0).Energy = 0f;

            // Cannot seed through the store's PlaceMemory accessor: with the flag off,
            // SimulationWorld constructs CreatureStore with zero place-memory slots, so there is
            // nothing to seed - which is itself part of what "unchanged" means here.
            Assert.That(config.CognitionEnabled, Is.False);

            for (int tick = 0; tick < 5; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.Creatures.GetDecisionAt(0).Action, Is.EqualTo(CreatureAction.Wander));
        }

        private static void SeedPlace(
            SimulationWorld world,
            int slot,
            SimVector2 position,
            ResourceKind kind,
            float lastKnownAmount,
            float confidence)
        {
            ref PlaceMemory place = ref world.Creatures.GetPlaceMemoryRefAt(0, slot);
            place.Position = position;
            place.Kind = kind;
            place.LastKnownAmount = lastKnownAmount;
            place.OutcomeValue = 0.5f;
            place.VisitCount = 1;
            place.Confidence = confidence;
            place.LastSeenTick = 0;
        }
    }
}
