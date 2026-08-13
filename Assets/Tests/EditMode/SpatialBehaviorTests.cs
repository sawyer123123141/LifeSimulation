using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
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
    }
}
