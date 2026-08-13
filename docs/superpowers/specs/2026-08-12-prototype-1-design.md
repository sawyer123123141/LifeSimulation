# Prototype 1 Architecture: Can Evolution Happen?

## Purpose

Prototype 1 must demonstrate that environmental pressure can change heritable trait distributions across generations without directly scripting the desired outcome. It is a controlled artificial-life experiment presented in Unity, not yet a complete ecosystem game.

The cross-prototype extension strategy is defined in `2026-08-12-product-architecture.md`. Prototype 1 establishes the regional simulation kernel, event seam, and experiment harness reused by later prototypes.

The causal loop is:

```text
genome
  -> phenotype, capabilities, and biological costs
  -> resource access, survival, and reproduction
  -> inherited variation
  -> population-level trait change under environmental pressure
```

Correctness, determinism, explainability, and simulation throughput take priority over visual polish and speculative flexibility.

## Scope

Prototype 1 includes:

- deterministic fixed-step simulation
- compact creature and genome data
- energy, hydration, rest, health, and age
- renewable food and water resources
- ground-plane movement in a 3D Unity scene
- bounded uniform-grid spatial queries
- utility-based action selection with diagnostics
- two-parent reproduction without biological sexes
- genome crossover and mutation
- birth, biological death, and fixed maximum age
- controlled baseline, drought, and food-scarcity experiments
- selected-creature and population diagnostics
- a thin, pooled Unity presentation layer
- deterministic tests, biological-math tests, experiment checks, and benchmarks

The required performance target is 1,000 fully simulated creatures at 10x real time in a non-development headless build on the current development computer. The stretch target is 10,000 headless creatures with reduced perception and decision frequencies. Rendering capacity is independent of simulated population.

## Explicit non-goals

Prototype 1 does not include:

- predators, combat, fear, aggression, or fleeing
- biological sexes, sexual dimorphism, or advanced mate preference
- advanced kinship or inbreeding mechanics
- memory, learning, social behavior, herds, or packs
- plant genetics or coevolution
- species clustering or evolutionary trees
- disease, temperature, seasons, or climate simulation
- pathfinding, volumetric movement, or a spherical world
- procedural terrain, multiple finished biomes, or simulation LOD
- final models, animation sets, polished UI, multiplayer, or save games
- DOTS/ECS, Burst, Jobs, or native C++ before profiling justifies them

Fear, aggression, fleeing, and threat perception are added with the predator/prey milestone rather than carried as inactive Prototype 1 data.

## Architectural decision

### Selected approach: dense domain-column storage

`SimulationWorld` owns dense arrays of small structs grouped by processing domain. All arrays use the same dense creature index. Stable creature IDs are distinct from mutable dense indexes.

This approach provides contiguous batch processing and a direct Burst/Jobs migration path without the indirection and framework cost of a custom ECS. It is also clearer than a single large creature struct because each system touches only its relevant columns.

Rejected alternatives:

1. Per-creature objects or MonoBehaviours are easy to start but impede batching, create allocation pressure, and mix simulation with presentation.
2. A generic ECS-like component registry adds abstractions and structural complexity before Prototype 1 needs dynamic component composition.

### Dependency direction

```text
LifeSimulation.Simulation (pure C#)
        ^
        |
LifeSimulation.Unity (Unity bootstrap, presentation, UI, debug)
        ^
        |
Unity scenes and authoring assets
```

The simulation assembly must not reference `UnityEngine`. Unity converts authored settings into an immutable pure-C# `SimulationConfig` before constructing the world.

## World ownership

`SimulationWorld` is the sole coordinator of simulation truth. It owns:

- immutable validated `SimulationConfig`
- simulation tick and elapsed simulated time
- `CreatureStore`
- `ResourceStore`
- uniform spatial grids
- focused simulation systems
- reused structural-change and interaction buffers
- current and cumulative metrics
- deterministic state hashing support
- a fixed-capacity simulation event buffer drained outside hot loops

Conceptually:

```csharp
SimulationWorld world = new SimulationWorld(config);
world.Step(config.FixedDeltaTime);
```

`Step` advances exactly one fixed tick. It rejects a delta different from the configured fixed delta in validation builds. Pacing, pause, and speed multipliers determine how many fixed ticks are requested; they never alter the simulation delta.

