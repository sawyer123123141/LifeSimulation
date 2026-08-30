# Generated plant placement, and what actually sets how tightly creatures pack

**Date:** 2026-08-30
**Status:** mechanism built in three variants — lattice, water-filtered and water-anchored — all
behind `generatedPlantSitesEnabled`, **default false and NOT switched on for `Y`**. Measured at 6
seeds across 23 arms and at 20 seeds across the 5 that mattered. **The recommendation is a scenario
change, not this feature**; see the last two sections.
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

## The metric had to be fixed second, and it changed conclusions

Mean nearest-neighbour **depends on how many creatures there are**. Ninety animals in a fixed arena
sit closer together than seventy for no behavioural reason, and the generated arms finish with fewer
animals than the control — so comparing raw distances between them credits an arm for killing
creatures. One arm read **2.513** against the control's 0.824 and was mostly a population of 77.

Every arm below is therefore reported as a **clumping index**: observed mean nearest-neighbour
divided by what the same number of animals would give scattered at random over the arena
(`0.5 * sqrt(area / n)`, the Poisson expectation). **1.0 is randomly dispersed, lower is clumped**,
and it is comparable across arms with different populations. The control sits at **0.324**.

## What the site-count sweep found (6 seeds)

`SplitSites` divides each **active** site into N sites sharing its capacity, so total productivity is
unchanged and the only thing that moves is where food is. Part 1 stays on the original coordinate, so
the founder placement still lands on a site and N=1 is fingerprint-identical.

| arm | food sites | alive | population | **clump index** | mean nearest | energy |
|---|---|---|---|---|---|---|
| control | 24 | 6 of 6 | 95.8 | **0.323** | 0.824 | 0.806 |
| x4 at spread **0** (capacity split, no geometry) | 42 | 6 of 6 | 84.3 | 0.331 | 0.951 | 0.772 |
| x2 at spread 3 | 30 | 6 of 6 | 95.0 | 0.343 | 0.879 | 0.786 |
| x4 at spread 3 | 42 | 5 of 6 | 95.8 | 0.425 | 1.086 | 0.808 |
| x8 at spread 3 | 66 | 4 of 6 | 95.0 | 0.543 | 1.393 | 0.789 |
| **x4 at spread 6** | 42 | **6 of 6** | 95.5 | **0.460** | 1.177 | 0.798 |
| x4 at spread 3, **water split too** | 42 | 6 of 6 | 95.8 | **0.301** | 0.768 | 0.812 |

Three things fall out.

1. **Dispersion responds to the number of food locations, monotonically.** Index 0.323 → 0.343 →
   0.425 → 0.543 as sites go 24 → 30 → 42 → 66, at unchanged population.
2. **Splitting capacity without moving anything is not the mechanism.** The spread-0 arm puts four
   coincident quarter-capacity sites at each original coordinate: index **0.331 against 0.323**, and
   population drops 95.8 to 84.3. Its raw distance rose to 0.951 purely because it killed animals.
   Geometry does the work, not the division.
3. **Splitting the water as well makes clumping WORSE** — index 0.301 against 0.323 at identical
   population. Each cluster becomes self-sufficient and the herd settles into tight local groups
   instead of moving between food and water. Water is not a second obstacle to remove; the commute
   is part of what produces the spacing there is.

Packing sites close is what kills worlds. Every extinction in the spread-3 arms was **late** — ticks
4,579, 7,089 and 8,359 of 12,000 — so grown populations collapsing, not founders failing. That is a
different failure mode from the bigger-world pilot, which failed on establishment.

## Generated placement itself — three variants, all measured, none adopted

`PlantSiteGenerator` replaces the authored **dormant** sites; the six active ones keep the founder
plants and the founder placement. Three ways of choosing the replacements were built and measured.

### Variant 1 — a fertility lattice over the arena (20 seeds)

| arm | food sites | alive | population | **clump index** | energy |
|---|---|---|---|---|---|
| control | 24 | **19 of 20** | 92.2 | 0.324 | 0.800 |
| hand split, x4 at spread 6 | 42 | **18 of 20** | **95.7** | **0.501** | 0.792 |
| food x8 at spread 3 | 66 | 14 of 20 | 95.6 | 0.480 | 0.799 |
| generated lattice, spacing 4 | 71 | 16 of 20 | **72.7** | 0.557 | 0.780 |
| generated lattice, spacing 4, water within 10 | 47 | 16 of 20 | 87.0 | 0.407 | 0.794 |

**"16 of 20 alive" flatters it.** The per-seed populations in the lattice arm are
**91, 96, 40, 96, 96, —, —, 96, 1, 28, 96, 95, 91, 96, —, 96, —, 41, 8, 96**: five surviving worlds
sit at 1, 8, 28, 40 and 41 against a cap of 96. The control is at 96 in eighteen of its nineteen
survivors. **Generated placement does not only kill worlds, it leaves others wrecked**, and a
survival count cannot see that. Its high index is partly the same artefact — a world of eight animals
is not dispersed, it is nearly dead.

### Variant 2 — the same lattice, filtered to sites within reach of water (6 seeds)

Fertility says where the ground is good and says nothing about where anything drinks, so this keeps
only candidates within a set distance of a water site.

