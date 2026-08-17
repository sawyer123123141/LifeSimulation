using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    public static class TemperatureField
    {
        public static float Sample(SimVector2 position, long tick)
        {
            // Purely spatial - fixed climate zones by location, not drifting weather over time.
            // `tick` is kept in the signature so every call site (movement decisions, presentation,
            // ThermoregulationSystem) stays unchanged; a future world-generation pass may reintroduce
            // real seasonal/weather variation on top of this static field.
            float spatial = (position.X * 0.18f) + (position.Y * 0.11f);
            return 20f + (8f * (float)Math.Sin(spatial));
        }
    }
}
