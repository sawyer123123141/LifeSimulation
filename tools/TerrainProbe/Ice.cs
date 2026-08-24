using System;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.TerrainProbe
{
    /// <summary>
    /// Where the ice is.
    ///
    /// <para>The standing note is that ice cover "looks high" at 0.074 of the surface. That number
    /// alone cannot say whether anything is wrong: Earth carries roughly 3% of its surface as
    /// permanent ice and about 10% of its land, so 7.4% is on the high side of plausible rather than
    /// obviously broken. <b>The question that decides it is not how much ice there is but where it
    /// is.</b></para>
    ///
    /// <para>Polar caps are ice doing what ice does. Ice sitting on every mountain in the tropics is
    /// altitude cooling turned up too far, and it reads wrong on sight even when the total looks
    /// reasonable. This separates the two, so the fix - if there is one - is aimed at the right
    /// coefficient instead of at the classification threshold.</para>
    /// </summary>
    internal static class Ice
    {
        /// <summary>Beyond this latitude, ice is a polar cap rather than a mountain.</summary>
        private const double PolarLatitude = 1.0472d;

        public static void Report(int seed, TerrainSettings settings, double maximumFrequency)
        {
            PlateStructure plates = PlateStructure.Create(seed, settings);

            const int Samples = 200000;
            int land = 0;
            int ice = 0;
            int polarIce = 0;
            double iceElevation = 0d;
            double landElevation = 0d;
            double highestIceLand = 0d;

            for (int index = 0; index < Samples; index++)
            {
                Direction(index, Samples, out double x, out double y, out double z);
                PlanetSample sample = PlanetTerrain.Sample(seed, plates, x, y, z, maximumFrequency, settings);
                if (sample.Elevation <= 0f) continue;

                land++;
                landElevation += sample.Elevation;

                if (PlanetBiome.Classify(sample) != BiomeKind.Ice) continue;

                ice++;
                iceElevation += sample.Elevation;

                double latitude = Math.Abs(Math.Asin(Math.Clamp(y, -1d, 1d)));
                if (latitude >= PolarLatitude) polarIce++;
                else if (sample.Elevation > highestIceLand) highestIceLand = sample.Elevation;
            }

            Console.WriteLine();
            Console.WriteLine("ice, seed " + seed);
            Console.WriteLine("  surface       " + Share(ice, Samples) + " of the sphere");
            Console.WriteLine("  land          " + Share(ice, land) + " of land");
            Console.WriteLine("  polar         " + Share(polarIce, ice) + " of ice is beyond 60 degrees");
            Console.WriteLine("  non-polar     " + Share(ice - polarIce, ice) + " of ice is not");
            Console.WriteLine("  mean elevation of ice   " + Mean(iceElevation, ice).ToString("0.000"));
            Console.WriteLine("  mean elevation of land  " + Mean(landElevation, land).ToString("0.000"));
            Console.WriteLine("  highest non-polar ice   " + highestIceLand.ToString("0.000") + " elevation");
        }

        private static string Share(int part, int whole)
        {
            return whole == 0 ? "n/a" : (100d * part / whole).ToString("0.00") + "%";
        }

        private static double Mean(double total, int count)
        {
            return count == 0 ? double.NaN : total / count;
        }

        /// <summary>An even spread of directions over the sphere, so shares are area shares.</summary>
        private static void Direction(int index, int count, out double x, out double y, out double z)
        {
            double offset = 2d / count;
            double increment = Math.PI * (3d - Math.Sqrt(5d));

            y = ((index * offset) - 1d) + (offset / 2d);
            double radius = Math.Sqrt(Math.Max(0d, 1d - (y * y)));
            double angle = index * increment;
            x = Math.Cos(angle) * radius;
            z = Math.Sin(angle) * radius;
        }
    }
}
