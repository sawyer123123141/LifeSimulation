# LifeSimulation Product Architecture: P0-P7

## Purpose

This document plans the full path from the first evolution proof to a scalable planet-scale artificial-life simulation. It defines the scientific question, architecture change, evidence, performance gate, and deliberate exclusions for every roadmap stage.

The stages are cumulative but remain independently testable:

| Roadmap stage | Working prototype | Central question |
|---|---|---|
| P0 | Prototype 1: Evolution proof | Can environmental pressure shift heritable traits? |
| P1 | Prototype 2: Predator-prey | Can ecological roles and population cycles emerge from traits? |
| P2 | Prototype 3: Costly cognition | Can memory and learning improve fitness enough to justify their biological cost? |
| P3 | Prototype 4: Rich physiology | Can additional biological trade-offs produce stable niches? |
| P4 | Prototype 5: Coevolving ecosystem | Can plants, consumers, and environmental change coevolve? |
| P5 | Prototype 6: Species and history | Can continuous evolution be summarized without turning labels into simulation truth? |
| P6 | Prototype 7: World scale | Can regions and simulation LOD expand population scale without invalidating ecology? |
| P7 | Prototype 8: Planet experience | Can users understand and explore the living world's present and history at multiple scales? |

P0 has its own detailed design in `2026-08-12-prototype-1-design.md`. Later stages are planned deeply enough to preserve extension seams and define completion, but their numerical models should be specified immediately before implementation using evidence from prior stages.

## Permanent architectural principles

These constraints apply to every stage:

1. Simulation truth is independent from Unity rendering truth.
2. Ecological roles and species labels are derived from traits and outcomes, not authoritative booleans.
3. Hot entity state uses explicit dense stores and blittable data, not deep object graphs or generic property dictionaries.
4. Structural changes occur at deterministic boundaries through reused command buffers.
5. Randomness is keyed by stable identity and purpose rather than mutable global draw order.
6. Every new trait must have a measurable benefit, cost, and experiment that can falsify its usefulness.
7. Every optimization requires a recorded before/after benchmark.
8. Large-scale approximations must conserve declared quantities and expose their uncertainty.
9. Analysis layers may classify, aggregate, and explain simulation data but may not feed hidden labels back into biology.
10. Each prototype has a scientific exit criterion in addition to compiling and performing well.

## Long-lived architecture

The final system grows into five cooperating layers:

```text
Experiment and configuration layer
        |
World orchestration and simulation-LOD layer          (P6+)
        |
High-fidelity regional simulation kernels             (P0-P4)
        |
Event, statistics, lineage, and analysis layer        (P0-P5)
        |
Unity presentation, inspection, and history views     (P0-P7)
```

P0's `SimulationWorld` becomes the high-fidelity regional kernel at P6 rather than being discarded. A world-scale coordinator owns multiple regions and chooses their fidelity. This is the main strategy for avoiding a later rewrite.

### Explicit stores, not a universal entity framework

Each major biological domain receives a focused dense store:

- `CreatureStore` in P0
- bounded resource store in P0
- optional bounded memory store in P2
- environment-field grids in P3/P4
- `PlantStore` or plant-cohort store in P4
- far-population cohort stores in P6

Stores may share stable-ID, compaction, and buffer utilities, but creatures, plants, and cohorts do not inherit from a generic simulated-object base class. Their update patterns and data are different enough that forced unification would hurt clarity and performance.

### Versioned schemas

Simulation configuration, genome layout, phenotype mapping, scenario definitions, and event formats carry explicit schema versions. During active prototype development, old worlds do not require indefinite save compatibility, but experiment manifests must identify the exact schema and code commit that produced them.

Genes remain explicit numeric fields. New prototype genes extend a versioned genome struct and phenotype calculation; they are not stored in string-keyed maps. Analysis exports can convert them to named columns.

### Read models and event output

Simulation systems expose two outward paths:

- read-only current-state views for presentation and inspection
- compact events for births, deaths, reproduction, environmental changes, migrations, and later major historical events

The pure simulation writes events to a fixed-capacity buffer. A host-provided sink drains the buffer outside hot loops to files, aggregates, or UI histories. Event overflow is an explicit invalid-run condition for experiments, never silent loss.

This event seam begins in P0 even though rich historical views arrive in P5.

## P0 — Prototype 1: Evolution proof

### Scientific question

Can drought or food scarcity reliably alter inherited water efficiency, food efficiency, body size, movement speed, metabolic pace, or vision through differential survival and reproduction?

### Product slice

- deterministic fixed-step creature simulation
- six-gene genome and cached phenotype
- needs, health, age, birth, death, and two-parent inheritance
- finite renewable food and water
- bounded-grid perception, movement, utility choices, and reproduction
- paired control/treatment experiment runner
- minimal 3D visualization and inspectable decision explanations
- benchmark and deterministic replay infrastructure

### Architecture delivered for later stages

- pure-C# regional simulation kernel
- dense stores, stable IDs, structural buffers, keyed randomness
- spatial query and action-scoring patterns
- versioned manifests and compact event output
- visual pooling and state-inspection boundary
- experiment, aggregation, and benchmark harnesses

### Exit gate

- at least one treatment-relevant trait satisfies the documented paired-seed effect criterion
- mechanistic survival/reproduction evidence supports the shift
- deterministic replay and conservation checks pass
- 1,000 creatures achieve 10x real time under the defined benchmark
- results do not depend on safety population-cap contact

### Deferred

Predation, fear, aggression, learning, evolving plants, species labels, multiple regions, and planet presentation.

## P1 — Prototype 2: Predator-prey emergence

### Scientific question

Can hunting, escape, diet, and energy economics generate predator/prey niches and population cycles without hardcoded predator or apex-species flags?

### New simulation data

- edible body biomass and carcass state
- attack capability, defense, maneuverability, and dietary phenotype
- threat and prey perception summaries
- short attack/recovery state and injury data
- active fear/aggression genes, defined as action-score tendencies with biological costs
- consumption preferences or digestion efficiency along a plant-to-meat continuum

No `IsPredator`, `IsPrey`, or `IsApexPredator` field is allowed. A predator is a creature that obtains a meaningful share of its energy through hunting outcomes.

### System extensions

- spatial queries add nearest viable prey, threats, and carcasses
- utility actions add stalk/chase, attack, flee, feed-on-carcass, and defensive responses
- movement supports pursuit and evasion steering without global pathfinding
- resource allocation expands to contested carcass biomass
- health incorporates wounds and recovery costs
- population analysis derives trophic behavior from energy-source history

Combat resolves in deterministic interaction batches. Attack order cannot depend on dense array position. Simultaneous claims are resolved through stable IDs and explicit conflict rules.

### Trade-offs

- attack power costs body mass, energy, or attack recovery
- speed and maneuverability cost movement energy
- armor/defense costs movement and basal energy
- strong fear improves survival but forfeits feeding/mating opportunities
- aggression improves contest participation but increases injury and wasted-energy risk
- meat digestion reduces plant efficiency or imposes digestive specialization costs

### Required experiments

- prey-only control remains viable under the prior P0 ecology
- introduced hunting-capable variation produces or fails to produce predation without labels
- resource-rich and resource-poor treatments compare specialization pressure
- predator removal and reintroduction test causal population response
- paired seeds measure lagged predator/prey population cycles and trait shifts

### Exit gate

- predation emerges from phenotype and utility outcomes
- both hunting and escape traits show measurable costs as well as benefits
- repeatable coupled population dynamics occur in a meaningful share of seeds
- no O(N²) perception or combat pass is introduced
- P0 experiments remain reproducible under the versioned P0 configuration

### Deferred

Long-term memory, learned hunting, pack behavior, disease, and species classification.

## P2 — Prototype 3: Costly cognition

### Scientific question

