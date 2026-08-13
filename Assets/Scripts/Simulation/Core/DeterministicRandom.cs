using System;

namespace LifeSimulation.Simulation.Core
{
    public static class DeterministicRandom
    {
        private const float Inverse24BitRange = 1f / 16777216f;

        private static ulong UInt64(
            int worldSeed,
            RandomDomain domain,
            long tickOrOrdinal,
            long entityA,
            long entityB,
            int purpose)
        {
            ulong value = Mix(unchecked((ulong)(long)worldSeed));
            value = Mix(value ^ (ulong)domain);
            value = Mix(value ^ unchecked((ulong)tickOrOrdinal));
            value = Mix(value ^ unchecked((ulong)entityA));
            value = Mix(value ^ unchecked((ulong)entityB));
            return Mix(value ^ unchecked((ulong)(long)purpose));
        }

        public static float Float01(
            int worldSeed,
            RandomDomain domain,
            long tickOrOrdinal,
            long entityA,
            long entityB,
            int purpose)
        {
            return (UInt64(worldSeed, domain, tickOrOrdinal, entityA, entityB, purpose) >> 40)
                * Inverse24BitRange;
        }

        public static float Gaussian(
            int worldSeed,
            RandomDomain domain,
            long tickOrOrdinal,
            long entityA,
            long entityB,
            int purpose)
        {
            float first = Float01(worldSeed, domain, tickOrOrdinal, entityA, entityB, purpose);
            float second = Float01(worldSeed, domain, tickOrOrdinal, entityA, entityB, unchecked(purpose + 1));
            double positiveFirst = Math.Max(first, Inverse24BitRange);
            return (float)(Math.Sqrt(-2d * Math.Log(positiveFirst)) * Math.Cos(2d * Math.PI * second));
        }

        private static ulong Mix(ulong value)
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
