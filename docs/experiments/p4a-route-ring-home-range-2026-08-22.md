# Soft home-range affinity on route-capable geometry

**Date:** 2026-08-22
**Raw data:** `p4a-route-ring-home-range-2026-08-22.csv`
**Follows:** `p4a-home-range-affinity-2026-08-22.md`, which found the mechanism null for route
formation in the three shipped observation scenarios.
**Status:** predictions registered before the run; results appended after.

## The hypothesis being tested — and its weakness

The previous experiment gave two candidate explanations for the null. This run separates them.

1. **Geometry.** Stable and scarcity co-locate food and water and put the second cluster beyond
   vision range, so patch fidelity was already 1.0000 with the flag off. No mechanism can improve
   on a saturated metric.
2. **Redundancy.** The affinity centre chases the creature's own recent position, so
   distance-to-centre is nearly collinear with distance-to-candidate, which `ResourceUtility`
   already charges as a travel burden.

Only explanation 1 is repaired by new geometry. **Geometry is the next hypothesis, not an
established fix.** If the ring below still produces a null, explanation 2 stands on its own, and
the architecture is a measured negative rather than an unlucky test.

## Scenario: `ObservationRouteRing` (`p4a-observation-route-ring`)

Eight resource sites on a radius-8 ring centred on the origin, alternating Food and Water, founders
placed at the centre:

| angle | kind | position |
|---|---|---|
| 0° | Food | (8, 0) |
| 45° | Water | (5.657, 5.657) |
| 90° | Food | (0, 8) |
| 135° | Water | (-5.657, 5.657) |
| 180° | Food | (-8, 0) |
| 225° | Water | (-5.657, -5.657) |
| 270° | Food | (0, -8) |
| 315° | Water | (5.657, -5.657) |

Design properties this geometry is built to have:

- **A route can exist.** Adjacent food-to-water separation is **6.12** units, inside the 6-10 band,
  so a creature must physically shuttle between two distinct points to satisfy both needs.
- **The choice is a genuine tie.** Every site has **two** opposite-kind neighbours at *exactly* the
  same distance. Travel burden cannot break that tie; only something like familiarity can. This is
  the decision opportunity the previous scenarios never provided.
- **An alternative route stays visible.** Same-kind sites are 11.31 apart, inside the vision range
  of creatures with a `VisionRange` gene above ~0.61, so leaving for a different arc of the ring
  remains a live option rather than being invisible.
- **Survival is held comparable.** Total capacity and regeneration match `ObservationStable`
  exactly (1200 food at 60/s, 120 water at 6/s), split across four sites of each kind, so a
  survival difference against that baseline is a scenario effect and not a productivity change.

Scenario data only - no behavior flag, no change to `5`, `6`, `7`, `9`, `N`, `R`, or any factory.

## Metrics

Computed identically in both arms from observable state:

- **pair_repeat_fraction** — the primary route measure. Every cross-kind leg (a food site entered
  after a water site, or the reverse) is compared with the creature's previous cross-kind leg; the
  fraction that repeat the *same ordered pair of sites* is route reuse. A creature shuttling
  F(8,0) ↔ W(5.657,5.657) scores near 1.0; one wandering the ring scores near chance.
- **food_to_water / water_to_food** — leg counts, to confirm shuttling happens at all.
- **same_site_fraction**, **mean_distinct_sites** — patch stickiness, to separate "useful route"
  from "refuses to leave one site".
- **cross_kind_opportunity_fraction** — creature-ticks with two or more water candidates *and* a
  food candidate in vision. The opportunity check: the tie-break needs a tie to break.
- **multi_food_fraction / multi_water_fraction**, **mean_familiarity**,
  **familiar_opportunity_fraction** — manipulation checks.
- **mean_distance_from_centre** (flag-agnostic shadow centre, 0.25 learning fraction),
  **mean_speed**, **food/water consumed**, **births**, **final_population**, extinctions.

## Predictions, registered before running

