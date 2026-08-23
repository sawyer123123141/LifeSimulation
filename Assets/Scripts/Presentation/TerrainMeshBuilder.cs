using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// The single construction path for every terrain mesh, used by both the live preview and the
    /// offline PNG capture.
    ///
    /// <para><b>Why this exists.</b> The capture and the runtime previously built their scenes
    /// separately, and drifted: 321 samples offline against 161 live, vertex jitter and alternating
    /// diagonals offline only, a water plane offline and none live. So the diagnostic PNGs stopped
    /// being evidence about the Play view - they showed a different mesh. A capture that cannot
    /// reproduce the runtime is not an instrument, it is a second implementation.</para>
    ///
    /// <para>Anything that differs between the two now has to differ <i>here</i>, deliberately and
    /// visibly, rather than by two files quietly falling out of step.</para>
    /// </summary>
    public static class TerrainMeshBuilder
    {
        /// <summary>Samples per side of a flat patch. One value, shared.</summary>
        public const int PatchResolution = 193;

        /// <summary>Icosphere subdivisions for the planet. One value, shared.</summary>
        public const int PlanetSubdivisions = 5;

        /// <summary>Drawn planet radius in world units.</summary>
        public const float PlanetDrawRadius = 60f;

        /// <summary>Relief on the planet as a fraction of its radius.</summary>
        public const float PlanetReliefFraction = 0.06f;

        /// <summary>
        /// World units of height per unit of elevation, <b>constant across every view</b>.
        ///
        /// <para>This used to be a fraction of view width, which silently made zooming in flatten the
        /// terrain: the 400-unit view got 28 units per elevation unit and the 50-unit arena got 3.5,
        /// so the same ground rendered eight times flatter the closer you looked. Elevation is a
        /// physical quantity - 1.0 is about 30 metres - and 30 metres does not shrink because the
        /// camera moved.</para>
        ///
        /// <para>The exaggeration factor is in <c>PlanetReliefFraction</c> for the globe, which is a
        /// genuine artistic choice about a body whose real relief would be invisible at that size.
        /// </para>
        /// </summary>
        public const float ElevationToWorldUnits = 30f;

        public static float PatchHeightScale(float halfWidth)
        {
            return ElevationToWorldUnits;
        }

        /// <summary>
        /// A flat patch centred on a latitude/longitude, sampled as a window on the sphere so the
        /// flat views and the globe show the same world at different zooms.
        /// </summary>
        public static void BuildPatch(
            int seed, PlateStructure plates, double centreLatitude, double centreLongitude,
            float halfWidth, float heightScale,
            out Vector3[] vertices, out Color[] colors, out int[] triangles,
            TerrainSettings settings = null)
        {
            // Defaults to what the viewer is tuned to. The arena passes the SIMULATION's settings
            // when terrain drives the ecology, because a picture that disagrees with the model is the
            // failure this project exists to avoid - and the tuning panel would otherwise desync them
            // the moment anyone moved a slider.
            settings = settings ?? TerrainView.Settings;
            int side = PatchResolution;
            double angularWidth = 2d * halfWidth / SphereRadius;
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(
                (int)(side * (2d * Mathf.PI) / angularWidth));

            vertices = new Vector3[side * side];
            colors = new Color[side * side];
            triangles = new int[(side - 1) * (side - 1) * 6];

            for (int row = 0; row < side; row++)
            {
                float z = Mathf.Lerp(-halfWidth, halfWidth, row / (float)(side - 1));
                for (int column = 0; column < side; column++)
                {
                    float x = Mathf.Lerp(-halfWidth, halfWidth, column / (float)(side - 1));
                    PlanetSample sample = PlanetTerrain.SampleAtLatLon(
                        seed, plates,
                        centreLatitude + (z / SphereRadius),
                        centreLongitude + (x / SphereRadius),
                        maximumFrequency, settings);

                    int vertex = (row * side) + column;

                    // Elevation is signed displacement from sea level, so height is one multiply:
                    // no threshold, no branch, nothing to put a slope discontinuity at the waterline.
                    vertices[vertex] = new Vector3(x, sample.Elevation * heightScale, z);
                    colors[vertex] = PlanetBiome.Shade(sample);
                }
            }

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
        /// The planet as a subdivided icosahedron: no pole to pinch, near-uniform triangles, and the
        /// flat-shaded low-poly look as a side effect.
        /// </summary>
        public static void BuildPlanet(
            int seed, PlateStructure plates,
            out Vector3[] vertices, out Color[] colors, out int[] triangles)
        {
            IcoSphere.Build(PlanetSubdivisions, out Vector3[] directions, out int[] indices);
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(
                IcoSphere.SamplesAroundEquator(PlanetSubdivisions));

            vertices = new Vector3[directions.Length];
            colors = new Color[directions.Length];
            for (int index = 0; index < directions.Length; index++)
            {
                Vector3 direction = directions[index];
                PlanetSample sample = PlanetTerrain.Sample(
                    seed, plates, direction.x, direction.y, direction.z, maximumFrequency, TerrainView.Settings);

                vertices[index] = direction * (PlanetDrawRadius * (1f + (sample.Elevation * PlanetReliefFraction)));
                colors[index] = PlanetBiome.Shade(sample);
            }

            triangles = indices;
        }

        /// <summary>How far past the terrain the sea extends, so its edge is never in frame.</summary>
        public const float WaterOverhang = 1.8f;

        /// <summary>Samples per side of the water mesh. Enough for a swell to read as a curve.</summary>
        private const int WaterResolution = 65;

        /// <summary>
        /// A sea surface: gently displaced, and larger than the land it surrounds.
        ///
        /// <para>Replaces a flat primitive plane, which read as a sheet of plastic for two reasons.
        /// Its edges were visible - a finite quad the same size as the terrain, so the rectangle
        /// border cut across the view - and its surface had no variation at all, which no real water
        /// has at any scale.</para>
        ///
        /// <para>The swell is two crossed waves plus a noise term, all in world space, so it tiles
        /// with nothing and repeats at no obvious interval. Amplitude is a few centimetres against a
        /// creature of one metre: enough to catch the light and break the mirror, not enough to look
        /// like a storm.</para>
        ///
        /// <para><b>Deliberately a mesh rather than a plane primitive</b>, and deliberately built from
        /// a phase parameter. Animating it later is then a matter of advancing the phase per frame -
        /// purely presentation, touching no hash and no determinism guarantee, exactly as discussed
        /// when the water question first came up.</para>
        /// </summary>
        public static void BuildWaterSurface(
            float halfWidth, float phase,
            out Vector3[] vertices, out int[] triangles)
        {
            int side = WaterResolution;
            float extent = halfWidth * WaterOverhang;
            vertices = new Vector3[side * side];
            triangles = new int[(side - 1) * (side - 1) * 6];

            // Swell sized to the view: a wavelength of about a fifth of the visible water, so a few
            // crests cross the frame rather than a single dome or a rippled carpet.
            float wavelength = Mathf.Max(6f, extent * 0.22f);
            float k = 2f * Mathf.PI / wavelength;
            float amplitude = Mathf.Clamp(halfWidth * 0.004f, 0.03f, 0.35f);

            for (int row = 0; row < side; row++)
            {
                float z = Mathf.Lerp(-extent, extent, row / (float)(side - 1));
                for (int column = 0; column < side; column++)
                {
                    float x = Mathf.Lerp(-extent, extent, column / (float)(side - 1));

                    float primary = Mathf.Sin((x * k) + phase);
                    float secondary = Mathf.Sin((((x * 0.42f) + (z * 0.91f)) * k * 0.63f) - (phase * 0.7f));
                    float chop = Mathf.PerlinNoise((x * 0.05f) + phase * 0.05f, (z * 0.05f) - phase * 0.03f) - 0.5f;

                    float height = amplitude * ((0.55f * primary) + (0.30f * secondary) + (0.60f * chop));
                    vertices[(row * side) + column] = new Vector3(x, height, z);
                }
            }

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
        /// Smooth-shaded mesh, for water. Flat shading is right for terrain, where facets read as
        /// slope, and wrong for a sea surface, where they read as broken glass.
        /// </summary>
        public static Mesh SmoothShaded(Vector3[] vertices, int[] triangles, string name)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A smooth sphere at exactly sea level, for the planet's ocean.
        ///
        /// <para>Needed since elevation became signed displacement: the sea bed is now genuinely
        /// displaced downward, so without a sea surface the planet renders bumpy blue sea bed and
        /// calls it water. Land protrudes through this because land elevation is positive.</para>
        /// </summary>
        public static void BuildOceanSphere(out Vector3[] vertices, out int[] triangles)
        {
            IcoSphere.Build(PlanetSubdivisions - 1, out Vector3[] directions, out int[] indices);
            vertices = new Vector3[directions.Length];
            for (int index = 0; index < directions.Length; index++)
            {
                vertices[index] = directions[index] * PlanetDrawRadius;
            }

            triangles = indices;
        }

        /// <summary>
        /// Commit flat shaded: every triangle gets its own three vertices, so each face carries one
        /// normal and renders as a facet. Colour stays per corner, because painting a whole face with
        /// one corner's colour quantises the palette to one value per triangle - which is blocky
        /// banding, not low poly. Flat shading and flat colour are separate choices.
        /// </summary>
        public static Mesh FlatShaded(Vector3[] vertices, Color[] colors, int[] indices, string name)
        {
            var flatVertices = new Vector3[indices.Length];
            var flatColors = new Color[indices.Length];
            var flatTriangles = new int[indices.Length];
            for (int index = 0; index < indices.Length; index++)
            {
                flatVertices[index] = vertices[indices[index]];
                if (colors != null) flatColors[index] = colors[indices[index]];
                flatTriangles[index] = index;
            }

            var mesh = new Mesh
            {
                name = name,
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            };

            mesh.vertices = flatVertices;
            if (colors != null) mesh.colors = flatColors;
            mesh.triangles = flatTriangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Water: opaque, lit, slightly glossy.
        ///
        /// <para>Opaque rather than transparent on purpose. Transparent water showed the sea bed
        /// through it, and the sea bed is a finite patch - so its straight edge was visible as a
        /// lighter rectangle sitting in the middle of the ocean. Depth would have to be faked in a
        /// shader to fix that properly; an opaque surface simply has no such artefact, and the
        /// shallows still read through the beach band on the terrain itself.</para>
        /// </summary>
        public static Material CreateWaterMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.153f, 0.412f, 0.616f);
            material.SetFloat("_Glossiness", 0.72f);
            material.SetFloat("_Metallic", 0.1f);
            return material;
        }

        /// <summary>Terrain material: lit, reading per-vertex colour, which Standard ignores.</summary>
        public static Material CreateTerrainMaterial()
        {
            Shader shader = Shader.Find("LifeSimulation/VertexColorLit") ?? Shader.Find("Standard");
            return new Material(shader) { color = Color.white };
        }

        /// <summary>
        /// Lighting used by both the live scene and the capture. A single directional light leaves
        /// every surface angled away from it black, which on terrain reads as hard dark bands in the
        /// geometry - it is not: an unlit render of the same mesh shows a continuous surface.
        /// </summary>
        public static void ConfigureLighting(Transform key, Transform fill)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.52f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.40f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.20f);
            RenderSettings.ambientIntensity = 1f;
            DynamicGI.UpdateEnvironment();

            if (key != null) key.rotation = Quaternion.Euler(45f, -35f, 0f);
            if (fill != null) fill.rotation = Quaternion.Euler(28f, 160f, 0f);
        }

        private const double SphereRadius = 500d;
    }
}
