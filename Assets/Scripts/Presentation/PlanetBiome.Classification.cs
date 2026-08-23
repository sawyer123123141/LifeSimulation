namespace LifeSimulation.Presentation
{
    public enum BiomeKind
    {
        Ocean = 0,
        Beach = 1,
        Ice = 2,
        Tundra = 3,
        Desert = 4,
        Marsh = 5,
        Grassland = 6,
        Scrub = 7,
    }

    /// <summary>
    /// The half of <see cref="PlanetBiome"/> that names a biome, kept free of UnityEngine types.
    ///
    /// <para><b>Why it is a separate file.</b> Colour needs Unity; naming does not. Split, the
    /// offline probe in <c>tools/TerrainProbe</c> can answer "what biomes are in this view" while the
    /// editor holds the project lock - which is exactly when someone is looking at terrain and asking
    /// that question. Undivided, the only instrument that could answer it was a menu item that
    /// refuses to start whenever the editor is open.</para>
    ///
    /// <para><b>Classify and Shade must agree.</b> They read the same fields with the same
    /// thresholds; only one of them rounds. If a threshold moves here it moves there too, or the
    /// counts stop describing the picture.</para>
    /// </summary>
    public static partial class PlanetBiome
    {
        public static BiomeKind Classify(PlanetSample sample)
        {
            if (sample.Elevation <= 0f) return BiomeKind.Ocean;

            float land = Clamp01(sample.Elevation / PlanetTerrain.HighGround);
            if (land < 0.045f) return BiomeKind.Beach;
            if (sample.Temperature < 0.18f) return BiomeKind.Ice;
            if (sample.Temperature < 0.33f) return BiomeKind.Tundra;
            if (sample.Moisture < 0.34f) return BiomeKind.Desert;
            if (sample.Moisture > 0.72f && land < 0.14f) return BiomeKind.Marsh;
            if (sample.Moisture > 0.46f) return BiomeKind.Grassland;
            return BiomeKind.Scrub;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
