using System;
using LifeSimulation.Simulation.Environment;

namespace LifeSimulation.Presentation
{
    /// <summary>One point on the planet surface.</summary>
    public readonly struct PlanetSample
    {
        public PlanetSample(float elevation, float moisture, float temperature, float continent)
        {
            Elevation = elevation;
            Moisture = moisture;
            Temperature = temperature;
            Continent = continent;
        }

        /// <summary>
        /// <b>Signed displacement from sea level.</b> Positive is land, negative is sea bed, zero is
        /// the coast.
        ///
        /// <para>Deliberately <b>not</b> a 0..1 field with sea level somewhere inside it. That was
        /// the previous design and it was the root of the terracing: a bounded range forces a clamp,
        /// a clamp forces a knee to soften it, and a threshold in the middle forces a branch at the
        /// waterline - three separate places where the slope of the terrain jumps, which is what a
        /// terrace is. Signed displacement has nothing to clamp, no knee and no interior threshold,
        /// so the coast is simply the zero crossing.</para>
        /// </summary>
        public float Elevation { get; }

        public float Moisture { get; }
        public float Temperature { get; }

        /// <summary>Continental shelf height before terrain: negative for oceanic plates.</summary>
        public float Continent { get; }
    }

    /// <summary>
    /// Planet-scale terrain: tectonic structure carrying layered noise, composed as signed
    /// displacement from sea level.
    ///
    /// <para><b>Structure comes from process, not noise.</b> Per the world-generation design, noise
    /// alone cannot produce continents - every feature is independent of every other, so nothing
    /// explains anything else. Land is where continental plates are; ranges lie along boundaries
    /// where plates meet; trenches sit offshore of coastal ranges because one plate goes under the
    /// other. See <see cref="PlateStructure"/>.</para>
    ///
    /// <para><b>Composition follows SebLague/Procedural-Planets</b>, read from source - see
    /// docs/terrain-brainstorm-2-2026-08-23.md. Layers sum as signed contributions, later layers are
    /// masked by earlier ones, and clamping happens per layer where a flat basin is actually wanted,
    /// never as a global squash of the composed result.</para>
    ///
    /// <para><b>Presentation only.</b> Nothing under <c>Assets/Scripts/Simulation</c> reads this, so
    /// it moves no hash and affects no recorded result.</para>
    /// </summary>
    public static class PlanetTerrain
    {
        /// <summary>
        /// Elevation treated as fully high ground when normalising colour. A palette reference only:
        /// nothing is clamped to it and ground may exceed it.
        /// </summary>
        public const float HighGround = 0.55f;

        // Base frequencies in features per radian, roughly 3x apart so each band is visibly its own
        // scale rather than blurring into the next.
        private const double ContinentFrequency = 1.15d;
        private const double MountainFrequency = 3.6d;
        private const double HillFrequency = 6.5d;
        private const double DetailFrequency = 11d;
        private const double MoistureFrequency = 1.9d;
        private const double ClimateNoiseFrequency = 2.4d;
        private const double JitterFrequency = 16d;

        /// <summary>Domain warp on the plate lookup, so coastlines wander off the cell edge.</summary>
        private const double WarpFrequency = 2.1d;
        private const double WarpStrength = 0.32d;

        private const double Lacunarity = 2d;
        private const double Gain = 0.5d;

        /// <summary>
        /// Steepest slope the renderer can represent, in elevation units per radian.
        ///
        /// <para>Measured: the mesh samples every 2.5 units while the ridged band produced roughly 17
        /// units of rise across 4 units of ground - a 76 degree face - which a heightfield renders as
        /// a staircase of alternating near-vertical and near-horizontal facets. Doubling mesh
        /// resolution doubled the stripe count without removing them, so the binding limit is
        /// <b>slope</b>, not sampling frequency.</para>
        /// </summary>
        private const double MaximumSlope = 0.55d;

        /// <summary>Highest safely renderable frequency for a view with this many samples around a full turn.</summary>
        public static double MaximumFrequencyFor(int samplesAroundEquator)
        {
            return samplesAroundEquator / (4d * Math.PI);
        }

        /// <summary>
        /// Octaves that fit under a resolution limit. Octaves finer than the sample spacing do not
        /// add detail, they add aliasing - which is what turned the globe into static.
        /// </summary>
        public static int OctavesUnder(double baseFrequency, double maximumFrequency, int cap)
        {
            int octaves = 1;
            double frequency = baseFrequency;
            while (octaves < cap && frequency * Lacunarity <= maximumFrequency)
            {
                frequency *= Lacunarity;
                octaves++;
            }

            return octaves;
        }

        /// <summary>
        /// Largest amplitude a band at this frequency may carry without exceeding
        /// <see cref="MaximumSlope"/>. Derived rather than hand-tuned, so raising a band's frequency
        /// automatically lowers its height instead of producing cliffs.
        /// </summary>
        private static double SlopeLimited(double amplitude, double frequency)
        {
            return Math.Min(amplitude, MaximumSlope / Math.Max(frequency, 1e-6d));
        }

