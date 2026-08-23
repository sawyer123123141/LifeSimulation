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

            /// <summary>A close view, near the scale the simulation actually runs at.</summary>
            Island = 2,

            /// <summary>The whole planet, as an actual sphere.</summary>
            Planet = 3,
        }

        /// <summary>Half-width of the wide patch, in simulation units. 200 shows ~24 landforms across.</summary>
        public const float WidePatchHalfWidth = 200f;

        /// <summary>
        /// Half-width of the close view: four times the arena, so the playable area is a visible
        /// fraction of it. This is the view that answers "what would a creature actually stand on?"
        ///
        /// <para>It replaces an earlier island mask, which multiplied elevation by a radial falloff
        /// to force a landmass ringed by ocean. That was a workaround for having no continents;
        /// plate structure produces real land and sea, so the mask only destroyed it.</para>
        /// </summary>
        public const float RegionHalfWidth = 100f;

        /// <summary>
        /// The close view is offset from the continental centre so a coastline is in frame. Centred
        /// exactly on the plate it showed nothing but inland grass - true to the world and useless
        /// for judging it.
        /// </summary>
        private const float RegionOffset = 150f;

        private const int PatchResolution = 161;
        /// <summary>
        /// Icosphere subdivisions. 5 gives 20,480 near-uniform triangles - comparable detail to the
        /// 192x96 lat/lon grid it replaces, without the polar singularity or the equatorial stretch.
        /// </summary>
        private const int PlanetSubdivisions = 5;
        private const int TextureWidth = 384;
        private const int TextureHeight = 192;

        /// <summary>Drawn planet radius in world units. Unrelated to the field's sampling radius.</summary>
        private const float PlanetDrawRadius = 60f;

        /// <summary>Relief on the planet as a fraction of its radius.</summary>
        private const float PlanetReliefFraction = 0.075f;

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

        /// <summary>Tectonic structure for the current seed. Rebuilt only when the seed changes.</summary>
        private PlateStructure _plates;

        /// <summary>
        /// Where the flat views are centred. Not the origin: a flat view spans about one plate, and
        /// the plate at the origin is whichever the Fibonacci construction put there - an oceanic one
        /// at seed 42, so both flat views rendered as open sea while the planet was 30% land.
        /// </summary>
        private double _centreLatitude;
        private double _centreLongitude;

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

        /// <summary>
        /// Relief on the wide patch, as a fraction of its width. The patch is eight times wider than
        /// the arena, so reusing the arena's height directly would draw 400 units of world with 14
        /// units of relief - flat, whatever the elevation field says.
        ///
        /// <para>Real terrain is far flatter than this: Everest is about 0.2% of the distance it
        /// spans. A truthful ratio reads as a plain, so terrain is exaggerated here as it is in every
        /// game that draws it.</para>
        /// </summary>
        private const float PatchReliefFraction = 0.075f;

        /// <summary>
        /// Patch relief, still scaled by the arena tuning so <c>[</c> and <c>]</c> keep working -
        /// they now adjust it relative to a height that suits the patch rather than the arena.
        /// </summary>
        /// <summary>Extent of the flat view currently selected.</summary>
        private float CurrentHalfWidth
        {
            get { return Current == Mode.Island ? RegionHalfWidth : WidePatchHalfWidth; }
        }

        private float PatchHeightScale
        {
            get { return CurrentHalfWidth * 2f * PatchReliefFraction * (HeightScale / 14f); }
        }

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
                    return $"2/3 close view - {RegionHalfWidth * 2f:0} units, near simulation scale{LiveSuffix}";
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
                        return WidePatchHalfWidth;
                    case Mode.Island:
                        return RegionHalfWidth;
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
            _plates = new PlateStructure(seed);
            _plates.GetContinentalCentre(out _centreLatitude, out _centreLongitude);
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

            // Every view needs the plate structure, and only the planet path used to ask for it -
            // so the patch modes dereferenced a null PlateStructure. Ensured once here rather than
            // per branch, which is what let the two paths disagree in the first place.
            FieldFor(world);

            if (Current == Mode.Planet)
            {
                BuildPlanet(world);
            }
            else
            {
                BuildWidePatch(world, CurrentHalfWidth);
            }
        }

        /// <summary>
        /// A flat patch far wider than the arena, so many landforms and several biomes are visible at
        /// once. This is the view that makes "is the scale right?" answerable, because scale is a
        /// comparison and a single landform cannot be compared with anything.
        /// </summary>
        private void BuildWidePatch(SimulationWorld world, float halfWidth)
        {
            int side = PatchResolution;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[side * side];
            var triangles = new int[(side - 1) * (side - 1) * 6];

            for (int row = 0; row < side; row++)
            {
                float v = row / (float)(side - 1);
                float z = Mathf.Lerp(-halfWidth, halfWidth, v);
                for (int column = 0; column < side; column++)
                {
                    float u = column / (float)(side - 1);
                    float x = Mathf.Lerp(-halfWidth, halfWidth, u);
                    PlanetSample sample = SamplePatch(world, x, z);
                    int vertex = row * side + column;
                    float elevation = sample.Elevation;
                    float height = Mathf.Max(0f, elevation - SeaLevel) / (1f - SeaLevel) * PatchHeightScale;
                    vertices[vertex] = new Vector3(x, height, z);
                    uv[vertex] = new Vector2(u, v);
                }
            }

            WriteQuads(triangles, side);
            Commit(vertices, uv, triangles);
            if (NeedsTexture(world)) ApplyTexture(BuildPatchTexture(world, halfWidth));
        }

        /// <summary>
        /// The whole world as a sphere. Longitude and latitude are converted back into the
        /// <c>SimVector2</c> coordinates the field expects — the field maps
        /// <c>longitude = x / SphereRadius</c> and <c>latitude = y / SphereRadius</c>, so walking
        /// longitude over ±π and latitude over ±π/2 covers the entire surface, of which the arena is
        /// a 50-unit speck near the equator.
        /// </summary>
        /// <summary>
        /// The planet, as a subdivided icosahedron.
        ///
        /// <para>A lat/lon sphere converges every longitude vertex on a single point at each pole, so
        /// triangles there degenerate and their normals fan out - the starburst "bottom of a balloon"
        /// pinch. That is singular by construction, not a tuning problem. An icosphere has no pole and
        /// near-uniform triangles everywhere, and gives the flat-shaded low-poly look as a side
        /// effect. Colour is per-vertex rather than a texture, which also removes the equirectangular
        /// seam and the polar texture stretch.</para>
        /// </summary>
        private void BuildPlanet(SimulationWorld world)
        {
            FieldFor(world);
            IcoSphere.Build(PlanetSubdivisions, out Vector3[] directions, out int[] indices);
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(IcoSphere.SamplesAroundEquator(PlanetSubdivisions));

            var vertices = new Vector3[directions.Length];
            var colors = new Color[directions.Length];
            for (int index = 0; index < directions.Length; index++)
            {
                Vector3 direction = directions[index];
                PlanetSample sample = PlanetTerrain.Sample(
                    world.Config.WorldSeed, _plates, direction.x, direction.y, direction.z, maximumFrequency);

                float relief = 1f + (Mathf.Max(0f, sample.Elevation - SeaLevel) / (1f - SeaLevel) * PlanetReliefFraction);
                vertices[index] = direction * (PlanetDrawRadius * relief);
                colors[index] = PlanetBiome.Shade(sample);
            }

            CommitColored(vertices, colors, indices);
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

        private Texture2D BuildPatchTexture(SimulationWorld world, float halfWidth)
        {
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                float worldZ = Mathf.Lerp(-halfWidth, halfWidth, (y + 0.5f) / TextureHeight);
                for (int x = 0; x < TextureWidth; x++)
                {
                    float worldX = Mathf.Lerp(-halfWidth, halfWidth, (x + 0.5f) / TextureWidth);
                    PlanetSample sample = SamplePatch(world, worldX, worldZ);
                    pixels[(y * TextureWidth) + x] = Shade(sample, 1f);
                }
            }

            return MakeTexture(pixels);
        }

        private static Color Shade(PlanetSample sample, float elevationScale)
        {
            return PlanetBiome.Shade(sample);
        }

        /// <summary>Two triangles per grid cell, wound consistently across the patch.</summary>
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

        /// <summary>
        /// Flat-shaded commit with per-vertex colour instead of a texture. Each triangle gets its own
        /// three vertices, all three carrying the colour of the first, so a face is one flat colour
        /// and one normal - the low-poly look, and no interpolation smearing a biome across a face.
        /// </summary>
        private void CommitColored(Vector3[] vertices, Color[] colors, int[] indices)
        {
            var flatVertices = new Vector3[indices.Length];
            var flatColors = new Color[indices.Length];
            var flatTriangles = new int[indices.Length];
            for (int index = 0; index < indices.Length; index += 3)
            {
                Color face = colors[indices[index]];
                for (int corner = 0; corner < 3; corner++)
                {
                    flatVertices[index + corner] = vertices[indices[index + corner]];
                    flatColors[index + corner] = face;
                    flatTriangles[index + corner] = index + corner;
                }
            }

            _root.transform.position = Vector3.zero;
            _mesh.Clear();
            _mesh.vertices = flatVertices;
            _mesh.colors = flatColors;
            _mesh.triangles = flatTriangles;
            _mesh.uv = new Vector2[flatVertices.Length];
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            // Standard ignores mesh.colors, so the planet would render white under it.
            Shader vertexColor = Shader.Find("LifeSimulation/VertexColorLit");
            if (vertexColor != null) _renderer.material.shader = vertexColor;
            _renderer.material.mainTexture = null;
            _renderer.material.color = Color.white;
        }

        private void ApplyTexture(Texture2D texture)
        {
            // Back to the textured path after the planet may have swapped the shader out.
            Shader standard = Shader.Find("Standard");
            if (standard != null && _renderer.material.shader != standard) _renderer.material.shader = standard;
            _renderer.material.mainTexture = texture;
            _renderer.material.color = Color.white;
        }
    }
}
