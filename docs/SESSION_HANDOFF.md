# Session Handoff — 2026-08-23

**Head at handoff: `c58b444`** (`revert: rivers, both attempts`), pushed to `origin/main`. Working
tree clean apart from the untracked Unity `.meta` files, `Assets/_Recovery/` and
`ProjectSettings/PackageManagerSettings.asset` that are **never to be staged**. **503 / 19 / 33 / 1
green**, Unity compile clean.

### Phase G — the join measured, a local climate band, and rivers rejected (2026-08-23)

| commit | what |
|---|---|
| `88a236b` | **the join moves no plant conclusion** - 480 runs - because it flattens the arena |
| `d7caa52` | that negative result, and why a 50 m window cannot see continental climate |
| `af43b76` | **terrain sets the regional climate, a local band supplies the variation** |
| `d0d2199` | rivers, attempt one - carved as a slot |
| `65e78ac` | rivers, attempt two - inscribed with a valley blend |
| `c58b444` | **both reverted.** Painted rivers cannot drain, erode or animate |

Earlier phases and their nineteen commits (`9442bd0` through `6b87771`) are in Phase F below.

**Read `docs/terrain-brainstorm-2-2026-08-23.md` and `docs/reference-implementations.md` before
touching terrain**, and `docs/terrain-caves-and-rivers.md` before adding to it. The first explains
why the original design was structurally wrong rather than under-tuned.

Two documentation commits sit between that and the previous handoff state (`c197061`): `d40f7ea`
rewrote this file, and the docs commit that follows `7343653` records the fingerprint work below.

Read this first, then `docs/CLAUDE_HANDOFF_2026-08-22.md` for architecture, scientific context,
testing rules and user preferences. `docs/ROADMAP.md` is the backlog. `docs/superpowers/plans/` is
an archive, not a backlog.

---

## 1. What was completed this session

Twenty commits, `f0a691d` through `c197061`. Three phases.

### Phase A — P4a feature work

| commit | what |
|---|---|
| `f0a691d` | key `R` home-range playtest; P5 panel hides routine continuity rows |
| `a817ccb` | home-range measured null for route formation |
| `173f3a3` | `ObservationRouteRing` scenario; home-range **closed as a measured negative** |
| `c528ced` | `ObservationShiftingPatches` scenario; map-turnover measurement |
| `6d64df0` | key `V` shifting-patches playtest at world seed 45 |
| `0b0387c` | founder mortality diagnosis: separation **sterilises**, does not kill |
| `15c7a5a` | inspector shows *why* a creature cannot breed; "ready to breed" count |

### Phase B — evidence-integrity audit (triggered by an external code review)

| commit | what |
|---|---|
| `4cc9a47` | **fix:** `PlantPatchStore.ReplaceAt` did not reset plant age |
| `9763374` | **fix:** statistics sampled before deaths committed; `CaptureStatistics()` added |
| `c97efe9` | paired old-vs-fixed blast-radius audit |
| `0fbb7f8` | `CaptureStatistics` guarded to a settled step boundary; corrected an overclaim |
| `1eb801c` | affected-evidence ledger widened to 9 docs; 168-site replication committed |
| `3f3b77c` | plant lifetime accounting **survives** the fix |
| `c19c1a5` | plant corpus revalidated on fixed code |
| `8f55b6e` | low-occupancy replication calibrated (occupancy is a cliff) |
| `06d80a8` | low-occupancy conclusions are **unverifiable** |
| `bbd7a76` | grazing deficit quantified |

### Phase C — P1 queue (all four items complete)

| commit | what |
|---|---|
| `c751a13` | finite/range validation at config and scenario boundaries |
| `b8a61a7` | mandatory experiment manifest + scenario layout fingerprint |
| `d3fac12` | P5 clustering allocation made linear in population |
| `c197061` | resource allocation benchmarked and **deliberately not optimised** |

### Phase D — state fingerprint V2 (the item the last handoff queued)

| commit | what |
|---|---|
| `7343653` | `ComputeStateFingerprint` (V2) + `SimulationConfig.ComputeConfigurationHash`; `BehaviorHash` extended with plant age/cooldown after measuring it changes no verdict |
| `32900de` | selected-creature action history — an outside observer, testable headlessly |
| `e6ce068` | safety-gated rendezvous reassessed at an operating point with survival headroom |

---

### Phase E — terrain (2026-08-23)

| commit | what |
|---|---|
| `94b2686` | terrain statistics instrument; baseline recorded |
| `e40eb7d` | planet was too cold and too wet to have biomes |
| `9ab9a2a` | icosphere planet, blended biomes, flat views centred on land |
| `e189ccf` | striped combs were lighting and z-fighting, not terrain |
| `02579d5` | **second brainstorm: the bounded 0..1 range was the defect** |
| `2cbedcb` | **signed elevation and plate blending, from the reference implementation** |
| `8da9b72` | live preview and offline capture unified onto one build path |
| `e6da13d` | planet-marked render: are the two views the same world? |
| `eead1b1` | creature-scale terrain; the arena now stands on the planet |
| `29fb83e` | the sea was a flat primitive plane |
| `6136493`, `c326d72` | terrain handoff rewritten; next-session decision recorded |

---

### Phase F — terrain tuning, the join, and a round world (2026-08-23)

| commit | what |
|---|---|
| `9442bd0` | tunables become `TerrainSettings`; creature-scale bands retuned; `J` panel |
| `3832b23` | `tools/TerrainProbe`; each window sampled at **its own** resolvable frequency |
| `9c1c6f2` | the retune recorded with its sweep |
| `c9b73d8` | every panel control describes itself |
| `d5d04b0`, `a111e6b` | flat views can be aimed anywhere; live biome readout |
| `ce71fcb`, `1e8e2df` | **the 82 degree wall: second-nearest plate changing hands** |
| `38fea7c` | caves and rivers: what they need, before the join fixed the shape |
| `8c82c77` | generation moves into `Simulation`, with no ambient settings |
| `6c35905` | **the join** - terrain drives the environment, behind a flag |
| `2e1f2af`, `ed7879b` | `O`: the arena drawn on the planet it is a window on |
| `96990b8`, `165cb8f`, `56ba489`, `354b9e9`, `b336b7d` | four camera/toggle bugs, all mine |
| `6b87771` | `SimulationWorld` and `DecisionSystem` split into partials |

---

## 2. Verified numeric results — do not re-derive these

### Home range (CLOSED — do not reopen, do not tune)

- `ObservationRouteRing` gives **90.6%** of creature-ticks a genuine equidistant choice at **0.88**
  mean familiarity, with an unsaturated off-arm route metric of **0.7955**.
- Flag on: route repeatability **fell −0.0345 (t −2.87, 8/30 up)**; same-site clinging **rose
  +0.0594 (t +4.93, 26/30 up)**.
- In shipped scenarios the route metric is saturated at **1.0000** flag-off; delta **+0.0000**, and
  **+0.0001** at a 10x bonus. The 10x arm cost **2.7%** food intake for no births.

