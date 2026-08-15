# World Generation Design

**Status:** design approved. A0 and A1 are specified for implementation here. A2, A3, and B have their load-bearing architectural decisions recorded but need their own specs before implementation.

**Scope:** how a world is generated — global structure, continuous fields, and derived networks — plus the recipe mechanism that lets different kinds of worlds exist without changing code. It does not specify terrain meshes, region partitioning, or camera work beyond recording decisions those cycles must honour.

## Program decomposition

Each row is its own spec, plan, and implementation cycle.

| Sub-project | Contents | Depends on |
|---|---|---|
| **A0** (specified here) | global structure: plates, boundaries, drift | — |
| **A1** (specified here) | continuous fields: elevation, temperature, moisture, fertility, cave density | A0 |
| **A2** | derived networks: rivers, lakes, coastlines, shorelines | A1 |
| **A3** | layered world model and position representation | — (kernel change, independently schedulable) |
| B | biome classification and creature adaptation | A1, A2 |
| C | sphere topology, region partitioning | A1 |
| D | terrain mesh, visual style, chunk streaming, geometry level of detail | A1, A2, C |
| E | organism-to-planet camera zoom | D |
| *(later)* | erosion pass | A2 |

**A3 is not world-generation work.** It changes creature position representation, the spatial grid, perception, and the state hash. It is recorded here because caves motivated it, but it schedules independently and must not be bundled into a generation implementation plan.

## Core principle: structure comes from process, not from better noise

Noise cannot produce continents. Every feature in a noise field is independent of every other, so there are no causes and nothing explains anything else — the result reads as splatter regardless of how it is tuned.

Real terrain is legible because processes produced it. Mountain ranges lie along plate boundaries. Island arcs curve because subduction curves. Rivers branch because water accumulates downhill. Biomes band by latitude because insolation does.

World generation is therefore specified as an **ordered pipeline of processes**, each consuming the output of earlier ones:

```text
plates and drift          ->  continental structure, boundary types
boundary contributions    ->  mountain ranges, trenches, rifts, island arcs
fractal detail            ->  local relief
(later) erosion           ->  weathered slopes, carved valleys
flow accumulation         ->  rivers, lakes, coastlines
climate fields            ->  temperature, moisture, with real ranges casting rain shadow
biome classification      ->  named biomes from temperature and moisture
```

Noise appears only as detail *within* this structure, never as its source.

## Core mechanism: world recipes

Different worlds must differ in **which processes run**, not merely in the numbers those processes use. A parameter struct produces a wetter world or a colder one; it cannot produce a world with no plates, or one with volcanism, or one shaped by glaciation.

A world is therefore defined by a **recipe**: an ordered list of generation stages with their parameters.

```text
WorldRecipe
├── stages: ordered list of GenerationStage
└── each stage declares:
        - identity
        - the outputs it produces
        - the outputs it consumes
        - its parameter block
```

Stages resolve in declared dependency order, failing loudly on a missing dependency or a cycle. A standard planet is one recipe. A moon with no plates is a recipe omitting the tectonic stages. A volcanic world adds a stage. None of these require touching generation code.

This generalizes the dependency-graph rule already required for derived networks rather than introducing a second mechanism.

**Deliberately not built:** a scripting language, a plugin system, or string-keyed property bags for field values. The last is forbidden by `product-architecture.md` ("Genes remain explicit numeric fields... not stored in string-keyed maps") and the same reasoning applies to environment values: explicit typed fields, versioned when they change. The first two are speculative weight with no current requirement.

**Honest limit of this flexibility.** Recipes vary which processes run and how. They do not vary the fundamentals: sphere topology, the field value set, and the coordinate model are fixed by this design. Changing those is a rewrite, which is precisely why the sphere decision below is made now rather than later.

## Three tiers

| Tier | Contents | Storage | Depends on |
|---|---|---|---|
| **0** | global structure: plates, boundaries, drift vectors | small, generated once, in memory | — |
| **1** | continuous fields sampled at any point | none beyond a 256-byte permutation table | tier 0 |
| **2** | derived networks: rivers, lakes, coastlines | generated once, in memory, never serialized | tier 1 |

