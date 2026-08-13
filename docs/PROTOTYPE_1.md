# Prototype 1: Can Evolution Happen?

## Success criteria

Prototype 1 succeeds when all of these are true:

1. A seeded population can survive, reproduce, mutate, and die.
2. Offspring inherit genes from parents with controlled mutation.
3. Genes have real costs and benefits that affect survival/reproduction.
4. Environmental pressure changes population trait distributions over generations.
5. We can click an individual and see what it is doing and why.
6. The simulation runs substantially faster than real time.
7. We have benchmark data before attempting major optimization work.

## Explicitly out of scope

- spherical planet
- procedural planet generation
- multiple finished biomes
- polished original creature models
- plant evolution
- advanced social behavior
- species clustering
- evolutionary tree
- lore/history system
- multiplayer

## Phase A: Simulation skeleton

- simulation clock
- deterministic random seed support
- creature IDs
- compact `CreatureState`
- compact `Genome`
- spawn/remove creatures
- population counters

## Phase B: Biology / needs

Implement:
- energy
- hydration
- rest
- health
- age

Genes should influence costs. Examples:
- higher metabolism burns more energy
- larger body may cost more energy
- water efficiency slows hydration loss
- speed should have an energy cost

## Phase C: Resources

- water zones / points
- renewable food patches
- consumption
- regrowth
- scarcity controls for experiments

## Phase D: Movement + perception

- simulation position
- speed derived partly from genome
- nearby-resource detection
- spatial grid
- target selection
- movement toward target

## Phase E: Utility brain

Initial actions:
- Wander
- SeekFood
- Eat
- SeekWater
- Drink
- Rest
- Flee
- Reproduce

For each decision, store enough diagnostic information to inspect:
- candidate actions
- scores
- relevant need values
- risk/cost terms
- winning action

Example inspector output:

```text
Creature #382
Age: 7.3
Hydration: 12%
Energy: 48%

Current action: Seek Water

Why?
Thirst urgency     +0.91
Water confidence   +0.78
Predator risk      -0.14
Travel cost        -0.09
Final score         0.76
```

## Phase F: Reproduction

- eligibility rules
- mate selection
- genome crossover
- mutation
- offspring spawn
- reproduction energy cost
- generation / lineage tracking where cheap

Mutation should usually be small. Rare larger mutations can come later if needed.

## Phase G: Selection experiments

### Drought test

Reduce water availability and run many generations.

Question: do water-related traits shift naturally?

### Food scarcity test

Reduce food supply.

Question: do metabolism, size, speed, or efficiency traits shift naturally?

Do not tune the equations just to make the expected graph appear. The point is to test whether the system actually creates selection.

## Phase H: Visual layer

Prototype visuals may use temporary/purchased low-poly assets.

Minimum animation states:
- idle
- walk
- run
- eat
- drink
- rest

Body-size gene should visibly affect model scale if practical.

## Phase I: Debug UI

### Selected creature
- ID
- age
- health
- energy
- hydration
- rest
- genome
- current action
- target
- recent utility scores

### Global stats
- population
- births
- deaths
- average age
- average gene values
- generation count / elapsed sim time
- simulation speed
- system timings

### Controls
- pause
- 1x
- 5x
- 25x
- 100x

## Phase J: Benchmark

Test at least:
- 100 creatures
- 500 creatures
- 1,000 creatures

Record:
- total simulation step time
- perception time
- decision time
- needs/metabolism time
- rendering frame time
- memory usage
- simulation ticks/second

Only after this should we decide exactly where Burst and Jobs are needed.
