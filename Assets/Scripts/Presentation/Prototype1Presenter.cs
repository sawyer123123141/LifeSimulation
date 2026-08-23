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

namespace LifeSimulation.Presentation
{
    public sealed class Prototype1Presenter : MonoBehaviour
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
        private readonly List<CreatureId> _staleCreatureIds = new List<CreatureId>();
        private readonly List<Transform> _resourceViews = new List<Transform>();
        private SimulationWorld _world;
        private Camera _simulationCamera;
        private Renderer _terrainRenderer;
        private Texture2D _temperatureHeatmap;
        private MeshFilter _terrainMeshFilter;
        private Mesh _terrainMesh;
        private GameObject _waterSurface;

        /// <summary>
        /// Look-at-it view of the fields, decoupled from the 50-unit arena. Cycled with <c>K</c>.
        /// Presentation only - see <see cref="TerrainPreview"/>.
        /// </summary>
        private TerrainPreview _terrainPreview;
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
                    _heatmapUpdateAccumulator += _world.Config.FixedDeltaTime;
                }
            }

            UpdateTemperatureHeatmapIfNeeded();
            SynchronizePresentation();
        }

        private void OnGUI()
        {
            EnsureInitialized();
            GUI.Box(new Rect(12f, 12f, 440f, 276f), "LifeSimulation — Prototype 1");
            GUI.Label(new Rect(24f, 40f, 300f, 22f), $"Population: {_world.CreatureCount}    Tick: {_world.CurrentTick}");
            GUI.Label(new Rect(24f, 62f, 400f, 22f), $"Scenario: {_scenarioId}    Speed: {_speedMultiplier:0}x    {(_isPaused ? "Paused" : "Running")}");
            if (_world != null && _world.Config.ElevationFieldEnabled)
            {
                GUI.Label(new Rect(24f, 84f, 430f, 22f),
                    $"Height {_terrainHeightScale:0.0}  ([ lower, ] raise or PgDn/PgUp)");
                GUI.Label(new Rect(24f, 106f, 430f, 22f),
                    $"Smoothing {_terrainSmoothingRadius:0.0}  (, less, . more or -/=)");
                GUI.Label(new Rect(24f, 128f, 430f, 22f),
                    $"K view: {(_terrainPreview == null ? "off" : _terrainPreview.Describe())}");
            }
            DrawSelectedCreatureInspector();
            DrawSelectedCreatureHistory();
            var stats = _world.Statistics;
            DrawPopulationCondition(stats);
            DrawP5HistoryPanel();
            GUI.Label(new Rect(24f, 84f, 400f, 22f), $"Generation: {stats.HighestGeneration}    Births: {stats.BirthCount}    Deaths: {stats.DeathCount}");
            GUI.Label(new Rect(24f, 106f, 400f, 22f), $"Food: {stats.AvailableFood:0.0}    Water: {stats.AvailableWater:0.0}");
            GUI.Label(new Rect(24f, 216f, 400f, 22f), $"Predation: {stats.AttackHitCount} hits  {stats.PredationDeathCount} kills  {stats.CumulativeCarcassConsumed:0.0} meat");
            if (_world.Config.CognitionEnabled)
            {
                GUI.Label(new Rect(24f, 260f, 420f, 22f), $"Mean P2 genes: memory {stats.MeanMemoryCapacityGene:0.00} | retention {stats.MeanMemoryRetentionGene:0.00} | learning {stats.MeanLearningRateGene:0.00}");
            }
            if (_world.Config.PhysiologyEnabled)
            {
                GUI.Label(new Rect(24f, 238f, 420f, 22f), $"Mean P3 genes: temperature {stats.MeanTemperatureToleranceGene:0.00} | fertility {stats.MeanFertilityInvestmentGene:0.00} | lifespan {stats.MeanLifespanTendencyGene:0.00}");
            }
            GUI.Label(new Rect(24f, 128f, 420f, 22f), $"Mean genes: size {stats.MeanBodySizeGene:0.00} · speed {stats.MeanMovementSpeedGene:0.00} · metabolism {stats.MeanMetabolicPaceGene:0.00}");
            GUI.Label(new Rect(24f, 150f, 420f, 22f), $"Mean genes: vision {stats.MeanVisionRangeGene:0.00} · water {stats.MeanWaterEfficiencyGene:0.00} · food {stats.MeanFoodEfficiencyGene:0.00}");
            GUI.Label(new Rect(24f, 172f, 420f, 22f), "Space pause · 1/2/4/8 speed · B/D/F resources · P predators · C cognition · T temperature · G foraging memory · E starter habitat · 5/6/7/9 watch scenarios · R home range · V shifting patches · H overlay");
            GUI.Label(new Rect(24f, 194f, 440f, 22f), "Colors: green wander · gold food · blue water · purple mate · cyan flee · red hunt");
        }

        private void DrawPopulationCondition(SimulationStatistics stats)
        {
            GUI.Box(new Rect(464f, 12f, 280f, 264f), "Population condition");
            GUI.Label(new Rect(476f, 40f, 250f, 22f), $"Energy: {stats.MeanEnergyFraction:P0}");
            GUI.Label(new Rect(476f, 62f, 250f, 22f), $"Hydration: {stats.MeanHydrationFraction:P0}");
            GUI.Label(new Rect(476f, 84f, 250f, 22f), $"Food eaten: {stats.CumulativeFoodConsumed:0.0}");
            GUI.Label(new Rect(476f, 106f, 250f, 22f), $"Water used: {stats.CumulativeWaterConsumed:0.0}");
            CountFertileAdults(out int fertileAdults, out int adultCount);
            GUI.Label(new Rect(476f, 128f, 250f, 22f), $"Ready to breed: {fertileAdults} of {adultCount} adults");
            GUI.Label(new Rect(476f, 150f, 250f, 22f), $"Deaths: food {stats.StarvationDeathCount}  water {stats.DehydrationDeathCount}");
            GUI.Label(new Rect(476f, 172f, 250f, 22f), _hasRecentEvent ? FormatRecentEvent() : "Latest event: waiting");
            if (_world.Config.FounderProfile == FounderProfile.PredationVariation)
            {
                GUI.Label(new Rect(476f, 194f, 250f, 22f), $"P1 cohorts: hunters {stats.ViableHunterCount}  others {stats.NonHunterCount}");
            }
            GUI.Label(new Rect(476f, 216f, 250f, 22f), "Watch: 5 stable · 6 scarce · 7 migration · 9 mating · R home range · V shifting");
            GUI.Label(new Rect(476f, 238f, 250f, 22f), _scenarioHint);
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

        private void CaptureRecentEvent()
        {
            if (_world.Events.Count > 0)
            {
                _recentEvent = _world.Events.GetAt(_world.Events.Count - 1);
                _hasRecentEvent = true;
            }
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

            var directionalLight = new GameObject("Sun").AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.25f;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraObject = new GameObject("Simulation Camera");
            _simulationCamera = cameraObject.AddComponent<Camera>();
            _simulationCamera.orthographic = false;
            _simulationCamera.fieldOfView = 55f;
            cameraObject.AddComponent<GroundPlaneCameraController>();
            _simulationCamera.backgroundColor = new Color(0.06f, 0.09f, 0.13f);

        }

        private void ResetSimulation(SimulationScenario scenario, SimulationConfig config = null)
        {
            foreach (KeyValuePair<CreatureId, Transform> creatureView in _creatureViews)
            {
                Destroy(creatureView.Value.gameObject);
            }

            _creatureViews.Clear();
            for (int index = 0; index < _resourceViews.Count; index++)
            {
                Destroy(_resourceViews[index].gameObject);
            }

            _resourceViews.Clear();
            _world = new SimulationWorld(config ?? CreatePlayableConfig(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: 4)));
            scenario.ApplyTo(_world);
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

        /// <summary>
        /// Elevation as a readable relief map: everything below <see cref="OverlaySeaLevel"/> reads
        /// as water, and land ramps beach -> grass -> rock -> snow. Banded rather than a smooth
        /// gradient because the point of looking at terrain is to see where the contours are, and a
        /// continuous ramp hides exactly the ridge structure the ridged-multifractal field exists to
        /// produce.
        /// </summary>
        private static Color ShadeElevation(float elevation)
        {
            if (elevation <= OverlaySeaLevel)
            {
                float depth = OverlaySeaLevel <= 0f ? 0f : Mathf.Clamp01(elevation / OverlaySeaLevel);
                return Color.Lerp(new Color(0.043f, 0.129f, 0.278f), new Color(0.176f, 0.408f, 0.616f), depth);
            }

            float land = Mathf.Clamp01((elevation - OverlaySeaLevel) / (1f - OverlaySeaLevel));
            if (land < 0.08f) return Color.Lerp(new Color(0.827f, 0.776f, 0.573f), new Color(0.573f, 0.678f, 0.376f), land / 0.08f);
            if (land < 0.45f) return Color.Lerp(new Color(0.573f, 0.678f, 0.376f), new Color(0.353f, 0.478f, 0.278f), (land - 0.08f) / 0.37f);
            if (land < 0.78f) return Color.Lerp(new Color(0.353f, 0.478f, 0.278f), new Color(0.478f, 0.451f, 0.412f), (land - 0.45f) / 0.33f);
            return Color.Lerp(new Color(0.478f, 0.451f, 0.412f), Color.white, (land - 0.78f) / 0.22f);
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
        /// Ground height under a simulation position, in Unity units, read from the cached height
        /// grid with bilinear interpolation.
        ///
        /// <para><b>Cosmetic only.</b> The simulation is a flat plane: every position is a
        /// <c>SimVector2</c>, distance is 2D, and nothing in <c>Assets/Scripts/Simulation</c> knows
        /// this function exists. Creatures are drawn standing on the relief; they do not walk up it,
        /// and a hill costs them nothing. Making elevation affect movement is a simulation change -
        /// flag, tests and an experiment - not a presentation one.</para>
        ///
        /// <para>Reading the cache rather than resampling matters twice over: creatures land exactly
        /// on the drawn surface rather than on a separately computed one, and a creature costs an
        /// array read per frame instead of a noise evaluation.</para>
        /// </summary>
        private float GroundHeightAt(float x, float z)
        {
            if (_terrainHeights == null || _world == null || !_world.Config.ElevationFieldEnabled) return 0f;

            int side = TerrainResolution;
            float u = Mathf.Clamp01((x + TerrainHalfWidth) / (2f * TerrainHalfWidth)) * (side - 1);
            float v = Mathf.Clamp01((z + TerrainHalfWidth) / (2f * TerrainHalfWidth)) * (side - 1);
            int column = Mathf.Clamp((int)u, 0, side - 2);
            int row = Mathf.Clamp((int)v, 0, side - 2);
            float fx = u - column;
            float fz = v - row;

            float bottomLeft = _terrainHeights[row * side + column];
            float bottomRight = _terrainHeights[row * side + column + 1];
            float topLeft = _terrainHeights[(row + 1) * side + column];
            float topRight = _terrainHeights[(row + 1) * side + column + 1];
            return Mathf.Lerp(Mathf.Lerp(bottomLeft, bottomRight, fx), Mathf.Lerp(topLeft, topRight, fx), fz);
        }

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
            if (_terrainHeights == null || _terrainHeights.Length != side * side)
            {
                _terrainHeights = new float[side * side];
            }

            if (_world == null || !_world.Config.ElevationFieldEnabled)
            {
                System.Array.Clear(_terrainHeights, 0, _terrainHeights.Length);
                return;
            }

            var raw = new float[side * side];
            for (int row = 0; row < side; row++)
            {
                float z = Mathf.Lerp(-TerrainHalfWidth, TerrainHalfWidth, row / (float)(side - 1));
                for (int column = 0; column < side; column++)
                {
                    float x = Mathf.Lerp(-TerrainHalfWidth, TerrainHalfWidth, column / (float)(side - 1));
                    raw[row * side + column] = _world.Environment.Sample(new SimVector2(x, z)).Elevation;
                }
            }

            // Each pass is a 3x3 box over one cell, so N passes smooth roughly N cells outward.
            float cellSize = 2f * TerrainHalfWidth / (side - 1);
            int passes = Mathf.Clamp(Mathf.RoundToInt(_terrainSmoothingRadius / Mathf.Max(cellSize, 0.0001f)), 0, 32);
            var scratch = new float[side * side];
            for (int pass = 0; pass < passes; pass++)
            {
                for (int row = 0; row < side; row++)
                {
                    for (int column = 0; column < side; column++)
                    {
                        float total = 0f;
                        int taps = 0;
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int sampleRow = row + dz;
                            if (sampleRow < 0 || sampleRow >= side) continue;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int sampleColumn = column + dx;
                                if (sampleColumn < 0 || sampleColumn >= side) continue;
                                total += raw[sampleRow * side + sampleColumn];
                                taps++;
                            }
                        }

                        scratch[row * side + column] = total / taps;
                    }
                }

                float[] swap = raw;
                raw = scratch;
                scratch = swap;
            }

            for (int index = 0; index < raw.Length; index++)
            {
                _terrainHeights[index] = Mathf.Max(0f, raw[index] - OverlaySeaLevel) / (1f - OverlaySeaLevel) * _terrainHeightScale;
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

        /// <summary>
        /// Water sits just above y=0, which is where the ground sits once everything at or below sea
        /// level has been flattened. Hidden entirely when there is no elevation field, since a sea
        /// over a flat world would be covering the whole arena.
        /// </summary>
        private void UpdateWaterSurface()
        {
            if (_waterSurface == null) return;

            bool hasTerrain = _world != null && _world.Config.ElevationFieldEnabled;
            _waterSurface.SetActive(hasTerrain);
            _waterSurface.transform.position = new Vector3(0f, 0.06f, 0f);
        }

        /// <summary>
        /// Show or hide everything that belongs to the arena when a preview opens or closes.
        ///
        /// <para>Hiding the ground alone was not enough: creatures, resources and the sea stayed
        /// where they were, so the planet appeared to hover over a field of animals floating in
        /// empty space. A preview replaces the scene rather than being added to it.</para>
        ///
        /// <para>The camera is re-framed too, because its zoom ceiling and pan clamp are sized for a
        /// 50-unit arena - a 400-unit patch could not be pulled away from far enough to see, which
        /// is why it read as a featureless flat plane.</para>
        /// </summary>
        private void ApplyTerrainPreviewMode(TerrainPreview.Mode mode)
        {
            bool arenaVisible = mode == TerrainPreview.Mode.Off;

            _terrainRenderer.enabled = arenaVisible;
            if (_waterSurface != null)
            {
                _waterSurface.SetActive(arenaVisible && _world != null && _world.Config.ElevationFieldEnabled);
            }

            foreach (KeyValuePair<CreatureId, Transform> pair in _creatureViews)
            {
                if (pair.Value != null) pair.Value.gameObject.SetActive(arenaVisible);
            }

            for (int index = 0; index < _resourceViews.Count; index++)
            {
                if (_resourceViews[index] != null) _resourceViews[index].gameObject.SetActive(arenaVisible);
            }

            var cameraController = _simulationCamera == null ? null : _simulationCamera.GetComponent<GroundPlaneCameraController>();
            if (cameraController == null) return;

            if (arenaVisible) cameraController.ResetFrame();
            else cameraController.Frame(_terrainPreview.FramingRadius);
        }

        /// <summary>
        /// Live terrain tuning. The correct height and smoothing cannot be derived - they depend on
        /// how the relief reads beside a 1-unit creature - so they are dialled here rather than
        /// guessed in source and recompiled.
        /// </summary>
        private void HandleTerrainTuningInput()
        {
            bool changed = false;
            // Two bindings each, because bracket and comma keys are not in the same place on every
            // keyboard layout and a tuning control you cannot find is not a tuning control.
            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.PageDown)) { _terrainHeightScale = Mathf.Max(0f, _terrainHeightScale - 2f); changed = true; }
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.PageUp)) { _terrainHeightScale += 2f; changed = true; }
            if (Input.GetKeyDown(KeyCode.Comma) || Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) { _terrainSmoothingRadius = Mathf.Max(0f, _terrainSmoothingRadius - 0.4f); changed = true; }
            if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) { _terrainSmoothingRadius += 0.4f; changed = true; }
            if (!changed) return;

            BuildTerrainMesh();
            UpdateWaterSurface();
            if (_terrainPreview != null && _terrainPreview.Current != TerrainPreview.Mode.Off)
            {
                _terrainPreview.HeightScale = _terrainHeightScale;
                _terrainPreview.Rebuild(_world);
            }
        }

        /// <summary>
        /// Rebuild the ground as a displaced grid over the arena. Flat when the elevation flag is
        /// off, which keeps every existing scenario looking exactly as it did.
        /// </summary>
        private void BuildTerrainMesh()
        {
            if (_terrainMesh == null) return;

            RebuildTerrainHeights();

            const float halfWidth = TerrainHalfWidth;
            int side = TerrainResolution;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[side * side];
            var triangles = new int[(side - 1) * (side - 1) * 6];

            for (int row = 0; row < side; row++)
            {
                float v = row / (float)(side - 1);
                float z = Mathf.Lerp(-halfWidth, halfWidth, v);
                for (int column = 0; column < side; column++)
                {
                    float u = column / (float)(side - 1);
                    float x = Mathf.Lerp(-halfWidth, halfWidth, u);
                    int vertex = row * side + column;
                    vertices[vertex] = new Vector3(x, _terrainHeights[vertex], z);
                    uv[vertex] = new Vector2(u, v);
                }
            }

            int triangle = 0;
            for (int row = 0; row + 1 < side; row++)
            {
                for (int column = 0; column + 1 < side; column++)
                {
                    int bottomLeft = row * side + column;
                    int topLeft = bottomLeft + side;
                    triangles[triangle++] = bottomLeft;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = bottomLeft + 1;
                    triangles[triangle++] = topLeft;
                    triangles[triangle++] = topLeft + 1;
                }
            }

            _terrainMesh.Clear();
            _terrainMesh.vertices = vertices;
            _terrainMesh.uv = uv;
            _terrainMesh.triangles = triangles;
            _terrainMesh.RecalculateNormals();
            _terrainMesh.RecalculateBounds();
        }

        private void UpdateTemperatureHeatmapIfNeeded()
        {
            if (_world == null || _heatmapUpdateAccumulator < HeatmapUpdateInterval)
            {
                return;
            }

            Bounds terrainBounds = _terrainRenderer.bounds;
            float minX = terrainBounds.min.x;
            float maxX = terrainBounds.max.x;
            float minZ = terrainBounds.min.z;
            float maxZ = terrainBounds.max.z;
            for (int y = 0; y < HeatmapResolution; y++)
            {
                float z = Mathf.Lerp(minZ, maxZ, (y + 0.5f) / HeatmapResolution);
                int rowStart = y * HeatmapResolution;
                for (int x = 0; x < HeatmapResolution; x++)
                {
                    float worldX = Mathf.Lerp(minX, maxX, (x + 0.5f) / HeatmapResolution);
                    var position = new SimVector2(worldX, z);
                    if (_overlay == TerrainOverlay.Elevation)
                    {
                        _temperaturePixels[rowStart + x] = ShadeElevation(_world.Environment.Sample(position).Elevation);
                    }
                    else if (_overlay == TerrainOverlay.Biome)
                    {
                        EnvironmentSample sample = _world.Environment.Sample(position);
                        // Shade each biome by its own fertility so the map shows gradient within a
                        // region, not just flat colour blocks.
                        _temperaturePixels[rowStart + x] = BiomeColors[ClassifyBiome(sample)]
                            * Mathf.Lerp(0.72f, 1.18f, sample.Fertility);
                    }
                    else
                    {
                        float temperature = TemperatureField.Sample(position, _world.CurrentTick);
                        float temperatureFraction = Mathf.InverseLerp(ColdTemperature, HotTemperature, temperature);
                        _temperaturePixels[rowStart + x] = Color.Lerp(Color.blue, Color.red, temperatureFraction);
                    }
                }
            }

            _temperatureHeatmap.SetPixels(_temperaturePixels);
            _temperatureHeatmap.Apply();
            _heatmapUpdateAccumulator = 0f;
            if (_overlay != TerrainOverlay.None)
            {
                ApplyTemperatureHeatmap();
            }
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
                elevationFieldEnabled: true);
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

        private void CreateResourceView(ResourceState resource)
        {
            PrimitiveType primitive = resource.Kind == ResourceKind.Food
                ? PrimitiveType.Cylinder
                : resource.Kind == ResourceKind.Water ? PrimitiveType.Cube : PrimitiveType.Sphere;
            var view = GameObject.CreatePrimitive(primitive);
            view.name = resource.Kind.ToString();
            view.transform.position = new Vector3(resource.Position.X, GroundHeightAt(resource.Position.X, resource.Position.Y) + 0.25f, resource.Position.Y);
            view.transform.localScale = new Vector3(2f, 0.5f, 2f);
            view.GetComponent<Renderer>().material.color = GetResourceColor(resource.Kind);
            _resourceViews.Add(view.transform);
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

        private void SynchronizeResourceViews()
        {
            for (int index = 0; index < _world.Resources.Count; index++)
            {
                var resource = _world.Resources.GetAt(index);
                if (index >= _resourceViews.Count)
                {
                    CreateResourceView(resource);
                }

                Transform view = _resourceViews[index];
                float fraction = resource.Capacity <= 0f ? 0f : resource.Amount / resource.Capacity;
                float height = Mathf.Lerp(0.08f, 0.5f, fraction);
                view.position = new Vector3(resource.Position.X, GroundHeightAt(resource.Position.X, resource.Position.Y) + height * 0.5f, resource.Position.Y);
                view.localScale = new Vector3(2f, height, 2f);
                Color baseColor = GetResourceColor(resource.Kind);
                view.GetComponent<Renderer>().material.color = Color.Lerp(baseColor * 0.2f, baseColor, fraction);
            }
        }

        private static Color GetResourceColor(ResourceKind kind)
        {
            if (kind == ResourceKind.Food) return new Color(0.95f, 0.72f, 0.15f);
            if (kind == ResourceKind.Water) return new Color(0.15f, 0.65f, 1f);
            return new Color(0.55f, 0.12f, 0.08f);
        }

        private void SynchronizePresentation()
        {
            RemoveStaleCreatureViews();
            SynchronizeResourceViews();
            for (int index = 0; index < _world.CreatureCount; index++)
            {
                CreatureId id = _world.GetCreatureIdAt(index);
                if (!_creatureViews.TryGetValue(id, out Transform view))
                {
                    view = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                    view.name = $"Creature {id.Value}";
                    float hue = (id.Value * 0.61803398875f) % 1f;
                    view.GetComponent<Renderer>().material.color = Color.HSVToRGB(hue, 0.55f, 0.95f);
                    _creatureViews.Add(id, view);
                    if (_terrainPreview != null && _terrainPreview.Current != TerrainPreview.Mode.Off)
                    {
                        // Born while a preview is up: stay hidden until the arena comes back.
                        view.gameObject.SetActive(false);
                    }
                }

                var movement = _world.GetCreatureMovementAt(index);
                CreatureAction action = _world.GetCreatureDecisionAt(index).Action;
                view.position = new Vector3(movement.Position.X, GroundHeightAt(movement.Position.X, movement.Position.Y) + 0.55f, movement.Position.Y);
                float ageScale = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(_world.GetCreatureNeedsAt(index).Age / 4f));
                float bodyScale = Mathf.Lerp(0.7f, 1.35f, _world.Creatures.GetGenomeAt(index).BodySize);
                view.localScale = Vector3.one * (GetActionScale(action) * ageScale * bodyScale);
                view.GetComponent<Renderer>().material.color = GetActionColor(action);
            }
        }

        private void RemoveStaleCreatureViews()
        {
            _staleCreatureIds.Clear();
            foreach (KeyValuePair<CreatureId, Transform> pair in _creatureViews)
            {
                if (!_world.TryGetCreatureIndex(pair.Key, out _))
                {
                    if (_hasSelectedCreature && pair.Key.Equals(_selectedCreature))
                    {
                        _hasSelectedCreature = false;
                    }

                    Destroy(pair.Value.gameObject);
                    _staleCreatureIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < _staleCreatureIds.Count; index++)
            {
                _creatureViews.Remove(_staleCreatureIds[index]);
            }
        }

        private static Color GetActionColor(CreatureAction action)
        {
            switch (action)
            {
                case CreatureAction.SeekFood:
                case CreatureAction.Eat:
                    return new Color(1f, 0.72f, 0.12f);
                case CreatureAction.SeekWater:
                case CreatureAction.Drink:
                    return new Color(0.15f, 0.68f, 1f);
                case CreatureAction.Reproduce:
                case CreatureAction.SeekMate:
                    return new Color(0.9f, 0.25f, 0.9f);
                case CreatureAction.SeekPrey:
                case CreatureAction.Attack:
                    return new Color(0.98f, 0.2f, 0.16f);
                case CreatureAction.Flee:
                    return new Color(0.2f, 0.95f, 0.95f);
                case CreatureAction.SeekThermalComfort:
                    return new Color(1f, 0.45f, 0.08f);
                default:
                    return new Color(0.35f, 0.92f, 0.45f);
            }
        }

        private static float GetActionScale(CreatureAction action)
        {
            if (action == CreatureAction.Eat || action == CreatureAction.Drink || action == CreatureAction.Reproduce || action == CreatureAction.Attack)
            {
                return 1.12f + (0.08f * Mathf.Sin(Time.unscaledTime * 8f));
            }

            return 1f;
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
                if (ReproductionSystem.CanReproduce(needs, _world.Creatures.GetPhenotypeAt(index), _world.Creatures.GetReproductionRefAt(index)))
                {
                    fertile++;
                }
            }
        }

        /// <summary>
        /// Keep the history pointed at whatever is selected. Called once per simulated step, so the
        /// history's resolution is the tick rather than the frame and is therefore independent of
        /// frame rate and of the speed multiplier.
        /// </summary>
        private void ObserveSelectedCreature()
        {
            if (!_hasSelectedCreature)
            {
                if (_selectedCreatureHistory.IsTracking) _selectedCreatureHistory.Clear();
                return;
            }

            _selectedCreatureHistory.Track(_selectedCreature);
            _selectedCreatureHistory.Observe(_world);
        }

        /// <summary>
        /// What the selected creature has been doing, rather than what it is doing right now. The
        /// inspector answers the second question already; this answers the first, which is the one
        /// that makes a commute, a failed foraging trip or a long thirsty wander legible while
        /// watching.
        /// </summary>
        private void DrawSelectedCreatureHistory()
        {
            const float panelX = 464f;
            const float panelY = 300f;
            const float lineHeight = 22f;
            const int maximumEpisodes = 6;

            GUI.Box(new Rect(panelX, panelY, 280f, 268f), "Selected creature history");

            if (!_hasSelectedCreature || !_selectedCreatureHistory.IsTracking)
            {
                GUI.Label(new Rect(panelX + 12f, panelY + 28f, 260f, lineHeight), "Click a creature to follow it.");
                return;
            }

            float y = panelY + 28f;
            if (!_selectedCreatureHistory.IsAlive)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), "This creature has died.");
                y += lineHeight;
            }
            else if (_selectedCreatureHistory.TryGetOpenEpisode(out CreatureActionEpisode current))
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), $"Now: {DescribeEpisode(current)}");
                y += lineHeight;
            }

            GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), $"Watched {DescribeSeconds(_selectedCreatureHistory.ObservedTicks)}, mostly {DescribeBusiestAction()}");
            y += lineHeight + 4f;

            if (_selectedCreatureHistory.EpisodeCount == 0)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), "No completed activity yet.");
                return;
            }

            int shown = Math.Min(maximumEpisodes, _selectedCreatureHistory.EpisodeCount);
            for (int index = 0; index < shown; index++)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), DescribeEpisode(_selectedCreatureHistory.GetEpisodeAt(index)));
                y += lineHeight;
            }

            int hidden = _selectedCreatureHistory.EpisodeCount - shown;
            if (hidden > 0)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), $"{hidden} older activit{(hidden == 1 ? "y" : "ies")} hidden");
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

        private void DrawSelectedCreatureInspector()
        {
            const float inspectorTop = 300f;
            GUI.Box(new Rect(12f, inspectorTop, 440f, 324f), "Creature Inspector");
            if (!_hasSelectedCreature || !_world.TryGetCreatureIndex(_selectedCreature, out int index))
            {
                GUI.Label(new Rect(24f, inspectorTop + 26f, 350f, 22f), "Click a creature to inspect it.");
                return;
            }

            var needs = _world.GetCreatureNeedsAt(index);
            var phenotype = _world.Creatures.GetPhenotypeAt(index);
            var genome = _world.Creatures.GetGenomeAt(index);
            var lineage = _world.Creatures.GetLineageAt(index);
            var decision = _world.GetCreatureDecisionAt(index);
            var diagnostics = _world.GetCreatureDecisionDiagnosticsAt(index);
            MemoryState memory = _world.GetCreatureMemoryAt(index);
            GUI.Label(new Rect(24f, inspectorTop + 26f, 360f, 22f), $"Selected #{_selectedCreature.Value} | Gen {lineage.Generation} | {decision.Action}");
            GUI.Label(new Rect(24f, inspectorTop + 48f, 360f, 22f), $"Energy {needs.Energy:0}/{phenotype.EnergyCapacity:0} | Water {needs.Hydration:0}/{phenotype.HydrationCapacity:0}");
            GUI.Label(new Rect(24f, inspectorTop + 70f, 360f, 22f), $"Health {needs.Health:0}/{phenotype.HealthCapacity:0} | Age {needs.Age:0.0}s");
            GUI.Label(new Rect(24f, inspectorTop + 92f, 360f, 22f), $"Genes size {genome.BodySize:0.00} | speed {genome.MovementSpeed:0.00} | metabolism {genome.MetabolicPace:0.00}");
            GUI.Label(new Rect(24f, inspectorTop + 114f, 420f, 22f), $"Why: food {diagnostics.FoodScore:0.00} ({(diagnostics.FoodVisible ? "seen" : "unseen")}) | water {diagnostics.WaterScore:0.00} ({(diagnostics.WaterVisible ? "seen" : "unseen")})");
            GUI.Label(new Rect(24f, inspectorTop + 136f, 420f, 22f), $"Also: flee {diagnostics.FleeScore:0.00} | hunt {diagnostics.HuntScore:0.00} | carcass {diagnostics.CarcassScore:0.00} | warmth {diagnostics.ThermalScore:0.00}");
            GUI.Label(new Rect(24f, inspectorTop + 158f, 360f, 22f), $"Parents: {lineage.FirstParent.Value}, {lineage.SecondParent.Value}");
            GUI.Label(new Rect(24f, inspectorTop + 180f, 420f, 22f), $"Breeding: {DescribeBreedingReadiness(needs, phenotype, _world.Creatures.GetReproductionRefAt(index))}");
            float optionalDetailY = inspectorTop + 202f;
            if (_world.Config.FounderProfile == FounderProfile.PredationVariation)
            {
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"P1 traits: attack {genome.Attack:0.00} | defense {genome.Defense:0.00} | aggression {genome.Aggression:0.00} | diet {genome.DietSpecialization:0.00}");
                optionalDetailY += 22f;
            }
            if (_world.Config.CognitionEnabled)
            {
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"P2 traits: memory {genome.MemoryCapacity:0.00} | retention {genome.MemoryRetention:0.00} | learning {genome.LearningRate:0.00} | explore {genome.Exploration:0.00}");
                optionalDetailY += 22f;
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"Learned: food {memory.FoodOutcomeValue:0.00} ({memory.FoodExperienceCount}) | water {memory.WaterOutcomeValue:0.00} ({memory.WaterExperienceCount})");
                optionalDetailY += 22f;
            }
            if (_world.Config.PhysiologyEnabled)
            {
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"Temperature tolerance: {genome.TemperatureTolerance:0.00} | local field {TemperatureField.Sample(_world.GetCreatureMovementAt(index).Position, _world.CurrentTick):0.0} C");
                optionalDetailY += 22f;
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"Life history: fertility {genome.FertilityInvestment:0.00} | lifespan {genome.LifespanTendency:0.00} | max age {phenotype.MaximumAgeSeconds:0}s");
            }
        }
    }
}
