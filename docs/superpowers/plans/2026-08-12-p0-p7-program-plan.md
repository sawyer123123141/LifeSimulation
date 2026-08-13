# P0-P7 Product Program Plan

**Goal:** Deliver the LifeSimulation roadmap as eight evidence-gated prototypes, progressing from a deterministic evolution proof to an inspectable planet-scale artificial-life simulation.

**Architecture:** P0 builds a pure-C# high-fidelity regional simulation kernel behind a Unity presentation boundary. Later prototypes extend explicit dense stores and analysis capabilities; P6 wraps proven regional kernels in world orchestration and simulation LOD rather than replacing them.

**Tech Stack:** Unity 6 (6000.2.14f1), C#, Unity Test Framework, runtime UI Toolkit, versioned experiment manifests, CSV/binary result summaries, optional Burst/Jobs only after profiling.

## Global constraints

- Simulation truth must remain independent of GameObjects and rendering.
- Hot simulation state must remain explicit, compact, blittable, and batch-friendly.
- Randomness must be keyed by stable identity and purpose.
- Structural mutation must occur at deterministic boundaries.
- Ecological roles and species labels must be derived, never authoritative biology flags.
- Every heritable trait requires an explicit cost, benefit, and falsifiable experiment.
- Each prototype must preserve or version the regression evidence from earlier prototypes.
- No performance claim is accepted without comparable benchmark evidence.
- Generated worlds, raw experiment data, Unity caches, builds, and IDE files stay out of Git.
- Implementation proceeds one prototype at a time; later numerical specifications are frozen only after earlier evidence exists.

---

## Program dependency chain

```text
P0 Evolution proof
  -> P1 Predator-prey
  -> P2 Costly cognition
  -> P3 Rich physiology
  -> P4 Coevolving ecosystem
  -> P5 Species and history
  -> P6 World scale and simulation LOD
  -> P7 Planet experience
```

Each arrow is an evidence gate, not merely a calendar milestone. Work may prototype presentation ideas early, but production integration cannot bypass the preceding simulation gate.

## Shared delivery cycle

Every prototype follows the same eight phases:

1. **Baseline lock:** preserve deterministic scenarios, state hashes where applicable, experiment summaries, and benchmarks from the prior prototype.
2. **Focused design:** freeze new data, equations, scheduling, diagnostics, and explicit exclusions.
3. **Test fixtures:** add deterministic unit fixtures and at least one causal control/treatment fixture before broad behavior implementation.
4. **Minimal causal loop:** implement the smallest loop capable of testing the prototype's scientific question.
5. **Explainability:** expose why decisions, fitness, classifications, or approximations produced their outputs.
6. **Paired experiments:** run controls and treatments across recorded paired seeds; preserve negative and invalid outcomes.
7. **Performance gate:** profile representative populations and optimize only measured bottlenecks.
8. **Evidence freeze:** commit versioned scenarios, compact summaries, findings, limitations, and migration notes.

## P0 — Evolution proof delivery

Detailed executable plan: `2026-08-12-p0-evolution-proof-implementation.md`.

### Work packages

- Unity/assembly bootstrap and repository hygiene
- deterministic simulation core and dense stores
- six-gene phenotype and needs mathematics
- finite renewable resources and fair contention
- bounded-grid perception and ground-plane movement
- utility decisions and fixed-size diagnostics
- two-parent crossover, mutation, birth, and death
- paired baseline/drought/food-scarcity experiment runner
- pooled 3D visuals, selection, controls, and debug inspection
- 100/500/1,000-creature benchmark suite

### Exit evidence

- deterministic state replay for equal seed/config/commands/ticks
- treatment-relevant trait shift meeting the paired-seed statistical criterion
- mechanistic link between the trait and reproductive output/survival
- 1,000 creatures simulated at 10x real time in the defined headless benchmark
- no recurring managed allocation in obvious fixed-tick hot loops

### Program artifacts

- versioned P0 genome, config, scenario, event, and result schemas
- reusable regional kernel and event-drain seam
- P0 regression scenarios frozen for every later prototype

## P1 — Predator-prey delivery

### Baseline lock

- Run frozen P0 baseline, drought, and scarcity fixtures on the P1 branch before adding predation.
- Store the resulting version/summary comparison in the P1 evidence report.
- Fail integration if P0 reproduction, resource conservation, or deterministic fixtures regress without an approved schema migration.

### Data phase

- Extend the versioned genome with attack, defense, maneuverability, fear, aggression, and diet-specialization genes.
- Add cached predation phenotype values, wound/recovery state, edible biomass, and carcass resource state.
- Extend perception summaries with prey, threat, and carcass observations.
- Extend energy-source telemetry so trophic behavior can be derived from consumed biomass.

### Behavior phase

- Add deterministic chase, evade, attack, defend, and carcass-consumption interactions.
- Resolve simultaneous attacks and carcass claims in request buffers independent of dense index.
- Add utility terms for expected energy reward, injury risk, escape probability, opportunity cost, fear, and aggression.
- Keep roles emergent: no predator/prey/apex booleans may enter simulation state.

### Experiment phase

