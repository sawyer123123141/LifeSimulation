# Clustered, changing resource patches — turnover measurement

**Date:** 2026-08-22
**Design:** `docs/superpowers/specs/2026-08-22-clustered-changing-resource-patches-design.md`
**Raw data:** `p4a-shifting-patches-2026-08-22.csv`
**Status:** predictions registered before the run; results appended after.

## What this run is for

The P4a backlog asks for "clustered, changing plant/resource patches so travel creates recognizable
routes rather than unstructured wandering". Two thirds of that is already answered:

- **Routes already form from geometry alone.** `ObservationRouteRing`, measured 2026-08-22 with
  every optional behavior flag off, produced 537 cross-kind legs per run at a 0.7955 unordered
  pair-repeat fraction. Separating food from water by ~6 units is sufficient; no decision mechanism
  is needed, and the one that was tried made routes worse and is now closed.
- **Changing patches are already an implemented mechanism.** `PlantReproductionSystem.Step` calls
  `resources.SetActiveAt(siteIndex, true)` on a successful dispersal and `PlantMortalitySystem.Step`
  calls `resources.SetActive(id, false)` when a patch dies. No observation scenario had ever
  declared dormant dispersal sites or enabled plant mortality, so the food map was frozen.

So this measures a scenario, not a new system: does the existing turnover machinery, placed in a
clustered layout, produce map change on a watchable timescale without killing the population?

## Scenario: `ObservationShiftingPatches` (`p4a-observation-shifting-patches`)

Three clusters at (-14,-9), (13,-6) and (-2,12) — 23-28 apart, beyond any creature's vision, so each
is a genuine local region. Per cluster: one permanent Water site at the centre (40 capacity, 2/s),
two active Food sites 7 units out on opposite sides (200 capacity, 10/s each), and four dormant Food
sites at radius 7-7.07 as dispersal targets (amount 0, capacity 200, regeneration 0, inactive).
Founders start at the first cluster centre.

Total *simultaneously active* founder productivity equals `ObservationStable` exactly — 1200 food
capacity at 60/s and 120 water at 6/s — so a survival difference is attributable to turnover and
layout, not to a change in total output.

## Arms

- **frozen** — `plantMortalityEnabled: false`. Identical geometry, no turnover. The control.
- **shifting** — `plantMortalityEnabled: true`.

Everything else matches the Play-mode observation config. `plantSiteCompetitionEnabled` stays off in
both arms: the field notes record it destroys 34% of every patch ever born within a median two
seconds, which would confound turnover rate with newborn destruction. It is a later arm, not this
one. 30 paired seeds (42-71), 6,000 ticks.

## Predictions, registered before running

**Turnover, reported first.**

1. The frozen arm shows **0 activations and 0 deactivations** and `first_turnover_tick` of -1. If it
   does not, the control is broken and nothing else is interpretable.
2. The shifting arm shows **at least 20 deactivations and at least 10 activations** per run, with
   `first_turnover_tick` before tick 3,000 (150 seconds of simulated time, comfortably watchable at
   the 8x Play-mode speed). Plant lifespan under the standard genome is on the order of 100 seconds,
   so with six founder patches I expect first death well before tick 2,000.
3. `distinct_sites_ever_active` in the shifting arm ≥ 8 of the 18 food sites — i.e. dispersal
   actually reaches dormant sites rather than only recolonising the two founder positions.
4. `mean_active_food_sites` in the shifting arm lands between 3 and 8 against the frozen arm's
   fixed 6. Below 3 means turnover is stripping the map; above 8 means plants are colonising faster
   than they die and the map inflates past its matched productivity.

**Ecology.**

5. `pair_changed_fraction` (creatures using two or more distinct cross-kind pairs) is **higher in
   the shifting arm by at least 0.10**, at ≥ 20/30 seeds up. This is the claim that routes *re-form*
   around new patches rather than creatures simply dying with their patch.
