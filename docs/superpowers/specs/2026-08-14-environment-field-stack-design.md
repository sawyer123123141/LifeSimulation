# Environment Field Stack Design

**Status:** design approved. Sub-project A1 is specified for implementation here; A2 and A3 are scoped with their architectural decisions recorded, and each needs its own spec before implementation.

**Scope of this document:** the continuous environment fields (A1), plus the load-bearing architectural decisions that A2 and A3 depend on. It does not specify biomes, terrain meshes, regions, or camera work.

## Position in the terrain program

The environment work decomposes as follows. Each row is its own spec, plan, and implementation cycle.

| Sub-project | Contents | Depends on |
|---|---|---|
| **A1** (specified here) | continuous fields: elevation, temperature, moisture, fertility, cave density | — |
| **A2** | derived structures: rivers, lakes, coastlines | A1 |
| **A3** | layered world model and position representation | — (kernel change, independently schedulable) |
| B | biome derivation, creature adaptation to fields | A1, A2 |
| C | sphere topology, region partitioning | A1 |
| D | terrain mesh and visual style | A1, A2, C |
| E | organism-to-planet camera zoom | D |

A1 is first because it is the only piece that pays off immediately: the P4 producer kernel requires moisture and fertility fields by name (`p0-p7-program-plan.md`, P4 coevolution phase), and every later sub-project reads from it.

**A3 is not terrain work.** It changes creature position representation, the spatial grid, perception, and the state hash. It is listed here because caves motivated it, but it can be scheduled independently and should not be bundled into a terrain implementation plan.

## Scientific and product question

Can a small set of seeded scalar fields produce spatially varied, temporally shifting selection pressure that is reproducible across runs and cheap enough to sample inside fixed-tick hot loops?

Success is not visual. Success is that two runs with different world seeds present measurably different environmental conditions at the same simulation coordinates, that the same seed reproduces bit-identically, and that sampling costs little enough to call from perception or plant-growth loops.

## Permanent constraints this design inherits

From `product-architecture.md`:

1. Simulation truth stays independent of Unity rendering truth. Nothing in this design references `UnityEngine`.
2. Hot state stays explicit, compact, and blittable.
3. Randomness is keyed by stable identity and purpose, never mutable draw order.
4. Ecological roles and labels are derived, never authoritative flags. Fields produce numbers; they do not produce a `BiomeType` enum in simulation state.
5. Every optimization requires a recorded before/after benchmark.
6. Earlier prototypes' frozen evidence must keep reproducing or receive an explicit versioned migration.

## Architecture: two tiers

The environment splits into two tiers with different storage characteristics. This split is what makes the system extensible: new environmental features are added as tier-2 generators without modifying tier 1.

### Tier 1 — continuous fields (A1)

Pure functions of position. No storage, no state, no serialization. Answers "what is the value here?" by evaluating noise at one point.

Suitable for anything whose value at a point depends only on that point: elevation, temperature, moisture, fertility, cave density.

### Tier 2 — derived structures (A2)

Generated once per world from tier 1, held in memory, never serialized. Deterministically rebuildable from the seed alone, so they remain disposable cache rather than saved state.

Required for anything whose value at a point depends on *other* points. Rivers are the motivating case and are examined below. Lakes and coastlines fall out of the same pass. Later additions — mineral veins, reefs, volcanic regions — register here as additional generators.

### Why rivers cannot be tier 1

A river exists at a point because water from an entire upstream catchment funnels through it. Answering "is there a river here?" requires knowing what lies uphill, which is a non-local query. Pure-function approaches produce river-shaped noise that does not connect, does not flow consistently downhill, and does not reach the sea — the failure is immediately visible.

A2 therefore builds a coarse global node network over the sphere, samples elevation at each node, routes flow downhill, and accumulates catchment area. A river segment exists where accumulated flow exceeds a threshold. Lakes are basins where flow has no downhill exit. Coastlines fall out of the elevation/sea-level boundary.

This is a genuine departure from the storage-free property of tier 1, and it is accepted deliberately because the alternative does not produce believable rivers. The mitigating property is that the structures are derived, not authored: seed plus configuration regenerates them exactly, so nothing is ever written to disk.

## Core decision: sample by sphere direction, simulate in local tangent 2D

Fields are functions of a **3D unit-sphere direction**, not a 2D plane coordinate:

```text
Sample(SimVector3 direction, long tick, int worldSeed, in WorldGeography geography) -> EnvironmentSample
```

