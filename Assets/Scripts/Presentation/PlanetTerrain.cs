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

        /// <summary>0..1, with <see cref="PlanetTerrain.SeaLevel"/> as the waterline.</summary>
        public float Elevation { get; }

        public float Moisture { get; }
        public float Temperature { get; }

        /// <summary>The continent mask before terrain: 0 is deep ocean, 1 is continental interior.</summary>
        public float Continent { get; }
    }

    /// <summary>
    /// Planet-scale terrain, built from explicitly separated scales.
    ///
    /// <para><b>Why this exists rather than reusing the arena field directly.</b> The simulation's
    /// <c>EnvironmentField</c> is one band of noise doing everything: elevation, moisture and
    /// fertility are all 3-5 octave fBm at roughly the same base frequency, sized so about three
    /// features span the 50-unit arena. That is correct for a 50-unit world. Rendered over a wide
    /// area it produces a uniform gravel field - every peak the same size, no continents, no ranges,
    /// no plains - because there is no low-frequency band to give it large structure, and biomes
    /// speckle because moisture varies at the same 17-unit scale as the hills.</para>
    ///
    /// <para><b>The structure.</b> Real terrain reads as terrain because scales are separated and
    /// combined multiplicatively rather than summed into one band:</para>
    /// <list type="number">
    /// <item>a very-low-frequency <b>continent mask</b> that decides where land exists at all;</item>
    /// <item><b>mountain belts</b> (ridged, so they form chains) modulated by that mask, so ranges
    /// sit inside landmasses instead of rising out of the sea at random;</item>
    /// <item><b>local relief</b> at small amplitude, for texture rather than shape.</item>
    /// </list>
    ///
    /// <para><b>Sampling below Nyquist.</b> Every band's octave count is derived from the resolution
    /// the caller will actually render at. Adding octaves finer than the sample spacing does not add
    /// detail - it adds aliasing, which is what turned the globe into static: the finest octave
    /// carried roughly 4,000 features around a sphere drawn with 192 columns. See
    /// <see cref="OctavesUnder"/>.</para>
    ///
    /// <para><b>Presentation only.</b> This drives the terrain viewer. Nothing under
    /// <c>Assets/Scripts/Simulation</c> reads it, so it moves no hash and affects no result. It is a
    /// prototype for what the simulation's field could become at P6/P7, kept out of the simulation
    /// until it earns its way in through a flag and a re-measure.</para>
    /// </summary>
    public static class PlanetTerrain
    {
        /// <summary>Waterline as a fraction of the elevation range.</summary>
        public const float SeaLevel = 0.38f;

        // Base frequencies, in features per radian on the unit sphere. The gaps between them are the
        // point: roughly 3x apart, so each band is visibly its own scale rather than blurring into
        // the next. One continent band, one mountain band, one detail band.
        private const double ContinentFrequency = 1.15d;
        private const double MountainFrequency = 3.6d;

        /// <summary>
        /// Rolling ground. Sits between the mountain and detail bands, and applies to all land
        /// rather than only near plate boundaries - without it, plate interiors are flat plateaus.
        /// </summary>
        private const double HillFrequency = 6.5d;
        private const double DetailFrequency = 11d;
        private const double MoistureFrequency = 1.9d;
        private const double ClimateNoiseFrequency = 2.4d;

        private const double Lacunarity = 2d;
        private const double Gain = 0.5d;

        /// <summary>
        /// How many octaves fit under a resolution limit before they start aliasing.
        ///
        /// <para><paramref name="maximumFrequency"/> is the highest feature density the caller can
        /// represent: with <c>n</c> samples around the equator, that is <c>n / (4 * pi)</c>, since
        /// resolving a wave needs two samples per period. Octaves past this do not render as detail,
        /// they render as noise.</para>
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

        /// <summary>Highest safely renderable frequency for a view with this many samples around a full turn.</summary>
        public static double MaximumFrequencyFor(int samplesAroundEquator)
        {
            return samplesAroundEquator / (4d * Math.PI);
        }

        /// <summary>
        /// Sample the surface at a unit direction. <paramref name="maximumFrequency"/> comes from
        /// <see cref="MaximumFrequencyFor"/> for the view being drawn.
        /// </summary>
        public static PlanetSample Sample(int seed, PlateStructure plates, double dx, double dy, double dz, double maximumFrequency)
        {
            if (plates == null)
            {
                throw new ArgumentNullException(
                    nameof(plates),
                    "PlanetTerrain needs a PlateStructure; structure comes from plates, not from noise.");
            }

            // --- 1. Structure comes from plates, not from noise.
            //
            // The world-generation design is explicit that noise cannot produce continents: every
            // feature in a noise field is independent of every other, so nothing explains anything
            // else and it reads as splatter however it is tuned. Land is where continental plates
            // are, and relief is where plates meet - which is why ranges form chains, why trenches
            // sit offshore of coastal ranges, and why island arcs curve.
            PlateSample plate = plates.Sample(dx, dy, dz);

            // Coastlines follow plate edges exactly if left alone, which reads as polygonal. A
            // warped noise band perturbs the land/sea threshold so the coast wanders across the
            // boundary without moving the plate itself.
            int coastOctaves = OctavesUnder(ContinentFrequency, maximumFrequency, 4);
            double coastNoise = EnvironmentNoise.WarpedFbm(
                seed, channel: 200,
                dx * ContinentFrequency, dy * ContinentFrequency, dz * ContinentFrequency,
                coastOctaves, Lacunarity, Gain, warpStrength: 0.55d);

            double continent = EnvironmentNoise.Clamp01(plate.BaseElevation + (0.22d * (coastNoise - 0.5d)));

            // --- 2. Boundary contribution. Each kind of margin makes its own landform, at a width
            // that suits it: collisions raise broad interior ranges, subduction raises a narrow
            // coastal range with a trench on the oceanic side, arcs are narrow and offshore, rifts
            // cut down rather than up.
            double distance = plate.BoundaryDistance;
            double boundaryEffect = 0d;
            switch (plate.Boundary)
            {
                case BoundaryKind.ContinentalCollision:
                    boundaryEffect = 0.62d * plate.Intensity * Falloff(distance, 0.26d);
                    break;
                case BoundaryKind.Subduction:
                    boundaryEffect = plate.OnOceanicSide
                        ? -0.42d * plate.Intensity * Falloff(distance, 0.07d)
                        : 0.54d * plate.Intensity * Falloff(distance, 0.13d);
                    break;
                case BoundaryKind.IslandArc:
                    boundaryEffect = 0.50d * plate.Intensity * Falloff(distance, 0.05d);
                    break;
                case BoundaryKind.Divergent:
                    boundaryEffect = -0.24d * plate.Intensity * Falloff(distance, 0.09d);
                    break;
                case BoundaryKind.Transform:
                    boundaryEffect = 0.05d * plate.Intensity * Falloff(distance, 0.04d);
                    break;
            }

            // --- 3. Relief.
            //
            // The previous version multiplied ALL ridged detail by the boundary contribution, so a
            // plate interior - where that contribution is zero by design - had exactly zero relief.
            // Continental interiors rendered as perfectly flat plateaus and the only raised ground
            // anywhere was a line along a plate edge. Relief has to have two independent parts:
            // ground that is never flat, and ranges that occur where plates meet.
            int mountainOctaves = OctavesUnder(MountainFrequency, maximumFrequency, 5);
            double ridges = EnvironmentNoise.RidgedFbm(
                seed, channel: 240,
                dx * MountainFrequency, dy * MountainFrequency, dz * MountainFrequency,
                mountainOctaves, Lacunarity, Gain, ridgeWeighting: 1.6d);

            // Rolling ground, applied across all land regardless of tectonics. This is what hills,
            // valleys and undulating plains are: terrain that exists because the surface is not a
            // plane, not because two plates met.
            int hillOctaves = OctavesUnder(HillFrequency, maximumFrequency, 4);
            double hills = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    seed, channel: 300,
                    dx * HillFrequency, dy * HillFrequency, dz * HillFrequency,
                    hillOctaves, Lacunarity, Gain, warpStrength: 0.25d),
                strength: 1.8d);

            int detailOctaves = OctavesUnder(DetailFrequency, maximumFrequency, 3);
            double detail = EnvironmentNoise.Fbm(
                seed, channel: 320,
                dx * DetailFrequency, dy * DetailFrequency, dz * DetailFrequency,
                detailOctaves, Lacunarity, Gain);

            double boundaryRelief = Math.Max(0d, boundaryEffect);

            // Land gets hills everywhere and ranges near boundaries; the sea floor gets a gentler
            // version of the same, so it is not a mirror-flat basin either.
            double aboveWater = EnvironmentNoise.Clamp01((continent - SeaLevel) / (1d - SeaLevel));
            double hillAmplitude = 0.10d + (0.16d * aboveWater);

            double elevation = continent
                + boundaryEffect
                + (hillAmplitude * (hills - 0.5d))
                + (0.34d * ridges * boundaryRelief)
                + (0.05d * (detail - 0.5d));

            elevation = EnvironmentNoise.Clamp01(elevation);

            // --- Climate. Latitude is the structure; noise only perturbs the bands so they do not
            // read as stripes. dy is sin(latitude) for a unit direction.
            // cos(latitude), not 1 - |sin(latitude)|. dy is sin(latitude) for a unit direction, so
            // the old term fell linearly in sin and was down to 0.29 by 45 degrees - measured
            // temperature median 0.310, below the tundra threshold, and 79% of all land rendered as
            // ice. Insolation goes as cos(latitude), which is 0.707 at 45 degrees.
            double latitudeTerm = Math.Sqrt(Math.Max(0d, 1d - (dy * dy)));
            double climateNoise = EnvironmentNoise.Fbm(
                seed, channel: 360,
                dx * ClimateNoiseFrequency, dy * ClimateNoiseFrequency, dz * ClimateNoiseFrequency,
                OctavesUnder(ClimateNoiseFrequency, maximumFrequency, 3), Lacunarity, Gain);
            double temperature = (0.78d * latitudeTerm) + (0.22d * climateNoise);

            // Lapse rate: height costs warmth, applied only above the waterline.
            double land01 = elevation <= SeaLevel ? 0d : (elevation - SeaLevel) / (1d - SeaLevel);
            // 0.55 put every range past the snow threshold, so the only raised ground on the
            // planet also rendered white. High ground should read colder than the valley beside it,
            // not become an ice cap the moment it rises.
            // Measured: with cos(latitude) the temperature median is 0.625, yet 38% of land still
            // rendered as ice - because the lapse rate cools land specifically, and land is where
            // biomes are. 0.32 dropped high ground by 0.26. High ground should be colder than the
            // valley, not glaciated.
            temperature = EnvironmentNoise.Clamp01(temperature - (0.18d * land01));

            // --- Moisture. Wet near the ocean and dry in continental interiors, which is what puts
            // deserts inland instead of scattering them, plus a warped band for regional variety.
            // Contrast expansion, which was missing everywhere. Raw fBm spans about .37-.82 rather
            // than 0-1, so moisture measured 0.476 to 0.889 and could never reach the desert
            // threshold of 0.34 - deserts were unreachable by construction, not by tuning.
            double moistureNoise = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    seed, channel: 400,
                    dx * MoistureFrequency, dy * MoistureFrequency, dz * MoistureFrequency,
                    OctavesUnder(MoistureFrequency, maximumFrequency, 4), Lacunarity, Gain, warpStrength: 0.4d),
                strength: 3.0d);

            // Continental interiors dry out. Widened from 0.65 to 0.9 so high inland ground can
            // genuinely reach arid rather than merely damp.
            double continentality = 1d - (0.9d * EnvironmentNoise.Clamp01((continent - SeaLevel) / (1d - SeaLevel)));
            double moisture = EnvironmentNoise.Clamp01((0.62d * moistureNoise) + (0.38d * continentality));

            return new PlanetSample(
                (float)elevation,
                (float)moisture,
                (float)temperature,
                (float)continent);
        }

        /// <summary>Sample from a latitude and longitude in radians.</summary>
        public static PlanetSample SampleAtLatLon(int seed, PlateStructure plates, double latitude, double longitude, double maximumFrequency)
        {
            double cosLatitude = Math.Cos(latitude);
            return Sample(
                seed,
                plates,
                cosLatitude * Math.Sin(longitude),
                Math.Sin(latitude),
                cosLatitude * Math.Cos(longitude),
                maximumFrequency);
        }

        /// <summary>
        /// Influence of a boundary at an angular distance from it. Exponential rather than linear so
        /// a range has a crest and shoulders rather than a triangular profile, and so a distant plate
        /// interior is genuinely unaffected instead of faintly tilted.
        /// </summary>
        private static double Falloff(double distance, double width)
        {
            if (width <= 0d) return 0d;
            double t = distance / width;
            return Math.Exp(-t * t);
        }

    }
}