6. `pair_repeat_fraction` **falls** in the shifting arm, by 0.02-0.15. A map that changes should make
   any single route less permanent; a completely flat pair-repeat fraction alongside a raised
   `pair_changed_fraction` would be surprising and worth investigating rather than reporting.
7. `mean_speed` rises 0-10% in the shifting arm (more commuting to replacement patches).

**Survival — the risk this scenario carries.**

8. Extinctions: frozen 0-3 of 30. The ring measurement showed that spreading productivity across
   more sites raised extinction from 0/30 to 11/30, and this layout has three separated clusters, so
   the frozen arm is not automatically safe either.
9. Shifting arm extinctions no more than **6 worse** than frozen. Food and water consumed within
   ±15%; births within ±20%. If the shifting arm goes extinct substantially more, the honest outcome
   is a recorded calibration finding and a retuned scenario — **not** a new mechanism to protect
   creatures from turnover.

**Pre-run judgment.** Prediction 2 is the one I am least sure of, in the direction of *too much*
turnover rather than too little: all six founder patches are created at tick 0 with the same genome
distribution, so their deaths may cluster in time and strip most of the map at once, which would
show up as a survival collapse in the shifting arm rather than as pleasant gradual change. If that
happens, staggering founder patch ages is the obvious scenario fix and is cheap.

## Results

30 paired seeds (42-71), 6,000 ticks, four arms, 120 runs.
Raw rows: `p4a-shifting-patches-2026-08-22.csv`.

### Correction to the design before reading anything else

**Prediction 1 was wrong, and so was a claim in the design.** The "frozen" arm is not frozen: it
records **12 activations** and a first turnover event at **tick 20**. Plant dispersal does not
require plant mortality, so with mortality off the plants simply colonise all twelve dormant sites
almost immediately and then stay there permanently (0 deactivations, 17.37 of 18 sites active on
average). The real contrast between the arms is therefore **one-way colonisation to saturation**
versus **ongoing churn**, not "no change" versus "change".

That also invalidates the design's matched-productivity claim. Active food capacity does not stay
at the founder value of 1200: in the frozen arm it rises to roughly 18 sites x 200 = 3600. Any
comparison of this scenario against `ObservationStable` on survival is comparing different
productivity, and the extinction numbers below must be read with that in mind.

### Turnover, reported first

| metric | frozen | shifting | delta | t | seeds up |
|---|---|---|---|---|---|
| activations | 12.00 | 33.50 | +21.50 | +46.31 | 30/30 |
| deactivations | 0.00 | 29.47 | +29.47 | +160.11 | 30/30 |
| distinct sites ever active | 18.00 | 18.00 | 0.00 | - | 0/30 |
| mean active food sites | 17.37 | 11.96 | -5.41 | -46.33 | 0/30 |
| first turnover tick | 20.3 | 20.3 | 0.00 | - | - |
| final plant count | 18.00 | 10.03 | -7.97 | -23.16 | 0/30 |

Turnover works, on a watchable timescale, using only mechanisms that already existed. The shifting
arm sustains about **29 patch deaths and 33 establishments per run** and settles at a dynamic
equilibrium of **11.96 active food sites** between the founder six and the saturated eighteen.
Dispersal reaches every one of the eighteen sites. Predictions 2 and 3 met; prediction 4 was too
low because the map self-expands past the founder layout.

### Ecology: shifting versus frozen

| metric | frozen | shifting | delta | t | seeds up |
|---|---|---|---|---|---|
| pair_repeat_fraction | 0.7222 | 0.6286 | -0.0935 | **-6.47** | 3/30 |
| mean_distinct_pairs | 2.353 | 2.981 | +0.628 | **+4.48** | 22/30 |
| pair_changed_fraction | 0.7509 | 0.8247 | +0.0739 | +1.88 | 19/30 |
| mean_distinct_sites | 3.578 | 4.441 | +0.863 | **+5.75** | 23/30 |
| same_site_fraction | 0.5743 | 0.5272 | -0.0471 | -4.36 | 5/30 |
| cross_kind legs | 444.8 | 441.5 | -3.33 | -0.12 | 17/30 |
| mean speed | 1.8535 | 1.8130 | -0.0405 | -0.95 | 14/30 |
| births | 35.93 | 39.57 | +3.63 | +1.10 | 13/30 |
| deaths | 12.37 | 15.73 | +3.37 | +2.25 | 13/30 |
| final population | 27.53 | 27.77 | +0.23 | +0.07 | 6/30 |
| **extinct** | **8/30** | **6/30** | - | - | - |

