using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
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
        public void ResourcePerceptionKeepsFourNearestCandidatesInStableDistanceOrder()
        {
            var resources = new ResourceStore(initialCapacity: 5);
            resources.Add(ResourceKind.Food, new SimVector2(4f, 0f), 0.5f, 1f, 1f, 0f);
            ResourceId expectedFirst = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 0.5f, 1f, 1f, 0f);
            resources.Add(ResourceKind.Food, new SimVector2(3f, 0f), 0.5f, 1f, 1f, 0f);
            resources.Add(ResourceKind.Food, new SimVector2(2f, 0f), 0.5f, 1f, 1f, 0f);
            resources.Add(ResourceKind.Food, new SimVector2(5f, 0f), 0.5f, 1f, 1f, 0f);
            var positions = new[] { resources.GetAt(0).Position, resources.GetAt(1).Position, resources.GetAt(2).Position, resources.GetAt(3).Position, resources.GetAt(4).Position };
            var grid = new UniformGrid(new ArenaBounds(-6f, 6f, -6f, 6f), 2f, initialOccupantCapacity: 5);
            grid.Rebuild(positions, positions.Length);
            var candidates = new ResourceCandidateBuffer();

            PerceptionSystem.FindAvailableResources(resources, grid, new SimVector2(0f, 0f), 6f, ResourceKind.Food, ref candidates);

            Assert.That(candidates.Count, Is.EqualTo(4));
            Assert.That(candidates.GetAt(0).ResourceId, Is.EqualTo(expectedFirst));
            Assert.That(candidates.GetAt(0).Distance, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(candidates.GetAt(3).Distance, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void CreaturePerceptionSelectsNearestOtherCreatureAndBreaksDistanceTiesByStableId()
        {
            var creatures = new CreatureStore(initialCapacity: 3);
            CreatureId observer = creatures.Add(Genome.Neutral, new SimVector2(0f, 0f));
            CreatureId expected = creatures.Add(Genome.Neutral, new SimVector2(-1f, 0f));
            creatures.Add(Genome.Neutral, new SimVector2(1f, 0f));
            var positions = new[]
            {
                creatures.GetMovementAt(0).Position,
                creatures.GetMovementAt(1).Position,
                creatures.GetMovementAt(2).Position,
            };
            var grid = new UniformGrid(new ArenaBounds(-4f, 4f, -4f, 4f), 2f, initialOccupantCapacity: 3);
            grid.Rebuild(positions, positions.Length);

            CreatureObservation observation = PerceptionSystem.FindNearestOtherCreature(
                creatures,
                grid,
                new SimVector2(0f, 0f),
                visionRange: 2f,
                observer);

            Assert.That(observation.IsValid, Is.True);
            Assert.That(observation.CreatureId, Is.EqualTo(expected));
            Assert.That(observation.Distance, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void PredationDecisionPursuesAWeakerNearbyCreatureWhenHungerMakesMeatWorthwhile()
        {
            Phenotype hunter = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f, aggression: 1f, dietSpecialization: 1f));
            Phenotype prey = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f));
            CreatureNeeds needs = CreatureNeeds.Full(hunter);
            needs.Energy = hunter.EnergyCapacity * 0.05f;
            var preyObservation = new CreatureObservation(new CreatureId(2), 1, 0.5f);

            CreatureDecision decision = PredationSystem.Decide(
                needs,
                hunter,
                prey,
                preyObservation,
                new CreatureDecision(CreatureAction.Wander, -1, 0f));

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekPrey));
            Assert.That(decision.TargetCreatureId, Is.EqualTo(new CreatureId(2)));
        }

        [Test]
        public void PredationDecisionFleesAThreatWhenFearOutweighsAPlantSeekingScore()
        {
            Phenotype fearful = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, defense: 0.1f, fear: 1f));
            Phenotype hunter = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f, aggression: 1f, dietSpecialization: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(fearful);
            var hunterObservation = new CreatureObservation(new CreatureId(2), 1, 0.25f);

            CreatureDecision decision = PredationSystem.Decide(
                needs,
                fearful,
                hunter,
                hunterObservation,
                new CreatureDecision(CreatureAction.Wander, -1, 0f));

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.Flee));
            Assert.That(decision.TargetCreatureId, Is.EqualTo(new CreatureId(2)));
        }

        [Test]
        public void PredationThreatIgnoresCreaturesWithoutAFeasibleHuntingStrategy()
        {
            Phenotype nonHunter = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f));
            Phenotype defender = Phenotype.FromGenome(Genome.Neutral);

            Assert.That(PredationSystem.HuntCapability(nonHunter, defender), Is.EqualTo(0f));
            Assert.That(PredationSystem.Threat(nonHunter, defender), Is.EqualTo(0f));
            Assert.That(PredationSystem.HasViableHuntingStrategy(nonHunter), Is.False);
        }

        [Test]
        public void MemoryStoresObservationsAndLetsConfidenceDecayWithoutAllocatingHistory()
        {
            MemoryState memory = default;

            MemorySystem.RememberResource(ref memory, ResourceKind.Food, new SimVector2(3f, -1f));
            MemorySystem.TickDecay(ref memory, 2f, confidenceDecayPerSecond: 0.2f);

            Assert.That(memory.FoodPosition.X, Is.EqualTo(3f));
            Assert.That(memory.FoodPosition.Y, Is.EqualTo(-1f));
            Assert.That(memory.FoodAge, Is.EqualTo(2f));
            Assert.That(memory.FoodConfidence, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void MemoryCanGuideAHungryWandererToAHighConfidenceFoodLocation()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var memory = new MemoryState { FoodPosition = new SimVector2(4f, -2f), FoodConfidence = 0.8f, FoodOutcomeValue = 1f, FoodExperienceCount = 1 };

            CreatureDecision decision = DecisionSystem.PreferRememberedResource(
                needs,
                phenotype,
                memory,
                new CreatureDecision(CreatureAction.Wander, -1, 0f),
                out SimVector2 target);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(-1));
            Assert.That(target.X, Is.EqualTo(4f));
            Assert.That(target.Y, Is.EqualTo(-2f));
        }

        [Test]
        public void CognitiveResourceDecisionUsesLearnedOutcomesRatherThanAssumingWaterIsAlwaysBest()
        {
            Phenotype phenotype = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, learningRate: 1f, exploration: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            needs.Hydration = 0f;
            var memory = new MemoryState { FoodOutcomeValue = 1f, WaterOutcomeValue = 0.05f, FoodExperienceCount = 1, WaterExperienceCount = 1 };

            CreatureDecision decision = DecisionSystem.DecideFromLearnedOutcomes(
                needs,
                phenotype,
                memory,
                new ResourceObservation(new ResourceId(1), 0, 1f),
                new ResourceObservation(new ResourceId(2), 1, 1f),
                out _);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekFood));
        }

        [Test]
        public void LearningUpdatesOnlyTheExperiencedResourceOutcome()
        {
            var memory = new MemoryState();

            MemorySystem.LearnResourceOutcome(ref memory, ResourceKind.Water, outcome: 1f, learningRate: 0.5f);

            Assert.That(memory.WaterOutcomeValue, Is.EqualTo(0.5f));
            Assert.That(memory.WaterExperienceCount, Is.EqualTo(1));
            Assert.That(memory.FoodOutcomeValue, Is.EqualTo(0f));
        }

        [Test]
        public void FailedSearchSubstantiallyReducesTrustInTheRememberedResource()
        {
            var memory = new MemoryState { WaterConfidence = 1f };

            MemorySystem.RecordFailedSearch(ref memory, ResourceKind.Water);

            Assert.That(memory.WaterConfidence, Is.EqualTo(0.35f));
        }

        [Test]
        public void ThermalDiscomfortCanOverrideWanderingAndChooseABetterNearbyPatch()
        {
            Phenotype phenotype = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, temperatureTolerance: 0f));
            var position = new SimVector2(8f, 0f);

            CreatureDecision decision = ThermoregulationSystem.PreferThermalComfort(
                phenotype,
                position,
                tick: 0,
                new CreatureDecision(CreatureAction.Wander, -1, 0f));
            SimVector2 target = ThermoregulationSystem.FindNearbyComfortTarget(position, 0, new ArenaBounds(-25f, 25f, -25f, 25f));

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekThermalComfort));
            Assert.That(Math.Abs(TemperatureField.Sample(target, 0) - 20f), Is.LessThan(Math.Abs(TemperatureField.Sample(position, 0) - 20f)));
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
        public void DecisionCandidateBufferSelectsHighestScoreAndUsesStableTieBreak()
        {
            var candidates = new DecisionCandidateBuffer();
            Assert.That(candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekWater, targetResourceIndex: 7, targetCreatureId: default, score: 0.8f)), Is.True);
            Assert.That(candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekFood, targetResourceIndex: 5, targetCreatureId: default, score: 0.9f)), Is.True);
            Assert.That(candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekFood, targetResourceIndex: 2, targetCreatureId: default, score: 0.9f)), Is.True);

            Assert.That(candidates.TryGetBest(out DecisionCandidate best), Is.True);
            Assert.That(best.Intent, Is.EqualTo(CreatureIntent.SeekFood));
            Assert.That(best.TargetResourceIndex, Is.EqualTo(2));
        }

        [Test]
        public void DecisionCandidateBufferRetainsVisibleAndRememberedCandidates()
        {
            var candidates = new DecisionCandidateBuffer();
            for (int index = 0; index < 12; index++)
            {
                Assert.That(candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekFood, index, default, index)), Is.True);
            }

            Assert.That(candidates.Count, Is.EqualTo(12));
            Assert.That(candidates.TryGetBest(out DecisionCandidate best), Is.True);
            Assert.That(best.TargetResourceIndex, Is.EqualTo(11));
        }

        [Test]
        public void IntentUtilityPrefersARicherFoodPatchWhenItsNeedGainBeatsTravelBurden()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 2);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, initialAmount: 0.05f, capacity: 0.05f, regenerationPerSecond: 0f, nutritionMultiplier: 0.5f);
            resources.Add(ResourceKind.Food, new SimVector2(2f, 0f), 1f, initialAmount: 10f, capacity: 10f, regenerationPerSecond: 0f, nutritionMultiplier: 1.5f);
            var food = new ResourceCandidateBuffer();
            food.Consider(new ResourceObservation(resources.GetAt(0).Id, 0, 1f));
            food.Consider(new ResourceObservation(resources.GetAt(1).Id, 1, 2f));

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), food, default, carcass: default, memory: default, cognitionEnabled: false, threat: default, threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false, tick: 0, diagnostics: out _);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(1));
        }

        [Test]
        public void IntentUtilityLetsThreatExposureFavorANearerAdequateFoodPatch()
        {
            Phenotype phenotype = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, fear: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 2);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, initialAmount: 5f, capacity: 5f, regenerationPerSecond: 0f);
            resources.Add(ResourceKind.Food, new SimVector2(2f, 0f), 1f, initialAmount: 10f, capacity: 10f, regenerationPerSecond: 0f, nutritionMultiplier: 1.5f);
            var food = new ResourceCandidateBuffer();
            food.Consider(new ResourceObservation(resources.GetAt(0).Id, 0, 1f));
            food.Consider(new ResourceObservation(resources.GetAt(1).Id, 1, 2f));

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(needs, new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, fear: 1f, riskAversion: 1f), phenotype, resources, new SimVector2(0f, 0f), food, default, carcass: default, memory: default, cognitionEnabled: false, threat: new CreatureObservation(new CreatureId(99), 0, 1f), threatIntensity: 0.8f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false, tick: 0, diagnostics: out _);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(0));
        }

        [Test]
        public void TravelSensitivityGeneMakesOtherwiseIdenticalCreaturesChooseDifferentFoodTargets()
        {
            Genome lowSensitivity = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, travelSensitivity: 0f);
            Genome highSensitivity = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, travelSensitivity: 1f);
            Phenotype phenotype = Phenotype.FromGenome(lowSensitivity);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 2);
            resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, initialAmount: 5f, capacity: 5f, regenerationPerSecond: 0f);
            resources.Add(ResourceKind.Food, new SimVector2(10f, 0f), 1f, initialAmount: 10f, capacity: 10f, regenerationPerSecond: 0f, nutritionMultiplier: 1.5f);
            var food = new ResourceCandidateBuffer();
            food.Consider(new ResourceObservation(resources.GetAt(0).Id, 0, 1f));
            food.Consider(new ResourceObservation(resources.GetAt(1).Id, 1, 10f));

            CreatureDecision lowDecision = DecisionSystem.DecideIntentUtilityV1(needs, lowSensitivity, phenotype, resources, new SimVector2(0f, 0f), food, default, carcass: default, memory: default, cognitionEnabled: false, threat: default, threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false, tick: 0, diagnostics: out _);
            CreatureDecision highDecision = DecisionSystem.DecideIntentUtilityV1(needs, highSensitivity, phenotype, resources, new SimVector2(0f, 0f), food, default, carcass: default, memory: default, cognitionEnabled: false, threat: default, threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false, tick: 0, diagnostics: out _);

            Assert.That(lowDecision.TargetResourceIndex, Is.EqualTo(1));
            Assert.That(highDecision.TargetResourceIndex, Is.EqualTo(0));
        }

        [Test]
        public void IntentUtilityLetsAHighConfidenceRememberedFoodTargetCompeteWithVisibleWater()
        {
            Genome genome = Genome.Neutral;
            Phenotype phenotype = Phenotype.FromGenome(genome);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Energy = 0f;
            var resources = new ResourceStore(initialCapacity: 1);
            resources.Add(ResourceKind.Water, new SimVector2(1f, 0f), 1f, 10f, 10f, 0f);
            var water = new ResourceCandidateBuffer();
            water.Consider(new ResourceObservation(resources.GetAt(0).Id, 0, 1f));
            var memory = new MemoryState
            {
                FoodPosition = new SimVector2(2f, 0f),
                FoodConfidence = 1f,
                FoodOutcomeValue = 1f,
                FoodExperienceCount = 1,
            };

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(needs, genome, phenotype, resources, new SimVector2(0f, 0f), default, water, carcass: default, memory: memory, cognitionEnabled: true, threat: default, threatIntensity: 0f, otherPhenotype: default, predationEnabled: false, physiologyEnabled: false, tick: 0, diagnostics: out _);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekFood));
            Assert.That(decision.TargetResourceIndex, Is.EqualTo(-1));
        }

        [Test]
        public void IntentUtilityLetsASevereThreatBeatAnOtherwiseIdleCreature()
        {
            Genome selfGenome = new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, riskAversion: 1f);
            Phenotype self = Phenotype.FromGenome(selfGenome);
            Phenotype threat = Phenotype.FromGenome(new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, attack: 1f, aggression: 1f, dietSpecialization: 1f));
            CreatureNeeds needs = CreatureNeeds.Full(self);
            var resources = new ResourceStore(initialCapacity: 0);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(needs, selfGenome, self, resources, new SimVector2(0f, 0f), default, default, carcass: default, memory: default, cognitionEnabled: false, threat: new CreatureObservation(new CreatureId(2), 1, 1f), threatIntensity: PredationSystem.Threat(threat, self), otherPhenotype: threat, predationEnabled: true, physiologyEnabled: false, tick: 0, diagnostics: out _);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.Flee));
            Assert.That(decision.TargetCreatureId, Is.EqualTo(new CreatureId(2)));
        }

        [Test]
        public void IntentUtilitySeeksAHealthyMatureNearbyMateWhenSurvivalNeedsAreSafe()
        {
            Phenotype phenotype = Phenotype.FromGenome(Genome.Neutral);
            CreatureNeeds needs = CreatureNeeds.Full(phenotype);
            needs.Age = 21f;
            var resources = new ResourceStore(initialCapacity: 0);

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV1(
                needs, Genome.Neutral, phenotype, resources, new SimVector2(0f, 0f), default, default,
                carcass: default, memory: default, cognitionEnabled: false, threat: default, threatIntensity: 0f,
                otherPhenotype: default, predationEnabled: false, physiologyEnabled: false,
                reproduction: default, mate: new CreatureObservation(new CreatureId(2), 1, 1f), mateNeeds: needs, matePhenotype: phenotype,
                mateReproduction: default, reproductionEnabled: true, tick: 0, diagnostics: out _);

            Assert.That(decision.Action, Is.EqualTo(CreatureAction.SeekMate));
            Assert.That(decision.TargetCreatureId, Is.EqualTo(new CreatureId(2)));
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
