using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    /// <summary>
    /// Deterministic 3D value noise, built on <see cref="DeterministicRandom"/> so it inherits the
    /// project's platform-exact hashing rather than introducing a second source of randomness.
    ///
    /// <para><b>Why value noise on the existing hash</b>, rather than Perlin/simplex with a gradient
    /// table: a gradient table would have to be specified exactly to stay reproducible, and any
    /// engine-provided noise (Unity's <c>Mathf.PerlinNoise</c>) is platform-variable and therefore
    /// unusable here. Hashing integer lattice corners through <c>DeterministicRandom.Float01</c>
    /// needs no table and cannot drift.</para>
    ///
    /// <para><b>Why 3D</b>: fields are evaluated at a point on a sphere, not on a plane. Sampling 3D
    /// noise on a sphere surface is seamless by construction — no wrapping, no pole artifacts, no
    /// UV distortion — and the same function works for the flat prototype and for the eventual
    /// planet at P7. A tileable 2D scheme would have to be discarded there.</para>
    /// </summary>
    public static class EnvironmentNoise
    {
        /// <summary>Quintic fade, the standard smootherstep. Zero first and second derivatives at the ends, so octaves do not show lattice creases.</summary>
        private static double Fade(double t)
        {
            return t * t * t * ((t * ((t * 6d) - 15d)) + 10d);
        }

        private static int Floor(double value)
        {
            int truncated = (int)value;
            return value < truncated ? truncated - 1 : truncated;
        }

        private static float Lattice(int worldSeed, int channel, int x, int y, int z)
        {
            return DeterministicRandom.Float01(worldSeed, RandomDomain.TerrainField, x, y, z, channel);
        }

        /// <summary>Single-octave 3D value noise in 0..1.</summary>
        public static double ValueNoise(int worldSeed, int channel, double x, double y, double z)
        {
            int xi = Floor(x), yi = Floor(y), zi = Floor(z);
            double u = Fade(x - xi), v = Fade(y - yi), w = Fade(z - zi);

            double c000 = Lattice(worldSeed, channel, xi, yi, zi);
            double c100 = Lattice(worldSeed, channel, xi + 1, yi, zi);
            double c010 = Lattice(worldSeed, channel, xi, yi + 1, zi);
            double c110 = Lattice(worldSeed, channel, xi + 1, yi + 1, zi);
            double c001 = Lattice(worldSeed, channel, xi, yi, zi + 1);
            double c101 = Lattice(worldSeed, channel, xi + 1, yi, zi + 1);
            double c011 = Lattice(worldSeed, channel, xi, yi + 1, zi + 1);
            double c111 = Lattice(worldSeed, channel, xi + 1, yi + 1, zi + 1);

            double x00 = c000 + ((c100 - c000) * u);
            double x10 = c010 + ((c110 - c010) * u);
            double x01 = c001 + ((c101 - c001) * u);
            double x11 = c011 + ((c111 - c011) * u);
            double y0 = x00 + ((x10 - x00) * v);
            double y1 = x01 + ((x11 - x01) * v);
            return y0 + ((y1 - y0) * w);
        }

        /// <summary>
        /// Fractal Brownian motion, normalized to 0..1. Amplitudes are divided by their own sum, so
        /// the output range does not depend on octave count or gain.
        /// </summary>
        public static double Fbm(
            int worldSeed,
            int channel,
            double x,
            double y,
            double z,
            int octaves,
            double lacunarity,
            double gain)
        {
            double sum = 0d;
            double amplitude = 1d;
            double totalAmplitude = 0d;
            double frequency = 1d;

            for (int octave = 0; octave < octaves; octave++)
            {
                sum += amplitude * ValueNoise(worldSeed, channel + octave, x * frequency, y * frequency, z * frequency);
                totalAmplitude += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return totalAmplitude <= 0d ? 0d : sum / totalAmplitude;
        }

        /// <summary>
        /// Domain-warped fBm. Warping displaces the sample point by another noise field, which turns
        /// smooth blobs into lobed, interlocking regions with recognisable boundaries — the
        /// difference between "a gradient" and "somewhere with a coastline".
        /// </summary>
        public static double WarpedFbm(
            int worldSeed,
            int channel,
            double x,
            double y,
            double z,
            int octaves,
            double lacunarity,
            double gain,
            double warpStrength)
        {
            if (warpStrength <= 0d)
            {
                return Fbm(worldSeed, channel, x, y, z, octaves, lacunarity, gain);
            }

            double wx = (ValueNoise(worldSeed, channel + 64, x, y, z) - 0.5d) * 2d * warpStrength;
            double wy = (ValueNoise(worldSeed, channel + 65, x, y, z) - 0.5d) * 2d * warpStrength;
            double wz = (ValueNoise(worldSeed, channel + 66, x, y, z) - 0.5d) * 2d * warpStrength;
            return Fbm(worldSeed, channel, x + wx, y + wy, z + wz, octaves, lacunarity, gain);
        }

        /// <summary>
        /// Expand contrast about the midpoint. Necessary because fBm sums independent octaves and
        /// therefore concentrates near 0.5 — a raw 4-octave field spans roughly .37...82 rather than
        /// the full range, and a field that barely varies gives adaptation genes almost nothing to
        /// select on. That weakness is the same one that made plant defense unmeasurable, so it is
        /// worth correcting at the source rather than compensating downstream.
        /// </summary>
        public static double Contrast(double value, double strength)
        {
            return Clamp01(0.5d + ((value - 0.5d) * strength));
        }

        public static double Clamp01(double value)
        {
            return value < 0d ? 0d : value > 1d ? 1d : value;
        }
    }
}