### Shifting patches (`V`, world seed 45)

- ~**29** patch deaths and ~**33** establishments per 6,000-tick run; equilibrium **11.96** active
  food sites.
- Route permanence **−0.0935 (t −6.47)**; distinct routes per creature **+0.628 (t +4.48, 22/30)**;
  cross-kind legs unchanged (445 vs 441). **No survival cost.**
- Honest extinction rate **6/30**. Seed 42 dies; seed 45 chosen from the 24/30 that establish.

### The reproduction gate (DECIDED — keep as-is)

- `ReproductionSystem.CanReproduce` needs energy AND hydration AND health each **≥70%** of capacity.
- Adults satisfy it **95.0%** of adult-ticks with co-located resources, **56.8%** on the route ring,
  **33.5%** when food sits 7 units from water.
- Marginals collapse (energy above 0.7: 95.1% → 46.3%) **plus** a simultaneity penalty of **8.6–12.8
  points**.
- **Nothing starves or dehydrates**: 0 dehydration deaths, 0.07 starvations per run; all four
  founders die of **age** at tick ~2500 in every arm. Minimum hydration reached averages **0.445**.

### Plant lifetime accounting (CONFIRMED on fixed code)

Pre-fix → post-fix, same probe, version-independent detector:

- takeover fraction **0.3409 → 0.3471** (recorded 34%)
- median takeover lifetime **1.95 s → 1.95 s** (recorded ~2 s)
- R²(takeover, offspring) **0.5013 → 0.5164** (recorded 51.9%)
- R²(realised lifespan, offspring) among patches that **died of age**: **0.0039** (recorded 0.024 —
  same claim). Pooled with right-censored survivors it reads 0.14; **that is an artefact.**

### Plant corpus revalidation (120 seeds, varying founders, fixed code)

- `Dispersal` **+0.1119, t +15.63, 110/120** (recorded t +14→+19.6, 105–119/120) — confirmed
- `SeedInvestment` **+0.0872, t +7.10, 91/120** (recorded t +4.8→+6.8) — confirmed
- Establishment contest, paired on−off: **+0.0362, t +3.22, 72/120** (recorded t +4.03, 76/120) —
  **replicates**
- `SeedProductionRate` at 24 sites: **t −2.80, 43/120 up** — null, as recorded
- Survival: **0/120 extinct, 0/120 frozen** in all six combinations

### Occupancy is a cliff in target spacing

| spacing | occupancy | extinct |
|---|---|---|
| 4 | 0.833 | 0/10 |
| 8 | 0.528 | 0/10 |
| **9.5** | **0.311** | **0/10** |
| 11 | 0.085 | 3/10 |
| 13.3 | 0.023 | 9/10 |

`DispersalRange = 4 + 20 × Dispersal` and Dispersal evolves upward, so a mature patch throws seeds
14–24 units; any tighter lattice saturates. Viable window ≈ spacing 9.3–9.7, ~4% of the swept range.

### P1 measurements

- Genetic distance: **240 bytes/pair** before → 187 KB / 4.8 MB / **120 MB and 126 ms** at 40 / 200 /
  1,000 creatures. After: 4.3 KB / 21 KB / **104 KB and 50 ms**. **1,151x** less, **2.5x** faster.
- Resource allocation: cost is **O(requests × distinct resources)**, not O(requests²). 1,000 requests
  on 1 resource = **16.9 µs**; on 24 resources = **165 µs**. Full 12,000-tick runs: **0.012 /
  0.090 / 0.227 ms per tick** at peak populations 38 / 48 / 523.

---

### State fingerprint V2 (DONE — `7343653`)

Three hashes, three jobs, and they must stay separate:

| | job | includes configuration? |
|---|---|---|
| `ComputeStateHash` (V1) | frozen historical identifier; tests pin its literals | only `WorldSeed` |
| `ComputeStateFingerprint` (V2) | "will these two worlds evolve identically from here?" | **yes, all of it** |
| `ComputeBehaviorHash` | did this gene/flag reach behavior? | **no, and never** |

V2 adds, over V1: the config hash, `_birthOrdinal`, `_plantSeedOrdinal`, the three store id
counters, `PlantSiteRegistry` contents and order, plant `Age` and `ReproductionCooldownRemaining`,
and home-range state **unconditionally** rather than behind its flag. Guarded to a settled step
boundary, like `CaptureStatistics`. Excluded on purpose: reporting accumulators, liveness counters,
derived caches.

`BehaviorHash` also gained plant `Age` and `ReproductionCooldownRemaining` — decided by measurement,
not argument. Prediction stated first (the inert set would not move, since all four inert flags are
inert for a *reachability* reason); measured with the lines in and out: **identical inert set,
identical plant gene verdicts, 33 / 19 / 1 either way**. No `BehaviorHash` value is pinned as a
literal anywhere, so extending it invalidated no baseline.

Config hash covers **44 of 46** public `SimulationConfig` properties; `FixedDeltaTime` and
`MaximumMemorySlots` are derived from inputs already hashed. Two drift guards: every `bool`
constructor parameter must move the hash, and the property count is pinned.

**Green: 489 / 19 / 33 / 1**, up from 480 / 19 / 33 / 1. The three liveness counts being unchanged
*was* the acceptance criterion.

---

### Selected-creature history (DONE — `32900de`)

`CreatureActionHistory` records, for one creature at a time, a bounded list of action episodes plus
a lifetime budget of ticks per action. Each episode carries the needs it started and finished on,
which is the whole point: a `SeekFood` episode ending with **less** energy than it started is a
failed trip, and that is invisible from an instantaneous inspector reading.

**It lives outside `SimulationWorld` on purpose.** It samples the world; the world never reads it.
So it adds no simulation state, appears in no hash, and cannot change a tick. A per-creature history
held *inside* the world would be future-determining state by the letter of the fingerprint design
and would need re-arguing every time a fingerprint changed. Not config-flag-gated either — a
diagnostics flag has to be behavior-inert to be correct, and `FlagLivenessAnalysis` would then
report it inert and fail the known-inert-flag assertion. Same reasoning as `SimulationWorld.Liveness`.

Ten tests. The load-bearing one: an observed world and an unobserved world have **identical V2
fingerprints** after 400 ticks — the first real use of the fingerprint from `7343653`. Both that
test and the determinism test assert the observer actually recorded something, and a third asserts
the run produced more than one kind of episode, since a single unbroken `Wander` would satisfy
determinism while showing the player nothing.

Sampled once per simulated step, not per frame, so resolution is independent of frame rate and of
the speed multiplier. Drawn in its own panel at (464, 300) rather than lengthening the inspector,
which is already at full height with all optional trait rows showing.

---

### Safety-gated rendezvous (CLOSED — "works, buys nothing")

The 2026-08-21 null was partly unmeasurable: **all 240 of its runs ended at exactly 48**, the
population cap, zero variance. Its birth null stands; its survival null was a ceiling.

