# Generated plant placement, and what actually sets how tightly creatures pack

**Date:** 2026-08-30
**Status:** mechanism built, flag `generatedPlantSitesEnabled` **default false and NOT switched on
for `Y`**. Measured at 6 seeds across 11 arms and at 20 seeds across the 5 that mattered.
**Harness:** `tools/SitePilot` (committed). `Y`'s exact configuration, 12,000 ticks, seeds from 42.
**Control:** arm 1 is layout-fingerprint-identical to `Y` and reproduced the recorded numbers at 6
seeds — population **95.833**, mean nearest-neighbour **0.824**, mean energy **0.806**, against the
0.824 / ~96 / 0.806 on record. Every number below is read against that.

## The premise was wrong, and the correction is the first finding

The handoff says creatures clump because **food exists at six locations**, and that all three failed
levers held that constant. Measured: unmodified `Y` at tick 12,000 has **23.2 of its 24 food sites
active**, with mean spacing 5.48 between them. Plants colonise almost every dormant coordinate the
scenario declares. Six is the count at tick 0, not the count the herd lives in.

So the herd piles up **in a world that already has twenty-three food locations**. "More locations"
was never the whole mechanism, and anything built purely on that reading would have been built on a
number nobody had checked.

## What the site-count sweep found (6 seeds)

`SplitSites` divides each **active** site into N sites sharing its capacity, so total productivity is
unchanged and the only thing that moves is where food is. Part 1 stays on the original coordinate, so
the founder placement still lands on a site and N=1 is fingerprint-identical.

| arm | food sites | alive | mean nearest | <0.5 | energy |
|---|---|---|---|---|---|
| control | 24 | 6 of 6 | 0.824 | 55.0% | 0.806 |
| x4 at spread **0** (capacity split, no geometry) | 42 | 6 of 6 | 0.951 | 45.2% | 0.772 |
| x2 at spread 3 | 30 | 6 of 6 | 0.879 | 52.5% | 0.786 |
| x4 at spread 3 | 42 | 5 of 6 | 1.086 | 48.0% | 0.808 |
| x8 at spread 3 | 66 | 4 of 6 | 1.393 | 29.2% | 0.789 |
| **x4 at spread 6** | 42 | **6 of 6** | **1.177** | 40.7% | 0.798 |
| x4 at spread 3, **water split too** | 42 | 6 of 6 | **0.768** | 59.5% | 0.812 |

Three things fall out.

1. **Spacing responds to the number of food locations, monotonically.** 0.824 → 0.879 → 1.086 →
   1.393 as sites go 24 → 30 → 42 → 66.
2. **Splitting capacity without moving anything is not the mechanism.** The spread-0 arm puts four
   coincident quarter-capacity sites at each original coordinate: spacing moves to 0.951 with
   per-seed range 0.693-1.378, overlapping the control's 0.607-1.073, and population drops from 95.8
   to 84.3. Geometry is doing the work, not the division.
3. **Splitting the water as well makes clumping WORSE** — 0.768, below the control. Each cluster
   becomes self-sufficient and the herd settles into tight local groups instead of moving between
   food and water. This matters for the design: water is not a plant, so generated placement cannot
   move it.

Packing sites close is what kills worlds. Every extinction in the spread-3 arms was **late** — ticks
4,579, 7,089 and 8,359 of 12,000 — so grown populations collapsing, not founders failing. That is a
different failure mode from the bigger-world pilot, which failed on establishment.

## Generated placement itself (20 seeds)

`PlantSiteGenerator` puts sites on a jittered lattice over the arena, keeps those whose local
fertility clears a threshold, and divides the replaced dormant sites' capacity between them in
proportion to that fertility. It replaces the authored **dormant** sites only; the active ones keep
the founder plants and the founder placement.

| arm | food sites | alive | population | mean nearest | energy |
|---|---|---|---|---|---|
| control | 24 | **19 of 20** | 92.2 | 0.903 | 0.800 |
| hand split, x4 at spread 6 | 42 | **18 of 20** | 95.7 | **1.280** | 0.792 |
| generated, spacing 5, fertility .45 | 54 | 16 of 20 | 89.5 | 1.173 | 0.762 |
| generated, spacing 5, capacity 24 each | 54 | 17 of 20 | 90.4 | 1.183 | 0.781 |
| generated, spacing 6, capacity 24 each | 37 | 18 of 20 | 94.5 | 1.017 | 0.804 |
| generated, spacing 6, fertility .45 | 37 | 15 of 20 | 95.8 | 0.999 | 0.799 |
| generated, spacing 5, fertility **.60** *(6 seeds only)* | 32 | **2 of 6** | 95.5 | 1.268 | 0.806 |

**The crude hand split beats generated placement on both axes** — more spacing (1.280 against 1.173)
at better survival (18 of 20 against 16). A strict fertility threshold is outright dangerous: .60
left 2 of 6 worlds alive.

**Capacity dilution is not the cause.** Giving every generated site the authored capacity of 24
instead of a share of the budget — which makes the world genuinely richer — moved survival 16 to 17
of 20 and spacing not at all. The cost is in the geometry, not in how much each site holds.

**The best current explanation is the water tether**, and it is consistent with all three results:
splitting food into rings *around the existing water points* raises spacing most and costs nothing;
splitting water as well collapses spacing; and scattering food onto fertile ground regardless of
where water is raises travel, drops mean energy to 0.762, and kills worlds late. `Y` has water at six
hand-typed coordinates, co-located with the six active food sites. **A plant feature cannot move
water, so generated plant placement is working against a tether it does not control.**

## What generated placement does buy, and it is visible

Rendered through `CreatureArenaCapture.CaptureSitePilot` at `Y`'s own configuration, seed 42, tick
12,000 (`Logs/creature-models/sitepilot-*.png`):

- **control** — one dense knot of overlapping animals, much of it standing **past the drawn
  shoreline, over the sea plane**. (The obvious explanation is that some hand-typed coordinates are
  below sea level at this seed. **That is a reading of the picture, not a measurement** — nothing
  here sampled elevation at those six coordinates.)
- **hand split x4 spread 6** — animals distributed in small groups across the ground, individually
  distinguishable, one residual pile.
- **generated** — animals spread across the ground, and in this render **all of them on land**.
  Fertility ridges at moderate moisture, so waterlogged ground scores poorly and the filter does not
  place sites there. One seed, one picture: the pattern is worth a proper count before it is claimed
  as a property of the mechanism.

That last point is the thing generated placement does that no amount of splitting authored
coordinates can: **the food map stops being a list of coordinates that may or may not be on ground
the world supports.**

## What was built

- `PlantSiteGenerator` — pure, deterministic in the world seed, tested (12 tests).
- `SimulationScenario.SplitSites` — the measurement instrument, kept like `WithFeedingRadius` was.
- `SimulationScenario.ApplyTo` now registers **resource** indices explicitly rather than relying on
  definition index equalling resource index, which stops being true the moment dormant sites are
  replaced.
- `SimulationConfig.GeneratedPlantSitesEnabled` and three parameters, default off, hashed, in the
  manifest.
- `CreatureArenaCapture.CaptureSitePilot` — the three renders above, at `Y`'s configuration rather
  than the pressured cell.

**672 headless tests green.** Nothing recorded moved: the flag is off by default and `Y` is unchanged.

## What not to do next

- Do not tune the fertility threshold upward. .60 killed 4 of 6 worlds.
- Do not read "more sites is better" from the monotone spacing column. Packing sites close kills
  grown populations, late.
- Do not re-run the water split as a fix. It makes clumping worse, measured.
