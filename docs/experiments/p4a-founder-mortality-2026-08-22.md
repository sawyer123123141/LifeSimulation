# Why juvenile founders fail under separated food and water

**Date:** 2026-08-22
**Raw data:** `p4a-founder-mortality-2026-08-22.csv`
**Status:** predictions registered before the run; results appended after.

## The question

Three scenarios, the same four juvenile founders, the same config, and very different outcomes:

| scenario | food/water layout | extinct |
|---|---|---|
| `ObservationStable` | co-located at each cluster | 0/30 |
| `ObservationShiftingPatches` | food 7 units from water | 6/30 |
| `ObservationRouteRing` | food 6.12 units from water | 11/30 |

Extinct runs record 0-3 births against a survivor mean of 48.2, so this is a failure to *establish*,
not a later collapse. Four calibration levers are already refuted: mature founders (5/30 vs 6/30),
eight founders (14/30 — overshoot then crash), placing founders on a food site (14/30, worse than
starting on water), and 1.5x capacity and regeneration (5/30).

This run stops guessing at levers and asks what actually kills them.

## Method

Three arms — `stable`, `shifting`, `ring` — seeds 42-71, 6,000 ticks, the standard observation
config. The four founder ids are captured before stepping, and every `Death` event is attributed to
a founder or not, with its `DeathCause` and tick. For each still-living founder, every tick records
energy and hydration as fractions of that creature's own phenotype capacity; the run keeps the
minimum and the lifetime mean of each. Founder travel distance and mean distance to the nearest
water and nearest active food are recorded on the same schedule.

`SimulationStatistics.StarvationDeathCount` and `DehydrationDeathCount` are recorded as a
cross-check on the event tally: if the two disagree, the event attribution is wrong and nothing else
in the run is trustworthy.

## Predictions, registered before running

1. **Cross-check passes** — event-tallied deaths equal the statistics counters in every run.
2. **The killer is dehydration, not starvation.** Water is the smaller store in every one of these
   scenarios and the one that cannot be carried. In the separated arms I expect founder dehydration
   deaths to outnumber founder starvation deaths by at least 2:1, and the gap to be much smaller or
   reversed in `stable`.
3. **Minimum hydration fraction is the metric that separates the arms**: `stable` founders bottom
   out above 0.25; `shifting` and `ring` founders bottom out below 0.15.
4. **Minimum energy fraction is similar across arms** (within 0.10). If energy also collapses, the
   problem is total travel cost rather than a specific resource, which is a different diagnosis and
   a different fix.
5. **Founder travel is 1.5-3x higher** in the separated arms, and mean distance to water is at
   least 3 units higher.
6. **Founder deaths cluster early** — first founder death before tick 1,500 in the separated arms.

**What each outcome would imply.** If predictions 2 and 3 hold, the mechanism is that a juvenile
committed to a food patch cannot make the round trip to water before dehydrating, and the honest
fixes are about the decision policy's need-anticipation (start moving to water before thirst is
urgent) or about juvenile physiology — *not* a home-range-style affinity term, which is closed. If
instead energy collapses alongside hydration, the diagnosis is that the round trip simply costs more
than a juvenile's metabolism can fund, and the fix is a juvenile capability or cost question.

**Pre-run doubt.** Prediction 2 is the one I would bet on but it has a real competitor: juveniles may
be dying of starvation *while standing at water*, because the plant patches near them are grazed
down by the whole founder group at once. The per-founder minimum energy and hydration fractions
should distinguish these cleanly, which is why both are recorded rather than just the death cause.

## Results

90 runs, 3 arms x 30 seeds, 6,000 ticks. Raw rows: `p4a-founder-mortality-2026-08-22.csv`.
The event tally matched `SimulationStatistics` in all 90 runs, so prediction 1 held and the
attribution is trustworthy.

### Predictions 2 and 3 are refuted outright: nothing starves and nothing dehydrates

| arm | extinct | founder deaths | by starvation | by dehydration | other (age) | first founder death | min energy fraction | min hydration fraction |
|---|---|---|---|---|---|---|---|---|
| stable | 0/30 | 4.00 | 0.00 | 0.00 | 4.00 | tick 2622 | 0.470 | 0.671 |
| shifting | 6/30 | 4.00 | 0.07 | 0.00 | 3.93 | tick 2497 | 0.330 | 0.445 |
| ring | 11/30 | 4.00 | 0.00 | 0.00 | 4.00 | tick 2571 | 0.447 | 0.510 |

