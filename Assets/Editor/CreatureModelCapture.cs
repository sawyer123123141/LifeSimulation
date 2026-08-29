using System.IO;
using System.Linq;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Behavior;
using UnityEditor;
using UnityEngine;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// Renders the creature models to a PNG so they can actually be looked at.
    ///
    /// <para><b>Why this exists.</b> Scale, orientation and whether a tint reads on a given mesh
    /// cannot be established by compiling. A model that arrives a hundred times too large, or
    /// facing sideways, or with no material and therefore invisible, produces a clean compile, a
    /// passing test suite and a completely broken screen. The project already learned this once -
    /// `docs/terrain-brief-review-2026-08-24.md` records PNGs becoming "evidence about a mesh
    /// nobody was looking at". This is the cheap way to look.</para>
    ///
    /// <para>Needs a real graphics device, so run it WITHOUT <c>-nographics</c>:
    /// <c>Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// LifeSimulation.EditorTools.CreatureModelCapture.CaptureAll</c></para>
    /// </summary>
    public static class CreatureModelCapture
    {
        private const string OutputFolder = "Logs/creature-models";
        private const int Width = 1280;
        private const int Height = 720;

        [MenuItem("LifeSimulation/Capture creature models")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);

            var staged = new System.Collections.Generic.List<GameObject>();
            var root = new GameObject("CaptureRoot");

            try
            {
                var lightObject = new GameObject("CaptureLight");
                lightObject.transform.SetParent(root.transform);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                var cameraObject = new GameObject("CaptureCamera");
                cameraObject.transform.SetParent(root.transform);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.18f, 0.20f, 0.24f);

                // Every model in the catalog, laid out in a row, each sitting on the same ground
                // plane so relative size is readable at a glance. Spacing is generous because the
                // question being answered is "how big are these", and models that overlap cannot
                // answer it.
                var models = System.Enum.GetValues(typeof(CreatureModelRole))
                    .Cast<CreatureModelRole>()
                    .SelectMany(role => CreatureModelCatalog.ModelsFor(role).Select(m => (role, m)))
                    .ToArray();

                const float spacing = 4f;
                for (int index = 0; index < models.Length; index++)
                {
                    (CreatureModelRole role, CreatureModelDefinition definition) = models[index];
                    var prefab = Resources.Load<GameObject>($"{CreatureModelCatalog.ResourcePath}/{definition.ModelName}");
                    if (prefab == null)
                    {
                        Debug.LogError($"capture: could not load {definition.ModelName}");
                        continue;
                    }

                    GameObject instance = Object.Instantiate(prefab, root.transform);
                    instance.name = definition.ModelName;
                    instance.transform.position = new Vector3(index * spacing, 0f, 0f);
                    instance.transform.rotation = Quaternion.Euler(0f, definition.YawOffsetDegrees, 0f);
                    instance.transform.localScale = Vector3.one * definition.ModelScale;
                    staged.Add(instance);

                    ReportBounds(role, definition, instance);
                }

                // A one-unit cube at the origin, for scale. "The wolf is 0.9 units tall" is a fact
                // a picture can carry only if there is something known in the picture.
                GameObject reference = GameObject.CreatePrimitive(PrimitiveType.Cube);
                reference.name = "OneUnitReference";
                reference.transform.SetParent(root.transform);
                reference.transform.position = new Vector3(-spacing, 0.5f, 0f);

                float centre = (models.Length - 1) * spacing * 0.5f;
                Render(camera, new Vector3(centre, 6f, -22f), Quaternion.Euler(12f, 0f, 0f), "all-models-front.png");
                Render(camera, new Vector3(centre, 26f, -6f), Quaternion.Euler(70f, 0f, 0f), "all-models-top.png");
                Render(camera, new Vector3(centre - 24f, 4f, 6f), Quaternion.Euler(6f, 75f, 0f), "all-models-side.png");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Debug.Log($"capture: wrote {staged.Count} models to {OutputFolder}");
        }

        /// <summary>
        /// The numbers behind the picture. A render says "that looks wrong"; the bounds say by how
        /// much, which is what turns a look into a value for <c>ModelScale</c>.
        /// </summary>
        private static void ReportBounds(CreatureModelRole role, CreatureModelDefinition definition, GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError($"capture: {definition.ModelName} has NO renderer");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            int materialCount = renderers.Sum(r => r.sharedMaterials.Count(m => m != null));
            var animation = instance.GetComponentInChildren<Animation>();
            Debug.Log(
                $"MODELSIZE {role}/{definition.ModelName}"
                + $" size=({bounds.size.x:0.00}, {bounds.size.y:0.00}, {bounds.size.z:0.00})"
                + $" materials={materialCount}"
                + $" clips={(animation == null ? 0 : animation.GetClipCount())}");
        }

        private static void Render(Camera camera, Vector3 position, Quaternion rotation, string fileName)
        {
            camera.transform.SetPositionAndRotation(position, rotation);

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                readback.Apply();
                File.WriteAllBytes(Path.Combine(OutputFolder, fileName), readback.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(readback);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}