Does bounded memory or learning improve fitness in changing environments enough to offset brain energy, rest, and decision costs?

### New simulation data

- fixed-capacity memory records for resource, threat, and encounter observations
- confidence, age, and decay for each memory
- compact learned-value estimates for supported action/context categories
- cognition capacity, learning rate, memory retention, and exploration genes
- derived brain energy/rest cost

Memory is optional dense sidecar data indexed by creature slot. Fixed per-creature capacity avoids per-creature collections and unbounded growth. Increasing capacity is a heritable benefit with a proportional memory and metabolism cost.

### System extensions

- perception writes observations into deterministic bounded memory replacement
- decision scoring distinguishes current perception from remembered information
- confidence decays with time and failed searches
- simple reinforcement updates action/context values from need changes and survival outcomes
- exploration balances learned value against uncertainty

Learning affects behavior during a lifetime but does not directly modify the genome. Genes control capacity and learning behavior; offspring inherit predispositions, not acquired memories.

### Required experiments

- stationary-resource control where memory should offer little benefit
- periodically relocated resources where memory can become stale
- recurring hazard/resource patterns where learning can help
- ablation runs with memory disabled but otherwise identical founders
- energy-cost sweeps to find when cognition ceases to pay for itself

### Exit gate

- cognition improves reproductive output in at least one environment and loses or becomes neutral in another
- stale or incorrect memory can cause measurable mistakes
- cognitive benefit disappears when its biological cost is removed from the causal analysis
- memory and learning remain bounded in time and storage
- P1 behavior remains explainable through current and remembered score terms

### Deferred

Neural networks, language, culture, pack coordination, and inherited learned weights.

## P3 — Prototype 4: Rich physiology and niche formation

### Scientific question

Can additional physiological trade-offs create multiple persistent survival strategies without manually assigning species roles?

### Candidate traits

- fertility investment and gestation/reproduction delay
- lifespan tendency and maintenance cost
- digestion specialization
- temperature tolerance and thermoregulation cost
- sexual-selection signals only if two-parent mating needs richer choice
- optional immunity/disease only after a focused design proves it adds useful pressure

Traits enter one focused experiment at a time. The prototype does not add every plausible biology variable simultaneously.

### Architecture extensions

- phenotype is split into focused metabolism, locomotion, sensory, reproduction, and tolerance groups if profiling supports it
- environment adds deterministic coarse scalar fields such as temperature
- utility and needs systems consume local environment samples
- reproduction can add gestation/investment state without changing two-parent ancestry
- scenario manifests declare enabled physiology modules and genome schema

The project continues to use explicit structs. Optional prototype modules are compile/config-time world capabilities, not per-creature polymorphic objects.

### Required experiments

- stable versus fluctuating temperature gradients
- abundant versus scarce food with digestion specialists and generalists
- high versus low mortality pressure for fertility/lifespan trade-offs
- niche-removal and environment-change tests to observe adaptation or collapse

### Exit gate

- at least two distinct trait strategies persist because they exploit different conditions, not because of hardcoded category protection
- added traits retain measurable costs in environments where their benefit is absent
- enabled modules do not invalidate deterministic replay
- simulation throughput remains measured at P0/P1 scales

### Deferred

Evolving plants, explicit species labels, broad climate systems, and large-world partitioning.

## P4 — Prototype 5: Coevolving ecosystem

### Scientific question

Can evolving producers and consumers create reciprocal selection, changing food webs, and adaptation to environmental shifts?

### Plant representation

Plants receive their own explicit dense store or spatial cohorts, selected after measuring required plant counts. They are not remodeled as creatures.

Initial plant traits may include:

- growth rate
- mature biomass and seed investment
- water demand
- nutritional value
- physical or chemical defense
- dispersal distance
- temperature/moisture tolerance

Individual plants are appropriate near observed regions; cohorts or seed-bank distributions may represent dense populations. The representation and any aggregation threshold must conserve biomass and heritable distributions.