**All four founders die in every arm, of old age, at almost exactly the same tick.** Starvation and
dehydration are essentially absent — 0.07 founder starvations per run in the worst arm — and
founders never approach empty: the *minimum* hydration fraction any founder reaches averages 0.445
even in the worst arm. The round-trip-dehydration hypothesis, which I would have bet on, is wrong.

So the failure is not mortality at all. Founders live equally long everywhere. In the separated
arms they simply **fail to replace themselves before ageing out.** Extinction is a reproduction
failure wearing a mortality costume.

### Energy means do not explain it either

Mean founder energy fraction is 0.776 (stable), 0.640 (shifting), 0.680 (ring), and travel rises
from 212 to ~344. But within an arm, the extinct and surviving runs are barely distinguishable on
these: shifting extinct 0.621 versus survived 0.645, ring extinct 0.646 versus survived 0.699. The
outcome is **bimodal** — 0.1 to 5.2 births in extinct runs against 48-49 in survivors — which no
smooth energy gradient explains. Something binary is gating reproduction.

### The actual mechanism: the reproduction gate is a *joint* condition

`ReproductionSystem.CanReproduce` requires energy **and** hydration **and** health each at or above
**70% of that creature's own capacity**, plus adult age and no cooldown. Measuring how often adult
creatures satisfy each marginal, and how often they satisfy both at once:

| arm | energy ≥ 0.7 | hydration ≥ 0.7 | **both at once** | weaker marginal | simultaneity shortfall | births |
|---|---|---|---|---|---|---|
| stable | 0.951 | 0.992 | **0.950** | 0.951 | **0.001** | 66.3 |
| shifting | 0.463 | 0.619 | **0.335** | 0.463 | **0.128** | 39.6 |
| ring | 0.654 | 0.660 | **0.568** | 0.654 | **0.086** | 31.1 |

Two distinct effects, and both point the same way:

1. **The marginals collapse.** Energy is above the 70% bar 95.1% of the time with co-located
   resources and only 46.3% of the time when food sits 7 units from water.
2. **A real simultaneity penalty sits on top of that.** With co-located resources, satisfying both
   at once costs essentially nothing beyond satisfying the harder one alone (shortfall 0.001).
   Under separation it costs a further 8.6 to 12.8 points, because a creature is topping one need
   up while the other drains. Being fed and being watered become anti-correlated states.

The result is that an adult is reproduction-eligible **95% of the time in `ObservationStable` and
only 33.5% of the time in `ObservationShiftingPatches`.** Splitting extinct from surviving runs
confirms the gate is the discriminator: shifting 0.258 versus 0.354, ring 0.436 versus 0.644.

## Verdict

**Separating food from water does not kill creatures; it sterilises them.** Every arm loses its
four founders to age at tick ~2500. The co-located arm replaces them many times over; the separated
arms often do not, because the 70%/70% joint reproduction gate is satisfied a third to a half as
often when a creature must commute between its two needs.

This retires the framing of the P4a "optional juvenile local-area bias" item as a fix for this
problem. **Juveniles are not the failing class and mortality is not the failure mode.** A bias that
keeps young creatures near their birth area would not raise the joint eligibility window; it might
even lower it, by keeping them near whichever single resource they were born beside.

It also explains, after the fact, every refuted calibration lever:

- **founders placed on a food site made things worse** (14/30 versus 6/30) because it lowers the
  hydration marginal, which was already the binding half;
- **mature founders barely helped** because age was never the constraint;
- **eight founders made it much worse** because more grazers depress both marginals at once;
- **1.5x capacity and regeneration bought almost nothing** because throughput is not the
  constraint — *simultaneity* is, and refilling faster does not make the two needs peak together.

## The open design decision — for a human

The joint 70%/70% gate means **any world with spatially separated food and water is systematically
sub-fertile**, and that is a property of the biology, not of any scenario. Three defensible answers:

1. **Keep it and call it realism.** Commuting between resources genuinely reduces fertility. Then
   separated-resource scenarios simply need higher productivity or more founders to be viable, and
   `ObservationStable`'s 0/30 is the outlier rather than the norm.
2. **Lower or decouple the thresholds** — for example require 70% energy but only 50% hydration, on
   the argument that hydration is the cheaper and faster need to top up.
3. **Gate on a rolling average rather than instantaneous levels**, so a creature that has been well
   fed and well watered recently stays eligible while walking between the two.

**This is not a change to make unilaterally.** `ReproductionSystem.CanReproduce` is core biology on
the hot path of every scenario; changing it would invalidate every population baseline, every
survival calibration, and every plant-selection result on record, all of which were measured under
the current gate. Option 1 costs nothing and is the status quo. Options 2 and 3 need an explicit
decision and then a full re-baseline.

Recorded rather than acted on.
