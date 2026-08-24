using LifeSimulation.Simulation.Core;
using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    /// <summary>
    /// A look-at-it view of the terrain generator, decoupled from the simulation arena.
    ///
    /// <para><b>Why this exists.</b> The arena is 50 units wide and carries about three landforms,
    /// far too small a sample to judge whether terrain reads at the right scale or whether biomes
    /// have any variety. The generator is a pure function of position and seed, so a viewer can
    /// render any extent - or the whole planet - without the simulation being involved.</para>
    ///
    /// <para><b>Entirely presentation.</b> Nothing here is read by anything under
    /// <c>Assets/Scripts/Simulation</c>, so it moves no hash and affects no recorded result.</para>
    ///
    /// <para><b>Mesh construction lives in <see cref="TerrainMeshBuilder"/>, shared with the offline
    /// PNG capture.</b> The two used to build their scenes separately and drifted - different
    /// resolutions, different triangulation, water in one and not the other - which quietly made the
    /// diagnostic renders evidence about a different mesh than the one on screen.</para>
    ///
    /// <para><b>Seeing the globe does not mean the world is round.</b> A spherical simulation is a
    /// spatial-model refactor; a spherical view is nearly free.</para>
    /// </summary>
    public sealed class TerrainPreview
    {
        public enum Mode
        {
            Off = 0,

            /// <summary>A wide flat patch: what the generator does over a large area.</summary>
            WidePatch = 1,

            /// <summary>A close view, near the scale the simulation actually runs at.</summary>
            Region = 2,

            /// <summary>The whole planet, as an actual sphere.</summary>
            Planet = 3,
        }

        public const float WidePatchHalfWidth = 200f;
        public const float RegionHalfWidth = 100f;

        private readonly GameObject _root;
        private readonly MeshFilter _meshFilter;
        private readonly MeshRenderer _renderer;
        private readonly Material _terrainMaterial;

        private GameObject _oceanSphere;
        private GameObject _oceanPlane;
        private float _oceanPlaneHalfWidth = float.MinValue;
        private PlateStructure _plates;
        private int _plateSeed = int.MinValue;
        private int _plateRevision = int.MinValue;
        private double _centreLatitude;
        private double _centreLongitude;
        private bool _centreChosenByCaller;

        public TerrainPreview()
        {
            _root = new GameObject("Terrain Preview");
            _meshFilter = _root.AddComponent<MeshFilter>();
            _renderer = _root.AddComponent<MeshRenderer>();
            _terrainMaterial = TerrainMeshBuilder.CreateTerrainMaterial();
            _renderer.sharedMaterial = _terrainMaterial;
            _root.SetActive(false);
        }

        public Mode Current { get; private set; } = Mode.Off;

        /// <summary>
        /// Where the flat views are centred, in radians.
        ///
        /// <para>Movable because a 400-unit window spans about one plate, so the default coastline
        /// shows whatever biomes happen to be there and no others. The planet has ice, tundra,
        /// desert, marsh, scrub and grassland; a single fixed window is not evidence about any of
        /// them except its own.</para>
        ///
        /// <para>Setting either one pins the centre, so a later plate rebuild does not drag the view
        /// back to the computed coast. <see cref="ResetCentreToCoast"/> releases it.</para>
        /// </summary>
        public double CentreLatitude
        {
            get { return _centreLatitude; }
            set
            {
                _centreLatitude = value;
                _centreChosenByCaller = true;
            }
        }

        public double CentreLongitude
        {
            get { return _centreLongitude; }
            set
            {
                _centreLongitude = value;
                _centreChosenByCaller = true;
            }
        }

        /// <summary>World seed the current plates and terrain were built from.</summary>
        public int Seed
        {
            get { return _plateSeed; }
        }

        /// <summary>The plate structure behind the current view, for a caller that wants to walk it.</summary>
        public PlateStructure Plates
        {
            get { return _plates; }
        }

        /// <summary>Return to the computed coastline: a continental plate meeting an oceanic one.</summary>
        public void ResetCentreToCoast()
        {
            _centreChosenByCaller = false;
            if (_plates != null) _plates.GetCoastalCentre(out _centreLatitude, out _centreLongitude);
        }

        /// <summary>Vertical exaggeration, relative to the shared default. Tunable at runtime.</summary>
        public float HeightScale { get; set; } = 14f;

        /// <summary>Extent of the flat view currently selected, for sizing a water plane.</summary>
        public float CurrentHalfWidth
        {
            get { return Current == Mode.Region ? RegionHalfWidth : WidePatchHalfWidth; }
        }

        /// <summary>How far the camera must pull back to see the current view.</summary>
        public float FramingRadius
        {
            get
            {
                switch (Current)
                {
                    case Mode.WidePatch:
                    case Mode.Region:
                        return CurrentHalfWidth;
                    case Mode.Planet:
                        return TerrainMeshBuilder.PlanetDrawRadius * 1.35f;
                    default:
                        return 0f;
                }
            }
        }

        public string Describe()
        {
            switch (Current)
            {
                case Mode.WidePatch:
                    return $"1/3 wide patch - {WidePatchHalfWidth * 2f:0} units of generator";
                case Mode.Region:
                    return $"2/3 close view - {RegionHalfWidth * 2f:0} units, near simulation scale";
                case Mode.Planet:
                    return "3/3 planet - the VIEW is spherical, the simulation is still flat";
                default:
                    return "off (K to open the terrain viewer)";
            }
        }

        public Mode Advance(SimulationWorld world)
        {
            // Three, not four. The fourth was a globe of its own, built as one fixed mesh at
            // draw radius 60 - and O now shows the real planet at true radius with level of detail,
            // which is the same view done properly. Two planets on two keys, one of them worse, is
            // a choice nobody wants to have to make. Mode.Planet is left in the enum because the
            // preview still knows how to build one; nothing reaches it from the key any more.
            Current = (Mode)(((int)Current + 1) % 3);
            Rebuild(world);
            return Current;
        }

        public void Hide()
        {
            Current = Mode.Off;
            Rebuild(null);
        }

        public void Rebuild(SimulationWorld world)
        {
            if (world == null || Current == Mode.Off)
            {
                _root.SetActive(false);
                SetOceanVisible(false, false, 0f);
                return;
            }

            EnsurePlates(world);
            _root.SetActive(true);

            if (Current == Mode.Planet)
            {
                TerrainMeshBuilder.BuildPlanet(
                    world.Config.WorldSeed, _plates,
                    out Vector3[] vertices, out Color[] colors, out int[] triangles);
                _meshFilter.sharedMesh = TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Planet");
                SetOceanVisible(true, false, 0f);
            }
            else
            {
                float halfWidth = CurrentHalfWidth;
                TerrainMeshBuilder.BuildPatch(
                    world.Config.WorldSeed, _plates, _centreLatitude, _centreLongitude,
                    halfWidth, TerrainMeshBuilder.PatchHeightScale(halfWidth) * (HeightScale / 14f),
                    out Vector3[] vertices, out Color[] colors, out int[] triangles);
                _meshFilter.sharedMesh = TerrainMeshBuilder.FlatShaded(vertices, colors, triangles, "Terrain Patch");
                SetOceanVisible(false, true, halfWidth);
            }

            _root.transform.position = Vector3.zero;
            _renderer.sharedMaterial = _terrainMaterial;
        }

        /// <summary>
        /// Plate structure and a view centre on a coastline.
        ///
        /// <para>Not the origin: a flat view spans about one plate, and the plate at the origin is
        /// whichever the construction put there - an oceanic one at seed 42, which rendered both flat
        /// views as open sea. Centring on a plate centre then over-corrected to 100% land with a
        /// single biome. A coast is where a continental plate meets an oceanic one, so it can be
        /// computed rather than searched for.</para>
        /// </summary>
        private void EnsurePlates(SimulationWorld world)
        {
            int seed = world.Config.WorldSeed;
            int revision = TerrainView.SettingsRevision;
            if (_plates != null && _plateSeed == seed && _plateRevision == revision) return;

            _plates = TerrainView.CreatePlates(seed);
            _plateSeed = seed;
            _plateRevision = revision;

            // A caller-chosen centre survives a plate rebuild. Otherwise moving a plate slider would
            // snap the view back to the computed coast and look like the control did nothing.
            if (!_centreChosenByCaller)
            {
                _plates.GetCoastalCentre(out _centreLatitude, out _centreLongitude);
            }
        }

        /// <summary>
        /// Sea surfaces. The planet gets a sphere at sea level and the flat views a plane at zero.
        ///
        /// <para>Both are needed because elevation is signed displacement: the sea bed is genuinely
        /// displaced downward, so with no sea surface a view renders bumpy blue sea bed and calls it
        /// water. Sea level is exactly zero, so nothing here is a guessed offset.</para>
        /// </summary>
        private void SetOceanVisible(bool sphere, bool plane, float halfWidth)
        {
            if (sphere && _oceanSphere == null)
            {
                _oceanSphere = new GameObject("Ocean Sphere");
                _oceanSphere.AddComponent<MeshFilter>();
                _oceanSphere.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
                TerrainMeshBuilder.BuildOceanSphere(out Vector3[] vertices, out int[] triangles);
                _oceanSphere.GetComponent<MeshFilter>().sharedMesh =
                    TerrainMeshBuilder.FlatShaded(vertices, null, triangles, "Ocean Sphere");
            }

            if (plane && _oceanPlane == null)
            {
                _oceanPlane = new GameObject("Ocean Plane");
                _oceanPlane.AddComponent<MeshFilter>();
                _oceanPlane.AddComponent<MeshRenderer>().sharedMaterial = TerrainMeshBuilder.CreateWaterMaterial();
            }

            // Built from the shared swell mesh, not from PrimitiveType.Plane. The capture already
            // used the swell while this used a flat primitive, so the sea in a diagnostic PNG was
            // not the sea in the Play view - the same drift that made TerrainMeshBuilder the single
            // build path in the first place. A flat primitive also reads as plastic: no variation at
            // any scale, and a visible rectangular edge exactly the size of the terrain.
            if (plane && !Mathf.Approximately(_oceanPlaneHalfWidth, halfWidth))
            {
                TerrainMeshBuilder.BuildWaterSurface(
                    halfWidth, 0f, out Vector3[] waterVertices, out int[] waterTriangles);
                _oceanPlane.GetComponent<MeshFilter>().sharedMesh =
                    TerrainMeshBuilder.SmoothShaded(waterVertices, waterTriangles, "Ocean Plane");
                _oceanPlaneHalfWidth = halfWidth;
            }

            if (_oceanSphere != null) _oceanSphere.SetActive(sphere);
            if (_oceanPlane != null)
            {
                _oceanPlane.SetActive(plane);

                // Sea level is exactly zero: elevation is signed displacement, so nothing here is a
                // guessed offset against a threshold.
                if (plane) _oceanPlane.transform.position = Vector3.zero;
            }
        }
    }
}
