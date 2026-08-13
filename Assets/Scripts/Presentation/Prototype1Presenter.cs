using System.Collections.Generic;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;
using UnityEngine;

namespace LifeSimulation.Presentation
{
    public sealed class Prototype1Presenter : MonoBehaviour
    {
        private readonly Dictionary<CreatureId, Transform> _creatureViews = new Dictionary<CreatureId, Transform>();
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
            GUI.Box(new Rect(12f, 12f, 330f, 104f), "LifeSimulation — Prototype 1");
            GUI.Label(new Rect(24f, 40f, 300f, 22f), $"Population: {_world.CreatureCount}    Tick: {_world.CurrentTick}");
            GUI.Label(new Rect(24f, 62f, 300f, 22f), $"Speed: {_speedMultiplier:0}x    {(_isPaused ? "Paused" : "Running")}");
            GUI.Label(new Rect(24f, 84f, 300f, 22f), "Space pause · 1/2/4/8 set speed");
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
                view.position = new Vector3(movement.Position.X, 0.55f, movement.Position.Y);
            }
        }
    }
}
