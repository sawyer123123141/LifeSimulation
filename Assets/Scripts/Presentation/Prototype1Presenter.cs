using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    public sealed partial class Prototype1Presenter : MonoBehaviour
    {
        private const int HeatmapResolution = 128;
        private const float BreedingThresholdFraction = 0.7f;
        private const float HeatmapUpdateInterval = 2f;
        private const float ColdTemperature = 12f;
        private const float HotTemperature = 28f;

        private static readonly SimVector2[] DemoFounderPositions =
        {
            new SimVector2(-12.4f, -8.4f),
            new SimVector2(-11.6f, -8.4f),
            new SimVector2(-12.4f, -7.6f),
            new SimVector2(-11.6f, -7.6f),
        };

        private static readonly SimVector2[] PredationFounderPositions =
        {
            new SimVector2(-13f, -9f), new SimVector2(-11f, -9f), new SimVector2(-9f, -9f), new SimVector2(-7f, -9f),
            new SimVector2(-13f, -6f), new SimVector2(-11f, -6f), new SimVector2(-9f, -6f), new SimVector2(-7f, -6f),
            new SimVector2(-12f, -3f), new SimVector2(-10f, -3f), new SimVector2(-8f, -3f), new SimVector2(-6f, -3f),
        };

        private readonly Dictionary<CreatureId, Transform> _creatureViews = new Dictionary<CreatureId, Transform>();

        /// <summary>
        /// Renderers cached alongside the view transforms. `SynchronizePresentation` set the colour
        /// through `GetComponent&lt;Renderer&gt;()` on every creature and every resource EVERY frame -
        /// a component lookup per object per frame, which at the populations the ecology now reaches
        /// (200-500, against the 9-17 the only Play-mode profile ever saw) is several hundred
        /// lookups a frame for a value that is already known at creation time.
        /// </summary>
        private readonly Dictionary<CreatureId, Renderer> _creatureRenderers = new Dictionary<CreatureId, Renderer>();

        /// <summary>
        /// The legacy <c>Animation</c> component on each creature's model, when it has one. Absent
        /// for the capsule fallback, which is the whole reason it is a separate dictionary rather
        /// than a field on a view struct - a world with no model pack still has to run.
        /// </summary>
        private readonly Dictionary<CreatureId, Animation> _creatureAnimations = new Dictionary<CreatureId, Animation>();

        /// <summary>
        /// What each creature was last seen doing, so a clip is only crossfaded when the action
        /// actually changes. Playing every frame would restart the animation every frame and hold
        /// every creature on the first pose of its clip.
        /// </summary>
        private readonly Dictionary<CreatureId, CreatureAction> _creatureActions = new Dictionary<CreatureId, CreatureAction>();

        /// <summary>Model definition chosen for each creature, fixed at birth because the genome is.</summary>
        private readonly Dictionary<CreatureId, CreatureModelDefinition> _creatureModels = new Dictionary<CreatureId, CreatureModelDefinition>();

        /// <summary>
        /// Last heading each creature actually had, in degrees.
        ///
        /// <para>Kept because a creature that is standing still has no heading to compute - the
        /// step between its previous and current position is zero - and a model that snaps back to
        /// facing north every time it pauses to eat looks broken. Holding the last real heading
        /// means it keeps facing the way it was going.</para>
        /// </summary>
        private readonly Dictionary<CreatureId, float> _creatureHeadings = new Dictionary<CreatureId, float>();
        private readonly List<CreatureId> _staleCreatureIds = new List<CreatureId>();
        private readonly List<Transform> _resourceViews = new List<Transform>();

        /// <summary>Resource renderers, cached for the same reason as <see cref="_creatureRenderers"/>.</summary>
        private readonly List<Renderer> _resourceRenderers = new List<Renderer>();
        private SimulationWorld _world;
        private Camera _simulationCamera;
        private Renderer _terrainRenderer;
        private Texture2D _temperatureHeatmap;
        private MeshFilter _terrainMeshFilter;
        private Mesh _terrainMesh;
        private GameObject _waterSurface;
        private PlateStructure _arenaPlates;
        private int _arenaPlateSeed = int.MinValue;
        private int _arenaPlateRevision = int.MinValue;
        private double _arenaCentreLatitude;
        private double _arenaCentreLongitude;
        private Material _arenaTerrainMaterial;

        /// <summary>Set while a terrain preview is open, so the fixed-pixel HUD does not cover it.</summary>
        private bool _hudHidden;
        private bool _pausedBeforePlanetView;
        private float _planetRebuildDue;

        /// <summary>
        /// Look-at-it view of the fields, decoupled from the 50-unit arena. Cycled with <c>K</c>.
        /// Presentation only - see <see cref="TerrainPreview"/>.
        /// </summary>
        private TerrainPreview _terrainPreview;

        /// <summary>Runtime tuning over <see cref="TerrainView.Settings"/>. Presentation only.</summary>
        private readonly TerrainTuningPanel _terrainTuningPanel = new TerrainTuningPanel();
        private Color[] _temperaturePixels;
        private Color _terrainColor;
        private float _accumulator;
        private float _heatmapUpdateAccumulator;
        private float _speedMultiplier = 4f;
        private bool _isPaused;
        private bool _showTemperatureHeatmap = true;
        private TerrainOverlay _overlay = TerrainOverlay.Temperature;
        private ResourceId _draggedResourceId;
        private bool _isDraggingResource;
        private string _scenarioId;
        private string _scenarioHint;
        private CreatureId _selectedCreature;
        private bool _hasSelectedCreature;

        /// <summary>
        /// History of what the selected creature has been doing. An outside observer: it reads the
        /// world and the world never reads it, so watching a creature cannot change the run. A test
        /// pins that by fingerprinting an observed and an unobserved world.
        /// </summary>
        private readonly CreatureActionHistory _selectedCreatureHistory = new CreatureActionHistory();
        private SimulationEvent _recentEvent;
        private bool _hasRecentEvent;
        private P5HistoryPanelSession _p5HistorySession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfNeeded()
        {
            if (FindFirstObjectByType<Prototype1Presenter>() == null)
            {
                new GameObject("Prototype 1 Presenter").AddComponent<Prototype1Presenter>();
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            // A script edit while Play mode is running triggers a Unity domain reload, which
            // resets non-serialized fields like _world to their defaults without re-invoking
            // Awake() on GameObjects that already existed in the scene. Without this guard,
            // every subsequent Update/OnGUI throws a NullReferenceException on _world forever.
            if (_world != null)
            {
                if (_p5HistorySession == null)
                {
                    _p5HistorySession = P5HistoryPanelSession.CreateForWorld(_world);
                }

                return;
            }
            CreateEnvironment();
            ResetSimulation(Prototype1Scenarios.Baseline);
        }

        private void Update()
        {
            EnsureInitialized();
            SamplePerformance();
            HandleInput();
            if (!_isPaused)
            {
                _accumulator += Time.unscaledDeltaTime * _speedMultiplier;
                int stepLimit = 200;
                while (_accumulator >= _world.Config.FixedDeltaTime && stepLimit-- > 0)
                {
                    _world.Step(_world.Config.FixedDeltaTime);
                    _p5HistorySession.Advance(_world);
                    ObserveSelectedCreature();
                    CaptureRecentEvent();
                    _world.Events.Clear();
                    _accumulator -= _world.Config.FixedDeltaTime;
                }
            }

            // Wall time, not simulated time. This used to advance inside the step loop by
            // FixedDeltaTime, so at speed 8 the overlay rebuilt eight times as often - and each
            // rebuild was measured at 192.95 ms. The refresh rate of a diagnostic overlay has no
            // business scaling with the simulation speed multiplier.
            _heatmapUpdateAccumulator += Time.unscaledDeltaTime;
            UpdatePlanetRebuild();
            UpdateTemperatureHeatmapIfNeeded();
            SynchronizePresentation();
        }

        private void DrawP5HistoryPanel()
        {
            const int maximumRows = 8;
            const float panelX = 756f;
            const float panelY = 12f;
            const float lineHeight = 22f;
            ClusterHistoryPolicy policy = _p5HistorySession.Policy;
            GUI.Box(new Rect(panelX, panelY, 520f, 340f), "P5 history evidence");
            GUI.Label(new Rect(panelX + 12f, panelY + 28f, 500f, lineHeight), _p5HistorySession.StatusText);
            GUI.Label(new Rect(panelX + 12f, panelY + 50f, 500f, lineHeight),
                $"Threshold {P5HistoryPanelSession.GeneticThreshold:0.00} · cadence {P5HistoryPanelSession.ObservationIntervalTicks} ticks · mode full population");
            GUI.Label(new Rect(panelX + 12f, panelY + 72f, 500f, lineHeight),
                $"Ancestry {(_p5HistorySession.AncestryIsComplete ? "complete" : "incomplete")} through tick {_p5HistorySession.AncestryCompleteThroughTick} · output {_p5HistorySession.DisplayEventCount}/{_p5HistorySession.OutputCapacity}");
            GUI.Label(new Rect(panelX + 12f, panelY + 94f, 500f, lineHeight),
                $"Analysis settings: current {policy.MinimumSupportedCurrentMembers}/{policy.MinimumCurrentSupportFraction:P0} · prior {policy.MinimumSupportingPreviousMembers}/{policy.MinimumPreviousSupportFraction:P0} · depth {policy.MaximumAncestorGenerations} · persistence {policy.RequiredSuccessorObservations}/{policy.RequiredAbsentObservations}");
            int hiddenRoutineCount = _p5HistorySession.HiddenRoutineContinuityCount;
            string routineNote = hiddenRoutineCount == 0
                ? string.Empty
                : $"    ({hiddenRoutineCount} routine continuities hidden)";
            GUI.Label(new Rect(panelX + 12f, panelY + 116f, 500f, lineHeight), $"Latest evidence (newest first):{routineNote}");

            // Routine confirmed continuity is a heartbeat, not an event, and floods the bounded
            // panel. It stays in the analytical history; only these eight rows are filtered.
            int rowCount = Mathf.Min(maximumRows, _p5HistorySession.NotableEventCount);
            for (int row = 0; row < rowCount; row++)
            {
                ClusterHistoryEvent historyEvent = _p5HistorySession.GetNotableEventAt(_p5HistorySession.NotableEventCount - 1 - row);
                GUI.Label(new Rect(panelX + 12f, panelY + 138f + (row * lineHeight), 500f, lineHeight), FormatP5HistoryEvent(historyEvent));
            }
        }

        private static string FormatP5HistoryEvent(ClusterHistoryEvent historyEvent)
        {
            string status = historyEvent.Status == ClusterHistoryEventStatus.Candidate
                ? "candidate"
                : historyEvent.Status == ClusterHistoryEventStatus.Confirmed
                    ? "confirmed"
                    : "unresolved";
            string kind = historyEvent.Kind == ClusterHistoryEventKind.ConfirmedLineageExtinction
                ? "lineage extinction evidence"
                : historyEvent.Kind.ToString();
            string tickRange = historyEvent.FirstObservedTick == historyEvent.LastObservedTick
                ? $"tick {historyEvent.FirstObservedTick}"
                : $"ticks {historyEvent.FirstObservedTick}-{historyEvent.LastObservedTick}";
            string evidenceNote = GetP5EvidenceNote(historyEvent);
            return $"{status}: {kind} · {tickRange} · {FormatP5Tracks(historyEvent)}{evidenceNote}";
        }

        private static string FormatP5Tracks(ClusterHistoryEvent historyEvent)
        {
            string tracks = "tracks";
            for (int index = 0; index < historyEvent.PreviousTrackCount; index++)
            {
                tracks += $" #{historyEvent.GetPreviousTrackIdAt(index)}";
            }

            if (historyEvent.PreviousTrackCount > 0 && historyEvent.CurrentTrackCount > 0)
            {
                tracks += " →";
            }

            for (int index = 0; index < historyEvent.CurrentTrackCount; index++)
            {
                tracks += $" #{historyEvent.GetCurrentTrackIdAt(index)}";
            }

            return tracks;
        }

        private static string GetP5EvidenceNote(ClusterHistoryEvent historyEvent)
        {
            if (!historyEvent.EventHistoryIsComplete || !historyEvent.AncestryCoverageIsComplete)
            {
                return " · ancestry incomplete";
            }

            if (historyEvent.IsSampled)
            {
                return " · sampled observation";
            }

            if (historyEvent.UnresolvedReason == ClusterHistoryUnresolvedReason.LivingDescendant)
            {
                return " · living descendant";
            }

            if (historyEvent.UnresolvedReason == ClusterHistoryUnresolvedReason.AmbiguousStrongRelations)
            {
                return " · ambiguous reorganisation";
            }

            return string.Empty;
        }

        private string FormatRecentEvent()
        {
            if (_recentEvent.Kind == SimulationEventKind.Birth)
            {
                return $"Latest birth: #{_recentEvent.Subject.Value}";
            }

            return $"Latest death: #{_recentEvent.Subject.Value} ({_recentEvent.DeathCause})";
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _isPaused = !_isPaused;
            }

            // Flying takes the letter keys. WASDQE are movement while the right button is held, and
            // three of them - D drought, E starter habitat, F food scarcity - also restart the
            // simulation, so without this, strafing right threw away the run being watched. The
            // camera reads the same button, so the two agree on when flight is happening.
            if (Input.GetMouseButton(1)) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) _speedMultiplier = 1f;
            if (Input.GetKeyDown(KeyCode.Alpha2)) _speedMultiplier = 2f;
            if (Input.GetKeyDown(KeyCode.Alpha4)) _speedMultiplier = 4f;
            if (Input.GetKeyDown(KeyCode.Alpha8)) _speedMultiplier = 8f;
            if (Input.GetKeyDown(KeyCode.B)) ResetSimulation(Prototype1Scenarios.Baseline);
            if (Input.GetKeyDown(KeyCode.D)) ResetSimulation(Prototype1Scenarios.Drought);
            if (Input.GetKeyDown(KeyCode.F)) ResetSimulation(Prototype1Scenarios.FoodScarcity);
            if (Input.GetKeyDown(KeyCode.P)) ResetPredationSimulation();
            if (Input.GetKeyDown(KeyCode.C)) ResetCognitionSimulation();
            if (Input.GetKeyDown(KeyCode.T)) ResetPhysiologySimulation();
            if (Input.GetKeyDown(KeyCode.G)) ResetForagingMemoryDemo();
            if (Input.GetKeyDown(KeyCode.M)) ResetMatingDemo();
            if (Input.GetKeyDown(KeyCode.E)) ResetWatchableStarterHabitat();
            if (Input.GetKeyDown(KeyCode.Alpha5)) ResetObservationScenario(Prototype4Scenarios.ObservationStable, foundersAreMature: false, mateSelectionEnabled: false);
            if (Input.GetKeyDown(KeyCode.Alpha6)) ResetObservationScenario(Prototype4Scenarios.ObservationScarcity, foundersAreMature: false, mateSelectionEnabled: false);
            if (Input.GetKeyDown(KeyCode.Alpha7)) ResetObservationScenario(Prototype4Scenarios.ObservationMigration, foundersAreMature: false, mateSelectionEnabled: false);
            if (Input.GetKeyDown(KeyCode.Alpha9)) ResetObservationScenario(Prototype4Scenarios.ObservationMating, foundersAreMature: true, mateSelectionEnabled: true);
            // R is the matched partner of 5: identical scenario, seed and config except for the
            // home-range flag, so the two can be watched back to back.
            if (Input.GetKeyDown(KeyCode.R)) ResetObservationScenario(Prototype4Scenarios.ObservationStable, foundersAreMature: false, mateSelectionEnabled: false, homeRangeAffinityEnabled: true);
            // V is the only scenario whose food map changes while you watch: plant patches die and
            // seedlings establish on dormant sites. It runs seed 45 rather than the usual 42
            // because seed 42's founders are one of six cases in thirty that fail to establish in
            // this layout - see docs/experiments/p4a-shifting-patches-2026-08-22.md.
            if (Input.GetKeyDown(KeyCode.V)) ResetObservationScenario(Prototype4Scenarios.ObservationShiftingPatches, foundersAreMature: false, mateSelectionEnabled: false, plantMortalityEnabled: true, worldSeed: 45);
            // Y is the terrain scenario: procedural environment fields with the elevation channel and
            // its lapse rate on. Press H until the overlay reaches Elevation to see the relief.
            if (Input.GetKeyDown(KeyCode.Y)) ResetTerrainPlaytest();
            if (Input.GetKeyDown(KeyCode.N)) ResetAllFlagsPlaytestSimulation();
            if (Input.GetKeyDown(KeyCode.H)) ToggleTemperatureHeatmap();
            HandleTerrainTuningInput();
            if (Input.GetKeyDown(KeyCode.O))
            {
                _sphericalArena = !_sphericalArena;

                // Everything the toggle needs is driven from here, not from the terrain mesh path.
                // That path early-returns to a flat arena whenever the elevation field is off - which
                // most scenarios are - so hanging the planet off it meant pressing this key did
                // nothing at all except sag the creatures by the sagitta, which is invisible on
                // purpose.
                ArenaProjection.Spherical = _sphericalArena;
                EnsureArenaPlates();
                UpdatePlanetBackdrop();
                RebuildTerrainViews();
                ApplyCameraRange();
                ApplyPlanetView(_sphericalArena);
            }

            if (Input.GetKeyDown(KeyCode.J)) _terrainTuningPanel.Toggle();
            if (Input.GetKeyDown(KeyCode.K) && _terrainPreview != null)
            {
                _terrainPreview.HeightScale = _terrainHeightScale;
                ApplyTerrainPreviewMode(_terrainPreview.Advance(_world));
            }
            if (Input.GetMouseButtonDown(0) && !TryBeginResourceDrag()) TrySelectCreature();
            if (Input.GetMouseButton(0)) UpdateResourceDrag();
            if (Input.GetMouseButtonUp(0)) _isDraggingResource = false;
        }

        private void CreateEnvironment()
        {
            var terrain = new GameObject("Prototype Terrain");
            _terrainMeshFilter = terrain.AddComponent<MeshFilter>();
            _terrainRenderer = terrain.AddComponent<MeshRenderer>();
            _terrainRenderer.material = new Material(Shader.Find("Standard"));
            _terrainMesh = new Mesh { name = "Prototype Terrain Mesh" };
            _terrainMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _terrainMeshFilter.sharedMesh = _terrainMesh;
            BuildTerrainMesh();
            CreateWaterSurface();
            _terrainPreview = new TerrainPreview();
            _terrainColor = new Color(0.16f, 0.28f, 0.16f);
            _terrainRenderer.material.color = _terrainColor;
            _temperatureHeatmap = new Texture2D(HeatmapResolution, HeatmapResolution, TextureFormat.RGBA32, false);
            _temperatureHeatmap.wrapMode = TextureWrapMode.Clamp;
            _temperatureHeatmap.filterMode = FilterMode.Bilinear;
            _temperaturePixels = new Color[HeatmapResolution * HeatmapResolution];

            // Ambient light. Without it the only illumination is the directional light below, so any
            // surface angled away from it renders black - which on terrain means steep slopes turn
            // into hard dark bands that read as blocky cutoffs in the geometry. They are not: an
            // unlit render of the same mesh shows a continuous surface with no banding at all.
            //
            // Setting these is not sufficient on its own; the ambient probe is baked from them and
            // has to be regenerated, or shaders keep sampling the previous black probe.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.52f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.40f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.20f);
            RenderSettings.ambientIntensity = 1f;
            DynamicGI.UpdateEnvironment();

            var directionalLight = new GameObject("Sun").AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.25f;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Fill light. With one light, every surface facing away from it renders black, which on
            // terrain turns steep slopes into hard dark bands that read as blocky cutoffs in the
            // geometry - they are not, as an unlit render of the same mesh shows a continuous
            // surface. Ambient settings alone did not reach the shader, so this guarantees it.
            var fillLight = new GameObject("Fill Light").AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.55f;
            fillLight.color = new Color(0.72f, 0.80f, 0.92f);
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(28f, 160f, 0f);

            var cameraObject = new GameObject("Simulation Camera");
            _simulationCamera = cameraObject.AddComponent<Camera>();
            _simulationCamera.orthographic = false;
            _simulationCamera.fieldOfView = 55f;
            cameraObject.AddComponent<FreeFlyCameraController>();
            _simulationCamera.backgroundColor = new Color(0.06f, 0.09f, 0.13f);

        }

        private void ResetSimulation(SimulationScenario scenario, SimulationConfig config = null)
        {
            foreach (KeyValuePair<CreatureId, Transform> creatureView in _creatureViews)
            {
                Destroy(creatureView.Value.gameObject);
            }

            _creatureViews.Clear();
            _creatureRenderers.Clear();
            _creatureAnimations.Clear();
            _creatureActions.Clear();
            _creatureModels.Clear();
            _creatureHeadings.Clear();
            for (int index = 0; index < _resourceViews.Count; index++)
            {
                Destroy(_resourceViews[index].gameObject);
            }

            _resourceViews.Clear();
            _resourceRenderers.Clear();
            _world = new SimulationWorld(config ?? CreatePlayableConfig(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: 4)));
            scenario.ApplyTo(_world);
            _rawElevationValid = false;
            // The relief is a function of this world's seed and flags, so it is rebuilt per scenario
            // rather than once at startup.
            BuildTerrainMesh();
            UpdateWaterSurface();
            _p5HistorySession = P5HistoryPanelSession.CreateForWorld(_world);
            _scenarioId = scenario.Id;
            _scenarioHint = GetScenarioHint(scenario.Id);
            _accumulator = 0f;
            _heatmapUpdateAccumulator = HeatmapUpdateInterval;
            _hasSelectedCreature = false;
            _hasRecentEvent = false;
            for (int index = 0; index < _world.Resources.Count; index++)
            {
                CreateResourceView(_world.Resources.GetAt(index));
            }

            ArrangeDemoFounders();
            UpdateTemperatureHeatmapIfNeeded();
            SynchronizePresentation();
        }

        /// <summary>
        /// Ground overlay modes, cycled with <c>H</c>. Biome reads the simulation's own
        /// <c>EnvironmentField</c>, so with <c>ProceduralEnvironmentFieldsEnabled</c> off it renders
        /// the flat legacy environment honestly rather than pretending there is terrain.
        /// </summary>
        private enum TerrainOverlay
        {
            None = 0,
            Temperature = 1,
            Biome = 2,

            /// <summary>
            /// Raw elevation, shaded water -> lowland -> upland -> peak. Reads
            /// <c>EnvironmentSample.Elevation</c>, which is <b>zero unless
            /// <c>SimulationConfig.ElevationFieldEnabled</c> is set</c> - so with the flag off this
            /// renders a flat sea rather than inventing terrain that the simulation does not have.
            /// Press <c>Y</c> for a scenario that turns the flag on.
            /// </summary>
            Elevation = 3,
        }

        /// <summary>Sea level as a fraction of the 0..1 elevation range, for the overlay only.</summary>
        private const float OverlaySeaLevel = 0.38f;

        /// <summary>Biome palette, matching docs/experiments/field-atlas.html so the game view and the atlas agree.</summary>
        private static readonly Color[] BiomeColors =
        {
            new Color(0.784f, 0.635f, 0.290f), // arid
            new Color(0.498f, 0.584f, 0.659f), // cold steppe
            new Color(0.290f, 0.490f, 0.431f), // marsh
            new Color(0.247f, 0.561f, 0.227f), // fertile grassland
            new Color(0.604f, 0.561f, 0.369f), // scrub
        };

        private static int ClassifyBiome(EnvironmentSample sample)
        {
            if (sample.Moisture < 0.34f) return 0;
            if (sample.Temperature < 0.42f) return 1;
            if (sample.Moisture > 0.74f && sample.Fertility < 0.48f) return 2;
            if (sample.Fertility > 0.58f) return 3;
            return 4;
        }

        /// <summary>Vertices per side of the ground mesh. 129 gives 0.39-unit quads across the arena.</summary>
        private const int TerrainResolution = 129;

        /// <summary>Half the arena width, matching the simulation's hardcoded (-25, 25) bounds.</summary>
        private const float TerrainHalfWidth = 25f;

        /// <summary>
        /// World units of relief between sea level and the highest ground. <b>Tunable at runtime</b>
        /// with <c>[</c> and <c>]</c>, because the right value cannot be reasoned out - it depends on
        /// how the relief reads next to a 1-unit creature, which only looking at it can settle.
        ///
        /// <para>Starts at 14 rather than the original 5. A landform here is roughly 16.7 units wide
        /// (<c>FeaturesAcrossArena = 3</c> over a 50-unit arena), so at height 5 a "mountain" was
        /// 17 wide and 5 tall - a squat lump beside a creature 1 unit tall. 14 gives a slope that
        /// reads as terrain rather than as a bump.</para>
        /// </summary>
        /// <summary>
        /// Whether the arena is drawn curved onto the planet it is a window on.
        ///
        /// <para>Presentation only. The simulation stays a flat 50-unit square with Euclidean
        /// distances - see <see cref="ArenaProjection"/> - so this toggles what is on screen and
        /// nothing else.</para>
        /// </summary>
        private bool _sphericalArena;

        private GameObject _planetBackdrop;
        private PlanetChunkedSurface _planetSurface;
        private int _planetSurfaceSeed = int.MinValue;
        private int _planetSurfaceRevision = int.MinValue;

        private float _terrainHeightScale = 14f;

        /// <summary>
        /// Radius, in world units, of the box filter applied to elevation before displacing the
        /// mesh. <b>Tunable at runtime</b> with <c>,</c> and <c>.</c>
        ///
        /// <para>This exists because the elevation field carries detail below creature scale: five
        /// octaves at lacunarity 2.15 put the finest features at 16.7 / 2.15^4 = <b>0.78 units</b>,
        /// smaller than a creature, and a 0.39-unit mesh resolves that perfectly - so the ground
        /// renders visibly noisy. Filtering is done <b>here rather than in the field</b> because the
        /// field is simulation state that feeds the lapse rate; smoothing it there would change
        /// ecology, while smoothing the mesh changes only what you see.</para>
        /// </summary>
        private float _terrainSmoothingRadius = 1.2f;


        /// <summary>Cached smoothed height grid, TerrainResolution x TerrainResolution, in world units.</summary>
        private float[] _terrainHeights;

        /// <summary>
        /// Raw elevation samples, cached so tuning never resamples the field.
        ///
        /// <para>Sampling is the expensive half by a wide margin - <c>EnvironmentField.Sample</c>
        /// runs several multi-octave warped-fBm evaluations per call - while applying a height scale
        /// is a multiply. Keeping them apart is what makes <c>[</c> and <c>]</c> instant instead of
        /// rebuilding 16,000 noise samples per keypress.</para>
        /// </summary>
        private float[] _rawElevation;
        private int _rawElevationSeed = int.MinValue;
        private bool _rawElevationValid;
        private float _blurredForRadius = -1f;
        private float[] _blurredElevation;

        /// <summary>
        /// Sample the elevation field onto the mesh grid, then blur it <i>on the grid</i>.
        ///
        /// <para>The earlier version averaged a fixed 3x3 of point samples spread across the
        /// smoothing radius, which is not a blur at all: as the radius grows those three taps move
        /// apart and <b>undersample</b> the field, synthesising a new jagged pattern instead of
        /// removing one. Turning smoothing up made the ground rougher. A box filter needs sample
        /// density proportional to its radius, so the fix is to sample once per grid cell and then
        /// filter neighbouring cells, which is both correct and independent of radius in cost.</para>
        ///
        /// <para>Filtering happens here rather than in <c>EnvironmentField</c> because the field is
        /// simulation state feeding the lapse rate: smoothing it there would change ecology and every
        /// elevation-on hash, while smoothing the mesh changes only what is drawn.</para>
        /// </summary>
        private void RebuildTerrainHeights()
        {
            int side = TerrainResolution;
            int cells = side * side;
            if (_terrainHeights == null || _terrainHeights.Length != cells) _terrainHeights = new float[cells];

            if (_world == null || !_world.Config.ElevationFieldEnabled)
            {
                System.Array.Clear(_terrainHeights, 0, _terrainHeights.Length);
                return;
            }

            EnsureRawElevation(side, cells);
            EnsureBlurredElevation(side, cells);

            for (int index = 0; index < cells; index++)
            {
                _terrainHeights[index] = Mathf.Max(0f, _blurredElevation[index] - OverlaySeaLevel) / (1f - OverlaySeaLevel) * _terrainHeightScale;
            }
        }

        /// <summary>
        /// A flat water surface at sea level.
        ///
        /// <para>Deliberately its <b>own</b> mesh object rather than part of the ground, so that
        /// animating it later - per-frame vertex displacement, or a shader - is purely additive and
        /// touches nothing else. Water is presentation only; nothing under
        /// <c>Assets/Scripts/Simulation</c> will ever know it exists, so no amount of animation can
        /// affect a hash, a test or determinism.</para>
        /// </summary>
        private void CreateWaterSurface()
        {
            if (_waterSurface != null) return;

            _waterSurface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _waterSurface.name = "Water Surface";
            Destroy(_waterSurface.GetComponent<Collider>());
            _waterSurface.transform.localScale = new Vector3(5f, 1f, 5f);

            var material = _waterSurface.GetComponent<Renderer>().material;
            material.color = new Color(0.157f, 0.408f, 0.616f, 0.78f);
            UpdateWaterSurface();
        }

        private void ToggleTemperatureHeatmap()
        {
            _overlay = (TerrainOverlay)(((int)_overlay + 1) % 4);
            _showTemperatureHeatmap = _overlay != TerrainOverlay.None;
            if (_showTemperatureHeatmap)
            {
                _heatmapUpdateAccumulator = HeatmapUpdateInterval;
                UpdateTemperatureHeatmapIfNeeded();
                ApplyTemperatureHeatmap();
                return;
            }

            _terrainRenderer.material.mainTexture = null;
            _terrainRenderer.material.color = _terrainColor;
        }

        private void ApplyTemperatureHeatmap()
        {
            _terrainRenderer.material.mainTexture = _temperatureHeatmap;
            _terrainRenderer.material.color = Color.white;
        }

        private void OnDestroy()
        {
            if (_temperatureHeatmap != null)
            {
                Destroy(_temperatureHeatmap);
            }
        }

        private void ResetAllFlagsPlaytestSimulation()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: PredationFounderPositions.Length);
            ResetSimulation(
                Prototype1Scenarios.Baseline,
                new SimulationConfig(
                    defaults.WorldSeed,
                    defaults.InitialPopulation,
                    defaults.Schedule,
                    maximumPopulation: 150,
                    founderProfile: FounderProfile.PredationVariation,
                    decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                    plantCohortsEnabled: true,
                    predationEconomicsEnabled: true,
                    decisionStaggerEnabled: true,
                    multiThreatPerceptionEnabled: true,
                    restBehaviorEnabled: true,
                    juvenileCapabilityEnabled: true,
                    parentalFollowingEnabled: true,
                    kinRecognitionEnabled: true,
                    // LearnedResourceQualityEnabled only affects the Legacy+CognitionEnabled decision
                    // path (see DecisionSystem.DecideFromLearnedOutcomes) - this scenario runs
                    // IntentUtilityV1, so the flag is inert here. Included for completeness/future-proofing
                    // in case the policy ever changes, not because it does anything right now.
                    learnedResourceQualityEnabled: true,
                    mateSelectionEnabled: true,
                    plantSiteCompetitionEnabled: true,
                    plantMortalityEnabled: true,
                    plantDefenseDeterrenceEnabled: true,
                    plantQualityPreferenceEnabled: true,
                    plantTemperatureAdaptationEnabled: true,
                    proceduralEnvironmentFieldsEnabled: true));
            for (int index = 0; index < _world.CreatureCount; index++)
            {
                _world.Creatures.GetNeedsRefAt(index).Age = ReproductionSystem.AdultAgeSeconds;
            }
            _scenarioId = "all-flags-playtest";
        }

        private void ResetPredationSimulation()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: PredationFounderPositions.Length);
            ResetSimulation(
                Prototype1Scenarios.Baseline,
                new SimulationConfig(
                    defaults.WorldSeed,
                    defaults.InitialPopulation,
                    defaults.Schedule,
                    maximumPopulation: 150,
                    founderProfile: FounderProfile.PredationVariation,
                    decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                    predationEconomicsEnabled: true));
        }

        private void ResetCognitionSimulation()
        {
            ResetSimulation(
                Prototype1Scenarios.Baseline,
                CreatePlayableConfig(SimulationConfig.CreatePrototype2Defaults(worldSeed: 42, initialPopulation: PredationFounderPositions.Length)));
        }

        private void ResetPhysiologySimulation()
        {
            ResetSimulation(
                Prototype1Scenarios.Baseline,
                CreatePlayableConfig(SimulationConfig.CreatePrototype3Defaults(worldSeed: 42, initialPopulation: PredationFounderPositions.Length)));
        }

        private void ResetForagingMemoryDemo()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype2Defaults(worldSeed: 42, initialPopulation: PredationFounderPositions.Length);
            ResetSimulation(
                Prototype1Scenarios.ForagingMemoryDemo,
                new SimulationConfig(
                    defaults.WorldSeed,
                    defaults.InitialPopulation,
                    defaults.Schedule,
                    defaults.MaximumPopulation,
                    defaults.FounderProfile,
                    cognitionEnabled: true,
                    physiologyEnabled: false,
                    decisionPolicyVersion: DecisionPolicyVersion.Legacy,
                    plantCohortsEnabled: false,
                    foragingEconomicsEnabled: true));
            _scenarioId = "foraging-memory-demo";
        }

        private void ResetMatingDemo()
        {
            ResetSimulation(
                Prototype1Scenarios.Baseline,
                CreatePlayableConfig(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: DemoFounderPositions.Length)));
            for (int index = 0; index < _world.CreatureCount; index++)
            {
                _world.Creatures.GetNeedsRefAt(index).Age = 21f;
            }
            _scenarioId = "mature-mating-demo";
        }

        private void ResetWatchableStarterHabitat()
        {
            ResetObservationScenario(Prototype4Scenarios.WatchableStarterHabitat, foundersAreMature: true, mateSelectionEnabled: true);
        }

        /// <summary>
        /// Terrain playtest: procedural environment fields with the elevation channel and its lapse
        /// rate enabled, so <c>H</c>'s Elevation overlay shows real relief rather than a flat sea.
        ///
        /// <para><b>Population cap 96, not the usual 40.</b> Two measured reasons. Elevation is
        /// inert at cap 48 because patches sit near capacity and growth is multiplied by
        /// <c>(1 - Biomass/Capacity)</c>, so a change to the growth *limit* has nothing to act on
        /// (docs/experiments/p4-elevation-field-2026-08-19.md); heavier grazing is what lets the
        /// limit matter. But the cap cannot simply be removed - the herbivore configuration goes
        /// 0/6 extinct at cap 96 and 5/6 at cap 200, because past that the population overshoots and
        /// starves (docs/experiments/p4-cap-pinning-audit-2026-08-22.md). 96 is the highest cap
        /// measured to survive.</para>
        ///
        /// <para>Fertility adaptation is on for the other measured reason: fertility binds the
        /// growth <c>Min</c> at 82-90% of plant-reachable positions, so while it is the smallest
        /// channel a colder crest changes nothing at all.</para>
        /// </summary>
        private void ResetTerrainPlaytest()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed: 42, initialPopulation: 4);
            var config = new SimulationConfig(
                defaults.WorldSeed,
                defaults.InitialPopulation,
                defaults.Schedule,
                maximumPopulation: 96,
                defaults.FounderProfile,
                defaults.CognitionEnabled,
                defaults.PhysiologyEnabled,
                DecisionPolicyVersion.IntentUtilityV1,
                defaults.PlantCohortsEnabled,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: true,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                // The playtest is the one scenario where the ground being looked at and the ground
                // being simulated ought to be the same ground. Safe to switch on here only because
                // the local band exists: without it this field is nearly constant across a 50-unit
                // window and the temperature heatmap below would show one flat colour.
                terrainDrivenEnvironmentEnabled: true,
                // And a hill costs something to climb. Measured across three conditions before being
                // switched on anywhere: it destabilises nothing at a sane population cap, moves no
                // gene, and puts creatures on flatter ground - the mechanism's own prediction, at
                // t = -2.09 with a sign test agreeing. See
                // docs/experiments/p6-slope-cost-cap100-2026-08-24.md. The configuration default
                // stays false; this is a scenario choice, and every recorded plant result was
                // measured without it.
                slopeMovementCostEnabled: true,
                // And the cold at the top of it is real. Same three conditions, same discipline:
                // 80 seeds at each of moderate, lean and scarce found no detected survival cost
                // (105 extinct of 240 against 94, z = 1.02) while halving the selection on
                // temperature tolerance and doubling the spread between worlds - the standard
                // deviation of the endpoint across 40 worlds goes 0.074 to 0.145. This is the
                // scenario whose whole purpose is that terrain means something, and a creature that
                // ignores altitude and latitude contradicts it. See
                // docs/experiments/p6-terrain-temperature-2026-08-24.md. Configuration default stays
                // false; every recorded thermal result was measured without it.
                terrainDrivenTemperatureEnabled: true,
                // And an injury is an injury rather than a life sentence. Health only ever
                // decremented - five subtractions in NeedsSystem and no addition anywhere - and it is
                // one of the three conditions on the mate-seeking gate, so losing a fifth of it meant
                // permanent sterility. Measured across the same three conditions: extinctions
                // 94 of 240 to 82, better at moderate and lean and identical at scarce, never worse.
                // See docs/experiments/p6-health-recovery-2026-08-24.md. Configuration default stays
                // false; every recorded result predates it.
                healthRecoveryEnabled: true);
            ResetSimulation(Prototype4Scenarios.ConsumerDefenseCalibrationModerate, config);
            _scenarioId = "p6-terrain-playtest";
            _scenarioHint = "Watch: press H to reach the Elevation overlay";
            _overlay = TerrainOverlay.Elevation;
            _showTemperatureHeatmap = true;
            _heatmapUpdateAccumulator = HeatmapUpdateInterval;
            UpdateTemperatureHeatmapIfNeeded();
            ApplyTemperatureHeatmap();
        }

        private static string GetScenarioHint(string scenarioId)
        {
            if (scenarioId == "p4-observation-stable") return "Watch: two sustainable patches";
            if (scenarioId == "p4-observation-scarcity") return "Watch: local resources run low";
            if (scenarioId == "p4-observation-migration") return "Watch: travel toward richer patches";
            if (scenarioId == "p4-observation-mating") return "Watch: purple courtship and births";
            if (scenarioId == "p4a-observation-shifting-patches") return "Watch: food patches die and regrow elsewhere";
            if (scenarioId == "p4-watchable-starter-habitat") return "Watch: a compact mixed habitat";
            return string.Empty;
        }

        private void ResetObservationScenario(
            SimulationScenario scenario,
            bool foundersAreMature,
            bool mateSelectionEnabled,
            bool homeRangeAffinityEnabled = false,
            bool plantMortalityEnabled = false,
            int worldSeed = 42)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, initialPopulation: 4);
            var config = new SimulationConfig(
                defaults.WorldSeed,
                defaults.InitialPopulation,
                defaults.Schedule,
                maximumPopulation: 40,
                defaults.FounderProfile,
                defaults.CognitionEnabled,
                defaults.PhysiologyEnabled,
                DecisionPolicyVersion.IntentUtilityV1,
                defaults.PlantCohortsEnabled,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: mateSelectionEnabled,
                plantMortalityEnabled: plantMortalityEnabled,
                homeRangeAffinityEnabled: homeRangeAffinityEnabled);
            ResetSimulation(scenario, config);
            if (homeRangeAffinityEnabled)
            {
                _scenarioId = scenario.Id + "-home-range";
                _scenarioHint = "Watch: do they keep returning to one patch?";
            }

            if (!foundersAreMature)
            {
                return;
            }

            for (int index = 0; index < _world.CreatureCount; index++)
            {
                _world.Creatures.GetNeedsRefAt(index).Age = ReproductionSystem.AdultAgeSeconds;
            }
        }

        private static SimulationConfig CreatePlayableConfig(SimulationConfig defaults)
        {
            return new SimulationConfig(
                defaults.WorldSeed,
                defaults.InitialPopulation,
                defaults.Schedule,
                defaults.MaximumPopulation,
                defaults.FounderProfile,
                defaults.CognitionEnabled,
                defaults.PhysiologyEnabled,
                DecisionPolicyVersion.IntentUtilityV1,
                defaults.PlantCohortsEnabled);
        }

        private void ArrangeDemoFounders()
        {
            SimVector2[] positions = _world.Config.FounderProfile == FounderProfile.PredationVariation
                ? PredationFounderPositions
                : DemoFounderPositions;
            for (int index = 0; index < _world.CreatureCount && index < positions.Length; index++)
            {
                _world.SetCreaturePosition(_world.GetCreatureIdAt(index), positions[index]);
            }
        }

        private void TrySelectCreature()
        {
            Ray ray = _simulationCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                return;
            }

            foreach (KeyValuePair<CreatureId, Transform> pair in _creatureViews)
            {
                if (pair.Value == hit.transform)
                {
                    _selectedCreature = pair.Key;
                    _hasSelectedCreature = true;
                    return;
                }
            }
        }

        private bool TryBeginResourceDrag()
        {
            Ray ray = _simulationCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                return false;
            }

            for (int index = 0; index < _resourceViews.Count; index++)
            {
                if (_resourceViews[index] != hit.transform)
                {
                    continue;
                }

                ResourceState resource = _world.Resources.GetAt(index);
                if (resource.Kind != ResourceKind.Food && resource.Kind != ResourceKind.Water)
                {
                    return false;
                }

                _draggedResourceId = resource.Id;
                _isDraggingResource = true;
                return true;
            }

            return false;
        }

        private void UpdateResourceDrag()
        {
            if (!_isDraggingResource)
            {
                return;
            }

            Ray ray = _simulationCamera.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float distance))
            {
                return;
            }

            Vector3 worldPosition = ray.GetPoint(distance);
            _world.Resources.SetPosition(
                _draggedResourceId,
                _world.Arena.Clamp(new SimVector2(worldPosition.x, worldPosition.z)));
        }

        /// <summary>
        /// Reproduction needs energy, hydration and health simultaneously at or above 70% of this
        /// creature's own capacity, plus adult age and no cooldown. Measurement on 2026-08-22 found
        /// that joint window is the binding constraint in any world where food and water are apart
        /// - satisfied 95% of adult ticks with co-located resources and 33.5% when food sits seven
        /// units from water - and none of it was visible while watching. Report the first unmet
        /// condition rather than a bare ready/not-ready flag, so the reason is legible.
        /// </summary>
        private static string DescribeBreedingReadiness(CreatureNeeds needs, Phenotype phenotype, ReproductionState reproduction)
        {
            if (needs.Age < ReproductionSystem.AdultAgeSeconds)
            {
                return $"juvenile ({needs.Age:0.0}s of {ReproductionSystem.AdultAgeSeconds:0}s)";
            }

            if (reproduction.CooldownRemaining > 0f)
            {
                return $"resting after breeding ({reproduction.CooldownRemaining:0.0}s left)";
            }

            float energyFraction = phenotype.EnergyCapacity <= 0f ? 0f : needs.Energy / phenotype.EnergyCapacity;
            float hydrationFraction = phenotype.HydrationCapacity <= 0f ? 0f : needs.Hydration / phenotype.HydrationCapacity;
            float healthFraction = phenotype.HealthCapacity <= 0f ? 0f : needs.Health / phenotype.HealthCapacity;
            if (energyFraction < BreedingThresholdFraction)
            {
                return $"too hungry ({energyFraction:P0} of the {BreedingThresholdFraction:P0} needed)";
            }

            if (hydrationFraction < BreedingThresholdFraction)
            {
                return $"too thirsty ({hydrationFraction:P0} of the {BreedingThresholdFraction:P0} needed)";
            }

            if (healthFraction < BreedingThresholdFraction)
            {
                return $"too hurt ({healthFraction:P0} of the {BreedingThresholdFraction:P0} needed)";
            }

            return "ready";
        }

        /// <summary>Adults that meet the full reproduction gate right now, and adults in total.</summary>
        private void CountFertileAdults(out int fertile, out int adults)
        {
            fertile = 0;
            adults = 0;
            for (int index = 0; index < _world.CreatureCount; index++)
            {
                CreatureNeeds needs = _world.GetCreatureNeedsAt(index);
                if (needs.Age < ReproductionSystem.AdultAgeSeconds)
                {
                    continue;
                }

                adults++;
                if (ReproductionSystem.CanReproduce(needs, _world.Creatures.GetPhenotypeAt(index), _world.Creatures.GetReproductionRefAt(index), _world.Config.ReproductionNeedFraction))
                {
                    fertile++;
                }
            }
        }

        /// <summary>
        /// "SeekWater 4.1s, water -3%" - the need delta is the point. A long SeekFood that ends with
        /// energy lower than it started is a failed trip, and that is invisible from an
        /// instantaneous reading of the same creature.
        /// </summary>
        private string DescribeEpisode(CreatureActionEpisode episode)
        {
            string duration = DescribeSeconds(episode.DurationTicks);
            switch (episode.Action)
            {
                case CreatureAction.SeekFood:
                case CreatureAction.Eat:
                case CreatureAction.SeekCarcass:
                case CreatureAction.FeedCarcass:
                    return $"{episode.Action} {duration}, food {DescribeDelta(episode.EnergyDelta)}";
                case CreatureAction.SeekWater:
                case CreatureAction.Drink:
                    return $"{episode.Action} {duration}, water {DescribeDelta(episode.HydrationDelta)}";
                default:
                    return $"{episode.Action} {duration}";
            }
        }

        private static string DescribeDelta(float fractionDelta)
        {
            int percent = (int)Math.Round(fractionDelta * 100f);
            return percent > 0 ? $"+{percent}%" : $"{percent}%";
        }

        private string DescribeSeconds(long ticks)
        {
            return $"{ticks * _world.Config.FixedDeltaTime:0.0}s";
        }

        private string DescribeBusiestAction()
        {
            CreatureAction busiest = CreatureAction.Wander;
            long best = -1L;
            foreach (CreatureAction action in Enum.GetValues(typeof(CreatureAction)))
            {
                long ticks = _selectedCreatureHistory.GetObservedTicksFor(action);
                if (ticks > best)
                {
                    best = ticks;
                    busiest = action;
                }
            }

            if (best <= 0L) return "nothing yet";

            float share = _selectedCreatureHistory.ObservedTicks <= 0L
                ? 0f
                : best / (float)_selectedCreatureHistory.ObservedTicks;
            return $"{busiest} ({share * 100f:0}%)";
        }
    }
}
