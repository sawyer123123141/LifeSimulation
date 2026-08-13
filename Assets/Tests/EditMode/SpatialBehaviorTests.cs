using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
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
    }
}