1. **Opportunity (reported first).** `cross_kind_opportunity_fraction` ≥ 0.40;
   `multi_water_fraction` ≥ 0.40; `multi_food_fraction` ≥ 0.20. Mean familiarity on the enabled arm
   0.70-0.95. Hashes differ in 30/30 seeds. If the opportunity fractions come in near zero the
   geometry failed and nothing downstream is interpretable.
2. **Route reuse.** Flag off, `pair_repeat_fraction` ≈ 0.45-0.60 (a creature at a food site has two
   equidistant waters; the score tie is broken by whatever the utility rounding and stagger
   happen to favour, so it will not be exactly 0.5). Flag on: **+0.05 to +0.20**, at least 20/30
   seeds up. This is the prediction that decides the feature.
3. **Stickiness rather than routes** is the alternative outcome to watch for: `same_site_fraction`
   up and `mean_distinct_sites` down by more than 1.0 site while `pair_repeat_fraction` stays flat.
   That would be a negative result, not a success.
4. **Distance from centre** falls 5-20% when on.
5. **Intake and survival.** food and water consumed within ±5%; births within ±10% with no reliable
   sign count; 0/30 extinct in both arms (matched productivity to a scenario that never went
   extinct).

**Pre-run judgment.** I put roughly even odds on prediction 2. The equidistant-neighbour tie is a
real decision opportunity that the previous scenarios lacked, which is the strongest argument for
the geometry hypothesis. Against it: the bonus is capped at 0.1 while the two tied candidates also
differ in *amount* and in whichever direction the creature is already moving, and the centre after
a few successes sits between the food and the water a creature used - which is close to the
midpoint of the ring arc and therefore nearly equidistant from *both* competing waters again. If
that second argument is right, the tie is restored and the result is null a second time.

Checking that argument numerically before running: a creature shuttling F(8,0) to W(5.657,5.657)
settles a centre near (6.8, 2.8), which is 3.08 from the used water and 8.53 from the competing
one. The bonus differential is 0.062 versus 0.013 - about 0.049 of real leverage on a pair of
candidates whose travel-burden terms are equal at the site. So the tie is **not** restored, and
this geometry gives the mechanism a fair chance. The remaining risk runs the other way: if the
off-arm tie-break is already near-deterministic, `pair_repeat_fraction` may saturate high with the
flag off and be uninformative in the same way fidelity was in stable. Read the off-arm value before
reading the delta.

## Results

30 paired seeds (42-71), 6,000 ticks, 60 runs. Raw rows: `p4a-route-ring-home-range-2026-08-22.csv`.

### Opportunity and manipulation checks, first

| check | off | on | prediction | met? |
|---|---|---|---|---|
| cross_kind_opportunity_fraction | 0.9062 | 0.9009 | >= 0.40 | **yes, by a wide margin** |
| multi_water_fraction | 0.9062 | 0.9010 | >= 0.40 | yes |
| multi_food_fraction | 0.5216 | 0.5085 | >= 0.20 | yes |
| mean_familiarity | 0.0000 | 0.8826 | 0.70-0.95 | yes |
| familiar_opportunity_fraction | 0.0000 | 0.8597 | - | - |
| hashes differ | - | - | 30/30 | 28/30 (two seeds extinguish before divergence) |

**The geometry hypothesis is confirmed as a scenario fix.** Creatures spend **90.6%** of their
creature-ticks with two or more water candidates and at least one food candidate in vision, and
**86.0%** of creature-ticks have familiarity above zero *and* a same-kind choice available. This is
the decision opportunity the shipped observation scenarios never provided. Whatever the mechanism
does here, it cannot be excused by lack of opportunity.

### The deciding metric moved the wrong way

