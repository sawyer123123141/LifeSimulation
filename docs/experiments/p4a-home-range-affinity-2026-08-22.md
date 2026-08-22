# Soft home-range affinity — fixed-seed flag off/on measurement

**Date:** 2026-08-22
**Raw data:** `p4a-home-range-affinity-2026-08-22.csv`
**Status:** predictions registered before the run; results appended after.

## What is being measured

`HomeRangeAffinityEnabled` off versus on, matched on seed, scenario and every other config value.
Configuration is the same one the Play-mode observation keys build: `CreatePrototype4Defaults`,
`IntentUtilityV1`, 4 founders, `maximumPopulation` 40, mate selection off, 6,000 ticks (300 s at
20 Hz). Seeds 42-71, 30 per arm, three conditions: `ObservationStable`, `ObservationScarcity`,
`ObservationMigration`.

Metrics are computed by the probe from observable world state, identically in both arms, so no
metric depends on the flag being on:

- **site visit** — a creature entering within 2.0 of a resource cluster after being outside it.
- **same_site_fraction** — of all site entries after the first, the fraction that re-enter the
  cluster the creature last used. This is the patch-fidelity/route-reuse measure.
- **mean_distinct_sites** — how many different clusters a creature touched in its lifetime.
- **distance_from_centre** — distance from a *shadow* home centre the probe maintains in **both**
  arms with the same 0.25 learning fraction, updated on every site entry. A flag-agnostic measure
  of how far creatures roam from their own habitual area.
- **food_consumed / water_consumed** — cumulative intake.
- **final_population**, **births**, extinction counts — survival.
- **multi_food_candidate_fraction** — fraction of creature-ticks with **two or more** food patches
  inside the creature's own vision range. This is the *opportunity* measure: the affinity bonus is
  a tie-break among visible candidates, so where this is zero the mechanism has nothing to break.

## Predictions, registered before running

The geometry argument that drives these predictions: `Phenotype.VisionRange` is `4 + 12 * gene`,
so at most 16 world units. `ObservationStable` and `ObservationScarcity` place two clusters at
(-12,-8) and (12,8) — **28.8 apart** — and put food and water at the *same* point in each cluster.
`ObservationMigration` places four clusters along y = -8 at x = -18, -6, 6, 18, spaced **12** apart.

1. **Opportunity.** `multi_food_candidate_fraction` ≈ 0.00 in stable and scarcity (no creature can
   see both clusters). Migration: 0.05-0.30.
2. **The flag still reaches behavior.** Hashes differ off/on in 30/30 seeds in all three
   conditions, because the bonus also raises food/water scores against rest, wander and remembered
   targets even when only one patch is visible.
3. **Route reuse.** Stable and scarcity: no change beyond noise (|t| < 2, sign count 12-18/30).
   Migration: a modest rise in `same_site_fraction`, +0.02 to +0.10, ≥ 18/30 seeds up.
4. **Roaming.** `distance_from_centre` falls when on. Migration -5% to -25%; stable and scarcity
   -0% to -8%.
5. **Distinct sites.** Falls slightly in migration (-0.1 to -0.5 clusters per creature); flat in
   stable and scarcity.
6. **Intake and survival.** food/water consumed within ±5%; extinction counts equal between arms;
   births within ±5% with no reliable direction.

**Pre-run judgment.** If prediction 1 holds, then in two of the three named conditions the design
*cannot* produce a route, only patch stickiness, because food and water are co-located and the
second cluster is never a visible candidate. That would be a scenario-geometry finding, not a
mechanism success, and the correct next step would be clustered resource patches with separated
food and water rather than tuning the affinity constants.

## Results

Raw per-seed rows: `p4a-home-range-affinity-2026-08-22.csv` (180 runs, off/on paired by seed).
Bonus-magnitude sensitivity rows: `p4a-home-range-bonus-sensitivity-2026-08-22.csv`.

### Manipulation check first

| condition | mean familiarity (on) | creature-ticks with 2+ visible food patches | ticks with familiarity>0 AND 2+ patches | hash differs |
|---|---|---|---|---|
| stable | 0.888 | 0.0000 | 0.0000 | 30/30 |
| scarcity | 0.797 | 0.0000 | 0.0000 | 11/30 |
| migration | 0.840 | 0.2700 | 0.2077 | 29/30 |

The learning half of the mechanism works: familiarity sits near 0.8-0.9, so the bonus is being
computed with real state, and the flag reaches behavior (hashes diverge). Prediction 1 held: in
stable and scarcity **no creature ever sees two food patches at once**, so the tie-break has
literally nothing to choose between. Migration is the only condition with genuine opportunity, and
there it is large - roughly one creature-tick in five.

### Flag off versus on (seeds 42-71, 6,000 ticks)

Stable (population pinned at the cap, 39.8/40 both arms, 0/30 extinct both arms):

| metric | off | on | delta | t | seeds up |
|---|---|---|---|---|---|
| same_site_fraction | 1.0000 | 1.0000 | +0.0000 | +0.00 | 0/30 |
| mean_distinct_sites | 0.997 | 0.999 | +0.0023 | +1.44 | 6/30 |
| distance_from_centre | 1.6613 | 1.6610 | -0.0003 | -0.05 | 14/30 |
| site visits | 1510.5 | 1511.4 | +0.93 | +0.07 | 15/30 |
| food consumed | 1202.4 | 1196.5 | -5.90 | -2.25 | 11/30 |
| water consumed | 367.0 | 366.1 | -0.89 | -1.18 | 14/30 |
| births | 66.33 | 66.30 | -0.03 | -0.18 | 6/30 |

