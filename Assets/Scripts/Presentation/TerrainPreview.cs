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

            /// <summary>A wide flat patch, many landforms across.</summary>
            WidePatch = 1,

            /// <summary>The whole planet, as an actual sphere.</summary>
            Planet = 2,
        }

        /// <summary>Half-width of the wide patch, in simulation units. 200 shows ~24 landforms across.</summary>
        public const float WidePatchHalfWidth = 200f;

        private const int PatchResolution = 193;
        private const int SphereLongitudeSteps = 256;
        private const int SphereLatitudeSteps = 128;
        private const int TextureWidth = 512;
        private const int TextureHeight = 256;

        /// <summary>Drawn planet radius in world units. Unrelated to the field's sampling radius.</summary>
        private const float PlanetDrawRadius = 60f;

        /// <summary>Relief on the planet as a fraction of its radius.</summary>
        private const float PlanetReliefFraction = 0.055f;

        private readonly GameObject _root;
        private readonly MeshFilter _meshFilter;
        private readonly MeshRenderer _renderer;
        private readonly Mesh _mesh;
        private Texture2D _texture;

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

        /// <summary>What the preview is currently showing, for the on-screen readout.</summary>
        public string Describe()
        {
            switch (Current)
            {
                case Mode.WidePatch:
                    return $"wide patch, {WidePatchHalfWidth * 2f:0} units across (arena is 50)";
                case Mode.Planet:
                    return "whole planet - the VIEW is spherical, the simulation is still flat";
                default:
                    return "off";
            }
        }

        public Mode Advance(SimulationWorld world)
        {
            Current = (Mode)(((int)Current + 1) % 3);
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
            if (Current == Mode.WidePatch)
            {
                BuildWidePatch(world);
            }
            else
            {
                BuildPlanet(world);
            }
        }

        /// <summary>
        /// A flat patch far wider than the arena, so many landforms and several biomes are visible at
        /// once. This is the view that makes "is the scale right?" answerable, because scale is a
        /// comparison and a single landform cannot be compared with anything.
        /// </summary>
        private void BuildWidePatch(SimulationWorld world)
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
                    EnvironmentSample sample = world.Environment.Sample(new SimVector2(x, z));
                    int vertex = row * side + column;
                    float height = Mathf.Max(0f, sample.Elevation - SeaLevel) / (1f - SeaLevel) * HeightScale;
                    vertices[vertex] = new Vector3(x, height, z);
                    uv[vertex] = new Vector2(u, v);
                }
            }

            WriteQuads(triangles, side);
            Commit(vertices, uv, triangles);
            ApplyTexture(BuildPatchTexture(world));
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

                    EnvironmentSample sample = SampleAtLatLon(world, latitude, longitude);
                    float relief = 1f + (Mathf.Max(0f, sample.Elevation - SeaLevel) / (1f - SeaLevel) * PlanetReliefFraction);
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
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = topLeft + 1;
                }
            }

            Commit(vertices, uv, triangles);
            ApplyTexture(BuildPlanetTexture(world));
            _root.transform.position = new Vector3(0f, PlanetDrawRadius + 20f, 0f);
        }

        /// <summary>
        /// Convert a latitude/longitude back into the arena coordinates the field samples in. The
        /// field treats the arena as a small equatorial window, so this is its inverse.
        /// </summary>
        private static EnvironmentSample SampleAtLatLon(SimulationWorld world, double latitude, double longitude)
        {
            var position = new SimVector2(
                (float)(longitude * EnvironmentField.SphereRadius),
                (float)(latitude * EnvironmentField.SphereRadius));
            return world.Environment.Sample(position);
        }

        private Texture2D BuildPatchTexture(SimulationWorld world)
        {
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                float worldZ = Mathf.Lerp(-WidePatchHalfWidth, WidePatchHalfWidth, (y + 0.5f) / TextureHeight);
                for (int x = 0; x < TextureWidth; x++)
                {
                    float worldX = Mathf.Lerp(-WidePatchHalfWidth, WidePatchHalfWidth, (x + 0.5f) / TextureWidth);
                    pixels[(y * TextureWidth) + x] = Shade(world.Environment.Sample(new SimVector2(worldX, worldZ)));
                }
            }

            return MakeTexture(pixels);
        }

        private Texture2D BuildPlanetTexture(SimulationWorld world)
        {
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                double latitude = (((y + 0.5d) / TextureHeight) - 0.5d) * Math.PI;
                for (int x = 0; x < TextureWidth; x++)
                {
                    double longitude = (((x + 0.5d) / TextureWidth) - 0.5d) * 2d * Math.PI;
                    pixels[(y * TextureWidth) + x] = Shade(SampleAtLatLon(world, latitude, longitude));
                }
            }

            return MakeTexture(pixels);
        }

        /// <summary>Sea level as a fraction of the elevation range, matching the arena overlay.</summary>
        private const float SeaLevel = 0.38f;

        /// <summary>
        /// Colour that combines all four fields, so the preview answers "do these read as a world?"
        /// rather than "what does one channel look like?". Water first, then cold ground, then the
        /// moisture/fertility classification that decides the rest.
        /// </summary>
        private static Color Shade(EnvironmentSample sample)
        {
            if (sample.Elevation > 0f && sample.Elevation <= SeaLevel)
            {
                float depth = Mathf.Clamp01(sample.Elevation / SeaLevel);
                return Color.Lerp(new Color(0.035f, 0.106f, 0.235f), new Color(0.153f, 0.376f, 0.573f), depth);
            }

            float land = sample.Elevation <= 0f ? 0f : Mathf.Clamp01((sample.Elevation - SeaLevel) / (1f - SeaLevel));

            if (sample.Temperature < 0.24f) return Color.Lerp(new Color(0.86f, 0.90f, 0.93f), Color.white, land);
            if (sample.Temperature < 0.40f) return Color.Lerp(new Color(0.498f, 0.584f, 0.659f), new Color(0.62f, 0.66f, 0.68f), land);
            if (sample.Moisture < 0.34f) return Color.Lerp(new Color(0.816f, 0.706f, 0.443f), new Color(0.647f, 0.545f, 0.361f), land);
            if (sample.Moisture > 0.74f && sample.Fertility < 0.48f) return new Color(0.259f, 0.435f, 0.388f);
            if (sample.Fertility > 0.58f) return Color.Lerp(new Color(0.235f, 0.529f, 0.216f), new Color(0.318f, 0.435f, 0.251f), land);
            return Color.Lerp(new Color(0.588f, 0.549f, 0.361f), new Color(0.463f, 0.435f, 0.396f), land);
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

        private void Commit(Vector3[] vertices, Vector2[] uv, int[] triangles)
        {
            _root.transform.position = Vector3.zero;
            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;
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
