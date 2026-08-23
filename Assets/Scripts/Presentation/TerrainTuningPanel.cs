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

        /// <summary>
        /// Style for the line under each slider. Built on first draw rather than in a field, because
        /// <c>GUI.skin</c> only exists inside OnGUI.
        /// </summary>
        private GUIStyle _hint;

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
            if (_hint == null)
            {
                // Every control says what it does in a few words. A slider labelled only
                // "Continentality" is a control nobody touches, and the whole point of the panel is
                // that these are judged by eye rather than derived.
                _hint = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    wordWrap = true,
                    padding = new RectOffset(2, 2, 0, 4),
                };
                _hint.normal.textColor = new Color(0.72f, 0.75f, 0.78f);
            }

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
                    GUILayout.Label("How tall each layer of relief is. 1.0 is about 30 m.", _hint);
                    changed |= Slider(
                        "Local relief", ref settings.LocalAmplitude, 0d, 0.12d, settings.LocalFrequency,
                        "Undulations you walk over. The band that made the close view bumpy.");
                    changed |= Slider(
                        "Micro relief", ref settings.MicroAmplitude, 0d, 0.05d, settings.MicroFrequency,
                        "Ankle-scale texture. Only exists in the close view and the arena.");
                    changed |= Slider(
                        "Fine detail", ref settings.DetailAmplitude, 0d, 0.30d, settings.DetailFrequency,
                        "Roughness across a whole hillside.");
                    changed |= Slider(
                        "Rolling ground", ref settings.RollingAmplitude, 0d, 0.60d, settings.HillFrequency,
                        "Broad hills over all land, so interiors are not plateaus.");
                    changed |= Slider(
                        "Mountain ranges", ref settings.RangeAmplitude, 0d, 1.00d, settings.MountainFrequency,
                        "Peaks along plate margins. Zero gives a world with no mountains.");
                    GUILayout.Space(6f);
                    changed |= Slider(
                        "Maximum slope", ref settings.MaximumSlope, 0.5d, 20d, 0d,
                        "Steepest ground the mesh can draw. A ceiling, not a target: a band above it "
                        + "is clipped to it, and two clipped bands sum to a staircase.");
                    break;

                case 1:
                    GUILayout.Label("How wide each layer's features are. The metres are the size.", _hint);
                    changed |= Slider(
                        "Local", ref settings.LocalFrequency, 10d, 200d, 0d,
                        "Width of an undulation.", showWavelength: true);
                    changed |= Slider(
                        "Micro", ref settings.MicroFrequency, 40d, 400d, 0d,
                        "Width of a ground bump. Past what a view can draw, it vanishes.",
                        showWavelength: true);
                    changed |= Slider(
                        "Detail", ref settings.DetailFrequency, 2d, 40d, 0d,
                        "Width of surface roughness.", showWavelength: true);
                    changed |= Slider(
                        "Hills", ref settings.HillFrequency, 1d, 20d, 0d,
                        "Distance from one hilltop to the next.", showWavelength: true);
                    changed |= Slider(
                        "Mountains", ref settings.MountainFrequency, 1d, 12d, 0d,
                        "Spacing of peaks within a range.", showWavelength: true);
                    changed |= Slider(
                        "Continents", ref settings.ContinentFrequency, 0.4d, 4d, 0d,
                        "Size of a landmass. Low means few, large continents.", showWavelength: true);
                    GUILayout.Space(6f);
                    changed |= Slider(
                        "Octave step", ref settings.Lacunarity, 1.5d, 3d, 0d,
                        "How much finer each added layer of noise is.");
                    changed |= Slider(
                        "Octave falloff", ref settings.Gain, 0.2d, 0.8d, 0d,
                        "How much fainter each added layer is. High is rougher ground.");
                    break;

                case 2:
                    GUILayout.Label("Temperature and moisture together pick the biome colour.", _hint);
                    changed |= Slider(
                        "Latitude weight", ref settings.TemperatureLatitudeWeight, 0d, 1d, 0d,
                        "How much latitude sets temperature. Low scatters climate at random.");
                    changed |= Slider(
                        "Altitude cooling", ref settings.AltitudeCooling, 0d, 1d, 0d,
                        "How cold high ground gets. This is the ice control.");
                    GUILayout.Space(6f);
                    changed |= Slider(
                        "Moisture contrast", ref settings.MoistureContrast, 1d, 4d, 0d,
                        "Spread between wettest and driest. Low means no deserts at all.");
                    changed |= Slider(
                        "Inland drying", ref settings.Continentality, 0d, 1.5d, 0d,
                        "How dry interiors get. This is what puts deserts inland.");
                    changed |= Slider(
                        "Moisture scale", ref settings.MoistureFrequency, 0.5d, 6d, 0d,
                        "Size of a wet or dry region.", showWavelength: true);
                    changed |= Slider(
                        "Climate noise", ref settings.ClimateNoiseFrequency, 0.5d, 8d, 0d,
                        "Size of warm and cool patches away from latitude.", showWavelength: true);
                    changed |= Slider(
                        "Edge jitter", ref settings.JitterFrequency, 4d, 40d, 0d,
                        "Raggedness of biome borders, so they look grown not drawn.",
                        showWavelength: true);
                    break;

                default:
                    GUILayout.Label(
                        "Land comes from tectonic plates, not from noise. These decide where "
                        + "continents are at all, so they change the world rather than tune it.", _hint);
                    int plateCount = settings.PlateCount;
                    changed |= IntSlider(
                        "Plate count", ref plateCount, 4, 60,
                        "Fewer plates means larger continents and longer mountain ranges.");
                    settings.PlateCount = plateCount;
                    changed |= Slider(
                        "Continental share", ref settings.ContinentalFraction, 0.05d, 0.9d, 0d,
                        "How many plates carry land. The main control on how much ocean there is.");
                    GUILayout.Space(6f);
                    changed |= Slider(
                        "Coast roughness", ref settings.ShelfNoiseStrength, 0d, 1d, 0d,
                        "Bays and headlands. Zero gives a smooth, artificial shoreline.");
                    changed |= Slider(
                        "Coast wander", ref settings.WarpStrength, 0d, 1d, 0d,
                        "How far a coast strays from the straight plate edge underneath it.");
                    changed |= Slider(
                        "Wander scale", ref settings.WarpFrequency, 0.5d, 8d, 0d,
                        "Length of one meander in that wander.", showWavelength: true);
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
        private bool Slider(
            string label, ref double value, double minimum, double maximum,
            double frequencyForMetres, string description = null, bool showWavelength = false)
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
            if (!string.IsNullOrEmpty(description)) GUILayout.Label(description, _hint);
            GUILayout.Space(4f);

            // The slider is fed a float, so an untouched control returns the double rounded through
            // single precision - a difference of about 1e-9 on these values. An exact comparison
            // would therefore report a change every frame and rebuild the terrain continuously.
            if (Math.Abs(slider - value) <= Math.Max(1e-6d, Math.Abs(value) * 1e-5d)) return false;

            value = slider;
            return true;
        }

        private bool IntSlider(string label, ref int value, int minimum, int maximum, string description = null)
        {
            GUILayout.Label($"{label}  {value}");
            int slider = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, minimum, maximum));
            if (!string.IsNullOrEmpty(description)) GUILayout.Label(description, _hint);
            GUILayout.Space(4f);
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