## Simulation data model

### Aligned creature columns

Living creatures occupy indexes `[0, Count)`. The store maintains aligned arrays for:

- `CreatureIdentity`: ID, parent IDs, generation, birth tick
- `CreatureNeeds`: current energy, hydration, rest, health, and age
- `CreatureKinematics`: current/previous position, facing, and movement effort
- `CreatureBehavior`: action, target, action timing, and deterministic wander state
- `CreatureReproduction`: maturity, cooldown tick, willingness, and offspring count
- `Genome`: six normalized heritable genes
- `Phenotype`: values derived from the genome at birth
- `PerceptionState`: nearest currently perceived food, water, and eligible mate
- `DecisionDiagnostics`: candidate totals and winning-score components

The structs are blittable and contain no managed references. Arrays grow geometrically outside hot loops and may be preallocated from expected population. The expected maximum population is a safety limit, not an ecological carrying-capacity mechanism.

Grouped structs are the starting layout. Profiling may later justify splitting an especially hot group into separate primitive arrays without changing `SimulationWorld` or presentation APIs.

### Stable identity and compact removal

Creature IDs are monotonically increasing signed 64-bit integers and are never reused within a world. Dense indexes are not exposed as lasting identity.

An ID-to-index lookup supports stable targets and inspection. Removal uses swap-back compaction across every aligned array:

1. Remove the ID mapping for the dead creature.
2. Copy the final dense slot into the removed slot.
3. Update the moved creature's ID mapping.
4. Decrement `Count`.

Systems do not add or remove creatures while iterating. They emit pending birth and death records into preallocated buffers. Marked-dead creatures are skipped immediately, while births and physical compaction occur at the lifecycle boundary near the end of the tick.

Rejected births caused by the safety population limit do not charge the parents. Rejections are counted because repeatedly reaching the limit would introduce artificial selection pressure and invalidate an experiment.

### Resources

Food patches and water sources use a separate dense `ResourceStore`. Prototype resources are created with the world and retain stable slots for the run; depletion sets available amount to zero rather than removing the resource.

Each resource contains:

- stable resource ID and kind
- 2D simulation position and interaction radius
- current amount and capacity
- regeneration rate
- active flag controlled by scheduled experiment commands

Food and water use the same storage and allocation mechanism but separate configuration ranges. Finite water capacity and recharge make drought and competition measurable; water is not an infinite trigger volume.

## Deterministic randomness

The simulation does not use a shared mutable `System.Random`. Random values are generated from deterministic keys containing:

```text
world seed
+ system identifier
+ simulation tick or birth ordinal
+ creature/resource/parent IDs
+ draw-purpose index
```

Parent IDs are sorted before constructing a reproduction key. Draw-purpose indexes are fixed by meaning, such as crossover choice, mutation occurrence, mutation magnitude, or wander direction. Adding an unrelated draw therefore does not shift subsequent random outcomes.

This makes random outcomes independent of dense-array order and prepares random work for later parallel execution.

The determinism contract is: the same seed, validated configuration, scheduled commands, build, platform, and number of ticks produce the same simulation-state hash. Cross-platform bit-identical floating-point behavior is not required for Prototype 1.

## Clock and scheduling

### Fixed time

The base simulation frequency is 20 Hz, giving a fixed delta of 0.05 simulated seconds.

Starting system frequencies are exact divisors of the base rate:

| Work | Frequency |
|---|---:|
| Movement and action interaction | 20 Hz |
| Spatial rebuild and perception | 4 Hz |
| Needs, aging, and health | 2 Hz |
| Utility decisions | 2 Hz |
| Resource regeneration | 1 Hz |
| Reproduction matching | 1 Hz |
| Population statistics | 1 Hz |
| Unity rendering | independent |

Decision ticks always receive fresh perception. These frequencies are configuration values constrained to deterministic integer tick intervals.

### Tick order

Each base tick performs scheduled work in this order:

1. Apply experiment commands due at this tick.
2. Regenerate resources when scheduled.
3. Update needs, age, and health when scheduled.
4. Mark creatures that died biologically.
5. Rebuild spatial indexes and refresh perception when scheduled.
6. Evaluate utility decisions when scheduled.
7. Integrate movement and collect action-interaction requests.
8. Resolve food and water requests and apply allocations.
9. Match willing, eligible reproductive pairs when scheduled.
10. Apply deaths and successful births.
11. Capture population statistics and timing samples when scheduled.

