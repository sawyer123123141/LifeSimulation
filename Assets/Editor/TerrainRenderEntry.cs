using System.IO;
using LifeSimulation.Presentation;
using UnityEditor;
using UnityEngine;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// Renders the terrain views to PNG files, headlessly.
    ///
    /// <para><b>Why this exists.</b> Every other instrument in this project measures the <i>field</i>
    /// - elevation deciles, land fraction, biome counts, saturation. None of them can see the
    /// <i>image</i>. A colour quantised to one value per triangle produces byte-identical field
    /// statistics, so the blockiest possible render and a perfect one are indistinguishable to
    /// everything built so far. That gap is why fixes kept being declared and then corrected.</para>
    ///
    /// <para>Run with graphics enabled - <c>-nographics</c> disables the rendering this needs:</para>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///   -executeMethod LifeSimulation.EditorTools.TerrainRenderEntry.Render
    /// </code>
    /// </summary>
    public static class TerrainRenderEntry
    {
        private const int Width = 900;
        private const int Height = 600;
        private const int Seed = 42;

        [MenuItem("Life Simulation/Render Terrain Views")]
        public static void Render()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "terrain");
            Directory.CreateDirectory(directory);

            var plates = new PlateStructure(Seed);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);

            RenderPatch(directory, "wide-400", plates, centreLatitude, centreLongitude, 200f);
            RenderPatch(directory, "close-200", plates, centreLatitude, centreLongitude, 100f);
            RenderPatch(directory, "arena-50", plates, centreLatitude, centreLongitude, 25f);
            RenderPlanet(directory, "planet", plates);

            Debug.Log("Terrain views rendered to " + directory);
        }

        private static void RenderPatch(
            string directory, string name, PlateStructure plates,
            double centreLatitude, double centreLongitude, float halfWidth)
        {
            const int side = 321;
            double angularWidth = 2d * halfWidth / 500d;
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor((int)(side * (2d * Mathf.PI) / angularWidth));
            float heightScale = halfWidth * 2f * 0.07f;

            float cell = halfWidth * 2f / (side - 1);
            var vertices = new Vector3[side * side];
            var colors = new Color[side * side];
            for (int row = 0; row < side; row++)
            {
                float z = Mathf.Lerp(-halfWidth, halfWidth, row / (float)(side - 1));
                for (int column = 0; column < side; column++)
                {
                    float x = Mathf.Lerp(-halfWidth, halfWidth, column / (float)(side - 1));
                    // Jitter the sample position sideways. A perfectly regular grid gives every
                    // triangle the same shape and orientation, so flat shading turns a steep slope
                    // into repeating corduroy stripes rather than facets. Offsetting each vertex
                    // within its own cell breaks that regularity without moving the field: the
                    // terrain is unchanged, only where it is sampled.
                    float jx = (Hash01(column, row, 17) - 0.5f) * cell * 0.28f;
                    float jz = (Hash01(column, row, 31) - 0.5f) * cell * 0.28f;
                    x += jx;
                    z += jz;
                    PlanetSample sample = PlanetTerrain.SampleAtLatLon(
                        Seed, plates,
                        centreLatitude + (z / 500d), centreLongitude + (x / 500d), maximumFrequency);

                    int vertex = (row * side) + column;
                    // Signed, not clamped. Clamping the sea floor to zero put a vertical cliff at
                    // every waterline - land dropped straight to a flat plane, and the shoreline
                    // stair-stepped because adjacent vertices jumped from land height to exactly 0.
                    // Letting the sea bed go below zero makes the coast a continuous slope, which is
                    // what a beach is. Depth is compressed relative to height so the ocean reads as
                    // shallow shelf rather than a pit.

                    // One multiply: elevation is already signed displacement from sea level.
                    float height = sample.Elevation * heightScale;
                    vertices[vertex] = new Vector3(x, height, z);
                    colors[vertex] = PlanetBiome.Shade(sample);
                }
            }

            var triangles = new int[(side - 1) * (side - 1) * 6];
            int triangle = 0;
            for (int row = 0; row + 1 < side; row++)
            {
                for (int column = 0; column + 1 < side; column++)
                {
                    int bottomLeft = (row * side) + column;
                    int topLeft = bottomLeft + side;
                    // Alternate the diagonal per cell. With one fixed diagonal every quad splits the
                    // same way, which is the other half of the corduroy.
                    bool flip = ((row + column) & 1) == 0;
                    if (flip)
                    {
                        triangles[triangle++] = bottomLeft;
                        triangles[triangle++] = topLeft;
                        triangles[triangle++] = bottomLeft + 1;
                        triangles[triangle++] = bottomLeft + 1;
                        triangles[triangle++] = topLeft;
                        triangles[triangle++] = topLeft + 1;
                    }
                    else
                    {
                        triangles[triangle++] = bottomLeft;
                        triangles[triangle++] = topLeft;
                        triangles[triangle++] = topLeft + 1;
                        triangles[triangle++] = bottomLeft;
                        triangles[triangle++] = topLeft + 1;
                        triangles[triangle++] = bottomLeft + 1;
                    }

                }
            }

            Capture(directory, name, BuildFlatShaded(vertices, colors, triangles), halfWidth, halfWidth);
        }

        private static void RenderPlanet(string directory, string name, PlateStructure plates)
        {
            const int subdivisions = 5;
            IcoSphere.Build(subdivisions, out Vector3[] directions, out int[] indices);
            double maximumFrequency = PlanetTerrain.MaximumFrequencyFor(IcoSphere.SamplesAroundEquator(subdivisions));

            var vertices = new Vector3[directions.Length];
            var colors = new Color[directions.Length];
            for (int index = 0; index < directions.Length; index++)
            {
                Vector3 direction = directions[index];
                PlanetSample sample = PlanetTerrain.Sample(Seed, plates, direction.x, direction.y, direction.z, maximumFrequency);
                float relief = 1f + (sample.Elevation * 0.075f);
                vertices[index] = direction * (60f * relief);
                colors[index] = PlanetBiome.Shade(sample);
            }

            Capture(directory, name, BuildFlatShaded(vertices, colors, indices), 78f, 0f);
        }

        /// <summary>Unshared vertices per triangle: one normal per face, colour still per corner.</summary>
        private static Mesh BuildFlatShaded(Vector3[] vertices, Color[] colors, int[] indices)
        {
            var flatVertices = new Vector3[indices.Length];
            var flatColors = new Color[indices.Length];
            var flatTriangles = new int[indices.Length];
            for (int index = 0; index < indices.Length; index++)
            {
                flatVertices[index] = vertices[indices[index]];
                flatColors[index] = colors[indices[index]];
                flatTriangles[index] = index;
            }

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = flatVertices;
            mesh.colors = flatColors;
            mesh.triangles = flatTriangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }


        /// <summary>Deterministic 0..1 from a grid cell, for position jitter.</summary>
        private static float Hash01(int x, int y, int salt)
        {
            unchecked
            {
                int h = (x * 73856093) ^ (y * 19349663) ^ (salt * 83492791);
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        private static void Capture(string directory, string name, Mesh mesh, float framingRadius, float waterHalfWidth)
        {
            var root = new GameObject("Capture");
            var filter = root.AddComponent<MeshFilter>();
            var renderer = root.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;

            // Diagnostic switch. Unlit removes lighting entirely: if a dark band survives that, it is
            // geometry - a hole or a fold - and not a shading artefact. Guessing between the two cost
            // several rounds.
            bool unlit = System.Environment.GetCommandLineArgs().Length > 0
                && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-terrainUnlit") >= 0;
            Shader shader = unlit
                ? Shader.Find("Unlit/Color")
                : Shader.Find("LifeSimulation/VertexColorLit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (unlit) material.color = new Color(0.6f, 0.75f, 0.55f);
            renderer.sharedMaterial = material;

            GameObject water = null;
            if (waterHalfWidth > 0f)
            {
                water = GameObject.CreatePrimitive(PrimitiveType.Plane);
                water.transform.localScale = new Vector3(waterHalfWidth / 5f, 1f, waterHalfWidth / 5f);

                // Slightly below the waterline. At exactly y=0 the water plane is coplanar with the
                // terrain wherever the terrain crosses sea level - the entire shoreline - and the two
                // surfaces z-fight, which renders as a striped comb following the coast contour.
                water.transform.position = new Vector3(0f, -0.35f, 0f);
                var waterMaterial = new Material(Shader.Find("Standard"));
                waterMaterial.color = new Color(0.176f, 0.404f, 0.588f);
                water.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            }

            // Ambient light. A batchmode scene has no lighting setup, so ambient is zero and any face
            // angled away from the single directional light renders pure black. On a steep slope,
            // alternating facets face toward and away from the sun, which produced alternating black
            // stripes that looked exactly like a terrain defect - and survived four separate attempts
            // to fix the field, because the field was never the cause. The runtime view has ambient;
            // the capture must too, or it is not showing what the player sees.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.52f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.40f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.20f);
            RenderSettings.ambientIntensity = 1f;

            // Setting RenderSettings is not enough: the ambient probe is baked from them and must be
            // regenerated, or shaders keep sampling the old (black) probe. Without this the ambient
            // colours above are ignored entirely, which is why they appeared to change nothing.
            DynamicGI.UpdateEnvironment();

            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;

            // Self-shadowing test. Hard-edged black bands on a steep slope are textbook shadow acne:
            // a surface shadowing itself because the depth bias is too small for the mesh scale.
            light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);


            // Fill light from the opposite side. Ambient alone did not reach the shader in batchmode -
            // setting RenderSettings and regenerating the probe both changed nothing - so faces
            // pointing away from the key light received no illumination at all and rendered as hard
            // black bands. A second dim light guarantees every face gets something, which is what
            // ambient was supposed to do and is robust to however the probe is plumbed.
            var fillObject = new GameObject("Fill");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.72f, 0.80f, 0.92f);
            fill.shadows = LightShadows.None;
            fillObject.transform.rotation = Quaternion.Euler(28f, 160f, 0f);

            var cameraObject = new GameObject("Capture Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 5000f;

            // 35 degrees looks across the relief rather than straight down at it, which is the angle
            // that makes slope legible; straight down flattens everything to a colour map.
            var rotation = Quaternion.Euler(35f, 30f, 0f);
            cameraObject.transform.rotation = rotation;
            cameraObject.transform.position = rotation * new Vector3(0f, 0f, -framingRadius * 2.1f);

            var target = new RenderTexture(Width, Height, 24);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());

            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(lightObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(fillObject);
            if (water != null) Object.DestroyImmediate(water);
        }
    }
}
