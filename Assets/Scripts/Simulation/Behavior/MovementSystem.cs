using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Behavior
{
    public readonly struct ArenaBounds
    {
        public ArenaBounds(float minimumX, float maximumX, float minimumY, float maximumY)
        {
            if (minimumX > maximumX || minimumY > maximumY)
            {
                throw new ArgumentException("Arena minimum bounds cannot exceed maximum bounds.");
            }

            MinimumX = minimumX;
            MaximumX = maximumX;
            MinimumY = minimumY;
            MaximumY = maximumY;
        }

        public float MinimumX { get; }
        public float MaximumX { get; }
        public float MinimumY { get; }
        public float MaximumY { get; }

        public SimVector2 Clamp(SimVector2 position)
        {
            return new SimVector2(
                Math.Max(MinimumX, Math.Min(MaximumX, position.X)),
                Math.Max(MinimumY, Math.Min(MaximumY, position.Y)));
        }
    }

    public struct MovementState
    {
        public MovementState(SimVector2 position)
        {
            PreviousPosition = position;
            Position = position;
        }

        public SimVector2 PreviousPosition;
        public SimVector2 Position;
    }

    public static class MovementSystem
    {
        public static float MoveToward(
            ref MovementState state,
            SimVector2 target,
            float maximumSpeed,
            float deltaTime,
            ArenaBounds arena)
        {
            if (maximumSpeed < 0f || float.IsNaN(maximumSpeed) || float.IsInfinity(maximumSpeed))
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSpeed));
            }

            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            float offsetX = target.X - state.Position.X;
            float offsetY = target.Y - state.Position.Y;
            float targetDistance = (float)Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY));
            float distance = Math.Min(targetDistance, maximumSpeed * deltaTime);
            state.PreviousPosition = state.Position;

            if (targetDistance > 0f && distance > 0f)
            {
                float scale = distance / targetDistance;
                state.Position = arena.Clamp(new SimVector2(
                    state.Position.X + (offsetX * scale),
                    state.Position.Y + (offsetY * scale)));
            }

            return SimVector2.Distance(state.PreviousPosition, state.Position);
        }
    }
}