Scarcity: **30/30 seeds extinct in both arms.** Every downstream number in this condition is
measured on a corpse, exactly like the old `PredationVariation` trap, so nothing here adjudicates
anything. Recorded for completeness only: site visits +3.83 (t +2.17, 18/30), water consumed
+0.81 (t +4.44, 20/30), births +0.03.

Migration (7/30 extinct off, 4/30 on):

| metric | off | on | delta | t | seeds up |
|---|---|---|---|---|---|
| same_site_fraction | 0.9747 | 0.9747 | +0.0000 | +0.00 | 17/30 |
| mean_distinct_sites | 1.496 | 1.451 | -0.0455 | -1.39 | 11/30 |
| distance_from_centre | 2.458 | 2.431 | -0.0275 | -0.48 | 10/30 |
| site visits | 1114.3 | 1156.3 | +42.03 | +0.79 | 17/30 |
| food consumed | 706.3 | 749.1 | +42.79 | +1.20 | 14/30 |
| water consumed | 237.0 | 258.5 | +21.49 | +1.99 | 19/30 |
| births | 52.80 | 57.90 | +5.10 | +1.67 | 18/30 |
| final population | 29.93 | 31.17 | +1.23 | +0.52 | 5/30 |

The extinction difference is 3 discordant pairs in one direction and 0 in the other: a sign test
gives p = 0.125. The births and water effects sit at 18/30 and 19/30 seeds up, well under the bar
this project uses for "selected/effective", and they have no matched drift control. **None of
these is a demonstrated benefit.**

### Bonus-magnitude sensitivity: is 0.1 simply too small?

The decision bonus constant was temporarily raised 10x (`DefaultHomeRangeBonusMaximum` 0.1 to 1.0,
reverted afterwards) and the same sweep re-run, compared against the same flag-off baseline:

| condition | metric | off | bonus 1.0 | delta | t | seeds up |
|---|---|---|---|---|---|---|
| stable | same_site_fraction | 1.0000 | 1.0000 | +0.0000 | +0.00 | 0/30 |
| stable | distance_from_centre | 1.6613 | 1.5947 | -0.0666 | **-6.92** | 3/30 |
| stable | site visits | 1510.5 | 1395.4 | -115.10 | **-5.88** | 6/30 |
| stable | food consumed | 1202.4 | 1169.4 | -32.99 | **-6.70** | 2/30 |
| stable | births | 66.33 | 66.77 | +0.43 | +0.83 | 11/30 |
| migration | same_site_fraction | 0.9747 | 0.9747 | +0.0001 | +0.04 | 17/30 |
| migration | mean_distinct_sites | 1.496 | 1.431 | -0.0651 | -1.81 | 12/30 |
| migration | births | 52.80 | 59.37 | +6.57 | +1.50 | 21/30 |

At 10x the mechanism finally has a large, unambiguous effect - and it is exactly the failure mode
the design was supposed to avoid. Creatures cling harder to the patch they are already on
(-6.9% distance from the centre, -7.6% site re-entries), eat **less** (-2.7%), and gain nothing in
births or population. Route structure does not move at all: same-site fidelity changes by 0.0001.

## Verdict

**Soft home-range affinity does not create recognisable routes, and it is not merely a matter of
tuning the bonus.** Two separate reasons, both measured:

1. **The shipped observation scenarios cannot contain a route.** Food and water are co-located at
   every cluster, and clusters are 28.8 apart against a maximum vision range of 16. Patch fidelity
   is already **1.0000 with the flag off** - creatures pick a cluster and never leave it. There is
   no behavior left for an affinity bonus to add. A route needs at least two separated resources a
   creature actually shuttles between.
2. **The bonus duplicates information the utility function already has.** The centre is dragged
   toward wherever the creature last fed successfully, which is approximately where the creature
   already is. Distance-to-centre is therefore nearly collinear with distance-to-candidate, and
   `ResourceUtility` already charges a travel burden on that distance. The affinity term mostly
   reinforces the candidate the score would have picked anyway, which is why migration - with 20.8%
   of creature-ticks holding two visible patches and familiarity near 0.84 - shows a same-site
   fidelity delta of exactly 0.0000, and still 0.0001 at ten times the bonus.

Do not mark the P4a soft-home-range bullet complete. The flag stays default false; `R` stays as
the matched Play-mode control, with the honest expectation that it currently looks the same as `5`.

## What would actually be worth trying next

- **Scenario geometry first, not constants.** Build the clustered, changing resource patches from
  the P4a backlog with food and water **separated by roughly 6-10 units inside one cluster** and
  several clusters within travel range. Only then can a shuttle route exist, and this same probe
  re-measures it unchanged.
- **If affinity is retained, give it information travel burden lacks.** A centre that tracks the
  creature's own recent position cannot beat a distance term. A centre anchored to the *set* of
  resources a creature has successfully used (for example the midpoint of its last successful food
  and water sites) at least encodes something the per-candidate distance does not.
- **Do not use the scarcity condition to judge anything** until a survivable scarcity calibration
  exists; it is 30/30 extinct in both arms.
- **Do not read the migration births/water differences as a benefit.** 18/30 and 19/30 seeds up,
  no drift control, and the 10x arm does not sharpen them.
