using System;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Spatial
{
    public sealed class UniformGrid
    {
        private readonly ArenaBounds _arena;
        private readonly float _cellSize;
        private readonly int _columns;
        private readonly int _rows;
        private readonly int[] _cellCounts;
        private readonly int[] _cellOffsets;
        private readonly int[] _cellCursors;
        private int[] _occupantIndexes;

        public UniformGrid(ArenaBounds arena, float cellSize, int initialOccupantCapacity)
        {
            if (cellSize <= 0f || float.IsNaN(cellSize) || float.IsInfinity(cellSize))
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            }

            if (initialOccupantCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialOccupantCapacity));
            }

            _arena = arena;
            _cellSize = cellSize;
            _columns = Math.Max(1, (int)Math.Ceiling((arena.MaximumX - arena.MinimumX) / cellSize));
            _rows = Math.Max(1, (int)Math.Ceiling((arena.MaximumY - arena.MinimumY) / cellSize));
            int cellCount = _columns * _rows;
            _cellCounts = new int[cellCount];
            _cellOffsets = new int[cellCount + 1];
            _cellCursors = new int[cellCount];
            _occupantIndexes = new int[Math.Max(initialOccupantCapacity, 1)];
        }

        public int CellCount => _cellCounts.Length;
        public int Columns => _columns;
        public int Rows => _rows;

        public void Rebuild(SimVector2[] positions, int count)
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            if (count < 0 || count > positions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            EnsureOccupantCapacity(count);
            Array.Clear(_cellCounts, 0, _cellCounts.Length);
            for (int index = 0; index < count; index++)
            {
                _cellCounts[GetCellIndex(positions[index])]++;
            }

            _cellOffsets[0] = 0;
            for (int cellIndex = 0; cellIndex < CellCount; cellIndex++)
            {
                _cellOffsets[cellIndex + 1] = _cellOffsets[cellIndex] + _cellCounts[cellIndex];
                _cellCursors[cellIndex] = _cellOffsets[cellIndex];
            }

            for (int index = 0; index < count; index++)
            {
                int cellIndex = GetCellIndex(positions[index]);
                _occupantIndexes[_cellCursors[cellIndex]++] = index;
            }
        }

        public int GetCellIndex(SimVector2 position)
        {
            int column = ClampCellCoordinate((int)Math.Floor((position.X - _arena.MinimumX) / _cellSize), _columns);
            int row = ClampCellCoordinate((int)Math.Floor((position.Y - _arena.MinimumY) / _cellSize), _rows);
            return (row * _columns) + column;
        }

        public int GetCellStart(int cellIndex)
        {
            ValidateCellIndex(cellIndex);
            return _cellOffsets[cellIndex];
        }

        public int GetCellEnd(int cellIndex)
        {
            ValidateCellIndex(cellIndex);
            return _cellOffsets[cellIndex + 1];
        }

        public int GetOccupantIndexAt(int occupantIndex)
        {
            if ((uint)occupantIndex >= (uint)_cellOffsets[CellCount])
            {
                throw new ArgumentOutOfRangeException(nameof(occupantIndex));
            }

            return _occupantIndexes[occupantIndex];
        }

        private static int ClampCellCoordinate(int coordinate, int dimensions)
        {
            return Math.Max(0, Math.Min(dimensions - 1, coordinate));
        }

        private void EnsureOccupantCapacity(int required)
        {
            if (required > _occupantIndexes.Length)
            {
                Array.Resize(ref _occupantIndexes, Math.Max(required, _occupantIndexes.Length * 2));
            }
        }

        private void ValidateCellIndex(int cellIndex)
        {
            if ((uint)cellIndex >= (uint)CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(cellIndex));
            }
        }
    }
}
