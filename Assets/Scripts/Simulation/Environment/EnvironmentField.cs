using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    public readonly struct EnvironmentSample
    {
        public EnvironmentSample(float moisture, float fertility, float temperature, float elevation = 0f)
        {
            Moisture = moisture;
            Fertility = fertility;
            Temperature = temperature;
            Elevation = elevation;
        }

        public float Moisture { get; }
        public float Fertility { get; }
        public float Temperature { get; }

        /// <summary>
        /// Height above the arena floor, 0..1, and zero unless
        /// <c>SimulationConfig.ElevationFieldEnabled</c> is set. Unlike the other three channels
        /// nothing reads this directly for growth: it reaches the world through the lapse rate in
        /// <see cref="EnvironmentField"/>. That is deliberate - a fourth channel that plants had to
        /// adapt to would ship as another tax on a genome that already cannot pay
        /// (docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md).
        /// </summary>
        public float Elevation { get; }
    }

    public sealed class EnvironmentField
    {
        /// <summary>
        /// Sphere radius the arena is treated as a patch of. Large relative to the 50-unit arena, so
        /// curvature is negligible today; shrinking it at P7 turns the same field functions into a
        /// planet without touching them.
        /// </summary>
        public const double SphereRadius = 500d;

        /// <summary>Roughly how many noise features span the arena. Three reads as distinct regions at a zoom where a creature is one unit.</summary>
        private const double FeaturesAcrossArena = 3d;

        private const double ArenaSize = 50d;

        /// <summary>Noise-space scale on the unit sphere, derived so feature size is independent of <see cref="SphereRadius"/>.</summary>
        private const double NoiseScale = FeaturesAcrossArena * SphereRadius / ArenaSize;

        private readonly EnvironmentSample _constantSample;
        private readonly bool _usesMoistureGradient;
        private readonly bool _usesProceduralFields;
        private readonly bool _usesElevation;
        private readonly int _worldSeed;

        /// <summary>
        /// How much of the temperature range the full height of the terrain removes. 0.45 is chosen
        /// so a ridge crest is decisively colder than the valley beside it without driving
        /// temperature to zero, which would stop growth outright rather than merely limiting it.
        /// </summary>
        private const double LapseRate = .45d;

        public EnvironmentField(float moisture = 1f, float fertility = 1f, float temperature = 1f)
        {
            _constantSample = new EnvironmentSample(moisture, fertility, temperature);
        }

        private EnvironmentField(bool usesMoistureGradient)
        {
            _constantSample = new EnvironmentSample(1f, 1f, 1f);
            _usesMoistureGradient = usesMoistureGradient;
        }

        private EnvironmentField(int worldSeed, bool procedural, bool elevation)
        {
            _constantSample = new EnvironmentSample(1f, 1f, 1f);
            _usesProceduralFields = procedural;
            _usesElevation = elevation;
            _worldSeed = worldSeed;
        }

        public static EnvironmentField CreateMoistureGradient() { return new EnvironmentField(true); }

        /// <summary>
        /// Procedural moisture, fertility and temperature, sampled on a sphere. Deterministic in
        /// <paramref name="worldSeed"/> and position only.
        /// </summary>
        public static EnvironmentField CreateProcedural(int worldSeed, bool elevationEnabled = false)
        {
            return new EnvironmentField(worldSeed, true, elevationEnabled);
        }

        /// <summary>
        /// Map an arena position onto a point on the unit sphere, then scale into noise space.
        /// Small-angle patch mapping: the arena is a lat/lon window near the equator, so noise is
        /// evaluated in 3D on the surface and is seamless everywhere including the poles.
        /// </summary>
        private void SpherePoint(SimVector2 position, out double x, out double y, out double z)
        {
            double longitude = position.X / SphereRadius;
            double latitude = position.Y / SphereRadius;
            double cosLatitude = Math.Cos(latitude);
            x = cosLatitude * Math.Sin(longitude) * NoiseScale;
            y = Math.Sin(latitude) * NoiseScale;
            z = cosLatitude * Math.Cos(longitude) * NoiseScale;
        }

        public EnvironmentSample Sample(SimVector2 position)
        {
            if (_usesProceduralFields) return SampleProcedural(position);
            if (!_usesMoistureGradient) return _constantSample;
            float moisture = .25f + (.75f * Clamp01((position.X + 25f) / 50f));
            return new EnvironmentSample(moisture, 1f, 1f);
        }

        private EnvironmentSample SampleProcedural(SimVector2 position)
        {
            SpherePoint(position, out double x, out double y, out double z);

            // Moisture: domain-warped fBm. The warp is what produces lobed wet and dry regions with
            // recognisable boundaries instead of the smooth ramp this replaced. Remapped to
            // .15..1.0 so dry ground is genuinely limiting without being lethal - moisture 0 stops
            // growth outright in PlantGrowthSystem.
            double moistureNoise = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    _worldSeed, channel: 0, x, y, z,
                    octaves: 4, lacunarity: 2d, gain: .5d, warpStrength: .35d),
                strength: 2.4d);
            double moisture = .15d + (.85d * moistureNoise);

            // Temperature: latitude band plus a noise perturbation. Latitude is the natural structure
            // on a sphere and survives to P7 unchanged; the perturbation keeps bands from reading as
            // stripes. Weighted .7 structural / .3 noise so the gradient stays legible.
            // latitudeTerm is already 0..1 - warm at the equator, cold toward the band edges. It is
            // used directly; an earlier version remapped it with (t + 1) * .5 and squashed the whole
            // field into .6..1, which is exactly the flat-environment problem this work exists to fix.
            double latitudeTerm = EnvironmentNoise.Clamp01(
                1d - Math.Abs(Math.Sin(position.Y / SphereRadius) * (SphereRadius / ArenaSize) * 2d));
            double temperatureNoise = EnvironmentNoise.Fbm(
                _worldSeed, channel: 32, x, y, z,
                octaves: 3, lacunarity: 2.1d, gain: .45d);
            double temperature = .20d + (.80d * EnvironmentNoise.Clamp01((.7d * latitudeTerm) + (.3d * temperatureNoise)));

            // Fertility: independent noise, then penalised at both moisture extremes. Waterlogged
            // ground and arid ground are both poor soil, so fertility ridges at moderate moisture.
            // This is a deliberate ecological choice rather than a third independent layer - it makes
            // biomes feel causally connected instead of like stacked noise, and it means the best
            // ground is contested rather than uniformly good.
            double fertilityNoise = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    _worldSeed, channel: 96, x, y, z,
                    octaves: 3, lacunarity: 2d, gain: .5d, warpStrength: .2d),
                strength: 2.0d);
            double moistureBalance = 1d - EnvironmentNoise.Clamp01(Math.Abs(moisture - .55d) * 1.8d);
            double fertility = .20d + (.80d * EnvironmentNoise.Clamp01(fertilityNoise * (.35d + (.65d * moistureBalance))));

            // Elevation: ridged multifractal, so the terrain reads as connected chains rather than
            // scattered hills. It reaches the world only through the lapse rate below - deliberately
            // no growth channel of its own, because a fourth channel plants had to adapt to would be
            // another unpayable tax on genes that already cannot pay
            // (docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md).
            //
            // Flag-off must be byte-identical, so nothing above this point may read elevation and
            // temperature is only rewritten inside the branch.
            double elevation = 0d;
            if (_usesElevation)
            {
                elevation = EnvironmentNoise.RidgedFbm(
                    _worldSeed, channel: 160, x, y, z,
                    octaves: 5, lacunarity: 2.15d, gain: .5d, ridgeWeighting: 1.8d);

                // Lapse rate: height costs warmth. This is the whole reason elevation exists as a
                // simulation quantity rather than a rendering one - it makes high ground a real
                // ecological gradient instead of decoration, using the channel that already limits
                // growth. Floored at .02 rather than 0: temperature 0 stops growth outright, and a
                // dead crest is less interesting than a cold one.
                temperature = Math.Max(.02d, temperature - (LapseRate * elevation));
            }

            return new EnvironmentSample((float)moisture, (float)fertility, (float)temperature, (float)elevation);
        }

        private static float Clamp01(float value) { return value < 0f ? 0f : value > 1f ? 1f : value; }
    }
}
