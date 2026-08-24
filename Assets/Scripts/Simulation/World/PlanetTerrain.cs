using System;
using LifeSimulation.Simulation.Environment;

namespace LifeSimulation.Simulation.World
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
    /// <para><b>This lives in Simulation now.</b> It was prototyped in Presentation, where fifteen
    /// rounds of iteration moved no hash and needed no re-measure. It moved because the simulation
    /// cannot read Presentation: for a hill to cost a creature anything, the elevation the creature
    /// experiences and the elevation the renderer draws have to come from the same function.</para>
    ///
    /// <para><b>There is no ambient default here, and there must not be.</b> While this was in
    /// Presentation a mutable static held the active settings, which was the right trade for a
    /// tuning panel. In Simulation it would be state outside <c>SimulationConfig</c> that changes
    /// behaviour - invisible to the configuration hash, so two worlds with identical hashes could
    /// diverge. Settings are passed explicitly; the panel's mutable instance stays in Presentation,
    /// in <c>TerrainView</c>.</para>
    /// </summary>
    public static class PlanetTerrain
    {
        /// <summary>
        /// Elevation treated as fully high ground when normalising colour. A palette reference only:
        /// nothing is clamped to it and ground may exceed it.
        /// </summary>
        public const float HighGround = 0.55f;

        /// <summary>
        /// Highest safely renderable frequency for a view with this many samples around a full turn.
        /// </summary>
        public static double MaximumFrequencyFor(int samplesAroundEquator)
        {
            return samplesAroundEquator / (4d * Math.PI);
        }

        /// <summary>
        /// Octaves that fit under a resolution limit. Octaves finer than the sample spacing do not
        /// add detail, they add aliasing - which is what turned the globe into static.
        /// </summary>
        public static int OctavesUnder(double baseFrequency, double maximumFrequency, int cap, double lacunarity = 2d)
        {
            int octaves = 1;
            double frequency = baseFrequency;
            while (octaves < cap && frequency * lacunarity <= maximumFrequency)
            {
                frequency *= lacunarity;
                octaves++;
            }

            return octaves;
        }

        /// <summary>
        /// Largest amplitude a band at this frequency may carry without exceeding
        /// <see cref="MaximumSlope"/>. Derived rather than hand-tuned, so raising a band's frequency
        /// automatically lowers its height instead of producing cliffs.
        /// </summary>
        private static double SlopeLimited(double amplitude, double frequency, double maximumSlope)
        {
            return Math.Min(amplitude, maximumSlope / Math.Max(frequency, 1e-6d));
        }

        /// <summary>
        /// How much of a band at this frequency the view can carry: zero where the mesh can only just
        /// represent it, one half an octave later.
        ///
        /// <para>The bands used to be switched on by <c>if (maximumFrequency >= BandFrequency)</c>,
        /// which makes a band appear at <b>full amplitude</b> the moment the camera crosses a
        /// threshold. Zooming then changed the character of the ground rather than its detail, which
        /// reads as the world being rebuilt rather than approached.</para>
        /// </summary>
        private static double BandWeight(double maximumFrequency, double bandFrequency)
        {
            if (bandFrequency <= 0d) return 1d;
            double t = (maximumFrequency - bandFrequency) / (0.5d * bandFrequency);
            if (t <= 0d) return 0d;
            if (t >= 1d) return 1d;
            return t * t * (3d - (2d * t));
        }

        public static PlanetSample Sample(
            int seed, PlateStructure plates, double dx, double dy, double dz, double maximumFrequency,
            TerrainSettings settings)
        {
            TerrainSettings s = settings ?? throw new ArgumentNullException(nameof(settings));
            if (plates == null)
            {
                throw new ArgumentNullException(
                    nameof(plates),
                    "PlanetTerrain needs a PlateStructure; structure comes from plates, not from noise.");
            }

            // Domain warp the plate lookup so a coastline wanders instead of tracing the Voronoi cell
            // edge. Perturbing a threshold cannot move a boundary; moving the sample position can.
            double warpX = 0d, warpY = 0d, warpZ = 0d;
            AddWarp(seed, 500, dx, dy, dz, s.WarpFrequency, s.WarpStrength, ref warpX, ref warpY, ref warpZ);
            AddWarp(seed, 503, dx, dy, dz, s.WarpFrequency * 3.7d, s.WarpStrength * 0.35d, ref warpX, ref warpY, ref warpZ);
            PlateSample plate = plates.Sample(dx + warpX, dy + warpY, dz + warpZ);

            // Layer 1: continental shelf, signed. Land and sea come from plate type, not from a
            // threshold applied to a bounded field.
            int shelfOctaves = OctavesUnder(s.ContinentFrequency, maximumFrequency, 4, s.Lacunarity);
            double shelfNoise = EnvironmentNoise.WarpedFbm(
                seed, channel: 200,
                dx * s.ContinentFrequency, dy * s.ContinentFrequency, dz * s.ContinentFrequency,
                shelfOctaves, s.Lacunarity, s.Gain, warpStrength: 0.55d) - 0.5d;

            // Blend the shelf between the two nearest plates. A Voronoi lookup is piecewise constant,
            // so taking only the nearest plate put a measured step of 0.825 in elevation between
            // samples one unit apart - a vertical cliff at every plate boundary, and the source of
            // the terraces that traced closed contours. Blending makes the seam a slope.
            //
            // Evaluated against BOTH candidate neighbours and crossfaded. Which plate is
            // second-nearest changes along a line through the cell interior, and everything about a
            // margin - its kind, its intensity, the neighbour's own height - changes with it. See
            // PlateSample.AlternateWeight.
            double shelf = Lerp(
                ShelfFor(plate, plate.Primary),
                ShelfFor(plate, plate.Alternate),
                plate.AlternateWeight) + (s.ShelfNoiseStrength * shelfNoise);

            // Layer 2: boundary landforms, signed, blended twice over.
            //
            // Across the seam, because which SIDE a point is on flips the instant the nearest plate
            // changes - at a subduction margin that switches a -0.34 trench for a +0.46 range, a jump
            // of 0.8 exactly on the boundary.
            //
            // And between the two candidate neighbours, because kind and intensity belong to a PAIR
            // of plates. That was the remaining wall: measured 0.277 to 0.528 across 1.04 metres at
            // latitude 48.7, identical shelf and seam distance on both sides, Divergent on one and
            // ContinentalCollision on the other, with the seam blend already saturated at 1.000 and
            // therefore smoothing nothing.
            double boundary = Lerp(
                BoundaryFor(plate.Primary),
                BoundaryFor(plate.Alternate),
                plate.AlternateWeight);

            // Layer 3: ranges, masked by the boundary lift so peaks gather along margins and plate
            // interiors stay open. This is the reference implementation's first-layer-as-mask idea.
            int mountainOctaves = OctavesUnder(s.MountainFrequency, maximumFrequency, 3, s.Lacunarity);
            double ridges = EnvironmentNoise.RidgedFbm(
                seed, channel: 240,
                dx * s.MountainFrequency, dy * s.MountainFrequency, dz * s.MountainFrequency,
                mountainOctaves, s.Lacunarity, s.Gain, ridgeWeighting: 1.6d);
            double ranges = SlopeLimited(s.RangeAmplitude, s.MountainFrequency, s.MaximumSlope) * ridges * Math.Max(0d, boundary);

            // Layer 4: rolling ground across all land, so interiors are never flat plateaus. The mask
            // fades in across the shoreline rather than switching at it.
            int hillOctaves = OctavesUnder(s.HillFrequency, maximumFrequency, 3, s.Lacunarity);
            double hills = (EnvironmentNoise.WarpedFbm(
                seed, channel: 300,
                dx * s.HillFrequency, dy * s.HillFrequency, dz * s.HillFrequency,
                hillOctaves, s.Lacunarity, s.Gain, warpStrength: 0.25d) - 0.5d) * 2d;
            double landMask = Smooth01((shelf + boundary) / 0.16d);
            double rolling = SlopeLimited(s.RollingAmplitude, s.HillFrequency, s.MaximumSlope) * hills * (0.35d + (0.65d * landMask));

            // Layer 5: fine detail, amplitude limited by slope rather than chosen by hand.
            int detailOctaves = OctavesUnder(s.DetailFrequency, maximumFrequency, 2, s.Lacunarity);
            double detail = (EnvironmentNoise.Fbm(
                seed, channel: 320,
                dx * s.DetailFrequency, dy * s.DetailFrequency, dz * s.DetailFrequency,
                detailOctaves, s.Lacunarity, s.Gain) - 0.5d) * 2d;

            // Layers 6 and 7: local and micro relief, at the scale of the thing walking on them.
            // Faded in across the resolution limit rather than switched on at it - see BandWeight.
            double localWeight = BandWeight(maximumFrequency, s.LocalFrequency);
            double local = 0d;
            if (localWeight > 0d)
            {
                local = localWeight * SlopeLimited(s.LocalAmplitude, s.LocalFrequency, s.MaximumSlope) * ((EnvironmentNoise.WarpedFbm(
                    seed, channel: 460,
                    dx * s.LocalFrequency, dy * s.LocalFrequency, dz * s.LocalFrequency,
                    OctavesUnder(s.LocalFrequency, maximumFrequency, 3, s.Lacunarity), s.Lacunarity, s.Gain, warpStrength: 0.3d) - 0.5d) * 2d);
            }

            double microWeight = BandWeight(maximumFrequency, s.MicroFrequency);
            double micro = 0d;
            if (microWeight > 0d)
            {
                micro = microWeight * SlopeLimited(s.MicroAmplitude, s.MicroFrequency, s.MaximumSlope) * ((EnvironmentNoise.Fbm(
                    seed, channel: 480,
                    dx * s.MicroFrequency, dy * s.MicroFrequency, dz * s.MicroFrequency,
                    OctavesUnder(s.MicroFrequency, maximumFrequency, 2, s.Lacunarity), s.Lacunarity, s.Gain) - 0.5d) * 2d);
            }

            // Local relief belongs on land and in the shallows, not carved into deep ocean floor.
            double localMask = 0.25d + (0.75d * Smooth01((shelf + boundary) / 0.20d));

            // Sum. No clamp, no saturation curve, no interior sea level: the coast is the zero
            // crossing of this value.
            double elevation = shelf + boundary + ranges + rolling
                + (SlopeLimited(s.DetailAmplitude, s.DetailFrequency, s.MaximumSlope) * detail)
                + ((local + micro) * localMask);

            // Climate. dy is sin(latitude) for a unit direction, so this is cos(latitude) - the
            // insolation curve, rather than 1 - |sin| which is far too steep and froze the planet.
            double latitudeTerm = Math.Sqrt(Math.Max(0d, 1d - (dy * dy)));
            double climateNoise = EnvironmentNoise.Fbm(
                seed, channel: 360,
                dx * s.ClimateNoiseFrequency, dy * s.ClimateNoiseFrequency, dz * s.ClimateNoiseFrequency,
                OctavesUnder(s.ClimateNoiseFrequency, maximumFrequency, 3, s.Lacunarity), s.Lacunarity, s.Gain);
            double temperature = (s.TemperatureLatitudeWeight * latitudeTerm)
                + ((1d - s.TemperatureLatitudeWeight) * climateNoise);

            double aboveWater = Math.Max(0d, elevation) / HighGround;
            temperature = EnvironmentNoise.Clamp01(temperature - (s.AltitudeCooling * aboveWater));

            // Moisture. Contrast expanded because raw fBm spans about .37-.82, and without it the dry
            // end is unreachable and deserts cannot occur at all.
            double moistureNoise = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    seed, channel: 400,
                    dx * s.MoistureFrequency, dy * s.MoistureFrequency, dz * s.MoistureFrequency,
                    OctavesUnder(s.MoistureFrequency, maximumFrequency, 4, s.Lacunarity), s.Lacunarity, s.Gain, warpStrength: 0.4d),
                strength: s.MoistureContrast);

            // Continental interiors dry out, which puts deserts inland rather than scattering them.
            double continentality = 1d - (s.Continentality * Math.Min(1d, aboveWater));

            // High-frequency jitter so biome edges are ragged rather than clean level sets of a
            // smooth field, which read as drawn rather than grown.
            double jitter = EnvironmentNoise.Fbm(
                seed, channel: 440,
                dx * s.JitterFrequency, dy * s.JitterFrequency, dz * s.JitterFrequency,
                OctavesUnder(s.JitterFrequency, maximumFrequency, 2, s.Lacunarity), s.Lacunarity, s.Gain) - 0.5d;

            double moisture = EnvironmentNoise.Clamp01(
                (0.62d * moistureNoise) + (0.38d * continentality) + (0.07d * jitter));
            temperature = EnvironmentNoise.Clamp01(temperature + (0.05d * jitter));

            return new PlanetSample((float)elevation, (float)moisture, (float)temperature, (float)shelf);
        }

        /// <summary>
        /// The plate sample behind a point, warp included.
        ///
        /// <para>Diagnostic only. When the field has a step in it, the question is always whether the
        /// plate lookup underneath changed - and answering it from outside meant re-deriving the
        /// domain warp, which is how a diagnostic ends up describing a slightly different point than
        /// the one it is diagnosing.</para>
        /// </summary>
        public static PlateSample SamplePlate(
            int seed, PlateStructure plates, double dx, double dy, double dz, TerrainSettings settings)
        {
            TerrainSettings s = settings ?? throw new ArgumentNullException(nameof(settings));
            double warpX = 0d, warpY = 0d, warpZ = 0d;
            AddWarp(seed, 500, dx, dy, dz, s.WarpFrequency, s.WarpStrength, ref warpX, ref warpY, ref warpZ);
            AddWarp(seed, 503, dx, dy, dz, s.WarpFrequency * 3.7d, s.WarpStrength * 0.35d, ref warpX, ref warpY, ref warpZ);
            return plates.Sample(dx + warpX, dy + warpY, dz + warpZ);
        }

        public static PlanetSample SampleAtLatLon(
            int seed, PlateStructure plates, double latitude, double longitude, double maximumFrequency,
            TerrainSettings settings)
        {
            double cosLatitude = Math.Cos(latitude);
            return Sample(
                seed, plates,
                cosLatitude * Math.Sin(longitude),
                Math.Sin(latitude),
                cosLatitude * Math.Cos(longitude),
                maximumFrequency, settings);
        }

        /// <summary>Shelf height for one candidate neighbour, blended across its seam.</summary>
        private static double ShelfFor(PlateSample plate, PlateNeighbour neighbour)
        {
            return Lerp(
                ShelfHeight(neighbour.Continental, neighbour.BaseElevation),
                ShelfHeight(plate.Continental, plate.BaseElevation),
                neighbour.Blend);
        }

        /// <summary>Boundary landform for one candidate neighbour, blended across its seam.</summary>
        private static double BoundaryFor(PlateNeighbour neighbour)
        {
            double distance = neighbour.BoundaryDistance;
            return Lerp(
                BoundaryContribution(neighbour.Boundary, neighbour.Intensity, distance, !neighbour.Continental),
                BoundaryContribution(neighbour.Boundary, neighbour.Intensity, distance, neighbour.OnOceanicSide),
                neighbour.Blend);
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
