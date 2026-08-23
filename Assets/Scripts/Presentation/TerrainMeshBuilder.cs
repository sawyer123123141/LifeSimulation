using UnityEngine;

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
        /// Relief on a flat patch as a fraction of its width. Real terrain is far flatter - Everest
        /// is about 0.2% of the distance it spans - but a truthful ratio reads as a plain.
        /// </summary>
        public const float PatchReliefFraction = 0.07f;

        public static float PatchHeightScale(float halfWidth)
        {
            return halfWidth * 2f * PatchReliefFraction;
        }

        /// <summary>
        /// A flat patch centred on a latitude/longitude, sampled as a window on the sphere so the
        /// flat views and the globe show the same world at different zooms.
        /// </summary>
        public static void BuildPatch(
            int seed, PlateStructure plates, double centreLatitude, double centreLongitude,
            float halfWidth, float heightScale,
            out Vector3[] vertices, out Color[] colors, out int[] triangles)
        {
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
                        maximumFrequency);

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
                    seed, plates, direction.x, direction.y, direction.z, maximumFrequency);

                vertices[index] = direction * (PlanetDrawRadius * (1f + (sample.Elevation * PlanetReliefFraction)));
                colors[index] = PlanetBiome.Shade(sample);
            }

            triangles = indices;
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

        /// <summary>Water colour, translucent so shallows read as shallow rather than as paint.</summary>
        public static Material CreateWaterMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.161f, 0.427f, 0.635f, 0.72f);

            // Standard is opaque unless switched to the transparent path explicitly; setting only the
            // alpha does nothing.
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            material.SetFloat("_Glossiness", 0.65f);
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
