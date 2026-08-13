using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Spatial;

namespace LifeSimulation.Simulation.Core
{
    public sealed class SimulationWorld
    {
        private const float AdultAgeSeconds = 20f;
        private const float ReproductionCooldownSeconds = 15f;
        private CreatureId[] _pendingDeaths;
        private int _pendingDeathCount;
        private long _spawnOrdinal;
        private SimVector2[] _resourcePositions;
        private SimVector2[] _creaturePositions;
        private ResourceRequest[] _resourceRequests;
        private float[] _resourceAllocations;
        private int[] _reproductionCandidates;
        private bool[] _reproductionMatched;
        private readonly CreatureIndexComparer _creatureIndexComparer;
        private int _resourceRequestCount;
        private long _birthOrdinal;

        public SimulationWorld(SimulationConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Config.Validate();
            Creatures = new CreatureStore(Config.InitialPopulation);
            Resources = new ResourceStore(initialCapacity: 8);
            Arena = new ArenaBounds(-25f, 25f, -25f, 25f);
            ResourceGrid = new UniformGrid(Arena, cellSize: 5f, initialOccupantCapacity: 8);
            CreatureGrid = new UniformGrid(Arena, cellSize: 5f, initialOccupantCapacity: Config.InitialPopulation);
            _pendingDeaths = new CreatureId[Math.Max(Config.InitialPopulation, 1)];
            _resourcePositions = new SimVector2[8];
            _creaturePositions = new SimVector2[Math.Max(Config.InitialPopulation, 1)];
            _resourceRequests = new ResourceRequest[Math.Max(Config.InitialPopulation, 1)];
            _resourceAllocations = new float[_resourceRequests.Length];
            _reproductionCandidates = new int[Math.Max(Config.InitialPopulation, 1)];
            _reproductionMatched = new bool[_reproductionCandidates.Length];
            _creatureIndexComparer = new CreatureIndexComparer(Creatures);

            for (int index = 0; index < Config.InitialPopulation; index++)
            {
                Spawn();
            }
        }

        public SimulationConfig Config { get; }
        public CreatureStore Creatures { get; }
        public ResourceStore Resources { get; }
        public ArenaBounds Arena { get; }
        public UniformGrid ResourceGrid { get; }
        public UniformGrid CreatureGrid { get; }
        public int CreatureCount => Creatures.Count;
        public long CurrentTick { get; private set; }
        public SimulationStatistics Statistics { get; private set; }

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
            if (!Creatures.TryGetIndex(id, out _))
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
            _pendingDeaths[_pendingDeathCount++] = id;
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
                Resources.Regenerate(1f / Config.Schedule.ResourcesHz);
            }

            if (IsDue(nextTick, Config.Schedule.PerceptionHz))
            {
                RebuildResourceGrid();
            }

            if (IsDue(nextTick, Config.Schedule.DecisionsHz))
            {
                TickDecisions(nextTick);
            }

            TickMovement(nextTick);
            if (IsDue(nextTick, Config.Schedule.NeedsHz))
            {
                TickNeeds();
            }