| arm | food sites | alive | population | **clump index** |
|---|---|---|---|---|
| control | 24 | 6 of 6 | 95.8 | 0.323 |
| spacing 4, water within 6 | 28 | 4 of 6 | 75.0 | 0.329 |
| spacing 4, water within 8 | 39 | 5 of 6 | 77.0 | 0.467 |
| spacing 5, water within 8 | 30 | 5 of 6 | 83.8 | 0.329 |
| spacing 4, water within 10 | 47 | **6 of 6** | 83.7 | 0.376 |
| spacing 3, water within 6 | 38 | 4 of 6 | 96.0 | 0.358 |

**Negative.** The filter mostly removes sites, and removing sites is the opposite of the lever.

### Variant 3 — sites generated on a ring around each water site (6 seeds)

The winning hand split put four sites on a radius-6 ring around each water point. This is that
geometry as a rule instead of typed coordinates: candidates on the ring, fertility still deciding
which of them become sites, a slot on poor ground retried at another angle and then abandoned.

| arm | food sites | alive | population | **clump index** |
|---|---|---|---|---|
| control | 24 | 6 of 6 | 95.8 | 0.323 |
| hand split, x4 at spread 6 | 42 | 6 of 6 | 95.5 | **0.460** |
| ring 6, 4 per water | 18 | 6 of 6 | 80.2 | 0.308 |
| ring 6, 6 per water | 24 | 6 of 6 | **95.8** | 0.310 |
| ring 8, 4 per water | 19 | 5 of 6 | 77.6 | 0.298 |
| ring 6, 8 per water | 32 | 6 of 6 | 84.3 | 0.331 |
| ring 4, 4 per water | 19 | 5 of 6 | 95.8 | 0.303 |

**Also negative, and this one is informative.** Copying the winning geometry exactly reproduces the
control, not the win: ring 6 with 6 per water is survival-perfect at full population and sits at
**0.310**, indistinguishable from doing nothing.

## Why the hand split wins and no variant of generated placement does

Generated placement governs the **dormant** sites — where dispersal may go. The hand split does
something the feature cannot: it divides **the six active patches**, the rich ones creatures actually
stand on, from capacity 24 into four of capacity 6 each, six units apart.

The three negatives all agree with that reading:

- The lattice adds sites and leaves the six rich patches intact — worlds thin out and some collapse.
- The water filter removes sites and leaves the six rich patches intact — nothing moves.
- The anchored ring reproduces the split's *geometry* and leaves the six rich patches intact —
  nothing moves.

**The pile is six rich patches, not a shortage of coordinates.** A creature stands where the food is
dense enough to be worth standing on. Adding thin sites elsewhere does not move it; the one change
that does is making the rich patches smaller and putting distance between the pieces.

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
  place sites there. One seed, one picture: worth a proper count before it is claimed as a property
  of the mechanism.

That last point is the one thing generated placement does that splitting authored coordinates cannot:
**the food map stops being a list of coordinates that may or may not be on ground the world
supports.** On this evidence it is not worth the population cost.

## Recommendation

**Ship the split, keep the feature switched off.**

1. Apply `SplitSites(parts: 4, spread: 6)` to `Y`'s layout. Measured at 20 seeds: clumping index
   **0.324 → 0.501**, population **92.2 → 95.7**, survival 19 of 20 → 18 of 20, energy 0.800 → 0.792.
   The only change measured that improves the picture without degrading the worlds. **It edits a
   playtest scenario, which is the user's call.**
2. Leave `generatedPlantSitesEnabled` false. All three variants are committed, tested and documented
   so that none of them is re-tried from scratch.
3. **If generated placement is picked up again, the thing to change is which sites are ACTIVE and how
   much each holds** — the feature currently governs neither. That means generating the founder
   patches as well as the dispersal targets, and deciding what the founder placement stands on, which
   is exactly the thing that killed the tiled-habitat probe.

## What was built

- `PlantSiteGenerator` — pure, deterministic in the world seed, three placement modes, **19 tests**.
- `SimulationScenario.SplitSites` — the instrument that produced the recommendation, kept like
  `WithFeedingRadius` was.
- `SimulationScenario.ApplyTo` now registers **resource** indices explicitly rather than relying on
  definition index equalling resource index, which stops being true the moment dormant sites are
  replaced.
- Six `SimulationConfig` parameters, all default-inert, hashed and in the manifest.
- `tools/SitePilot` — 23 arms, a control fingerprint-identical to `Y`, and a clumping index that does
  not reward an arm for killing animals.
- `CreatureArenaCapture.CaptureSitePilot` — three renders at `Y`'s own configuration.

**676 headless tests green.** Nothing recorded moved: every flag is off by default and `Y` is
unchanged.

## What not to do next

- Do not tune the fertility threshold upward. .60 left 2 of 6 worlds alive.
- Do not read a raw mean nearest-neighbour across arms with different populations. It rewards
  killing animals; use the index.
- Do not re-run the water split, the water-distance filter, or the water-anchored ring. All three are
  measured negatives.
- Do not read "16 of 20 alive" as viability without looking at the per-seed populations.
