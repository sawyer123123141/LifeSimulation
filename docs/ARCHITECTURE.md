# Architecture

## Core principle

Keep **simulation truth** separate from **rendering truth**.

A creature may exist in the simulation without owning a Unity `GameObject`. A visible object mirrors simulation state; it is not the authoritative biological object.

## Layers

### 1. Simulation Core

Responsibilities:
- creature state
- genomes
- needs
- reproduction
- mutation
- behavior decisions
- environment state
- spatial indexing
- population statistics

Keep this layer as engine-independent as practical.

### 2. Simulation Scheduling

Different systems should update at different frequencies. A creature walking toward water does not need to reconsider its entire life 60 times every second.

Initial rates to test:

```text
Movement            10-30 Hz
Perception           2-10 Hz
Decision making       2-5 Hz
Needs / metabolism    1-5 Hz
Population stats       ~1 Hz
Rendering             60+ FPS
```

These are starting points, not laws.

### 3. Presentation

Unity-facing systems:
- meshes
- materials
- animation
- camera
- UI
- selection / inspection
- debug overlays

## Creature representation

Avoid one giant `Creature : MonoBehaviour` containing genetics, biology, AI, movement, memory, and rendering.

Prefer compact state processed in batches.

```csharp
public struct CreatureState
{
    public int Id;
    public int GenomeIndex;

    public float Energy;
    public float Hydration;
    public float Rest;
    public float Health;
    public float Age;

    public CreatureAction CurrentAction;
}
```

## Genome

Prototype genes should be compact numeric values.

```csharp
public struct Genome
{
    public float BodySize;
    public float MoveSpeed;
    public float Metabolism;
    public float VisionRange;
    public float WaterEfficiency;
    public float FoodEfficiency;
    public float Fear;
    public float Aggression;
}
```

Later genes may affect morphology, sensory traits, fertility, digestion, temperature tolerance, cognition, social behavior, and other biology.

## Utility brain

The first brain is utility-based.

Each possible action receives a score. Example:

```text
SeekWater =
    thirst urgency
  × perceived/remembered water confidence
  - danger penalty
  - travel cost
```

Genes can modify these scores. Two creatures in the same situation may therefore choose differently.

Store the component scores for debugging so the selected-creature UI can explain the decision.

## Perception

Never compare every creature with every other creature.

Prototype strategy:
1. uniform spatial grid / spatial hash
2. query nearby cells
3. filter candidates by distance and sensory range

This avoids an O(N²) perception disaster dressed up as an ecosystem.

## Movement and navigation

Do not begin with full A* pathfinding for every animal.

Prefer:
- coarse target direction
- local steering
- obstacle avoidance
- simple environmental memory later

This is cheaper and often more animal-like.

## Rendering separation

`CreatureVisual` should receive presentation data such as:
- position
- facing
- body size
- phenotype parameters
- current action / animation state

It should not own authoritative genetics, metabolism, reproduction, or decision state.

## Future simulation LOD

Long-term scaling model:

```text
Near       full individual simulation
Medium     lower-frequency perception + decisions
Far        coarse individual simulation
Very far   population/statistical simulation
```

This lets the world eventually contain far more life than the renderer displays at once.
