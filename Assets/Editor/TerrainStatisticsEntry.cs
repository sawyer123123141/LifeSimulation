using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LifeSimulation.Presentation;
using UnityEditor;
using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// Measures what the terrain generator actually produces, instead of inferring it from a
    /// screenshot.
    ///
    /// <para><b>Why.</b> Five rounds of terrain changes produced no visible difference, because the
    /// terms being adjusted were an order of magnitude smaller than the term that dominates. A render
    /// cannot distinguish "this change did nothing" from "this change did something worth 3% of the
    /// dominant term". These numbers can. See docs/terrain-brainstorm-2026-08-23.md.</para>
    ///
    /// <para>Run headless:</para>
    /// <code>
    /// Unity.exe -batchmode -nographics -quit -projectPath &lt;project&gt; \
    ///   -executeMethod LifeSimulation.EditorTools.TerrainStatisticsEntry.Dump
    /// </code>
    /// </summary>
    public static class TerrainStatisticsEntry
    {
        private const int LongitudeSamples = 512;
        private const int LatitudeSamples = 256;
        private const int Seed = 42;

        /// <summary>Matches <see cref="TerrainMeshBuilder"/>: one radian of arc is 500 metres.</summary>
        private const double SphereRadius = 500d;

        [MenuItem("Life Simulation/Dump Terrain Statistics")]
        public static void Dump()
        {
            var report = new StringBuilder();
            PlateStructure plates = TerrainView.CreatePlates(Seed);

            // Sample at the resolution the globe view draws at, so the numbers describe what is
            // actually rendered rather than a sharper field nobody sees.
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(LongitudeSamples);

            var elevations = new List<double>(LongitudeSamples * LatitudeSamples);
            var moistures = new List<double>(elevations.Capacity);
            var temperatures = new List<double>(elevations.Capacity);
            var biomeCounts = new Dictionary<BiomeKind, int>();
            var landElevations = new List<double>();

            // Elevation against distance to the nearest plate boundary: if boundaries raise ranges,
            // mean elevation must fall as distance grows. If it does not, the boundary machinery is
            // not reaching the output.
            const int distanceBuckets = 8;
            var kindNear = new Dictionary<BoundaryKind, double[]>();
            var kindFar = new Dictionary<BoundaryKind, double[]>();
            var bucketTotals = new double[distanceBuckets];
            var bucketCounts = new int[distanceBuckets];
            double maximumBoundaryDistance = 0d;

            for (int latitudeIndex = 0; latitudeIndex < LatitudeSamples; latitudeIndex++)
            {
                double latitude = (((latitudeIndex + 0.5d) / LatitudeSamples) - 0.5d) * Math.PI;
                double cosLatitude = Math.Cos(latitude);

                // Area weighting: a lat/lon grid over-samples the poles. Cells are counted by their
                // true area so "30% land" means 30% of the surface, not 30% of the grid.
                for (int longitudeIndex = 0; longitudeIndex < LongitudeSamples; longitudeIndex++)
                {
                    double longitude = (((longitudeIndex + 0.5d) / LongitudeSamples) - 0.5d) * 2d * Math.PI;
                    double dx = cosLatitude * Math.Sin(longitude);
                    double dy = Math.Sin(latitude);
                    double dz = cosLatitude * Math.Cos(longitude);

                    PlanetSample sample = PlanetTerrain.Sample(Seed, plates, dx, dy, dz, maximumFrequency, TerrainView.Settings);
                    elevations.Add(sample.Elevation);
                    moistures.Add(sample.Moisture);
                    temperatures.Add(sample.Temperature);

                    BiomeKind biome = PlanetBiome.Classify(sample);
                    biomeCounts.TryGetValue(biome, out int count);
                    biomeCounts[biome] = count + 1;

                    if (sample.Elevation > 0f) landElevations.Add(sample.Elevation);

                    PlateSample plate = plates.Sample(dx, dy, dz);
                    if (plate.BoundaryDistance > maximumBoundaryDistance) maximumBoundaryDistance = plate.BoundaryDistance;
                }
            }

            // Second pass for the boundary buckets, now that the range is known.
            for (int latitudeIndex = 0; latitudeIndex < LatitudeSamples; latitudeIndex += 2)
            {
                double latitude = (((latitudeIndex + 0.5d) / LatitudeSamples) - 0.5d) * Math.PI;
                double cosLatitude = Math.Cos(latitude);
                for (int longitudeIndex = 0; longitudeIndex < LongitudeSamples; longitudeIndex += 2)
                {
                    double longitude = (((longitudeIndex + 0.5d) / LongitudeSamples) - 0.5d) * 2d * Math.PI;
                    double dx = cosLatitude * Math.Sin(longitude);
                    double dy = Math.Sin(latitude);
                    double dz = cosLatitude * Math.Cos(longitude);

                    PlateSample plate = plates.Sample(dx, dy, dz);
                    if (!plate.Continental) continue;

                    PlanetSample sample = PlanetTerrain.Sample(Seed, plates, dx, dy, dz, maximumFrequency, TerrainView.Settings);
                    int bucket = Math.Min(
                        distanceBuckets - 1,
                        (int)(plate.BoundaryDistance / Math.Max(1e-6d, maximumBoundaryDistance) * distanceBuckets));
                    bucketTotals[bucket] += sample.Elevation;
                    bucketCounts[bucket]++;

                    // Near means within a quarter of the widest boundary influence, far beyond half.
                    Dictionary<BoundaryKind, double[]> target =
                        plate.BoundaryDistance < maximumBoundaryDistance * 0.15d ? kindNear
                        : plate.BoundaryDistance > maximumBoundaryDistance * 0.5d ? kindFar
                        : null;
                    if (target != null)
                    {
                        if (!target.TryGetValue(plate.Boundary, out double[] accumulator))
                        {
                            accumulator = new double[2];
                            target[plate.Boundary] = accumulator;
                        }

                        accumulator[0] += sample.Elevation;
                        accumulator[1]++;
                    }
                }
            }

            elevations.Sort();
            moistures.Sort();
            temperatures.Sort();
            landElevations.Sort();

            report.AppendLine("=== TERRAIN STATISTICS ===");
            report.AppendLine($"seed {Seed}, {LongitudeSamples}x{LatitudeSamples} samples, maxFrequency {maximumFrequency:0.0}");
            report.AppendLine("sea level 0.000 (elevation is signed displacement)");
            report.AppendLine();

            AppendDeciles(report, "elevation", elevations);
            AppendDeciles(report, "moisture", moistures);
            AppendDeciles(report, "temperature", temperatures);

            int total = elevations.Count;
            int land = landElevations.Count;
            report.AppendLine();
            report.AppendLine($"LAND FRACTION {land / (double)total:0.000}  (target ~0.30)");
            if (land > 0)
            {
                AppendDeciles(report, "land elevation", landElevations);
                report.AppendLine($"highest land {landElevations[land - 1]:0.000} against HighGround {PlanetTerrain.HighGround:0.000}");
            }

            report.AppendLine();
            report.AppendLine("ELEVATION BY DISTANCE FROM PLATE BOUNDARY (continental only)");
            report.AppendLine("bucket | mean elevation | samples");
            for (int bucket = 0; bucket < distanceBuckets; bucket++)
            {
                double mean = bucketCounts[bucket] == 0 ? 0d : bucketTotals[bucket] / bucketCounts[bucket];
                report.AppendLine($"{bucket,6} | {mean,14:0.0000} | {bucketCounts[bucket],7}");
            }

            report.AppendLine();
            report.AppendLine("ELEVATION NEAR vs FAR, BY BOUNDARY KIND (continental only)");
            report.AppendLine("Averaging across kinds hides the signal: a collision raising ground is");
            report.AppendLine("diluted by transforms that raise none and rifts that lower it.");
            report.AppendLine("kind                 | near  | far   | lift");
            foreach (BoundaryKind kind in Enum.GetValues(typeof(BoundaryKind)))
            {
                if (!kindNear.ContainsKey(kind)) kindNear[kind] = new double[2];
                if (!kindFar.ContainsKey(kind)) kindFar[kind] = new double[2];
            }

            foreach (KeyValuePair<BoundaryKind, double[]> pair in kindNear)
            {
                double near = pair.Value[1] <= 0 ? 0d : pair.Value[0] / pair.Value[1];
                double[] far = kindFar[pair.Key];
                double farMean = far[1] <= 0 ? 0d : far[0] / far[1];
                report.AppendLine($"{pair.Key,-20} | {near,5:0.000} | {farMean,5:0.000} | {near - farMean,5:0.000}");
            }

            // What the flat views actually see. The global land fraction is true and useless for
            // this: a 400-unit patch spans about one plate, so where it is centred decides whether
            // it shows a continent or an empty ocean.
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);
            report.AppendLine();
            report.AppendLine($"FLAT VIEW CENTRE  lat {centreLatitude:0.000} lon {centreLongitude:0.000}");
            report.AppendLine("Each window is sampled at ITS OWN resolvable frequency and at the mesh");
            report.AppendLine("resolution it is drawn with - not at the globe's. The bands below 55");
            report.AppendLine("cycles/radian only exist in the closer views, so measuring every window");
            report.AppendLine("at the globe frequency describes a field none of these views renders.");
            report.AppendLine("Grade is |dh| between adjacent mesh samples, in metres per metre: 0.10");
            report.AppendLine("is a gentle slope, 0.36 the slope ceiling, 1.00 a 45 degree face.");
            AppendWindow(report, plates, "wide patch (400u)", centreLatitude, centreLongitude, 200d);
            AppendWindow(report, plates, "close view (200u)", centreLatitude, centreLongitude, 100d);
            AppendWindow(report, plates, "arena (50u)", centreLatitude, centreLongitude, 25d);
            AppendWindow(report, plates, "origin-centred 400u", 0d, 0d, 200d);

            // The same window walked toward a pole. The global biome counts say the planet has ice,
            // tundra and desert; the shipped view centre is at latitude -15 degrees and shows none of
            // them. A biome that exists globally and never appears in any view is, for every purpose
            // anyone has, absent - so the sweep is the check that the palette is reachable rather
            // than merely present.
            report.AppendLine();
            report.AppendLine("THE SAME WINDOWS AT OTHER LATITUDES (same longitude)");
            report.AppendLine("Both flat views move together, so the close view matters as much as the");
            report.AppendLine("wide one - and it holds a quarter of the area, so it fits fewer biomes.");
            foreach (double halfWidth in new[] { 200d, 100d })
            {
                report.AppendLine();
                report.AppendLine($"-- {halfWidth * 2:0} unit window --");
                foreach (double latitude in new[] { -1.30d, -1.00d, -0.70d, -0.40d, 0d, 0.40d, 0.55d, 0.70d, 0.85d, 1.00d, 1.30d })
                {
                    AppendWindow(
                        report, plates, $"lat {latitude * 180d / Math.PI,6:0.0} deg",
                        latitude, centreLongitude, halfWidth);
                }
            }

            // Contrast() clamps, so an over-strong setting pins whole regions to 0 or 1. Saturated
            // ground is flat ground with a hard edge where it crosses - banding, not variety.
            int moistureLow = 0, moistureHigh = 0, temperatureLow = 0, temperatureHigh = 0;
            foreach (double value in moistures)
            {
                if (value <= 0.001d) moistureLow++;
                if (value >= 0.999d) moistureHigh++;
            }

            foreach (double value in temperatures)
            {
                if (value <= 0.001d) temperatureLow++;
                if (value >= 0.999d) temperatureHigh++;
            }

            int elevationHigh = 0, elevationLow = 0;
            foreach (double value in elevations)
            {
                if (value >= 0.999d) elevationHigh++;
                if (value <= 0.001d) elevationLow++;
            }

            report.AppendLine();
            report.AppendLine($"elevation    at 0 {elevationLow / (double)total:0.0000}   at 1 {elevationHigh / (double)total:0.0000}   <- clamped plateaus with cliff edges");

            report.AppendLine();
            report.AppendLine("SATURATION (clamped to 0 or 1 - flat regions with hard edges)");
            report.AppendLine($"moisture     at 0 {moistureLow / (double)total:0.0000}   at 1 {moistureHigh / (double)total:0.0000}");
            report.AppendLine($"temperature  at 0 {temperatureLow / (double)total:0.0000}   at 1 {temperatureHigh / (double)total:0.0000}");

            // Adjacent-sample gradient. A terrace is a slope discontinuity, so if the field is
            // continuous the largest jump between neighbouring samples should be small and smoothly
            // distributed. A big outlier means the FIELD steps, not the renderer.
            {
                const int line = 400;
                double worst = 0d;
                double worstAt = 0d;
                double previous = double.NaN;
                var jumps = new List<double>();
                for (int step = 0; step <= line; step++)
                {
                    double t = step / (double)line;
                    double lat = centreLatitude + ((t - 0.5d) * 0.8d);
                    double value = PlanetTerrain.SampleAtLatLon(Seed, plates, lat, centreLongitude, maximumFrequency, TerrainView.Settings).Elevation;
                    if (!double.IsNaN(previous))
                    {
                        double jump = Math.Abs(value - previous);
                        jumps.Add(jump);
                        if (jump > worst) { worst = jump; worstAt = lat; }
                    }

                    previous = value;
                }

                jumps.Sort();
                report.AppendLine();
                report.AppendLine("ADJACENT-SAMPLE ELEVATION JUMP along a meridian through the view centre");
                report.AppendLine($"samples {line}, spacing {0.8d / line * 500d:0.00} units");
                report.AppendLine($"median {jumps[jumps.Count / 2]:0.00000}   p90 {jumps[(int)(jumps.Count * 0.9)]:0.00000}   max {worst:0.00000} at lat {worstAt:0.000}");
                report.AppendLine($"max/median ratio {worst / Math.Max(1e-9d, jumps[jumps.Count / 2]):0.0}  (a smooth field is under ~20)");
            }

            report.AppendLine();
            report.AppendLine("BIOME COUNTS");
            foreach (KeyValuePair<BiomeKind, int> pair in biomeCounts)
            {
                report.AppendLine($"{pair.Key,-12} {pair.Value,8}  {pair.Value / (double)total:0.000}");
            }

            string text = report.ToString();
            Debug.Log(text);
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "terrain-statistics.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        /// <summary>
        /// What one flat view actually shows: land, biomes, and how steep the ground is.
        ///
        /// <para><b>Sampled at the window's own resolvable frequency</b>, computed exactly as
        /// <see cref="TerrainMeshBuilder.BuildPatch"/> computes it. This used to take the globe's
        /// frequency for every window, which silenced the local and micro bands entirely - so the
        /// instrument reported on a field that no flat view renders, and could not have seen the
        /// creature-scale relief that turned out to be the thing that looked wrong.</para>
        ///
        /// <para><b>Grade, not just spread.</b> Deciles and land fraction are identical whether a
        /// field is smooth or cliffed; the adjacent-sample gradient is what found the 885x plate
        /// step, and it is the only number here that can see roughness at all.</para>
        /// </summary>
        private static void AppendWindow(
            StringBuilder report, PlateStructure plates, string label,
            double centreLatitude, double centreLongitude, double halfWidth)
        {
            int side = TerrainMeshBuilder.PatchResolution;
            double angularWidth = 2d * halfWidth / SphereRadius;
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(
                (int)(side * (2d * Math.PI) / angularWidth));

            var height = new double[side, side];
            int land = 0;
            double minimum = double.MaxValue;
            double maximum = double.MinValue;
            var biomes = new HashSet<BiomeKind>();
            var biomeCounts = new Dictionary<BiomeKind, int>();
            for (int row = 0; row < side; row++)
            {
                double z = -halfWidth + (2d * halfWidth * row / (side - 1));
                for (int column = 0; column < side; column++)
                {
                    double x = -halfWidth + (2d * halfWidth * column / (side - 1));
                    PlanetSample sample = PlanetTerrain.SampleAtLatLon(
                        Seed, plates,
                        centreLatitude + (z / SphereRadius), centreLongitude + (x / SphereRadius),
                        maximumFrequency, TerrainView.Settings);

                    height[row, column] = sample.Elevation * TerrainMeshBuilder.ElevationToWorldUnits;
                    if (sample.Elevation > 0f) land++;
                    if (sample.Elevation < minimum) minimum = sample.Elevation;
                    if (sample.Elevation > maximum) maximum = sample.Elevation;
                    BiomeKind kind = PlanetBiome.Classify(sample);
                    biomes.Add(kind);
                    biomeCounts.TryGetValue(kind, out int seen);
                    biomeCounts[kind] = seen + 1;
                }
            }

            double spacing = 2d * halfWidth / (side - 1);
            var landGrades = new List<double>();
            for (int row = 0; row < side; row++)
            {
                for (int column = 0; column + 1 < side; column++)
                {
                    double left = height[row, column];
                    double right = height[row, column + 1];
                    if (left > 0d && right > 0d) landGrades.Add(Math.Abs(right - left) / spacing);
                }
            }

            landGrades.Sort();
            report.AppendLine(
                $"{label,-22} land {land / (double)(side * side):0.000}  elevation {minimum:0.000}-{maximum:0.000}  " +
                $"biomes {biomes.Count}  maxFreq {maximumFrequency,6:0.0}  spacing {spacing:0.00} m");
            report.AppendLine(
                $"{string.Empty,-22} land grade  median {Quantile(landGrades, 0.5):0.000}  " +
                $"p90 {Quantile(landGrades, 0.9):0.000}  p99 {Quantile(landGrades, 0.99):0.000}  " +
                $"max {Quantile(landGrades, 1d):0.000}");

            // Named, not counted. "biomes 5" says the window is varied; it does not say whether any
            // of the five is the one somebody reported never seeing.
            var ordered = new List<KeyValuePair<BiomeKind, int>>(biomeCounts);
            ordered.Sort((left, right) => right.Value.CompareTo(left.Value));
            var mix = new StringBuilder();
            foreach (KeyValuePair<BiomeKind, int> entry in ordered)
            {
                if (mix.Length > 0) mix.Append("  ");
                mix.Append($"{entry.Key} {entry.Value / (double)(side * side):0.00%}");
            }

            report.AppendLine($"{string.Empty,-22} {mix}");
        }

        /// <summary>Value at a quantile of an already sorted list.</summary>
        private static double Quantile(List<double> sorted, double quantile)
        {
            if (sorted.Count == 0) return 0d;
            int index = (int)Math.Round(quantile * (sorted.Count - 1));
            return sorted[index];
        }

        private static void AppendDeciles(StringBuilder report, string label, List<double> sorted)
        {
            if (sorted.Count == 0)
            {
                report.AppendLine($"{label}: no samples");
                return;
            }

            report.Append(label.PadRight(16));
            report.Append("min ").Append(sorted[0].ToString("0.000", CultureInfo.InvariantCulture));
            for (int decile = 1; decile <= 9; decile++)
            {
                int index = Math.Min(sorted.Count - 1, (int)(sorted.Count * (decile / 10d)));
                report.Append("  p").Append(decile * 10).Append(' ').Append(sorted[index].ToString("0.000", CultureInfo.InvariantCulture));
            }

            report.Append("  max ").Append(sorted[sorted.Count - 1].ToString("0.000", CultureInfo.InvariantCulture));
            report.AppendLine();
        }
    }
}
