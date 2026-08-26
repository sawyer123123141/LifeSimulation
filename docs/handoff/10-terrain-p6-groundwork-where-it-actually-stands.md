## 10. Terrain (P6 groundwork) — where it actually stands

**Read `docs/terrain-brainstorm-2-2026-08-23.md` and `docs/reference-implementations.md` before
touching terrain.** The first explains why the original design was structurally wrong rather than
under-tuned; the second lists reference implementations and what each maps to.

**The lesson that cost the most: read the reference implementation before writing the system, not
after.** Fifteen rounds of first-principles tuning gave six wrong diagnoses; twenty minutes of
reading gave the architecture.

### Architecture as it now stands

| file | role |
|---|---|
| `PlateStructure` | T0 tectonics: Fibonacci plate seeds, spherical Voronoi, boundary classification by relative motion, and **blending between the two nearest plates** |
| `PlanetTerrain` | signed elevation composed from layered bands, plus moisture and temperature |
| `PlanetBiome` | Whittaker-style temperature x moisture palette, blended; `Classify` buckets the same fields for counting |
| `IcoSphere` | pole-free sphere |
| `TerrainMeshBuilder` | **the single mesh/material/lighting path**, shared by the live preview and the offline capture |
| `TerrainPreview` | the `K` viewer |
| `TerrainRenderEntry` | offline PNG capture (`Life Simulation > Render Terrain Views`) |
| `TerrainStatisticsEntry` | field statistics (`Life Simulation > Dump Terrain Statistics`) |
| `TerrainSettings` | **every tunable, in one object.** Pure C#, no Unity types, so an offline probe can compile it |
| `TerrainTuningPanel` | the `J` panel: live sliders over `PlanetTerrain.Active` |
| `tools/TerrainProbe` | **field measurement without Unity** - the editor instruments cannot run while the editor holds the lock |
| `TerrainView` | Presentation's mutable settings instance, for the panel. **Simulation never reads it** |
| `ArenaProjection` | maps a flat simulation position onto the planet, for drawing only |
| `PlanetBiome.Classification` | biome naming, free of UnityEngine, so the probe can count biomes |

**Generation lives in `Assets/Scripts/Simulation/World/`** (`PlanetTerrain`, `PlateStructure`,
`TerrainSettings`) since `8c82c77`. Presentation consumes it; the simulation reads it when the join
flag is on.

### Defects found and fixed, with the measurement that found each

These are recorded because most were invisible to reasoning and several survived multiple wrong
diagnoses:

- **Elevation was a bounded 0..1 field with sea level at 0.38 inside it.** That forces a clamp, the
  clamp forces a knee, and an interior threshold forces a branch at the waterline - three slope
  discontinuities, and a terrace *is* a slope discontinuity. Now **signed displacement**; the coast
  is the zero crossing.
- **The Voronoi plate lookup was piecewise constant.** Measured a jump of **0.825 between samples one
  unit apart against a median of 0.00093 - a ratio of 885**. Every plate boundary was a vertical
  cliff. Blending the two nearest plates took it to 0.0417, ratio 34.
- **`MaximumSlope` was wrong by 20x.** Elevation 1.0 is ~30 m and a radian is 500 m, so 0.55 was a
  **3% grade** and it crushed every band above ~10 cycles/radian to centimetres. Now 6.
- **Height scale was proportional to view width**, so the same ground rendered **eight times flatter
  the closer you looked**. Now a constant 30 world units per elevation unit.
- **Nothing existed at creature scale.** The hill band is a 77 m wavelength, so **less than one hill
  spanned the 50 m arena**. Added local (9 m) and micro (3 m) bands, sampled only when resolvable.
- **The striped combs were lighting, not geometry.** An **unlit render removed them entirely** - the
  test that should have been first, not seventh.
- **The preview had no water**, so the "sea with hills" was sea *bed*. The capture had water and the
  runtime did not, which is why the PNGs looked right and Play mode did not.
- **The capture and runtime had drifted** (321 vs 161 samples, jitter offline only, different
  camera). Now one path, so a PNG is evidence about the Play view.

### Instruments — use these before changing a coefficient

1. `Life Simulation > Dump Terrain Statistics` — deciles, land fraction, biome counts, saturation,
   **adjacent-sample gradient** (the one that found the 885x step), and per-window land fraction.
2. `Life Simulation > Render Terrain Views` — PNGs at 976x752 into `Logs/terrain`, including a
   **planet-marked** image tinting exactly the region the flat views show. That answers "are these
   the same world?", and it has already refuted one confident explanation.

3. `dotnet run` in **`tools/TerrainProbe`** - the same gradient measurement, compiled straight
   against `PlanetTerrain`, with **no Unity in the loop**. The editor instruments cannot run while
   the editor holds the project lock, which is exactly when terrain is being looked at.

