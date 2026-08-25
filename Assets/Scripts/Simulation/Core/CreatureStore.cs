using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;

namespace LifeSimulation.Simulation.Core
{
    public sealed class CreatureStore
    {
        private CreatureId[] _identities;
        private Genome[] _genomes;
        private Phenotype[] _phenotypes;
        private CreatureNeeds[] _needs;
        private MovementState[] _movement;
        private CreatureDecision[] _decisions;
        private DecisionDiagnostics[] _decisionDiagnostics;
        private CreatureLineage[] _lineages;
        private ReproductionState[] _reproduction;
        private CombatState[] _combat;
        private MemoryState[] _memory;
        private ForagingState[] _foraging;
        private HomeRangeState[] _homeRanges;
        private PlaceMemory[] _placeMemories;
        private readonly bool _metabolicIngestionEnabled;
        private readonly int _maximumMemorySlots;
        private readonly Dictionary<CreatureId, int> _indexById;
        private long _nextId;

        /// <summary>Read-only peek at the next id to be assigned. Exists only for state fingerprinting; never set.</summary>
        public long NextIdPeek => _nextId;

        public CreatureStore(int initialCapacity, int maximumMemorySlots = 0, bool metabolicIngestionEnabled = false)
        {
            _metabolicIngestionEnabled = metabolicIngestionEnabled;
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            if (maximumMemorySlots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumMemorySlots));
            }

