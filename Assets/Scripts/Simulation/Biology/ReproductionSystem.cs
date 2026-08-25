using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Spatial;

namespace LifeSimulation.Simulation.Biology
{
    public sealed class ReproductionSystem
    {
        public const float AdultAgeSeconds = 20f;
        private const float LegacyReproductionCooldownSeconds = 15f;
        private const float LegacyReproductionEnergyCostFraction = 0.2f;
        private const float MateDistance = 2f;

        private readonly float _needFraction;

        private readonly CreatureStore _creatures;
        private readonly bool _physiologyEnabled;
        private readonly bool _mateSelectionEnabled;
        private readonly CreatureIndexComparer _creatureIndexComparer;
        private SimVector2[] _creaturePositions;
        private int[] _candidates;
        private bool[] _matched;

        public ReproductionSystem(CreatureStore creatures, ArenaBounds arena, int initialCapacity, bool physiologyEnabled, bool mateSelectionEnabled = false, float needFraction = SimulationConfig.DefaultReproductionNeedFraction)
        {
            _needFraction = needFraction;
            _creatures = creatures ?? throw new ArgumentNullException(nameof(creatures));
            _physiologyEnabled = physiologyEnabled;
            _mateSelectionEnabled = mateSelectionEnabled;
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            int capacity = Math.Max(initialCapacity, 1);
            Grid = new UniformGrid(arena, cellSize: 5f, initialOccupantCapacity: capacity);
            _creaturePositions = new SimVector2[capacity];
            _candidates = new int[capacity];
            _matched = new bool[capacity];
            _creatureIndexComparer = new CreatureIndexComparer(_creatures);
        }

        public UniformGrid Grid { get; }

        public int Step(int worldSeed, float deltaTime, ref long birthOrdinal, long tick, int maximumPopulation, SimulationEventBuffer events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            int candidateCount = _creatures.Count;
            int births = 0;
            RebuildGrid(candidateCount);
            EnsureCapacity(candidateCount);
            Array.Clear(_matched, 0, candidateCount);
            for (int index = 0; index < candidateCount; index++)
            {
                ref ReproductionState reproduction = ref _creatures.GetReproductionRefAt(index);
                reproduction.CooldownRemaining = Math.Max(0f, reproduction.CooldownRemaining - deltaTime);
                _candidates[index] = index;
            }

            Array.Sort(_candidates, 0, candidateCount, _creatureIndexComparer);
            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                if (_creatures.Count >= maximumPopulation)
                {
                    break;
                }

                int firstIndex = _candidates[candidateIndex];
                if (_matched[firstIndex] || !IsReady(firstIndex))
                {
                    continue;
                }

                int secondIndex = _mateSelectionEnabled
                    ? FindSeekMateTarget(firstIndex, candidateCount)
                    : FindNearestReadyMate(firstIndex, candidateCount);
                if (secondIndex < 0)
                {
                    continue;
                }

                CreatureId child = CreateChild(firstIndex, secondIndex, worldSeed, ref birthOrdinal, tick);
                events.TryWrite(new SimulationEvent(
                    tick,
                    SimulationEventKind.Birth,
                    child,
                    _creatures.GetIdAt(firstIndex),
                    _creatures.GetIdAt(secondIndex),
                    DeathCause.None));
                births++;
                _matched[firstIndex] = true;
                _matched[secondIndex] = true;
            }

            return births;
        }

        private void RebuildGrid(int count)
        {
            EnsureCapacity(count);
            for (int index = 0; index < count; index++)
            {
                _creaturePositions[index] = _creatures.GetMovementAt(index).Position;
            }

            Grid.Rebuild(_creaturePositions, count);
        }

        private int FindNearestReadyMate(int firstIndex, int candidateCount)
        {
            SimVector2 firstPosition = _creatures.GetMovementAt(firstIndex).Position;
            int minimumColumn = Grid.GetColumn(firstPosition.X - MateDistance);
            int maximumColumn = Grid.GetColumn(firstPosition.X + MateDistance);
            int minimumRow = Grid.GetRow(firstPosition.Y - MateDistance);
            int maximumRow = Grid.GetRow(firstPosition.Y + MateDistance);
            int bestIndex = -1;
            float bestDistance = MateDistance;

            for (int row = minimumRow; row <= maximumRow; row++)
            {
                for (int column = minimumColumn; column <= maximumColumn; column++)
                {
                    int cell = Grid.GetCellIndex(column, row);
                    for (int occupant = Grid.GetCellStart(cell); occupant < Grid.GetCellEnd(cell); occupant++)
                    {
                        int candidate = Grid.GetOccupantIndexAt(occupant);
                        if (candidate == firstIndex || candidate >= candidateCount || _matched[candidate] || !IsReady(candidate))
                        {
                            continue;
                        }

                        float distance = SimVector2.Distance(firstPosition, _creatures.GetMovementAt(candidate).Position);
                        if (distance > MateDistance || (bestIndex >= 0 && distance > bestDistance))
                        {
                            continue;
                        }

                        if (bestIndex < 0 || distance < bestDistance || _creatures.GetIdAt(candidate).Value < _creatures.GetIdAt(bestIndex).Value)
                        {
                            bestIndex = candidate;
                            bestDistance = distance;
                        }
                    }
                }
            }

            return bestIndex;
        }

