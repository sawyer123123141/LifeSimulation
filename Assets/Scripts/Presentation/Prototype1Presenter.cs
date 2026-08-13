using System.Collections.Generic;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    public sealed class Prototype1Presenter : MonoBehaviour
    {
        private readonly Dictionary<CreatureId, Transform> _creatureViews = new Dictionary<CreatureId, Transform>();
        private readonly List<CreatureId> _staleCreatureIds = new List<CreatureId>();
        private SimulationWorld _world;
        private float _accumulator;
        private float _speedMultiplier = 4f;
        private bool _isPaused;

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
            _world = new SimulationWorld(SimulationConfig.CreatePrototype1Defaults(worldSeed: 42, initialPopulation: 80));
            CreateEnvironment();
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
            var stats = _world.Statistics;
            GUI.Label(new Rect(24f, 84f, 330f, 22f), $"Generation: {stats.HighestGeneration}    Mean body gene: {stats.MeanBodySizeGene:0.00}");
            GUI.Label(new Rect(24f, 106f, 330f, 22f), $"Food: {stats.AvailableFood:0.0}    Water: {stats.AvailableWater:0.0}");
            GUI.Label(new Rect(24f, 128f, 330f, 22f), "Space pause · 1/2/4/8 set speed");
            GUI.Label(new Rect(24f, 150f, 350f, 22f), "Green: wander · Gold: seek/eat food · Blue: seek/drink water");
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
            var simulationCamera = cameraObject.AddComponent<Camera>();
            simulationCamera.orthographic = true;
            simulationCamera.orthographicSize = 29f;
            simulationCamera.transform.position = new Vector3(0f, 40f, 0f);
            simulationCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            simulationCamera.backgroundColor = new Color(0.06f, 0.09f, 0.13f);

            CreateResource(ResourceKind.Food, new Vector3(-12f, 0.25f, -8f), new Color(0.95f, 0.72f, 0.15f));
            CreateResource(ResourceKind.Food, new Vector3(10f, 0.25f, 12f), new Color(0.95f, 0.72f, 0.15f));
            CreateResource(ResourceKind.Water, new Vector3(12f, 0.25f, -11f), new Color(0.15f, 0.65f, 1f));
            CreateResource(ResourceKind.Water, new Vector3(-9f, 0.25f, 11f), new Color(0.15f, 0.65f, 1f));
        }

        private void CreateResource(ResourceKind kind, Vector3 displayPosition, Color color)
        {
            _world.Resources.Add(kind, new SimVector2(displayPosition.x, displayPosition.z), 1.5f, 30f, 30f, 3f);
            var view = GameObject.CreatePrimitive(kind == ResourceKind.Food ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            view.name = kind.ToString();
            view.transform.position = displayPosition;
            view.transform.localScale = new Vector3(2f, 0.5f, 2f);
            view.GetComponent<Renderer>().material.color = color;
        }

        private void SynchronizePresentation()
        {
            RemoveStaleCreatureViews();
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
                view.localScale = Vector3.one * GetActionScale(action);
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
                default:
                    return new Color(0.35f, 0.92f, 0.45f);
            }
        }

        private static float GetActionScale(CreatureAction action)
        {
            if (action == CreatureAction.Eat || action == CreatureAction.Drink)
            {
                return 1.12f + (0.08f * Mathf.Sin(Time.unscaledTime * 8f));
            }

            return 1f;
        }
    }
}
