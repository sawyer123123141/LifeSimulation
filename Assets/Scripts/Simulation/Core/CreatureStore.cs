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
        private readonly Dictionary<CreatureId, int> _indexById;
        private long _nextId;

        public CreatureStore(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            _identities = new CreatureId[Math.Max(initialCapacity, 1)];
            _genomes = new Genome[_identities.Length];
            _phenotypes = new Phenotype[_identities.Length];
            _needs = new CreatureNeeds[_identities.Length];
            _movement = new MovementState[_identities.Length];
            _indexById = new Dictionary<CreatureId, int>(initialCapacity);
            _nextId = 1;
        }

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
            EnsureCapacity(Count + 1);

            var id = new CreatureId(_nextId++);
            _identities[Count] = id;
            _genomes[Count] = genome;
            _phenotypes[Count] = Phenotype.FromGenome(genome);
            _needs[Count] = CreatureNeeds.Full(_phenotypes[Count]);
            _movement[Count] = new MovementState(position);
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
