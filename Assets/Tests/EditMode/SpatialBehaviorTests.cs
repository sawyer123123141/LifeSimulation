using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Spatial;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class SpatialBehaviorTests
    {
        [Test]
        public void MovementSteersTowardTargetWithinSpeedAndArenaBounds()
        {
            var state = new MovementState(new SimVector2(0f, 0f));
            var arena = new ArenaBounds(-1f, 1f, -1f, 1f);

            float distance = MovementSystem.MoveToward(
                ref state,
                new SimVector2(10f, 0f),
                maximumSpeed: 2f,
                deltaTime: 0.5f,
                arena);

            Assert.That(distance, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(state.PreviousPosition.X, Is.EqualTo(0f));
            Assert.That(state.Position.X, Is.EqualTo(1f));
            Assert.That(state.Position.Y, Is.EqualTo(0f));
        }

        [Test]
        public void UniformGridGroupsDenseIndexesByBoundedCellWithoutAllocatingCandidates()
        {
            var grid = new UniformGrid(new ArenaBounds(0f, 4f, 0f, 4f), cellSize: 2f, initialOccupantCapacity: 3);
            var positions = new[]
            {
                new SimVector2(0.2f, 0.2f),
                new SimVector2(1.9f, 1.9f),
                new SimVector2(3.5f, 3.5f),
            };

            grid.Rebuild(positions, positions.Length);

            int lowerLeftCell = grid.GetCellIndex(new SimVector2(0f, 0f));
            int upperRightCell = grid.GetCellIndex(new SimVector2(4f, 4f));
            Assert.That(grid.GetCellEnd(lowerLeftCell) - grid.GetCellStart(lowerLeftCell), Is.EqualTo(2));
            Assert.That(grid.GetCellEnd(upperRightCell) - grid.GetCellStart(upperRightCell), Is.EqualTo(1));
            Assert.That(grid.GetOccupantIndexAt(grid.GetCellStart(upperRightCell)), Is.EqualTo(2));
        }

        [Test]
        public void PerceptionSelectsNearestAvailableResourceAndBreaksDistanceTiesByStableId()
        {
            var resources = new ResourceStore(initialCapacity: 3);
            ResourceId expectedFood = resources.Add(ResourceKind.Food, new SimVector2(-1f, 0f), 0.5f, 1f, 1f, 0f);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 0.5f, 1f, 1f, 0f);
            resources.Add(ResourceKind.Water, new SimVector2(0f, 1f), 0.5f, 1f, 1f, 0f);
            var positions = new[]
            {
                resources.GetAt(0).Position,
                resources.GetAt(1).Position,
                resources.GetAt(2).Position,
            };
            var grid = new UniformGrid(new ArenaBounds(-4f, 4f, -4f, 4f), 2f, initialOccupantCapacity: 3);
            grid.Rebuild(positions, positions.Length);

            ResourceObservation observation = PerceptionSystem.FindNearestAvailableResource(
                resources,
                grid,
                new SimVector2(0f, 0f),
                visionRange: 2f,
                ResourceKind.Food);

            Assert.That(observation.IsValid, Is.True);
            Assert.That(observation.ResourceId, Is.EqualTo(expectedFood));
            Assert.That(observation.Distance, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void DecisionPrefersTheMoreUrgentAvailableSurvivalNeed()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = phenotype.EnergyCapacity * 0.25f;
            needs.Hydration = phenotype.HydrationCapacity * 0.05f;
            var food = new ResourceObservation(new ResourceId(1), 0, 1f);
            var water = new ResourceObservation(new ResourceId(2), 1, 1f);

            CreatureDecision decision = DecisionSystem.Decide(needs, phenotype, food, water);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekWater));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(1));
        }

        [Test]
        public void DecisionDiagnosticsExposeTheCompetingSurvivalScores()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = phenotype.EnergyCapacity * 0.2f;
            needs.Hydration = phenotype.HydrationCapacity * 0.1f;
            var food = new ResourceObservation(new ResourceId(1), 0, 2f);
            var water = new ResourceObservation(new ResourceId(2), 1, 1f);

            CreatureDecision decision = DecisionSystem.Decide(needs, phenotype, food, water, out DecisionDiagnostics diagnostics);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekWater));
            Assert.That(diagnostics.FoodVisible, Is.True);
            Assert.That(diagnostics.WaterVisible, Is.True);
            Assert.That(diagnostics.WaterScore, Is.GreaterThan(diagnostics.FoodScore));
        }

        [Test]
        public void DecisionWandersWhenNoSurvivalNeedIsUrgent()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            var food = new ResourceObservation(new ResourceId(1), 0, 1f);
            var water = new ResourceObservation(new ResourceId(2), 1, 1f);

            CreatureDecision decision = DecisionSystem.Decide(needs, phenotype, food, water);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.Wander));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(-1));
        }

        [Test]
        public void WanderingCreatureUsesAHeadingLongEnoughToExploreMeaningfully()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype1Defaults(42, 0);
            var world = new SimulationWorld(config);
            CreatureId creature = world.Spawn();
            world.SetCreaturePosition(creature, new SimVector2(0f, 0f));

            for (int index = 0; index < 100; index++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(SimVector2.Distance(new SimVector2(0f, 0f), world.GetCreatureMovementAt(0).Position), Is.GreaterThan(10f));
        }
    }
}