- Preserve a prey-only control under P0 resource rules.
- Seed broad hunting/diet variation without labeling roles.
- Compare resource abundance levels and predator-removal/reintroduction treatments.
- Measure energy-source share, attack success, mortality causes, reproductive output, trait distributions, and lagged population correlation.

### Exit gate

- Hunting supplies a meaningful energy share for an emergent subset of phenotypes.
- Escape, attack, defense, fear, and aggression each demonstrate both a benefit and cost.
- Repeatable coupled population dynamics appear across recorded seeds.
- Spatial and combat candidate counts remain local rather than O(N²).
- P0 frozen evidence remains valid under its versioned configuration.

## P2 — Costly cognition delivery

### Baseline lock

- Freeze representative P1 prey-only and predator/prey scenarios with decision explanations and population-cycle summaries.
- Record memory-disabled hashes/outcomes as the P2 control baseline.

### Data phase

- Add a fixed-capacity aligned memory sidecar for resource, threat, and encounter observations.
- Add confidence, observation age, decay, and deterministic replacement metadata.
- Extend the genome/phenotype with memory capacity, retention, learning rate, and exploration tendency.
- Add explicit brain energy, rest, and decision-time costs.

### Learning phase

- Write current perception into memory without allocation.
- Permit utility scores to use remembered targets with lower confidence than current perception.
- Add bounded context/action value updates driven by need changes and survival outcomes.
- Ensure acquired memory and values die with the creature; offspring inherit only cognition genes.

### Experiment phase

- Compare memory-enabled and disabled paired founders in stationary-resource worlds.
- Relocate resources periodically to test stale memory.
- Add recurring spatial patterns where bounded learning can help.
- Sweep brain costs to locate when cognition becomes maladaptive.

### Exit gate

- Cognition improves reproductive output in at least one patterned environment.
- Cognition loses or becomes neutral in at least one simple or rapidly changing environment.
- Stale memories cause visible, explainable mistakes.
- Memory/storage cost remains fixed per configured capacity and hot loops remain allocation-free.

## P3 — Rich physiology delivery

### Baseline lock

- Freeze P2 scenarios showing useful, harmful, and neutral cognition.
- Establish a genome-schema migration fixture with cognition defaults for older scenarios.

### Trait slices

Implement and validate one slice at a time; do not merge the next slice until its control/treatment evidence and performance are recorded.

1. **Fertility/lifespan:** fertility investment, maturation delay, gestation/cooldown, lifespan tendency, and maintenance cost.
2. **Digestion:** plant/meat specialization, processing rate, yield, toxin/defense sensitivity, and generalist cost.
3. **Temperature:** deterministic coarse temperature field, tolerance range, thermoregulation cost, and heat/cold health pressure.
4. **Optional mate signaling:** add only if two-parent choice remains behaviorally impoverished after the prior slices.
5. **Optional disease/immunity:** requires a separate focused spec; it is not automatically part of P3.

### Experiment phase

- Compare stable and fluctuating temperature gradients.
- Compare specialist and generalist digestion across resource mixtures.
- Compare fertility/lifespan strategies under low and high external mortality.
- Remove a niche or shift an environmental field to observe adaptation, migration, or collapse.

### Exit gate

- At least two trait strategies persist by exploiting different measured conditions.
- Each added trait loses value or becomes costly when its relevant pressure is absent.
- No module requires per-creature polymorphic objects or breaks deterministic scheduling.
- P0-P2 frozen scenarios still run through their versioned configurations.

## P4 — Coevolving ecosystem delivery

### Baseline lock

- Select representative P3 consumer phenotypes and climate/digestion scenarios.
- Record food-patch energy/biomass accounting before migrating food to evolving producers.

### Producer kernel phase

- Add a dedicated dense plant or plant-cohort store after a measured individual-count prototype determines the appropriate representation.
- Implement growth, water/nutrient use, mature biomass, seed investment, dispersal, mutation, establishment, and death.
- Add plant genome/phenotype values for growth, water demand, nutrition, defense, dispersal, and environmental tolerance.
- Preserve the P0 food-resource API through a temporary compatibility facade while consumer systems migrate.

### Coevolution phase

- Connect plant defense and nutritional value to consumer digestion and feeding outcomes.
- Track energy flow from environment to plants to consumers/predators.
- Add slow deterministic moisture, fertility, and climate fields.
- Ensure individual/cohort transitions conserve biomass and trait distributions within declared bounds.

### Experiment phase

- Run defended/undefended plant populations under controlled herbivory.
- Compare consumer digestive response and plant defense response across paired seeds.
- Disturb habitat to test dispersal.
- Shift rainfall/temperature and compare control climates.
- Remove and restore producers to test recovery dynamics.

### Exit gate

- At least one reciprocal plant/consumer trait response is repeatable.
- Biomass and resource accounting stay within declared conservation tolerance.
- Producer density remains spatially partitioned and independent of GameObject count.
- Environmental change acts through explicit fields, not scripted population edits.

## P5 — Species and history delivery

### Baseline lock

- Freeze a P4 run with meaningful ancestry, coevolution, predation, and environmental change.
- Validate that the compact event seam captures every event required to reconstruct intended summaries.