| metric | off | on | delta | t | seeds up |
|---|---|---|---|---|---|
| **pair_repeat_fraction** | **0.7955** | **0.7610** | **-0.0345** | **-2.87** | **8/30** |
| same_site_fraction | 0.5330 | 0.5924 | +0.0594 | **+4.93** | **26/30** |
| mean_distinct_sites | 3.075 | 3.106 | +0.0315 | +0.28 | 15/30 |
| pair_transitions | 537.3 | 555.5 | +18.13 | +0.31 | 13/30 |
| food_to_water legs | 270.6 | 279.6 | +9.03 | +0.31 | 12/30 |
| water_to_food legs | 266.8 | 275.9 | +9.10 | +0.32 | 13/30 |
| mean_distance_from_centre | 2.124 | 2.142 | +0.0180 | +0.51 | 17/30 |
| mean_speed | 1.891 | 1.869 | -0.0220 | -0.71 | 15/30 |

The off-arm value of 0.7955 is not saturated, so the metric had headroom in both directions. It
used it - downward. **Enabling home-range affinity made routes measurably *less* repeatable** while
making creatures re-enter the *same* site more often (26/30 seeds up, t +4.93). That is
prediction 3, the failure mode, arriving with the two strongest sign counts in the whole
experiment. Range size did not shrink either: distinct sites and distance from the centre are flat.

### Intake, births, survival

| metric | off | on | delta | t | seeds up |
|---|---|---|---|---|---|
| food consumed | 633.2 | 731.6 | +98.43 | +1.36 | 20/30 |
| water consumed | 153.4 | 179.6 | +26.20 | +1.46 | 20/30 |
| births | 31.07 | 35.17 | +4.10 | +0.93 | 14/30 |
| final population | 23.90 | 25.63 | +1.73 | +0.45 | 8/30 |
| site entries | 1090.7 | 1250.1 | +159.5 | +1.30 | 18/30 |
| **extinct** | **11/30** | **9/30** | - | - | - |

Nothing here is a demonstrated benefit: 20/30, 20/30, 14/30 and 8/30 seeds up at |t| below 1.5.
Note also that **this ring goes extinct in 11/30 and 9/30 seeds despite matching
`ObservationStable`'s total capacity and regeneration** - prediction 5 was wrong. Splitting the same
productivity across eight sites makes it materially harder to survive than two co-located pairs.
That is a fact about the scenario, recorded so nobody later uses this ring as a survival calibration.

## Verdict: close soft home-range affinity as a measured negative

Across two experiments, five conditions and 240 runs, the mechanism has produced **no evidence of
useful route formation and repeated evidence of clinging**:

- shipped scenarios: route metric saturated at 1.0000 with the flag off; delta +0.0000, and
  +0.0001 at ten times the bonus; a 10x bonus cost 2.7% of food intake for no births.
- route-capable ring, with 90.6% decision opportunity and 0.88 familiarity: route repeatability
  **fell** 0.0345 (t -2.87, 8/30 up) while same-site re-entry **rose** 0.0594 (t +4.93, 26/30 up).

The geometry hypothesis was worth testing and is now settled: geometry was **not** the blocker.
The blocker is the mechanism. A bonus proportional to proximity-to-recent-success rewards staying
near where you just succeeded, which is the definition of clinging; it contains nothing that
rewards *completing a circuit* between two complementary resources, which is what a route is.

Per the standing instruction, this architecture is closed rather than tuned. The flag stays
`false` by default, the implementation and its tests stay in place (they are correct, and the
flag-off path is byte-identical), key `R` stays as the demonstration of the negative result, and
the design spec is marked superseded.

**If route behavior is wanted later, it needs a different mechanism** - something that scores a
*pair* of complementary resources, or a need-anticipation term that starts moving toward water
before thirst is urgent. Do not reopen this one by adjusting `DefaultHomeRangeBonusMaximum`,
`DefaultHomeRangeBonusFalloffDistance`, or the learning fraction: the sign of the effect, not its
size, is what is wrong.

`ObservationRouteRing` is kept as scenario data. It is the only geometry in the repository in which
a food/water route can physically exist, so it is the right harness for the clustered-changing-patch
work - but it is a harsh survival condition, not a calibration.
