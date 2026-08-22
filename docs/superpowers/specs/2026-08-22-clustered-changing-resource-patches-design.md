# Clustered, Changing Resource Patches — Design

**Status:** proposed
**Date:** 2026-08-22
**Backlog item:** `docs/ROADMAP.md` P4a — "clustered, changing plant/resource patches so travel
creates recognizable routes rather than unstructured wandering"

## Premise check first

Two parts of this backlog item are already answered, and saying so changes what gets built.

**"so travel creates recognizable routes" is already satisfied by geometry alone.** The
`ObservationRouteRing` measurement on 2026-08-22 recorded, *with every optional behavior flag off*,
a mean of 537 cross-kind legs per run at a **0.7955** unordered pair-repeat fraction. Creatures
already shuttle repeatably between a food site and a water site when the two are separated by
roughly 6 units. No new decision mechanism is needed for routes, and the closed home-range
experiment showed that adding one made routes *worse*. This item must not be used to reopen that.

**"changing patches" is already an implemented mechanism, merely never switched on where anyone can
watch it.** `PlantReproductionSystem.Step` ends a successful dispersal with
`resources.SetActiveAt(siteIndex, true)`, and `PlantMortalitySystem.Step` ends a patch's life with
`resources.SetActive(patch.FoodResourceId, false)`. Food sites therefore appear and disappear
already. What is missing is a scenario that (a) declares dormant sites for seeds to land in, and
(b) enables `plantMortalityEnabled` — the observation scenarios declare no dormant sites and the
observation config leaves plant mortality off, so the food map is frozen for the whole run.

So the remaining work is **scenario data plus a measurement**, not a new system. That is the
cheapest form this item can take and it is the form the field notes repeatedly recommend: place
existing mechanisms in a scenario that exercises them, then measure.

## What is actually unknown

1. Does patch turnover happen on a **watchable timescale** — tens of events inside a few minutes of
   play — or is it so slow the map looks frozen anyway, or so fast it looks like noise?
2. Does creature travel **follow** the moving food, or do creatures die when their local patch
   dies because the replacement established somewhere they cannot see?
3. Does the population **survive** turnover at a productivity level comparable to the scenarios
   already known to survive?

Only question 3 has a strong prior, and it is a worrying one: the route-ring measurement showed
that splitting a fixed productivity across eight sites raised extinction from 0/30 to 11/30. A
scenario with more sites still must be checked for survival, not assumed.

## Scenario: `ObservationShiftingPatches`

Scenario data only. No new config flag, no change to any existing scenario, factory, or key.

**Three clusters**, centred at (-14, -9), (13, -6) and (-2, 12). The clusters are far enough apart
(24-29 units) that a creature cannot see from one to the next, so a cluster is a genuine local
region, exactly as `ObservationStable` intends.

**Inside each cluster:**

- one **Water** site at the cluster centre — water is not a plant, never dies, and anchors the
  region;
- two **active Food** sites at radius 7 from the centre, on opposite sides. Radius 7 puts food
  6-9 units from water, which is the separation that produced repeatable shuttling in the ring;
- four **dormant Food** sites (`isActive: false`, amount 0) scattered at radius 5-9 within the same
  cluster, as dispersal targets.

That is 3 water, 6 active food and 12 dormant food sites. Active food capacity per site is set so
that total *simultaneously active* food capacity and regeneration start equal to
`ObservationStable`'s (1200 capacity, 60/s), i.e. 200 capacity and 10/s per active food site; water
is 40 capacity and 2/s per site. Total active productivity is therefore matched to a scenario that
never went extinct, while the site count that can be active at once is allowed to drift upward as
plants disperse — which is itself one of the things being measured.

Dispersal range must be able to reach the dormant sites from an active one; the intra-cluster
distances are 4-14 units, and `DispersalRange` is a plant gene, so the measurement must report the
realised establishment count rather than assume it.

## Configuration for the measurement

The existing observation config plus exactly two additions:

- `plantMortalityEnabled: true` — without it patches never die and the map cannot change.
- `plantCohortsEnabled: true` — already on via `CreatePrototype4Defaults`.

`plantSiteCompetitionEnabled` stays **off** for the first arm. The field notes record that site
competition is effectively infanticide: it destroys 34% of every patch ever born, inside a median
two seconds. Enabling it in the same step would confound turnover rate with newborn destruction.
It is a second arm, not part of arm one.

## Measurement

Fixed seeds 42-71, 6,000 ticks, arms:

- **A: frozen map** — `ObservationShiftingPatches` with `plantMortalityEnabled: false`. This is the
  control: identical geometry, no turnover.
- **B: shifting map** — the same with `plantMortalityEnabled: true`.
- **C: shifting map plus competition** — only if B survives.

Metrics, all computed identically across arms:

- **patch turnover**: count of food-site activations and deactivations; distinct sites ever active;
  mean active food sites per tick; ticks to first turnover event.
- **route metrics** carried over unchanged from the ring probe: cross-kind legs, unordered
  pair-repeat fraction, same-site fraction, distinct sites per creature.
- **route re-formation**: after a food site a creature had been using goes inactive, does that
  creature establish a new repeated pair within the run? Reported as the fraction of creatures whose
  most-used pair changes at least once.
- **survival**: final population, extinctions per arm, births, deaths, food and water consumed.
- **travel**: mean speed and total distance, to detect "creatures now spend their lives commuting".

Report the turnover count first. If arm B produces no turnover, or turnover after tick 5,000, the
scenario failed as a test bed and nothing downstream is interpretable.

## Acceptance

This item is satisfied if arm B shows turnover on a watchable timescale, creatures re-form routes
around the new patches, and survival is not materially worse than arm A. If turnover kills the
population, the honest outcome is a recorded calibration finding plus a retuned scenario, not a new
mechanism to protect creatures from it.

## Non-goals

- No new decision mechanism, and specifically no revival of home-range affinity or place memory.
- No new config flag; if the measurement wants one, that is a separate decision.
- No Play-mode key until the measurement says the scenario is worth watching.