### Storage phase

- Version compact birth, death, reproduction, predation, migration, and environment event records.
- Drain event batches outside hot loops into chunked binary history plus indexed summaries.
- Store periodic representative genome/phenotype/population snapshots.
- Keep raw generated histories outside Git; commit schemas and small fixtures only.

### Analysis phase

- Build ancestry indexes from parent events.
- Implement sampled genetic-distance calculations.
- Add clustering with thresholds, confidence, split, merge, appearance, and extinction summaries.
- Detect major ecological events from population, climate, biomass, and energy-flow changes.
- Ensure analysis outputs never feed species labels into mating, behavior, or survival.

### Validation phase

- Test synthetic ancestry graphs with known split/merge patterns.
- Measure clustering sensitivity to threshold and sampling.
- Compare sampled versus full clustering on manageable fixtures.
- Reconstruct timeline summaries from event fixtures.
- Verify enabling/disabling analysis leaves simulation hashes unchanged.

### Exit gate

- Species clusters provide useful separation with visible uncertainty.
- Ancestry and ecological timelines reconcile with source events.
- Storage growth, indexing time, and query latency are bounded and measured.
- The analysis layer remains causally read-only.

## P6 — World scale and simulation LOD delivery

### Baseline lock

- Freeze a compact single-region P5 world and history as the regional reference.
- Define conservation tolerances for population, biomass, resources, genes, ages, and migrations.

### World orchestration phase

- Add a world-level clock, seed namespace, global ID allocator, region topology, and event router.
- Treat the proven P0-P4 `SimulationWorld` as a high-fidelity regional kernel.
- Add deterministic region-boundary migration and environment exchange.
- Keep local 2D coordinates plus region identity; do not rewrite the kernel around sphere coordinates.

### Fidelity phase

- Add medium-frequency individual updates.
- Add far simplified individuals or cohorts carrying counts and trait distributions.
- Add very-far population cohorts and aggregate biomass/resource flows.
- Implement deterministic promotion/demotion at structural boundaries.
- Use stratified reconstruction rather than replacing distributions with averages.

### Validation phase

- Compare partitioned and unpartitioned versions of the same small world.
- Repeat promote/demote cycles and measure conservation drift.
- Test migration and disturbances while regions are far-simulated.
- Verify camera movement alone does not change ecology under a fixed fidelity policy.
- Scale regions, simulated population, high-fidelity population, and rendered population independently.

### Exit gate

- Transition drift remains within documented tolerances.
- Far regions continue ecological and evolutionary change.
- Total population substantially exceeds high-fidelity and rendered counts.
- Throughput follows active fidelity budget rather than total individual count.

## P7 — Planet experience delivery

### Baseline lock

- Freeze a multi-region P6 world with fidelity transitions, migration, history, and global reconciliation fixtures.
- Separate fidelity policy from camera policy before building zoom interactions.

### Planet mapping phase

- Map regions onto a small spherical topology.
- Convert region/local simulation positions into visual planet coordinates.
- Keep high-fidelity kernels in local tangent coordinates.
- Derive biome and climate-region presentation from environment fields.

### Experience phase

- Add organism follow/inspection.
- Add local ecology overlays for needs, threats, resources, and energy flow.
- Add regional climate, abundance, trait, and migration summaries.
- Add planet-level biome/ecology summaries.
- Add historical replay for lineage/species analysis, extinctions, migrations, and environmental shifts.
- Label whether displayed values are individual, sampled, cohort-derived, or historical estimates.

### Validation phase

- Verify zoom and coordinate transforms do not change simulation results under a fixed fidelity policy.
- Verify stable selection through pooling, region transitions, and coordinate conversion.
- Reconcile planet aggregates with regional totals.
- Reconstruct displayed historical summaries from stored events.
- Benchmark simulation, rendering, overlays, and history queries independently.

### Exit gate

- Organism-to-planet navigation is usable with bounded rendering cost.
- Present and historical ecological values reconcile across levels.
- Simulation fidelity and uncertainty are communicated honestly.
- The planet layer remains presentation and orchestration, not an alternative biology simulation.

## Repository and Git strategy

- Keep one detailed architecture spec per major prototype when that prototype enters implementation.
- Keep this program plan and one executable implementation plan for the active prototype.
- Group small related C# types by responsibility; split files when a domain has an independent test/review boundary.
- Do not commit empty aspirational directories.
- Commit generated Unity `.meta` files associated with tracked assets.
- Use text serialization for scenes and assets.
- Introduce Git LFS only when real binary art/audio assets arrive; do not add it for primitive Prototype 1 content.
- Add CI only after local batch-mode tests are stable and Unity licensing/secrets are deliberately configured.
- Commit small benchmark summaries and scenario fixtures; ignore raw result directories.
- Use milestone commits that leave the project compiling and tests passing.

## Program completion

LifeSimulation reaches the planned product architecture when P0-P7 exit gates pass, earlier frozen evidence remains reproducible or explicitly version-migrated, and the Unity planet experience remains a read/interaction layer over measured simulation truth.