Rationale: the program plan commits to a spherical planet at P7 while keeping high-fidelity kernels in local 2D tangent coordinates (`p0-p7-program-plan.md`: "Keep local 2D coordinates plus region identity; do not rewrite the kernel around sphere coordinates"). Two-dimensional plane noise cannot be wrapped onto a sphere without a visible seam and a pole singularity. Retrofitting spherical sampling after worlds exist would change every generated world and invalidate every frozen experiment fixture that depends on environment values.

Sampling by direction costs nothing today. The current flat arena is treated as a small tangent patch anchored at a fixed point on the sphere. The simulation continues to store and integrate planar positions exactly as it does now. Only the field lookup converts.

This is the single decision in A1 that is expensive to reverse later, and cheap to adopt now.

## Core decision: layered world, with continuous height deferred

Caves require animals to occupy a position that the surface heightmap does not determine.

On the surface, height is not a free variable: a ground animal at planar position `(X, Y)` sits at `Elevation(X, Y)`. Storing an independent height for surface creatures adds a value with exactly one legal setting, and then requires ground-clamping machinery every tick to enforce it — code written to remove a freedom that was just paid for.

Caves introduce exactly one new degree of freedom: at a given `(X, Y)` there may now be more than one legal place to be — on the hillside, or in the tunnel beneath it. A **layer index** encodes precisely that, and no more.

**Decision:** A3 adds a layer index, not a continuous height. The world becomes a small stack of surfaces (surface, shallow underground, deep underground). Each layer keeps ordinary planar coordinates and its own spatial grid. Cave entrances are transitions between layers. Perception does not cross layers, which is what makes hiding underground meaningful.

**Deferred, not foreclosed:** flying and swimming creatures are wanted eventually, and they genuinely need continuous height, because there the value is free rather than derived. The position type is therefore shaped so a continuous height can replace the layer index without restructuring callers:

```text
readonly struct SimPosition
{
    SimVector2 Planar;   // unchanged planar coordinates
    byte Layer;          // A3: discrete layer; later superseded by continuous height
}
```

Callers use accessors rather than touching the fields directly, so the eventual substitution is contained. A3's spec must define the migration path explicitly before implementation.

A3 sits behind a configuration flag defaulting to single-layer, so existing scenarios and frozen fixtures reproduce unchanged.

## Components (A1)

All types live in `LifeSimulation.Simulation.Environment` unless noted.

### `SimVector3` (in `Simulation.Core`, beside `SimVector2`)

Minimal readonly struct: `X`, `Y`, `Z`, plus `Normalized()`, `Dot()`, and `Length()`. Added because sphere directions require three components. It does not replace `SimVector2`; creature planar positions remain two-dimensional.

### `WorldGeography`

The readonly settings struct — the "customizable" surface. Parameterized rather than asset-authored, so a configuration UI can drive it later without reworking the simulation. Grouped knobs:

- **Shape:** `PlanetRadius`, `ElevationOctaves`, `ElevationLacunarity`, `ElevationGain`, `ElevationAmplitude`, `MountainSharpness`, `SeaLevel`
- **Thermal:** `EquatorTemperature`, `PoleTemperature`, `LapseRatePerUnitElevation`, `SeasonalAmplitude`, `SeasonalPeriodTicks`, `AxialTiltInfluence`
- **Hydrological:** `MoistureOctaves`, `MoistureAmplitude`, `RainShadowStrength`, `OceanProximityInfluence`
- **Subterranean:** `CaveOctaves`, `CaveThreshold`, `CaveDepthFalloff`
- **Drift:** `ClimateDriftPerTick`, `ClimateDriftAmplitude`

`Validate()` throws on out-of-range values, matching `SimulationConfig.Validate()`. A static `Default` provides documented baseline values. Presets ("wet world", "ice age") are just alternative instances; no preset machinery is built in A1.

`WorldGeography` deliberately does **not** carry a seed. World identity already lives in `SimulationConfig.WorldSeed`, and duplicating it would create two sources of truth that can disagree. The seed is passed explicitly alongside the geography into every sampling call.

### `SphereMapping`

Converts a local tangent planar position to a unit-sphere direction:

```text
SimVector3 Direction(SimVector2 planarPosition, in RegionAnchor anchor, in WorldGeography geography)
```

`RegionAnchor` holds the anchor's sphere direction plus two orthonormal tangent basis vectors. In A1 there is exactly one anchor, supplied by configuration; multi-region anchors are sub-project C. The mapping treats local units as arc length along the tangent basis and re-normalizes, which is accurate for patches small relative to `PlanetRadius` and degrades gracefully beyond that.

### `GradientNoise3D`

Deterministic 3D gradient noise plus fractal Brownian motion.

**`DeterministicRandom` is not modified.** Its existing signature exposes four independent integer slots — `(tickOrOrdinal, entityA, entityB, purpose)`. Lattice coordinates map to the first three; `purpose` selects the gradient component:

```text
DeterministicRandom.Float01(seed, RandomDomain.EnvironmentField, latticeX, latticeY, latticeZ, componentPurpose)
```

Gradients come from the classic 12-edge gradient table, indexed by the hashed value, avoiding trigonometry in the inner loop. A new enum member `RandomDomain.EnvironmentField = 9` is added; existing members keep their values so prior fixtures are unaffected.

Provided:

- `Value(SimVector3 point, int seed)` — single octave, range approximately `[-1, 1]`
- `Fbm(SimVector3 point, int seed, int octaves, float lacunarity, float gain)`
- `Ridged(SimVector3 point, int seed, int octaves, float lacunarity, float gain)` — for mountain ranges

Lattice interpolation uses the quintic smoothstep `6t^5 - 15t^4 + 10t^3`. Intermediate math uses `double`, narrowing to `float` only at the public boundary, to reduce platform drift.

### `EnvironmentFields`

The public sampling surface. **`SampleAll` is the primary API**; the individual accessors are conveniences that internally call it.

```text
EnvironmentSample SampleAll(SimVector3 direction, long tick, int worldSeed, in WorldGeography geography)
```

`EnvironmentSample` is a readonly struct carrying `Elevation`, `IsOcean`, `Temperature`, `Moisture`, `Fertility`, and `CaveDensity`.

Single-call sampling is required rather than stylistic: temperature depends on elevation through the lapse rate, moisture depends on elevation through rain shadow, and fertility depends on both. Separate accessors would evaluate the elevation fBm three redundant times — the dominant cost in the whole stack.

Field derivations:

- **Elevation** — `Fbm` blended with `Ridged` by `MountainSharpness`, scaled by `ElevationAmplitude`. Documented output range.
- **IsOcean** — `Elevation < SeaLevel`. A derived query, not stored state, and not read by any biology in A1.
- **Temperature** — latitude term interpolating `EquatorTemperature` to `PoleTemperature` by `|direction.Y|`, minus `LapseRatePerUnitElevation × max(0, Elevation - SeaLevel)`, plus a seasonal term of period `SeasonalPeriodTicks` scaled by `SeasonalAmplitude`. `AxialTiltInfluence` scales how strongly the seasonal term flips sign between hemispheres: at zero, seasons are global and synchronized; at one, the hemispheres are fully opposed. A slow climate-drift term is added last.
- **Moisture** — independent `Fbm` octaves, reduced downwind of high elevation by `RainShadowStrength`, then raised toward wet by an `OceanProximityInfluence` term.

  **Ocean proximity is a local proxy, not a real distance query.** True distance-to-nearest-ocean is a non-local search and belongs to tier 2. Until A2 exists, the term is driven by how far local elevation sits below a threshold above `SeaLevel` — low ground reads as wetter, high ground as drier. This is an approximation, chosen deliberately, and it means narrow inland basins read as moist. **A2 supersedes it** using the coastline structure produced by the flow pass, where a genuine distance query is affordable.

  Rain shadow is likewise approximated from the local elevation gradient — a two-sample finite difference along a fixed prevailing-wind direction — not a simulated atmosphere. It costs two extra elevation evaluations per sample, which is the single largest cost in `SampleAll` and the first thing to measure in the benchmark.
- **Fertility** — product of a moisture suitability curve, a temperature suitability curve, and an elevation penalty. Zero over ocean.
- **CaveDensity** — 3D `Fbm` evaluated at the sample direction scaled inward by depth, thresholded by `CaveThreshold` and attenuated by `CaveDepthFalloff`. Returns a continuous openness value; A3 interprets values above threshold as traversable cave space. In A1 it is generated and tested but read by nothing.

### Configuration integration

`SimulationConfig` gains two members:

- `bool EnvironmentEnabled` (default `false`)
- `WorldGeography Geography` (default `WorldGeography.Default`)

This follows the established pattern of `CognitionEnabled` and `PhysiologyEnabled`: an optional world capability resolved at configuration time, not a per-creature polymorphic object.

**The existing `TemperatureField` is not modified.** It remains the temperature source for `PhysiologyEnabled` scenarios so frozen P3 fixtures reproduce byte-identically. Migrating physiology onto `EnvironmentFields.Temperature` is sub-project B work, performed under a versioned configuration migration. This design deliberately accepts two temperature implementations coexisting for one prototype cycle; the alternative silently invalidates recorded P3 evidence.

Note that `TemperatureField` currently ignores `WorldSeed`, so every world shares one climate. `EnvironmentFields` resolves that on the new path rather than by editing the old one.

## Data flow