        private int FindSeekMateTarget(int firstIndex, int candidateCount)
        {
            CreatureDecision decision = _creatures.GetDecisionAt(firstIndex);
            if (decision.Action != CreatureAction.SeekMate
                || !_creatures.TryGetIndex(decision.TargetCreatureId, out int secondIndex))
            {
                return -1;
            }

            if (secondIndex < 0 || secondIndex >= candidateCount || secondIndex == firstIndex
                || _matched[secondIndex] || !IsReady(secondIndex))
            {
                return -1;
            }

            float distance = SimVector2.Distance(
                _creatures.GetMovementAt(firstIndex).Position,
                _creatures.GetMovementAt(secondIndex).Position);
            return distance <= MateDistance ? secondIndex : -1;
        }

        /// <summary>Test-only forwarder — Assets/Tests/EditMode has no InternalsVisibleTo, so private logic is exercised through a public passthrough, matching this session's established pattern.</summary>
        public int FindSeekMateTargetForTest(int firstIndex, int candidateCount)
        {
            return FindSeekMateTarget(firstIndex, candidateCount);
        }

        private CreatureId CreateChild(int firstIndex, int secondIndex, int worldSeed, ref long birthOrdinal, long tick)
        {
            CreatureId firstParent = _creatures.GetIdAt(firstIndex);
            CreatureId secondParent = _creatures.GetIdAt(secondIndex);
            Genome childGenome = GenomeInheritance.CreateChild(
                _creatures.GetGenomeAt(firstIndex),
                _creatures.GetGenomeAt(secondIndex),
                worldSeed,
                birthOrdinal++,
                mutationStandardDeviation: 0.03f);
            SimVector2 firstPosition = _creatures.GetMovementAt(firstIndex).Position;
            SimVector2 secondPosition = _creatures.GetMovementAt(secondIndex).Position;
            CreatureId child = _creatures.AddChild(
                childGenome,
                new SimVector2((firstPosition.X + secondPosition.X) * 0.5f, (firstPosition.Y + secondPosition.Y) * 0.5f),
                firstParent,
                secondParent);
            ChargeCost(firstIndex);
            ChargeCost(secondIndex);
            _creatures.GetReproductionRefAt(firstIndex).CooldownRemaining = CooldownFor(firstIndex);
            _creatures.GetReproductionRefAt(secondIndex).CooldownRemaining = CooldownFor(secondIndex);
            _creatures.SetDecisionAt(firstIndex, new CreatureDecision(CreatureAction.Reproduce, -1, 1f, tick));
            _creatures.SetDecisionAt(secondIndex, new CreatureDecision(CreatureAction.Reproduce, -1, 1f, tick));
            return child;
        }

        private bool IsReady(int index)
        {
            CreatureNeeds needs = _creatures.GetNeedsAt(index);
            Phenotype phenotype = _creatures.GetPhenotypeAt(index);
            return CanReproduce(needs, phenotype, _creatures.GetReproductionRefAt(index), _needFraction);
        }

        /// <summary>
        /// <paramref name="needFraction"/> is the gate on all three needs. The default reproduces the
        /// original literal exactly, so nothing recorded moves.
        ///
        /// <para>It is a parameter because it turned out to be the most likely explanation for the
        /// strongest selection signal in the model, and a hypothesis you cannot vary is not one you
        /// can test. See <c>docs/experiments/p6-nothing-starves-2026-08-24.md</c> - the population
        /// sits at 0.806 mean energy against the 0.80 seek-a-mate threshold, which is a homeostat
        /// held against the gate rather than an ecological equilibrium.</para>
        /// </summary>
        public static bool CanReproduce(CreatureNeeds needs, Phenotype phenotype, ReproductionState reproduction, float needFraction = SimulationConfig.DefaultReproductionNeedFraction)
        {
            return needs.Energy >= phenotype.EnergyCapacity * needFraction
                && needs.Hydration >= phenotype.HydrationCapacity * needFraction
                && needs.Health >= phenotype.HealthCapacity * needFraction
                && needs.Age >= AdultAgeSeconds
                && reproduction.CooldownRemaining <= 0f;
        }

        /// <summary>
        /// Seeking a mate is gated a tenth higher than breeding, which is the original relationship
        /// between the two literals and is preserved as an offset rather than a second knob.
        /// </summary>
        public static bool CanSeekMate(CreatureNeeds needs, Phenotype phenotype, ReproductionState reproduction, float needFraction = SimulationConfig.DefaultReproductionNeedFraction)
        {
            float seekFraction = needFraction + SimulationConfig.MateSeekingNeedMargin;
            return needs.Energy >= phenotype.EnergyCapacity * seekFraction
                && needs.Hydration >= phenotype.HydrationCapacity * seekFraction
                && needs.Health >= phenotype.HealthCapacity * seekFraction
                && needs.Age >= AdultAgeSeconds
                && reproduction.CooldownRemaining <= 0f;
        }

        private void ChargeCost(int index)
        {
            ref CreatureNeeds needs = ref _creatures.GetNeedsRefAt(index);
            Phenotype phenotype = _creatures.GetPhenotypeAt(index);
            float energyCost = _physiologyEnabled ? phenotype.ReproductionEnergyCostFraction : LegacyReproductionEnergyCostFraction;
            needs.Energy = Math.Max(0f, needs.Energy - (phenotype.EnergyCapacity * energyCost));
            needs.Hydration = Math.Max(0f, needs.Hydration - (phenotype.HydrationCapacity * 0.1f));
        }

        private float CooldownFor(int index)
        {
            return _physiologyEnabled ? _creatures.GetPhenotypeAt(index).ReproductionCooldownSeconds : LegacyReproductionCooldownSeconds;
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _candidates.Length)
            {
                return;
            }

            int capacity = Math.Max(required, _candidates.Length * 2);
            Array.Resize(ref _creaturePositions, capacity);
            Array.Resize(ref _candidates, capacity);
            Array.Resize(ref _matched, capacity);
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
    }
}