**A field statistic cannot see a rendering defect and a render cannot see a field discontinuity.**
Every instrument here exists because each missed something another caught.

### Known open

- **The planet has no adaptive LOD.** Subdivision is fixed at 5 (~20k triangles, ~19 m each), so
  zooming in never adds detail. That is T6 (chunk streaming, geometry level of detail), not tuning.
- A small stepped comb remains on some steep ridges.
- Ice cover looks high; the ice fraction was 0.074 of surface at the last measurement. The `J` panel
  has an **altitude cooling** slider, which is the coefficient that decides it.

### Adaptation is invisible, and the plan for that is a document on purpose

**`docs/creature-appearance.md`.** Selection moves temperature tolerance by a quarter of its range and
nothing on screen changes: size is driven by the body-size gene, which is one of the ten traits under
no detectable selection, and colour is driven by the current action. **P5 does not fix this** - it is
an analysis layer that can say a population split and draws nothing.

**Not built, deliberately: real models are expected soon.** The doc separates the part that survives a
model swap - a pure `(Genome) -> CreatureAppearance` function, testable headlessly like
`FreeCameraMotion` - from the part that does not, which is the three lines in
`Prototype1Presenter.Views.cs` that assume a capsule and one material. Building the pure half early is
safe; building the applied half early is what would be redone.

### Body size shrinks under scarcity, with a dose-response (2026-08-24)

**`docs/experiments/p6-body-size-shrinks-under-scarcity-2026-08-24.md`.** The mechanism was already
there: `bodyMass = 0.6 * 4^BodySize` is a fourfold range, charged against energy per distance and
water per second, and **nothing pays a creature for being large** - the only thing size buys is a
bigger carcass, which feeds whoever eats it.

Drift from founders, baseline arm, extinct runs excluded, 30 seeds per level:

| resources | body_size drift | t | vs control | control t | extinct |
|---|---|---|---|---|---|
| moderate 1.0x | -0.0160 | -1.20 | 2.6x | +1.22 | 1 / 30 |
| lean 0.6x | **-0.0394** | **-2.19** | 21.8x | +0.36 | 13 / 30 |
| scarce 0.35x | **-0.0769** | **-2.34** | 20.9x | -0.23 | 24 / 30 |

**Monotonic**: halve the resources and the shrinking roughly doubles. Not distinguishable from the
control at full resources - scarcity is what makes mass expensive enough to matter.

**Replicated at 80 seeds** (55 surviving): body_size **-0.0252, t = -3.23**, control +0.0002 at
t = 0.07. Direction and significance hold; the magnitude is smaller than the 30-seed run's -0.0394,
so read these as direction and rough size, not as a coefficient. `temperature_tolerance` in the same
run is +0.2999 at t = 24.3 - **1664x the control**.

**Survivor conditioning is real and is why the extinction counts sit beside every row.** Drift is over
surviving runs and scarcity causes the deaths, so magnitudes are not comparable *between* levels. At
0.35x only 6 of 30 survived; read that row as direction, not size.

**`SimulationScenario.Scaled(id, factor)` is new** - it multiplies every amount, capacity and
regeneration of an existing layout, so a scarcity arm differs from abundance in exactly one thing.

### Selection is happening, and it had never been measured (2026-08-24)

**`docs/experiments/p6-selection-is-happening-2026-08-24.md`.** Every creature measurement on record
compared arm against arm, which is blind to selection by construction - a trait under selection in
both arms cancels exactly. Drift from founders, against the `NeutralMarker` control:

| gene | founder | drift | t | vs control |
|---|---|---|---|---|
| temperature_tolerance | 0.480 | **+0.277** | **11.03** | 29x |
| lifespan_tendency | 0.517 | **+0.257** | **7.90** | 27x |
| urgency_exponent | 0.500 | -0.052 | -4.34 | 5.4x |
| neutral_marker | 0.500 | -0.010 | -0.72 | 1.0x |

**Every gene starts at 0.50**, so regression to the centre cannot explain a shift of +0.277 away from
it - that column is what makes this a finding rather than the retracted claim in
`p4-defense-selection-demonstrated-2026-08-18.md`. The other ten traits sit inside the control's
range: **no detectable selection there**, which is not the same as inert.

**Not explained:** why these three. Temperature tolerance moving alongside the terrain join - which
is what introduced a real temperature field - is a hypothesis this run does not test, and is the
obvious next measurement.

### Terrain is no longer cosmetic — both halves exist, one is unmeasured

**Half one, the field (`terrainDrivenEnvironmentEnabled`, done 2026-08-23).** Moisture, temperature
and elevation come from the terrain generator. The plant corpus was re-measured under it; see the
Phase G experiments.