**The population cap is load-bearing ecology, not a guard rail.** Extinct 0/8 at cap 72, **5/8 at
84**, 8/8 at 96 and above, where runs boom to ~293 births and collapse on starvation. Cap 84 is the
only point where survival is free to move, so the rerun used it.

Re-measured, 120 paired seeds, cap 84 (`p4a-rendezvous-headroom-2026-08-22.md`):

| | delta | t | sign |
|---|---:|---:|---|
| flee rate per creature-tick | −0.0285 | **−5.07** | 80/120 down |
| **predation deaths** | **−2.275** | **−4.64** | 70/120 down |
| births, raw | +12.81 | +2.04 | 72/120 up |
| births per creature-tick | +0.00001 | +1.24 | not significant |
| births, both-survived seeds (n=28) | +11.71 | +1.01 | not significant |
| starvation deaths | +1.15 | +0.85 | null |

Extinction 75/120 vs 66/120 **does not survive pairing**: discordant 26 vs 17, McNemar χ² 1.49.
The raw birth gain is **exposure, not fertility**.

**Verdict: the mechanism works and the ecology declines to reward it.** Starvation, not predation,
limits this population. Flag stays default `false`. Do not build pack architecture to force an
effect; do not tune the gate. Reopen only in a predation-limited habitat — a scenario question, not
a mechanism question. **This is not the home-range case**: home range was closed for the wrong sign,
this for a right-signed effect that reaches no outcome that matters.

**Provenance:** the 2026-08-21 configuration could not be recovered — 81 candidates tried against its
recorded state hash and births, none matched. The rerun is a **new condition**, not a rerun, and its
CSV carries an `ExperimentManifest`.

---

### Terrain (2026-08-23) — measured, do not re-derive

**Scale, settled:** elevation 1.0 is about **30 metres**; one radian is **500 metres**; 1 unit = 1
metre. A slope value `s` in elevation-per-radian is a real grade of `s * 30 / 500`.

**The Voronoi step — the defect fifteen rounds of tuning missed.** Adjacent-sample elevation along a
meridian, 1-unit spacing:

| | median | p90 | max | ratio |
|---|---:|---:|---:|---:|
| nearest-plate only | 0.00093 | 0.00213 | **0.825** | **885** |
| blended across the seam | 0.00122 | 0.01463 | 0.0417 | 34 |

Every plate boundary was a vertical cliff in the field. That is what the terraces tracing closed
contours were.

**Boundary lift, per kind** (continental only, near minus far): subduction **+0.346**, continental
collision **+0.164**, divergent **−0.193**, transform **+0.050**, island arc never continental.
Measured only after breaking out by kind — averaging across kinds let rifts cancel collisions and
read as +0.09.

**Climate and biomes**, before → after:

| | before | after |
|---|---:|---:|
| ice (fraction of surface) | 0.234 | **0.074** |
| grassland | 0.025 | **0.139** |
| desert | absent | 0.007 |
| scrub | absent | 0.023 |
| temperature median | 0.310 | 0.648 |
| moisture minimum | 0.476 | 0.232 |
| moisture saturated at 1.0 | 0.0356 | 0.0039 |
| elevation pinned at 1.000 | 0.0081 | 0.0000 |

Land fraction **0.296–0.298** against a 0.30 target throughout.

**The whole palette exists; the view could not reach it (2026-08-23).** Reported as "never really
saw ice, just green and some sand and water" - which is an accurate description of latitude -15
degrees, and not a description of the generator. The same 400-unit window, walked along one meridian:

| latitude | biome mix |
|---:|---|
| **-15 (the shipped centre)** | Ocean 61.1%, **Grassland 34.9%**, Scrub 1.6%, Beach 1.2%, Marsh 0.8%, Desert 0.5% |
| +23 | Ocean 75.8%, Grassland 10.7%, **Scrub 7.5%**, **Desert 4.7%** |
| +40 | Ocean 42.1%, Grassland 18.2%, Scrub 14.5%, **Tundra 14.2%**, Desert 4.9%, **Ice 4.6%** |
| +57 | **Ice 41.5%**, Grassland 17.4%, Tundra 14.6%, Scrub 12.5%, Ocean 11.6% |
| +75 | **Ice 78.8%**, Tundra 13.6%, Scrub 3.5%, Grassland 2.7% |

All seven biomes appear. **A biome that exists globally and appears in no view is absent for every
purpose anyone has**, so the flat-view centre is now a control (`J`, View tab) with a live biome
readout, and the statistics instrument names biomes per window instead of counting them.

Land is also strongly asymmetric - the south pole window is 100% ocean and the north 99.3% land -
which is plate placement, not climate. High-latitude land is nearly flat: median land grade 0.015 at
+75 against 0.088 at the coast, because it is plate interior far from any boundary.

**Correction: ice is 0.116 of the surface, not the 0.074 recorded earlier.** Land is 0.241, so ice is
close to half of all land. Not caused by the creature-scale retune: the globe samples at 40.7
cycles/radian, so the local and micro bands are gated off there and cannot move a global count. The
earlier figure is stale against a later terrain commit.

**Flat-view windows** — the measurement that showed the views were parked in the wrong place:

| centre | land | biomes |
|---|---:|---:|
| origin (as shipped for days) | **0.001** | 2 |
| continental plate centre | **1.000** | **1** |
| coastline (current) | 0.451 / 0.514 / 0.503 | 6 / 4 / 3 |

**Resolvable frequency:** planet **13.3** cycles/radian (icosphere subdivision 5, ~167 triangles
around the equator); patch **120.6**. That 9x gap is real level of detail, not a defect.

**Creature-scale relief, retuned (2026-08-23).** Reported bumpy in the 200-unit view and acceptable
in the 400-unit one. The difference is which bands the view can resolve: patch resolution is fixed at
193 samples, so the resolvable frequency is **120.6** at 400 units and **241.2** at 200 units, and the
micro band at 150 cycles/radian switches on between them.

Both fine bands were **clipped by the slope ceiling rather than chosen**: `min(0.16, 6/55) = 0.109`
and `min(0.08, 6/150) = 0.040`, so two bands rode the ceiling and summed. Adjacent-sample grade over
land, in metres per metre:

| local / micro amplitude | 400u median | 200u median | 200u p90 | 50u median |
|---|---:|---:|---:|---:|
| 0.16 / 0.08 (was clipped to 0.109 / 0.040) | 0.169 | **0.243** | 0.611 | 0.283 |
| 0.060 / 0.020 | 0.113 | 0.155 | 0.388 | 0.208 |
| **0.036 / 0.012 (now)** | **0.088** | **0.119** | **0.306** | **0.172** |
| 0.024 / 0.008 | 0.077 | 0.103 | 0.278 | 0.156 |
| planet-scale bands only | 0.063 | 0.085 | 0.253 | 0.160 |

The chosen values are both **under** the ceiling, so they are now decisions rather than clamps.
Nothing else in the field moved: at these three views the new band fade evaluates to weight 1 or 0
exactly as the old `if` did, and the retuned numbers reproduce the sweep row for row.

