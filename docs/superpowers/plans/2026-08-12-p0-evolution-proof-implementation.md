# P0 Evolution Proof Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic Unity 6 artificial-life prototype in which two-parent creatures compete for finite food and water, inherit mutated genes, and exhibit measurable treatment-driven evolution.

**Architecture:** A pure-C# `SimulationWorld` owns aligned dense stores and focused batch systems. Unity owns only pacing, pooled visuals, authoring, selection, and debug presentation; experiments and benchmarks drive the same simulation kernel without visuals.

**Tech Stack:** Unity 6000.2.14f1, C#, Unity Test Framework, runtime UI Toolkit, NUnit, batch-mode EditMode/PlayMode tests, CSV/JSON experiment summaries.

## Global constraints

- Use Unity 6000.2.14f1 and C#.
- Keep `LifeSimulation.Simulation` free of `UnityEngine` references.
- Use a 20 Hz fixed simulation step; speed controls alter tick count, never delta.
- Use dense aligned arrays, stable 64-bit IDs, swap-back removal, and buffered structural changes.
- Use keyed deterministic randomness, not shared mutable `System.Random`.
- Do not adopt DOTS/ECS, Burst, Jobs, native C++, pathfinding, predators, cognition, evolving plants, or final art.
- Avoid recurring managed allocation in fixed-tick hot loops.
- Required benchmark: 1,000 creatures simulate 600 seconds in at most 60 real seconds in a non-development headless build on the current computer.
- Commit Unity `.meta` files for tracked assets; ignore caches, builds, logs, raw experiment output, and generated IDE files.
- Every task ends with focused tests and a compiling project.

---

## Planned file map

### Project and repository

- `ProjectSettings/ProjectVersion.txt`: pins Unity 6000.2.14f1.
- `ProjectSettings/EditorSettings.asset`: text serialization and visible metadata.
- `Packages/manifest.json`: minimal Unity packages including Test Framework and UI Toolkit dependencies supplied by Unity.
- `.gitattributes`: stable text handling for C#, JSON, YAML, and Unity serialized assets.
- `.gitignore`: existing Unity rules plus raw benchmark/experiment output.

### Pure simulation

- `Assets/Scripts/Simulation/LifeSimulation.Simulation.asmdef`: Unity-free simulation assembly.
- `Assets/Scripts/Simulation/Core/SimulationTypes.cs`: `SimVector2`, IDs/handles, actions, resource/death enums, and fixed records shared across systems.
- `Assets/Scripts/Simulation/Core/SimulationConfig.cs`: immutable config groups, defaults, schedule validation, and scenario identity.
- `Assets/Scripts/Simulation/Core/DeterministicRandom.cs`: keyed hash/random conversion and Gaussian mutation draw.
- `Assets/Scripts/Simulation/Core/CreatureStore.cs`: aligned domain structs/arrays, capacity management, ID lookup, spawn, and swap-back removal.
- `Assets/Scripts/Simulation/Core/ResourceStore.cs`: stable resource slots and bounded amounts.
- `Assets/Scripts/Simulation/Core/SimulationEvents.cs`: fixed-capacity event buffer and versioned P0 event records.
- `Assets/Scripts/Simulation/Core/SimulationMetrics.cs`: counters, allocation-free rolling timings, and population snapshots.
- `Assets/Scripts/Simulation/Core/SimulationWorld.cs`: fixed tick ordering, lifecycle queues, systems, state hash, and read-only access.
- `Assets/Scripts/Simulation/Biology/GenomePhenotype.cs`: six-gene genome, phenotype mapping, crossover, and mutation.
- `Assets/Scripts/Simulation/Biology/NeedsSystem.cs`: needs, recovery, age, health, and biological death.
- `Assets/Scripts/Simulation/Biology/ReproductionSystem.cs`: eligibility, deterministic pair resolution, parent costs, cooldowns, and birth requests.
- `Assets/Scripts/Simulation/Spatial/UniformGrid.cs`: allocation-free bounded counting/prefix spatial grid.
- `Assets/Scripts/Simulation/Behavior/PerceptionSystem.cs`: nearest food/water/mate observations and query counters.
- `Assets/Scripts/Simulation/Behavior/DecisionSystem.cs`: fixed action scores, target choice, commitment, and diagnostics.
- `Assets/Scripts/Simulation/Behavior/MovementSystem.cs`: wandering, target steering, boundaries, and movement effort.
- `Assets/Scripts/Simulation/Systems/ResourceSystem.cs`: regeneration and proportional request allocation.
- `Assets/Scripts/Simulation/Systems/PopulationStatistics.cs`: fixed-interval gene, phenotype, needs, population, and throughput summaries.
- `Assets/Scripts/Simulation/Systems/ExperimentRunner.cs`: paired scenarios, manifests, result aggregation, and state/result hashes.

