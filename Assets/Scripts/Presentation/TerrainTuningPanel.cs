using System;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// A runtime panel over <see cref="PlanetTerrain.Active"/>.
    ///
    /// <para><b>Why this exists.</b> Terrain is judged by eye, against a one-metre creature, at three
    /// zoom levels that show different bands. That is not a judgement anything can make from source,
    /// and the record of this work is fifteen rounds of edit-recompile-look producing six wrong
    /// diagnoses. A slider that redraws the view in one frame answers in seconds what an edit and a
    /// domain reload answers in a minute.</para>
    ///
    /// <para><b>It changes nothing by existing.</b> Every value starts at the shipped default and
    /// <b>Reset</b> returns to it, so the terrain a player sees without opening this is exactly the
    /// terrain the generator ships. Presentation-only, so nothing here can move a hash.</para>
    ///
    /// <para><b>Frequencies are cycles per radian and a radian is 500 metres</b>, so the panel prints
    /// the wavelength in metres beside each one. A frequency alone is not readable, and reading one
    /// wrong is how <c>MaximumSlope</c> spent a session as a 3% grade.</para>
    /// </summary>
    public sealed class TerrainTuningPanel
    {
        private const float Width = 296f;

        private static readonly string[] TabNames = { "Relief", "Scale", "Climate", "Plates" };

        private Vector2 _scroll;
        private int _tab;

        public bool Visible { get; private set; }

        public void Toggle()
        {
            Visible = !Visible;
        }

        /// <summary>
        /// Draw the panel. <paramref name="onChanged"/> runs once per frame in which any value moved,
        /// and should rebuild whatever the caller draws terrain into.
        /// </summary>
        public void Draw(Action onChanged)
        {
            if (!Visible) return;

            TerrainSettings settings = PlanetTerrain.Active;
            float height = Mathf.Min(Screen.height - 24f, 640f);
            var area = new Rect(Screen.width - Width - 12f, 12f, Width, height);

            GUI.Box(area, "Terrain tuning  (J to close)");
            GUILayout.BeginArea(new Rect(area.x + 8f, area.y + 24f, area.width - 16f, area.height - 32f));

            bool changed = false;
            _tab = GUILayout.Toolbar(_tab, TabNames);
            _scroll = GUILayout.BeginScrollView(_scroll);

            switch (_tab)
            {
                case 0:
                    GUILayout.Label("Amplitudes are elevation units; 1.0 is about 30 m.");
                    changed |= Slider("Local relief", ref settings.LocalAmplitude, 0d, 0.12d, settings.LocalFrequency);
                    changed |= Slider("Micro relief", ref settings.MicroAmplitude, 0d, 0.05d, settings.MicroFrequency);
                    changed |= Slider("Fine detail", ref settings.DetailAmplitude, 0d, 0.30d, settings.DetailFrequency);
                    changed |= Slider("Rolling ground", ref settings.RollingAmplitude, 0d, 0.60d, settings.HillFrequency);
                    changed |= Slider("Mountain ranges", ref settings.RangeAmplitude, 0d, 1.00d, settings.MountainFrequency);
                    GUILayout.Space(6f);
                    GUILayout.Label("Ceiling on any band, in elevation per radian. A band above it");
                    GUILayout.Label("is clipped to it, so two clipped bands sum to a staircase.");
                    changed |= Slider("Maximum slope", ref settings.MaximumSlope, 0.5d, 20d, 0d);
                    break;

                case 1:
                    GUILayout.Label("Frequency is cycles per radian; a radian is 500 m.");
                    changed |= Slider("Local", ref settings.LocalFrequency, 10d, 200d, 0d, showWavelength: true);
                    changed |= Slider("Micro", ref settings.MicroFrequency, 40d, 400d, 0d, showWavelength: true);
                    changed |= Slider("Detail", ref settings.DetailFrequency, 2d, 40d, 0d, showWavelength: true);
                    changed |= Slider("Hills", ref settings.HillFrequency, 1d, 20d, 0d, showWavelength: true);
                    changed |= Slider("Mountains", ref settings.MountainFrequency, 1d, 12d, 0d, showWavelength: true);
                    changed |= Slider("Continents", ref settings.ContinentFrequency, 0.4d, 4d, 0d, showWavelength: true);
                    GUILayout.Space(6f);
                    changed |= Slider("Octave step", ref settings.Lacunarity, 1.5d, 3d, 0d);
                    changed |= Slider("Octave falloff", ref settings.Gain, 0.2d, 0.8d, 0d);
                    break;

                case 2:
                    GUILayout.Label("Temperature and moisture, which decide the biome.");
                    changed |= Slider("Latitude weight", ref settings.TemperatureLatitudeWeight, 0d, 1d, 0d);
                    changed |= Slider("Altitude cooling", ref settings.AltitudeCooling, 0d, 1d, 0d);
                    GUILayout.Label("Raise cooling for more snow on peaks; lower it for less ice.");
                    GUILayout.Space(6f);
                    changed |= Slider("Moisture contrast", ref settings.MoistureContrast, 1d, 4d, 0d);
                    changed |= Slider("Inland drying", ref settings.Continentality, 0d, 1.5d, 0d);
                    GUILayout.Label("Inland drying is what puts deserts inland rather than scattered.");
                    changed |= Slider("Moisture scale", ref settings.MoistureFrequency, 0.5d, 6d, 0d, showWavelength: true);
                    changed |= Slider("Climate noise", ref settings.ClimateNoiseFrequency, 0.5d, 8d, 0d, showWavelength: true);
                    changed |= Slider("Edge jitter", ref settings.JitterFrequency, 4d, 40d, 0d, showWavelength: true);
                    break;

                default:
                    GUILayout.Label("Structure comes from plates, not from noise, so these are the");
                    GUILayout.Label("controls that decide where continents are at all.");
                    int plateCount = settings.PlateCount;
                    changed |= IntSlider("Plate count", ref plateCount, 4, 60);
                    settings.PlateCount = plateCount;
                    changed |= Slider("Continental share", ref settings.ContinentalFraction, 0.05d, 0.9d, 0d);
                    GUILayout.Space(6f);
                    changed |= Slider("Coast roughness", ref settings.ShelfNoiseStrength, 0d, 1d, 0d);
                    changed |= Slider("Coast wander", ref settings.WarpStrength, 0d, 1d, 0d);
                    changed |= Slider("Wander scale", ref settings.WarpFrequency, 0.5d, 8d, 0d, showWavelength: true);
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label(settings.IsDefault() ? "defaults" : "modified");
            if (GUILayout.Button("Reset", GUILayout.Width(70f)))
            {
                PlanetTerrain.ResetSettings();
                changed = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (!changed) return;
            PlanetTerrain.MarkSettingsChanged();
            onChanged?.Invoke();
        }

        /// <summary>
        /// One labelled slider. Returns true only when the value actually moved, so a caller can
        /// rebuild meshes on change rather than every frame the panel is open.
        /// </summary>
        private static bool Slider(
            string label, ref double value, double minimum, double maximum,
            double frequencyForMetres, bool showWavelength = false)
        {
            string suffix = string.Empty;
            if (showWavelength)
            {
                suffix = $"   {Wavelength(value)}";
            }
            else if (frequencyForMetres > 0d)
            {
                // An amplitude means nothing without the wavelength it rides on: 0.036 over 9 m is
                // an undulation, the same 0.036 over 3 m is a step.
                suffix = $"   {value * 30d:0.00} m over {Wavelength(frequencyForMetres)}";
            }

            GUILayout.Label($"{label}  {value:0.000}{suffix}");
            var slider = (double)GUILayout.HorizontalSlider((float)value, (float)minimum, (float)maximum);
            GUILayout.Space(4f);

            // The slider is fed a float, so an untouched control returns the double rounded through
            // single precision - a difference of about 1e-9 on these values. An exact comparison
            // would therefore report a change every frame and rebuild the terrain continuously.
            if (Math.Abs(slider - value) <= Math.Max(1e-6d, Math.Abs(value) * 1e-5d)) return false;

            value = slider;
            return true;
        }

        private static bool IntSlider(string label, ref int value, int minimum, int maximum)
        {
            GUILayout.Label($"{label}  {value}");
            int slider = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, minimum, maximum));
            if (slider == value) return false;

            value = slider;
            return true;
        }

        /// <summary>Cycles per radian read back as a wavelength, which is the readable form.</summary>
        private static string Wavelength(double frequency)
        {
            if (frequency <= 0d) return "-";
            double metres = 500d / frequency;
            return metres >= 1000d ? $"{metres / 1000d:0.0} km" : $"{metres:0.0} m";
        }
    }
}
