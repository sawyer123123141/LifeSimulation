using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Spatial;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Core
{
    public sealed class SimulationWorld
    {
        /// <summary>Smoothing window for the recent-intake-rate exponential moving average.</summary>
        private const float ForagingIntakeRateWindowSeconds = 5f;

        /// <summary>Minimum confidence-discounted <see cref="ForagingEconomics.PatchScore"/> a remembered place needs to be worth traveling to, matching <c>DecisionSystem.MinimumUrgencyToSeekResource</c>.</summary>
        private const float RememberedPlaceMinimumScore = 0.05f;

        private CreatureId[] _pendingDeaths;
        private DeathCause[] _pendingDeathCauses;
        private SimVector2[] _pendingDeathPositions;
        private int _pendingDeathCount;
        private long _spawnOrdinal;
        private SimVector2[] _resourcePositions;
        private SimVector2[] _creaturePositions;
        private float[] _combatDamage;
        private float[] _foragingEnergyGained;
        private ResourceRequest[] _resourceRequests;
        private float[] _resourceAllocations;
        private readonly ReproductionSystem _reproduction;
        private int _resourceRequestCount;
        private long _birthOrdinal;
        private int _birthCount;
        private int _deathCount;
        private int _starvationDeathCount;
        private int _dehydrationDeathCount;
        private int _ageDeathCount;
        private int _healthDeathCount;
        private float _cumulativeFoodConsumed;
        private float _cumulativeWaterConsumed;
        private float _cumulativeCarcassConsumed;
        private int _attackHitCount;
        private int _predationDeathCount;
        private float _cumulativePlantGrowth;
        private float _cumulativePlantBiomassConsumed;
        private float _initialPlantBiomass;
        private long _plantSeedOrdinal;
        private int _plantBirthCount;

        public SimulationWorld(SimulationConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Config.Validate();
            Creatures = new CreatureStore(Config.InitialPopulation, Config.MaximumMemorySlots);
            Resources = new ResourceStore(initialCapacity: 8);
            Plants = new PlantPatchStore(initialCapacity: 8);
            PlantSites = new PlantSiteRegistry(initialCapacity: 8);
            Environment = Config.PlantCohortsEnabled ? EnvironmentField.CreateMoistureGradient() : new EnvironmentField();
            Arena = new ArenaBounds(-25f, 25f, -25f, 25f);
            ResourceGrid = new UniformGrid(Arena, cellSize: 5f, initialOccupantCapacity: 8);
            CombatGrid = new UniformGrid(Arena, cellSize: 5f, initialOccupantCapacity: Config.InitialPopulation);
            _pendingDeaths = new CreatureId[Math.Max(Config.InitialPopulation, 1)];
            _pendingDeathCauses = new DeathCause[_pendingDeaths.Length];
            _pendingDeathPositions = new SimVector2[_pendingDeaths.Length];
            _resourcePositions = new SimVector2[8];
            _creaturePositions = new SimVector2[Math.Max(Config.InitialPopulation, 1)];
            _combatDamage = new float[Math.Max(Config.InitialPopulation, 1)];
            _foragingEnergyGained = new float[Math.Max(Config.InitialPopulation, 1)];
            _resourceRequests = new ResourceRequest[Math.Max(Config.InitialPopulation, 1)];
            _resourceAllocations = new float[_resourceRequests.Length];
            _reproduction = new ReproductionSystem(Creatures, Arena, Config.InitialPopulation, Config.PhysiologyEnabled);
            Events = new SimulationEventBuffer(capacity: 1024);

            for (int index = 0; index < Config.InitialPopulation; index++)
            {
                Spawn(CreateFounderGenome(index));
            }
        }

        public SimulationConfig Config { get; }
        public CreatureStore Creatures { get; }
        public ResourceStore Resources { get; }
        public PlantPatchStore Plants { get; }
        public PlantSiteRegistry PlantSites { get; }
        public EnvironmentField Environment { get; }
        public ArenaBounds Arena { get; }
        public UniformGrid ResourceGrid { get; }
        public UniformGrid CombatGrid { get; }
        public UniformGrid CreatureGrid => _reproduction.Grid;
        public SimulationEventBuffer Events { get; }
        public int CreatureCount => Creatures.Count;
        public long CurrentTick { get; private set; }
        public SimulationStatistics Statistics { get; private set; }
        public DecisionTraceRecorder DecisionTrace { get; private set; }

        public int AddPlantPatch(ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float nutrition, float defense, bool countsAsInitialBiomass = true)
        {
            int patchIndex = Plants.Add(foodResourceId, position, biomass, capacity, growthRate, nutrition, defense);
            if (countsAsInitialBiomass) _initialPlantBiomass += biomass;
            return patchIndex;
        }

        public void EnableDecisionTrace(CreatureId sampledCreatureId, int capacity)
        {
            DecisionTrace = new DecisionTraceRecorder(sampledCreatureId, capacity);
        }

        public CreatureId GetCreatureIdAt(int index)
        {
            return Creatures.GetIdAt(index);
        }

        public CreatureId Spawn()
        {
            return Spawn(Genome.Neutral);
        }

        public CreatureId Spawn(Genome genome)
        {
            long spawnOrdinal = _spawnOrdinal++;
            return Creatures.Add(genome, new SimVector2(
                Lerp(Arena.MinimumX, Arena.MaximumX, DeterministicRandom.Float01(Config.WorldSeed, RandomDomain.BirthPlacement, spawnOrdinal, 0, 0, 0)),
                Lerp(Arena.MinimumY, Arena.MaximumY, DeterministicRandom.Float01(Config.WorldSeed, RandomDomain.BirthPlacement, spawnOrdinal, 0, 0, 1))));
        }

        private Genome CreateFounderGenome(long founderOrdinal)
        {
            if (Config.FounderProfile == FounderProfile.PredationVariation)
            {
                return PredationFounderFactory.Create(Config.WorldSeed, founderOrdinal);
            }

            if (Config.FounderProfile == FounderProfile.CognitionVariation)
            {
                return CognitionFounderFactory.Create(Config.WorldSeed, founderOrdinal);
            }

            if (Config.FounderProfile == FounderProfile.PhysiologyVariation)
            {
                return PhysiologyFounderFactory.Create(Config.WorldSeed, founderOrdinal);
            }

            const float standardDeviation = 0.12f;
            return new Genome(
                FounderGene(founderOrdinal, 0, standardDeviation),
                FounderGene(founderOrdinal, 2, standardDeviation),
                FounderGene(founderOrdinal, 4, standardDeviation),
                FounderGene(founderOrdinal, 6, standardDeviation),
                FounderGene(founderOrdinal, 8, standardDeviation),
                FounderGene(founderOrdinal, 10, standardDeviation),
                urgencyExponent: FounderGene(founderOrdinal, 20, standardDeviation),
                travelSensitivity: FounderGene(founderOrdinal, 22, standardDeviation),
                riskAversion: FounderGene(founderOrdinal, 24, standardDeviation),
                commitment: FounderGene(founderOrdinal, 26, standardDeviation));
        }

        private float FounderGene(long founderOrdinal, int purpose, float standardDeviation)
        {
            return 0.5f + (DeterministicRandom.Gaussian(
                Config.WorldSeed,
                RandomDomain.FounderGenome,
                founderOrdinal,
                0,
                0,
                purpose) * standardDeviation);
        }

        public bool TryGetCreatureIndex(CreatureId id, out int index)
        {
            return Creatures.TryGetIndex(id, out index);
        }

        public CreatureNeeds GetCreatureNeedsAt(int index)
        {
            return Creatures.GetNeedsAt(index);
        }

        public MovementState GetCreatureMovementAt(int index)
        {
            return Creatures.GetMovementAt(index);
        }

        public CreatureDecision GetCreatureDecisionAt(int index)
        {
            return Creatures.GetDecisionAt(index);
        }

        public DecisionDiagnostics GetCreatureDecisionDiagnosticsAt(int index)
        {
            return Creatures.GetDecisionDiagnosticsAt(index);
        }

        public MemoryState GetCreatureMemoryAt(int index)
        {
            return Creatures.GetMemoryRefAt(index);
        }

        public void SetCreaturePosition(CreatureId id, SimVector2 position)
        {
            if (!Creatures.TryGetIndex(id, out int index))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            ref MovementState movement = ref Creatures.GetMovementRefAt(index);
            movement = new MovementState(Arena.Clamp(position));
        }

        public void RequestDeath(CreatureId id, DeathCause cause)
        {
            if (!Creatures.TryGetIndex(id, out int creatureIndex))
            {
                return;
            }

            for (int index = 0; index < _pendingDeathCount; index++)
            {
                if (_pendingDeaths[index].Equals(id))
                {
                    return;
                }
            }

            EnsurePendingDeathCapacity(_pendingDeathCount + 1);
            _pendingDeaths[_pendingDeathCount] = id;
            _pendingDeathCauses[_pendingDeathCount] = cause;
            _pendingDeathPositions[_pendingDeathCount] = Creatures.GetMovementAt(creatureIndex).Position;
            _pendingDeathCount++;
        }

        public void Step(float fixedDeltaTime)
        {
            if (Math.Abs(fixedDeltaTime - Config.FixedDeltaTime) > 0.000001f)
            {
                throw new ArgumentException("Simulation steps must use the configured fixed delta.", nameof(fixedDeltaTime));
            }

            long nextTick = CurrentTick + 1;
            if (IsDue(nextTick, Config.Schedule.ResourcesHz))
            {
                float resourceDeltaTime = 1f / Config.Schedule.ResourcesHz;
                if (Config.PlantCohortsEnabled)
                {
                    Resources.RegenerateNonFood(resourceDeltaTime);
                    _cumulativePlantGrowth += PlantGrowthSystem.Step(Plants, Environment, resourceDeltaTime);
                    _plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, resourceDeltaTime, ref _plantSeedOrdinal);
                    PlantGrowthSystem.ProjectFoodResources(Plants, Resources);
                }
                else
                {
                    Resources.Regenerate(resourceDeltaTime);
                }
            }

            if (IsDue(nextTick, Config.Schedule.PerceptionHz))
            {
                RebuildResourceGrid();
                RebuildCombatGrid();
            }

            if (Config.ForagingEconomicsEnabled)
            {
                AdvanceForagingActionTime(Config.FixedDeltaTime);
            }

            if (Config.DecisionStaggerEnabled || IsDue(nextTick, Config.Schedule.DecisionsHz))
            {
                TickDecisions(nextTick);
            }

            TickMovement(nextTick);
            TickCombat(nextTick);
            if (IsDue(nextTick, Config.Schedule.NeedsHz))
            {
                TickNeeds();
            }

            ResolveResourceInteractions();

            if (Config.ForagingEconomicsEnabled)
            {
                UpdateForagingIntakeRate(Config.FixedDeltaTime);
            }

            if (IsDue(nextTick, Config.Schedule.ReproductionHz))
            {
                TickReproduction();
            }

            if (IsDue(nextTick, Config.Schedule.StatisticsHz))
            {
                Statistics = BuildStatistics(nextTick);
            }

            for (int index = 0; index < _pendingDeathCount; index++)
            {
                CreatureId deceased = _pendingDeaths[index];
                if (_pendingDeathCauses[index] == DeathCause.Predation
                    && Creatures.TryGetIndex(deceased, out int deceasedIndex))
                {
                    Phenotype phenotype = Creatures.GetPhenotypeAt(deceasedIndex);
                    Resources.Add(
                        ResourceKind.Carcass,
                        _pendingDeathPositions[index],
                        interactionRadius: 1.1f,
                        initialAmount: 10f * phenotype.BodyMass,
                        capacity: 10f * phenotype.BodyMass,
                        regenerationPerSecond: 0f);
                }

                if (Creatures.Remove(deceased))
                {
                    Events.TryWrite(new SimulationEvent(
                        nextTick,
                        SimulationEventKind.Death,
                        deceased,
                        default,
                        default,
                        _pendingDeathCauses[index]));
                    _deathCount++;
                    CountDeathCause(_pendingDeathCauses[index]);
                }
            }

            _pendingDeathCount = 0;
            CurrentTick = nextTick;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = 14695981039346656037UL;
            hash = Hash(hash, unchecked((ulong)Config.WorldSeed));
            hash = Hash(hash, unchecked((ulong)CurrentTick));
            hash = Hash(hash, unchecked((ulong)CreatureCount));
            hash = Hash(hash, unchecked((ulong)_spawnOrdinal));

            for (int index = 0; index < CreatureCount; index++)
            {
                hash = Hash(hash, unchecked((ulong)GetCreatureIdAt(index).Value));
                Genome genome = Creatures.GetGenomeAt(index);
                hash = HashFloat(hash, genome.BodySize);
                hash = HashFloat(hash, genome.MovementSpeed);
                hash = HashFloat(hash, genome.MetabolicPace);
                hash = HashFloat(hash, genome.VisionRange);
                hash = HashFloat(hash, genome.WaterEfficiency);
                hash = HashFloat(hash, genome.FoodEfficiency);
                hash = HashFloat(hash, genome.Attack);
                hash = HashFloat(hash, genome.Defense);
                hash = HashFloat(hash, genome.Maneuverability);
                hash = HashFloat(hash, genome.Fear);
                hash = HashFloat(hash, genome.Aggression);
                hash = HashFloat(hash, genome.DietSpecialization);
                hash = HashFloat(hash, genome.MemoryCapacity);
                hash = HashFloat(hash, genome.MemoryRetention);
                hash = HashFloat(hash, genome.LearningRate);
                hash = HashFloat(hash, genome.Exploration);
                hash = HashFloat(hash, genome.TemperatureTolerance);
                hash = HashFloat(hash, genome.FertilityInvestment);
                hash = HashFloat(hash, genome.LifespanTendency);
                hash = HashFloat(hash, genome.UrgencyExponent);
                hash = HashFloat(hash, genome.TravelSensitivity);
                hash = HashFloat(hash, genome.RiskAversion);
                hash = HashFloat(hash, genome.Commitment);
                hash = HashFloat(hash, genome.Persistence);

                CreatureNeeds needs = Creatures.GetNeedsAt(index);
                hash = HashFloat(hash, needs.Energy);
                hash = HashFloat(hash, needs.Hydration);
                hash = HashFloat(hash, needs.Rest);
                hash = HashFloat(hash, needs.Health);
                hash = HashFloat(hash, needs.Age);

                MovementState movement = Creatures.GetMovementAt(index);
                hash = HashFloat(hash, movement.PreviousPosition.X);
                hash = HashFloat(hash, movement.PreviousPosition.Y);
                hash = HashFloat(hash, movement.Position.X);
                hash = HashFloat(hash, movement.Position.Y);
                hash = HashFloat(hash, movement.DistanceSinceLastNeeds);

                CreatureDecision decision = Creatures.GetDecisionAt(index);
                hash = Hash(hash, unchecked((ulong)decision.Action));
                hash = Hash(hash, unchecked((ulong)(long)decision.TargetResourceIndex));
                hash = Hash(hash, unchecked((ulong)decision.TargetCreatureId.Value));
                hash = HashFloat(hash, decision.Score);
                hash = Hash(hash, unchecked((ulong)decision.DecisionTick));

                ReproductionState reproduction = Creatures.GetReproductionRefAt(index);
                hash = HashFloat(hash, reproduction.CooldownRemaining);

                CombatState combat = Creatures.GetCombatRefAt(index);
                hash = HashFloat(hash, combat.WoundSeverity);
                hash = HashFloat(hash, combat.AttackRecoveryRemaining);

                MemoryState memory = Creatures.GetMemoryRefAt(index);
                hash = HashFloat(hash, memory.FoodPosition.X);
                hash = HashFloat(hash, memory.FoodPosition.Y);
                hash = HashFloat(hash, memory.WaterPosition.X);
                hash = HashFloat(hash, memory.WaterPosition.Y);
                hash = HashFloat(hash, memory.ThreatPosition.X);
                hash = HashFloat(hash, memory.ThreatPosition.Y);
                hash = HashFloat(hash, memory.FoodConfidence);
                hash = HashFloat(hash, memory.WaterConfidence);
                hash = HashFloat(hash, memory.ThreatConfidence);
                hash = HashFloat(hash, memory.FoodAge);
                hash = HashFloat(hash, memory.WaterAge);
                hash = HashFloat(hash, memory.ThreatAge);
                hash = HashFloat(hash, memory.ActiveRememberedTarget.X);
                hash = HashFloat(hash, memory.ActiveRememberedTarget.Y);
                hash = Hash(hash, memory.HasActiveRememberedTarget ? 1UL : 0UL);
                hash = HashFloat(hash, memory.FoodOutcomeValue);
                hash = HashFloat(hash, memory.WaterOutcomeValue);
                hash = Hash(hash, unchecked((ulong)memory.FoodExperienceCount));
                hash = Hash(hash, unchecked((ulong)memory.WaterExperienceCount));
            }

            hash = Hash(hash, unchecked((ulong)Resources.Count));
            for (int index = 0; index < Resources.Count; index++)
            {
                ResourceState resource = Resources.GetAt(index);
                hash = Hash(hash, unchecked((ulong)resource.Id.Value));
                hash = Hash(hash, unchecked((ulong)resource.Kind));
                hash = HashFloat(hash, resource.Position.X);
                hash = HashFloat(hash, resource.Position.Y);
                hash = HashFloat(hash, resource.InteractionRadius);
                hash = HashFloat(hash, resource.Amount);
                hash = HashFloat(hash, resource.Capacity);
                hash = HashFloat(hash, resource.RegenerationPerSecond);
                hash = Hash(hash, resource.IsActive ? 1UL : 0UL);
                hash = HashFloat(hash, resource.NutritionMultiplier);
            }

            hash = Hash(hash, unchecked((ulong)Plants.Count));
            for (int index = 0; index < Plants.Count; index++)
            {
                PlantPatchState patch = Plants.GetAt(index);
                hash = Hash(hash, unchecked((ulong)patch.Id.Value));
                hash = Hash(hash, unchecked((ulong)patch.FoodResourceId.Value));
                hash = HashFloat(hash, patch.Biomass);
                hash = HashFloat(hash, patch.Capacity);
                hash = HashFloat(hash, patch.GrowthRate);
                hash = HashFloat(hash, patch.Nutrition);
                hash = HashFloat(hash, patch.Defense);
                hash = HashFloat(hash, patch.Genome.Growth);
                hash = HashFloat(hash, patch.Genome.SeedInvestment);
                hash = HashFloat(hash, patch.Genome.WaterEfficiency);
                hash = HashFloat(hash, patch.Genome.Nutrition);
                hash = HashFloat(hash, patch.Genome.Defense);
                hash = HashFloat(hash, patch.Genome.Dispersal);
                hash = HashFloat(hash, patch.Genome.MoistureTolerance);
                hash = HashFloat(hash, patch.Genome.TemperatureTolerance);
                hash = Hash(hash, unchecked((ulong)patch.Lineage.LineageId.Value));
                hash = Hash(hash, unchecked((ulong)patch.Lineage.ParentId.Value));
                hash = Hash(hash, unchecked((ulong)patch.Lineage.Generation));
            }

            return hash;
        }

        private void EnsurePendingDeathCapacity(int required)
        {
            if (required <= _pendingDeaths.Length)
            {
                return;
            }

            Array.Resize(ref _pendingDeaths, Math.Max(required, _pendingDeaths.Length * 2));
            Array.Resize(ref _pendingDeathCauses, _pendingDeaths.Length);
            Array.Resize(ref _pendingDeathPositions, _pendingDeaths.Length);
        }

        private bool IsDue(long tick, int frequencyHz)
        {
            int interval = Config.Schedule.BaseFrequencyHz / frequencyHz;
            return tick % interval == 0;
        }

        private void TickNeeds()
        {
            float deltaTime = 1f / Config.Schedule.NeedsHz;
            for (int index = 0; index < Creatures.Count; index++)
            {
                ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(index);
                ref MovementState movement = ref Creatures.GetMovementRefAt(index);
                NeedsSystem.Tick(ref needs, Creatures.GetPhenotypeAt(index), deltaTime, movement.DistanceSinceLastNeeds);
                if (Config.PhysiologyEnabled)
                {
                    NeedsSystem.ApplyTemperatureStress(ref needs, Creatures.GetPhenotypeAt(index), TemperatureField.Sample(movement.Position, CurrentTick + 1), deltaTime);
                }
                movement.DistanceSinceLastNeeds = 0f;
                if (needs.Health <= 0f)
                {
                    DeathCause cause = needs.Hydration <= 0f
                        ? DeathCause.Dehydration
                        : needs.Energy <= 0f ? DeathCause.Starvation : DeathCause.Health;
                    RequestDeath(Creatures.GetIdAt(index), cause);
                }
                else if (Config.PhysiologyEnabled && needs.Age >= Creatures.GetPhenotypeAt(index).MaximumAgeSeconds)
                {
                    RequestDeath(Creatures.GetIdAt(index), DeathCause.Age);
                }

                if (Config.CognitionEnabled)
                {
                    MemorySystem.TickDecay(
                        ref Creatures.GetMemoryRefAt(index),
                        deltaTime,
                        Creatures.GetPhenotypeAt(index).MemoryConfidenceDecayPerSecond);
                }
            }
        }

        private void TickMovement(long nextTick)
        {
            for (int index = 0; index < Creatures.Count; index++)
            {
                CreatureId id = Creatures.GetIdAt(index);
                ref MovementState movement = ref Creatures.GetMovementRefAt(index);
                SimVector2 target = GetMovementTarget(index, id, nextTick, movement.Position);
                MovementSystem.MoveToward(
                    ref movement,
                    target,
                    Creatures.GetPhenotypeAt(index).MaximumSpeed,
                    Config.FixedDeltaTime,
                    Arena);
            }
        }

        private SimVector2 GetMovementTarget(int creatureIndex, CreatureId creatureId, long tick, SimVector2 position)
        {
            CreatureDecision decision = Creatures.GetDecisionAt(creatureIndex);
            if ((decision.Action == CreatureAction.SeekFood
                    || decision.Action == CreatureAction.SeekWater
                    || decision.Action == CreatureAction.Eat
                    || decision.Action == CreatureAction.Drink
                    || decision.Action == CreatureAction.SeekCarcass
                    || decision.Action == CreatureAction.FeedCarcass)
                && (uint)decision.TargetResourceIndex < (uint)Resources.Count)
            {
                ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                if (resource.IsActive && resource.Amount > 0f)
                {
                    return resource.Position;
                }
            }

            if (Config.CognitionEnabled)
            {
                MemoryState memory = Creatures.GetMemoryRefAt(creatureIndex);
                if (memory.HasActiveRememberedTarget
                    && (decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.SeekWater))
                {
                    return memory.ActiveRememberedTarget;
                }
            }

            if (decision.Action == CreatureAction.SeekPrey || decision.Action == CreatureAction.Attack || decision.Action == CreatureAction.SeekMate)
            {
                if (Creatures.TryGetIndex(decision.TargetCreatureId, out int targetIndex))
                {
                    return Creatures.GetMovementAt(targetIndex).Position;
                }
            }

            if (decision.Action == CreatureAction.Flee
                && Creatures.TryGetIndex(decision.TargetCreatureId, out int threatIndex))
            {
                SimVector2 threatPosition = Creatures.GetMovementAt(threatIndex).Position;
                float x = position.X - threatPosition.X;
                float y = position.Y - threatPosition.Y;
                float length = (float)Math.Sqrt((x * x) + (y * y));
                if (length > 0.0001f)
                {
                    return new SimVector2(position.X + (x / length), position.Y + (y / length));
                }
            }

            if (Config.PhysiologyEnabled && decision.Action == CreatureAction.SeekThermalComfort)
            {
                return ThermoregulationSystem.FindNearbyComfortTarget(position, tick, Arena);
            }

            if (Config.CognitionEnabled && decision.Action == CreatureAction.Wander)
            {
                MemoryState memory = Creatures.GetMemoryRefAt(creatureIndex);
                bool useFoodHome = memory.FoodConfidence >= 0.5f && memory.FoodConfidence >= memory.WaterConfidence;
                bool useWaterHome = memory.WaterConfidence >= 0.5f && memory.WaterConfidence > memory.FoodConfidence;
                if (useFoodHome || useWaterHome)
                {
                    SimVector2 home = useFoodHome ? memory.FoodPosition : memory.WaterPosition;
                    const float homeRadius = 3f;
                    if (SimVector2.Distance(position, home) > homeRadius)
                    {
                        return home;
                    }

                    long homeEpoch = tick / (Config.Schedule.BaseFrequencyHz * 5L);
                    float homeAngle = DeterministicRandom.Float01(
                        Config.WorldSeed,
                        RandomDomain.Exploration,
                        homeEpoch,
                        creatureId.Value,
                        0,
                        2) * ((float)Math.PI * 2f);
                    return new SimVector2(
                        home.X + ((float)Math.Cos(homeAngle) * homeRadius),
                        home.Y + ((float)Math.Sin(homeAngle) * homeRadius));
                }
            }

            long explorationEpoch = tick / (Config.Schedule.BaseFrequencyHz * 5L);
            float angle = DeterministicRandom.Float01(
                Config.WorldSeed,
                RandomDomain.Exploration,
                explorationEpoch,
                creatureId.Value,
                0,
                0) * ((float)Math.PI * 2f);
            return new SimVector2(
                position.X + (float)Math.Cos(angle),
                position.Y + (float)Math.Sin(angle));
        }

        private void TickDecisions(long tick)
        {
            int interval = Config.Schedule.BaseFrequencyHz / Config.Schedule.DecisionsHz;
            for (int index = 0; index < Creatures.Count; index++)
            {
                if (Config.DecisionStaggerEnabled && (tick + index) % interval != 0)
                {
                    continue;
                }

                MovementState movement = Creatures.GetMovementAt(index);
                Phenotype phenotype = Creatures.GetPhenotypeAt(index);
                CreatureDecision previousDecision = Creatures.GetDecisionAt(index);
                ResourceObservation food = PerceptionSystem.FindNearestAvailableResource(
                    Resources,
                    ResourceGrid,
                    movement.Position,
                    phenotype.VisionRange,
                    ResourceKind.Food);
                ResourceObservation water = PerceptionSystem.FindNearestAvailableResource(
                    Resources,
                    ResourceGrid,
                    movement.Position,
                    phenotype.VisionRange,
                    ResourceKind.Water);
                ResourceObservation carcass = PerceptionSystem.FindNearestAvailableResource(
                    Resources,
                    ResourceGrid,
                    movement.Position,
                    phenotype.VisionRange,
                    ResourceKind.Carcass);
                var foodCandidates = new ResourceCandidateBuffer();
                var waterCandidates = new ResourceCandidateBuffer();
                CreatureObservation other = default;
                float threatIntensity = 0f;
                if (Config.ForagingEconomicsEnabled && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy && !Config.CognitionEnabled)
                {
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Food, ref foodCandidates);
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Water, ref waterCandidates);
                }
                if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                {
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Food, ref foodCandidates);
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Water, ref waterCandidates);
                    if (Config.FounderProfile == FounderProfile.PredationVariation
                        || Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                    {
                        other = PerceptionSystem.FindNearestOtherCreature(Creatures, CombatGrid, movement.Position, phenotype.VisionRange, Creatures.GetIdAt(index));
                        if (other.IsValid)
                        {
                            threatIntensity = PredationSystem.Threat(Creatures.GetPhenotypeAt(other.CreatureIndex), phenotype, other.Distance, Config.PredationEconomicsEnabled);
                        }
                    }
                }
                if (Config.CognitionEnabled)
                {
                    ref MemoryState memory = ref Creatures.GetMemoryRefAt(index);
                    if (memory.HasActiveRememberedTarget
                        && SimVector2.Distance(movement.Position, memory.ActiveRememberedTarget) <= 1f
                        && ((previousDecision.Action == CreatureAction.SeekFood && !food.IsValid)
                            || (previousDecision.Action == CreatureAction.SeekWater && !water.IsValid)))
                    {
                        MemorySystem.RecordFailedSearch(
                            ref memory,
                            previousDecision.Action == CreatureAction.SeekFood ? ResourceKind.Food : ResourceKind.Water);
                    }

                    if (food.IsValid) MemorySystem.RememberResource(ref memory, ResourceKind.Food, Resources.GetAt(food.ResourceIndex).Position);
                    if (water.IsValid) MemorySystem.RememberResource(ref memory, ResourceKind.Water, Resources.GetAt(water.ResourceIndex).Position);
                }
                if (Config.CognitionEnabled
                    && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy
                    && previousDecision.TargetResourceIndex < 0
                    && (previousDecision.Action == CreatureAction.SeekFood || previousDecision.Action == CreatureAction.SeekWater))
                {
                    // TargetResourceIndex < 0 with a Seek action, under the Legacy policy, only ever
                    // comes from the remembered-place override below - a real visible target always
                    // carries its resource index. Arriving there and still seeing nothing of that kind
                    // means the remembered place was wrong; RecordFailedPlaceSearch itself is a no-op
                    // until the creature is actually within SamePlaceRadius of a matching slot.
                    ResourceKind previousKind = previousDecision.Action == CreatureAction.SeekFood ? ResourceKind.Food : ResourceKind.Water;
                    bool nothingOfThatKindVisible = previousKind == ResourceKind.Food ? !food.IsValid : !water.IsValid;
                    if (nothingOfThatKindVisible)
                    {
                        int usableSlotCount = GetUsableMemorySlotCount(Creatures.GetGenomeAt(index));
                        MemorySystem.RecordFailedPlaceSearch(Creatures, index, usableSlotCount, movement.Position, previousKind, Config.SamePlaceRadius);
                    }
                }
                DecisionDiagnostics diagnostics;
                CreatureDecision decision;
                if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                {
                    decision = DecisionSystem.DecideIntentUtilityV1(
                        Creatures.GetNeedsAt(index),
                        Creatures.GetGenomeAt(index),
                        phenotype,
                        Resources,
                        movement.Position,
                        foodCandidates,
                        waterCandidates,
                        carcass,
                        Creatures.GetMemoryRefAt(index),
                        Config.CognitionEnabled,
                        other,
                        threatIntensity,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        Config.FounderProfile == FounderProfile.PredationVariation,
                        Config.PhysiologyEnabled,
                        Creatures.GetReproductionRefAt(index),
                        other,
                        other.IsValid ? Creatures.GetNeedsAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetReproductionRefAt(other.CreatureIndex) : default,
                        true,
                        tick,
                        out diagnostics,
                        Config.PredationEconomicsEnabled,
                        Config.ThreatFalloffDistance);
                    if (Config.CognitionEnabled)
                    {
                        ref MemoryState memory = ref Creatures.GetMemoryRefAt(index);
                        memory.HasActiveRememberedTarget = false;
                        if (decision.TargetResourceIndex < 0 && decision.Action == CreatureAction.SeekFood)
                        {
                            memory.ActiveRememberedTarget = memory.FoodPosition;
                            memory.HasActiveRememberedTarget = true;
                        }
                        else if (decision.TargetResourceIndex < 0 && decision.Action == CreatureAction.SeekWater)
                        {
                            memory.ActiveRememberedTarget = memory.WaterPosition;
                            memory.HasActiveRememberedTarget = true;
                        }
                        if (decision.Action == CreatureAction.Flee && other.IsValid)
                        {
                            MemorySystem.RememberThreat(ref memory, Creatures.GetMovementAt(other.CreatureIndex).Position);
                        }
                    }
                }
                else if (Config.CognitionEnabled)
                {
                    decision = DecisionSystem.DecideFromLearnedOutcomes(Creatures.GetNeedsAt(index), phenotype, Creatures.GetMemoryRefAt(index), food, water, out diagnostics);
                }
                else if (Config.ForagingEconomicsEnabled)
                {
                    decision = DecisionSystem.Decide(
                        Creatures.GetNeedsAt(index),
                        phenotype,
                        foodCandidates,
                        waterCandidates,
                        previousDecision.Action,
                        Creatures.GetForagingRefAt(index).SecondsInCurrentAction,
                        Config.HandlingSeconds,
                        Config.ReferenceGain,
                        Config.CommitmentStrength,
                        Config.CommitmentHalfLifeSeconds,
                        out diagnostics);
                }
                else
                {
                    decision = DecisionSystem.Decide(Creatures.GetNeedsAt(index), phenotype, food, water, out diagnostics);
                }
                if (Config.ForagingEconomicsEnabled
                    && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy
                    && !Config.CognitionEnabled
                    && previousDecision.Action == CreatureAction.Eat)
                {
                    float currentPatchIntakeRate = index < _foragingEnergyGained.Length
                        ? _foragingEnergyGained[index] / Config.FixedDeltaTime
                        : 0f;
                    float recentIntakeRate = Creatures.GetForagingRefAt(index).RecentIntakeRate;
                    if (ForagingEconomics.ShouldAbandon(currentPatchIntakeRate, recentIntakeRate, phenotype.Persistence, Config.GiveUpSensitivity))
                    {
                        decision = new CreatureDecision(CreatureAction.SeekFood, -1, decision.Score);
                    }
                }
                if (Config.CognitionEnabled && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy)
                {
                    // Remembered places only compete once nothing visible was suitable: a visible
                    // patch already won the primary decision above, and confidence (<=1) discounting
                    // an identical remembered PatchScore means a real, currently-seen option always
                    // beats an equally good memory of one - it never needs to be scored to lose.
                    ref MemoryState memory = ref Creatures.GetMemoryRefAt(index);
                    memory.HasActiveRememberedTarget = false;
                    if (decision.Action == CreatureAction.Wander)
                    {
                        Genome genome = Creatures.GetGenomeAt(index);
                        int usableSlotCount = GetUsableMemorySlotCount(genome);
                        bool foundRememberedPlace = TryScoreBestRememberedPlace(
                            index,
                            usableSlotCount,
                            phenotype,
                            Creatures.GetNeedsAt(index),
                            movement.Position,
                            memory.ThreatPosition,
                            memory.ThreatConfidence,
                            out SimVector2 bestPosition,
                            out ResourceKind bestKind,
                            out float bestScore);
                        if (foundRememberedPlace && bestScore >= RememberedPlaceMinimumScore)
                        {
                            decision = new CreatureDecision(
                                bestKind == ResourceKind.Water ? CreatureAction.SeekWater : CreatureAction.SeekFood,
                                -1,
                                bestScore);
                            memory.ActiveRememberedTarget = bestPosition;
                            memory.HasActiveRememberedTarget = true;
                        }
                    }
                }
                if (Config.FounderProfile == FounderProfile.PredationVariation && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy)
                {
                    if (!other.IsValid)
                    {
                        other = PerceptionSystem.FindNearestOtherCreature(
                            Creatures,
                            CombatGrid,
                            movement.Position,
                            phenotype.VisionRange,
                            Creatures.GetIdAt(index));
                    }
                    if (other.IsValid)
                    {
                        decision = PredationSystem.Decide(
                            Creatures.GetNeedsAt(index),
                            phenotype,
                            Creatures.GetPhenotypeAt(other.CreatureIndex),
                            other,
                            decision,
                            ref diagnostics,
                            Config.PredationEconomicsEnabled);
                        if (Config.CognitionEnabled && decision.Action == CreatureAction.Flee)
                        {
                            MemorySystem.RememberThreat(ref Creatures.GetMemoryRefAt(index), Creatures.GetMovementAt(other.CreatureIndex).Position);
                        }
                    }

                    decision = PredationSystem.PreferCarcassWhenUseful(
                        Creatures.GetNeedsAt(index),
                        phenotype,
                        carcass,
                        decision,
                        ref diagnostics);
                }
                if (Config.PhysiologyEnabled && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy)
                {
                    decision = ThermoregulationSystem.PreferThermalComfort(phenotype, movement.Position, tick, decision, ref diagnostics);
                }
                CreatureDecision selectedIntent = decision;
                if (Config.CognitionEnabled
                    && decision.TargetResourceIndex < 0
                    && (decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.SeekWater))
                {
                    ResourceObservation visibleResource = decision.Action == CreatureAction.SeekFood ? food : water;
                    if (visibleResource.IsValid)
                    {
                        ResourceState resource = Resources.GetAt(visibleResource.ResourceIndex);
                        MemoryState memory = Creatures.GetMemoryRefAt(index);
                        SimVector2 rememberedTarget = decision.Action == CreatureAction.SeekFood ? memory.FoodPosition : memory.WaterPosition;
                        if (SimVector2.Distance(rememberedTarget, resource.Position) <= resource.InteractionRadius)
                        {
                            decision = new CreatureDecision(decision.Action, visibleResource.ResourceIndex, decision.Score);
                        }
                    }
                }
                if ((decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.SeekWater)
                    && (uint)decision.TargetResourceIndex < (uint)Resources.Count)
                {
                    ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                    if (SimVector2.Distance(movement.Position, resource.Position) <= resource.InteractionRadius)
                    {
                        decision = new CreatureDecision(
                            resource.Kind == ResourceKind.Food ? CreatureAction.Eat : CreatureAction.Drink,
                            decision.TargetResourceIndex,
                            decision.Score);
                    }
                }

                if (decision.Action == CreatureAction.SeekCarcass
                    && (uint)decision.TargetResourceIndex < (uint)Resources.Count)
                {
                    ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                    if (resource.Kind == ResourceKind.Carcass
                        && SimVector2.Distance(movement.Position, resource.Position) <= resource.InteractionRadius)
                    {
                        decision = new CreatureDecision(CreatureAction.FeedCarcass, decision.TargetResourceIndex, decision.Score);
                    }
                }

                if (decision.Action == CreatureAction.SeekPrey
                    && Creatures.TryGetIndex(decision.TargetCreatureId, out int preyIndex)
                    && SimVector2.Distance(movement.Position, Creatures.GetMovementAt(preyIndex).Position) <= 1.1f)
                {
                    decision = new CreatureDecision(
                        CreatureAction.Attack,
                        -1,
                        decision.Score,
                        targetCreatureId: decision.TargetCreatureId);
                }

                Creatures.SetDecisionAt(index, new CreatureDecision(
                    decision.Action,
                    decision.TargetResourceIndex,
                    decision.Score,
                    tick,
                    decision.TargetCreatureId));
                if (Config.ForagingEconomicsEnabled && previousDecision.Action != decision.Action)
                {
                    Creatures.GetForagingRefAt(index).SecondsInCurrentAction = 0f;
                }
                Creatures.SetDecisionDiagnosticsAt(index, diagnostics.WithWinningAction(decision.Action));
                if (DecisionTrace != null)
                {
                    DecisionInvalidationReason invalidationReason = DetermineDecisionInvalidation(previousDecision, selectedIntent, decision);
                    DecisionTrace.Record(new DecisionTraceEntry(tick, Creatures.GetIdAt(index), previousDecision, decision, diagnostics, invalidationReason));
                }
            }
        }

        private DecisionInvalidationReason DetermineDecisionInvalidation(CreatureDecision previousDecision, CreatureDecision selectedIntent, CreatureDecision executionDecision)
        {
            if (previousDecision.TargetResourceIndex >= 0
                && (uint)previousDecision.TargetResourceIndex < (uint)Resources.Count)
            {
                ResourceState previousResource = Resources.GetAt(previousDecision.TargetResourceIndex);
                if (!previousResource.IsActive || previousResource.Amount <= 0f)
                {
                    return DecisionInvalidationReason.PreviousResourceUnavailable;
                }
            }

            if (selectedIntent.Action != executionDecision.Action
                || selectedIntent.TargetResourceIndex != executionDecision.TargetResourceIndex
                || !selectedIntent.TargetCreatureId.Equals(executionDecision.TargetCreatureId))
            {
                return DecisionInvalidationReason.ExecutionTransition;
            }

            return previousDecision.Action != selectedIntent.Action
                || previousDecision.TargetResourceIndex != selectedIntent.TargetResourceIndex
                || !previousDecision.TargetCreatureId.Equals(selectedIntent.TargetCreatureId)
                ? DecisionInvalidationReason.HigherScoredIntent
                : DecisionInvalidationReason.None;
        }

        private void RebuildResourceGrid()
        {
            EnsureResourcePositionCapacity(Resources.Count);
            for (int index = 0; index < Resources.Count; index++)
            {
                _resourcePositions[index] = Resources.GetAt(index).Position;
            }

            ResourceGrid.Rebuild(_resourcePositions, Resources.Count);
        }

        private void EnsureResourcePositionCapacity(int required)
        {
            if (required > _resourcePositions.Length)
            {
                Array.Resize(ref _resourcePositions, Math.Max(required, _resourcePositions.Length * 2));
            }
        }

        private void RebuildCombatGrid()
        {
            EnsureCreaturePositionCapacity(Creatures.Count);
            for (int index = 0; index < Creatures.Count; index++)
            {
                _creaturePositions[index] = Creatures.GetMovementAt(index).Position;
            }

            CombatGrid.Rebuild(_creaturePositions, Creatures.Count);
        }

        private void EnsureCreaturePositionCapacity(int required)
        {
            if (required > _creaturePositions.Length)
            {
                Array.Resize(ref _creaturePositions, Math.Max(required, _creaturePositions.Length * 2));
            }
        }

        private void ResolveResourceInteractions()
        {
            if (Config.ForagingEconomicsEnabled)
            {
                EnsureForagingEnergyGainedCapacity(Creatures.Count);
                Array.Clear(_foragingEnergyGained, 0, Creatures.Count);
            }

            _resourceRequestCount = 0;
            for (int creatureIndex = 0; creatureIndex < Creatures.Count; creatureIndex++)
            {
                CreatureDecision decision = Creatures.GetDecisionAt(creatureIndex);
                if ((decision.Action != CreatureAction.SeekFood
                        && decision.Action != CreatureAction.SeekWater
                        && decision.Action != CreatureAction.Eat
                        && decision.Action != CreatureAction.Drink
                        && decision.Action != CreatureAction.SeekCarcass
                        && decision.Action != CreatureAction.FeedCarcass)
                    || (uint)decision.TargetResourceIndex >= (uint)Resources.Count)
                {
                    continue;
                }

                ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                if (!resource.IsActive || resource.Amount <= 0f
                    || ((decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.Eat) && resource.Kind != ResourceKind.Food)
                    || ((decision.Action == CreatureAction.SeekWater || decision.Action == CreatureAction.Drink) && resource.Kind != ResourceKind.Water)
                    || ((decision.Action == CreatureAction.SeekCarcass || decision.Action == CreatureAction.FeedCarcass) && resource.Kind != ResourceKind.Carcass))
                {
                    continue;
                }

                MovementState movement = Creatures.GetMovementAt(creatureIndex);
                if (SimVector2.Distance(movement.Position, resource.Position) > resource.InteractionRadius)
                {
                    continue;
                }

                Phenotype phenotype = Creatures.GetPhenotypeAt(creatureIndex);
                float requestedAmount = (decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.Eat || decision.Action == CreatureAction.SeekCarcass || decision.Action == CreatureAction.FeedCarcass)
                    ? phenotype.IngestionRate * Config.FixedDeltaTime
                    : 1.25f * Config.FixedDeltaTime;
                EnsureResourceRequestCapacity(_resourceRequestCount + 1);
                _resourceRequests[_resourceRequestCount++] = new ResourceRequest(
                    decision.TargetResourceIndex,
                    creatureIndex,
                    requestedAmount);
            }

            ResourceAllocationSystem.Resolve(Resources, _resourceRequests, _resourceRequestCount, _resourceAllocations);
            for (int requestIndex = 0; requestIndex < _resourceRequestCount; requestIndex++)
            {
                float allocatedAmount = _resourceAllocations[requestIndex];
                if (allocatedAmount <= 0f)
                {
                    continue;
                }

                ResourceRequest request = _resourceRequests[requestIndex];
                ResourceState resource = Resources.GetAt(request.ResourceIndex);
                if (Config.PlantCohortsEnabled && resource.Kind == ResourceKind.Food)
                {
                    int plantPatchIndex = Plants.FindIndex(resource.Id);
                    if (plantPatchIndex >= 0)
                    {
                        _cumulativePlantBiomassConsumed += Plants.ConsumeAt(plantPatchIndex, allocatedAmount);
                    }
                }
                ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(request.CreatureIndex);
                if (resource.Kind == ResourceKind.Food || resource.Kind == ResourceKind.Carcass)
                {
                    Phenotype phenotype = Creatures.GetPhenotypeAt(request.CreatureIndex);
                    Genome genome = Creatures.GetGenomeAt(request.CreatureIndex);
                    float nutrition = resource.Kind == ResourceKind.Carcass
                        ? allocatedAmount * phenotype.MeatYieldMultiplier
                        : allocatedAmount * phenotype.PlantFoodYieldMultiplier * resource.NutritionMultiplier * (1f - (resource.PlantDefense * (1f - genome.FoodEfficiency)));
                    NeedsSystem.ConsumeFood(ref needs, phenotype, nutrition);
                    _cumulativeFoodConsumed += nutrition;
                    if (Config.ForagingEconomicsEnabled)
                    {
                        _foragingEnergyGained[request.CreatureIndex] += nutrition;
                    }
                    if (resource.Kind == ResourceKind.Carcass)
                    {
                        _cumulativeCarcassConsumed += nutrition;
                    }
                    else if (Config.CognitionEnabled)
                    {
                        float actualFoodIntakeRate = nutrition * phenotype.FoodYield / Config.FixedDeltaTime;
                        MemorySystem.LearnResourceOutcome(
                            ref Creatures.GetMemoryRefAt(request.CreatureIndex),
                            ResourceKind.Food,
                            MemorySystem.ComputeIntakeOutcome(actualFoodIntakeRate, Config.ExpectedIntakeRate),
                            phenotype.LearningRate);
                    }
                }
                else
                {
                    NeedsSystem.DrinkWater(ref needs, Creatures.GetPhenotypeAt(request.CreatureIndex), allocatedAmount);
                    _cumulativeWaterConsumed += allocatedAmount;
                    if (Config.CognitionEnabled)
                    {
                        float actualWaterIntakeRate = allocatedAmount / Config.FixedDeltaTime;
                        MemorySystem.LearnResourceOutcome(
                            ref Creatures.GetMemoryRefAt(request.CreatureIndex),
                            ResourceKind.Water,
                            MemorySystem.ComputeIntakeOutcome(actualWaterIntakeRate, Config.ExpectedIntakeRate),
                            Creatures.GetPhenotypeAt(request.CreatureIndex).LearningRate);
                    }
                }
            }
        }

        private void EnsureResourceRequestCapacity(int required)
        {
            if (required <= _resourceRequests.Length)
            {
                return;
            }

            int nextCapacity = Math.Max(required, _resourceRequests.Length * 2);
            Array.Resize(ref _resourceRequests, nextCapacity);
            Array.Resize(ref _resourceAllocations, nextCapacity);
        }

        private void TickReproduction()
        {
            _birthCount += _reproduction.Step(
                Config.WorldSeed,
                1f / Config.Schedule.ReproductionHz,
                ref _birthOrdinal,
                CurrentTick + 1,
                Config.MaximumPopulation,
                Events);
        }

        private void TickCombat(long tick)
        {
            EnsureCombatDamageCapacity(Creatures.Count);
            Array.Clear(_combatDamage, 0, Creatures.Count);
            for (int index = 0; index < Creatures.Count; index++)
            {
                ref CombatState combat = ref Creatures.GetCombatRefAt(index);
                combat.AttackRecoveryRemaining = Math.Max(0f, combat.AttackRecoveryRemaining - Config.FixedDeltaTime);
                CreatureDecision decision = Creatures.GetDecisionAt(index);
                if (decision.Action != CreatureAction.Attack
                    || combat.AttackRecoveryRemaining > 0f
                    || !Creatures.TryGetIndex(decision.TargetCreatureId, out int targetIndex)
                    || targetIndex == index)
                {
                    continue;
                }

                MovementState attackerMovement = Creatures.GetMovementAt(index);
                MovementState defenderMovement = Creatures.GetMovementAt(targetIndex);
                float engagementDistance = SimVector2.Distance(attackerMovement.Position, defenderMovement.Position);
                if (engagementDistance > 1.1f)
                {
                    continue;
                }

                Phenotype attacker = Creatures.GetPhenotypeAt(index);
                Phenotype defender = Creatures.GetPhenotypeAt(targetIndex);
                float hitChance = 0.20f + (0.70f * PredationSystem.Threat(attacker, defender, engagementDistance, Config.PredationEconomicsEnabled));
                float roll = DeterministicRandom.Float01(
                    Config.WorldSeed,
                    RandomDomain.AttackResolution,
                    tick,
                    Creatures.GetIdAt(index).Value,
                    decision.TargetCreatureId.Value,
                    0);
                combat.AttackRecoveryRemaining = 0.75f;
                if (roll > hitChance)
                {
                    continue;
                }

                float damage = 4f + (12f * attacker.AttackPower);
                _combatDamage[targetIndex] += damage;
                _attackHitCount++;
            }

            for (int index = 0; index < Creatures.Count; index++)
            {
                float damage = _combatDamage[index];
                if (damage <= 0f)
                {
                    continue;
                }

                Phenotype defender = Creatures.GetPhenotypeAt(index);
                ref CreatureNeeds targetNeeds = ref Creatures.GetNeedsRefAt(index);
                ref CombatState targetCombat = ref Creatures.GetCombatRefAt(index);
                targetNeeds.Health -= damage;
                targetCombat.WoundSeverity += damage / defender.HealthCapacity;
                if (targetNeeds.Health <= 0f) RequestDeath(Creatures.GetIdAt(index), DeathCause.Predation);
            }
        }

        private void EnsureCombatDamageCapacity(int required)
        {
            if (required > _combatDamage.Length)
            {
                Array.Resize(ref _combatDamage, Math.Max(required, _combatDamage.Length * 2));
            }
        }

        private void AdvanceForagingActionTime(float deltaTime)
        {
            for (int index = 0; index < Creatures.Count; index++)
            {
                Creatures.GetForagingRefAt(index).SecondsInCurrentAction += deltaTime;
            }
        }

        private void UpdateForagingIntakeRate(float deltaTime)
        {
            float smoothing = Math.Min(1f, deltaTime / ForagingIntakeRateWindowSeconds);
            for (int index = 0; index < Creatures.Count; index++)
            {
                ref ForagingState foraging = ref Creatures.GetForagingRefAt(index);
                float sampleRate = _foragingEnergyGained[index] / deltaTime;
                foraging.RecentIntakeRate += (sampleRate - foraging.RecentIntakeRate) * smoothing;
            }
        }

        private int GetUsableMemorySlotCount(Genome genome)
        {
            return SimulationConfig.ComputeMemorySlotCount(Config.MinimumMemorySlots, Config.AdditionalMemorySlots, genome.MemoryCapacity);
        }

        /// <summary>
        /// Scores every occupied, non-zero-confidence place-memory slot up to <paramref name="usableSlotCount"/>
        /// with <see cref="ForagingEconomics.PatchScore"/> - substituting each place's <c>LastKnownAmount</c>
        /// for the observed remaining amount - discounted by that place's <c>Confidence</c>, and reduced by
        /// <see cref="ForagingEconomics.ThreatAvoidance"/> for the creature's remembered threat (if any, from
        /// <paramref name="threatPosition"/>/<paramref name="threatConfidence"/>). Returns the highest-scoring
        /// place, if any occupied slot exists and its net score is positive.
        /// </summary>
        private bool TryScoreBestRememberedPlace(
            int creatureIndex,
            int usableSlotCount,
            Phenotype phenotype,
            CreatureNeeds needs,
            SimVector2 origin,
            SimVector2 threatPosition,
            float threatConfidence,
            out SimVector2 bestPosition,
            out ResourceKind bestKind,
            out float bestScore)
        {
            bestPosition = default;
            bestKind = default;
            bestScore = 0f;
            bool found = false;

            Span<PlaceMemory> threatPlaces = stackalloc PlaceMemory[1];
            int threatPlaceCount = 0;
            if (threatConfidence > 0f)
            {
                threatPlaces[0] = new PlaceMemory { Position = threatPosition, Confidence = threatConfidence };
                threatPlaceCount = 1;
            }

            for (int slot = 0; slot < usableSlotCount; slot++)
            {
                PlaceMemory place = Creatures.GetPlaceMemoryRefAt(creatureIndex, slot);
                if (place.VisitCount <= 0 || place.Confidence <= 0f)
                {
                    continue;
                }

                bool seekingWater = place.Kind == ResourceKind.Water;
                float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
                float current = seekingWater ? needs.Hydration : needs.Energy;
                float urgency = Math.Max(0f, Math.Min(1f, 1f - (current / capacity)));
                float distance = SimVector2.Distance(origin, place.Position);
                float rawScore = ForagingEconomics.PatchScore(urgency, place.LastKnownAmount, distance, phenotype, 1f, Config.HandlingSeconds, Config.ReferenceGain) * place.Confidence;
                float avoidance = ForagingEconomics.ThreatAvoidance(place.Position, threatPlaces.Slice(0, threatPlaceCount), phenotype, Config.ThreatFalloffDistance);
                float score = rawScore - avoidance;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = place.Position;
                    bestKind = place.Kind;
                    found = true;
                }
            }

            return found;
        }

        private void EnsureForagingEnergyGainedCapacity(int required)
        {
            if (required > _foragingEnergyGained.Length)
            {
                Array.Resize(ref _foragingEnergyGained, Math.Max(required, _foragingEnergyGained.Length * 2));
            }
        }

        private SimulationStatistics BuildStatistics(long tick)
        {
            float bodySizeTotal = 0f;
            float movementSpeedTotal = 0f;
            float metabolicPaceTotal = 0f;
            float visionRangeTotal = 0f;
            float waterEfficiencyTotal = 0f;
            float foodEfficiencyTotal = 0f;
            float temperatureToleranceTotal = 0f;
            float fertilityInvestmentTotal = 0f;
            float lifespanTendencyTotal = 0f;
            float urgencyExponentTotal = 0f;
            float travelSensitivityTotal = 0f;
            float riskAversionTotal = 0f;
            float commitmentTotal = 0f;
            float attackTotal = 0f;
            float defenseTotal = 0f;
            float aggressionTotal = 0f;
            float dietSpecializationTotal = 0f;
            int viableHunterCount = 0;
            float memoryCapacityTotal = 0f;
            float memoryRetentionTotal = 0f;
            float learningRateTotal = 0f;
            float explorationTotal = 0f;
            float energyFractionTotal = 0f;
            float hydrationFractionTotal = 0f;
            int highestGeneration = 0;
            for (int index = 0; index < Creatures.Count; index++)
            {
                Genome genome = Creatures.GetGenomeAt(index);
                Phenotype phenotype = Creatures.GetPhenotypeAt(index);
                CreatureNeeds needs = Creatures.GetNeedsAt(index);
                bodySizeTotal += genome.BodySize;
                movementSpeedTotal += genome.MovementSpeed;
                metabolicPaceTotal += genome.MetabolicPace;
                visionRangeTotal += genome.VisionRange;
                waterEfficiencyTotal += genome.WaterEfficiency;
                foodEfficiencyTotal += genome.FoodEfficiency;
                temperatureToleranceTotal += genome.TemperatureTolerance;
                fertilityInvestmentTotal += genome.FertilityInvestment;
                lifespanTendencyTotal += genome.LifespanTendency;
                urgencyExponentTotal += genome.UrgencyExponent;
                travelSensitivityTotal += genome.TravelSensitivity;
                riskAversionTotal += genome.RiskAversion;
                commitmentTotal += genome.Commitment;
                attackTotal += genome.Attack;
                defenseTotal += genome.Defense;
                aggressionTotal += genome.Aggression;
                dietSpecializationTotal += genome.DietSpecialization;
                if (PredationSystem.HasViableHuntingStrategy(phenotype)) viableHunterCount++;
                memoryCapacityTotal += genome.MemoryCapacity;
                memoryRetentionTotal += genome.MemoryRetention;
                learningRateTotal += genome.LearningRate;
                explorationTotal += genome.Exploration;
                energyFractionTotal += needs.Energy / phenotype.EnergyCapacity;
                hydrationFractionTotal += needs.Hydration / phenotype.HydrationCapacity;
                highestGeneration = Math.Max(highestGeneration, Creatures.GetLineageAt(index).Generation);
            }

            float food = 0f;
            float water = 0f;
            float plantBiomass = 0f;
            int dormantPlantPatches = 0;
            int highestPlantGeneration = 0;
            float plantGrowthTotal = 0f;
            float plantNutritionTotal = 0f;
            float plantDefenseTotal = 0f;
            for (int index = 0; index < Resources.Count; index++)
            {
                ResourceState resource = Resources.GetAt(index);
                if (resource.Kind == ResourceKind.Food) food += resource.Amount;
                else if (resource.Kind == ResourceKind.Water) water += resource.Amount;
            }

            for (int index = 0; index < Plants.Count; index++)
            {
                PlantPatchState patch = Plants.GetAt(index);
                plantBiomass += patch.Biomass;
                if (patch.IsDormant) dormantPlantPatches++;
                highestPlantGeneration = Math.Max(highestPlantGeneration, patch.Lineage.Generation);
                plantGrowthTotal += patch.Genome.Growth;
                plantNutritionTotal += patch.Genome.Nutrition;
                plantDefenseTotal += patch.Genome.Defense;
            }

            float reciprocalPopulation = Creatures.Count == 0 ? 0f : 1f / Creatures.Count;
            return new SimulationStatistics(
                tick,
                Creatures.Count,
                highestGeneration,
                bodySizeTotal * reciprocalPopulation,
                movementSpeedTotal * reciprocalPopulation,
                metabolicPaceTotal * reciprocalPopulation,
                visionRangeTotal * reciprocalPopulation,
                waterEfficiencyTotal * reciprocalPopulation,
                foodEfficiencyTotal * reciprocalPopulation,
                energyFractionTotal * reciprocalPopulation,
                hydrationFractionTotal * reciprocalPopulation,
                food,
                water,
                _cumulativeFoodConsumed,
                _cumulativeWaterConsumed,
                _birthCount,
                _deathCount,
                _attackHitCount,
                _predationDeathCount,
                _cumulativeCarcassConsumed,
                temperatureToleranceTotal * reciprocalPopulation,
                fertilityInvestmentTotal * reciprocalPopulation,
                lifespanTendencyTotal * reciprocalPopulation,
                urgencyExponentTotal * reciprocalPopulation,
                travelSensitivityTotal * reciprocalPopulation,
                riskAversionTotal * reciprocalPopulation,
                commitmentTotal * reciprocalPopulation,
                _starvationDeathCount,
                _dehydrationDeathCount,
                _ageDeathCount,
                _healthDeathCount,
                attackTotal * reciprocalPopulation,
                defenseTotal * reciprocalPopulation,
                aggressionTotal * reciprocalPopulation,
                dietSpecializationTotal * reciprocalPopulation,
                viableHunterCount,
                memoryCapacityTotal * reciprocalPopulation,
                memoryRetentionTotal * reciprocalPopulation,
                learningRateTotal * reciprocalPopulation,
                explorationTotal * reciprocalPopulation,
                plantBiomass,
                _cumulativePlantGrowth,
                _cumulativePlantBiomassConsumed,
                dormantPlantPatches,
                plantBiomass - (_initialPlantBiomass + _cumulativePlantGrowth - _cumulativePlantBiomassConsumed),
                _plantBirthCount,
                Plants.Count,
                highestPlantGeneration,
                Plants.Count == 0 ? 0f : plantGrowthTotal / Plants.Count,
                Plants.Count == 0 ? 0f : plantNutritionTotal / Plants.Count,
                Plants.Count == 0 ? 0f : plantDefenseTotal / Plants.Count);
        }

        private void CountDeathCause(DeathCause cause)
        {
            switch (cause)
            {
                case DeathCause.Starvation: _starvationDeathCount++; break;
                case DeathCause.Dehydration: _dehydrationDeathCount++; break;
                case DeathCause.Age: _ageDeathCount++; break;
                case DeathCause.Health: _healthDeathCount++; break;
                case DeathCause.Predation: _predationDeathCount++; break;
            }
        }

        private static float Lerp(float minimum, float maximum, float t)
        {
            return minimum + ((maximum - minimum) * t);
        }

        private static ulong Hash(ulong hash, ulong value)
        {
            return (hash ^ value) * 1099511628211UL;
        }

        private static ulong HashFloat(ulong hash, float value)
        {
            return Hash(hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(value)));
        }
    }
}