**A hard band gate is a pop.** `if (maximumFrequency >= MicroFrequency)` gives a band **full**
amplitude the instant the camera crosses the threshold, so zooming changed the character of the
ground rather than its detail. Now faded across half an octave (`BandWeight`).

**The 82 degree wall (FIXED, `ce71fcb`).** Reported as "big cut offs" when jumping to another
continent. Not colour - geometry. At latitude 48.7 the field stepped **0.277 to 0.528 between samples
1.04 metres apart, a grade of 7.24**, with *identical* shelf and *identical* seam distance on both
sides, reading **Divergent** on one and **ContinentalCollision** on the other.

Cause: boundary kind and intensity belong to a **pair** of plates, so they change the instant a
different plate becomes second-nearest - along a line through the cell interior, far from any seam,
where the seam blend has already saturated to **1.000** and smooths nothing. Blending the shelf fixed
the seam and could never have fixed this.

Fix: carry **both** candidate neighbours and crossfade on how close they are to swapping
(`SwapTransition = 0.12` radians = 60 m). Where they change places their distances are equal, so both
sides evaluate the same half-and-half mixture.

| | before | after |
|---|---:|---:|
| worst step, lat 48.7 | **7.24** | **2.80** |
| max grade, lat 40.1 | 5.92 | 2.80 |
| max grade, lat 22.9 | 5.32 | 1.05 |
| medians and biome mix | — | unchanged to within a point |

**First hypothesis was WRONG and cost nothing because it was measured, not argued:** a suspected
off-by-one in the seam smoothstep (`Smooth01(0)` returning 0.5, which would have made every seam
discontinuous). It returns 0. The plate-state print on either side of the worst step named the real
cause in two minutes.

**Three scale errors, each worth remembering:**
- `MaximumSlope` 0.55 was a **3% grade** — wrong by 20x, and it crushed every band above ~10
  cycles/radian to centimetres. Now 6 (a 36% grade).
- Height scale was proportional to view width: 28 units per elevation unit at 400, **3.5 at 50** —
  the same ground **eight times flatter the closer you looked**. Now a constant 30.
- The hill band is a **77 m wavelength**, so **less than one hill spanned the 50 m arena**. Local
  (9 m) and micro (3 m) bands added.

---

### The join (DONE behind a flag — `6c35905`)

`terrainDrivenEnvironmentEnabled`, **default false, last optional constructor parameter**. On, the
simulation's moisture, temperature and elevation come from `PlanetTerrain` - the same function, seed,
window centre and detail limit the arena mesh is built from.

- **Flag-off is byte-identical.** The suite pins V1 state hashes as literals; 499 of them pass
  unchanged.
- **Detail limit is derived, not chosen:** the simulation samples at the frequency the 193-sample
  arena mesh resolves. Reading a sharper field would mean a creature climbing a bump nobody drew.
- **Output ranges deliberately held** where the plant systems were calibrated - moisture .15 to 1,
  temperature .20 to 1 before lapse, fertility .20 to 1, elevation 0 to 1 - so any difference the
  re-measure finds is the **shape** of the field changing, not its scale.
- Sea bed reads as elevation **zero**, not negative: elevation is a lapse-rate input, and ground below
  the waterline being warmer than the shore is not a claim worth making.
- Four tests (`TerrainDrivenEnvironmentTests`). The load-bearing one asserts the field's elevation
  **equals the generator's own sample at five spread positions** - one point agrees even with a wrong
  window centre or swapped axes. A manipulation check pins that the field actually varies.
- The **manifest drift guard fired on the first run**: a flag the manifest does not name makes a
  result irreproducible. That is the guard working, not a failure.

### A round world, without a spherical simulation (`2e1f2af`)

**`O`** draws the arena curved onto the planet it is a window on. **Presentation only** - positions
stay `SimVector2` on a flat 50-unit square with Euclidean distances, mapped by `ArenaProjection`
after the tick. No hash moves, no flag, nothing re-measured.

- The planet's centre sits at **(0, -500, 0)**, so the arena's centre lands on the origin with its
  normal up. There the mapping is the **identity**, which is why the camera rig kept working.
- **True scale was free.** The globe preview draws at radius 60 with relief fraction 0.06 - 3.6 units
  per elevation unit; at radius 500 the same fraction is **30**, exactly the arena's own figure. The
  two views were always the same shape at different sizes. **I predicted the mountains would shrink;
  they do not.**
- The patch is curved by remapping the flat builder's output, never by a second spherical builder.
- Ground heights are cached from the **flat** vertices, before curving: "how high is the ground here"
  is a question in simulation coordinates.

### File sizes (`6b87771`, and the presenter split before it)

| file | before | after |
|---|---:|---:|
| `Prototype1Presenter` | 1886 | **1033** + Hud 194, Terrain 536, Views 180 |
| `SimulationWorld` | 2058 | **844** + Ticking 643, Hashing 427, Statistics 193 |
| `DecisionSystem` | 1021 | **592** + Scoring 324, Legacy 130 |

Done **mechanically**: a scanner lifts whole members by brace depth, ignoring braces inside strings
and comments; nothing rewritten, no member changing class. The simulation splits are verified by 503
green tests including every pinned hash literal. **No file was read into context to do it** - the
script is in the session scratchpad and is worth promoting to `tools/` if it is wanted again.

---

## 3. Unresolved findings

### The three low-occupancy plant conclusions are UNVERIFIABLE

`p4-site-abundance-seed-production-rate-2026-08-20.md`,
`p4-low-occupancy-plant-route-audit-2026-08-20.md`,
`p4-low-occupancy-growth-trait-reaudit-2026-08-20.md` — banners stay.

Their scenario was never committed and cannot be recovered (no ZZZ probe was ever committed, so
none was ever deleted; the CSV and writeups give count, config, seeds, ticks and occupancy but never
coordinates). The calibrated replication reproduces occupancy **0.311** but grazes at
**0.00261 vs 0.00699 — a ratio of 0.373** — because its free-site pool sits outside the ±25 creature
arena and is never grazed. Placing 162 targets at non-saturating spacing *inside* the arena is
geometrically impossible.

Measured at that condition, for the record: `SeedProductionRate` **+0.00424, t +0.72, 64/120** (does
not replicate); `SeedlingResilience` contest-on−off **−0.00248, t −0.34, 53/120** (reversal not
demonstrated, but the +0.0362 advantage seen at 24 sites is **abolished**); the six growth-rate nulls
**hold**. `PlantEstablishmentContestEnabled` costs **19/120 extinctions** at low occupancy against
4/120 base.

**Do not attempt a fourth reconstruction.** If free-site abundance matters, re-derive it as a NEW
experiment with a committed scenario, in a geometry that fits inside the grazed arena.

### Lifespan-headroom claim: not adjudicated, control was confounded

`mortality-off` gives lifespan no channel but also removes site turnover and rewrites the regime:
the same comparison moved `Dispersal` **+0.0834 (t +21.40, 118/120)**, `NutrientUptake` **−0.0466
(t −7.62)**, `WaterEfficiency` **−0.0445 (t −8.61)**. Needs a lifespan-specific control that does
not exist.

