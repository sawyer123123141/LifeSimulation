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
                    boundaryEffect = 0.42d * plate.Intensity * Falloff(distance, 0.26d);
                    break;
                case BoundaryKind.Subduction:
                    boundaryEffect = plate.OnOceanicSide
                        ? -0.30d * plate.Intensity * Falloff(distance, 0.07d)
                        : 0.36d * plate.Intensity * Falloff(distance, 0.13d);
                    break;
                case BoundaryKind.IslandArc:
                    boundaryEffect = 0.34d * plate.Intensity * Falloff(distance, 0.05d);
                    break;
                case BoundaryKind.Divergent:
                    boundaryEffect = -0.16d * plate.Intensity * Falloff(distance, 0.09d);
                    break;
                case BoundaryKind.Transform:
                    boundaryEffect = 0.05d * plate.Intensity * Falloff(distance, 0.04d);
                    break;
            }

            // --- 3. Ridged detail, concentrated where the boundary is already raising ground, so
            // foothills gather around ranges instead of scattering across plains.
            int mountainOctaves = OctavesUnder(MountainFrequency, maximumFrequency, 5);
            double ridges = EnvironmentNoise.RidgedFbm(
                seed, channel: 240,
                dx * MountainFrequency, dy * MountainFrequency, dz * MountainFrequency,
                mountainOctaves, Lacunarity, Gain, ridgeWeighting: 1.6d);

            int detailOctaves = OctavesUnder(DetailFrequency, maximumFrequency, 3);
            double detail = EnvironmentNoise.Fbm(
                seed, channel: 320,
                dx * DetailFrequency, dy * DetailFrequency, dz * DetailFrequency,
                detailOctaves, Lacunarity, Gain);

            double relief = Math.Max(0d, boundaryEffect);
            double elevation = continent
                + boundaryEffect
                + (0.30d * ridges * relief)
                + (0.045d * (detail - 0.5d));

            elevation = EnvironmentNoise.Clamp01(elevation);

            // --- Climate. Latitude is the structure; noise only perturbs the bands so they do not
            // read as stripes. dy is sin(latitude) for a unit direction.
            double latitudeTerm = 1d - Math.Abs(dy);
            double climateNoise = EnvironmentNoise.Fbm(
                seed, channel: 360,
                dx * ClimateNoiseFrequency, dy * ClimateNoiseFrequency, dz * ClimateNoiseFrequency,
                OctavesUnder(ClimateNoiseFrequency, maximumFrequency, 3), Lacunarity, Gain);
            double temperature = (0.78d * latitudeTerm) + (0.22d * climateNoise);

            // Lapse rate: height costs warmth, applied only above the waterline.
            double land01 = elevation <= SeaLevel ? 0d : (elevation - SeaLevel) / (1d - SeaLevel);
            temperature = EnvironmentNoise.Clamp01(temperature - (0.55d * land01));

            // --- Moisture. Wet near the ocean and dry in continental interiors, which is what puts
            // deserts inland instead of scattering them, plus a warped band for regional variety.
            double moistureNoise = EnvironmentNoise.WarpedFbm(
                seed, channel: 400,
                dx * MoistureFrequency, dy * MoistureFrequency, dz * MoistureFrequency,
                OctavesUnder(MoistureFrequency, maximumFrequency, 4), Lacunarity, Gain, warpStrength: 0.4d);
            double continentality = 1d - (0.65d * EnvironmentNoise.Clamp01((continent - SeaLevel) / (1d - SeaLevel)));
            double moisture = EnvironmentNoise.Clamp01((0.55d * moistureNoise) + (0.45d * continentality));

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
