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
        public static PlanetSample Sample(int seed, double dx, double dy, double dz, double maximumFrequency)
        {
            // --- 1. Continents. Heavily warped so coastlines are lobed and irregular rather than
            // circular blobs. This band alone decides land from ocean.
            int continentOctaves = OctavesUnder(ContinentFrequency, maximumFrequency, 4);
            double continentNoise = EnvironmentNoise.WarpedFbm(
                seed, channel: 200,
                dx * ContinentFrequency, dy * ContinentFrequency, dz * ContinentFrequency,
                continentOctaves, Lacunarity, Gain, warpStrength: 0.55d);

            // Land covers roughly a third of the surface, as on Earth. SmoothStep rather than a hard
            // cut so the coast has a shelf instead of a cliff.
            double continent = SmoothStep(0.46d, 0.62d, continentNoise);

            // --- 2. Mountain belts. Ridged, so peaks form connected chains. Multiplied by a belt
            // mask - itself low frequency - so ranges occupy part of a continent rather than
            // covering all of it, which is what makes plains exist.
            int mountainOctaves = OctavesUnder(MountainFrequency, maximumFrequency, 5);
            double ridges = EnvironmentNoise.RidgedFbm(
                seed, channel: 240,
                dx * MountainFrequency, dy * MountainFrequency, dz * MountainFrequency,
                mountainOctaves, Lacunarity, Gain, ridgeWeighting: 1.6d);

            double beltMask = EnvironmentNoise.Fbm(
                seed, channel: 280,
                dx * 1.6d, dy * 1.6d, dz * 1.6d,
                OctavesUnder(1.6d, maximumFrequency, 2), Lacunarity, Gain);
            beltMask = SmoothStep(0.42d, 0.78d, beltMask);

            // --- 3. Local relief. Small amplitude: texture, not shape.
            int detailOctaves = OctavesUnder(DetailFrequency, maximumFrequency, 3);
            double detail = detailOctaves <= 0
                ? 0.5d
                : EnvironmentNoise.Fbm(
                    seed, channel: 320,
                    dx * DetailFrequency, dy * DetailFrequency, dz * DetailFrequency,
                    detailOctaves, Lacunarity, Gain);

            // --- Combine multiplicatively. Ocean depth follows the continent mask so there are
            // shelves and trenches rather than a flat floor; land rises from the coast, with ranges
            // where the belt mask allows and gentle ground where it does not.
            double elevation;
            if (continent <= 0d)
            {
                double oceanFloor = 0.06d + (0.24d * continentNoise);
                elevation = oceanFloor;
            }
            else
            {
                double baseLand = SeaLevel + (0.06d * continent);
                double relief = (0.46d * ridges * beltMask) + (0.10d * (detail - 0.5d));
                elevation = baseLand + (continent * relief);
            }

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
            double continentality = 1d - (0.65d * continent);
            double moisture = EnvironmentNoise.Clamp01((0.55d * moistureNoise) + (0.45d * continentality));

            return new PlanetSample(
                (float)elevation,
                (float)moisture,
                (float)temperature,
                (float)continent);
        }

        /// <summary>Sample from a latitude and longitude in radians.</summary>
        public static PlanetSample SampleAtLatLon(int seed, double latitude, double longitude, double maximumFrequency)
        {
            double cosLatitude = Math.Cos(latitude);
            return Sample(
                seed,
                cosLatitude * Math.Sin(longitude),
                Math.Sin(latitude),
                cosLatitude * Math.Cos(longitude),
                maximumFrequency);
        }

        private static double SmoothStep(double edge0, double edge1, double value)
        {
            if (edge1 <= edge0) return value < edge0 ? 0d : 1d;
            double t = EnvironmentNoise.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3d - (2d * t));
        }
    }
}
