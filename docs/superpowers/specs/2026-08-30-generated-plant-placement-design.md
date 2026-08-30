# Generated plant placement — design

**Date:** 2026-08-30
**Status:** BUILT AND MEASURED — see `docs/experiments/p6-generated-plant-placement-2026-08-30.md`
for the results, which are what to read. **Two things in this document were refuted by its own
pilot** and are marked below: the "six locations" premise in section 1, and the assumption in section
5 step 2 that water splitting would be the *remedy* if water bound the herd. The mechanism exists
behind `generatedPlantSitesEnabled`, default false, and is **not switched on for `Y`**.
**Owning spec:** `docs/superpowers/specs/2026-08-14-system-integration-design.md`, whose
`ResourceState` table is the work.

## 1. The problem, stated as the thing on screen

Creatures pile up. In `Y` (`Prototype4Scenarios.ConsumerDefenseCalibrationModerate`, cap 96) the
herd reads as a heap of animals standing on each other. Three levers were measured against it and
all three failed:

| lever | mean nearest-neighbour | verdict |
|---|---|---|
| feed in place (movement) | 0.705 to 0.824 | real, small, shipped |
| four times the world area | 0.945 at best | costs establishment |
| four times the feeding radius | no effect at all | rejected |

**All three held the number of food locations at six.** That is the remaining variable, and it is
the one this design moves.

> **REFUTED BY THE PILOT.** Unmodified `Y` at tick 12,000 has **23.2 of 24 food sites active** — the
> plants colonise nearly every dormant coordinate. Six is the count at tick 0. The herd piles up in a
> world that already has twenty-three food locations, so "six locations" was never the mechanism.

## 2. What already exists — not rebuilt

- Plants are real: `PlantPatchState` with biomass, capacity, growth rate, nutrition, defence, a
  heritable `PlantGenome`, lineage, age. Growth, mortality, seeding, dispersal range, distance
  falloff on establishment, and a competition contest all exist and are tested.
- **Each plant owns the `Food` resource creatures eat** (`PlantPatchState.foodResourceId`). Food
  sites *are* plants. The behaviour layer needs no change, exactly as the integration design says.
- `EnvironmentField.Sample` returns moisture, **fertility**, temperature and elevation at any
  position, terrain-driven in `Y`. Fertility is already read — as the binding term in the growth
  *limit* (`PlantGrowthSystem`, `Min(moisture, fertility, temperature)`).

## 3. What is actually missing

`PlantSiteRegistry` is a fixed list of resource indices, filled by `SimulationScenario.ApplyTo`
from hand-typed `ResourceDefinition` coordinates. `Y` carries **6 active food sites and 18 dormant
ones, all literals**, and `PlantReproductionSystem.FindSite` samples that list and nothing else.
So a plant may only ever exist at a coordinate a human typed.

Against the integration design's table:

| `ResourceState` field | Today | Target |
|---|---|---|
| `Position` | authored by scenario | where a plant established, on ground the fertility field allows |
| `Capacity` | authored constant | set by local fertility |
| `RegenerationPerSecond` | authored constant | set by local fertility and moisture |
| `NutritionMultiplier` | plant genome | **already done** |

## 4. The proposed change — what it is, and what it deliberately is not

### 4.1 Not this

**No soft attraction, no distance falloff toward good ground, no pull on creature movement.** That
is handoff section 4 decision 1, closed as a measured negative with the **sign** of the effect
wrong. Nothing here touches creature scoring, movement or perception. The only thing that moves is
where a *plant* is allowed to be.

### 4.2 The mechanism

1. **`PlantSiteGenerator`** — pure, deterministic in `(worldSeed, bounds, spacing, threshold)`,
   takes an `EnvironmentField`, emits `ResourceDefinition[]` of dormant `Food` sites:
   - a **jittered lattice** over the arena bounds, spacing an explicit parameter. Not Poisson-disc:
     the 168-site replication measured occupancy to be a *cliff* in spacing (0.833 at 4, 0.311 at
     9.5, ecosystem collapse at 13.3), so spacing must be a knob read off the source, not an
     emergent property of a sampler.
   - **acceptance by fertility**: keep a candidate when `Sample(position).Fertility >= threshold`.
     Fertility is bounded `.20 .. 1`, so the threshold is a real filter and its *rejection rate* is
     a number the pilot has to report.
   - `Capacity` and `RegenerationPerSecond` scaled from the site's own fertility (and moisture for
     regeneration), around the authored constants, so total arena productivity is a stated quantity
     rather than an accident.