### System extensions

- food patches become a compatibility facade over plant biomass during migration
- plant growth consumes local water/light/nutrient budgets
- seed production, mutation, dispersal, establishment, and death create plant generations
- creature digestion and plant defenses create reciprocal fitness effects
- environment fields add slow moisture, fertility, and climate variables
- food-web analysis derives energy transfer between phenotype clusters

### Required experiments

- defended versus undefended plant populations under herbivory
- consumer digestion/efficiency response to plant defenses
- seed-dispersal response to spatial disturbance
- rainfall or temperature shift with control climates
- producer removal and recovery experiments

### Exit gate

- at least one reciprocal plant/consumer trait response is repeatable across paired seeds
- total biomass and resource accounting remain within declared conservation/error bounds
- plant density does not force per-plant GameObjects or all-to-all queries
- climate changes affect survival through explicit fields and physiology, not scripted population edits

### Deferred

Species naming, complete evolutionary trees, planet geometry, and very-far statistical simulation.

## P5 — Prototype 6: Species and ecological history

### Scientific question

Can the system summarize continuous ancestry and ecological change into useful, honest species and history views without making those derived labels authoritative?

### Analysis architecture

This stage expands the external event and analysis layer rather than the hot simulation kernel:

- append-only compact birth, death, reproduction, migration, predation, and environment events
- ancestry graph indexed outside active creature storage
- periodic genome/phenotype samples
- genetic-distance calculations on sampled or representative populations
- clustering with explicit thresholds, confidence, merge, split, and extinction events
- ecological event detection based on population and energy-flow changes

Species IDs are analysis output. Simulation systems never read them to decide mating, behavior, diet, or survival. If reproductive compatibility is later modeled, it derives directly from genes/phenotypes rather than cluster names.

### Storage policy

- active simulation retains only hot current state
- compact event batches are drained asynchronously by the host
- long histories use chunked binary data plus indexed summaries
- Git stores schemas and small fixtures, not generated world histories
- UI reads prepared snapshots rather than scanning raw history every frame

### Required validation

- synthetic lineage fixtures with known splits and merges
- threshold-sensitivity analysis for clustering
- comparison of full-population and sampled clustering
- replay from event fixtures for timeline views
- checks that removing the analysis layer leaves simulation hashes unchanged

### Exit gate

- clusters describe meaningful genetic separation with visible uncertainty
- split, merge, and extinction histories follow ancestry/event data
- analysis has no causal feedback into simulation
- storage growth and query latency are bounded and benchmarked

### Deferred

World partitioning, far-population approximation, and planet-scale display.

## P6 — Prototype 7: World scale and simulation LOD

### Scientific question

Can the world contain far more life than can be fully simulated or rendered while preserving plausible population, trait, biomass, and migration dynamics?

### Regional orchestration

A new `WorldSimulation` coordinator owns:

- global tick/calendar and seed namespace
- stable global ID allocation
- region topology and environment summaries
- multiple high-fidelity regional kernels derived from P0 `SimulationWorld`
- medium/far individual or cohort representations
- migrations and fidelity-transition command buffers
- global statistics and history routing

Regions use local 2D coordinates plus a region identifier. The existing ground-plane kernel becomes a local tangent-region simulation rather than being rewritten for spherical coordinates.

### Fidelity levels

```text
Near:      full individual state, perception, decisions, and optional visuals
Medium:    individuals with reduced perception/decision frequency
Far:       simplified individuals or cohorts with aggregate resource interactions
Very far:  population cohorts carrying trait distributions and biomass flows
```

Only one representation is authoritative for an organism/population at a time. Promotion and demotion occur at deterministic boundaries.

### Conservation contracts

LOD transitions declare and test conservation of:

- population count
- total living biomass and stored energy within tolerance
- resource biomass/water within tolerance
- gene means, variances, and bounded distributions
- age/generation distributions
- migration counts