**Half two, the slope (`slopeMovementCostEnabled`, added 2026-08-24, DEFAULT OFF AND UNMEASURED).**
Climbing costs energy. The mechanism is the smallest one available: energy drain is already
proportional to `DistanceSinceLastNeeds`, so a climb is charged **as extra distance** -
`climb x PlanetTerrain.MetresPerElevationUnit x SimulationConfig.SlopeClimbCost`, uphill only, no new
creature state. `SlopeClimbCost` is 4, the human figure, and is a coefficient rather than a
measurement of this world.

`SlopeMovementCostTests` pins three things: it is live with terrain under it, it is **exactly** inert
without elevation, and it is off by default. All three compare `ComputeBehaviorHash`, not
`ComputeStateFingerprint` - **the fingerprint folds in the configuration hash, so it differs the
moment any flag differs, which makes a liveness test pass vacuously.** That mistake was made and
caught here.

**The measurement exists now, and it is a null.** `tools/CreatureSweep`, 240 runs over 120 paired
seeds: **nothing crosses |t| = 2 in fourteen columns**, and the `NeutralMarker` control - a gene that
responds to nothing by construction - sits mid-pack among the columns that supposedly could respond.
Extinctions 2 against 3. Full account in `docs/experiments/p6-slope-cost-2026-08-24.md`.

**Read the limitations before treating that as settled.** 96 of 120 pairs finished at the population
cap of 48, so a survival effect smaller than the headroom cannot appear; and nothing records distance
travelled, so a behavioural response is not distinguishable from indifference. **The flag stays
off.** A decisive version needs a scenario built for the question rather than inherited from the
plant corpus - population uncapped, resources placed so reaching them means climbing, and a
distance-travelled statistic.

**Third condition, and the one that answers it** (`--focused 120 100`,
`docs/experiments/p6-slope-cost-cap100-2026-08-24.md`). At a cap of 100 the ecology is healthy -
**2 extinctions against 2** in 120 pairs, every pair diverged - and the table is quiet enough to
read. **Creatures that pay to climb stand on flatter ground**: `occupied_slope` -0.0345, t = -2.09,
sign test 71 negative against 45, z = 2.41. That is the mechanism's a-priori prediction and it is a
behavioural readout rather than a gene, so it needs no selection to have happened. Energy points the
same way.

**Do not oversell it.** The control `NeutralMarker` comes in at sign z = -1.64, which is the noise
floor of these tests; 2.41 clears it without towering over it, and one column of fourteen past
|t| = 2 is what chance produces. **No gene moved** - every gene column is at |t| <= 1.36. Behaviour
responds within a lifetime; selection would need a stronger cost or far longer runs.

**`slopeMovementCostEnabled` is now on for the `Y` terrain playtest scenario** - the scenario whose
whole purpose is that terrain means something. The configuration default stays `false`, and every
recorded plant result remains scoped to runs without it.

**A second, focused run fixed both limitations** (`--focused`, 60 relief-bearing seeds, cap 200,
`docs/experiments/p6-slope-cost-focused-2026-08-24.md`). Every pair diverged - 0 identical hashes -
and the result is **suggestive, not established**: extinction 46 against 38, but the paired test is
what counts and the discordant pairs are 13 against 5, McNemar chi-squared 2.72 against 3.84 for
p = .05.

**Every gene column in that run is an artefact, and the control proves it.** All of them moved by
about the same amount including `NeutralMarker` (t = -1.79), which responds to nothing - so the
movement is composition from differential extinction, not selection. The two cells past |t| = 2 sit
inside the control's own spread. The headline population figure of -27.5 is the extinction signal in
different units, since it counts dead worlds as zero.

**What that run cost:** the cap of 200 made the ecology fragile - 33 of 60 pairs went extinct in both
arms - so it measures a stressed population being pushed rather than a healthy one being selected on.
And the new occupancy metrics need both arms to have survivors, which only 9 pairs did. **Next
condition: cap around 100, more seeds.** `occupied_elevation` and `occupied_slope` are computed from
creature positions and the environment field, so they needed no new simulation state at all.

**Half the first corpus had no hill in it,** and `CreatureSweep --relief` is what makes that readable: the
arena window is 0.1 radian on a coastal centre, and what lands in it ranges from 25 m of relief with
22 m of climb per traverse (seed 55) to a perfectly flat ocean floor (seed 161). 58 of 120 pairs were
byte-identical. Restricting to the 62 that diverged doubles every mean and **leaves every t
unchanged**, so the null is a finding rather than dilution.

**Note: `ConfigurationHashVersion` went 1 to 2**, per its own rule, because the covered field set
changed. It seeds the configuration hash, so **every V2 fingerprint shifts** - no recorded value was
found in the docs or pinned in a test, and V1 `ComputeStateHash` values are untouched.
