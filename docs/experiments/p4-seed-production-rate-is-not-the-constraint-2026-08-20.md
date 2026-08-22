# Seed Production Rate Is Not the Constraint — 2026-08-20


> **AFFECTED EVIDENCE — 2026-08-22.** Its central argument is a plant LIFETIME BUDGET (a ~95.8s patch spending 58.7s mature, off cooldown and unable to find a site). The age fix changes lifetime accounting directly, so this decomposition cannot be cleared by a trait sweep alone. This document's runs used both
> `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled`, so they are on the path the
> `PlantPatchStore.ReplaceAt` takeover-age defect changed (fixed in `4cc9a47`): before the fix, a
> seedling installed by a takeover carried the incumbent's accumulated age and was frequently aged
> out within a tick or two.
>
> Revalidation on fixed code is tracked in `p4-postfix-revalidation-2026-08-22.md`. Until it lands,
> treat the figures here as unverified on current code. Nothing below has been edited or recomputed.


> **PARTIALLY CLEARED — 2026-08-22.** The plant lifetime accounting in this document was
> re-measured on fixed code and on a pre-fix worktree with a version-independent detector: takeover
> fraction 0.347 (recorded 34%), median takeover lifetime 1.95 s (recorded ~2 s), R²(takeover,
> offspring) 0.516 (recorded 51.9%), and R² of realised lifespan on offspring among patches that
> died of age 0.0039 (recorded 0.024 — same claim, essentially nothing). The fix moves each by under
> two points. **The lifetime decomposition stands.** The banner above applies only to this document's
> trait-selection content. See `p4-postfix-lifetime-accounting-2026-08-22.md`.

The third and last route that skips the `(1 - Biomass/Capacity)` growth gate was seeding **rate**,
capped by a hard-coded `ReproductionCooldownSeconds = 20f`. This wires an eleventh plant gene into
it and measures the result.

**The result is a null, and the null is correct.** The premise behind the task — that the cooldown
is what limits plant reproductive output — was wrong, and the data refuting it had already been
collected on the same day in
`p4-where-plant-fitness-is-decided-2026-08-20.md`. It was not read carefully enough before the
task was set.

## What was built

`SimulationConfig.PlantSeedProductionRateEnabled`, defaulting `false`, plus
`PlantSeedProductionRateDispersalCharge` (default 2). `PlantGenome.SeedProductionRate` is the
eleventh trait, with the full parameter treatment — constructor, `CloneMutated` at index 10,
`ToTraits`/`FromTraits`, `TraitNames`, `TraitCount`, `ComputeStateHash`.

```
reproductionCooldownSeconds = 20 * (1.5 - .75 * SeedProductionRate)     // 30s .. 15s
dispersalRange             -= charge * SeedProductionRate               // gated by the same flag
```

Both halves are gated together, so flag-off is byte-identical.

## The sweep: null at every charge

120 seeds per arm, 12,000 ticks, founders varied.

| arm | `SeedProductionRate` | seeds up | Dispersal control | extinct |
|---|---:|---:|---:|---:|
| flag disabled | t +1.51 | 70/120 | t +13.94 | 0/120 |
| charge 0 | t +3.22 | **68/120** | t +13.23 | 0/120 |
| charge 2 | t -0.62 | 57/120 | t +13.22 | 0/120 |
| charge 6 | t -1.77 | 53/120 | t +15.56 | 0/120 |

**Read the sign test, not the t.** The charge-0 arm has a `t` of +3.22 and yet **fewer seeds up
(68) than the disabled arm (70)**, where the gene does nothing at all. A trait cannot be more
directionally selected than its own inert control while being less directionally consistent than
it. That `t` is magnitude-driven, which is the exact shape of the claim retracted on 2026-08-18.
The route does not clear the bar at any charge, including zero.

Compare `SeedlingResilience` on the establishment route in the same harness: t +4.03 with **76/120
up** at a charge of 2, and t +6.24 with 85/120 up uncharged. Directional consistency rose as the
charge fell, which is what a real response looks like.

## Why: the cooldown was never the limiting factor

Founders pinned at one `SeedProductionRate` per arm, so the cooldown is a **constant** per arm,
zero dispersal charge, seeds 42-71:

| cooldown | mean plant births | plant generations | final patches | extinct |
|---:|---:|---:|---:|---:|
| 30.0 s | 203.7 | 15.9 | 22.6 | 0/30 |
| 22.5 s | 210.2 | 14.8 | 22.6 | 0/30 |
| 20.0 s (flag off) | 218.9 | 15.3 | 23.0 | 0/30 |
| 15.0 s | 221.8 | 14.8 | 23.1 | 0/30 |

**Halving the cooldown buys 8.9% more births and no extra generations.** A gene whose full 2x
range moves the population-level outcome by under 10% cannot produce a detectable individual
response at a standing genetic SD of 0.078 — that SD spans 15.6% of the trait range, so the
per-individual fitness differential is on the order of 1%, far under the drift in a six-patch
population.

The reason is the lifetime time budget. Recomputed from the 3,668 patches that died of old age in
`p4-plant-route-patches-2026-08-20.csv`:

| | seconds of a 95.8 s life |
|---|---:|
| growing to the 75% maturity threshold | 6.7 |
| on reproduction cooldown | 30.4 |
| **mature, off cooldown, and failing to find a free site** | **58.7** |

At 91% site occupancy a patch already spends **61% of its life eligible to seed and failing**.
Cooldown time freed by the gene is simply added to a pool of time that is already being wasted.
Site availability is the constraint; the cooldown never was.

## What this settles

All three routes off the growth-rate gate are now measured:

| route | verdict |
|---|---|
| **establishment** | **selectable** — `SeedlingResilience`, t +4.03, 76/120 up, with a real cost |
| mortality | no headroom — a live 2x genetic span converting at R² = 0.024 |
| seed production | **live but not selectable** — full 2x span moves births under 10% |

Two of the three fail for the *same* reason, and it is not the growth gate: **reproduction here is
site-limited.** Lifespan and cooldown both buy more *time*, and time is not scarce — free sites
are. Only establishment acts on the scarce thing.

## Disposition: kept, as a negative control

`SeedProductionRate` stays in the genome despite the null. The project has a strong positive
control (`Dispersal`, t +14 to +19.6) and has never had a **live, wired, measured-null** channel to
read against — `Genome.NeutralMarker` is kept for exactly this purpose on the animal side, but it
is unwired, which makes it a weaker control. A trait that demonstrably reaches behaviour and
demonstrably does not get selected is the more useful negative control of the two.

`PlantLivenessTests` pins the 2x span, so if a future change alters it, the sub-10% birth
measurement above stops applying and the test says so.

## Method note

The sweep that produced the table above saved no CSV, contrary to the convention in this
directory, so those four rows cannot be re-analysed without re-running. The cooldown-binding table
below it was re-measured directly and is reproducible from the committed probe description.
