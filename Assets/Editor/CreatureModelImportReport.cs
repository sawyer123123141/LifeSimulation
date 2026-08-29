using System.Linq;
using System.Text;
using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Behavior;
using UnityEditor;
using UnityEngine;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// What Unity actually made of the creature FBX files.
    ///
    /// <para>Reads the imported assets rather than the source binaries, because the question that
    /// matters for playback is not "what animation stacks are in the file" but "what clips did the
    /// importer produce, and what are they called". Those differ: a file with several takes can
    /// still import as one merged clip, and a clip name that does not exist is a silent failure at
    /// runtime - the animator is asked for a state that is not there and simply plays nothing.</para>
    ///
    /// <para>Batch-runnable so it can be checked without opening the editor:
    /// <c>Unity.exe -batchmode -quit -projectPath . -executeMethod
    /// LifeSimulation.EditorTools.CreatureModelImportReport.Report</c></para>
    /// </summary>
    public static class CreatureModelImportReport
    {
        private const string ModelFolder = "Assets/Resources/CreatureModels";

        [MenuItem("LifeSimulation/Report creature model import")]
        public static void Report()
        {
            var builder = new StringBuilder();
            builder.AppendLine("CREATURE MODEL IMPORT REPORT");

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelFolder });
            builder.AppendLine($"models found: {guids.Length}");

            foreach (string guid in guids.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

                AnimationClip[] clips = assets
                    .OfType<AnimationClip>()
                    .Where(clip => !clip.name.StartsWith("__preview__"))
                    .ToArray();
                var root = assets.OfType<GameObject>().FirstOrDefault();

                builder.AppendLine(
                    $"  {System.IO.Path.GetFileNameWithoutExtension(path)}"
                    + $" | rig={(importer == null ? "?" : importer.animationType.ToString())}"
                    + $" | clips={clips.Length}"
                    + $" | animator={(root != null && root.GetComponentInChildren<Animator>() != null)}"
                    + $" | animation={(root != null && root.GetComponentInChildren<Animation>() != null)}"
                    + $" | legacyClips={clips.Count(c => c.legacy)}"
                    + $" | skinned={(root == null ? 0 : root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length)}");
                builder.AppendLine("      " + string.Join(", ", clips.Select(c => c.name)));
            }

            Debug.Log(builder.ToString());
        }

        /// <summary>
        /// Checks every clip name <see cref="CreatureModelCatalog"/> can produce against the clips
        /// the importer actually made.
        ///
        /// <para><b>This is the guard that makes a pack swap safe.</b> The mapping is a table of
        /// strings and the failure mode of a wrong string is silent - the animator is asked for a
        /// state that is not there and the creature simply stands still. Unit tests can pin the
        /// table against itself but cannot see the assets; this can. Run it after changing the
        /// table or dropping in different models.</para>
        /// </summary>
        [MenuItem("LifeSimulation/Validate creature model catalog")]
        public static void Validate()
        {
            var builder = new StringBuilder();
            builder.AppendLine("CREATURE MODEL CATALOG VALIDATION");
            int problems = 0;

            var actions = (CreatureAction[])System.Enum.GetValues(typeof(CreatureAction));
            foreach (CreatureModelRole role in System.Enum.GetValues(typeof(CreatureModelRole)))
            {
                foreach (CreatureModelDefinition model in CreatureModelCatalog.ModelsFor(role))
                {
                    string path = $"{ModelFolder}/{model.ModelName}.fbx";
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    if (assets == null || assets.Length == 0)
                    {
                        builder.AppendLine($"  MISSING MODEL {role}/{model.ModelName} at {path}");
                        problems++;
                        continue;
                    }

                    var available = new System.Collections.Generic.HashSet<string>(
                        assets.OfType<AnimationClip>().Select(clip => clip.name));

                    var missing = actions
                        .Select(action => CreatureModelCatalog.ClipFor(action, model))
                        .Concat(new[] { model.DeathClip })
                        .Distinct()
                        .Where(name => !available.Contains(name))
                        .ToArray();

                    if (missing.Length == 0)
                    {
                        builder.AppendLine($"  OK   {role}/{model.ModelName}");
                        continue;
                    }

                    problems += missing.Length;
                    builder.AppendLine($"  FAIL {role}/{model.ModelName} missing: {string.Join(", ", missing)}");
                    builder.AppendLine($"       available: {string.Join(", ", available.OrderBy(n => n))}");
                }
            }

            builder.AppendLine(problems == 0
                ? "CATALOG VALID - every mapped clip exists on every model"
                : $"CATALOG INVALID - {problems} problem(s)");
            Debug.Log(builder.ToString());
        }
    }
}
