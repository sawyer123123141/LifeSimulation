using System.IO;
using LifeSimulation.Presentation;
using UnityEditor;
using UnityEngine;
using LifeSimulation.Simulation.World;

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

            PlateStructure plates = TerrainView.CreatePlates(Seed);
            plates.GetCoastalCentre(out double centreLatitude, out double centreLongitude);

            RenderPatch(directory, "wide-400", plates, centreLatitude, centreLongitude, TerrainPreview.WidePatchHalfWidth);
            RenderPatch(directory, "close-200", plates, centreLatitude, centreLongitude, TerrainPreview.RegionHalfWidth);
            RenderPatch(directory, "arena-50", plates, centreLatitude, centreLongitude, 25f, withCreatures: true);
            // A high-latitude window as well as the default coast. The default centre is at -15
            // degrees and holds grassland, beach and marsh only; every other biome the palette has
            // lives further north, so a render set that never leaves the coast is not evidence about
            // the palette. 0.85 radians is the most varied close view measured on this meridian:
            // grassland 34%, scrub 29%, tundra 25%, ice 6%.
            RenderPatch(directory, "close-200-north", plates, 0.85d, centreLongitude, TerrainPreview.RegionHalfWidth);

            RenderPlanet(directory, "planet", plates, centreLatitude, centreLongitude, markPatch: false);
            RenderPlanet(directory, "planet-marked", plates, centreLatitude, centreLongitude, markPatch: true);

            Debug.Log("Terrain views rendered to " + directory);
        }

        private static void RenderPatch(
            string directory, string name, PlateStructure plates,
            double centreLatitude, double centreLongitude, float halfWidth, bool withCreatures = false)
        {
            TerrainMeshBuilder.BuildPatch(
                Seed, plates, centreLatitude, centreLongitude,
                halfWidth, TerrainMeshBuilder.PatchHeightScale(halfWidth),
                out Vector3[] vertices, out Color[] colors, out int[] triangles);

            Capture(
                directory, name,
                TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Terrain Patch"),
                halfWidth, waterPlaneHalfWidth: halfWidth, waterSphere: false, viewDirection: Vector3.zero,
                creatureScaleMarkers: withCreatures);
        }

        /// <summary>
        /// The planet, optionally with the flat views' window marked.
        ///
        /// <para>This is a test, not decoration. The flat views and the globe are supposed to be the
        /// same world at different levels of detail, but nothing had actually checked that their
        /// lat/lon mapping agrees - so a mismatch would have looked exactly like a level-of-detail
        /// difference. If the marker lands on the coastline the patch shows, they agree.</para>
        /// </summary>
        private static void RenderPlanet(
            string directory, string name, PlateStructure plates,
            double centreLatitude, double centreLongitude, bool markPatch)
        {
            TerrainMeshBuilder.BuildPlanet(
                Seed, plates, out Vector3[] vertices, out Color[] colors, out int[] triangles);

            if (markPatch)
            {
                // The wide patch spans +-halfWidth/SphereRadius radians about the centre.
                double halfAngle = TerrainPreview.WidePatchHalfWidth / 500d;
                for (int index = 0; index < vertices.Length; index++)
                {
                    Vector3 direction = vertices[index].normalized;
                    double latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));
                    double longitude = Mathf.Atan2(direction.x, direction.z);
                    double dLat = latitude - centreLatitude;
                    double dLon = Mathf.DeltaAngle((float)(centreLongitude * Mathf.Rad2Deg), (float)(longitude * Mathf.Rad2Deg)) * Mathf.Deg2Rad;

                    if (System.Math.Abs(dLat) <= halfAngle && System.Math.Abs(dLon) <= halfAngle)
                    {
                        colors[index] = Color.Lerp(colors[index], new Color(1f, 0.25f, 0.35f), 0.55f);
                    }
                }
            }

            // Face the patch centre. Without this the globe showed whichever hemisphere happened to
            // point at a fixed camera, and the flat views' region sat on the far limb - so the two
            // views were of DIFFERENT PARTS of the planet, which looks exactly like a level-of-detail
            // difference and is not one.
            double cosLatitude = System.Math.Cos(centreLatitude);
            var lookAt = new Vector3(
                (float)(cosLatitude * System.Math.Sin(centreLongitude)),
                (float)System.Math.Sin(centreLatitude),
                (float)(cosLatitude * System.Math.Cos(centreLongitude)));

            Capture(
                directory, name,
                TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Planet"),
                TerrainMeshBuilder.PlanetDrawRadius * 1.35f, waterPlaneHalfWidth: 0f, waterSphere: true,
                viewDirection: lookAt);
        }

        private static void Capture(
            string directory, string name, Mesh mesh,
            float framingRadius, float waterPlaneHalfWidth, bool waterSphere, Vector3 viewDirection,
            bool creatureScaleMarkers = false)
        {
            var root = new GameObject("Capture");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateTerrainMaterial();

            GameObject water = null;
            if (waterPlaneHalfWidth > 0f)
            {
                water = new GameObject("Water");
                water.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
                TerrainMeshBuilder.BuildWaterSurface(
                    waterPlaneHalfWidth, 0f, out Vector3[] waterVertices, out int[] waterTriangles);
                water.AddComponent<MeshFilter>().sharedMesh =
                    TerrainMeshBuilder.SmoothShaded(waterVertices, waterTriangles, "Water");

                // Sea level is exactly zero now that elevation is signed displacement.
                water.transform.position = Vector3.zero;
            }
            else if (waterSphere)
            {
                water = new GameObject("Ocean Sphere");
                water.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
                TerrainMeshBuilder.BuildOceanSphere(out Vector3[] oceanVertices, out int[] oceanTriangles);
                water.AddComponent<MeshFilter>().sharedMesh =
                    TerrainMeshBuilder.FlatShaded(oceanVertices, null, oceanTriangles, "Ocean Sphere");
            }

            // Creature-sized markers. A creature is 1 unit, and 1 unit is 1 metre by the settled
            // scale, so these are the only way an image conveys how big the terrain actually is.
            // They are scale references, not simulated creatures - the capture has no world.
            var markers = new System.Collections.Generic.List<GameObject>();
            if (creatureScaleMarkers)
            {
                Mesh terrain = mesh;
                Vector3[] surface = terrain.vertices;
                for (int index = 0; index < 40; index++)
                {
                    // Deterministic spread over the mesh, keeping only points above sea level.
                    int vertex = (int)((index * 7919L) % surface.Length);
                    int guard = 0;
                    while (surface[vertex].y <= 0.2f && guard++ < 64)
                    {
                        vertex = (vertex + 9973) % surface.Length;
                    }

                    if (surface[vertex].y <= 0.2f) continue;

                    var marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Object.DestroyImmediate(marker.GetComponent<Collider>());
                    marker.transform.position = surface[vertex] + new Vector3(0f, 0.55f, 0f);
                    marker.transform.localScale = new Vector3(0.7f, 0.55f, 0.7f);
                    var markerMaterial = new Material(Shader.Find("Standard"));
                    markerMaterial.color = new Color(0.85f, 0.25f, 0.35f);
                    marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
                    markers.Add(marker);
                }
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
            if (viewDirection == Vector3.zero)
            {
                // Flat views: pitch only, no yaw, because GroundPlaneCameraController has no yaw
                // control and a yawed capture would show an angle the Game view cannot produce.
                var rotation = Quaternion.Euler(52f, 0f, 0f);
                cameraObject.transform.rotation = rotation;
                cameraObject.transform.position = rotation * new Vector3(0f, 0f, -framingRadius * 2.1f);
            }
            else
            {
                // Planet: look straight down at the given surface point.
                cameraObject.transform.position = viewDirection.normalized * framingRadius * 2.1f;
                cameraObject.transform.rotation = Quaternion.LookRotation(-viewDirection.normalized, Vector3.up);
            }

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
            foreach (GameObject marker in markers) Object.DestroyImmediate(marker);
        }
    }
}