An earlier draft used two tiers with fields as the base. That inverted the real dependency: elevation cannot be evaluated until plate structure exists, so plates must precede fields rather than derive from them.

All tiers are **derived, never authored**. Seed plus recipe regenerates every tier exactly, so nothing is ever written to disk.

### Why rivers cannot be tier 1

A river exists at a point because water from an entire upstream catchment funnels through it. Answering "is there a river here?" requires knowing what lies uphill, which is a non-local query. Pure-function approaches produce river-shaped noise that does not connect, does not flow consistently downhill, and does not reach the sea — the failure is immediately visible.

A2 builds a global node network, samples elevation at each node, routes flow downhill, and accumulates catchment area. A river segment exists where accumulated flow exceeds a threshold. Lakes are basins with no downhill exit. Coastlines and shoreline bands fall out of the elevation/sea-level boundary.

**River resolution must be hierarchical.** A single global network cannot carry rivers at both planet and walking scale: kilometre-scale streams across a planet-sized world imply node counts in the hundreds of millions. A2 therefore uses a coarse global network for major rivers, lakes, and coastlines, plus per-region refinement generated on demand, seeded by region identity and constrained to connect to whichever coarse river passes through. Refinement must be deterministic given `(regionId, seed, coarse network)` so that a region refined, discarded, and refined again produces identical rivers. A2's spec defines node budgets and tests refinement idempotence.

## Scientific and product question

Can a pipeline of seeded generative processes produce worlds that are structurally varied, visually legible, and reproducible, while remaining cheap enough to sample inside fixed-tick simulation loops?

Success is not visual alone. Success is that two seeds produce structurally different worlds — different continents, not merely different noise — that the same seed reproduces bit-identically, and that sampling cost fits the simulation budget.

**Sampling cost is an open question, not an assumption.** A naive construction is unaffordable: evaluating the elevation fBm three times with a six-round hash at each of eight lattice corners per octave lands around 7,000–9,000 operations per sample, plausibly 1–3 microseconds. For scale, the recorded P0 baseline is 0.245 ms per simulation step at 1,000 founders, so one naive sample per creature per tick would cost several times the entire simulation step.

Three design choices attack this: permutation-table gradient lookup, analytic derivatives instead of finite differencing, and coarse evaluation of slowly-varying plate influence. Together they are expected to reduce cost by more than an order of magnitude. **Expected is not measured**, which is why the benchmark is built first.

## Permanent constraints inherited

From `product-architecture.md`:

1. Simulation truth stays independent of Unity rendering truth. Nothing here references `UnityEngine`.
2. Hot state stays explicit, compact, and blittable.
3. Randomness is keyed by stable identity and purpose, never mutable draw order.
4. Ecological roles and labels are derived, never authoritative flags. Generation produces numbers and structures; biome names are analysis output (B), never simulation state.
5. Every optimization requires a recorded before/after benchmark.
6. Earlier prototypes' frozen evidence must keep reproducing or receive an explicit versioned migration.

## Core decision: sample by sphere direction, simulate in local tangent 2D

All generation is a function of a **3D unit-sphere direction**, not a 2D plane coordinate.

The program plan commits to a spherical planet at P7 while keeping high-fidelity kernels in local 2D tangent coordinates ("Keep local 2D coordinates plus region identity; do not rewrite the kernel around sphere coordinates"). Plane noise cannot wrap onto a sphere without a visible seam and a pole singularity, and retrofitting spherical sampling after worlds exist changes every generated world and invalidates every frozen fixture that depends on environment values.

Sampling by direction costs nothing today: the current flat arena is a small tangent patch anchored at a fixed point on the sphere, and the simulation continues to store and integrate planar positions exactly as it does now. Only the lookup converts.

This is the decision in the whole program most expensive to reverse, and cheapest to adopt now.

## Core decision: layered world, with continuous height deferred

Caves require animals to occupy a position the surface heightmap does not determine.

On the surface, height is not free: a ground animal at planar position `(X, Y)` sits at `Elevation(X, Y)`. An independent height value would have exactly one legal setting and would require per-tick ground-clamping to enforce — code written to remove a freedom just paid for.

