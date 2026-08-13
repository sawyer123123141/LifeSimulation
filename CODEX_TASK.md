# Codex Bootstrap Task

Create the actual Unity 6 project around the architecture already documented in this repository.

## Required setup

- Unity 6
- C#
- 3D project
- Keep simulation code separate from Unity presentation code
- Do not implement a giant all-in-one `CreatureMonoBehaviour`
- Do not prematurely introduce full DOTS/ECS
- Keep hot simulation data compact and batch-friendly so it can migrate to Burst/Jobs later

## Folder direction

```text
Assets/
  Scripts/
    Simulation/
      Core/
      Biology/
      Genetics/
      Behavior/
      Spatial/
      Systems/
    Presentation/
    UI/
    Debug/
  Scenes/
  Prefabs/
  Art/
  Materials/
  Animations/
Tests/
  EditMode/
  PlayMode/
```

## First implementation target

Build only enough to establish the simulation skeleton:

1. `SimulationWorld`
2. `CreatureState`
3. `Genome`
4. deterministic simulation seed
5. simulation clock / speed multiplier
6. spawn/remove creature support
7. basic needs ticking
8. tests for deterministic needs updates
9. a minimal Unity bootstrap MonoBehaviour that owns/steps `SimulationWorld`
10. basic performance counters

Do not add final models, procedural planets, predators, pathfinding, or polished UI yet.

## Architecture expectation

Conceptually:

```csharp
SimulationWorld world = new SimulationWorld(config);
world.Step(simulationDeltaTime);
```

Unity should step the world and visualize selected state. The simulation should not require one authoritative GameObject per creature.

## Quality requirements

- compile cleanly
- no per-tick garbage in obvious hot loops
- avoid unnecessary abstractions
- small focused classes/systems
- comments explain non-obvious decisions, not obvious syntax
- add tests for important biological math
- update docs if implementation forces an architectural change
- benchmark before claiming an optimization helped

Read `README.md` and everything in `docs/` before implementing.
