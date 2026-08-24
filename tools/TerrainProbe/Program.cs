using System;
using System.Collections.Generic;
using System.Text;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.TerrainProbe
{
    /// <summary>
    /// Terrain field measurement, without Unity.
    ///
    /// <para><b>What it measures.</b> Two things a person actually asks about a view: how steep the
    /// ground is, and which biomes are in it.</para>
    ///
    /// <para>Steepness is the adjacent-sample gradient over land - the difference in metres between
    /// neighbouring mesh samples divided by the spacing between them, which is a real grade. 0.10 is
    /// gentle, 0.36 is the slope ceiling, 1.00 is a 45 degree face. Distribution statistics cannot
    /// see roughness at all; deciles and land fraction came out identical whether the field was
    /// smooth or carrying a measured 0.825 cliff.</para>
    ///
    /// <para>Biomes are <b>named with their share</b>, not counted. "5 biomes" says a window is
    /// varied and cannot say whether any of the five is the one somebody reported never seeing.</para>
    ///
    /// <para><b>Each view is sampled at its own resolvable frequency</b>, exactly as
    /// <c>TerrainMeshBuilder.BuildPatch</c> computes it, because the fine bands only exist in the
    /// closer views. Measuring every view at the globe's frequency describes a field none of them
    /// renders.</para>
    ///
    /// <para><b>Field only.</b> It cannot see a lighting artefact, a triangulation seam or a
    /// z-fight; the render cannot see a step discontinuity in the field.</para>
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

        /// <summary>The two flat views, by half width: `K` once, then twice.</summary>
        private static readonly double[] Views = { 200d, 100d };

        private static void Main(string[] args)
        {
            var current = new TerrainSettings();
            if (args.Length > 0 && args[0] == "--ice")
            {
                // Sampled at the globe's own band limit, which is what a person looking at the
                // planet actually sees - measuring ice at arena resolution would count specks the
                // render never draws.
                Ice.Report(Seed, current, MaximumFrequencyFor(200d));
                return;
            }

            PlateStructure plates = PlateStructure.Create(Seed, current);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);
            Console.WriteLine($"seed {Seed}   default centre lat {centreLatitude:0.0000} lon {centreLongitude:0.0000}");
            Console.WriteLine("grade is |dh| between adjacent samples, metres per metre, over land only");

            var withoutFineBands = new TerrainSettings { LocalAmplitude = 0d, MicroAmplitude = 0d };

            foreach (double halfWidth in Views)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"=== window {halfWidth * 2:0} units   maxFrequency {MaximumFrequencyFor(halfWidth):0.0}   " +
                    $"sample spacing {2d * halfWidth / (Side - 1):0.00} m");
                Report(plates, centreLatitude, centreLongitude, halfWidth, "as configured", current);
                Report(plates, centreLatitude, centreLongitude, halfWidth, "planet bands only", withoutFineBands);
            }

            // The same window walked toward a pole. A biome that exists globally and appears in no
            // view is, for every purpose anyone has, absent - so this is the check that the palette
            // is reachable rather than merely present.
            foreach (double halfWidth in Views)
            {
                Console.WriteLine();
                Console.WriteLine($"=== {halfWidth * 2:0} unit window along a meridian, same longitude");
                foreach (double latitude in new[] { -1.30d, -0.70d, -0.27d, 0d, 0.40d, 0.55d, 0.70d, 0.85d, 1.00d, 1.30d })
                {
                    Report(
                        plates, latitude, centreLongitude, halfWidth,
                        $"lat {latitude * 180d / Math.PI,6:0.0}", current);
                }
            }

            WorstStep(plates, 0.85d, centreLongitude, 100d);
            WorstStep(plates, centreLatitude, centreLongitude, 100d);
        }

        /// <summary>
        /// The largest single step in the field, and what the plate lookup was doing on either side
        /// of it.
        ///
        /// <para>A median says the ground is smooth on average; a wall is a maximum. This finds the
        /// worst adjacent pair over land and prints both plate samples, because the question when a
        /// step appears is always whether the plate lookup underneath it changed - and a step whose
        /// two sides report different boundary kinds is a piecewise-constant lookup leaking into the
        /// field, not terrain.</para>
        /// </summary>
        private static void WorstStep(
            PlateStructure plates, double centreLatitude, double centreLongitude, double halfWidth)
        {
            double maximumFrequency = MaximumFrequencyFor(halfWidth);
            double spacing = 2d * halfWidth / (Side - 1);
            double worst = -1d;
            double worstLatitude = 0d, worstLongitudeLeft = 0d, worstLongitudeRight = 0d;

            for (int row = 0; row < Side; row++)
            {
                double z = -halfWidth + (2d * halfWidth * row / (Side - 1));
                double latitude = centreLatitude + (z / SphereRadius);
                double previous = double.NaN;
                double previousLongitude = 0d;
                for (int column = 0; column < Side; column++)
                {
                    double x = -halfWidth + (2d * halfWidth * column / (Side - 1));
                    double longitude = centreLongitude + (x / SphereRadius);
                    double elevation = PlanetTerrain
                        .SampleAtLatLon(Seed, plates, latitude, longitude, maximumFrequency, new TerrainSettings())
                        .Elevation * ElevationToMetres;

                    if (!double.IsNaN(previous) && previous > 0d && elevation > 0d)
                    {
                        double step = Math.Abs(elevation - previous) / spacing;
                        if (step > worst)
                        {
                            worst = step;
                            worstLatitude = latitude;
                            worstLongitudeLeft = previousLongitude;
                            worstLongitudeRight = longitude;
                        }
                    }

                    previous = elevation;
                    previousLongitude = longitude;
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                $"=== worst step in the {halfWidth * 2:0} unit window at lat {centreLatitude * 180d / Math.PI:0.0}: " +
                $"grade {worst:0.00} over {spacing:0.00} m");
            DescribePlate(plates, worstLatitude, worstLongitudeLeft, maximumFrequency, "left ");
            DescribePlate(plates, worstLatitude, worstLongitudeRight, maximumFrequency, "right");
        }

        private static void DescribePlate(
            PlateStructure plates, double latitude, double longitude, double maximumFrequency, string label)
        {
            double cosLatitude = Math.Cos(latitude);
            double dx = cosLatitude * Math.Sin(longitude);
            double dy = Math.Sin(latitude);
            double dz = cosLatitude * Math.Cos(longitude);

            PlateSample plate = PlanetTerrain.SamplePlate(Seed, plates, dx, dy, dz, new TerrainSettings());
            PlanetSample sample = PlanetTerrain.Sample(Seed, plates, dx, dy, dz, maximumFrequency, new TerrainSettings());
            Console.WriteLine(
                $"  {label}  elevation {sample.Elevation,7:0.000}  shelf {sample.Continent,7:0.000}  " +
                $"kind {plate.Boundary,-21} intensity {plate.Intensity:0.000}  " +
                $"seam {plate.BoundaryDistance:0.0000}  blend {plate.Blend:0.000}  " +
                $"continental {plate.Continental}/{plate.NeighbourContinental}  " +
                $"base {plate.BaseElevation:0.000}/{plate.NeighbourBaseElevation:0.000}");
        }

        /// <summary>Highest frequency a patch of this width can carry, as <c>BuildPatch</c> derives it.</summary>
        private static double MaximumFrequencyFor(double halfWidth)
        {
            double angularWidth = 2d * halfWidth / SphereRadius;
            return (int)(Side * (2d * Math.PI) / angularWidth) / (4d * Math.PI);
        }

        private static void Report(
            PlateStructure plates, double centreLatitude, double centreLongitude,
            double halfWidth, string label, TerrainSettings settings)
        {
            double maximumFrequency = MaximumFrequencyFor(halfWidth);
            var height = new double[Side, Side];
            var biomeCounts = new Dictionary<BiomeKind, int>();
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
                    BiomeKind kind = PlanetBiome.Classify(sample);
                    biomeCounts.TryGetValue(kind, out int seen);
                    biomeCounts[kind] = seen + 1;
                }
            }

            double spacing = 2d * halfWidth / (Side - 1);
            var landGrades = new List<double>();
            for (int row = 0; row < Side; row++)
            {
                for (int column = 0; column + 1 < Side; column++)
                {
                    double left = height[row, column];
                    double right = height[row, column + 1];
                    if (left > 0d && right > 0d) landGrades.Add(Math.Abs(right - left) / spacing);
                }
            }

            landGrades.Sort();
            Console.WriteLine(
                $"  {label,-18} grade median {Quantile(landGrades, 0.5):0.000}  p90 {Quantile(landGrades, 0.9):0.000}  " +
                $"max {Quantile(landGrades, 1d):0.000}");
            Console.WriteLine($"  {string.Empty,-18} {Mix(biomeCounts, Side * Side)}");
        }

        /// <summary>Biomes present, largest share first.</summary>
        private static string Mix(Dictionary<BiomeKind, int> counts, double total)
        {
            var ordered = new List<KeyValuePair<BiomeKind, int>>(counts);
            ordered.Sort((left, right) => right.Value.CompareTo(left.Value));

            var text = new StringBuilder();
            foreach (KeyValuePair<BiomeKind, int> entry in ordered)
            {
                if (entry.Value / total < 0.0005d) continue;
                if (text.Length > 0) text.Append("  ");
                text.Append($"{entry.Key} {entry.Value / total:0.0%}");
            }

            return text.ToString();
        }

        private static double Quantile(List<double> sorted, double quantile)
        {
            if (sorted.Count == 0) return 0d;
            int index = (int)Math.Round(quantile * (sorted.Count - 1));
            return sorted[index];
        }
    }
}