Caves add exactly one degree of freedom: at a given `(X, Y)` there may be more than one legal place to be. A **layer index** encodes that and no more.

**Decision:** A3 adds a layer index, not continuous height. The world becomes a small stack of surfaces. Each layer keeps planar coordinates and its own spatial grid. Cave entrances are transitions between layers. Perception does not cross layers, which is what makes hiding underground meaningful. A3 sits behind a flag defaulting to single-layer so frozen fixtures reproduce unchanged.

**Deferred, not foreclosed:** flying and swimming are wanted eventually and genuinely need continuous height, because there the value is free rather than derived. Position is shaped for substitution:

```text
readonly struct SimPosition
{
    SimVector2 Planar;   // unchanged planar coordinates
    byte Layer;          // A3: discrete layer; later superseded by continuous height
}
```

**The type change is contained; the semantic change is not.** Logic branching on a discrete layer does not translate mechanically to continuous height, because "underground" stops being a category and becomes a comparison against local surface elevation. Accessors hide storage, not meaning. A3's spec must enumerate the branch sites it creates and state how each becomes a height comparison later.

## A0 — global structure

### Plates

Scatter `PlateCount` seed directions on the sphere using deterministic low-discrepancy placement. Spherical Voronoi cells around those seeds are the plates. Each plate carries:

- a type, oceanic or continental, assigned by seeded threshold against `ContinentalFraction`
- a base elevation offset, continental plates riding higher
- a drift direction, a unit tangent vector at the plate centroid
- a drift rate

Plate count in the tens produces continent-scale structure; the exact figure is a recipe parameter.

### Boundaries

For each pair of adjacent plates, relative motion at the shared boundary classifies it:

| Relative motion | Plate types | Produces |
|---|---|---|
| convergent | continental + continental | high interior ranges |
| convergent | oceanic + continental | coastal range with offshore trench |
| convergent | oceanic + oceanic | curved island arc |
| divergent | any | rift valley or mid-ocean ridge |
| transform | any | fault line, minimal relief |

Each boundary stores its type and an intensity from the magnitude of relative motion.

### What A0 exposes

```text
PlateSample SamplePlate(SimVector3 direction, in WorldStructure structure)
```

returning the containing plate, the distance to the nearest boundary, and that boundary's type and intensity. This is the only interface A1 consumes.

**Cost note:** this query runs inside every elevation sample. Plate influence varies slowly across space, so it is a candidate for coarse evaluation with interpolation rather than exact per-sample nearest-boundary search. The benchmark decides.

## A1 — continuous fields

All types live in `LifeSimulation.Simulation.Environment` unless noted.

### `SimVector3` (in `Simulation.Core`)

Minimal readonly struct: `X`, `Y`, `Z`, `Normalized()`, `Dot()`, `Length()`. Sphere directions need three components. It does not replace `SimVector2`; creature planar positions remain two-dimensional.

### `WorldGeography`

The parameter block for the field stages — parameterized rather than asset-authored, so a configuration UI can drive it later without reworking the simulation.

- **Structure:** `PlateCount`, `ContinentalFraction`, `BoundaryFalloffDistance`, `BoundaryIntensityScale`
- **Shape:** `PlanetRadius`, `ElevationOctaves`, `ElevationLacunarity`, `ElevationGain`, `ElevationAmplitude`, `MountainSharpness`, `SeaLevel`, `DomainWarpStrength`
- **Thermal:** `EquatorTemperature`, `PoleTemperature`, `LapseRatePerUnitElevation`, `SeasonalAmplitude`, `SeasonalPeriodTicks`, `AxialTiltInfluence`
- **Hydrological:** `MoistureOctaves`, `MoistureAmplitude`, `RainShadowStrength`, `PrevailingWindDirection`, `OceanProximityInfluence`
- **Subterranean:** `CaveOctaves`, `CaveThreshold`, `CaveDepthFalloff`
- **Surface:** `ShorelineBandWidth`, `ShorelineMaximumSlope`
- **Drift:** `ClimateDriftPerTick`, `ClimateDriftAmplitude`