Actions persist between decision ticks. An action commitment and inertia bonus prevent rapid action oscillation while invalid targets are cleared immediately.

### Pacing

Unity's simulation pacer accumulates unscaled real time multiplied by the requested simulation speed and calls fixed steps. Interactive mode enforces a configurable real-frame tick budget and reports any backlog rather than freezing the UI. A headless runner processes fixed ticks continuously without a rendering budget.

Pause and speed settings are pacing controls, not biology inputs. Running the same number of ticks at 1x and 100x produces the same state hash.

## Genome and phenotype

### Active genes

Prototype 1 uses six normalized `[0, 1]` genes:

- body size
- movement speed
- metabolic pace
- vision range
- water efficiency
- food efficiency

Genes are clamped only at creation, crossover, and mutation boundaries. Systems consume cached phenotype values rather than repeatedly converting genes.

### Configurable phenotype mapping

The following formulas establish the intended relationships. Numerical coefficients live in validated biology configuration so balancing does not require structural code changes.

```text
body mass       = 0.6 * 4^(body-size gene)       // 0.6 to 2.4 mass units
maximum speed   = 1.0 + 3.0 * speed gene         // 1 to 4 m/s
metabolic pace  = 0.7 + 0.8 * metabolism gene    // 0.7 to 1.5
vision range    = 4.0 + 12.0 * vision gene        // 4 to 16 m
food yield      = 0.75 + 0.5 * food-efficiency gene
water-loss mult = 1.0 - 0.55 * water-efficiency gene
```

Capacities scale from body mass:

```text
energy capacity    proportional to mass
hydration capacity proportional to mass^0.8
health capacity    proportional to mass^0.67
rest capacity      fixed at 100 units
```

### Explicit trade-offs

| Gene | Benefit | Cost |
|---|---|---|
| Body size | greater reserves, health, and starvation buffer | greater basal, movement, and reproduction costs |
| Movement speed | reaches scarce resources and mates sooner | nonlinear energy cost while moving plus small muscle-maintenance cost |
| Metabolic pace | faster eating, digestion, and rest recovery | higher basal energy and hydration drain |
| Vision range | detects resources and mates farther away | small ongoing sensory energy/rest cost |
| Water efficiency | lower hydration loss | increasing metabolic energy overhead |
| Food efficiency | more energy per unit of food | slower maximum ingestion and small digestive overhead |

No gene is intended to be universally optimal. Interactions are deliberate: a fast creature pays heavily only when it uses speed, while maintaining high speed still has a smaller baseline cost; large creatures have greater endurance but consume more total resources.

### Biological rates

Basal energy drain is proportional to:

```text
base rate
* mass^0.75
* metabolic pace
* (1 + maintenance costs from speed, vision, water efficiency, and food efficiency)
```

Movement energy is proportional to distance, body mass, and a nonlinear function of actual speed. Hydration loss scales with body mass and metabolic pace, then applies water efficiency. Rest decays while awake or active and recovers only while resting; higher metabolic pace permits faster recovery at its continuing energy cost.

Eating converts an allocated amount of food biomass into energy using food yield, subject to an ingestion-rate limit. High food efficiency extracts more total energy but takes longer. Drinking similarly has a rate limit. Needs are stored in absolute units; utility and UI use normalized fractions derived from phenotype capacities.

### Health and death

Health declines when energy or hydration is empty and more slowly when rest is empty. Health can recover slowly while all needs are above configured safety thresholds, with an additional recovery bonus while resting.

Creatures die when health reaches zero or when age reaches the configured fixed Prototype 1 maximum lifespan. Lifespan is not heritable yet. Extinction is recorded as a valid experimental outcome; the simulation never silently introduces replacement creatures.

All biological rates are integrated using the scheduled fixed biological delta, making results independent of rendering and pacing.

## Spatial model, perception, and movement

### Simulation space

Simulation positions are custom blittable 2D vectors on the XZ ground plane. Unity supplies visual height separately. The core experiment arena is an open bounded rectangle; boundary steering keeps creatures inside it.

Complex obstacles are deferred because they would confound early resource-selection experiments and introduce navigation work unrelated to the evolution proof.

### Uniform grid

