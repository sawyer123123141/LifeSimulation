using UnityEngine;

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
    /// Biome from a <see cref="PlanetSample"/>, and the colour that represents it.
    ///
    /// <para>Extracted from the renderer so that the terrain statistics dump classifies with exactly
    /// the same code that draws. Measuring one classifier while looking at another would make the
    /// numbers meaningless, which is the whole point of instrumenting this.</para>
    ///
    /// <para><b>Known limitation, to be fixed rather than tuned around.</b> This is an ordered
    /// if-chain, so whichever variable is tested first dominates: temperature is checked before
    /// moisture, so anything cold is Ice or Tundra no matter how wet it is. A Whittaker-style
    /// classification takes the temperature/moisture <i>pair</i> and does not have that ordering
    /// artefact. Recorded in docs/terrain-brainstorm-2026-08-23.md.</para>
    /// </summary>
    public static class PlanetBiome
    {
        public static BiomeKind Classify(PlanetSample sample)
        {
            if (sample.Elevation <= PlanetTerrain.SeaLevel) return BiomeKind.Ocean;

            float land = Mathf.Clamp01((sample.Elevation - PlanetTerrain.SeaLevel) / (1f - PlanetTerrain.SeaLevel));
            if (land < 0.045f) return BiomeKind.Beach;
            if (sample.Temperature < 0.18f) return BiomeKind.Ice;
            if (sample.Temperature < 0.33f) return BiomeKind.Tundra;
            if (sample.Moisture < 0.34f) return BiomeKind.Desert;
            if (sample.Moisture > 0.72f && land < 0.14f) return BiomeKind.Marsh;
            if (sample.Moisture > 0.46f) return BiomeKind.Grassland;
            return BiomeKind.Scrub;
        }

        public static Color Shade(PlanetSample sample)
        {
            float land = sample.Elevation <= PlanetTerrain.SeaLevel
                ? 0f
                : Mathf.Clamp01((sample.Elevation - PlanetTerrain.SeaLevel) / (1f - PlanetTerrain.SeaLevel));

            switch (Classify(sample))
            {
                case BiomeKind.Ocean:
                {
                    float depth = Mathf.Clamp01(sample.Elevation / PlanetTerrain.SeaLevel);
                    return Color.Lerp(new Color(0.035f, 0.106f, 0.235f), new Color(0.180f, 0.451f, 0.647f), depth);
                }

                case BiomeKind.Beach:
                    return new Color(0.902f, 0.831f, 0.639f);
                case BiomeKind.Ice:
                    return Color.Lerp(new Color(0.86f, 0.90f, 0.93f), Color.white, land);
                case BiomeKind.Tundra:
                    return Color.Lerp(new Color(0.498f, 0.584f, 0.659f), new Color(0.62f, 0.66f, 0.68f), land);
                case BiomeKind.Desert:
                    return Color.Lerp(new Color(0.878f, 0.769f, 0.478f), new Color(0.706f, 0.588f, 0.376f), land);
                case BiomeKind.Marsh:
                    return new Color(0.259f, 0.435f, 0.388f);
                case BiomeKind.Grassland:
                    return Color.Lerp(new Color(0.325f, 0.612f, 0.243f), new Color(0.239f, 0.408f, 0.220f), land);
                default:
                    return Color.Lerp(new Color(0.588f, 0.549f, 0.361f), new Color(0.463f, 0.435f, 0.396f), land);
            }
        }
    }
}