```text
creature planar position
        |
   SphereMapping.Direction(planar, anchor, geography)
        |
   SimVector3 unit direction  +  tick  +  WorldSeed  +  WorldGeography
        |
   EnvironmentFields.SampleAll
        |
   EnvironmentSample { Elevation, IsOcean, Temperature, Moisture, Fertility, CaveDensity }
        |
   (no consumer in A1 — inert by default)
```

## Determinism and state

Tier-1 fields hold no mutable state. Every value is a pure function of `(direction, tick, worldSeed, geography)`. Consequences:

- Nothing is added to `ComputeStateHash`, and the hash is unchanged while the flag is off — asserted by test.
- No serialization is ever required. Seed plus configuration reproduces any world.
- Sampling is thread-safe and trivially parallelizable, with no cross-sample dependencies.
- Coarse sampling for distant, low-fidelity regions requires no separate code path (sub-project C).

Tier-2 structures (A2) hold generated data but remain derived and disposable; their spec must state how regeneration determinism is tested.

**Known limitation, stated explicitly:** Burst compilation, FMA contraction, or vectorization may alter float results. Until measured, cross-platform bit-identical field values are not claimed. Same-binary determinism is claimed and tested.

## Error handling

- `WorldGeography.Validate()` throws `ArgumentOutOfRangeException` on non-finite or out-of-range parameters, called once at configuration time, never in the sampling path.
- `SampleAll` performs no validation and no allocation. It is a hot-loop function; callers pass a validated `WorldGeography`.
- Directions are normalized defensively inside `SphereMapping`, not inside `SampleAll`.
- Non-finite output is a defect, not a runtime-handled condition. The range-bound test exists to catch it.

## Testing (A1)

Deterministic fixtures are written before implementation, per the standard delivery cycle.

1. **Reproducibility** — identical `(seed, direction, tick)` yields bit-identical values across repeated calls and fresh instances.
2. **Seed sensitivity** — differing seeds produce materially different fields; mean absolute difference across N sampled directions exceeds a documented threshold.
3. **Sphere continuity** — sample a dense great circle crossing the region where a plane-based generator would seam; assert maximum adjacent-sample delta stays below a bound. *This test is the reason the sphere decision exists; it must exist and pass.*
4. **Pole sanity** — samples near both poles are finite, in range, and free of singular behavior.
5. **Range bounds** — every field stays within documented bounds over M pseudo-random directions and a tick sweep.
6. **Lapse-rate monotonicity** — at fixed direction and tick, increasing elevation strictly decreases temperature.
7. **Seasonal closure** — temperature at a fixed point after exactly `SeasonalPeriodTicks` returns to its starting value within epsilon.
8. **Cave depth attenuation** — cave density falls monotonically with depth beyond the falloff distance, and is zero above the surface.
9. **Hash invariance** — enabling `EnvironmentEnabled` with no consumers leaves `ComputeStateHash` identical over 1,000 ticks.
10. **Allocation guard** — 100,000 `SampleAll` calls allocate zero managed bytes.
11. **Frozen-fixture regression** — all existing P0 through P3 fixtures produce unchanged results and unchanged state hashes.

A micro-benchmark records nanoseconds per `SampleAll` into `docs/benchmarks/`, following the project's benchmark discipline (commit SHA, hardware, build type).

## Explicit exclusions

Deferred deliberately; none of these are in A1:

- rivers, lakes, coastlines, and the flow-accumulation pass (A2)
- layered positions, cave traversal, and spatial-grid changes (A3)
- biome classification or naming (B)
- any biology reading any field (B)
- terrain meshes, materials, or visual style (D)
- region partitioning or multiple anchors (C)
- camera or zoom work (E)
- changes to food or water resource placement
- navigation, obstacles, or pathfinding
- plate tectonics — out of scope for the whole environment program
- hydraulic erosion — out of scope for A1 and A2. Flow accumulation finds where water *goes* on existing terrain; it does not carve valleys, so rivers will initially run through terrain that was not shaped by them. If that reads as artificial once terrain is visible (D), a light erosion pass applied to the elevation field is the remedy, and it warrants its own spec rather than being smuggled into A2.
- any caching or memoization of tier-1 fields, until a benchmark justifies it
- Burst or Jobs adoption

## Exit gate (A1)

A1 is complete when:

- all eleven test fixtures pass, sphere continuity included
- `SampleAll` allocates zero bytes and its cost is recorded in `docs/benchmarks/`
- frozen P0 through P3 fixtures reproduce with unchanged state hashes
- two different world seeds are shown to produce measurably different environmental conditions at identical simulation coordinates
- `TemperatureField` remains unmodified and P3 physiology evidence remains valid
- the `EnvironmentSample` surface is documented well enough for A2 and B to consume without reading field internals