`Validate()` throws on out-of-range values, matching `SimulationConfig.Validate()`. A static `Default` provides documented baselines.

`WorldGeography` deliberately carries no seed. World identity lives in `SimulationConfig.WorldSeed`; duplicating it would create two sources of truth that can disagree. The seed passes explicitly into every sampling call.

### `SphereMapping`

```text
SimVector3 Direction(SimVector2 planarPosition, in RegionAnchor anchor, in WorldGeography geography)
```

`RegionAnchor` holds an anchor direction plus two orthonormal tangent basis vectors. A1 has exactly one anchor from configuration; multiple anchors are sub-project C. Local units are treated as arc length along the tangent basis, then re-normalized — accurate for patches small relative to `PlanetRadius`, degrading gracefully beyond.

### `GradientNoise3D`

**Gradient selection uses a permutation table, not the `DeterministicRandom` mixer.**

An earlier draft routed every lattice corner through `DeterministicRandom.Float01`, reasoning that it reuses an already-tested primitive. That was sound for correctness and wrong on cost: `Float01` runs six mixing rounds, and gradient noise touches eight corners per octave, making the mixer the dominant expense in the whole system.

Instead a 256-entry permutation table is built once per world from `WorldSeed` — itself using `DeterministicRandom`, so the seeding path stays tested — and gradient lookup becomes a single array index. Expected improvement is one to two orders of magnitude per corner; the benchmark confirms or refutes it.

The table is 256 bytes of derived, deterministic, rebuilt-at-startup state. This is a deliberate, bounded relaxation of tier-1 statelessness: the property protected is "no baked world data", not "not one byte of anything".

`RandomDomain.EnvironmentField = 9` is added for table construction; existing members keep their values so prior fixtures are unaffected. `DeterministicRandom` itself is not modified.

Provided:

- `Value(SimVector3 point, int seed)` — single octave, range approximately `[-1, 1]`
- `ValueWithDerivative(SimVector3 point, int seed, out SimVector3 derivative)`
- `Fbm(...)` and `FbmWithDerivative(...)`
- `Ridged(...)` — for mountain ranges
- `DomainWarp(SimVector3 point, int seed, float strength)` — offsets input coordinates by a second noise field, converting residual blobbiness into flowing organic shapes for one extra evaluation

**Analytic derivatives replace finite differencing.** Rain shadow needs terrain slope. Obtaining it from two offset samples evaluates the elevation fBm three times per sample — the largest avoidable cost in the design. Gradient noise returns its own derivative for roughly 30% overhead instead of 200%, and that derivative is also the surface normal terrain meshing needs anyway. One change removes two-thirds of the rain-shadow cost and deletes a later normal-generation pass.

Lattice interpolation uses the quintic smoothstep `6t^5 - 15t^4 + 10t^3`, whose derivative `30t^4 - 60t^3 + 30t^2` the analytic path requires. Intermediate math uses `double`, narrowing to `float` at the public boundary, to reduce platform drift.

### `EnvironmentFields`

**`SampleAll` is the primary API**; individual accessors are conveniences that call it.

```text
EnvironmentSample SampleAll(SimVector3 direction, long tick, int worldSeed,
                            in WorldStructure structure, in WorldGeography geography)
```

`EnvironmentSample` is a readonly struct carrying `Elevation`, `SurfaceType`, `Temperature`, `Moisture`, `Fertility`, `CaveDensity`, and `SurfaceNormal`.

Single-call sampling is required, not stylistic: temperature depends on elevation through lapse rate, moisture depends on elevation through rain shadow, fertility depends on both, and all three depend on plate structure. Separate accessors would repeat the most expensive work several times over.

Derivations:

- **Elevation** — plate base offset, plus boundary contribution scaled by type and intensity and falling off with boundary distance, plus `Fbm` blended with `Ridged` by `MountainSharpness`, all sampled through `DomainWarp`.
- **SurfaceType** — derived classification: ocean below `SeaLevel`; shoreline within `ShorelineBandWidth` above it where slope is under `ShorelineMaximumSlope`; otherwise land. The shoreline band is what prevents grass running directly into water.
- **Temperature** — latitude term interpolating `EquatorTemperature` to `PoleTemperature` by `|direction.Y|`, minus `LapseRatePerUnitElevation × max(0, Elevation - SeaLevel)`, plus a seasonal term of period `SeasonalPeriodTicks` scaled by `SeasonalAmplitude`, with `AxialTiltInfluence` scaling how strongly that term opposes between hemispheres, plus a slow climate-drift term.
- **Moisture** — `Fbm` octaves, reduced downwind of high elevation by `RainShadowStrength` along `PrevailingWindDirection`, using the **analytic derivative** for slope rather than offset samples, then raised by an `OceanProximityInfluence` term.

  Ocean proximity in A1 is a local proxy: how far local elevation sits below a threshold above `SeaLevel`. True distance-to-ocean is non-local and belongs to tier 2. **A2 supersedes this** using its coastline structure. Until then, narrow inland basins read as moist.
- **Fertility** — product of moisture suitability, temperature suitability, and an elevation penalty. Zero over ocean.
- **CaveDensity** — 3D `Fbm` at the direction scaled inward by depth, thresholded by `CaveThreshold`, attenuated by `CaveDepthFalloff`. Generated and tested in A1; read by nothing until A3.

### Configuration integration

`SimulationConfig` gains:

- `bool EnvironmentEnabled` (default `false`)
- `WorldRecipe Recipe` (default `WorldRecipe.StandardPlanet`)

This follows the established pattern of `CognitionEnabled` and `PhysiologyEnabled`: an optional world capability resolved at configuration time, not a per-creature polymorphic object.

**The existing `TemperatureField` is not modified.** It remains the temperature source for `PhysiologyEnabled` scenarios so frozen P3 fixtures reproduce byte-identically. Migration onto `EnvironmentFields.Temperature` is sub-project B work under a versioned migration. Two temperature implementations coexist for one prototype cycle; the alternative silently invalidates recorded P3 evidence.

`TemperatureField` currently ignores `WorldSeed`, so every world shares one climate. `EnvironmentFields` resolves that on the new path rather than by editing the old one.

## Recorded decisions for later cycles

Captured because they constrain earlier work or because the reasoning is fresh. Detailed design belongs to each cycle.

### Biomes are a lookup, not noise (B)

Biome classification uses a **Whittaker-style table**: temperature on one axis, precipitation on the other, the pair selecting a biome. Tundra, taiga, temperate forest, grassland, desert, savanna, and rainforest fall out deterministically from fields A1 already produces. No noise is involved.

The table is data carried by the recipe, so a world can define its own biome set without code changes. Biome identity remains analysis output and never enters simulation state, per constraint 4.

### Streaming and presentation (D)

**The performance target is streaming, not generation.** Whole-world generation time is not a concern; a few seconds at startup is acceptable. The requirement is that terrain entering view is ready before it is visible, with no frame stalls.

A small planet is friendlier than an infinite world: the far side self-occludes, so terrain ever in view is bounded. At 2 km radius a visible hemisphere at uniform 64 m tiles is roughly 6,000 chunks — far too many, making level of detail mandatory and reducing that to a few hundred rendered tiles at any altitude.

- **Spatial subdivision:** cubed-sphere quadtree — six cube faces projected onto the sphere, subdivided by camera distance. Squares cannot tile a sphere directly; this is the standard resolution and yields orbit-to-ground zoom (E) as a consequence rather than a separate feature.
- **Alternative to prototype against it:** geometry clipmaps — nested grids centred on the camera displaced by a heightmap texture, with no runtime meshing. Competitive and possibly cheaper. D decides by prototype, not argument.
- **Generation runs on worker threads.** Tier-1 fields have no shared mutable state, so chunk generation parallelizes without locks.
- **GPU generates visuals; CPU remains authoritative for simulation.** GPU floats are not bit-identical across vendors and must never feed simulation. Visual-only divergence of a few centimetres is imperceptible and affects no experiment.
- **Hybrid cave meshing.** Surface stays cheap heightmap tiles; cave interiors need volumetric meshing, generated only where something occupies or approaches the underground layer.
- **Scatter placement.** Rocks and vegetation are placed by thresholded noise against fertility and surface type. Cheap, and currently unowned by any cycle.