The arena uses a bounded dense uniform grid rather than a dictionary-based spatial hash. A rebuild uses reused arrays:

- count occupants per cell
- calculate prefix offsets
- fill a contiguous array of creature indexes

Perception visits only cells intersecting the creature's vision radius. Variable vision ranges span the required number of cells. Static resources have a separate grid rebuilt only when their active state or placement changes.

Perception retains compact nearest-result summaries rather than variable candidate lists:

- nearest available food and distance
- nearest available water and distance
- nearest eligible mate and distance
- observation tick and validity flags

Distance ties resolve by stable ID. Perception counters record visited cells, examined candidates, and completed queries.

### Movement

Movement uses a desired direction toward the active target or a deterministic wander heading. Desired speed depends on action urgency and maximum phenotype speed. There is no A* pathfinding.

Movement records distance and effort for metabolism. Target IDs remain stable across dense compaction. Depleted or dead targets become invalid and cause the action to fall back at the next valid decision boundary.

Presentation interpolates between previous and current simulation positions. Interpolation never feeds back into simulation truth.

## Utility behavior

### Prototype actions

The active action set is:

- Wander
- SeekFood
- Eat
- SeekWater
- Drink
- Rest
- SeekMate
- Reproduce

`Reproduce` is the short interaction state created when a valid pair is matched; it is not a continuous search behavior.

### Scoring

Each decision calculates normalized need urgencies with configurable nonlinear response curves. A candidate score is composed from a small fixed set of terms:

```text
need or reproductive drive
+ target confidence/availability
- normalized travel cost
- projected energy cost
+ current-action inertia
+ eligibility/proximity term
```

Impossible actions receive an invalid score rather than a large negative magic number. Ties resolve by a documented fixed action order. A minimum commitment interval and modest inertia reduce thrashing, but urgent needs can still override the current action.

Genes influence behavior through actual capabilities, costs, and sensed opportunities. Prototype 1 does not add arbitrary hidden personality multipliers merely to force behavioral variety.

### Explainability

Each creature retains a compact latest-decision record:

- total score for every fixed action
- winning action
- winning need/drive term
- availability term
- distance term
- energy-cost term
- inertia term
- decision tick and target

This fixed-size record is cheap enough for all creatures. The Unity debug panel may retain a deeper ring history only for the selected creature; history is presentation data and is not part of simulation truth.

## Resource interaction

Eating and drinking produce fixed-buffer requests rather than immediately mutating a shared resource. Requests are grouped by resource. If supply covers all requests, each receives its requested amount. If supply is insufficient, available supply is divided proportionally among valid requests.

This avoids a dense-index first-come advantage during contention. Applied allocations update both resource amount and creature needs in a second pass. Resource amounts are clamped within `[0, capacity]`, and conservation checks verify that allocation never creates biomass or water.

Drought and food-scarcity scenarios alter only scheduled environmental configuration: active sources, capacities, initial amounts, or regeneration rates. They do not modify creature equations.

## Reproduction and inheritance

### Eligibility and matching

Prototype 1 has two-parent reproduction without sexes. A creature is eligible when it:

- has reached maturity age
- is alive and not already paired
- exceeds configured health, energy, hydration, and rest thresholds
- is past its reproduction cooldown
- currently expresses sufficient reproduction utility

Eligible creatures use spatial queries to find nearby eligible mates. Once per reproduction tick, candidates are processed in stable-ID order. Each creature can join at most one pair. The nearest valid willing mate wins, with stable ID as the tie-breaker.

There is no kinship rejection or inbreeding penalty in Prototype 1. Parent-child and sibling relationships remain observable through parent IDs but do not change eligibility.

### Successful birth

For a valid pair:

1. Confirm room below the safety population limit.
2. Create a child genome through uniform per-gene crossover.
3. Apply independent bounded mutation to each gene.
4. Create phenotype and initial state.
5. Place the child near the parents using deterministic bounded jitter.
6. Charge both parents only after birth succeeds.
7. Set both cooldowns and increment offspring counts.

Each gene chooses one parent with equal probability. The starting mutation model uses a configurable per-gene probability and zero-mean Gaussian perturbation with a small standard deviation, clamped to `[0, 1]`. Rare large mutations are deferred.

