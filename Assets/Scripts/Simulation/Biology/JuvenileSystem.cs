using System;

namespace LifeSimulation.Simulation.Biology
{
    public static class JuvenileSystem
    {
        public const float CapabilityFloor = 0.3f;

        public static float CapabilityMultiplier(float age, float adultAgeSeconds)
        {
            if (adultAgeSeconds <= 0f)
            {
                return 1f;
            }

            float t = Math.Max(0f, Math.Min(1f, age / adultAgeSeconds));
            return CapabilityFloor + ((1f - CapabilityFloor) * t);
        }
    }
}