### Terrain — open

- **The planet has adaptive level of detail** as of 2026-08-24 (`PlanetChunkedSurface`). See the
  section below. What is left of T6 is streaming cost, not detail.
- A small **stepped comb** remains on some steep ridges.
- **Ice cover looks high** at 0.074 of surface.
- **Terrain is cosmetic.** Creatures are drawn on relief; a hill costs them nothing.

### Terrain — hypotheses tried and REFUTED, do not retry

Each of these was a confident diagnosis of the striped combs, and each changed nothing visible:

1. **Elevation clamping producing mesas** — fixed (0.81% pinned at 1.0 → 0.00%), render identical.
2. **Mesh-edge tapering** — the combs were not at the patch boundary.
3. **Vertex jitter** — made it visibly worse (shredded slivers at 0.75 cell); reverted, then deleted.
4. **Boundary landform width** — widened 3x, no change.
5. **Ambient light** — set and probe regenerated, no change.
6. **Self-shadowing** — shadows disabled, no change.
7. **"It is just level of detail"** — refuted by the planet-marked render: the two views were showing
   **different parts of the planet**.

The test that actually worked: **render the same mesh unlit.** The stripes vanished completely,
which separated shading from geometry in one image. It should have been first, not seventh.

### The spherical view — partly unverified, and known imperfect

- **`PatchLift` is 0.02 units and is probably far too small.** The backdrop globe is subdivision 5 -
  about **19-unit triangle edges at true scale**, roughly seven triangles under the whole 50-unit
  arena - and it samples elevation at ~13 cycles/radian against the patch's **965**. The two surfaces
  can disagree by more than 0.02, in which case the coarse globe pokes through the fine patch. Not
  seen either way; fix is a larger lift or a slightly smaller backdrop radius.
- The user reports it as "**a bit buggy**" without specifics. Not diagnosed.
- The pan clamp was a **box in x/z** and fought at planet distance. **Fixed by deletion** - the free
  camera has no focus to clamp.
- **Four bugs shipped in a row on this feature**, all from wiring behaviour into paths not read
  carefully: the terrain path most scenarios never take (`96990b8`), a `Camera.main` lookup that never
  resolves (`165cb8f`), no yaw at all (`56ba489`), and a focus that zooming never re-clamped
  (`b336b7d`). **Presentation has no headless check that means anything** - a human in Play mode is
  the only instrument.

### `GeneticClusterHistory` is split (2026-08-24)

1324 lines became 313 + 555 + 439 + 65, as partials:

| file | what |
|---|---|
| `GeneticClusterHistory.cs` | fields, `Record`, `ProcessTransition`, segment and track state |
| `.Events.cs` | emitting, holding back pending evidence, writing unresolved |
| `.Graph.cs` | **every method static, reads no field** - relations, strong components, ancestry |
| `.Pending.cs` | the two nested evidence classes |

**The previous note here was wrong and cost two sessions of not doing this.** It said the members do
not sit at class indent and the bulk is nested types, so a mechanical pass finds nothing. The nested
types are the last 55 lines and every member sits at class indent. `.Graph.cs` was the real seam:
nothing in it can touch a history's state.

563 tests green, Unity compile clean, no behaviour change.

### Unverified by me

The breeding-readiness inspector UI (`15c7a5a`) compiles and passes tests but was **never seen in
Play mode**. Layout at 324px with all optional trait rows showing is untested. The same applies to
the selected-creature history panel (`32900de`): its *model* is covered by ten headless tests, but
the panel itself has never been seen rendered. It was placed at (464, 300) in free space beside the
population-condition box specifically to avoid stacking more onto the untested inspector.

### Not measured

Per-tick resource request counts were never instrumented. The do-not-optimise decision uses
population as an upper bound — sound for that decision, but it is a bound, not an attribution.

---

## 4. Decisions that must NOT be reopened

1. **Soft home-range affinity is closed as a measured negative.** Flag stays default `false`; code,
   tests and key `R` stay; spec and plan carry SUPERSEDED banners. Do not tune
   `DefaultHomeRangeBonusMaximum`, the falloff distance or the learning fraction — the **sign** of
   the effect is wrong, not its size.
2. **The joint 70%/70% reproduction gate stays** (user decision). Reduced fertility while commuting
   is accepted as real ecology. Do not change `ReproductionSystem.CanReproduce`. No re-baseline is
   needed and every result on record stands. Separated scenarios must be calibrated to be viable
   *under* the gate.
3. **Resource allocation is not to be optimised** at current scales. Revisit only if populations in
   the thousands and site counts in the hundreds coincide.
4. **`ObservationShiftingPatches` needs no further placement or productivity calibration** — six
   variants are recorded and the joint gate explains why all failed.
5. **Do not build the P4a juvenile local-area bias as a fix for separated-resource extinction.**
   Juveniles are not the failing class and mortality is not the failure mode.
6. **Place memory stays inert.** Never wire `MemorySystem.ObservePlace`.
7. **Do not use the competition-off arm as a drift control.** It disables no trait.
8. **Elevation is SIGNED DISPLACEMENT from sea level, never a bounded 0..1 field.** A bounded range
   forces a clamp, a clamp forces a knee, and an interior sea level forces a branch at the waterline
   - three slope discontinuities, and a terrace is a slope discontinuity. Do not reintroduce
   `SoftSaturate`, `Clamp01` on composed elevation, or a `SeaLevel` constant inside the field.
9. **Plate properties must be blended between the two nearest plates.** A Voronoi lookup is
   piecewise constant; taking only the nearest plate puts a measured 0.825 step in the field.
10. **`TerrainMeshBuilder` is the single mesh/material/lighting path.** The capture and the runtime
    must not build scenes separately again - they drifted, and the PNGs became evidence about a mesh
    nobody was looking at.
11. **Measure before changing a terrain coefficient.** Reasoning about the field produced six wrong
    diagnoses; the instruments produced every answer.
12. **A band amplitude must sit under the slope ceiling, not on it.** `SlopeLimited` is a safety
    ceiling. Two bands both clipped to it sum to relief no ground has - measured median land grade
    0.243 against 0.085 for the planet bands alone. If a chosen amplitude is above the ceiling, the
    number in the source is fiction.
13. **No ambient settings in `Simulation`.** `PlanetTerrain.Sample` and friends **require** a
    `TerrainSettings` argument. While generation lived in Presentation a mutable static was the right
    trade for a slider panel; in Simulation it would be behaviour-changing state outside
    `SimulationConfig`, invisible to the configuration hash, so two worlds with equal hashes could
    diverge. The viewer's mutable instance lives in `Presentation.TerrainView` and can only affect
    what is drawn. **Making terrain tunable per world means putting the values in the config and
    hashing them** - a deliberate later step, not a convenience.
14. **The boundary landform must be crossfaded between BOTH candidate neighbours.** Kind and
    intensity belong to a pair of plates, so a rank swap is a discontinuity even where the seam blend
    has saturated. This is the same rule as decision 9, one layer down: **ranking is a lookup.**