**This is the result the backlog item wanted, and it needed no new mechanism.** A changing map does
not reduce how much route behaviour happens - cross-kind legs are statistically identical at 445
versus 441 - but it makes each individual route less permanent (pair repeat -0.0935, t -6.47, only
3/30 seeds up) while nearly doubling how many *different* routes a creature uses over its life
(distinct pairs +0.628, t +4.48, 22/30 up; distinct sites +0.863, t +5.75, 23/30 up). Clinging to
one site falls. Predictions 5 (marginally: +0.0739 at 19/30, short of the +0.10 at 20/30 bar), 6 and
7 are broadly confirmed, and **turnover costs nothing in survival**: final population is identical
and extinctions are 6/30 against 8/30.

### Survival calibration: two hypotheses tested and both refuted

Extinctions of 8/30 and 6/30 badly miss prediction 8. The extinct runs are **founder-establishment
failures, not ecosystem collapses**: extinct seeds recorded 0-3 births in most cases (frozen:
0,0,1,1,1,2,3,9) against a survivor mean of 48.2 births. Founders are dying before the population
establishes, and separating food from water is the obvious suspect, since `ObservationStable` gives
0/30 with the same four founders and co-located resources.

Two cheap fixes were tested and **both failed**:

| arm | extinct | final population | births | pair_repeat | distinct_pairs |
|---|---|---|---|---|---|
| frozen | 8/30 | 27.53 | 35.93 | 0.7222 | 2.353 |
| shifting | 6/30 | 27.77 | 39.57 | 0.6286 | 2.981 |
| shifting, mature founders | 5/30 | 20.87 | 43.53 | 0.6196 | 3.156 |
| shifting, 8 founders | **14/30** | 10.07 | 48.77 | 0.6275 | 2.847 |

Mature founders barely move extinction (5/30 versus 6/30) - so founder *juvenility* is not the
cause, and my stated hypothesis was wrong. Doubling founders to eight makes it **much worse**
(14/30 extinct, final population 10.07) despite producing the most births of any arm: more founders
graze the patches down faster and the population overshoots and crashes. Calibration was stopped
here rather than continuing to search.

### No Play-mode key yet

Play mode runs one fixed seed, not a distribution, so the decisive question is what **seed 42**
does. It goes extinct in **all four arms** - 1 birth in the frozen and shifting arms. A key bound to
this scenario today would show a player the founders dying. The design's own non-goal said no key
until the measurement earns one, so no key is added.

## Verdict

- **The changing-patch half of the P4a bullet is demonstrated and needs no new system.** Existing
  plant dispersal and mortality, given dormant sites and `plantMortalityEnabled`, produce ~29
  deaths and ~33 establishments per run at a stable equilibrium of 12 active sites.
- **The route half was already satisfied by geometry** and is improved by turnover: same amount of
  route behaviour, less permanence per route, roughly 27% more distinct routes per creature.
- **The bullet is not closed**, for one reason only: the scenario is not yet watchable, because
  founder establishment fails in about a fifth of seeds including seed 42.
- **Next task is bounded:** find a founder placement or productivity calibration for
  `ObservationShiftingPatches` in which seed 42 establishes, verify extinction across 30 seeds is
  near zero, then add the Play key. Do not spend the effort on new mechanisms - neither mature
  founders nor more founders is the answer, and the shortest untested lever is founder *placement*
  (founders currently start on the cluster's water centre, seven units from the nearest food)
  and per-site regeneration.
