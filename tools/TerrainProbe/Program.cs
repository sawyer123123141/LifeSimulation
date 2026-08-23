using System;
using System.Collections.Generic;
using LifeSimulation.Presentation;

namespace LifeSimulation.TerrainProbe
{
    /// <summary>
    /// Terrain field measurement, without Unity.
    ///
    /// <para><b>What it measures.</b> For each flat view, the adjacent-sample gradient over land -
    /// the difference in metres between neighbouring mesh samples, divided by the spacing between
    /// them. That is a real grade: 0.10 is gentle, 0.36 is the slope ceiling, 1.00 is a 45 degree
    /// face. Distribution statistics cannot see roughness at all; deciles and land fraction came out
    /// identical whether the field was smooth or carrying a measured 0.825 cliff.</para>
    ///
    /// <para><b>Each view is sampled at its own resolvable frequency</b>, exactly as
    /// <c>TerrainMeshBuilder.BuildPatch</c> computes it, because the fine bands only exist in the
    /// closer views. Measuring every view at the globe's frequency describes a field none of them
    /// renders - which is what the editor instrument did, and it is why the creature-scale relief
    /// that turned out to be the problem was invisible to it.</para>
    ///
    /// <para><b>Field only.</b> It cannot see a lighting artefact, a triangulation seam or a
    /// z-fight; the render cannot see a step discontinuity in the field. Three instruments, each
    /// blind to what the others catch.</para>
    /// </summary>
    internal static class Program
    {
        private const int Seed = 42;

        /// <summary>Matches <c>TerrainMeshBuilder.PatchResolution</c>.</summary>
        private const int Side = 193;

        /// <summary>Matches <c>TerrainMeshBuilder</c>: one radian of arc is 500 metres.</summary>
        private const double SphereRadius = 500d;

        /// <summary>Matches <c>TerrainMeshBuilder.ElevationToWorldUnits</c>.</summary>
        private const double ElevationToMetres = 30d;

        private static void Main()
        {
            PlateStructure plates = PlateStructure.CreateActive(Seed);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);
            Console.WriteLine($"seed {Seed}   coastal centre lat {centreLatitude:0.0000} lon {centreLongitude:0.0000}");
            Console.WriteLine("grade is |dh| between adjacent samples, metres per metre, over land only");

            // The creature-scale bands are the ones that are hard to judge by eye and easy to get
            // wrong by an order of magnitude, so every run reports what they are contributing.
            var current = new TerrainSettings();
            var without = new TerrainSettings { LocalAmplitude = 0d, MicroAmplitude = 0d };

            foreach (double halfWidth in new[] { 200d, 100d, 25d })
            {
                double maximumFrequency = MaximumFrequencyFor(halfWidth);
                Console.WriteLine();
                Console.WriteLine(
                    $"=== window {halfWidth * 2:0} units   maxFrequency {maximumFrequency:0.0}   " +
                    $"sample spacing {2d * halfWidth / (Side - 1):0.00} m");
                Report(plates, centreLatitude, centreLongitude, halfWidth, maximumFrequency, "as configured", current);
                Report(plates, centreLatitude, centreLongitude, halfWidth, maximumFrequency, "planet bands only", without);
            }
        }

        /// <summary>Highest frequency a patch of this width can carry, as <c>BuildPatch</c> derives it.</summary>
        private static double MaximumFrequencyFor(double halfWidth)
        {
            double angularWidth = 2d * halfWidth / SphereRadius;
            return (int)(Side * (2d * Math.PI) / angularWidth) / (4d * Math.PI);
        }

        private static void Report(
            PlateStructure plates, double centreLatitude, double centreLongitude,
            double halfWidth, double maximumFrequency, string label, TerrainSettings settings)
        {
            var height = new double[Side, Side];
            for (int row = 0; row < Side; row++)
            {
                double z = -halfWidth + (2d * halfWidth * row / (Side - 1));
                for (int column = 0; column < Side; column++)
                {
                    double x = -halfWidth + (2d * halfWidth * column / (Side - 1));
                    PlanetSample sample = PlanetTerrain.SampleAtLatLon(
                        Seed, plates,
                        centreLatitude + (z / SphereRadius), centreLongitude + (x / SphereRadius),
                        maximumFrequency, settings);

                    height[row, column] = sample.Elevation * ElevationToMetres;
                }
            }

            double spacing = 2d * halfWidth / (Side - 1);
            var landGrades = new List<double>();
            double lowest = double.MaxValue;
            double highest = double.MinValue;
            for (int row = 0; row < Side; row++)
            {
                for (int column = 0; column + 1 < Side; column++)
                {
                    double left = height[row, column];
                    double right = height[row, column + 1];
                    if (left > 0d && right > 0d) landGrades.Add(Math.Abs(right - left) / spacing);
                    if (left < lowest) lowest = left;
                    if (left > highest) highest = left;
                }
            }

            landGrades.Sort();
            Console.WriteLine(
                $"  {label,-18} median {Quantile(landGrades, 0.5):0.000}  p90 {Quantile(landGrades, 0.9):0.000}  " +
                $"p99 {Quantile(landGrades, 0.99):0.000}  max {Quantile(landGrades, 1d):0.000}   " +
                $"height {lowest:0.0}..{highest:0.0} m");
        }

        private static double Quantile(List<double> sorted, double quantile)
        {
            if (sorted.Count == 0) return 0d;
            int index = (int)Math.Round(quantile * (sorted.Count - 1));
            return sorted[index];
        }
    }
}
