using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public sealed class PlantPatchStore
    {
        private PlantPatchId[] _ids;
        private ResourceId[] _foodResourceIds;
        private SimVector2[] _positions;
        private float[] _biomass;
        private float[] _capacities;
        private float[] _growthRates;
        private float[] _nutrition;
        private float[] _defense;
        private PlantGenome[] _genomes;
        private PlantLineage[] _lineages;
        private float[] _ages;
        private float[] _seedReserves;
        private float[] _reproductionCooldowns;
        private int _nextId = 1;

        public PlantPatchStore(int initialCapacity)
        {
            int capacity = Math.Max(1, initialCapacity);
            _ids = new PlantPatchId[capacity];
            _foodResourceIds = new ResourceId[capacity];
            _positions = new SimVector2[capacity];
            _biomass = new float[capacity];
            _capacities = new float[capacity];
            _growthRates = new float[capacity];
            _nutrition = new float[capacity];
            _defense = new float[capacity];
            _genomes = new PlantGenome[capacity];
            _lineages = new PlantLineage[capacity];
            _ages = new float[capacity];
            _seedReserves = new float[capacity];
            _reproductionCooldowns = new float[capacity];
        }

        public int Count { get; private set; }

        public int Add(ResourceId foodResourceId, SimVector2 position, float biomass, float capacity, float growthRate, float nutrition, float defense)
        {
            if (capacity < 0f || biomass < 0f || biomass > capacity || growthRate < 0f || nutrition < 0f || defense < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(biomass));
            }

            EnsureCapacity(Count + 1);
            int index = Count++;
            _ids[index] = new PlantPatchId(_nextId++);
            _foodResourceIds[index] = foodResourceId;
            _positions[index] = position;
            _biomass[index] = biomass;
            _capacities[index] = capacity;
            _growthRates[index] = growthRate;
            _nutrition[index] = nutrition;
            _defense[index] = defense;
            _genomes[index] = PlantGenome.Neutral;
            _lineages[index] = new PlantLineage(_ids[index], default, generation: 0);
            return index;
        }

        public PlantPatchState GetAt(int index)
        {
            return new PlantPatchState(_ids[index], _foodResourceIds[index], _positions[index], _biomass[index], _capacities[index], _growthRates[index], _nutrition[index], _defense[index], _genomes[index], _lineages[index], _ages[index], _seedReserves[index], _reproductionCooldowns[index]);
        }

        public void SetReproductionCooldown(int index, float value)
        {
            if ((uint)index >= (uint)Count) return;
            _reproductionCooldowns[index] = Math.Max(0f, value);
        }

        public void SetGenomeAndLineage(int index, PlantGenome genome, PlantLineage lineage)
        {
            if ((uint)index >= (uint)Count) return;
            _genomes[index] = genome;
            _lineages[index] = lineage;
        }

        public int FindIndex(ResourceId foodResourceId)
        {
            for (int index = 0; index < Count; index++)
            {
                if (_foodResourceIds[index].Equals(foodResourceId)) return index;
            }

            return -1;
        }

        public float ConsumeAt(int index, float amount)
        {
            if ((uint)index >= (uint)Count || amount <= 0f) return 0f;
            float consumed = Math.Min(_biomass[index], amount);
            _biomass[index] -= consumed;
            return consumed;
        }

        public void SetBiomass(int index, float biomass)
        {
            if ((uint)index >= (uint)Count) return;
            _biomass[index] = Math.Max(0f, Math.Min(_capacities[index], biomass));
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _ids.Length) return;
            int capacity = Math.Max(required, _ids.Length * 2);
            Array.Resize(ref _ids, capacity);
            Array.Resize(ref _foodResourceIds, capacity);
            Array.Resize(ref _positions, capacity);
            Array.Resize(ref _biomass, capacity);
            Array.Resize(ref _capacities, capacity);
            Array.Resize(ref _growthRates, capacity);
            Array.Resize(ref _nutrition, capacity);
            Array.Resize(ref _defense, capacity);
            Array.Resize(ref _genomes, capacity);
            Array.Resize(ref _lineages, capacity);
            Array.Resize(ref _ages, capacity);
            Array.Resize(ref _seedReserves, capacity);
            Array.Resize(ref _reproductionCooldowns, capacity);
        }
    }
}
