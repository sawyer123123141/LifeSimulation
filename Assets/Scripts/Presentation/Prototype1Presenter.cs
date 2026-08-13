using System.Collections.Generic;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    public sealed class Prototype1Presenter : MonoBehaviour
    {
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
        private float _accumulator;
        private float _speedMultiplier = 4f;
        private bool _isPaused;
        private string _scenarioId;
        private CreatureId _selectedCreature;
        private bool _hasSelectedCreature;

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
            CreateEnvironment();
            ResetSimulation(Prototype1Scenarios.Baseline);
        }

        private void Update()
        {
            HandleInput();
            if (!_isPaused)
            {
                _accumulator += Time.unscaledDeltaTime * _speedMultiplier;
                int stepLimit = 200;
                while (_accumulator >= _world.Config.FixedDeltaTime && stepLimit-- > 0)
                {
                    _world.Step(_world.Config.FixedDeltaTime);
                    _accumulator -= _world.Config.FixedDeltaTime;
                }
            }

            SynchronizePresentation();
            _world.Events.Clear();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 440f, 214f), "LifeSimulation — Prototype 1");
            GUI.Label(new Rect(24f, 40f, 300f, 22f), $"Population: {_world.CreatureCount}    Tick: {_world.CurrentTick}");
            GUI.Label(new Rect(24f, 62f, 400f, 22f), $"Scenario: {_scenarioId}    Speed: {_speedMultiplier:0}x    {(_isPaused ? "Paused" : "Running")}");
            DrawSelectedCreatureInspector();
            var stats = _world.Statistics;
            GUI.Label(new Rect(24f, 84f, 400f, 22f), $"Generation: {stats.HighestGeneration}    Births: {stats.BirthCount}    Deaths: {stats.DeathCount}");
            GUI.Label(new Rect(24f, 106f, 400f, 22f), $"Food: {stats.AvailableFood:0.0}    Water: {stats.AvailableWater:0.0}");
            GUI.Label(new Rect(24f, 216f, 400f, 22f), $"Predation: {stats.AttackHitCount} hits  {stats.PredationDeathCount} kills  {stats.CumulativeCarcassConsumed:0.0} meat");
            if (_world.Config.PhysiologyEnabled)
            {
                GUI.Label(new Rect(24f, 238f, 420f, 22f), $"Mean P3 genes: temperature {stats.MeanTemperatureToleranceGene:0.00} | fertility {stats.MeanFertilityInvestmentGene:0.00} | lifespan {stats.MeanLifespanTendencyGene:0.00}");
            }
            GUI.Label(new Rect(24f, 128f, 420f, 22f), $"Mean genes: size {stats.MeanBodySizeGene:0.00} · speed {stats.MeanMovementSpeedGene:0.00} · metabolism {stats.MeanMetabolicPaceGene:0.00}");
            GUI.Label(new Rect(24f, 150f, 420f, 22f), $"Mean genes: vision {stats.MeanVisionRangeGene:0.00} · water {stats.MeanWaterEfficiencyGene:0.00} · food {stats.MeanFoodEfficiencyGene:0.00}");
            GUI.Label(new Rect(24f, 172f, 420f, 22f), "Space pause · 1/2/4/8 speed · B/D/F resources · P predators · C cognition · T temperature");
            GUI.Label(new Rect(24f, 194f, 400f, 22f), "Green: wander · Gold: food · Blue: water · Purple: mate/reproduce");
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
            if (Input.GetMouseButtonDown(0)) TrySelectCreature();
        }

        private void CreateEnvironment()
        {
            var terrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terrain.name = "Prototype Terrain";
            terrain.transform.localScale = new Vector3(5f, 1f, 5f);
            terrain.GetComponent<Renderer>().material.color = new Color(0.16f, 0.28f, 0.16f);

            var directionalLight = new GameObject("Sun").AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.25f;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cameraObject = new GameObject("Simulation Camera");
            _simulationCamera = cameraObject.AddComponent<Camera>();
            _simulationCamera.orthographic = true;
            _simulationCamera.orthographicSize = 29f;
            _simulationCamera.transform.position = new Vector3(0f, 40f, 0f);
            _simulationCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
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
            _scenarioId = scenario.Id;
            _accumulator = 0f;
            _hasSelectedCreature = false;
            for (int index = 0; index < _world.Resources.Count; index++)
            {
                CreateResourceView(_world.Resources.GetAt(index));
            }

            ArrangeDemoFounders();
            SynchronizePresentation();
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
                    decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1));
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
                DecisionPolicyVersion.IntentUtilityV1);
        }

        private void CreateResourceView(ResourceState resource)
        {
            PrimitiveType primitive = resource.Kind == ResourceKind.Food
                ? PrimitiveType.Cylinder
                : resource.Kind == ResourceKind.Water ? PrimitiveType.Cube : PrimitiveType.Sphere;
            var view = GameObject.CreatePrimitive(primitive);
            view.name = resource.Kind.ToString();
            view.transform.position = new Vector3(resource.Position.X, 0.25f, resource.Position.Y);
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
                view.position = new Vector3(resource.Position.X, height * 0.5f, resource.Position.Y);
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
                }

                var movement = _world.GetCreatureMovementAt(index);
                CreatureAction action = _world.GetCreatureDecisionAt(index).Action;
                view.position = new Vector3(movement.Position.X, 0.55f, movement.Position.Y);
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

        private void DrawSelectedCreatureInspector()
        {
            GUI.Box(new Rect(12f, 232f, 440f, 184f), "Creature Inspector");
            if (!_hasSelectedCreature || !_world.TryGetCreatureIndex(_selectedCreature, out int index))
            {
                GUI.Label(new Rect(24f, 258f, 350f, 22f), "Click a creature to inspect it.");
                return;
            }

            var needs = _world.GetCreatureNeedsAt(index);
            var phenotype = _world.Creatures.GetPhenotypeAt(index);
            var genome = _world.Creatures.GetGenomeAt(index);
            var lineage = _world.Creatures.GetLineageAt(index);
            var decision = _world.GetCreatureDecisionAt(index);
            var diagnostics = _world.GetCreatureDecisionDiagnosticsAt(index);
            MemoryState memory = _world.GetCreatureMemoryAt(index);
            GUI.Label(new Rect(24f, 258f, 360f, 22f), $"Selected #{_selectedCreature.Value} | Gen {lineage.Generation} | {decision.Action}");
            GUI.Label(new Rect(24f, 280f, 360f, 22f), $"Energy {needs.Energy:0}/{phenotype.EnergyCapacity:0} | Water {needs.Hydration:0}/{phenotype.HydrationCapacity:0}");
            GUI.Label(new Rect(24f, 302f, 360f, 22f), $"Health {needs.Health:0}/{phenotype.HealthCapacity:0} | Age {needs.Age:0.0}s");
            GUI.Label(new Rect(24f, 324f, 360f, 22f), $"Genes size {genome.BodySize:0.00} | speed {genome.MovementSpeed:0.00} | metabolism {genome.MetabolicPace:0.00}");
            GUI.Label(new Rect(24f, 346f, 420f, 22f), $"Why: food {diagnostics.FoodScore:0.00} ({(diagnostics.FoodVisible ? "seen" : "unseen")}) | water {diagnostics.WaterScore:0.00} ({(diagnostics.WaterVisible ? "seen" : "unseen")})");
            GUI.Label(new Rect(24f, 368f, 360f, 22f), $"Parents: {lineage.FirstParent.Value}, {lineage.SecondParent.Value}");
            if (_world.Config.CognitionEnabled)
            {
                GUI.Label(new Rect(24f, 390f, 420f, 22f), $"Learned: food {memory.FoodOutcomeValue:0.00} ({memory.FoodExperienceCount}) | water {memory.WaterOutcomeValue:0.00} ({memory.WaterExperienceCount})");
            }
            if (_world.Config.PhysiologyEnabled)
            {
                GUI.Label(new Rect(24f, 412f, 420f, 22f), $"Temperature tolerance: {genome.TemperatureTolerance:0.00} | local field {TemperatureField.Sample(_world.GetCreatureMovementAt(index).Position, _world.CurrentTick):0.0} C");
                GUI.Label(new Rect(24f, 434f, 420f, 22f), $"Life history: fertility {genome.FertilityInvestment:0.00} | lifespan {genome.LifespanTendency:0.00} | max age {phenotype.MaximumAgeSeconds:0}s");
            }
        }
    }
}