Four rules against frame hitches: never generate on the main thread; budget GPU uploads per frame; never block on a missing chunk, draw the coarser parent instead; prefetch along camera velocity.

**Consequence for A1:** bulk sampling for mesh generation is a first-class use case. The benchmark must measure batched throughput, not only single-sample latency.

### Erosion (later)

Previously excluded outright. That was reasonable when terrain was noise and wrong for terrain built by processes. Flow accumulation routes water over existing land without carving it, so rivers initially drape across slopes rather than running in valleys — visibly different from reference imagery.

Erosion is therefore **planned but staged after A2**, with its own spec. It is the difference between terrain that looks generated and terrain that looks weathered.

Plate tectonics beyond A0's static structure — genuine plate motion over geological time — remains out of scope.

## Data flow

```text
seed + recipe
     |
  A0: plates, boundaries, drift            [tier 0, generated once]
     |
creature planar position
     |
  SphereMapping.Direction
     |
  SimVector3 direction + tick + seed + structure + geography
     |
  A1: EnvironmentFields.SampleAll          [tier 1, pure]
     |
  EnvironmentSample { Elevation, SurfaceType, Temperature, Moisture,
                      Fertility, CaveDensity, SurfaceNormal }
     |
  A2: flow accumulation                    [tier 2, generated once]
     |
  B: biome lookup                          [analysis only]
     |
  (no simulation consumer until B — inert by default)
```

## Determinism, state, and versioning

Tier 0 and tier 2 hold generated data; tier 1 holds nothing beyond the 256-byte permutation table, rebuilt deterministically from `WorldSeed` and never mutated. All of it is derived from seed and recipe, so:

- nothing enters `ComputeStateHash`, which stays unchanged while the flag is off — asserted by test
- no serialization is ever required
- tier-1 sampling is thread-safe and parallelizable with no cross-sample dependencies
- coarse sampling for low-fidelity regions needs no separate code path

**Known limitation:** Burst compilation, FMA contraction, or vectorization may alter float results. Cross-platform bit-identical values are not claimed until measured. Same-binary determinism is claimed and tested.

### Generation mathematics is versioned, not just configuration

Configuration and genome layouts already carry schema versions. The generation *mathematics* needs one too, and this is the failure mode most likely to go unnoticed.

Today generation is inert, so changing a constant harms nothing. Once biology reads fields (B), every experiment outcome depends on the exact derivations. Adjusting any of them silently moves results in previously frozen evidence — precisely the drift the gate system exists to prevent.

- `WorldGeneration.SchemaVersion` is incremented whenever any derivation changes output.
- Experiment manifests record it alongside config and genome schema versions.
- A test pins sampled output for a fixed input set against recorded golden values. That test failing is the intended signal: increment the version and re-run or explicitly migrate dependent evidence.

The golden-value test is introduced in A1, while nothing depends on the fields and establishing the baseline is free.

## Error handling

- `WorldGeography.Validate()` and `WorldRecipe.Validate()` throw `ArgumentOutOfRangeException` on invalid parameters, and recipe validation additionally rejects missing dependencies and cycles. Both run once at configuration time, never in the sampling path.
- `SampleAll` performs no validation and no allocation. Callers pass validated inputs.
- Directions are normalized defensively in `SphereMapping`, not in `SampleAll`.
- Non-finite output is a defect, not a runtime-handled condition; the range-bound test exists to catch it.

## Implementation ordering: benchmark first

The standard delivery cycle puts the performance gate at phase 7. **A0/A1 invert that deliberately.** The sampling-cost estimate is close enough to unaffordable that several parameters cannot be chosen honestly without measurement, and discovering that after the fields are built means rebuilding them.

1. `SimVector3`, `WorldGeography`, `WorldRecipe`, and `GradientNoise3D` — the minimum to evaluate one octave.
2. **Benchmark harness and first measurement**, recorded to `docs/benchmarks/`, covering single-sample latency and batched throughput.
3. The measurement decides three open parameters:
   - octave counts for elevation, moisture, and caves
   - whether plate lookup is exact per-sample or coarse-plus-interpolated
   - whether a tier-1 cache is optional or mandatory
