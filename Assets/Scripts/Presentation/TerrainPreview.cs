using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// A look-at-it view of the environment fields, decoupled from the simulation arena.
    ///
    /// <para><b>Why this exists.</b> The arena is 50 units wide and carries about three landforms,
    /// which is far too small a sample to judge whether terrain reads at the right scale or whether
    /// the biomes have any variety. But the fields are <i>pure functions of position and seed</i>,
    /// defined everywhere - so a preview can render any extent, or the whole planet, without the
    /// simulation being involved at all.</para>
    ///
    /// <para><b>Entirely presentation.</b> Nothing here is read by anything under
    /// <c>Assets/Scripts/Simulation</c>. It samples the same <c>EnvironmentField</c> the simulation
    /// uses and draws it. No hash moves, no test is affected, and no determinism guarantee is
    /// touched, whatever this renders.</para>
    ///
    /// <para><b>On the sphere view.</b> A spherical <i>simulation</i> is a spatial-model refactor:
    /// every position is a <c>SimVector2</c>, the arena is hardcoded, and movement, distance,
    /// dispersal and spatial hashing all assume a plane. A spherical <i>view</i> is nearly free,
    /// because <c>EnvironmentField</c> already samples 3D noise on a sphere of radius
    /// <see cref="EnvironmentField.SphereRadius"/> - this just walks the whole surface instead of the
    /// small equatorial window the arena occupies. Seeing the globe does not mean the world is
    /// round.</para>
    /// </summary>
    public sealed class TerrainPreview
    {
        public enum Mode
        {
            /// <summary>Off: the normal arena-sized ground.</summary>
            Off = 0,

            /// <summary>A wide flat patch, many landforms across: what the raw field looks like.</summary>
            WidePatch = 1,

            /// <summary>The same field shaped into a landmass ringed by ocean.</summary>
            Island = 2,

            /// <summary>The whole planet, as an actual sphere.</summary>
            Planet = 3,
        }

        /// <summary>Half-width of the wide patch, in simulation units. 200 shows ~24 landforms across.</summary>
        public const float WidePatchHalfWidth = 200f;

        private const int PatchResolution = 161;
        private const int SphereLongitudeSteps = 192;
        private const int SphereLatitudeSteps = 96;
        private const int TextureWidth = 384;
        private const int TextureHeight = 192;

        /// <summary>Drawn planet radius in world units. Unrelated to the field's sampling radius.</summary>
        private const float PlanetDrawRadius = 60f;

        /// <summary>Relief on the planet as a fraction of its radius.</summary>
        private const float PlanetReliefFraction = 0.045f;

        private readonly GameObject _root;
        private readonly MeshFilter _meshFilter;
        private readonly MeshRenderer _renderer;
        private readonly Mesh _mesh;
        private Texture2D _texture;

        /// <summary>
        /// The preview samples <b>its own</b> procedural field with elevation always enabled, rather
        /// than the live world's.
        ///
        /// <para>It previously used <c>world.Environment</c>, which is wrong for a terrain viewer:
        /// most scenarios run the flat legacy environment where moisture, fertility and temperature
        /// are all 1 and elevation is 0. Every view then displaced by zero and shaded to the same
        /// branch, so the wide patch and the island both rendered as an identical flat green plane
        /// and the planet as a smooth ball. The generator was fine; nothing was asking it anything.</para>
        ///
        /// <para>This is a viewer for the terrain generator, so it should show the generator. When
        /// the live scenario happens to use the same settings the two agree exactly, because both
        /// are pure functions of the same seed.</para>
        /// </summary>
        private int _fieldSeed = int.MinValue;

        // Sampling is the expensive half - EnvironmentField.Sample runs several multi-octave
        // warped-fBm evaluations per call, and a full texture was ~131,000 of them, which is the
        // pause felt on every keypress. Everything sampled is cached and keyed by what it depends
        // on, so changing the height scale rebuilds geometry from cached numbers and never resamples.
        private float[] _planetElevation;
        private Mode _texturedMode = Mode.Off;
        private int _texturedSeed = int.MinValue;

        public TerrainPreview()
        {
            _root = new GameObject("Terrain Preview");
            _meshFilter = _root.AddComponent<MeshFilter>();
            _renderer = _root.AddComponent<MeshRenderer>();
            _renderer.material = new Material(Shader.Find("Standard"));
            _mesh = new Mesh { name = "Terrain Preview Mesh" };
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _meshFilter.sharedMesh = _mesh;
            _root.SetActive(false);
        }

        public Mode Current { get; private set; } = Mode.Off;

        /// <summary>Vertical exaggeration of the wide patch, shared with the arena ground for consistency.</summary>
        public float HeightScale { get; set; } = 14f;

        private string LiveSuffix
        {
            get { return DiffersFromLiveWorld ? "  [generator preview, not this scenario]" : "  [matches this scenario]"; }
        }

        /// <summary>What the preview is currently showing, for the on-screen readout.</summary>
        public string Describe()
        {
            switch (Current)
            {
                case Mode.WidePatch:
                    return $"1/3 wide patch - {WidePatchHalfWidth * 2f:0} units of raw field{LiveSuffix}";
                case Mode.Island:
                    return $"2/3 island - the same field with a radial falloff{LiveSuffix}";
                case Mode.Planet:
                    return $"3/3 planet - the VIEW is spherical, the simulation is still flat{LiveSuffix}";
                default:
                    return "off (K to open the terrain viewer)";
            }
        }

        /// <summary>How far the camera must pull back to see the current view, in world units.</summary>
        public float FramingRadius
        {
            get
            {
                switch (Current)
                {
                    case Mode.WidePatch:
                    case Mode.Island:
                        return WidePatchHalfWidth;
                    case Mode.Planet:
                        return PlanetDrawRadius * 1.35f;
                    default:
                        return 0f;
                }
            }
        }

        /// <summary>True when the live scenario does not itself use procedural fields with elevation,
        /// so what is on screen is the generator rather than the world the creatures are living in.</summary>
        public bool DiffersFromLiveWorld { get; private set; }

        /// <summary>
        /// Planet-scale climate, so a wide view is not an equatorial strip of habitable ground with
        /// ice everywhere else. The standard field's latitude term reaches zero exactly at the arena
        /// edge by design - correct for a 50-unit world, and wrong for any view past it.
        /// See <see cref="EnvironmentField.CreatePlanetScaleClimate"/>.
        /// </summary>
        private void FieldFor(SimulationWorld world)
        {
            int seed = world.Config.WorldSeed;
            if (_fieldSeed == seed) return;

            _fieldSeed = seed;
            _planetElevation = null;
            _texturedMode = Mode.Off;
        }

        public Mode Advance(SimulationWorld world)
        {
            Current = (Mode)(((int)Current + 1) % 4);
            Rebuild(world);
            return Current;
        }

        public void Hide()
        {
            Current = Mode.Off;
            _root.SetActive(false);
        }

        public void Rebuild(SimulationWorld world)
        {
            if (world == null || Current == Mode.Off)
            {
                _root.SetActive(false);
                return;
            }

            _root.SetActive(true);
            if (Current == Mode.Planet)
            {
                BuildPlanet(world);
            }
            else
            {
                BuildWidePatch(world, island: Current == Mode.Island);
            }
        }

        /// <summary>
        /// A flat patch far wider than the arena, so many landforms and several biomes are visible at
        /// once. This is the view that makes "is the scale right?" answerable, because scale is a
        /// comparison and a single landform cannot be compared with anything.
        /// </summary>
        private void BuildWidePatch(SimulationWorld world, bool island)
        {
            int side = PatchResolution;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[side * side];
            var triangles = new int[(side - 1) * (side - 1) * 6];

            for (int row = 0; row < side; row++)
            {
                float v = row / (float)(side - 1);
                float z = Mathf.Lerp(-WidePatchHalfWidth, WidePatchHalfWidth, v);
                for (int column = 0; column < side; column++)
                {
                    float u = column / (float)(side - 1);
                    float x = Mathf.Lerp(-WidePatchHalfWidth, WidePatchHalfWidth, u);
                    PlanetSample sample = SamplePatch(world, x, z);
                    int vertex = row * side + column;
                    float elevation = island ? sample.Elevation * IslandFalloff(x, z) : sample.Elevation;
                    float height = Mathf.Max(0f, elevation - SeaLevel) / (1f - SeaLevel) * HeightScale;
                    vertices[vertex] = new Vector3(x, height, z);
                    uv[vertex] = new Vector2(u, v);
                }
            }

            WriteQuads(triangles, side);
            Commit(vertices, uv, triangles);
            if (NeedsTexture(world)) ApplyTexture(BuildPatchTexture(world, island));
        }

        /// <summary>
        /// The whole world as a sphere. Longitude and latitude are converted back into the
        /// <c>SimVector2</c> coordinates the field expects — the field maps
        /// <c>longitude = x / SphereRadius</c> and <c>latitude = y / SphereRadius</c>, so walking
        /// longitude over ±π and latitude over ±π/2 covers the entire surface, of which the arena is
        /// a 50-unit speck near the equator.
        /// </summary>
        private void BuildPlanet(SimulationWorld world)
        {
            FieldFor(world);
            EnsurePlanetElevation();
            int longitudeSteps = SphereLongitudeSteps;
            int latitudeSteps = SphereLatitudeSteps;
            var vertices = new Vector3[(longitudeSteps + 1) * (latitudeSteps + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[longitudeSteps * latitudeSteps * 6];

            for (int latitudeIndex = 0; latitudeIndex <= latitudeSteps; latitudeIndex++)
            {
                float v = latitudeIndex / (float)latitudeSteps;
                double latitude = (v - 0.5d) * Math.PI;
                for (int longitudeIndex = 0; longitudeIndex <= longitudeSteps; longitudeIndex++)
                {
                    float u = longitudeIndex / (float)longitudeSteps;
                    double longitude = (u - 0.5d) * 2d * Math.PI;

                    PlanetSample sample = PlanetTerrain.SampleAtLatLon(world.Config.WorldSeed, latitude, longitude, PlanetMaximumFrequency);
                    float relief = 1f + (Mathf.Max(0f, PlanetElevation(latitudeIndex, longitudeIndex, sample.Elevation) - SeaLevel) / (1f - SeaLevel) * PlanetReliefFraction);
                    float radius = PlanetDrawRadius * relief;

                    double cosLatitude = Math.Cos(latitude);
                    var direction = new Vector3(
                        (float)(cosLatitude * Math.Sin(longitude)),
                        (float)Math.Sin(latitude),
                        (float)(cosLatitude * Math.Cos(longitude)));

                    int vertex = (latitudeIndex * (longitudeSteps + 1)) + longitudeIndex;
                    vertices[vertex] = direction * radius;
                    uv[vertex] = new Vector2(u, v);
                }
            }

            int triangle = 0;
            for (int latitudeIndex = 0; latitudeIndex < latitudeSteps; latitudeIndex++)
            {
                for (int longitudeIndex = 0; longitudeIndex < longitudeSteps; longitudeIndex++)
                {
                    int bottomLeft = (latitudeIndex * (longitudeSteps + 1)) + longitudeIndex;
                    int topLeft = bottomLeft + longitudeSteps + 1;

                    // Counter-clockwise seen from OUTSIDE. The previous order was reversed, so every
                    // outward face was backface-culled and the inside of the far hemisphere showed
                    // through instead - the sphere looked transparent and inside-out from the front.
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = topLeft + 1;
                    triangles[triangle++] = topLeft;
                }
            }

            Commit(vertices, uv, triangles);
            if (NeedsTexture(world)) ApplyTexture(BuildPlanetTexture(world));
        }

        /// <summary>
        /// Convert a latitude/longitude back into the arena coordinates the field samples in. The
        /// field treats the arena as a small equatorial window, so this is its inverse.
        /// </summary>
        /// <summary>Sample elevation once per sphere vertex; only redone when the seed changes.</summary>
        private void EnsurePlanetElevation()
        {
            int width = SphereLongitudeSteps + 1;
            int height = SphereLatitudeSteps + 1;
            int cells = width * height;
            if (_planetElevation != null && _planetElevation.Length == cells) return;

            _planetElevation = new float[cells];
            for (int latitudeIndex = 0; latitudeIndex < height; latitudeIndex++)
            {
                double latitude = ((latitudeIndex / (double)SphereLatitudeSteps) - 0.5d) * Math.PI;
                for (int longitudeIndex = 0; longitudeIndex < width; longitudeIndex++)
                {
                    double longitude = ((longitudeIndex / (double)SphereLongitudeSteps) - 0.5d) * 2d * Math.PI;
                    _planetElevation[(latitudeIndex * width) + longitudeIndex] =
                        PlanetTerrain.SampleAtLatLon(_fieldSeed, latitude, longitude, PlanetMaximumFrequency).Elevation;
                }
            }
        }

        /// <summary>
        /// Smoothed planet elevation, averaged over the neighbouring lat/lon samples.
        ///
        /// <para>A globe carries the whole field across far fewer vertices than a patch does, so the
        /// finest octaves land below one vertex and displace isolated points - which renders as
        /// spikes rather than as terrain. Averaging removes what cannot be resolved at this size.
        /// The detail is still present in the flat views, where there are enough vertices to show
        /// it.</para>
        /// </summary>
        private float PlanetElevation(int latitudeIndex, int longitudeIndex, float fallback)
        {
            if (_planetElevation == null) return fallback;

            int width = SphereLongitudeSteps + 1;
            int height = SphereLatitudeSteps + 1;
            float total = 0f;
            int taps = 0;
            for (int dLatitude = -1; dLatitude <= 1; dLatitude++)
            {
                int row = latitudeIndex + dLatitude;
                if (row < 0 || row >= height) continue;
                for (int dLongitude = -1; dLongitude <= 1; dLongitude++)
                {
                    // Longitude wraps, so the seam averages against the far edge rather than itself.
                    int column = (longitudeIndex + dLongitude + width) % width;
                    total += _planetElevation[(row * width) + column];
                    taps++;
                }
            }

            return taps == 0 ? fallback : total / taps;
        }

        private static EnvironmentSample SampleAtLatLon(EnvironmentField field, double latitude, double longitude)
        {
            var position = new SimVector2(
                (float)(longitude * EnvironmentField.SphereRadius),
                (float)(latitude * EnvironmentField.SphereRadius));
            return field.Sample(position);
        }

        /// <summary>
        /// True when the colour map is stale. Height tuning does not invalidate it, which is most of
        /// what made adjusting a setting take a visible pause: the texture is tens of thousands of
        /// field evaluations and none of them depend on the height scale.
        /// </summary>
        private bool NeedsTexture(SimulationWorld world)
        {
            if (_texture != null && _texturedMode == Current && _texturedSeed == world.Config.WorldSeed) return false;
            _texturedMode = Current;
            _texturedSeed = world.Config.WorldSeed;
            return true;
        }

        private Texture2D BuildPatchTexture(SimulationWorld world, bool island)
        {
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                float worldZ = Mathf.Lerp(-WidePatchHalfWidth, WidePatchHalfWidth, (y + 0.5f) / TextureHeight);
                for (int x = 0; x < TextureWidth; x++)
                {
                    float worldX = Mathf.Lerp(-WidePatchHalfWidth, WidePatchHalfWidth, (x + 0.5f) / TextureWidth);
                    PlanetSample sample = SamplePatch(world, worldX, worldZ);
                    pixels[(y * TextureWidth) + x] = Shade(sample, island ? IslandFalloff(worldX, worldZ) : 1f);
                }
            }

            return MakeTexture(pixels);
        }

        private Texture2D BuildPlanetTexture(SimulationWorld world)
        {
            FieldFor(world);
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                double latitude = (((y + 0.5d) / TextureHeight) - 0.5d) * Math.PI;
                for (int x = 0; x < TextureWidth; x++)
                {
                    double longitude = (((x + 0.5d) / TextureWidth) - 0.5d) * 2d * Math.PI;
                    pixels[(y * TextureWidth) + x] = Shade(PlanetTerrain.SampleAtLatLon(world.Config.WorldSeed, latitude, longitude, PlanetMaximumFrequency), 1f);
                }
            }

            return MakeTexture(pixels);
        }

        /// <summary>Sea level as a fraction of the elevation range.</summary>
        private const float SeaLevel = PlanetTerrain.SeaLevel;

        /// <summary>
        /// Highest feature density each view can draw without aliasing.
        ///
        /// <para>The globe is the tight one: 192 columns around a full turn resolves frequencies up
        /// to 192 / 4pi, about 15. The previous version sampled a five-octave field whose finest band
        /// carried roughly 4,000 features around the equator - twenty times past what the mesh could
        /// represent - which is why it rendered as static rather than as terrain.</para>
        ///
        /// <para>The patch covers 0.8 radians with 161 samples, so it resolves far more, which is why
        /// the flat views legitimately show detail the globe cannot.</para>
        /// </summary>
        private static readonly double PlanetMaximumFrequency = PlanetTerrain.MaximumFrequencyFor(SphereLongitudeSteps);

        private static readonly double PatchMaximumFrequency =
            PlanetTerrain.MaximumFrequencyFor((int)(PatchResolution * (2d * Math.PI) / PatchAngularWidth));

        /// <summary>Angular width of the wide patch on the unit sphere, in radians.</summary>
        private const double PatchAngularWidth = 2d * WidePatchHalfWidth / EnvironmentField.SphereRadius;

        /// <summary>
        /// Sample the patch by treating it as a window on the sphere, which is exactly how the
        /// simulation's own field maps arena positions - so the patch and the globe show the same
        /// world at different zooms rather than two unrelated noise fields.
        /// </summary>
        private static PlanetSample SamplePatch(SimulationWorld world, float x, float z)
        {
            double longitude = x / EnvironmentField.SphereRadius;
            double latitude = z / EnvironmentField.SphereRadius;
            return PlanetTerrain.SampleAtLatLon(world.Config.WorldSeed, latitude, longitude, PatchMaximumFrequency);
        }

        /// <summary>
        /// Colour that combines all four fields, so the preview answers "do these read as a world?"
        /// rather than "what does one channel look like?". Water first, then cold ground, then the
        /// moisture/fertility classification that decides the rest.
        /// </summary>
        private static Color Shade(PlanetSample sample, float elevationScale)
        {
            float elevation = sample.Elevation * elevationScale;

            if (sample.Elevation > 0f && elevation <= SeaLevel)
            {
                float depth = Mathf.Clamp01(elevation / SeaLevel);
                return Color.Lerp(new Color(0.035f, 0.106f, 0.235f), new Color(0.180f, 0.451f, 0.647f), depth);
            }

            float land = sample.Elevation <= 0f ? 0f : Mathf.Clamp01((elevation - SeaLevel) / (1f - SeaLevel));

            // Beach: a narrow sand band just above the waterline. Cheap, and it is most of what makes
            // a coastline read as a coastline rather than as grass meeting blue.
            if (sample.Elevation > 0f && land < 0.045f) return new Color(0.902f, 0.831f, 0.639f);

            if (sample.Temperature < 0.24f) return Color.Lerp(new Color(0.86f, 0.90f, 0.93f), Color.white, land);
            if (sample.Temperature < 0.40f) return Color.Lerp(new Color(0.498f, 0.584f, 0.659f), new Color(0.62f, 0.66f, 0.68f), land);
            if (sample.Moisture < 0.34f) return Color.Lerp(new Color(0.878f, 0.769f, 0.478f), new Color(0.706f, 0.588f, 0.376f), land);
            if (sample.Moisture > 0.72f && land < 0.14f) return new Color(0.259f, 0.435f, 0.388f);
            if (sample.Moisture > 0.46f) return Color.Lerp(new Color(0.325f, 0.612f, 0.243f), new Color(0.239f, 0.408f, 0.220f), land);
            return Color.Lerp(new Color(0.588f, 0.549f, 0.361f), new Color(0.463f, 0.435f, 0.396f), land);
        }

        /// <summary>
        /// Radial falloff that turns an endless field into a landmass ringed by ocean, as in the
        /// reference art. Flat across the interior, then dropping to zero before the edge, so the
        /// coastline is produced by the shape of the field rather than cut off by the mesh boundary.
        /// </summary>
        private static float IslandFalloff(float x, float z)
        {
            float distance = Mathf.Sqrt((x * x) + (z * z)) / WidePatchHalfWidth;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.92f, distance));
        }

        private static void WriteQuads(int[] triangles, int side)
        {
            int triangle = 0;
            for (int row = 0; row + 1 < side; row++)
            {
                for (int column = 0; column + 1 < side; column++)
                {
                    int bottomLeft = (row * side) + column;
                    int topLeft = bottomLeft + side;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = topLeft + 1;
                }
            }
        }

        /// <summary>
        /// Commit the mesh <b>flat shaded</b>: every triangle gets its own three vertices, so each
        /// face carries one normal and renders as a distinct facet.
        ///
        /// <para>This is the low-poly look in the reference art, and it is not only decorative -
        /// faceting makes slope legible. A smooth-shaded heightfield at this resolution reads as an
        /// undifferentiated blob, which is part of why the terrain was hard to judge.</para>
        /// </summary>
        private void Commit(Vector3[] vertices, Vector2[] uv, int[] triangles)
        {
            var flatVertices = new Vector3[triangles.Length];
            var flatUv = new Vector2[triangles.Length];
            var flatTriangles = new int[triangles.Length];
            for (int index = 0; index < triangles.Length; index++)
            {
                flatVertices[index] = vertices[triangles[index]];
                flatUv[index] = uv[triangles[index]];
                flatTriangles[index] = index;
            }

            _root.transform.position = Vector3.zero;
            _mesh.Clear();
            _mesh.vertices = flatVertices;
            _mesh.uv = flatUv;
            _mesh.triangles = flatTriangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        private Texture2D MakeTexture(Color[] pixels)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.filterMode = FilterMode.Bilinear;
            }

            _texture.SetPixels(pixels);
            _texture.Apply();
            return _texture;
        }

        private void ApplyTexture(Texture2D texture)
        {
            _renderer.material.mainTexture = texture;
            _renderer.material.color = Color.white;
        }
    }
}