### Unity boundary

- `Assets/Scripts/Presentation/LifeSimulation.Unity.asmdef`: references simulation assembly.
- `Assets/Scripts/Presentation/SimulationAuthoring.cs`: serialized authoring values converted to validated pure config.
- `Assets/Scripts/Presentation/SimulationBootstrap.cs`: fixed-step accumulator, pause/speed controls, event drain, and world lifetime.
- `Assets/Scripts/Presentation/CreatureVisualPool.cs`: capped pooled primitive visuals and interpolation.
- `Assets/Scripts/UI/PrototypeDebugPanel.cs`: programmatic UI Toolkit metrics, controls, selection details, and decision explanation.
- `Assets/Scripts/Debug/PrototypeBatchEntry.cs`: batch experiment/benchmark entry points and compact result files.
- `Assets/Editor/ProjectSetup.cs`: idempotently creates the prototype scene/settings assets and permits regeneration.
- `Assets/Scenes/Prototype1.unity`: minimal camera, light, ground, bootstrap, and UI document.
- `Assets/Settings/Prototype1Settings.asset`: authored P0 defaults.

### Tests

- `Tests/EditMode/LifeSimulation.EditModeTests.asmdef`: NUnit simulation tests.
- `Tests/EditMode/CoreSimulationTests.cs`: config, random, stores, lifecycle, events, and replay hashes.
- `Tests/EditMode/BiologyTests.cs`: phenotype bounds/trade-offs, needs, health, age, crossover, mutation, and reproduction.
- `Tests/EditMode/SpatialBehaviorTests.cs`: grid/brute-force comparison, perception, movement, decisions, and diagnostics.
- `Tests/EditMode/ResourceExperimentTests.cs`: conservation, treatments, manifests, statistics, and paired founders.
- `Tests/PlayMode/LifeSimulation.PlayModeTests.asmdef`: Unity boundary tests.
- `Tests/PlayMode/PresentationTests.cs`: bootstrap, pacing, visual pooling, selection, and debug state.

Related small records stay in their responsible file; independent systems remain separate to avoid giant files.

## Task 1: Bootstrap Unity, assemblies, and repository hygiene

**Files:**
- Create project/repository files listed under Project and repository.
- Create: `Assets/Scripts/Simulation/LifeSimulation.Simulation.asmdef`
- Create: `Assets/Scripts/Presentation/LifeSimulation.Unity.asmdef`
- Create test assembly definitions.

**Interfaces:**
- Produces Unity project metadata loadable by Unity 6000.2.14f1.
- Produces `LifeSimulation.Simulation`, `LifeSimulation.Unity`, `LifeSimulation.EditModeTests`, and `LifeSimulation.PlayModeTests` assembly boundaries.

- [ ] **Step 1: Add Unity project/version/package files and text serialization settings**

Pin `m_EditorVersion: 6000.2.14f1`, enable force-text asset serialization, and include only Unity modules required by a built-in 3D project, UI Toolkit, and tests.

- [ ] **Step 2: Add assembly definitions**

