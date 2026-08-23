using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// The terrain settings the <b>viewer</b> draws with, and the tuning panel edits.
    ///
    /// <para><b>Why this is here and not beside the generator.</b> While generation lived in
    /// Presentation it carried a mutable static of its own, which was the right trade for a panel of
    /// live sliders - there is one terrain, and threading a settings argument through every mesh
    /// builder bought nothing. Generation now lives in <c>Simulation</c>, where the same static would
    /// be behaviour-changing state outside <c>SimulationConfig</c>: invisible to the
    /// configuration hash, so two worlds with equal hashes could diverge. Simulation therefore takes
    /// its settings explicitly, and the mutable one stays here where it can only affect what is
    /// drawn.</para>
    ///
    /// <para><b>Consequence worth knowing:</b> tuning the panel changes the view, not the world. Once
    /// terrain drives the ecology, the settings the simulation uses come from its config, and making
    /// the panel edit those means rebuilding the world rather than the mesh.</para>
    /// </summary>
    public static class TerrainView
    {
        /// <summary>What the viewer draws with. Starts at the shipped defaults.</summary>
        public static TerrainSettings Settings { get; set; } = new TerrainSettings();

        /// <summary>
        /// Incremented whenever <see cref="Settings"/> changes. Anything holding derived state - a
        /// plate structure, a built mesh - caches against this as well as the seed, because plate
        /// count and continental fraction are settings and a cache keyed on the seed alone would keep
        /// serving the old planet.
        /// </summary>
        public static int SettingsRevision { get; private set; }

        /// <summary>Call after editing <see cref="Settings"/>, so caches rebuild.</summary>
        public static void MarkSettingsChanged()
        {
            SettingsRevision++;
        }

        /// <summary>Restore every tunable to the value the generator ships with.</summary>
        public static void Reset()
        {
            Settings = new TerrainSettings();
            MarkSettingsChanged();
        }

        /// <summary>The plate structure the viewer's current settings describe.</summary>
        public static PlateStructure CreatePlates(int worldSeed)
        {
            return PlateStructure.Create(worldSeed, Settings);
        }
    }
}
