using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    public static class TemperatureField
    {
        public static float Sample(SimVector2 position, long tick)
        {
            float spatial = (position.X * 0.18f) + (position.Y * 0.11f);
            float seasonal = tick * 0.003f;
            return 20f + (8f * (float)Math.Sin(spatial + seasonal));
        }
    }
}