            _identities = new CreatureId[Math.Max(initialCapacity, 1)];
            _genomes = new Genome[_identities.Length];
            _phenotypes = new Phenotype[_identities.Length];
            _needs = new CreatureNeeds[_identities.Length];
            _movement = new MovementState[_identities.Length];
            _decisions = new CreatureDecision[_identities.Length];
            _decisionDiagnostics = new DecisionDiagnostics[_identities.Length];
            _lineages = new CreatureLineage[_identities.Length];
            _reproduction = new ReproductionState[_identities.Length];
            _combat = new CombatState[_identities.Length];
            _memory = new MemoryState[_identities.Length];
            _foraging = new ForagingState[_identities.Length];
            _homeRanges = new HomeRangeState[_identities.Length];
            _maximumMemorySlots = maximumMemorySlots;
            _placeMemories = new PlaceMemory[_identities.Length * _maximumMemorySlots];
            _indexById = new Dictionary<CreatureId, int>(initialCapacity);
            _nextId = 1;
        }

        /// <summary>Fixed row width of the place-memory sidecar; every creature's slots span this many entries.</summary>
        public int MaximumMemorySlots => _maximumMemorySlots;

        public int Count { get; private set; }

        public CreatureId Add()
        {
            return Add(Genome.Neutral);
        }

        public CreatureId Add(Genome genome)
        {
            return Add(genome, new SimVector2(0f, 0f));
        }

        public CreatureId Add(Genome genome, SimVector2 position)
        {
            return AddInternal(genome, position, default, default, generation: 0);
        }

        public CreatureId AddChild(Genome genome, SimVector2 position, CreatureId firstParent, CreatureId secondParent)
        {
            if (!_indexById.TryGetValue(firstParent, out int firstParentIndex)
                || !_indexById.TryGetValue(secondParent, out int secondParentIndex))
            {
                throw new ArgumentOutOfRangeException("Both parents must be alive.");
            }

            int generation = Math.Max(_lineages[firstParentIndex].Generation, _lineages[secondParentIndex].Generation) + 1;
            return AddInternal(genome, position, firstParent, secondParent, generation);
        }

        public CreatureLineage GetLineageAt(int index)
        {
            ValidateIndex(index);
            return _lineages[index];
        }

        public ref ReproductionState GetReproductionRefAt(int index)
        {
            ValidateIndex(index);
            return ref _reproduction[index];
        }

        public ref CombatState GetCombatRefAt(int index)
        {
            ValidateIndex(index);
            return ref _combat[index];
        }

        public ref MemoryState GetMemoryRefAt(int index)
        {
            ValidateIndex(index);
            return ref _memory[index];
        }

        public ref ForagingState GetForagingRefAt(int index)
        {
            ValidateIndex(index);
            return ref _foraging[index];
        }

        public ref HomeRangeState GetHomeRangeRefAt(int index)
        {
            ValidateIndex(index);
            return ref _homeRanges[index];
        }

        public ref PlaceMemory GetPlaceMemoryRefAt(int index, int slot)
        {
            ValidateIndex(index);
            if ((uint)slot >= (uint)_maximumMemorySlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            return ref _placeMemories[(index * _maximumMemorySlots) + slot];
        }

        private CreatureId AddInternal(
            Genome genome,
            SimVector2 position,
            CreatureId firstParent,
            CreatureId secondParent,
            int generation)
        {
            EnsureCapacity(Count + 1);

            var id = new CreatureId(_nextId++);
            _identities[Count] = id;
            _genomes[Count] = genome;
            _phenotypes[Count] = Phenotype.FromGenome(genome, _metabolicIngestionEnabled);
            _needs[Count] = CreatureNeeds.Full(_phenotypes[Count]);
            _movement[Count] = new MovementState(position);
            _decisions[Count] = new CreatureDecision(CreatureAction.Wander, -1, 0f);
            _decisionDiagnostics[Count] = default;
            _lineages[Count] = new CreatureLineage(id, firstParent, secondParent, generation);
            _reproduction[Count] = default;
            _combat[Count] = default;
            _memory[Count] = default;
            _foraging[Count] = default;
            _homeRanges[Count] = default;
            if (_maximumMemorySlots > 0)
            {
                Array.Clear(_placeMemories, Count * _maximumMemorySlots, _maximumMemorySlots);
            }

            _indexById.Add(id, Count);
            Count++;
            return id;
        }

        public bool TryGetIndex(CreatureId id, out int index)
        {
            return _indexById.TryGetValue(id, out index);
        }

        public CreatureId GetIdAt(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _identities[index];
        }

        public Genome GetGenomeAt(int index)
        {
            ValidateIndex(index);
            return _genomes[index];
        }

        /// <summary>
        /// Replace a creature's genome and rederive its phenotype. Diagnostics only: this is how
        /// the gene liveness harness injects a perturbation. Needs are left alone so the
        /// perturbation shows up through behavior rather than by resetting state directly.
        /// </summary>
        public void OverwriteGenomeAt(int index, Genome genome)
        {
            ValidateIndex(index);
            _genomes[index] = genome;
            _phenotypes[index] = Phenotype.FromGenome(genome, _metabolicIngestionEnabled);
        }

        public Phenotype GetPhenotypeAt(int index)
        {
            ValidateIndex(index);
            return _phenotypes[index];
        }

        public CreatureNeeds GetNeedsAt(int index)
        {
            ValidateIndex(index);
            return _needs[index];
        }

        public ref CreatureNeeds GetNeedsRefAt(int index)
        {
            ValidateIndex(index);
            return ref _needs[index];
        }

        public MovementState GetMovementAt(int index)
        {
            ValidateIndex(index);
            return _movement[index];
        }

        public ref MovementState GetMovementRefAt(int index)
        {
            ValidateIndex(index);
            return ref _movement[index];
        }

        public CreatureDecision GetDecisionAt(int index)
        {
            ValidateIndex(index);
            return _decisions[index];
        }

        public void SetDecisionAt(int index, CreatureDecision decision)
        {
            ValidateIndex(index);
            _decisions[index] = decision;
        }

        public DecisionDiagnostics GetDecisionDiagnosticsAt(int index)
        {
            ValidateIndex(index);
            return _decisionDiagnostics[index];
        }

        public void SetDecisionDiagnosticsAt(int index, DecisionDiagnostics diagnostics)
        {
            ValidateIndex(index);
            _decisionDiagnostics[index] = diagnostics;
        }

        public bool Remove(CreatureId id)
        {
            if (!_indexById.TryGetValue(id, out int removedIndex))
            {
                return false;
            }

            int lastIndex = Count - 1;
            CreatureId movedId = _identities[lastIndex];

            _indexById.Remove(id);
            if (removedIndex != lastIndex)
            {
                _identities[removedIndex] = movedId;
                _genomes[removedIndex] = _genomes[lastIndex];
                _phenotypes[removedIndex] = _phenotypes[lastIndex];
                _needs[removedIndex] = _needs[lastIndex];
                _movement[removedIndex] = _movement[lastIndex];
                _decisions[removedIndex] = _decisions[lastIndex];
                _decisionDiagnostics[removedIndex] = _decisionDiagnostics[lastIndex];
                _lineages[removedIndex] = _lineages[lastIndex];
                _reproduction[removedIndex] = _reproduction[lastIndex];
                _combat[removedIndex] = _combat[lastIndex];
                _memory[removedIndex] = _memory[lastIndex];
                _foraging[removedIndex] = _foraging[lastIndex];
                _homeRanges[removedIndex] = _homeRanges[lastIndex];
                if (_maximumMemorySlots > 0)
                {
                    Array.Copy(_placeMemories, lastIndex * _maximumMemorySlots, _placeMemories, removedIndex * _maximumMemorySlots, _maximumMemorySlots);
                }

                _indexById[movedId] = removedIndex;
            }

            Count--;
            return true;
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _identities.Length)
            {
                return;
            }

            int nextCapacity = Math.Max(required, _identities.Length * 2);
            Array.Resize(ref _identities, nextCapacity);
            Array.Resize(ref _genomes, nextCapacity);
            Array.Resize(ref _phenotypes, nextCapacity);
            Array.Resize(ref _needs, nextCapacity);
            Array.Resize(ref _movement, nextCapacity);
            Array.Resize(ref _decisions, nextCapacity);
            Array.Resize(ref _decisionDiagnostics, nextCapacity);
            Array.Resize(ref _lineages, nextCapacity);
            Array.Resize(ref _reproduction, nextCapacity);
            Array.Resize(ref _combat, nextCapacity);
            Array.Resize(ref _memory, nextCapacity);
            Array.Resize(ref _foraging, nextCapacity);
            Array.Resize(ref _homeRanges, nextCapacity);
            if (_maximumMemorySlots > 0)
            {
                Array.Resize(ref _placeMemories, nextCapacity * _maximumMemorySlots);
            }
        }

        private void ValidateIndex(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