15. **The simulation stays 2D; a round world is a display transform.** Positions are `SimVector2` on
    a flat 50-unit square with Euclidean distances. `ArenaProjection` maps them onto the sphere after
    the tick. Do not "fix" this by making the spatial model spherical without deciding to pay for it:
    distance stops being Euclidean, the perception grid has no drop-in equivalent, and **every
    recorded distance changes meaning**.
16. **Splits are mechanical or they do not happen.** Members are lifted whole by brace depth, nothing
    rewritten, no member changing class - so a split cannot alter behaviour and needs no reading. A
    split that requires understanding the file is a different, larger job.
17. **Terrain tunables live in `TerrainSettings`, not in `const` fields.** They are judged by eye
    against a one-metre creature at three zoom levels; that judgement cannot be made from source, and
    an edit-and-reload loop is why the previous round took fifteen passes. `PlanetTerrain.Active` is
    deliberately mutable global state - there is one terrain - and the explicit parameter on
    `Sample` exists so probes and tests can sweep without touching it.
12. **Safety-gated rendezvous is closed as "works, buys nothing."** Flag stays default `false`. Its
   effect is real and correctly signed; the ecology is starvation-limited, so it does not propagate.
   No pack architecture, no tuning. Reopen only in a predation-limited habitat.
9. **The three hashes stay three hashes.** V1 stays frozen and incomplete; V2 carries configuration;
   `BehaviorHash` never carries configuration. Merging any two of them breaks either a recorded
   baseline or `FlagLivenessAnalysis`, which would then report every flag as live.

---

## 5. Next task

**Step three of the join is DONE and its answer is negative (`88a236b`).** 480 runs, 120 seeds,
12,000 ticks, flat versus terrain-driven crossed with the establishment contest. Full writeup in
`docs/experiments/p4-terrain-join-2026-08-23.md`; the instrument is `tools/PlantSweep`, committed.

**No plant conclusion moves.** Paired terrain-minus-flat per seed, contest-on: `WaterEfficiency`
-0.0233 (t -2.72) is the only cell past |t| = 2 in twenty-two comparisons and is not claimed;
everything else sits under |t| = 1.9. Extinctions are uncorrelated between fields (2 both, 12
terrain-only, 6 flat-only, 100 neither).

**The reason is the real finding: the join makes the arena MORE uniform, not less.** Across 1,681
positions in the +/-25 arena, terrain moisture has sd **0.050 / 0.037 / 0.005** at seeds 42 / 71 /
161 against the flat field's **0.240 / 0.240 / 0.283**; temperature sd **0.014** against 0.189 at
seed 161. The arena is 50 units wide = **0.1 radian** on a 500-unit planet, and terrain climate
varies on continental scales. Terrain also runs systematically warmer: mean temperature 0.75-0.79
against 0.39-0.43.

**The flag is left OFF everywhere, including the terrain playtest.** Turning it on would flatten
that scenario's climate heatmap and buy nothing. This is a deliberate deviation from the queued
instruction, which assumed the join would add spatial structure.

**`plantTemperatureAdaptationEnabled` was already out of `KnownInertFlags`** - removed when the
procedural fields landed, with the reason recorded in the list's comment. The queued step three
predicted that failure; it had already happened.

### The decision was taken: a local band, not a wider unit (`docs/experiments/p4-terrain-local-band-2026-08-23.md`)

Of the two options, **option 2** is implemented: `SampleTerrain` treats terrain climate as the
regional **mean** and adds a zero-centred local band to moisture and temperature at the procedural
field's own noise scale. Option 1 - widening metres-per-unit - was **rejected**, because it changes
what every recorded distance means (decision 15).

- **Spread restored:** moisture sd **0.207 / 0.192 / 0.178** at seeds 42 / 71 / 161, against the
  procedural field's 0.240 / 0.240 / 0.283 and the band-less terrain's 0.050 / 0.037 / **0.005**.
  Temperature 0.154 / 0.134 / 0.108 against 0.182 / 0.201 / 0.189 - lower because the window sits at
  a warm latitude and the upward half of the band clips.
- **Strengths are `LocalMoistureStrength = .40`, `LocalTemperatureStrength = .32`**, chosen to match
  the procedural spread, not to maximise it. Raising them does not add variety, it deletes the
  regional signal.
- **Elevation is untouched** and still equals the generator's own sample exactly.
- **No new config flag**, because the band sits inside the already-gated terrain path. Flag-off is
  still byte-identical: **503 / 19 / 33 / 1 green**, Unity compile clean.
- **Re-measured, 480 more runs: still no plant conclusion moves.** Paired terrain-minus-flat,
  contest-on: `WaterEfficiency` -0.0221 (t -2.33) and `NutrientUptake` +0.0277 (t +2.06) are the
  only cells past |t| = 2 in twenty-two comparisons; neither is claimed. **This null now means
  something** - the field carries comparable structure and selection is indifferent to its source.
- **`NutrientUptake` is the one to watch** if anything is followed up: its selection weakens under
  terrain (contest-on flat -0.0536, t -5.39; terrain -0.0259, t -2.37), consistent with fertility
  being the channel whose shape moved most.
- **`p6-terrain-playtest` now sets `terrainDrivenEnvironmentEnabled: true`.** Only safe because the
  band exists - without it that scenario's temperature heatmap would be one flat colour. **No
  experiment configuration is touched**; `CreateFullEcosystemDefaults` still has the flag off, so
  every recorded baseline stands.

### Rivers: built twice, rejected, and REVERTED (`d0d2199` and `65e78ac`, reverted 2026-08-23)

**Head carries no river code.** Both attempts are reverted; the terrain is exactly what it was before
them. The postmortem is in `docs/terrain-caves-and-rivers.md` and is the authoritative account.

**Why it was rejected, in one line: painted rivers do not drain, cannot erode, and cannot animate.**
The generator is a pure function of one direction, so it reads nothing about a point's neighbours -
and drainage, erosion and a flowing surface are all relationships *between* neighbours. No amount of
profile shaping fixes that.

**What the two attempts cost, and what they bought.** Attempt one carved a fixed 5 m slot and read as
a stripe. Attempt two fixed every named artefact - monotone profile, compactly supported valley blend
after Peytavie et al., cut-only modifier, no flooding land to sea, gradient plus momentum instead of
eight compass points, a tributary pass, tapered heads - and the verdict was still that it does not
look like a river. **That is the finding worth keeping:** the fixes were correct individually and the
approach was wrong.

Numbers, since they cost real compute: seed 42 produced 58 courses and 2,005 segments in 1.1 s; a
snapped 50-unit arena window held 34.5% valley and 5.8% open water with a deepest cut of 4.1 m. The
480-run plant sweep over that field moved nothing conclusive - `MoistureTolerance` +0.0402 (t +2.57)
and `WaterEfficiency` -0.0189 (t -2.10) were the only cells past |t| = 2 in twenty-two comparisons.

**Next on this thread, and it is a prerequisite chain, not a menu:**