Aggregating individuals into cohorts retains distribution summaries rather than only averages. Expanding a cohort uses deterministic stratified sampling keyed by cohort identity and transition ordinal.

### World partition

- regions communicate through explicit migration and boundary-resource exchanges
- hot regional kernels never scan global populations
- update budgets choose frequency by relevance and measured ecological rate
- rendering requests snapshots independently of simulation fidelity
- background/headless runs can assign budgets without camera dependence

### Required experiments

- identical small worlds with and without region partitioning
- repeated promote/demote cycles measuring drift
- migration across region boundaries
- ecological disturbance occurring while a region is far-simulated
- scale tests increasing regions, population, and rendered subset separately

### Exit gate

- transitions remain within declared conservation/error bounds
- regional ecology does not change merely because the camera moves
- far regions evolve measurably without full individual updates
- total world population substantially exceeds high-fidelity and rendered counts
- throughput scales with active fidelity budget rather than total individual count alone

### Deferred

Final planet art, production camera experience, and polished historical replay.

## P7 — Prototype 8: Planet experience

### Product question

Can a user move from organism-level observation to planet-level ecology and historical replay while clearly understanding what is simulated, approximated, and derived?

### Planet mapping

- regions map onto a small spherical or planet-like topology
- high-fidelity simulation continues in local tangent coordinates
- planet rendering converts region/local positions to display coordinates
- climate regions and biome summaries derive from environment fields
- camera position affects rendering and optional interactive fidelity requests, not ecological rules

### Experience layers

- organism view: follow, inspect, and explain one creature
- local ecosystem view: resources, threats, populations, and energy flow
- regional view: climate, trait distributions, migrations, and abundance
- planet view: biome summaries, ecological pressure, and population trends
- history view: timeline, lineage/species analysis, extinctions, climate shifts, and uncertainty

Visual aggregation must label whether a value comes from full individuals, cohorts, sampling, or historical analysis. The interface must not imply precision the simulation does not contain.

### Required validation

- zoom and region transitions do not alter deterministic simulation outcomes under the same fidelity policy
- selected entities remain stable across visual pooling and coordinate transforms
- timeline reconstruction matches stored event summaries
- planet aggregates reconcile with region totals
- user-facing performance remains acceptable with simulation and history overlays active

### Exit gate

- seamless organism-to-planet navigation works with bounded rendering cost
- present and historical ecology are inspectable and internally reconciled
- fidelity and uncertainty are communicated honestly
- the planet layer remains presentation/orchestration rather than a replacement simulation

## Cross-stage implementation order

Each prototype follows the same gated cycle:

1. Write the focused numerical/behavioral design using evidence from the previous stage.
2. Add deterministic fixtures and failure tests before production behavior.
3. Implement the smallest end-to-end causal loop.
4. Add explanation and measurement before broad balancing.
5. Run paired controls and treatments.
6. Benchmark and optimize only measured bottlenecks.
7. Freeze a versioned scenario that preserves the prototype's result as a regression suite.

Later stages may not silently change earlier benchmark/scientific fixtures. Schema migrations either preserve the fixture or retain a versioned compatibility configuration.

## Program-level quality gates

The project is ready to progress from one prototype to the next only when:

- its scientific claim has repeatable evidence, including negative and invalid runs
- deterministic unit/integration fixtures pass
- prior prototype fixtures still pass or have an explicit versioned migration
- active hot loops avoid recurring managed allocation
- performance is recorded at representative counts
- new traits demonstrate both costs and benefits
- debug views explain new decisions and derived classifications
- generated data and Unity caches remain out of Git
- the repository documentation states exactly what was learned and what remains unknown

## Scope and delivery policy

The complete P0-P7 architecture and roadmap are planned now. Implementation proceeds one prototype at a time, beginning with P0. Later numerical constants, UI details, and storage technologies are finalized only when prior prototype measurements provide the missing evidence.

This sequencing preserves the long-term architecture without spending early implementation effort on systems whose requirements have not yet been observed.
