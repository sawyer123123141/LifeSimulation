using System;
using LifeSimulation.Simulation.World;
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
        private readonly bool _usesPlanetScaleClimate;
        private readonly int _worldSeed;

        // Terrain-driven mode. Built once, because a plate structure is a fixed cost and the coastal
        // centre is where the arena window sits - both are deterministic in the world seed alone.
        private readonly bool _usesTerrain;
        private readonly TerrainSettings _terrainSettings;
        private readonly PlateStructure _plates;
        private readonly RiverNetwork _rivers;
        private readonly double _terrainCentreLatitude;
        private readonly double _terrainCentreLongitude;

        /// <summary>Samples per side of the arena mesh - matches <c>TerrainMeshBuilder.PatchResolution</c>.</summary>
        private const int TerrainSampleResolution = 193;

        /// <summary>Half-width of the arena window - matches <c>Prototype1Presenter.TerrainHalfWidth</c>.</summary>
        private const double TerrainWindowHalfWidth = 25d;

        /// <summary>
        /// Finest terrain detail the simulation reads, derived exactly as
        /// <c>TerrainMeshBuilder.BuildPatch</c> derives it for the same window.
        ///
        /// <para>It has to match, or the simulation reads a sharper or blunter field than the one on
        /// screen and the join is a lie in the small: a creature would climb a bump nobody drew, or
        /// walk through one they can see.</para>
        /// </summary>
        private static readonly double TerrainMaximumFrequency = PlanetTerrain.MaximumFrequencyFor(
            (int)(TerrainSampleResolution * (2d * Math.PI) / (2d * TerrainWindowHalfWidth / SphereRadius)));

        /// <summary>
        /// How much of the temperature range the full height of the terrain removes. 0.45 is chosen
        /// so a ridge crest is decisively colder than the valley beside it without driving
        /// temperature to zero, which would stop growth outright rather than merely limiting it.
        /// </summary>
        private const double LapseRate = .45d;

        /// <summary>
        /// How far the local band may move moisture and temperature away from the regional value,
        /// in field units before the .15/.20 remap.
        ///
        /// <para>Chosen to restore the within-arena spread the procedural field had - roughly .24
        /// in moisture and .19 in temperature over the arena - so the plant systems see a landscape
        /// with the same amount of structure to select on, and any measured difference is the
        /// <b>shape</b> of that structure rather than its amount. Raising these does not make the
        /// world more varied so much as make the regional signal irrelevant.</para>
        /// </summary>
        private const double LocalMoistureStrength = .40d;
        private const double LocalTemperatureStrength = .32d;

        /// <summary>
        /// Local variation around a regional value: warped fBm, centred on zero and spanning
        /// -1 to +1, sampled at the same scale as the procedural field's own noise.
        /// </summary>
        private double LocalBand(int channel, double x, double y, double z)
        {
            double noise = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    _worldSeed, channel, x, y, z,
                    octaves: 4, lacunarity: 2d, gain: .5d, warpStrength: .35d),
                strength: 2.4d);
            return (2d * noise) - 1d;
        }

        public EnvironmentField(float moisture = 1f, float fertility = 1f, float temperature = 1f)
        {
            _constantSample = new EnvironmentSample(moisture, fertility, temperature);
        }

        private EnvironmentField(bool usesMoistureGradient)
        {
            _constantSample = new EnvironmentSample(1f, 1f, 1f);
            _usesMoistureGradient = usesMoistureGradient;
        }

        private EnvironmentField(int worldSeed, bool procedural, bool elevation, bool planetScaleClimate = false)
        {
            _constantSample = new EnvironmentSample(1f, 1f, 1f);
            _usesProceduralFields = procedural;
            _usesElevation = elevation;
            _usesPlanetScaleClimate = planetScaleClimate;
            _worldSeed = worldSeed;
        }

        private EnvironmentField(int worldSeed, TerrainSettings terrainSettings)
        {
            _constantSample = new EnvironmentSample(1f, 1f, 1f);
            _usesProceduralFields = true;
            _usesElevation = true;
            _usesTerrain = true;
            _worldSeed = worldSeed;
            _terrainSettings = terrainSettings;
            _plates = PlateStructure.Create(worldSeed, terrainSettings);
            _rivers = RiverNetwork.Create(worldSeed, _plates, terrainSettings);
            _plates.GetCoastalCentre(out _terrainCentreLatitude, out _terrainCentreLongitude);
        }

        /// <summary>
        /// The rivers of this world, or null when this field is not terrain-driven.
        ///
        /// <para>Exposed because the join is only worth anything if the ground simulated and the
        /// ground drawn are the same ground, and a river carves the ground. Anything checking that
        /// equivalence has to sample the generator with <b>this</b> network, not a fresh one - and a
        /// fresh one would in fact be identical, which is exactly why the check has to be handed the
        /// real one rather than left to reconstruct something that happens to agree.</para>
        /// </summary>
        public RiverNetwork Rivers { get { return _rivers; } }

        public static EnvironmentField CreateMoistureGradient() { return new EnvironmentField(true); }

        /// <summary>
        /// The terrain settings the <b>simulation</b> generates with.
        ///
        /// <para>The shipped defaults, and not the tuning panel's instance: the panel is a live
        /// control over what is drawn, and a live control over what is simulated would be behaviour
        /// outside <c>SimulationConfig</c> - invisible to the configuration hash, so two worlds with
        /// equal hashes could diverge. Making terrain tunable per world means putting these values
        /// into the config and hashing them, which is a deliberate later step.</para>
        /// </summary>
        public static TerrainSettings CreateTerrainSettings()
        {
            return new TerrainSettings();
        }

        /// <summary>
        /// Moisture, temperature and elevation taken from the <b>terrain generator</b> - the same
        /// function, seed, window and detail limit the arena mesh is built from.
        ///
        /// <para><b>This is the join.</b> Until now the ground a creature was drawn standing on and
        /// the ground the simulation read were two unrelated fields, so a hill cost a creature
        /// nothing. Here a ridge is genuinely colder, a rain shadow is genuinely drier, and the coast
        /// in the picture is the coast in the model.</para>
        ///
        /// <para>Output ranges are deliberately the same as the procedural field's - moisture
        /// .15 to 1, temperature .20 to 1 before lapse, fertility .20 to 1 - so plant systems see
        /// magnitudes they were calibrated against and any measured difference is the shape of the
        /// field changing rather than its scale.</para>
        /// </summary>
        public static EnvironmentField CreateTerrainDriven(int worldSeed)
        {
            return new EnvironmentField(worldSeed, CreateTerrainSettings());
        }

        /// <summary>
        /// Procedural moisture, fertility and temperature, sampled on a sphere. Deterministic in
        /// <paramref name="worldSeed"/> and position only.
        /// </summary>
        public static EnvironmentField CreateProcedural(int worldSeed, bool elevationEnabled = false)
        {
            return new EnvironmentField(worldSeed, true, elevationEnabled);
        }

        /// <summary>
        /// The same fields, but with a climate defined over the <b>whole sphere</b> rather than over
        /// the arena window.
        ///
        /// <para>The standard latitude term is
        /// <c>1 - |sin(lat) * (SphereRadius / ArenaSize) * 2|</c>, which for the small angles the
        /// arena occupies is <c>1 - |y| / 25</c>: temperature reaches zero exactly at the arena edge.
        /// That is deliberate and correct for a 50-unit world, and completely wrong past it - every
        /// position beyond the arena is frozen, so a wide view shows an equatorial strip of habitable
        /// ground and ice everywhere else. Here the term is <c>cos(latitude)</c>, warm at the equator
        /// and cold at the poles, which is the same shape defined over the actual sphere.</para>
        ///
        /// <para><b>Nothing in the simulation uses this yet.</b> It exists so the terrain viewer can
        /// show what a planet-scale climate looks like before anything commits to one. Adopting it
        /// for simulation is a behaviour change and needs a flag, tests and a re-measure of every
        /// procedural-field result; this factory alone changes nothing, because no default path
        /// reaches it.</para>
        /// </summary>
        public static EnvironmentField CreatePlanetScaleClimate(int worldSeed, bool elevationEnabled = true)
        {
            return new EnvironmentField(worldSeed, true, elevationEnabled, planetScaleClimate: true);
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
            if (_usesTerrain) return SampleTerrain(position);
            if (_usesProceduralFields) return SampleProcedural(position);
            if (!_usesMoistureGradient) return _constantSample;
            float moisture = .25f + (.75f * Clamp01((position.X + 25f) / 50f));
            return new EnvironmentSample(moisture, 1f, 1f);
        }

        /// <summary>
        /// The arena as a window on the planet, at the coastline the renderer centres on.
        ///
        /// <para>Same centre, same seed, same detail limit as the mesh - so the elevation here is the
        /// elevation drawn, to the last decimal.</para>
        /// </summary>
        private EnvironmentSample SampleTerrain(SimVector2 position)
        {
            PlanetSample terrain = PlanetTerrain.SampleAtLatLon(
                _worldSeed, _plates,
                _terrainCentreLatitude + (position.Y / SphereRadius),
                _terrainCentreLongitude + (position.X / SphereRadius),
                TerrainMaximumFrequency, _terrainSettings, _rivers);

            // Height above sea level, normalised against the palette's high-ground reference. Sea bed
            // reads as zero rather than negative: elevation is a lapse-rate input here, and ground
            // below the waterline being *warmer* than the shore is not a claim this wants to make.
            double land = EnvironmentNoise.Clamp01(terrain.Elevation / PlanetTerrain.HighGround);

            // Terrain climate sets the MEAN; a local band supplies the variation across the arena.
            //
            // Without the band the join delivers a more uniform field than the hand-written one it
            // replaces: the arena is 50 units wide, which is 0.1 radian on a 500-unit planet, and
            // moisture and temperature vary on continental scales. Measured over 1,681 arena
            // positions, terrain moisture had a standard deviation of .005 at seed 161 against the
            // procedural field's .283, and 480 runs found no plant conclusion moved - not because
            // terrain is ecologically neutral but because there was nothing left to be neutral
            // about. See docs/experiments/p4-terrain-join-2026-08-23.md.
            //
            // The band is centred on zero, so the regional value is still the average over the
            // window: which continent the arena sits on decides whether it is wet or dry, and the
            // band decides which end of the valley is wetter. Clamping keeps the recorded output
            // ranges (.15 to 1, and .20 to 1 before lapse) intact.
            SpherePoint(position, out double localX, out double localY, out double localZ);
            double moistureLocal = LocalBand(channel: 224, x: localX, y: localY, z: localZ);
            double temperatureLocal = LocalBand(channel: 240, x: localX, y: localY, z: localZ);

            double moisture = .15d + (.85d * EnvironmentNoise.Clamp01(
                EnvironmentNoise.Clamp01(terrain.Moisture) + (LocalMoistureStrength * moistureLocal)));
            double temperature = .20d + (.80d * EnvironmentNoise.Clamp01(
                EnvironmentNoise.Clamp01(terrain.Temperature) + (LocalTemperatureStrength * temperatureLocal)));

            // Fertility keeps the shape the procedural field established - independent noise ridged
            // at moderate moisture, so waterlogged and arid ground are both poor and the best soil is
            // contested. Only its moisture input changes, which is the point: fertility now follows
            // the rain shadows the terrain actually has.
            double fertilityNoise = EnvironmentNoise.Contrast(
                EnvironmentNoise.WarpedFbm(
                    _worldSeed, channel: 96, localX, localY, localZ,
                    octaves: 3, lacunarity: 2d, gain: .5d, warpStrength: .2d),
                strength: 2.0d);
            double moistureBalance = 1d - EnvironmentNoise.Clamp01(Math.Abs(moisture - .55d) * 1.8d);
            double fertility = .20d + (.80d * EnvironmentNoise.Clamp01(fertilityNoise * (.35d + (.65d * moistureBalance))));

            // Lapse rate, as in the procedural field: height costs warmth, floored rather than zeroed,
            // because temperature 0 stops growth outright and a dead crest is less interesting than a
            // cold one.
            temperature = Math.Max(.02d, temperature - (LapseRate * land));

            return new EnvironmentSample((float)moisture, (float)fertility, (float)temperature, (float)land);
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
            double latitudeTerm = _usesPlanetScaleClimate
                ? EnvironmentNoise.Clamp01(Math.Cos(position.Y / SphereRadius))
                : EnvironmentNoise.Clamp01(
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