1. **Region/chunk system.** A finite grid at fixed resolution, cached per region.
2. **Depression filling and D8 flow accumulation** on it - real drainage, branching, discharge.
3. **Erosion** - valleys form around rivers because water dug them.
4. **A water surface mesh** with flow vectors, which is the only way rivers animate.

**Do not attempt rivers again before step 1 exists.**

### The planet has level of detail (`PlanetChunkedSurface`, 2026-08-24)

**Twenty icosahedron faces, each the root of a quadtree, split where the camera is.** The single
subdivision-5 icosphere is gone from the runtime - it survives only in the offline capture, as the
"before" half of a comparison. Depth caps at 6, which is a 0.54 m triangle: finer than a creature.

**The detail is real.** Each chunk band-limits its elevation to its own grid
(`PlanetChunkLod.DetailLevelFor` = depth + 4 icosphere subdivisions), so splitting adds octaves
rather than re-drawing a smooth surface with more triangles. Sampling past the grid is what turned
the globe into static originally, so the limit is derived per chunk, never chosen.

**Measured, from the capture at 20 m altitude: 908 chunks, depths 1 to 6** - roughly 230k triangles
and, more to the point, **908 renderers**. That draw-call count is the open cost question; nothing
has profiled it in Play mode yet. Halving it is one line (`MaximumDepth` 5).

**Seams.** Neighbouring depths disagree along their shared edge because they band-limit differently.
`PlanetChunkSeamTests` measures it rather than leaving it to the eye: **worst case ~0.04 of a chunk
edge** at every level - 0.285 m at depth 6, 3.8 m at depth 1. Skirts are sized at 0.05 of the edge,
which clears it everywhere. Removing the disagreement entirely needs geomorphing, which is a separate
piece of work and is not queued.

**Chunks fully underneath the arena are dropped** (`HiddenByArena`), because the arena is drawn
separately at a finer resolution. The ring straddling its border is still drawn, so there is no hole
beside the patch edge.

**`PatchLift` is no longer suspect, and this is measured.** The patch samples about 12,000 around the
equator and the deepest chunk about 5,300 - and they produce **identical elevation**, because the
octave cap is reached before either band limit binds. The worry was real against the old
subdivision-5 backdrop, which sampled 166. `PlanetChunkSeamTests` guards it: if the octave cap is
ever raised, the patch gains detail the backdrop lacks and the test fails.

**A seam is visible where two depths meet**, reported from Play mode. Removing it needs neighbour-aware
morphing, which is a real piece of work and is not queued. **One cheap idea was tried and measured
worse** - see `Segments`.

### The developer camera is now free-fly (`FreeFlyCameraController`, 2026-08-24)

**The orbit rig is deleted.** `GroundPlaneCameraController` is gone; the camera is a position and a
direction, and the whole class of bug that came from clamping a focus point to a box in x and z is
gone with it. There is no focus, no orbit, and no handover between two framing rules - the three
things the previous camera's four shipped bugs came out of.

**Controls.** Hold the **right mouse button** to fly: mouse looks, **WASD** moves along the view
axes, **Q/E** down and up along the local vertical, **shift** boosts five times, **alt** slows to a
fifth, the **wheel** sets the speed dial. With the button up the camera is inert, which is what makes
this possible at all - `D`, `E` and `F` are scenario hotkeys, and a camera that swallowed them would
have cost more than it gained. **Arrow keys** move without the button, the **wheel alone** dollies,
and **Home** frames the arena. The HUD legend lists all of it.

**Speed is proportional to height above the surface**, clamped below by a floor of 1.8 units/second
and above by the extent of what is on screen. That single rule is why one camera works beside a
1-unit creature and around a 500-unit planet: the height above the ground *is* the scale the viewer
is working at.

**Two rules, never blended.** When `SetExtent` is given a surface radius, height is measured from the
sphere and up points away from its centre - so flying to the far side stays upright. With radius zero
the world is a plane and height is `y`. The arena and every preview use the plane; the curved arena
uses the sphere. There is deliberately **no interpolation between them**, because a handover between
two framing rules is exactly what produced `b336b7d`.

**It finally has an instrument.** `FreeCameraMotion` holds the arithmetic - `SpeedAt`,
`ClampAltitude`, `ClampPitch`, `AdjustDial` - with no Unity types in it, and is compiled into
`tools/HeadlessTests` by an explicit `<Compile Include>` alongside nine tests in
`FreeCameraMotionTests`. It is the only Presentation file in that project and the comment there says
why. `LifeSimulation.EditModeTests` also references `LifeSimulation.Unity` now.

**Not verified visually.** Camera behaviour only exists in Play mode and there is no way to drive
synthetic input headlessly, so the compile is clean and the arithmetic is tested but nobody has flown
it yet. **First thing to do next session: press Play, fly, and report.** Things most likely to be
wrong are feel rather than logic - look sensitivity (4.5 degrees per unit of mouse movement), the
dolly notch, and whether the speed floor is slow enough beside a creature.

### Also open, smaller

- ~~`PatchLift` needs raising~~ - **closed 2026-08-24, measured.** The backdrop and the patch agree
  exactly now that the backdrop has level of detail. See the planet section above.
- **Rivers are not on this list.** They were built, rejected and reverted; see the rivers section
  above. Nothing about them is to be attempted before the region/chunk system exists.
- Ice is heavy at high latitude; **Altitude cooling** on the `J` panel is the control. Judge it in the
  view now that the view can be aimed.

**Use `ComputeStateFingerprint()` for "do these two worlds evolve identically" questions.** Never
`ComputeStateHash` — V1 is a frozen historical identifier and is deliberately incomplete. Never
recompute or overwrite a recorded V1 value.

**Use `ExperimentManifest` + `ExperimentCsv` for every new experiment CSV.** `ExperimentCsv.Compose`
refuses without provenance; that is deliberate.

---

## 6. Test commands

From `tools/HeadlessTests`:

```powershell
dotnet build
dotnet test --no-build --filter "FullyQualifiedName!~LivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~PlantLivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~LivenessTests&FullyQualifiedName!~RiskAversionIsLiveOnlyWhenThreatsExist"
dotnet test --no-build --filter "FullyQualifiedName~RiskAversionIsLiveOnlyWhenThreatsExist"
```

**Green at handoff: 503 / 19 / 33 / 1.** RiskAversion alone takes ~16 s; silence is not a hang.