        public static PlanetSample Sample(int seed, PlateStructure plates, double dx, double dy, double dz, double maximumFrequency)
        {
            if (plates == null)
            {
                throw new ArgumentNullException(
                    nameof(plates),
                    "PlanetTerrain needs a PlateStructure; structure comes from plates, not from noise.");
            }

            // Domain warp the plate lookup so a coastline wanders instead of tracing the Voronoi cell
            // edge. Perturbing a threshold cannot move a boundary; moving the sample position can.
            double warpX = 0d, warpY = 0d, warpZ = 0d;
            AddWarp(seed, 500, dx, dy, dz, WarpFrequency, WarpStrength, ref warpX, ref warpY, ref warpZ);
            AddWarp(seed, 503, dx, dy, dz, WarpFrequency * 3.7d, WarpStrength * 0.35d, ref warpX, ref warpY, ref warpZ);
            PlateSample plate = plates.Sample(dx + warpX, dy + warpY, dz + warpZ);

            // Layer 1: continental shelf, signed. Land and sea come from plate type, not from a
            // threshold applied to a bounded field.
            int shelfOctaves = OctavesUnder(ContinentFrequency, maximumFrequency, 4);
            double shelfNoise = EnvironmentNoise.WarpedFbm(
                seed, channel: 200,
                dx * ContinentFrequency, dy * ContinentFrequency, dz * ContinentFrequency,
                shelfOctaves, Lacunarity, Gain, warpStrength: 0.55d) - 0.5d;

            // Blend the shelf between the two nearest plates. A Voronoi lookup is piecewise constant,
            // so taking only the nearest plate put a measured step of 0.825 in elevation between
            // samples one unit apart - a vertical cliff at every plate boundary, and the source of
            // the terraces that traced closed contours. Blending makes the seam a slope.
            double shelf = Lerp(
                ShelfHeight(plate.NeighbourContinental, plate.NeighbourBaseElevation),
                ShelfHeight(plate.Continental, plate.BaseElevation),
                plate.Blend) + (0.32d * shelfNoise);

            // Layer 2: boundary landforms, signed, and blended across the seam for the same reason
            // the shelf is. The kind and intensity of a boundary are properties of the PAIR of plates
            // and so are already continuous, but which SIDE a point is on flips the instant the
            // nearest plate changes - at a subduction margin that switches a -0.34 trench for a +0.46
            // range, a jump of 0.8 exactly on the boundary. Evaluating both sides and blending
            // removes it.
            double distance = plate.BoundaryDistance;
            double boundary = Lerp(
                BoundaryContribution(plate.Boundary, plate.Intensity, distance, !plate.NeighbourContinental),
                BoundaryContribution(plate.Boundary, plate.Intensity, distance, plate.OnOceanicSide),
                plate.Blend);

            // Layer 3: ranges, masked by the boundary lift so peaks gather along margins and plate
            // interiors stay open. This is the reference implementation's first-layer-as-mask idea.
            int mountainOctaves = OctavesUnder(MountainFrequency, maximumFrequency, 3);
            double ridges = EnvironmentNoise.RidgedFbm(
                seed, channel: 240,
                dx * MountainFrequency, dy * MountainFrequency, dz * MountainFrequency,
                mountainOctaves, Lacunarity, Gain, ridgeWeighting: 1.6d);
            double ranges = SlopeLimited(0.34d, MountainFrequency) * ridges * Math.Max(0d, boundary);

            // Layer 4: rolling ground across all land, so interiors are never flat plateaus. The mask
            // fades in across the shoreline rather than switching at it.
            int hillOctaves = OctavesUnder(HillFrequency, maximumFrequency, 3);
            double hills = (EnvironmentNoise.WarpedFbm(
                seed, channel: 300,
                dx * HillFrequency, dy * HillFrequency, dz * HillFrequency,
                hillOctaves, Lacunarity, Gain, warpStrength: 0.25d) - 0.5d) * 2d;
            double landMask = Smooth01((shelf + boundary) / 0.16d);
            double rolling = SlopeLimited(0.22d, HillFrequency) * hills * (0.35d + (0.65d * landMask));

            // Layer 5: fine detail, amplitude limited by slope rather than chosen by hand.
            int detailOctaves = OctavesUnder(DetailFrequency, maximumFrequency, 2);
            double detail = (EnvironmentNoise.Fbm(
                seed, channel: 320,
                dx * DetailFrequency, dy * DetailFrequency, dz * DetailFrequency,
                detailOctaves, Lacunarity, Gain) - 0.5d) * 2d;

            // Sum. No clamp, no saturation curve, no interior sea level: the coast is the zero
            // crossing of this value.
            double elevation = shelf + boundary + ranges + rolling + (SlopeLimited(0.09d, DetailFrequency) * detail);

            // Climate. dy is sin(latitude) for a unit direction, so this is cos(latitude) - the
            // insolation curve, rather than 1 - |sin| which is far too steep and froze the planet.
            double latitudeTerm = Math.Sqrt(Math.Max(0d, 1d - (dy * dy)));
            double climateNoise = EnvironmentNoise.Fbm(
                seed, channel: 360,
                dx * ClimateNoiseFrequency, dy * ClimateNoiseFrequency, dz * ClimateNoiseFrequency,
                OctavesUnder(ClimateNoiseFrequency, maximumFrequency, 3), Lacunarity, Gain);
            double temperature = (0.78d * latitudeTerm) + (0.22d * climateNoise);

            double aboveWater = Math.Max(0d, elevation) / HighGround;
            temperature = EnvironmentNoise.Clamp01(temperature - (0.30d * aboveWater));

            // Moisture. Contrast expanded because raw fBm spans about .37-.82, and without it the dry
            // end is unreachable and deserts cannot occur at all.
            double moistureNoise = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    seed, channel: 400,
                    dx * MoistureFrequency, dy * MoistureFrequency, dz * MoistureFrequency,
                    OctavesUnder(MoistureFrequency, maximumFrequency, 4), Lacunarity, Gain, warpStrength: 0.4d),
                strength: 2.2d);