The child stores both parent IDs, birth tick, and `max(parent generation) + 1`. A single lineage ID is deliberately omitted because two-parent ancestry is a graph; evolutionary-tree storage belongs to a later milestone.

## Experiments and evidence

### Scenario definition

Every run records:

- world seed
- simulation and scenario configuration hash
- build and commit identifier when available
- fixed step and system frequencies
- founder-population parameters
- treatment commands
- safety population limit
- benchmark hardware/build metadata when relevant

The same founder seed creates the same initial population before control or treatment resource settings differ. This permits paired comparisons.

### Required scenarios

1. Baseline control: ordinary food and water availability.
2. Drought: fewer active water sources, lower water capacity, or reduced recharge.
3. Food scarcity: reduced food capacity or regeneration.

Experiments run for a fixed tick duration with periodic population snapshots. Extinction, population-cap contact, invalid numerical state, and rejected births are explicit outcomes, never silently repaired.

### Measurements

Snapshots include:

- population, births, deaths, and extinction state
- simulated time and generation distribution
- mean, variance, minimum, maximum, and fixed quantiles for each gene
- corresponding phenotype summaries
- average age and normalized needs
- resource availability and consumption
- cumulative reproduction by trait bins
- simulation throughput and system timings

Death and end-of-run summaries retain genome, lifespan, cause of death, and offspring count outside hot creature storage. This provides a mechanistic link between traits and reproductive success without retaining a complete world history.

The simulation also emits compact versioned birth, death, reproduction, and environment-change events into a fixed-capacity buffer. The Unity/headless host drains that buffer to experiment aggregation or storage outside the fixed-step loop. Buffer overflow invalidates an experiment rather than silently losing the history needed by later lineage analysis.

### Evolution-proof criterion

Run at least 20 paired seeds for control and each treatment. A treatment supports the Prototype 1 claim when:

- a treatment-relevant heritable trait has a paired final-distribution shift whose 95% bootstrap confidence interval excludes zero
- the standardized effect magnitude is at least moderate (`|d| >= 0.5`)
- the direction is consistent in at least 75% of paired seeds
- survival or reproductive-output data provides a plausible mechanistic link
- the safety population cap does not drive the result
- the result is reproducible from recorded seeds and configuration

Extinction rates and counterintuitive results are reported honestly. Equations are not retuned merely to force an expected graph; changes require a documented biological or numerical defect and rerunning both control and treatment.

## Unity boundary and presentation

### Bootstrap

A minimal Unity bootstrap:

- converts serialized authoring settings into a validated `SimulationConfig`
- creates and owns one `SimulationWorld`
- advances it through the fixed-step pacer
- publishes read-only state to presentation and debug consumers
- supports pause and configured speed multipliers

No per-creature MonoBehaviour owns biology or decisions.

### Visual pool

A capped visual pool maps stable creature IDs to reusable GameObjects. Visuals read position, facing, size, needs tint, and action state. They do not write authoritative simulation fields.

The presentation policy may render all creatures below the cap, then prefer selected and camera-near creatures above it. Despawning a visual does not remove the simulated creature. Rendering counters report simulated and visible counts separately.

Prototype visuals use primitives or temporary low-poly assets. Final models and animation production remain out of scope; action-to-animation hooks may exist without requiring finished animations.

### Debug interface

A minimal runtime UI Toolkit panel is created programmatically to avoid a large set of layout assets. It provides:

- pause, 1x, 5x, 10x, 25x, and 100x controls
- population, births, deaths, tick, simulated time, and backlog
- simulation throughput, visible count, and per-system timings
- selected creature identity, parents, generation, age, needs, genome, phenotype, action, target, and decision explanation
- scenario/seed identifiers and experiment validity warnings

Selection maps a visual back to a stable creature ID. A creature with no visual remains inspectable through ID-based debug tools and experiment output.

## Metrics and performance

### Always-available counters

- active and peak creatures
- births, deaths by cause, and rejected births
- fixed ticks and simulated seconds
- perception queries and candidates examined
- decision evaluations
- resource requests and allocations
- rendered creature count
- requested and achieved speed multiplier
- tick backlog

### Timing

Pure C# timing uses `Stopwatch.GetTimestamp` around system batches, with fixed-size rolling samples and no per-tick collections. Instrumentation can be disabled for throughput comparisons. The Unity layer adds profiler markers at the batch boundary without contaminating the simulation assembly.

