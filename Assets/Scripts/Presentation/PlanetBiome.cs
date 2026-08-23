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
    /// Colour and biome for a <see cref="PlanetSample"/>.
    ///
    /// <para><b>Two functions with two jobs.</b> <see cref="Shade"/> is continuous - it blends across
    /// a temperature/moisture palette, so biomes fade into each other. <see cref="Classify"/> buckets
    /// the same variables into named biomes, which counting needs and blending cannot provide. They
    /// read the same fields with the same thresholds; only one of them rounds.</para>
    ///
    /// <para><b>Why the shading changed.</b> Classify is an ordered if-chain, and using it to colour
    /// meant every biome edge was a hard step - the "random cutoffs" in the render. Worse, an
    /// if-chain makes whichever variable is tested first dominate: temperature was checked before
    /// moisture, so anything cold was Ice or Tundra however wet it was. A Whittaker-style palette
    /// indexed by the temperature/moisture <i>pair</i> and interpolated bilinearly has neither
    /// problem.</para>
    /// </summary>
    public static class PlanetBiome
    {
        /// <summary>
        /// Land palette, indexed [temperature, moisture]: rows run cold to hot, columns dry to wet.
        /// Bilinear interpolation between the sixteen entries is what removes the hard edges.
        /// </summary>
        private static readonly Color[,] LandPalette =
        {
            // coldest: ice and bare rock, wetter only means more snow
            {
                new Color(0.741f, 0.741f, 0.729f), new Color(0.808f, 0.824f, 0.831f),
                new Color(0.898f, 0.918f, 0.929f), new Color(0.965f, 0.976f, 0.984f),
            },
            // cool: steppe into taiga
            {
                new Color(0.678f, 0.639f, 0.541f), new Color(0.573f, 0.596f, 0.502f),
                new Color(0.404f, 0.518f, 0.404f), new Color(0.278f, 0.416f, 0.353f),
            },
            // temperate: scrub into deciduous and marsh
            {
                new Color(0.749f, 0.686f, 0.478f), new Color(0.561f, 0.639f, 0.365f),
                new Color(0.361f, 0.588f, 0.278f), new Color(0.267f, 0.478f, 0.373f),
            },
            // hot: desert, savanna, tropical
            {
                new Color(0.878f, 0.788f, 0.545f), new Color(0.808f, 0.729f, 0.400f),
                new Color(0.478f, 0.639f, 0.267f), new Color(0.220f, 0.435f, 0.239f),
            },
        };

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
            // Ocean: depth from the waterline down, so shelves read lighter than basins.
            if (sample.Elevation <= PlanetTerrain.SeaLevel)
            {
                float depth = Mathf.Clamp01(sample.Elevation / PlanetTerrain.SeaLevel);
                return Color.Lerp(new Color(0.043f, 0.114f, 0.243f), new Color(0.212f, 0.478f, 0.663f), depth * depth);
            }

            float land = Mathf.Clamp01((sample.Elevation - PlanetTerrain.SeaLevel) / (1f - PlanetTerrain.SeaLevel));
            Color ground = SamplePalette(sample.Temperature, sample.Moisture);

            // Beach fades out over the first stretch above the waterline instead of ending at a line.
            float beach = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.005f, 0.05f, land));
            ground = Color.Lerp(ground, new Color(0.902f, 0.847f, 0.663f), beach);

            // Snow line: driven by temperature, so it follows climate rather than a fixed altitude,
            // and fades in rather than switching.
            float snow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0.12f, sample.Temperature));
            ground = Color.Lerp(ground, new Color(0.957f, 0.969f, 0.980f), snow);

            // Exposed rock on the steepest, highest ground, so peaks are not simply pale grass.
            float rock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.88f, land)) * (1f - snow);
            ground = Color.Lerp(ground, new Color(0.451f, 0.435f, 0.412f), rock * 0.75f);

            return ground;
        }

        /// <summary>Bilinear lookup into <see cref="LandPalette"/>. This is what removes the stepping.</summary>
        private static Color SamplePalette(float temperature, float moisture)
        {
            float t = Mathf.Clamp01(temperature) * (LandPalette.GetLength(0) - 1);
            float m = Mathf.Clamp01(moisture) * (LandPalette.GetLength(1) - 1);
            int t0 = Mathf.Clamp((int)t, 0, LandPalette.GetLength(0) - 2);
            int m0 = Mathf.Clamp((int)m, 0, LandPalette.GetLength(1) - 2);
            float ft = Mathf.SmoothStep(0f, 1f, t - t0);
            float fm = Mathf.SmoothStep(0f, 1f, m - m0);

            Color cold = Color.Lerp(LandPalette[t0, m0], LandPalette[t0, m0 + 1], fm);
            Color warm = Color.Lerp(LandPalette[t0 + 1, m0], LandPalette[t0 + 1, m0 + 1], fm);
            return Color.Lerp(cold, warm, ft);
        }
    }
}