Presentation changes additionally need a Unity compile — the headless project excludes
`Assets/Scripts/Presentation`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -logFile '.\Logs\compile.log'
```

Then check `grep -c "error CS"` on the log and confirm `Exiting batchmode successfully`.

---

### Terrain instruments

Unity menu, or headless `-executeMethod`:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -executeMethod LifeSimulation.EditorTools.TerrainStatisticsEntry.Dump -logFile '.\Logs\stats.log'
```

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.2.14f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim' -executeMethod LifeSimulation.EditorTools.TerrainRenderEntry.Render -logFile '.\Logs\render.log'
```

Statistics land in `Logs/terrain-statistics.txt`; PNGs in `Logs/terrain/`. **The render needs
graphics — `-nographics` disables it.** Both fail while the Unity editor holds the project lock;
either close the editor or run the menu items.

**Neither needs Unity closed if the question is about the field itself:**

```powershell
dotnet run --project tools\TerrainProbe -c Release
```

The probe lives at `tools/TerrainProbe` and compiles the generator directly - it is pure C#, which
is why it works without Unity - reporting **grade** (median, p90, max), **named biome mix**, and the
**worst single step with the plate state on either side of it**. That last one is what named the
82-degree wall; a median cannot see a wall.

That compiles `PlanetTerrain`, `PlateStructure` and `TerrainSettings` directly - they are pure C# -
and prints the adjacent-sample grade for each flat view at **its own** resolvable frequency, with and
without the creature-scale bands.

**A field statistic cannot see a rendering defect and a render cannot see a field discontinuity.**
Both exist because each missed something the other caught.

---

## 7. Play-mode keys

`Space` pause · `1`/`2`/`4`/`8` speed · `H` overlay · left-click select, drag resources.

Scenarios: **`V` shifting patches (best — the map changes as you watch)** · `5` stable · `6` scarcity
· `7` migration · `9` mating · `E` starter habitat · `R` home range (looks identical to `5` — that
is the measured result, not a bug) · `N` all-flags playtest · `B`/`D`/`F`/`P`/`C`/`T`/`G`/`M` older
demos.

---

## 8. Working-tree rules

Intentionally untracked: Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. **Never stage or delete them. Never `git add -A`** —
add named files only. Delete `Assets/Tests/EditMode/ZZZ*.cs` probes before committing; none exist at
handoff.

---

## 9. Deferred work — what to pick up after terrain

Written at the point of switching to P6 terrain groundwork. **The tree was clean at this point:**
nothing uncommitted, no `ZZZ*` probes, no `TODO`/`FIXME`/`NotImplementedException` anywhere in
`Assets/Scripts`, nothing unpushed, 499 / 19 / 33 / 1 green, Unity compile clean. Nothing below is
half-built — these are *unstarted* items, not loose ends.

### A. Small, finishable in a session each

1. **Resource depletion/recovery feedback** (P4a visible-feedback item). A player cannot currently
   see a patch being grazed down and regrowing. Presentation-side.
2. **Lineage display** (same roadmap line). Parents are shown as bare ids in the inspector;
   generation and descent are not visible.
3. **Two-parent reproduction** — named on the rendezvous roadmap line and never built. Note the
   rendezvous *gate* is closed as "works, buys nothing"; two-parent reproduction is a separate,
   still-open idea and should not be justified by the gate.

### B. Needs a human in Play mode — I cannot verify these headlessly

4. **Breeding-readiness inspector** (`15c7a5a`) and **selected-creature history panel** (`32900de`).
   Both compile and are covered by tests at the model level; **neither has ever been seen rendered.**
   The inspector is at full height (324px) with all optional trait rows showing, which is why the
   history went in its own panel at (464, 300) rather than lengthening it. Someone should press `V`,
   click a creature, and confirm both panels look right and do not overlap.

### C. Measurement debts — real, and each has a stated reason it is still open

5. **A carrying-capacity-limited habitat.** This is the one that matters most. Every plant trait
   result on record was measured with the herbivore population **pinned at 48**. Raising the cap does
   **not** free it — it produces boom-and-collapse (`p4-cap-pinning-audit-2026-08-22.md`). Until a
   habitat limited by carrying capacity rather than by a cap exists, the whole plant corpus carries a
   scope qualification. This is scenario design, not an audit.
6. **Lifespan headroom: not adjudicated.** The available control (`mortality-off`) is confounded —
   it also moved `Dispersal` +0.0834 (t +21.40), `NutrientUptake` −0.0466 and `WaterEfficiency`
   −0.0445. Needs a lifespan-specific control that does not exist.
7. **Per-tick resource request counts were never instrumented.** The do-not-optimise decision uses
   population as an upper bound — sound for declining to optimise, but it is a bound, not an
   attribution. Only needed if someone wants allocation reported as a *share* of tick time.

### D. Closed — do not reopen, and do not spend a session rediscovering why

8. **Soft home-range affinity** — closed as a measured negative. The **sign** is wrong, not the size.
9. **Safety-gated rendezvous** — closed as "works, buys nothing". Right sign, well powered
   (predation deaths t −4.64), but starvation limits the population. Reopen only in a
   predation-limited habitat.
10. **The three low-occupancy plant conclusions** — permanently unverifiable, banners stay. **Do not
    attempt a fourth reconstruction.**
11. **Place memory stays inert.** Never wire `MemorySystem.ObservePlace`.

### D2. Caves and rivers — asked for, and blocked on architecture

**Read `docs/terrain-caves-and-rivers.md`.** Both were requested; neither is a coefficient.

- **`PlanetTerrain.Sample` is a pure function of one direction.** Rivers and erosion depend on the
  ground *uphill*, which is a computation over the surface, not at a point. They need a pass over a
  finite region, which is the same chunk machinery adaptive level of detail needs.
- **A heightfield stores one surface per direction**, so it cannot express an overhang at any setting.
  Caves need a volumetric density field and marching cubes, with density seeded as
  `surfaceElevation - height` so the zero isosurface reproduces exactly the terrain that exists now.

Order: the join, then painted rivers (additive, cheap, no new machinery), then chunked regions, then
flow accumulation and erosion on that machinery, then caves. **Keep generation a pure function of
position and settings** - the probe, the statistics instrument and headless determinism all depend on
it, and it is why terrain could be iterated fifteen times without re-measuring anything.

### E. Large

12. **P5 — species and history.** Genetic distance, lineage ids, cluster history and the P5 panel
    already exist. Species clustering proper, the evolutionary tree, extinct branches and the
    historical timeline do not. This is the big remaining phase.

### Terrain-specific note

When terrain makes temperature genuinely vary, **`LivenessTests` will fail on
`plantTemperatureAdaptationEnabled`**. That flag is pinned inert *only* because `EnvironmentField`
returns Temperature = 1.0 everywhere and the adaptation expression collapses to the raw value at 1.0.
The failure is the **designed signal** — the same thing happened when the procedural fields landed.
Move the flag out of `LivenessTests.KnownInertFlags`; do not "fix" the test.

---

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

**Half the corpus had no hill in it,** and `CreatureSweep --relief` is what makes that readable: the
arena window is 0.1 radian on a coastal centre, and what lands in it ranges from 25 m of relief with
22 m of climb per traverse (seed 55) to a perfectly flat ocean floor (seed 161). 58 of 120 pairs were
byte-identical. Restricting to the 62 that diverged doubles every mean and **leaves every t
unchanged**, so the null is a finding rather than dilution.

**Note: `ConfigurationHashVersion` went 1 to 2**, per its own rule, because the covered field set
changed. It seeds the configuration hash, so **every V2 fingerprint shifts** - no recorded value was
found in the docs or pinned in a test, and V1 `ComputeStateHash` values are untouched.

