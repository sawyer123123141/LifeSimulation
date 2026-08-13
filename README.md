# LifeSimulation

A high-performance 3D artificial-life simulation about **genetics, natural selection, emergent behavior, ecology, and evolution**.

The long-term goal is a small living world where creatures are not told what species they are or what they are "supposed" to do. Survival strategies should emerge from genes, body traits, needs, perception, memory, the environment, and natural selection.

## Engine and technical direction

- Unity 6
- C#
- Data-oriented simulation core kept separate from Unity presentation
- Burst + Jobs for hot loops after profiling
- Unity GameObjects primarily represent visible creatures, not authoritative biology
- Native C++ only if a measured bottleneck eventually justifies the extra complexity

## Prototype 1: Can evolution happen?

The first prototype intentionally stays small. No procedural planet, giant biome system, elaborate lore engine, or custom zoology department yet.

### World
- Ground-plane simulation movement in a 3D Unity test enclosure
- Renewable food / plants
- Water sources
- Simple terrain and obstacles

### Creature state
- Energy
- Hydration
- Rest
- Health
- Age

### Initial genome
- Body size
- Movement speed
- Metabolism
- Vision range
- Water efficiency
- Food efficiency

Genes must have tradeoffs. Faster or larger creatures should not simply be universally better.

### Brain
The prototype uses a utility-based decision system. Creatures score competing actions from current needs, perceptions, and travel cost; the score terms are inspectable in-game.

Initial actions:
- Wander
- Seek food
- Eat
- Seek water
- Drink
- Rest
- Reproduce

Memory is later prototype work. The system stores enough information to explain **why** an action won.

### Evolution
- Reproduction
- Parent genome crossover
- Mutation
- Heritable traits
- Birth and death
- Population statistics across generations

Prototype success means environmental pressure can measurably shift trait distributions without manually scripting the outcome.

## Current prototype controls

Open the project in Unity 6 and enter Play mode. The runtime bootstrap starts four varied founders near food and water so reproduction is visible.

- `Space`: pause/resume
- `1`, `2`, `4`, `8`: simulation speed
- `B`: baseline resources
- `D`: drought treatment
- `F`: food-scarcity treatment
- `P`: experimental predator/prey mode (12 varied founders)
- `C`: cognition mode (bounded memory and learned resource outcomes)
- `T`: physiology mode (temperature, fertility, and lifespan tradeoffs)

Click a creature to inspect its identity, needs, inherited traits, lineage, current action, and food/water decision scores.

## P1 predator/prey status

P1 is in active development. The current deterministic slice has inherited attack, defense, maneuverability, fear, aggression, and diet traits; local creature perception; chase/flee choices; deterministic attacks and wounds; predation deaths; and contested edible carcasses. There are no predator or prey labels—roles emerge from traits and outcomes.

This is not yet the final P1 evidence gate: population-cycle experiments, predator-removal controls, trophic statistics, and Unity validation remain to be completed.

## Active P2 and P3 slices

P2 adds fixed-size creature memory, confidence decay, failed-search penalties, and lifetime learned food/water outcome values. A creature inherits learning capacity, retention, and exploration tendencies—not learned facts. P3 currently adds deterministic temperature stress and inherited tolerance, fertility investment, and lifespan tradeoffs. These features are enabled only in their matching prototype modes so earlier evidence remains reproducible.

For headless evidence sweeps, run `LifeSimulation.EditorTools.PrototypeBatchEntry.RunPrototype1Experiments`. Defaults are five paired seeds, 50 founders, and 20,000 ticks. Override them without changing source with `-lifeSimFirstSeed`, `-lifeSimSeedCount`, `-lifeSimFounders`, `-lifeSimMaximumPopulation`, and `-lifeSimTicks`. The drought evidence configuration is `-lifeSimSeedCount 20 -lifeSimFounders 100 -lifeSimMaximumPopulation 1500 -lifeSimTicks 100000`. Results are written to ignored `ExperimentResults/` CSV files, including a paired statistical summary.

## Architecture rule

> The simulation must not depend heavily on Unity GameObjects or MonoBehaviour update loops.

```text
SimulationWorld
├── CreatureState
├── Genome
├── SpatialGrid
├── PerceptionSystem
├── DecisionSystem
├── MetabolismSystem
├── ReproductionSystem
└── EnvironmentSystem
        │
        ▼
Unity Presentation
├── Creature visuals
├── Animation
├── Terrain
├── Camera
├── UI
└── Debug overlays
```

A creature can eventually exist in the simulation without requiring a fully rendered GameObject.

## Performance philosophy

The key metric is not just FPS. It is **simulation throughput**: how many creature-seconds the computer can simulate per real second.

Track at minimum:
- active creature count
- simulation ticks / second
- perception queries / second
- decision evaluations / second
- milliseconds per simulation system
- memory usage
- rendered creature count
- simulation speed multiplier

The project should be designed for scaling from hundreds of fully simulated creatures toward much larger populations using batching, spatial partitioning, different update rates, Burst / Jobs, and eventually simulation LOD.

## Visual direction

Simple, readable, stylized 3D. Prototype assets can be premade low-poly/cartoon creatures with basic idle, walk, run, eat, drink, and rest animations.

Visuals are a presentation layer. Many genetically different creatures may share one base model while genes alter scale, proportions, materials, patterns, or optional body parts later.

## Long-term possibilities

After the core evolution loop is proven:
- predators and prey
- food chains and apex predators that emerge from traits rather than labels
- memory and learning
- plant genetics and evolution
- coevolution
- social / herd / pack behavior
- species detection by genetic distance
- evolutionary trees and extinct branches
- climate shifts and ecological history
- multiple biomes
- generated terrain
- large-world simulation LOD
- a small spherical world with zoomable planetary history
- fictional evolvable creature body plans

See [`docs/`](docs/) for the detailed architecture, roadmap, prototype requirements, and performance strategy.
