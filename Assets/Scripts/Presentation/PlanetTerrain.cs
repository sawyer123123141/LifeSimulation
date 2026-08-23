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

        /// <summary>Frequency and amplitude of the domain warp applied to the plate lookup.</summary>
        private const double WarpFrequency = 2.1d;
        private const double WarpStrength = 0.32d;
        private const double MountainFrequency = 3.6d;

        /// <summary>
        /// Rolling ground. Sits between the mountain and detail bands, and applies to all land
        /// rather than only near plate boundaries - without it, plate interiors are flat plateaus.
        /// </summary>
        private const double HillFrequency = 6.5d;
        private const double DetailFrequency = 11d;
        private const double MoistureFrequency = 1.9d;
        private const double ClimateNoiseFrequency = 2.4d;

        /// <summary>
        /// High-frequency perturbation applied to climate for biome-edge raggedness only. Above the
        /// biome scale, below the detail scale.
        /// </summary>
        private const double JitterFrequency = 16d;

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
            // Domain warp the plate lookup. Perturbing the land/sea THRESHOLD, as before, cannot move
            // a boundary: the plate step is about 0.4 and the perturbation was 0.07, so coastlines
            // traced the Voronoi edge exactly - a straight line with a stair-stepped edge. Warping
            // the sample POSITION moves the boundary itself, which is the standard way to turn a
            // polygonal cell edge into a natural coastline.
            double warpX = (EnvironmentNoise.ValueNoise(seed, 500, dx * WarpFrequency, dy * WarpFrequency, dz * WarpFrequency) - 0.5d) * WarpStrength;
            double warpY = (EnvironmentNoise.ValueNoise(seed, 501, dx * WarpFrequency, dy * WarpFrequency, dz * WarpFrequency) - 0.5d) * WarpStrength;
            double warpZ = (EnvironmentNoise.ValueNoise(seed, 502, dx * WarpFrequency, dy * WarpFrequency, dz * WarpFrequency) - 0.5d) * WarpStrength;

            // A second, finer warp gives the coast detail at bay-and-headland scale rather than only
            // large lobes.
            const double fine = WarpFrequency * 3.7d;
            warpX += (EnvironmentNoise.ValueNoise(seed, 503, dx * fine, dy * fine, dz * fine) - 0.5d) * WarpStrength * 0.35d;
            warpY += (EnvironmentNoise.ValueNoise(seed, 504, dx * fine, dy * fine, dz * fine) - 0.5d) * WarpStrength * 0.35d;
            warpZ += (EnvironmentNoise.ValueNoise(seed, 505, dx * fine, dy * fine, dz * fine) - 0.5d) * WarpStrength * 0.35d;

            PlateSample plate = plates.Sample(dx + warpX, dy + warpY, dz + warpZ);

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
            // that suits it. Widths are roughly three times the height for a reason: a lift of 0.54
            // over 0.13 radians is 56 units of rise across 65 units of ground, which is a 40 degree
            // average slope and much steeper at the inflection - a wall rather than a range, and flat
            // shading on a wall renders as vertical stripes.
            //
            // Original widths: collisions raise broad interior ranges, subduction raises a narrow
            // coastal range with a trench on the oceanic side, arcs are narrow and offshore, rifts
            // cut down rather than up.
            double distance = plate.BoundaryDistance;
            double boundaryEffect = 0d;
            switch (plate.Boundary)
            {
                case BoundaryKind.ContinentalCollision:
                    boundaryEffect = 0.62d * plate.Intensity * Falloff(distance, 0.40d);
                    break;
                case BoundaryKind.Subduction:
                    boundaryEffect = plate.OnOceanicSide
                        ? -0.42d * plate.Intensity * Falloff(distance, 0.16d)
                        : 0.54d * plate.Intensity * Falloff(distance, 0.28d);
                    break;
                case BoundaryKind.IslandArc:
                    boundaryEffect = 0.50d * plate.Intensity * Falloff(distance, 0.12d);
                    break;
                case BoundaryKind.Divergent:
                    boundaryEffect = -0.24d * plate.Intensity * Falloff(distance, 0.18d);
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
            // Capped at 3, not 5. The fifth octave sits at 3.6 * 2^4 = 57.6 cycles per radian, an
            // 8.7-unit wavelength, and the ridged term applies its full amplitude at every octave -
            // so it produced ~17 units of height change across ~4 units of ground, a 76 degree face.
            // A heightfield sampled every 2.5 units renders that as a staircase of alternating
            // near-vertical and near-horizontal facets: the striped comb on every steep slope. The
            // limit that matters is not what the mesh can resolve, it is what SLOPE it can represent.
            int mountainOctaves = OctavesUnder(MountainFrequency, maximumFrequency, 3);
            double ridges = EnvironmentNoise.RidgedFbm(
                seed, channel: 240,
                dx * MountainFrequency, dy * MountainFrequency, dz * MountainFrequency,
                mountainOctaves, Lacunarity, Gain, ridgeWeighting: 1.6d);

            // Rolling ground, applied across all land regardless of tectonics. This is what hills,
            // valleys and undulating plains are: terrain that exists because the surface is not a
            // plane, not because two plates met.
            int hillOctaves = OctavesUnder(HillFrequency, maximumFrequency, 3);
            double hills = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    seed, channel: 300,
                    dx * HillFrequency, dy * HillFrequency, dz * HillFrequency,
                    hillOctaves, Lacunarity, Gain, warpStrength: 0.25d),
                strength: 1.8d);

            int detailOctaves = OctavesUnder(DetailFrequency, maximumFrequency, 2);
            double detail = EnvironmentNoise.Fbm(
                seed, channel: 320,
                dx * DetailFrequency, dy * DetailFrequency, dz * DetailFrequency,
                detailOctaves, Lacunarity, Gain);

            double boundaryRelief = Math.Max(0d, boundaryEffect);

            // Land gets hills everywhere and ranges near boundaries; the sea floor gets a gentler
            // version of the same, so it is not a mirror-flat basin either.
            double aboveWater = EnvironmentNoise.Clamp01((continent - SeaLevel) / (1d - SeaLevel));
            // Rendered at 200 units the old amplitudes gave about 2.8 units of relief - 1.4% of the
            // view width, which reads as a flat plane. Real ground over 200 m carries tens of metres.
            double hillAmplitude = 0.18d + (0.26d * aboveWater);

            double elevation = continent
                + boundaryEffect
                + (hillAmplitude * (hills - 0.5d))
                + (0.22d * ridges * boundaryRelief)
                + (0.07d * (detail - 0.5d));

            elevation = SoftSaturate(elevation);

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
                strength: 2.2d);

            // Continental interiors dry out. Widened from 0.65 to 0.9 so high inland ground can
            // genuinely reach arid rather than merely damp.
            double continentality = 1d - (0.9d * EnvironmentNoise.Clamp01((continent - SeaLevel) / (1d - SeaLevel)));
            // Boundary jitter. Without it, every biome edge is a smooth level set of a low-frequency
            // field - a clean arc, which reads as drawn rather than grown. A small high-frequency
            // term makes edges ragged at a scale below the biomes themselves, which is what natural
            // boundaries look like. Amplitude is deliberately small: this perturbs where a boundary
            // falls, it does not create regions of its own.
            double jitter = EnvironmentNoise.Fbm(
                seed, channel: 440,
                dx * JitterFrequency, dy * JitterFrequency, dz * JitterFrequency,
                OctavesUnder(JitterFrequency, maximumFrequency, 2), Lacunarity, Gain) - 0.5d;

            double moisture = EnvironmentNoise.Clamp01(
                (0.62d * moistureNoise) + (0.38d * continentality) + (0.07d * jitter));
            temperature = EnvironmentNoise.Clamp01(temperature + (0.05d * jitter));

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
        /// Compresses toward 0 and 1 instead of clipping at them.
        ///
        /// <para><b>Why not Clamp01.</b> Clamping maps every value above 1 to exactly 1, so wherever
        /// the composed elevation overshoots - which it does once amplitudes are large enough for
        /// relief to be visible - the result is a perfectly flat plateau with a vertical cliff at its
        /// edge. Measured at 0.81% of the surface at exactly 1.000 and 1.09% at exactly 0, and
        /// because it lands on the highest ground it is far more visible than that fraction suggests:
        /// this is the mesa-and-cliff banding that kept being reported.</para>
        ///
        /// <para>Above the knee the excess decays exponentially, so the curve approaches 1 without
        /// ever reaching it and its slope is continuous at the join - peaks round off instead of
        /// being sliced.</para>
        /// </summary>
        private static double SoftSaturate(double value)
        {
            const double upperKnee = 0.94d;
            const double lowerKnee = 0.06d;

            if (value > upperKnee)
            {
                double headroom = 1d - upperKnee;
                return upperKnee + (headroom * (1d - Math.Exp(-(value - upperKnee) / headroom)));
            }

            if (value < lowerKnee)
            {
                return lowerKnee * Math.Exp((value - lowerKnee) / lowerKnee);
            }

            return value;
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
