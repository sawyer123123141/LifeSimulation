using UnityEditor;

namespace LifeSimulation.EditorTools
{
    /// <summary>
    /// Import settings for the creature models, enforced in code rather than in metadata.
    ///
    /// <para><b>Why this exists as a script and not as checked-in import settings.</b> Unity
    /// normally records per-asset import settings in the neighbouring <c>.meta</c> file, but this
    /// repository intentionally does not track <c>.meta</c> files (handoff section 8). A fresh
    /// clone therefore re-imports these models with Unity's defaults. Anything the models need that
    /// differs from the default has to live here, or it is a setting that works on one machine and
    /// silently does not on the next.</para>
    ///
    /// <para><b>Legacy animation, deliberately.</b> The default Generic rig produces clips that can
    /// only be played through an <c>AnimatorController</c> asset, and authoring twelve controllers
    /// by hand in the editor is exactly the kind of step that cannot be scripted, reviewed or
    /// reproduced. Legacy clips play by name off an <c>Animation</c> component with no authored
    /// asset at all, which is what lets the whole pipeline be driven from code and verified
    /// headlessly. The playback call is isolated behind one method in the view layer, so moving to
    /// Mecanim later is a change in one place rather than a rewrite.</para>
    /// </summary>
    public sealed class CreatureModelPostprocessor : AssetPostprocessor
    {
        /// <summary>Kept in sync with <c>CreatureModelCatalog.ResourcePath</c> by the validator.</summary>
        private const string ModelFolder = "Assets/Resources/CreatureModels/";

        private void OnPreprocessModel()
        {
            if (assetPath == null || assetPath.IndexOf(ModelFolder, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;

            // The simulation already owns every creature's position, so nothing here may move a
            // transform on its own.
            importer.importConstraints = false;

            // Nothing in this project lights these models yet and the pack ships no textures, so
            // materials would import as unused assets that still have to be resolved on every
            // import. Colour comes from the view layer's per-creature tint.
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
}
