using System.IO;
using LifeSimulation.Presentation;
using UnityEditor;
using UnityEngine;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// Renders the terrain views to PNG files, headlessly.
    ///
    /// <para><b>Why this exists.</b> Every other instrument here measures the <i>field</i> -
    /// elevation deciles, land fraction, biome counts, gradient continuity. None can see the
    /// <i>image</i>. Colour quantised to one value per triangle produces byte-identical field
    /// statistics, so the blockiest render and a correct one are indistinguishable to all of them.
    /// </para>
    ///
    /// <para><b>It builds through <see cref="TerrainMeshBuilder"/>, the same path the live preview
    /// uses.</b> This file previously constructed its own mesh at a different resolution, with
    /// different triangulation and its own water plane - so its PNGs were evidence about a mesh
    /// nobody was looking at, and it missed a defect where the live view had no water at all. A
    /// capture that cannot reproduce the runtime is a second implementation, not an instrument.</para>
    ///
    /// <para>Needs graphics: <c>-nographics</c> disables the rendering this depends on.</para>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///   -executeMethod LifeSimulation.EditorTools.TerrainRenderEntry.Render
    /// </code>
    /// </summary>
    public static class TerrainRenderEntry
    {
        /// <summary>Matches the user's Game view, so the framing shown is the framing they get.</summary>
        private const int Width = 976;
        private const int Height = 752;
        private const int Seed = 42;

        [MenuItem("Life Simulation/Render Terrain Views")]
        public static void Render()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "terrain");
            Directory.CreateDirectory(directory);

            var plates = new PlateStructure(Seed);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);

            RenderPatch(directory, "wide-400", plates, centreLatitude, centreLongitude, TerrainPreview.WidePatchHalfWidth);
            RenderPatch(directory, "close-200", plates, centreLatitude, centreLongitude, TerrainPreview.RegionHalfWidth);
            RenderPlanet(directory, "planet", plates);

            Debug.Log("Terrain views rendered to " + directory);
        }

        private static void RenderPatch(
            string directory, string name, PlateStructure plates,
            double centreLatitude, double centreLongitude, float halfWidth)
        {
            TerrainMeshBuilder.BuildPatch(
                Seed, plates, centreLatitude, centreLongitude,
                halfWidth, TerrainMeshBuilder.PatchHeightScale(halfWidth),
                out Vector3[] vertices, out Color[] colors, out int[] triangles);

            Capture(
                directory, name,
                TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Terrain Patch"),
                halfWidth, waterPlaneHalfWidth: halfWidth, waterSphere: false);
        }

        private static void RenderPlanet(string directory, string name, PlateStructure plates)
        {
            TerrainMeshBuilder.BuildPlanet(
                Seed, plates, out Vector3[] vertices, out Color[] colors, out int[] triangles);

            Capture(
                directory, name,
                TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Planet"),
                TerrainMeshBuilder.PlanetDrawRadius * 1.35f, waterPlaneHalfWidth: 0f, waterSphere: true);
        }

        private static void Capture(
            string directory, string name, Mesh mesh,
            float framingRadius, float waterPlaneHalfWidth, bool waterSphere)
        {
            var root = new GameObject("Capture");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateTerrainMaterial();

            GameObject water = null;
            if (waterPlaneHalfWidth > 0f)
            {
                water = GameObject.CreatePrimitive(PrimitiveType.Plane);
                water.transform.localScale = new Vector3(waterPlaneHalfWidth / 5f, 1f, waterPlaneHalfWidth / 5f);

                // Sea level is exactly zero now that elevation is signed displacement, so the plane
                // sits there rather than at an offset guessed against a threshold.
                water.transform.position = Vector3.zero;
                water.GetComponent<Renderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
            }
            else if (waterSphere)
            {
                water = new GameObject("Ocean Sphere");
                water.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
                TerrainMeshBuilder.BuildOceanSphere(out Vector3[] oceanVertices, out int[] oceanTriangles);
                water.AddComponent<MeshFilter>().sharedMesh =
                    TerrainMeshBuilder.FlatShaded(oceanVertices, null, oceanTriangles, "Ocean Sphere");
            }

            var keyObject = new GameObject("Sun");
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.3f;
            key.shadows = LightShadows.None;

            var fillObject = new GameObject("Fill");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.72f, 0.80f, 0.92f);
            fill.shadows = LightShadows.None;

            TerrainMeshBuilder.ConfigureLighting(keyObject.transform, fillObject.transform);

            var cameraObject = new GameObject("Capture Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 5000f;

            // Pitch only, no yaw. GroundPlaneCameraController has no yaw control, so a yawed capture
            // would show the terrain from an angle the Game view cannot actually produce.
            var rotation = Quaternion.Euler(52f, 0f, 0f);
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
            Object.DestroyImmediate(keyObject);
            Object.DestroyImmediate(fillObject);
            Object.DestroyImmediate(cameraObject);
            if (water != null) Object.DestroyImmediate(water);
        }
    }
}