4. A0 plate structure, then `SphereMapping`, `EnvironmentFields`, and the remaining derivations against the chosen parameters.
5. Remaining fixtures and the frozen-fixture regression run.

Gradient lookup is no longer open — the permutation table is specified. The benchmark confirms the expected improvement rather than choosing.

This is consistent with `PERFORMANCE.md` ("optimize only measured bottlenecks"): the measurement is not an optimization pass, it is a missing design input.

## Testing

Deterministic fixtures are written before implementation.

1. **Reproducibility** — identical inputs yield bit-identical values across repeated calls and fresh instances.
2. **Seed sensitivity** — differing seeds produce structurally different worlds; plate layouts differ and mean absolute field difference exceeds a documented threshold.
3. **Sphere continuity** — a dense great circle crossing where a plane generator would seam shows maximum adjacent-sample delta below a bound. *This test is the reason the sphere decision exists.*
4. **Pole sanity** — samples near both poles are finite, in range, and non-singular.
5. **Range bounds** — every field stays within documented bounds over sampled directions and a tick sweep.
6. **Plate partition** — every direction resolves to exactly one plate; boundary distances are non-negative and continuous across cells.
7. **Boundary classification** — constructed plate pairs with known relative motion produce the expected boundary type.
8. **Lapse-rate monotonicity** — at fixed direction and tick, increasing elevation strictly decreases temperature.
9. **Seasonal closure** — temperature at a fixed point after exactly `SeasonalPeriodTicks` returns to its start within epsilon.
10. **Cave depth attenuation** — cave density falls monotonically with depth beyond the falloff distance and is zero above the surface.
11. **Derivative agreement** — the analytic derivative matches a finite-difference approximation within a documented tolerance.
12. **Recipe resolution** — stages resolve in dependency order; missing dependencies and cycles fail loudly; omitting the tectonic stages produces a valid plateless world.
13. **Hash invariance** — enabling `EnvironmentEnabled` with no consumers leaves `ComputeStateHash` identical over 1,000 ticks.
14. **Allocation guard** — 100,000 `SampleAll` calls allocate zero managed bytes.
15. **Frozen-fixture regression** — all existing P0 through P3 fixtures produce unchanged results and unchanged state hashes.
16. **Golden-value pin** — a fixed input set reproduces recorded output values, guarding the generation mathematics against silent drift.

A micro-benchmark records nanoseconds per `SampleAll` into `docs/benchmarks/` with commit SHA, hardware, and build type, measuring both single-sample latency and batched throughput.

## Explicit exclusions

- rivers, lakes, coastlines, and flow accumulation (A2)
- layered positions, cave traversal, spatial-grid changes (A3)
- biome tables and any biology reading any field (B)
- terrain meshes, streaming, geometry level of detail (D)
- region partitioning or multiple anchors (C)
- camera or zoom work (E)
- erosion — planned, staged after A2, own spec
- plate motion over geological time; A0 structure is static once generated
- changes to food or water resource placement
- navigation, obstacles, pathfinding
- scripting languages, plugin systems, string-keyed field bags
- caching of tier-1 fields is presumed excluded, but the early benchmark may overturn this — the one exclusion measurement may reverse within A1

## Exit gates

**A0** is complete when plate partition and boundary classification tests pass, plate structure regenerates identically from seed, and `SamplePlate` cost is recorded.

**A1** is complete when:

- all sixteen fixtures pass, sphere continuity and the golden-value pin included
- `SampleAll` allocates zero bytes, with single-sample and batched figures recorded in `docs/benchmarks/`
- the three benchmark-gated parameters are decided by measurement and their values recorded
- frozen P0 through P3 fixtures reproduce with unchanged state hashes
- two seeds demonstrably produce structurally different worlds, not merely different noise
- a recipe omitting tectonic stages produces a valid world, demonstrating the flexibility mechanism
- `TemperatureField` remains unmodified and P3 physiology evidence remains valid
- `WorldGeneration.SchemaVersion` exists and is written into experiment manifests
- the `EnvironmentSample` surface is documented well enough for A2 and B to consume without reading internals
