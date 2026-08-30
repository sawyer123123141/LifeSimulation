using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// Creates the one scene the project needs and puts it in the build list.
    ///
    /// <para><b>The scene is deliberately empty.</b> <c>Prototype1Presenter</c> carries a
    /// <c>[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]</c> that creates itself if no instance is
    /// present, and it then builds its own camera, lights, terrain, water and creature views in code.
    /// Nothing needs to be wired by hand, so a scene with objects in it would be a second place for
    /// the setup to live and a second place for it to drift - the failure decision 10 already
    /// records for terrain.</para>
    ///
    /// <para><b>Why it exists at all.</b> <c>EditorBuildSettings</c> had <c>m_Scenes: []</c>, so a
    /// built player had no scene to load and would start on nothing. In the editor this never showed,
    /// because pressing Play on any open scene is enough for the bootstrap to fire.</para>
    ///
    /// <para>Generated rather than saved by hand so it is reproducible: delete the scene, run this,
    /// and the result is identical.</para>
    /// </summary>
    public static class SceneBootstrap
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = SceneFolder + "/Prototype1.unity";

        [MenuItem("Life Simulation/Create the bootstrap scene")]
        public static void CreateBootstrapScene()
        {
            if (!Directory.Exists(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            UnityEngine.Debug.Log($"SCENE created {ScenePath} and registered it as the only build scene");
        }
    }
}