Report average and p95 total step time plus per-system totals. Report creature-seconds simulated per real second as the primary throughput metric.

### Required performance gate

After warm-up, a non-development headless build with 1,000 creatures must simulate 600 seconds in at most 60 real seconds on the current development computer:

- at least 10x achieved simulation speed
- average fixed-step time at or below 5 ms
- no recurring managed allocation in obvious hot tick loops
- no population-cap contact during the benchmark
- recorded p95 step time, memory, query counts, and system timings

The 10,000-creature headless run is a stretch measurement, not a completion blocker. It may lower perception and decision frequencies but must record those settings.

No optimization claim is accepted without before/after measurements from the same scenario, build type, hardware, and commit.

## Burst and Jobs migration path

`SimulationWorld` remains the coordinator. Migration happens one measured batch at a time:

1. Preserve store and system inputs/outputs.
2. Replace the system's managed arrays with native mirrors or native ownership at the boundary justified by profiling.
3. Port the pure numerical loop to a Burst-compatible job.
4. Keep structural changes in deterministic command buffers.
5. Compare state hashes where supported and biological outcomes across paired seeds.
6. Benchmark before keeping the migration.

Likely candidates are metabolism, movement integration, spatial-grid construction, perception filtering, and utility scoring. Reproduction conflict resolution and structural changes can remain sequential because they run less frequently and benefit from stable ordering.

Full ECS remains optional. The design does not require replacing the world or public read APIs to accelerate individual systems.

## Validation and failure handling

Construction rejects invalid configuration: negative rates, inconsistent capacities, non-divisible schedules, invalid arena/grid sizes, or a maximum population below founder count.

Runtime invariants include:

- aligned store counts and valid ID mappings
- finite positions, needs, genes, and resource amounts
- normalized genes within range
- needs and resources within configured bounds
- valid targets or explicit invalid handles
- no creature paired or removed twice in one tick
- resource allocations not exceeding available supply

Development and experiment modes fail fast on NaN, infinity, or broken store invariants. Invalid targets from ordinary deaths or depletion are cleared as expected state, not treated as exceptional failures.

## Test strategy

### EditMode simulation tests

- same seed/config/commands produce identical state hashes
- pacing and step batching do not change results for equal tick counts
- spawn, lookup, swap-back removal, and monotonic IDs remain correct
- structural queues do not skip or double-process creatures
- each gene mapping stays within documented bounds
- every active gene demonstrates its configured benefit and cost
- needs integration is deterministic and independent of rendering delta
- starvation, dehydration, exhaustion, recovery, aging, and death thresholds work
- spatial queries match brute force in generated small worlds
- perception tie-breaking uses stable IDs
- utility candidates and diagnostic components agree
- resource allocation conserves supply and avoids first-index priority
- reproductive matching creates no duplicate pairs
- crossover and mutation are deterministic, bounded, and statistically unbiased within tolerance
- parents are charged only for successful births
- experiment commands affect resources, not biological equations

### PlayMode boundary tests

- Unity bootstrap creates and advances one world
- pause and speed controls preserve fixed-step results
- visuals mirror state without owning it
- visual pooling respects the render cap
- selection survives dense-index changes through stable IDs

### Experiment checks

- paired scenarios start from identical founders
- recorded manifests can reproduce final hashes
- extinction and population-cap contact are reported
- result aggregation produces correct trait statistics on known fixtures

### Benchmarks

Benchmark 100, 500, and 1,000 creatures with rendering disabled, then separately measure presentation costs with configured visible counts. Record release/development status, hardware, seed, config hash, commit, warm-up, duration, average and p95 timings, memory, and throughput.

## Repository organization

Files are grouped by responsibility without creating one file per trivial type. Empty aspirational folders are not committed.

```text
Assets/
  Scripts/
    Simulation/
      Core/          world, configuration, stores, IDs, deterministic math
      Biology/       genome/phenotype, needs, reproduction
      Behavior/      perception, decisions, movement
      Spatial/       bounded uniform grid
      Systems/       resources and population statistics
    Presentation/    bootstrap, pacer, pooled creature visuals
    UI/              runtime debug panel and selection
    Debug/           experiment and benchmark Unity entry points
  Scenes/            one prototype scene
  Settings/          minimal authored configuration assets
Assets/
  Tests/
    EditMode/        grouped simulation tests by subsystem
    PlayMode/        Unity boundary tests
docs/
  superpowers/
    specs/            approved architecture
    plans/            phased implementation plan
```

