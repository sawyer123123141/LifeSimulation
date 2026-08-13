using System.Collections.Generic;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
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

        private readonly Dictionary<CreatureId, Transform> _creatureViews = new Dictionary<CreatureId, Transform>();
        private readonly List<CreatureId> _staleCreatureIds = new List<CreatureId>();
        private readonly List<Transform> _resourceViews = new List<Transform>();
        private SimulationWorld _world;
        private Camera _simulationCamera;
        private float _accumulator;
        private float _speedMultiplier = 4f;
        private bool _isPaused;
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
            _world = new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: 4));
            CreateEnvironment();
            ArrangeDemoFounders();
            SynchronizePresentation();
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
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 380f, 170f), "LifeSimulation — Prototype 1");
            GUI.Label(new Rect(24f, 40f, 300f, 22f), $"Population: {_world.CreatureCount}    Tick: {_world.CurrentTick}");
            GUI.Label(new Rect(24f, 62f, 300f, 22f), $"Speed: {_speedMultiplier:0}x    {(_isPaused ? "Paused" : "Running")}");
            DrawSelectedCreatureInspector();
            var stats = _world.Statistics;
            GUI.Label(new Rect(24f, 84f, 360f, 22f), $"Generation: {stats.HighestGeneration}    Births: {stats.BirthCount}    Deaths: {stats.DeathCount}");
            GUI.Label(new Rect(24f, 106f, 330f, 22f), $"Food: {stats.AvailableFood:0.0}    Water: {stats.AvailableWater:0.0}");
            GUI.Label(new Rect(24f, 128f, 330f, 22f), "Space pause · 1/2/4/8 set speed");
            GUI.Label(new Rect(24f, 150f, 350f, 22f), "Green: wander · Gold: food · Blue: water · Purple: reproduce");
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

            CreateResource(ResourceKind.Food, new Vector3(-12f, 0.25f, -8f), new Color(0.95f, 0.72f, 0.15f));
            CreateResource(ResourceKind.Water, new Vector3(-7f, 0.25f, -8f), new Color(0.15f, 0.65f, 1f));
            CreateResource(ResourceKind.Food, new Vector3(10f, 0.25f, 12f), new Color(0.95f, 0.72f, 0.15f));
            CreateResource(ResourceKind.Water, new Vector3(5f, 0.25f, 12f), new Color(0.15f, 0.65f, 1f));
        }

        private void CreateResource(ResourceKind kind, Vector3 displayPosition, Color color)
        {
            _world.Resources.Add(kind, new SimVector2(displayPosition.x, displayPosition.z), 1.5f, 30f, 30f, 3f);
            var view = GameObject.CreatePrimitive(kind == ResourceKind.Food ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            view.name = kind.ToString();
            view.transform.position = displayPosition;
            view.transform.localScale = new Vector3(2f, 0.5f, 2f);
            view.GetComponent<Renderer>().material.color = color;
            _resourceViews.Add(view.transform);
        }

        private void ArrangeDemoFounders()
        {
            for (int index = 0; index < _world.CreatureCount && index < DemoFounderPositions.Length; index++)
            {
                _world.SetCreaturePosition(_world.GetCreatureIdAt(index), DemoFounderPositions[index]);
            }
        }

        private void SynchronizeResourceViews()
        {
            for (int index = 0; index < _world.Resources.Count; index++)
            {
                var resource = _world.Resources.GetAt(index);
                Transform view = _resourceViews[index];
                float fraction = resource.Capacity <= 0f ? 0f : resource.Amount / resource.Capacity;
                float height = Mathf.Lerp(0.08f, 0.5f, fraction);
                view.position = new Vector3(resource.Position.X, height * 0.5f, resource.Position.Y);
                view.localScale = new Vector3(2f, height, 2f);
                Color baseColor = resource.Kind == ResourceKind.Food
                    ? new Color(0.95f, 0.72f, 0.15f)
                    : new Color(0.15f, 0.65f, 1f);
                view.GetComponent<Renderer>().material.color = Color.Lerp(baseColor * 0.2f, baseColor, fraction);
            }
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
                view.localScale = Vector3.one * (GetActionScale(action) * ageScale);
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
                    return new Color(0.9f, 0.25f, 0.9f);
                default:
                    return new Color(0.35f, 0.92f, 0.45f);
            }
        }

        private static float GetActionScale(CreatureAction action)
        {
            if (action == CreatureAction.Eat || action == CreatureAction.Drink || action == CreatureAction.Reproduce)
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
            GUI.Box(new Rect(12f, 178f, 400f, 140f), "Creature Inspector");
            if (!_hasSelectedCreature || !_world.TryGetCreatureIndex(_selectedCreature, out int index))
            {
                GUI.Label(new Rect(24f, 204f, 350f, 22f), "Click a creature to inspect it.");
                return;
            }

            var needs = _world.GetCreatureNeedsAt(index);
            var phenotype = _world.Creatures.GetPhenotypeAt(index);
            var genome = _world.Creatures.GetGenomeAt(index);
            var lineage = _world.Creatures.GetLineageAt(index);
            var decision = _world.GetCreatureDecisionAt(index);
            GUI.Label(new Rect(24f, 204f, 360f, 22f), $"Selected #{_selectedCreature.Value} | Gen {lineage.Generation} | {decision.Action}");
            GUI.Label(new Rect(24f, 226f, 360f, 22f), $"Energy {needs.Energy:0}/{phenotype.EnergyCapacity:0} | Water {needs.Hydration:0}/{phenotype.HydrationCapacity:0}");
            GUI.Label(new Rect(24f, 248f, 360f, 22f), $"Health {needs.Health:0}/{phenotype.HealthCapacity:0} | Age {needs.Age:0.0}s");
            GUI.Label(new Rect(24f, 270f, 360f, 22f), $"Genes size {genome.BodySize:0.00} | speed {genome.MovementSpeed:0.00} | metabolism {genome.MetabolicPace:0.00}");
            GUI.Label(new Rect(24f, 292f, 360f, 22f), $"Parents: {lineage.FirstParent.Value}, {lineage.SecondParent.Value}");
        }
    }
}