            ResolveResourceInteractions();

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
                Creatures.Remove(_pendingDeaths[index]);
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
                hash = HashFloat(hash, decision.Score);
                hash = Hash(hash, unchecked((ulong)decision.DecisionTick));
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
                movement.DistanceSinceLastNeeds = 0f;
                if (needs.Health <= 0f)
                {
                    RequestDeath(Creatures.GetIdAt(index), DeathCause.Health);
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
                    || decision.Action == CreatureAction.Drink)
                && (uint)decision.TargetResourceIndex < (uint)Resources.Count)
            {
                ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                if (resource.IsActive && resource.Amount > 0f)
                {
                    return resource.Position;
                }
            }

            float angle = DeterministicRandom.Float01(
                Config.WorldSeed,
                RandomDomain.Wander,
                tick,
                creatureId.Value,
                0,
                0) * ((float)Math.PI * 2f);
            return new SimVector2(
                position.X + (float)Math.Cos(angle),
                position.Y + (float)Math.Sin(angle));
        }

        private void TickDecisions(long tick)
        {
            for (int index = 0; index < Creatures.Count; index++)
            {
                MovementState movement = Creatures.GetMovementAt(index);
                Phenotype phenotype = Creatures.GetPhenotypeAt(index);
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
                CreatureDecision decision = DecisionSystem.Decide(Creatures.GetNeedsAt(index), phenotype, food, water);
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

                Creatures.SetDecisionAt(index, new CreatureDecision(
                    decision.Action,
                    decision.TargetResourceIndex,
                    decision.Score,
                    tick));
            }
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

        private void ResolveResourceInteractions()
        {
            _resourceRequestCount = 0;
            for (int creatureIndex = 0; creatureIndex < Creatures.Count; creatureIndex++)
            {
                CreatureDecision decision = Creatures.GetDecisionAt(creatureIndex);
                if ((decision.Action != CreatureAction.SeekFood
                        && decision.Action != CreatureAction.SeekWater
                        && decision.Action != CreatureAction.Eat
                        && decision.Action != CreatureAction.Drink)
                    || (uint)decision.TargetResourceIndex >= (uint)Resources.Count)
                {
                    continue;
                }

                ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                if (!resource.IsActive || resource.Amount <= 0f
                    || ((decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.Eat) && resource.Kind != ResourceKind.Food)
                    || ((decision.Action == CreatureAction.SeekWater || decision.Action == CreatureAction.Drink) && resource.Kind != ResourceKind.Water))
                {
                    continue;
                }

                MovementState movement = Creatures.GetMovementAt(creatureIndex);
                if (SimVector2.Distance(movement.Position, resource.Position) > resource.InteractionRadius)
                {
                    continue;
                }

                Phenotype phenotype = Creatures.GetPhenotypeAt(creatureIndex);
                float requestedAmount = (decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.Eat)
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
                ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(request.CreatureIndex);
                if (resource.Kind == ResourceKind.Food)
                {
                    NeedsSystem.ConsumeFood(ref needs, Creatures.GetPhenotypeAt(request.CreatureIndex), allocatedAmount);
                }
                else
                {
                    NeedsSystem.DrinkWater(ref needs, Creatures.GetPhenotypeAt(request.CreatureIndex), allocatedAmount);
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
            int candidateCount = Creatures.Count;
            RebuildCreatureGrid(candidateCount);
            EnsureReproductionCapacity(candidateCount);
            Array.Clear(_reproductionMatched, 0, candidateCount);
            float deltaTime = 1f / Config.Schedule.ReproductionHz;
            for (int index = 0; index < candidateCount; index++)
            {
                ref ReproductionState reproduction = ref Creatures.GetReproductionRefAt(index);
                reproduction.CooldownRemaining = Math.Max(0f, reproduction.CooldownRemaining - deltaTime);
                _reproductionCandidates[index] = index;
            }

            Array.Sort(_reproductionCandidates, 0, candidateCount, _creatureIndexComparer);
            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                int firstIndex = _reproductionCandidates[candidateIndex];
                if (_reproductionMatched[firstIndex] || !IsReadyToReproduce(firstIndex))
                {
                    continue;
                }

                int secondIndex = FindNearestReadyMate(firstIndex, candidateCount);
                if (secondIndex < 0)
                {
                    continue;
                }

                CreateChild(firstIndex, secondIndex);
                _reproductionMatched[firstIndex] = true;
                _reproductionMatched[secondIndex] = true;
            }
        }

        private void RebuildCreatureGrid(int count)
        {
            EnsureCreaturePositionCapacity(count);
            for (int index = 0; index < count; index++)
            {
                _creaturePositions[index] = Creatures.GetMovementAt(index).Position;
            }

            CreatureGrid.Rebuild(_creaturePositions, count);
        }

        private int FindNearestReadyMate(int firstIndex, int candidateCount)
        {
            const float mateDistance = 2f;
            SimVector2 firstPosition = Creatures.GetMovementAt(firstIndex).Position;
            int minimumColumn = CreatureGrid.GetColumn(firstPosition.X - mateDistance);
            int maximumColumn = CreatureGrid.GetColumn(firstPosition.X + mateDistance);
            int minimumRow = CreatureGrid.GetRow(firstPosition.Y - mateDistance);
            int maximumRow = CreatureGrid.GetRow(firstPosition.Y + mateDistance);
            int bestIndex = -1;
            float bestDistance = mateDistance;

            for (int row = minimumRow; row <= maximumRow; row++)
            {
                for (int column = minimumColumn; column <= maximumColumn; column++)
                {
                    int cell = CreatureGrid.GetCellIndex(column, row);
                    for (int occupant = CreatureGrid.GetCellStart(cell); occupant < CreatureGrid.GetCellEnd(cell); occupant++)
                    {
                        int candidate = CreatureGrid.GetOccupantIndexAt(occupant);
                        if (candidate == firstIndex || candidate >= candidateCount || _reproductionMatched[candidate] || !IsReadyToReproduce(candidate))
                        {
                            continue;
                        }

                        float distance = SimVector2.Distance(firstPosition, Creatures.GetMovementAt(candidate).Position);
                        if (distance > mateDistance || (bestIndex >= 0 && distance > bestDistance))
                        {
                            continue;
                        }

                        if (bestIndex < 0 || distance < bestDistance || Creatures.GetIdAt(candidate).Value < Creatures.GetIdAt(bestIndex).Value)
                        {
                            bestIndex = candidate;
                            bestDistance = distance;
                        }
                    }
                }
            }

            return bestIndex;
        }

        private void CreateChild(int firstIndex, int secondIndex)
        {
            CreatureId firstParent = Creatures.GetIdAt(firstIndex);
            CreatureId secondParent = Creatures.GetIdAt(secondIndex);
            Genome childGenome = GenomeInheritance.CreateChild(
                Creatures.GetGenomeAt(firstIndex),
                Creatures.GetGenomeAt(secondIndex),
                Config.WorldSeed,
                _birthOrdinal++,
                mutationStandardDeviation: 0.03f);
            SimVector2 firstPosition = Creatures.GetMovementAt(firstIndex).Position;
            SimVector2 secondPosition = Creatures.GetMovementAt(secondIndex).Position;
            Creatures.AddChild(
                childGenome,
                new SimVector2((firstPosition.X + secondPosition.X) * 0.5f, (firstPosition.Y + secondPosition.Y) * 0.5f),
                firstParent,
                secondParent);
            ChargeReproductionCost(firstIndex);
            ChargeReproductionCost(secondIndex);
            Creatures.GetReproductionRefAt(firstIndex).CooldownRemaining = ReproductionCooldownSeconds;
            Creatures.GetReproductionRefAt(secondIndex).CooldownRemaining = ReproductionCooldownSeconds;
            Creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.Reproduce, -1, 1f, CurrentTick + 1));
            Creatures.SetDecisionAt(secondIndex, new CreatureDecision(CreatureAction.Reproduce, -1, 1f, CurrentTick + 1));
        }

        private void EnsureCreaturePositionCapacity(int required)
        {
            if (required > _creaturePositions.Length)
            {
                Array.Resize(ref _creaturePositions, Math.Max(required, _creaturePositions.Length * 2));
            }
        }

        private void EnsureReproductionCapacity(int required)
        {
            if (required > _reproductionCandidates.Length)
            {
                int capacity = Math.Max(required, _reproductionCandidates.Length * 2);
                Array.Resize(ref _reproductionCandidates, capacity);
                Array.Resize(ref _reproductionMatched, capacity);
            }
        }

        private bool IsReadyToReproduce(int index)
        {
            CreatureNeeds needs = Creatures.GetNeedsAt(index);
            Phenotype phenotype = Creatures.GetPhenotypeAt(index);
            return needs.Energy >= phenotype.EnergyCapacity * 0.7f
                && needs.Hydration >= phenotype.HydrationCapacity * 0.7f
                && needs.Health >= phenotype.HealthCapacity * 0.7f
                && needs.Age >= AdultAgeSeconds
                && Creatures.GetReproductionRefAt(index).CooldownRemaining <= 0f;
        }

        private void ChargeReproductionCost(int index)
        {
            ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(index);
            Phenotype phenotype = Creatures.GetPhenotypeAt(index);
            needs.Energy = Math.Max(0f, needs.Energy - (phenotype.EnergyCapacity * 0.2f));
            needs.Hydration = Math.Max(0f, needs.Hydration - (phenotype.HydrationCapacity * 0.1f));
        }

        private SimulationStatistics BuildStatistics(long tick)
        {
            float bodySizeTotal = 0f;
            float energyFractionTotal = 0f;
            float hydrationFractionTotal = 0f;
            int highestGeneration = 0;
            for (int index = 0; index < Creatures.Count; index++)
            {
                Genome genome = Creatures.GetGenomeAt(index);
                Phenotype phenotype = Creatures.GetPhenotypeAt(index);
                CreatureNeeds needs = Creatures.GetNeedsAt(index);
                bodySizeTotal += genome.BodySize;
                energyFractionTotal += needs.Energy / phenotype.EnergyCapacity;
                hydrationFractionTotal += needs.Hydration / phenotype.HydrationCapacity;
                highestGeneration = Math.Max(highestGeneration, Creatures.GetLineageAt(index).Generation);
            }

            float food = 0f;
            float water = 0f;
            for (int index = 0; index < Resources.Count; index++)
            {
                ResourceState resource = Resources.GetAt(index);
                if (resource.Kind == ResourceKind.Food) food += resource.Amount;
                else water += resource.Amount;
            }

            float reciprocalPopulation = Creatures.Count == 0 ? 0f : 1f / Creatures.Count;
            return new SimulationStatistics(
                tick,
                Creatures.Count,
                highestGeneration,
                bodySizeTotal * reciprocalPopulation,
                energyFractionTotal * reciprocalPopulation,
                hydrationFractionTotal * reciprocalPopulation,
                food,
                water);
        }

        private sealed class CreatureIndexComparer : IComparer<int>
        {
            private readonly CreatureStore _creatures;

            public CreatureIndexComparer(CreatureStore creatures)
            {
                _creatures = creatures;
            }

            public int Compare(int left, int right)
            {
                return _creatures.GetIdAt(left).Value.CompareTo(_creatures.GetIdAt(right).Value);
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