Assembly definitions enforce dependency direction. Unity imports tests only from `Assets` or packages, so the repository uses `Assets/Tests` rather than the preliminary root-level folder direction. Generated IDE files, Unity caches, builds, logs, benchmark bulk output, and user settings remain ignored. Small curated benchmark summaries and scenario definitions may be committed; large raw run data is stored outside Git.

## Implementation milestones and acceptance gates

### Milestone 0: Unity and assembly bootstrap

- Create the Unity 6 project metadata, minimal packages, folders, and assembly definitions.
- Add a minimal scene and bootstrap boundary without creature functionality.
- Acceptance: Unity 6000.2.14f1 opens and compiles in batch mode; EditMode and PlayMode test assemblies are discoverable.

### Milestone 1: Deterministic simulation skeleton

- Implement config validation, fixed clock, deterministic keyed randomness, stores, IDs, spawn/remove, lifecycle buffers, and core metrics.
- Add minimal bootstrap stepping and deterministic state hash.
- Acceptance: deterministic replay, spawn/removal, pacing-equivalence, invariant, and zero-obvious-hot-allocation tests pass.

### Milestone 2: Genome, phenotype, and needs

- Implement six genes, cached phenotype mapping, biological drains/recovery, movement effort input, aging, health, and death.
- Acceptance: mapping bounds, explicit benefit/cost, deterministic integration, threshold, and biological-math tests pass.

### Milestone 3: Resources and deterministic allocation

- Implement food/water storage, regeneration, scenario commands, request buffering, proportional contention, eating, and drinking.
- Acceptance: conservation, regeneration, rate-limit, scarcity-command, and contention-fairness tests pass.

### Milestone 4: Spatial perception and movement

- Implement bounded grid rebuilds, nearest-result perception, target handles, wandering, seeking, boundary steering, and movement energy.
- Acceptance: grid results match brute force, movement is deterministic, invalid targets recover, and query counts avoid all-to-all behavior.

### Milestone 5: Utility decisions and diagnostics

- Implement active actions, score curves, commitment/inertia, target choice, and fixed-size explanation data.
- Acceptance: scenario fixtures choose expected actions for documented reasons without action thrashing or hidden personality modifiers.

### Milestone 6: Two-parent evolution loop

- Implement eligibility, deterministic matching, crossover, mutation, parent costs, cooldowns, births, and death summaries.
- Acceptance: complete seeded populations survive/reproduce/die; inheritance tests pass; no duplicate pairs or invalid lifecycle changes occur.

### Milestone 7: Experiments and evidence

- Implement control, drought, and food-scarcity scenarios; paired batch runner; manifests; CSV summaries; and statistical aggregation.
- Acceptance: founders pair exactly, runs reproduce from manifests, invalid runs are flagged, and at least one treatment satisfies the documented evolution-proof criterion without population-cap artifacts.

### Milestone 8: Presentation and inspection

- Implement pooled primitive visuals, interpolation, selection, runtime controls, creature explanations, and global metrics.
- Acceptance: simulation works with zero visuals, visible count is capped independently, selected IDs survive compaction, and UI shows recorded simulation truth.

### Milestone 9: Benchmark and optimization decision

- Run 100/500/1,000-creature headless benchmarks and presentation benchmarks.
- Remove only measured bottlenecks that prevent the required gate.
- Acceptance: 1,000 creatures achieve 10x for the defined run, results and environment are recorded, and Burst/Jobs decisions cite measured system costs.

## Approval decisions incorporated

- Complete Prototype 1 is the implementation scope.
- Dense domain-column storage is the selected architecture.
- Two-parent reproduction has no sexes; both parents pay successful-birth costs.
- Fear, aggression, fleeing, predators, and combat are deferred.
- Scale targets are 1,000 creatures at required 10x and 10,000 as a headless stretch measurement.
- The simulation uses same-build/platform deterministic floating-point behavior and keyed randomness.
- Prototype 1 uses an open bounded ground-plane arena and no pathfinding.
- The repository favors a small number of focused files over giant files or trivial file fragmentation.
