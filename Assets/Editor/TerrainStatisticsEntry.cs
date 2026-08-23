using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LifeSimulation.Presentation;
using UnityEditor;
using UnityEngine;

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

        [MenuItem("Life Simulation/Dump Terrain Statistics")]
        public static void Dump()
        {
            var report = new StringBuilder();
            var plates = new PlateStructure(Seed);

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

                    PlanetSample sample = PlanetTerrain.Sample(Seed, plates, dx, dy, dz, maximumFrequency);
                    elevations.Add(sample.Elevation);
                    moistures.Add(sample.Moisture);
                    temperatures.Add(sample.Temperature);

                    BiomeKind biome = PlanetBiome.Classify(sample);
                    biomeCounts.TryGetValue(biome, out int count);
                    biomeCounts[biome] = count + 1;

                    if (sample.Elevation > PlanetTerrain.SeaLevel) landElevations.Add(sample.Elevation);

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

                    PlanetSample sample = PlanetTerrain.Sample(Seed, plates, dx, dy, dz, maximumFrequency);
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
            report.AppendLine($"sea level {PlanetTerrain.SeaLevel:0.000}");
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
                report.AppendLine($"land elevation uses {(landElevations[land - 1] - PlanetTerrain.SeaLevel) / (1d - PlanetTerrain.SeaLevel):0.000} of the range above sea level");
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
            AppendWindow(report, plates, "wide patch (400u)", centreLatitude, centreLongitude, 200d, maximumFrequency);
            AppendWindow(report, plates, "close view (200u)", centreLatitude, centreLongitude, 100d, maximumFrequency);
            AppendWindow(report, plates, "arena (50u)", centreLatitude, centreLongitude, 25d, maximumFrequency);
            AppendWindow(report, plates, "origin-centred 400u", 0d, 0d, 200d, maximumFrequency);

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

        /// <summary>Land fraction and elevation spread inside one flat-view window.</summary>
        private static void AppendWindow(
            StringBuilder report, PlateStructure plates, string label,
            double centreLatitude, double centreLongitude, double halfWidth, double maximumFrequency)
        {
            const int steps = 64;
            int land = 0;
            double minimum = 1d;
            double maximum = 0d;
            var biomes = new HashSet<BiomeKind>();
            for (int row = 0; row < steps; row++)
            {
                double z = ((row / (double)(steps - 1)) - 0.5d) * 2d * halfWidth;
                for (int column = 0; column < steps; column++)
                {
                    double x = ((column / (double)(steps - 1)) - 0.5d) * 2d * halfWidth;
                    double latitude = centreLatitude + (z / 500d);
                    double longitude = centreLongitude + (x / 500d);
                    PlanetSample sample = PlanetTerrain.SampleAtLatLon(Seed, plates, latitude, longitude, maximumFrequency);
                    if (sample.Elevation > PlanetTerrain.SeaLevel) land++;
                    if (sample.Elevation < minimum) minimum = sample.Elevation;
                    if (sample.Elevation > maximum) maximum = sample.Elevation;
                    biomes.Add(PlanetBiome.Classify(sample));
                }
            }

            report.AppendLine($"{label,-22} land {land / (double)(steps * steps):0.000}  elevation {minimum:0.000}-{maximum:0.000}  biomes {biomes.Count}");
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