`LifeSimulation.Simulation.asmdef` must set `autoReferenced: true` and contain no Unity-specific assembly references. `LifeSimulation.Unity.asmdef` references it by assembly name. Test assemblies reference NUnit/Test Framework and their relevant production assembly.

- [ ] **Step 3: Add Git text/output rules**

Add LF/text handling for `.cs`, `.json`, `.md`, `.asmdef`, `.unity`, `.asset`, and `.meta`. Ignore `/ExperimentResults/`, `/BenchmarkResults/`, and `/TestResults/` while preserving small curated summaries under `docs/`.

- [ ] **Step 4: Open and compile the empty project in batch mode**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -logFile 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim\Logs\bootstrap.log'
```

Expected: Unity exits `0`, imports the project, and reports no compiler errors.

- [ ] **Step 5: Commit**

```powershell
git add .gitattributes .gitignore Assets Packages ProjectSettings Tests
git commit -m "build: bootstrap Unity project"
```

## Task 2: Define core types, config, and deterministic random

**Files:**
- Create: `Assets/Scripts/Simulation/Core/SimulationTypes.cs`
- Create: `Assets/Scripts/Simulation/Core/SimulationConfig.cs`
- Create: `Assets/Scripts/Simulation/Core/DeterministicRandom.cs`
- Test: `Tests/EditMode/CoreSimulationTests.cs`

**Interfaces:**
- Produces `readonly struct SimVector2`, `readonly struct CreatureId`, `readonly struct TargetHandle`, `enum CreatureAction`, `enum ResourceKind`, and `enum DeathCause`.
- Produces `SimulationConfig.CreatePrototype1Defaults(int seed, int initialPopulation)` and `void Validate()`.
- Produces `DeterministicRandom.UInt64(...)`, `Float01(...)`, and `Gaussian(...)` keyed by seed/system/tick/IDs/purpose.

- [ ] **Step 1: Write failing config/vector/random tests**

Cover vector arithmetic/distance, invalid fixed frequencies, invalid capacities, repeatable keyed draws, different purpose slots, sorted-parent reproduction keys, `[0,1)` float range, and bounded Gaussian mutation inputs.

```csharp
[Test]
public void KeyedDrawDoesNotDependOnCallOrder()
{
    float first = DeterministicRandom.Float01(42, RandomDomain.Mutation, 10, 7, 9, 2);
    _ = DeterministicRandom.Float01(42, RandomDomain.Wander, 99, 3, 0, 0);
    float repeated = DeterministicRandom.Float01(42, RandomDomain.Mutation, 10, 7, 9, 2);
    Assert.That(repeated, Is.EqualTo(first));
}
```

- [ ] **Step 2: Run the focused EditMode test and verify failure**

Run Unity EditMode tests filtered to `CoreSimulationTests`; expect missing-type/compiler failures.

- [ ] **Step 3: Implement minimal blittable types and validated config groups**

Config groups must include clock/schedules, capacities, arena/grid, biology, phenotype ranges/costs, behavior, reproduction, resources, metrics, events, and population safety limits. Validation rejects non-finite/negative values and schedules that are not integer base-tick intervals.

- [ ] **Step 4: Implement keyed deterministic random**

Use a stable 64-bit integer mixing function, convert the upper 24 bits to a float, and use Box-Muller with purpose-separated uniform draws for Gaussian mutation. Do not use runtime-dependent object hash codes.

- [ ] **Step 5: Run focused and full EditMode tests**

Expected: all Task 2 tests pass and the simulation assembly has no `UnityEngine` reference.

- [ ] **Step 6: Commit**

```powershell
git add Assets/Scripts/Simulation/Core Tests/EditMode
git commit -m "feat: add deterministic simulation primitives"
```

## Task 3: Build dense stores, event buffer, metrics, and lifecycle world

**Files:**
- Create: `CreatureStore.cs`, `ResourceStore.cs`, `SimulationEvents.cs`, `SimulationMetrics.cs`, `SimulationWorld.cs`.
- Test: `CoreSimulationTests.cs`.

**Interfaces:**
- Produces `CreatureId SimulationWorld.Spawn(in SpawnRequest request)`, `bool TryGetCreatureIndex(CreatureId id, out int index)`, `void RequestDeath(CreatureId id, DeathCause cause)`, `void Step(float fixedDeltaTime)`, and `ulong ComputeStateHash()`.
- Produces read-only `CreatureCount`, `CurrentTick`, `ElapsedSimulationSeconds`, metrics snapshot, and event-drain methods.

- [ ] **Step 1: Write failing store/lifecycle/event tests**

Test aligned counts, geometric capacity, monotonic IDs, lookup, swap-back mapping, duplicate death requests, structural changes after iteration, rejected births, fixed event capacity, overflow invalidation, timing sample rollover, and equal state hashes.

```csharp
[Test]
public void SwapBackRemovalPreservesMovedCreatureLookup()
{
    var world = TestWorld.Create(initialPopulation: 3);
    CreatureId first = world.GetCreatureIdAt(0);
    CreatureId last = world.GetCreatureIdAt(2);
    world.RequestDeath(first, DeathCause.Debug);
    world.Step(world.Config.FixedDeltaTime);
    Assert.That(world.TryGetCreatureIndex(last, out int movedIndex), Is.True);
    Assert.That(movedIndex, Is.EqualTo(0));
}
```

- [ ] **Step 2: Run tests and verify failure**

Expected: failures name the missing stores/world interfaces.

- [ ] **Step 3: Implement aligned stores and ID mapping**

Preallocate from config, grow outside loops, keep every column count aligned, and update the ID map on spawn/swap-back removal. Resource slots remain stable for the run.

- [ ] **Step 4: Implement fixed event/timing buffers and state hash**

Hash deterministic simulation fields in stable-ID order or an explicitly order-independent combination. Exclude wall-clock timings, Unity presentation, and buffer read cursors.

- [ ] **Step 5: Implement minimal world lifecycle and tick validation**

Apply buffered deaths/births only at the lifecycle boundary. For now other scheduled systems are no-ops with metric slots.

- [ ] **Step 6: Run tests and inspect allocations in a 10,000-tick core fixture**

Expected: lifecycle tests pass; after warm-up the fixture reports no recurring managed allocation from `Step`.

- [ ] **Step 7: Commit**

```powershell
git add Assets/Scripts/Simulation/Core Tests/EditMode/CoreSimulationTests.cs
git commit -m "feat: add dense simulation world lifecycle"
```

## Task 4: Implement genome, phenotype, needs, health, and age

**Files:**
- Create: `GenomePhenotype.cs`, `NeedsSystem.cs`.
- Modify: store/config/world files.
- Test: `BiologyTests.cs`.

**Interfaces:**
- Produces `Genome`, `Phenotype Phenotype.FromGenome(in Genome, in BiologyConfig)`, and `Genome RecombineAndMutate(...)`.
- Produces `NeedsSystem.Step(CreatureStore store, in SimulationConfig config, float biologyDelta)` and death requests.

- [ ] **Step 1: Write failing phenotype trade-off tests**

For each gene, compare low/high genomes and assert both documented benefit and cost: size reserves/cost, speed capability/maintenance, metabolism processing/drain, vision range/sensory cost, water retention/energy overhead, and food yield/ingestion rate.

- [ ] **Step 2: Write failing needs/death tests**

Cover deterministic drain, scheduled-delta equivalence, movement effort, eating/drinking limits, rest recovery, health damage/recovery, age death, clamping, and non-finite-state failure.

- [ ] **Step 3: Run Biology tests and verify failure**

Expected: missing genome/needs types.

- [ ] **Step 4: Implement cached phenotype mapping and needs system**

Use the exact equations and ranges in the P0 design spec. Store absolute needs; expose normalized fractions through phenotype capacity helpers.

- [ ] **Step 5: Add biology to scheduled world order**

Run needs at 2 Hz using exactly accumulated scheduled simulated time. Mark health/age deaths before perception and later systems.

- [ ] **Step 6: Run Biology and replay tests**

Expected: all trade-off and threshold tests pass; same seed/ticks retain the same hash.

- [ ] **Step 7: Commit**

```powershell
git add Assets/Scripts/Simulation/Biology Assets/Scripts/Simulation/Core Tests/EditMode
git commit -m "feat: add genome phenotype and needs biology"
```

## Task 5: Implement renewable resources and fair interaction

**Files:**
- Create: `Systems/ResourceSystem.cs`.
- Modify: config/store/world files.
- Test: `ResourceExperimentTests.cs`.

**Interfaces:**
- Produces resource creation/activation commands, `ResourceRequestBuffer`, `Regenerate`, `CollectRequest`, and `ResolveRequests` behavior.
- World actions consume only granted allocation amounts.

- [ ] **Step 1: Write failing conservation and contention tests**

Cover regeneration clamping, inactive sources, finite water, food yield, drink/eat rate limits, insufficient supply, proportional allocation independent of dense order, and no resource creation/loss beyond configured regeneration/consumption.

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Implement static resource slots and scheduled commands**

Scenario commands may change active state, capacity, current amount, and regeneration at a specified tick. Reject commands that produce invalid bounds.

- [ ] **Step 4: Implement two-pass proportional request resolution**

Collect requests without mutating needs/resources, sum requests per resource in reused arrays, calculate allocation ratios, then apply grants and subtract exactly the granted total.

- [ ] **Step 5: Integrate 1 Hz regeneration and 20 Hz action requests into world order**

- [ ] **Step 6: Run resource, replay, and allocation tests**

Expected: conservation holds within documented floating tolerance and reorder fixtures agree.

- [ ] **Step 7: Commit**

```powershell
git add Assets/Scripts/Simulation/Systems Assets/Scripts/Simulation/Core Tests/EditMode
git commit -m "feat: add finite renewable resources"
```

## Task 6: Implement uniform grid, perception, and movement

**Files:**
- Create: `Spatial/UniformGrid.cs`, `Behavior/PerceptionSystem.cs`, `Behavior/MovementSystem.cs`.
- Modify: stores/config/world.
- Test: `SpatialBehaviorTests.cs`.

**Interfaces:**
- Produces `UniformGrid.Rebuild(...)` and bounded cell iteration.
- Produces 4 Hz compact perception summaries and 20 Hz movement/effort updates.

- [ ] **Step 1: Write failing grid versus brute-force tests**

Generate deterministic small worlds and compare nearest food, water, and eligible mate for edge cells, empty cells, variable radii, ties, inactive/depleted resources, and dead creatures.

- [ ] **Step 2: Write failing movement tests**

Cover target steering, deterministic wander, speed bounds, fixed-step equivalence, previous/current position, arena boundaries, target invalidation, and mass/speed-dependent effort.

- [ ] **Step 3: Run focused tests and verify failure**

- [ ] **Step 4: Implement allocation-free counting/prefix grid**

Use reusable count, prefix, cursor, and contents arrays. Validate positions/cells and resolve equal-distance candidates by stable ID.

- [ ] **Step 5: Implement compact perception and movement**

Perception stores only nearest summaries. Wander draws use keyed tick/ID/purpose values. Movement stores effort consumed by the next biology update.

- [ ] **Step 6: Integrate scheduled order and counters**

Perception/grid run together at 4 Hz before every 2 Hz decision tick; movement runs every base tick. Record cells/candidates/queries.

- [ ] **Step 7: Run spatial, movement, replay, and allocation tests**

Expected: grid equals brute force; candidate counts demonstrate locality on a 1,000-creature fixture.

- [ ] **Step 8: Commit**

```powershell
git add Assets/Scripts/Simulation/Spatial Assets/Scripts/Simulation/Behavior Tests/EditMode
git commit -m "feat: add spatial perception and movement"
```

## Task 7: Implement utility decisions and explanations

**Files:**
- Create: `Behavior/DecisionSystem.cs`.
- Modify: behavior structs/config/world.
- Test: `SpatialBehaviorTests.cs`.

**Interfaces:**
- Produces 2 Hz evaluation for Wander, SeekFood, Eat, SeekWater, Drink, Rest, SeekMate, and Reproduce interaction state.
- Produces fixed-size `DecisionDiagnostics` totals and winning score components.

- [ ] **Step 1: Write failing action fixture tests**

Create explicit fixtures for critical thirst, critical hunger, low rest, nearby consumable resource, distant resource, reproduction readiness, no valid targets, current-action inertia, invalid-target override, and enum-order tie-breaking.

```csharp
[Test]
public void SevereThirstSelectsVisibleWaterAndExplainsScore()
{
    var fixture = DecisionFixture.ThirstyCreatureWithWater();
    fixture.StepDecision();
    Assert.That(fixture.Action, Is.EqualTo(CreatureAction.SeekWater));
    Assert.That(fixture.Diagnostics.WinningNeed, Is.GreaterThan(0f));
    Assert.That(fixture.Diagnostics.Availability, Is.GreaterThan(0f));
}
```

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Implement fixed candidate scoring**

Use normalized nonlinear urgencies, availability, normalized distance, projected energy cost, eligibility/proximity, and inertia. Mark impossible candidates invalid. Do not add hidden personality weights.

- [ ] **Step 4: Implement commitment and diagnostics**

Keep total scores for every action plus the winner's named components, target, and tick. Permit critical urgency or target invalidation to break commitment.

- [ ] **Step 5: Integrate 2 Hz decisions after fresh perception**

- [ ] **Step 6: Run action fixtures, replay tests, and a thrashing soak test**

Expected: documented actions win for documented reasons; stable fixtures do not switch actions every decision.

- [ ] **Step 7: Commit**

```powershell
git add Assets/Scripts/Simulation/Behavior Assets/Scripts/Simulation/Core Tests/EditMode
git commit -m "feat: add explainable utility decisions"
```

## Task 8: Implement two-parent reproduction and inheritance

**Files:**
- Create: `Biology/ReproductionSystem.cs`.
- Modify: genome/store/config/events/world.
- Test: `BiologyTests.cs`.

**Interfaces:**
- Produces eligibility/willingness evaluation, stable-ID deterministic pairing, successful-birth commands, parent costs/cooldowns, and birth/reproduction events.

- [ ] **Step 1: Write failing inheritance tests**

Cover per-gene parent selection, parent-order independence, mutation occurrence/purpose separation, zero-mean sampling tolerance, clamping, child generation/parents, deterministic birth position, and phenotype recomputation.

- [ ] **Step 2: Write failing matching/lifecycle tests**

Cover maturity/needs/cooldown thresholds, mutual willingness, nearest candidate, stable-ID tie, one pair per creature, successful parent costs, no charge on rejected birth, cooldown, offspring counts, and births becoming active only at lifecycle boundary.

- [ ] **Step 3: Run Biology tests and verify failure**

- [ ] **Step 4: Implement deterministic candidate collection and pair resolution**

Collect eligible indexes into a reused buffer, sort by stable ID once at 1 Hz, and mark matched creatures so conflicts resolve deterministically.

- [ ] **Step 5: Implement crossover, mutation, birth, parent cost, and events**

Charge both parents only after a child slot is accepted. Emit versioned reproduction and birth events; overflow invalidates experiment mode.

- [ ] **Step 6: Run inheritance, lifecycle, replay, and 100-generation soak tests**

Expected: no duplicate pairs or broken ID mappings; genomes remain finite/in range; identical runs hash equally.

- [ ] **Step 7: Commit**

```powershell
git add Assets/Scripts/Simulation/Biology Assets/Scripts/Simulation/Core Tests/EditMode
git commit -m "feat: add two-parent inheritance"
```

## Task 9: Implement statistics, scenarios, experiments, and evidence output

**Files:**
- Create: `PopulationStatistics.cs`, `ExperimentRunner.cs`.
- Modify: config/events/world.
- Test: `ResourceExperimentTests.cs`.

**Interfaces:**
- Produces versioned baseline/drought/scarcity scenario definitions, paired founder creation, run manifest, periodic snapshots, final trait statistics, lifetime summaries, confidence interval/effect-size aggregation, and compact CSV/JSON output DTOs.

- [ ] **Step 1: Write failing paired-founder and manifest tests**

Assert control/treatment founders match exactly before environmental configuration, manifests include seed/config/schema/commit identifiers, scheduled commands reproduce, and final state/result hashes match replay.

- [ ] **Step 2: Write failing statistics tests**

Use fixed numeric fixtures for mean, variance, quantiles, paired differences, bootstrap confidence intervals with keyed resampling, Cohen's d, direction consistency, extinction, cap contact, rejected births, and invalid event/numeric state.

- [ ] **Step 3: Run focused tests and verify failure**

- [ ] **Step 4: Implement allocation-free periodic population aggregation**

Reuse working buffers for quantiles and snapshots. Keep hot current metrics separate from experiment-only lifetime/result aggregation.

- [ ] **Step 5: Implement scenario runner and compact serializers**

Run a fixed tick duration, drain events, record validity flags, and write only after simulation batches finish. Raw output paths remain ignored.

- [ ] **Step 6: Add deterministic short baseline/drought/scarcity integration fixtures**

These fixtures verify treatment wiring and reproduction loops, not the final evolution claim.

- [ ] **Step 7: Run all EditMode tests and repeated short paired runs**

Expected: manifests replay; treatment affects only resource configuration; founders remain paired; invalid conditions are explicit.

- [ ] **Step 8: Commit**

```powershell
git add Assets/Scripts/Simulation/Systems Assets/Scripts/Simulation/Core Tests/EditMode
git commit -m "feat: add evolutionary experiment harness"
```

## Task 10: Implement Unity bootstrap, scene, pooled visuals, and debug UI

**Files:**
- Create Unity-boundary, Editor, scene, and settings files listed in the file map.
- Test: `PresentationTests.cs`.

**Interfaces:**
- Produces `SimulationAuthoring.ToConfig()`, `SimulationBootstrap.World`, pause/speed controls, visual cap/pool, ID selection, and read-only debug panel.

- [ ] **Step 1: Write failing PlayMode boundary tests**

Cover one-world ownership, fixed-step accumulation, pause, equal-tick speed equivalence, backlog, visual cap, pool reuse, visual/state separation, interpolation, stable-ID selection after compaction, and displayed diagnostics.

- [ ] **Step 2: Run PlayMode tests and verify failure**

- [ ] **Step 3: Implement authoring and pacer**

Convert serialized fields into immutable pure config once. Use `Time.unscaledDeltaTime`, speed multiplier, fixed accumulator, and per-frame tick budget. Never pass variable deltas to `SimulationWorld`.

- [ ] **Step 4: Implement pooled primitive visuals**

Create/reuse primitive objects, map them by stable ID, interpolate state, scale by body size, tint by normalized health/needs, and cap independently of population.

- [ ] **Step 5: Implement programmatic UI Toolkit debug panel**

Cache controls/labels. Update text at a limited UI frequency. Show global counters/timings, experiment warnings, selected identity/parents/generation/needs/genome/phenotype/action/target, and winning score terms.

- [ ] **Step 6: Implement idempotent scene/settings setup**

Create `Prototype1.unity` with ground, camera, light, bootstrap, and UI document. Running setup twice must not duplicate objects/assets.

- [ ] **Step 7: Run EditMode, PlayMode, and batch compile**

Expected: all tests pass; zero visuals still permits headless simulation; visible count never exceeds cap.

- [ ] **Step 8: Commit**

```powershell
git add Assets Tests ProjectSettings
git commit -m "feat: add prototype presentation and inspection"
```

## Task 11: Add batch entry points and benchmark gate

**Files:**
- Create: `Debug/PrototypeBatchEntry.cs`.
- Modify: metrics/experiment files and repository docs.
- Create curated summary: `docs/benchmarks/P0_BASELINE.md` after measurements.

**Interfaces:**
- Produces Unity command-line methods for one scenario, paired experiment suite, and benchmark matrix.

- [ ] **Step 1: Add a failing batch smoke test**

Invoke the batch entry with a small deterministic scenario and assert exit success plus manifest/result hash files.

- [ ] **Step 2: Implement command-line parsing and explicit exit codes**

Return nonzero for invalid config, failed invariant, event overflow, unexpected population-cap contact, failed tests, or output failure. Include seed/scenario/count/ticks/output in arguments.

- [ ] **Step 3: Run 100/500/1,000-creature headless benchmarks**

Warm up before measurement. Record average/p95 step and system timings, creature-seconds/real-second, memory, allocation, query/evaluation counts, build type, hardware, seed, config hash, and commit.

- [ ] **Step 4: Profile only if the required gate fails**

Use recorded per-system timings to select the dominant loop. Improve algorithm/layout before considering Burst/Jobs. Preserve before/after reports from the identical run.

- [ ] **Step 5: Re-run the required benchmark**

Expected: 1,000 creatures simulate 600 seconds in at most 60 real seconds, average step at or below 5 ms, without cap contact or recurring hot-loop allocation.

- [ ] **Step 6: Run the 10,000-creature stretch measurement**

Record reduced perception/decision settings and result; failure does not block P0.

- [ ] **Step 7: Commit curated benchmark evidence**

```powershell
git add Assets/Scripts/Debug docs/benchmarks README.md
git commit -m "perf: record prototype baseline"
```

## Task 12: Run full evolution experiments and freeze P0 evidence

**Files:**
- Modify result/report documentation.
- Create: `docs/experiments/P0_EVOLUTION_RESULTS.md`.
- Update: `README.md`, `docs/PROTOTYPE_1.md`, and `docs/ROADMAP.md` with achieved status and limitations.

**Interfaces:**
- Consumes the paired scenario runner and versioned manifests.
- Produces compact scientific evidence and frozen regression scenario identifiers for P1.

- [ ] **Step 1: Run at least 20 paired baseline/drought seeds**

Use identical founders and fixed duration. Preserve manifests/raw output outside Git; aggregate water-efficiency and correlated fitness outcomes.

- [ ] **Step 2: Run at least 20 paired baseline/food-scarcity seeds**

Aggregate food efficiency, metabolism, body size, speed, vision, and fitness outcomes.

- [ ] **Step 3: Audit invalid runs before interpreting effects**

Separate extinction, cap contact, event overflow, numerical failure, or rejected-birth artifacts. Do not remove valid counterexamples.

- [ ] **Step 4: Evaluate the predeclared evolution criterion**

Require the 95% paired bootstrap interval to exclude zero, `|d| >= 0.5`, direction consistency in at least 75% of pairs, and a survival/reproduction mechanism. Report failure honestly if no trait qualifies.

- [ ] **Step 5: Fix only demonstrated defects**

If equations violate their documented trade-off, conservation, or numerical behavior, add a failing test, fix the defect, and rerun both control and treatment from scratch. Do not tune merely to force the expected graph.

- [ ] **Step 6: Run final Unity test/compile/benchmark suite**

Expected: all deterministic, EditMode, PlayMode, experiment, and benchmark gates pass.

- [ ] **Step 7: Freeze evidence and commit**

```powershell
git add README.md docs
git commit -m "docs: record prototype evolution evidence"
```

## P0 completion criteria

P0 is complete only when Tasks 1-12 pass, the repository is clean, Unity batch compilation succeeds, the required benchmark succeeds, and at least one treatment satisfies the predeclared evolution criterion. A well-measured failed hypothesis is a valid research result and must be preserved, but it sends P0 back to documented biological-design review rather than advancing the roadmap under a false success claim.
