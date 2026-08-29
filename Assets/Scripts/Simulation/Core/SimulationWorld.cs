using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Spatial;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Core
{
    public sealed partial class SimulationWorld
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

        /// <summary>
        /// How often creatures actually choose to flee, and how many decisions were taken in total.
        /// Counting only - never hashed, never read by any system - because the claim it exists to
        /// test was inferred rather than measured: `risk_aversion` is selected against at t = -3 to
        /// -6 and the proposed reason is that its foraging-caution role outweighs its flee role, but
        /// nothing reported how often fleeing even happened. See
        /// <c>docs/emergent-behaviour-fleeing-is-selected-against-2026-08-29.md</c>.
        /// </summary>
        private int _fleeDecisionCount;

        private int _decisionCount;

        /// <summary>Health removed by combat, cumulative. Diagnostics only; never hashed.</summary>
        private float _cumulativeCombatDamage;

        private float _defenseAtDeathTotal;

        private int _defenseAtDeathCount;

        private float _defenseAtPredationDeathTotal;

        private int _defenseAtPredationDeathCount;
        private int _predationDeathCount;
        private float _cumulativePlantGrowth;
        private float _cumulativePlantBiomassConsumed;
        private float _cumulativePlantBiomassLostToMortality;
        private float _initialPlantBiomass;
        private float _plantBiomassSeconds;
        private float _plantPatchSeconds;
        private long _plantSeedOrdinal;
        private int _plantBirthCount;

        public SimulationWorld(SimulationConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Config.Validate();
            Creatures = new CreatureStore(Config.InitialPopulation, Config.MaximumMemorySlots, Config.MetabolicIngestionEnabled);
            Resources = new ResourceStore(initialCapacity: 8);
            Plants = new PlantPatchStore(initialCapacity: 8);
            PlantSites = new PlantSiteRegistry(initialCapacity: 8);
            Environment = Config.ProceduralEnvironmentFieldsEnabled
                ? Config.TerrainDrivenEnvironmentEnabled
                    ? EnvironmentField.CreateTerrainDriven(Config.WorldSeed)
                    : EnvironmentField.CreateProcedural(Config.WorldSeed, Config.ElevationFieldEnabled)
                : Config.PlantCohortsEnabled ? EnvironmentField.CreateMoistureGradient() : new EnvironmentField();
            Climate = Config.TerrainDrivenTemperatureEnabled
                ? ClimateField.FromTerrain(Environment)
                : default;
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
            _reproduction = new ReproductionSystem(Creatures, Arena, Config.InitialPopulation, Config.PhysiologyEnabled, Config.MateSelectionEnabled, Config.ReproductionNeedFraction, Config.GradedFertilityEnabled, Config.GradedFertilityStrength);
            Events = new SimulationEventBuffer(capacity: 1024);

            for (int index = 0; index < Config.InitialPopulation; index++)
            {
                Spawn(CreateFounderGenome(index));
            }
        }

        public SimulationConfig Config { get; }

        /// <summary>
        /// Where a creature's temperature in degrees comes from. A <c>default</c> instance is the
        /// fixed sine every recorded thermal result was measured against.
        /// </summary>
        public ClimateField Climate { get; }
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
                neutralMarker: FounderGene(founderOrdinal, 26, standardDeviation));
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

            // Defense of the creature that is dying, accumulated here because this is the only place
            // that still has its index. Added 2026-08-26 to answer the one question the combat
            // instruments left open: `defense` is under t = 11 selection while predation kills 0.53
            // creatures per run, and a small number of deaths falling on the LOW-defense tail
            // outruns a large number falling at random. Comparing these means against the population
            // mean is what separates the two; nothing reported it.
            float dyingDefense = Creatures.GetGenomeAt(creatureIndex).Defense;
            _defenseAtDeathTotal += dyingDefense;
            _defenseAtDeathCount++;
            if (cause == DeathCause.Predation)
            {
                _defenseAtPredationDeathTotal += dyingDefense;
                _defenseAtPredationDeathCount++;
            }
        }

        /// <summary>
        /// Hash of everything a gene can only reach <i>through behavior</i>: needs, movement,
        /// decisions, combat, reproduction, memory, population, resources and plant biomass —
        /// but no genome or phenotype field.
        ///
        /// This is the basis of the gene liveness test. Perturbing a gene always moves
        /// <see cref="ComputeStateHash"/>, because that hash includes the genome directly; it moves
        /// this hash only if the gene actually influenced something. A gene whose perturbation
        /// leaves this hash untouched over a long run has no path to behavior, which is precisely
        /// the <c>NeutralMarker</c> shape that a caller-search cannot detect.
        ///
        /// Deliberately excludes <c>LivenessRecorder</c> counters, which must never affect any hash.
        /// </summary>
        /// <summary>
        /// Optional runtime liveness probe sink. <b>Null by default</b>, and deliberately not gated
        /// by a <c>SimulationConfig</c> flag: a diagnostics flag would have to be behavior-inert to
        /// be correct, and <c>FlagLivenessAnalysis</c> would then report it as inert and fail the
        /// known-inert-flag assertion. An optional sink avoids that contradiction and costs a null
        /// check when unused.
        ///
        /// <para>Nothing in the simulation may ever read this. It records only; see
        /// <c>LivenessRecorder</c>. It is absent from both hashes by construction.</para>
        ///
        /// <para>Covers what perturbation cannot: the �4 "executes but always on empty data" class,
        /// where there is no gene or flag to flip. <c>TryScoreBestRememberedPlace</c> and
        /// <c>RecordFailedPlaceSearch</c> run every tick against permanently empty place-memory
        /// slots, and place memory reading as live is what produced the retracted root cause in
        /// docs/experiments/p4-memory-root-cause-retracted-2026-08-17.md.</para>
        /// </summary>
        public LivenessRecorder Liveness { get; set; }

        /// <summary>
        /// Overwrite one trait across every living creature. Diagnostics only — used by the gene
        /// liveness harness to inject a perturbation before stepping. Not called by simulation logic.
        /// </summary>
        public void OverwriteTraitForAllCreatures(int traitIndex, float value)
        {
            for (int index = 0; index < CreatureCount; index++)
            {
                Creatures.OverwriteGenomeAt(index, Creatures.GetGenomeAt(index).WithTrait(traitIndex, value));
            }
        }

        /// <summary>
        /// Overwrite one plant trait across every live patch. Diagnostics only — the plant-side
        /// counterpart of <see cref="OverwriteTraitForAllCreatures"/>, used by
        /// <c>PlantGeneLivenessAnalysis</c>. Not called by simulation logic.
        ///
        /// <para>Lineage is preserved so the perturbation changes only the trait under test.</para>
        /// </summary>
        public void OverwritePlantTraitForAllPatches(int traitIndex, float value)
        {
            for (int index = 0; index < Plants.Count; index++)
            {
                PlantPatchState patch = Plants.GetAt(index);
                Plants.SetGenomeAndLineage(index, patch.Genome.WithTrait(traitIndex, value), patch.Lineage);
            }
        }

        /// <summary>Field-set version for <see cref="ComputeStateFingerprint"/>. Bump on any change to the fields it covers.</summary>
        public const int StateFingerprintVersion = 2;

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
                return ThermoregulationSystem.FindNearbyComfortTarget(position, tick, Arena, Climate);
            }

            if (decision.Action == CreatureAction.Rest)
            {
                return position;
            }

            if (Config.ParentalFollowingEnabled
                && decision.Action == CreatureAction.Wander
                && Creatures.GetNeedsAt(creatureIndex).Age < ReproductionSystem.AdultAgeSeconds)
            {
                CreatureLineage lineage = Creatures.GetLineageAt(creatureIndex);
                SimVector2? parentPosition = FindNearestAliveParent(lineage, position);
                if (parentPosition.HasValue)
                {
                    const float followRadius = 2f;
                    if (SimVector2.Distance(position, parentPosition.Value) > followRadius)
                    {
                        return parentPosition.Value;
                    }

                    long followEpoch = tick / (Config.Schedule.BaseFrequencyHz * 5L);
                    float followAngle = DeterministicRandom.Float01(
                        Config.WorldSeed,
                        RandomDomain.Exploration,
                        followEpoch,
                        creatureId.Value,
                        0,
                        3) * ((float)Math.PI * 2f);
                    return new SimVector2(
                        parentPosition.Value.X + ((float)Math.Cos(followAngle) * followRadius),
                        parentPosition.Value.Y + ((float)Math.Sin(followAngle) * followRadius));
                }
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

        private SimVector2? FindNearestAliveParent(CreatureLineage lineage, SimVector2 position)
        {
            SimVector2? firstPosition = null;
            if (Creatures.TryGetIndex(lineage.FirstParent, out int firstIndex))
            {
                firstPosition = Creatures.GetMovementAt(firstIndex).Position;
            }

            SimVector2? secondPosition = null;
            if (Creatures.TryGetIndex(lineage.SecondParent, out int secondIndex))
            {
                secondPosition = Creatures.GetMovementAt(secondIndex).Position;
            }

            if (!firstPosition.HasValue)
            {
                return secondPosition;
            }

            if (!secondPosition.HasValue)
            {
                return firstPosition;
            }

            float firstDistance = SimVector2.Distance(position, firstPosition.Value);
            float secondDistance = SimVector2.Distance(position, secondPosition.Value);
            return firstDistance <= secondDistance ? firstPosition : secondPosition;
        }

        public SimVector2? FindNearestAliveParentForTest(CreatureLineage lineage, SimVector2 position)
        {
            return FindNearestAliveParent(lineage, position);
        }

        private Phenotype GetEffectivePhenotype(int index)
        {
            Phenotype phenotype = Creatures.GetPhenotypeAt(index);
            if (!Config.JuvenileCapabilityEnabled)
            {
                return phenotype;
            }

            float multiplier = JuvenileSystem.CapabilityMultiplier(Creatures.GetNeedsAt(index).Age, ReproductionSystem.AdultAgeSeconds);
            return phenotype.WithJuvenileScaling(multiplier);
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

                // Deterrence: defended tissue is harder to strip, so a defended patch loses less
                // biomass per bite and the grazer carries away correspondingly less. With the flag
                // off this is exactly allocatedAmount, so the path stays byte-identical.
                //
                // Without it, Defense scales only the nutrition term below while ConsumeAt removes
                // the full bite. Defense then protects no tissue, giving the patch carrying it no
                // individual benefit and leaving nothing for selection to act on.
                //
                // ResourceAllocationSystem.Resolve has already drawn the full bite out of the food
                // resource pool, so between resource ticks the pool under-reports what the patch
                // still holds. PlantGrowthSystem.ProjectFoodResources resyncs it from biomass on
                // the next resource tick.
                if (Config.PlantDefenseDeterrenceEnabled && resource.Kind == ResourceKind.Food)
                {
                    float undeterredAmount = allocatedAmount;
                    allocatedAmount *= 1f - (resource.PlantDefense * Config.PlantDefenseDeterrenceStrength);

                    // Known-live control for the recorder itself. A liveness tool that only ever
                    // reports INERT is indistinguishable from one that is broken, so at least one
                    // probe must sit on a path that genuinely fires.
                    Liveness?.RecordOutput(LivenessProbe.PlantDefenseDeterrence, allocatedAmount, undeterredAmount);
                    if (allocatedAmount <= 0f)
                    {
                        continue;
                    }
                }

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

                    if (Config.HomeRangeAffinityEnabled && resource.Kind == ResourceKind.Food)
                    {
                        HomeRangeSystem.RecordSuccess(
                            ref Creatures.GetHomeRangeRefAt(request.CreatureIndex),
                            Creatures.GetMovementAt(request.CreatureIndex).Position);
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

                    if (Config.HomeRangeAffinityEnabled)
                    {
                        HomeRangeSystem.RecordSuccess(
                            ref Creatures.GetHomeRangeRefAt(request.CreatureIndex),
                            Creatures.GetMovementAt(request.CreatureIndex).Position);
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
    }
}