2. **A `SimulationScenario` transform**, not a hand-written scenario.
   `WithGeneratedFoodSites(id, field, spacing, threshold)` copies **every** definition — water,
   founder placement, plant genomes, the existing active sites — and appends the generated dormant
   ones. A scenario is not its visible resources; the tiled-habitat probe already paid for that.
3. **A flag**, `generatedPlantSitesEnabled`, default `false`, switched on for `Y` only if the pilot
   earns it. Every recorded ecology number stays comparable.

### 4.3 The confound this design names up front

In `Y`, **water is co-located with food at the same six coordinates**. Spreading plants without
spreading water leaves creatures tethered to six drinking points. Generated placement is a *plant*
feature and water is not a plant, so if water binds the herd, generated placement cannot fix
clumping on its own. **The pilot tests this rather than assuming either way.**

## 5. The pilot — measured before anything is built

Every arm runs `Y`'s exact configuration (cap 96, 4 founders, terrain join, slope cost, terrain
temperature, health recovery, wander hysteresis, feed-in-place), 12,000 ticks, seeds 42-47.

**Control arm in every run.** Factor 1 / split 1 must be **layout-fingerprint-identical** to `Y`
and must reproduce the recorded numbers — population ~96, mean nearest-neighbour **0.824**, mean
energy **0.806**. A harness that misses those has found a bug, not an ecology result. Two harness
bugs on 2026-08-29 produced numbers that read exactly like findings, and only a control caught them.

### Step 0 — is the premise even true? (no production code)

Count, at tick 12,000 in unmodified `Y`: **how many `Food` resources are active**, how many plant
patches are alive, and how creatures are distributed relative to them.

- If `Y` already ends with 20+ active food sites and the herd still piles at a few, **"six
  locations" is a false premise** and this design answers the wrong question. Report and stop.
- If it ends near six, the premise holds and step 1 proceeds.

### Step 1 — does the number of food locations move spacing at all?

A `SplitFoodSites(id, n)` transform: each **active** Food definition becomes `n` sites near the
original position with `Capacity` and `RegenerationPerSecond` divided by `n`, so **total
productivity is unchanged** and the only variable is how many places food is. `n = 1` is the
control and must be fingerprint-identical.

Arms: `n` = 1 (control), 2, 4, 8. Reported per arm: survivors, mean nearest-neighbour, share within
0.5 and within 1.0, mean energy, population.

- **If spacing does not respond to `n`,** the "six locations" diagnosis is wrong and generated
  placement will not fix the pile either. Cheap negative; it stops the build.
- **If it responds,** step 2 asks whether water is the second tether.

### Step 2 — does water bind the herd?

> **MEASURED, AND THE ANSWER INVERTS THE EXPECTATION BELOW.** Splitting water as well made clumping
> **worse** — 0.768 against the control's 0.824 — because each cluster becomes self-sufficient and
> the herd settles into tight local groups. Water does tether, and the tether is *load-bearing for
> spacing*: the arms that spread food into rings **around the existing water points** performed best.
> Do not propose splitting or generating water as the fix.

The same transform applied to Water as well as Food, at the best `n` from step 1. If splitting food
alone moves spacing as much as splitting both, water does not bind and plant-only generation is
sufficient. If splitting both is needed, **generated placement alone cannot fix clumping**, and
that finding — not a bigger build — is the deliverable.

### Step 3 — only if 1 and 2 pass: generation itself

Build section 4.2, then re-measure against the same control, plus: rejection rate of the fertility
filter, occupancy (live patches / generated sites), correlation between site fertility and patch
biomass, and the count of active food sites at tick 12,000.

## 6. What would make this a failure, stated in advance

- Spacing does not respond to site count in step 1.
- Survival regresses: `Y` currently ends 6 of 6 alive near 96. Any arm that kills worlds is a
  failure even if spacing improves — the bigger-world pilot failed exactly this way, on
  **establishment**, not carrying capacity.
- Occupancy collapses: too many generated sites at too wide a spacing and plants cannot reach the
  next one, the measured cliff at spacing 13.3.
- The control arm does not reproduce 0.824 / ~96 / 0.806. Then nothing else in the run is evidence.

## 7. Risks carried from the record

- **Reading this codebase produces plausible mechanisms faster than correct ones.** Every claim in
  section 5 is a question, not a prediction.
- Moving where food is moves where every creature spends its time, so **every recorded ecology
  result would be measured against a different world.** Hence the default-`false` flag and the
  playtest scenario as the only place it is switched on.
- `gradedFertilityEnabled` is **off for `Y` by the user's explicit choice**. Do not fold it in.
