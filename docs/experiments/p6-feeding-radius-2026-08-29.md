# Feeding radius and clumping — a measured negative

**Date:** 2026-08-29
**Status:** negative result, 6 seeds an arm. The lever does not work; the reason it does not work is
the useful part.
**Question:** does widening the disc a creature can feed from stop the herd reading as a pile?

## Why it looked like the answer

`Y` has six food sites of `InteractionRadius` **1.5** for **96** creatures — sixteen animals sharing a
disc three units across, while a creature model is about one unit wide. With `FeedInPlaceEnabled` a
creature switches to `Eat` on entering the radius and then stands still, so the radius is where a
feeding animal stops. Widening it should widen the standing area with the square of the radius.

## Method

`Y`'s configuration, 12,000 ticks, seeds 42-47, `SimulationScenario.WithFeedingRadius` scaling every
site's radius and nothing else. Arm 1 is the control at today's radius, and a factor of one is
fingerprint-identical to the original layout, so a broken harness shows as a wrong baseline rather
than as a finding.

## Results

| radius | survived | mean nearest | under 0.5 | under 1.0 | mean energy |
|---|---|---|---|---|---|
| **1.5** (today) | 6 of 6 | **0.824** | 55.0% | 71.3% | 0.806 |
| 3.0 | 5 of 6 | 0.716 | 46.4% | 59.3% | 0.672 |
| 4.5 | 6 of 6 | 0.875 | 51.0% | 67.4% | 0.804 |
| 6.0 | 5 of 6 | 0.724 | 44.1% | 60.0% | 0.683 |

**No effect, and not even a monotone one.** Per-seed spacing at radius 1.5 spans 0.706-1.073; at
radius 6.0 it spans 0.680-1.190. Same distribution. The two arms with an extinction have lower means
for that reason alone, and with six seeds that is noise rather than a destabilisation finding — it is
recorded but not claimed.

## Why the prediction was wrong

The prediction assumed the measurement was of feeding animals. It is not: it is a snapshot of the
whole population, and most creatures are not feeding at any instant. **They cluster because they are
bound to six locations, and widening each disc does not change how many places there are to be.**

## What this closes

Three levers have now been measured against clumping, and all three failed:

| lever | result |
|---|---|
| movement — creatures stop walking into the patch centre | 0.705 to 0.824, real but small |
| world size — four times the area, four times the sites | 0.726 to 0.945 at best, and it costs establishment |
| feeding radius — up to four times the disc | no effect |

**Every one of them kept six food locations.** Spacing is set by the number and spread of food
locations, not by how creatures move between them, how much empty ground surrounds them, or how large
each feeding disc is.

## What that points at

`docs/superpowers/specs/2026-08-14-system-integration-design.md` already specifies it. Its table says
a resource's `Position` should be "where a plant established" and its `Capacity` "set by local
fertility", against today's "authored by scenario" and "authored constant". Plants are real - biomass,
heritable genome, defence, nutrition, lineage, growth modulated by local moisture and temperature,
mortality, dispersal, establishment contests - but they may only exist at the **26 hand-typed
coordinates** in the scenario, because `PlantSiteRegistry` is, in the Phase 1 plan's own words, "a
fixed, pre-built list of eligible target slots".

Generated placement changes the one variable all three failed levers held constant. It is also the
project's own stated keystone: *"Plants are the keystone, and they are the least specified piece in
the project"*, and *"P4 plants done: the two halves become one system. Food appears because of
climate; animals move because of food."*

Plant system Phase 1 deferred the soil-moisture half explicitly — "real soil moisture is Phase 2,
alongside terrain" — and terrain landed this week. The precondition it was waiting on now exists.

## Kept, not reverted

`SimulationScenario.WithFeedingRadius` stays with its tests, the way soft home-range affinity's code
and key stayed after its own measured negative. The result is recorded here so the lever is not
re-tried from scratch.