            // Continental interiors dry out, which puts deserts inland rather than scattering them.
            double continentality = 1d - (0.85d * Math.Min(1d, aboveWater));

            // High-frequency jitter so biome edges are ragged rather than clean level sets of a
            // smooth field, which read as drawn rather than grown.
            double jitter = EnvironmentNoise.Fbm(
                seed, channel: 440,
                dx * JitterFrequency, dy * JitterFrequency, dz * JitterFrequency,
                OctavesUnder(JitterFrequency, maximumFrequency, 2), Lacunarity, Gain) - 0.5d;

            double moisture = EnvironmentNoise.Clamp01(
                (0.62d * moistureNoise) + (0.38d * continentality) + (0.07d * jitter));
            temperature = EnvironmentNoise.Clamp01(temperature + (0.05d * jitter));

            return new PlanetSample((float)elevation, (float)moisture, (float)temperature, (float)shelf);
        }

        public static PlanetSample SampleAtLatLon(int seed, PlateStructure plates, double latitude, double longitude, double maximumFrequency)
        {
            double cosLatitude = Math.Cos(latitude);
            return Sample(
                seed, plates,
                cosLatitude * Math.Sin(longitude),
                Math.Sin(latitude),
                cosLatitude * Math.Cos(longitude),
                maximumFrequency);
        }

        /// <summary>
        /// Height contributed by a plate margin, for one side of it. Widths are several times the
        /// height so a margin is a ridge rather than a wall.
        /// </summary>
        private static double BoundaryContribution(BoundaryKind kind, double intensity, double distance, bool onOceanicSide)
        {
            switch (kind)
            {
                case BoundaryKind.ContinentalCollision:
                    return 0.55d * intensity * Falloff(distance, 0.40d);
                case BoundaryKind.Subduction:
                    return onOceanicSide
                        ? -0.34d * intensity * Falloff(distance, 0.16d)
                        : 0.46d * intensity * Falloff(distance, 0.28d);
                case BoundaryKind.IslandArc:
                    return 0.42d * intensity * Falloff(distance, 0.12d);
                case BoundaryKind.Divergent:
                    return -0.20d * intensity * Falloff(distance, 0.18d);
                case BoundaryKind.Transform:
                    return 0.05d * intensity * Falloff(distance, 0.10d);
                default:
                    return 0d;
            }
        }

        /// <summary>Shelf height for one plate: continental sits above sea level, oceanic below.</summary>
        private static double ShelfHeight(bool continental, double baseElevation)
        {
            return continental
                ? 0.10d + (0.30d * baseElevation)
                : -0.34d - (0.22d * (1d - baseElevation));
        }

        private static double Lerp(double from, double to, double t)
        {
            return from + ((to - from) * t);
        }

        private static void AddWarp(
            int seed, int channel, double dx, double dy, double dz,
            double frequency, double strength,
            ref double warpX, ref double warpY, ref double warpZ)
        {
            warpX += (EnvironmentNoise.ValueNoise(seed, channel, dx * frequency, dy * frequency, dz * frequency) - 0.5d) * strength;
            warpY += (EnvironmentNoise.ValueNoise(seed, channel + 1, dx * frequency, dy * frequency, dz * frequency) - 0.5d) * strength;
            warpZ += (EnvironmentNoise.ValueNoise(seed, channel + 2, dx * frequency, dy * frequency, dz * frequency) - 0.5d) * strength;
        }

        /// <summary>
        /// Influence of a boundary at an angular distance. Exponential rather than linear, so a range
        /// has a crest and shoulders instead of a triangular profile, and plate interiors are
        /// genuinely unaffected rather than faintly tilted.
        /// </summary>
        private static double Falloff(double distance, double width)
        {
            if (width <= 0d) return 0d;
            double t = distance / width;
            return Math.Exp(-t * t);
        }

        /// <summary>Smoothstep over -1..1, for fading a mask in across a shoreline.</summary>
        private static double Smooth01(double value)
        {
            double t = EnvironmentNoise.Clamp01((value + 1d) * 0.5d);
            return t * t * (3d - (2d * t));
        }
    }
}
