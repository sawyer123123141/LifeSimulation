# Slope cost, measured where it can act: suggestive, not established

**2026-08-24. 120 runs, 60 paired seeds, 12,000 ticks. `tools/CreatureSweep --focused 60`.**
Corpus: `p6-slope-cost-focused-2026-08-24.csv`.

**Not comparable with `p6-slope-cost-2026-08-24`** — different seeds, different population cap. It
answers the same question under conditions chosen so the flag *can* act, rather than conditions
inherited from the plant corpus.

## What the first sweep could not do, and what changed

| limitation | fix | did it work |
|---|---|---|
| half the arenas flat, 58 of 120 pairs byte-identical | seeds filtered to ≥ 5 m of climb per traverse | **yes — 0 identical pairs of 60** |
| population capped at 48, 96 of 120 pairs pinned there | cap raised to 200 | yes, and see the warning below |
| nothing recorded movement | `occupied_elevation`, `occupied_slope` from creature positions | yes, but underpowered — see below |

Seeds are selected on a property of the **world**, not of the result. The terrain is identical in
both arms, so the filter cannot favour either one.

## The finding

**Charging for climbs makes populations die more often, and the effect is not established.**

| | slope-off | slope-on |
|---|---|---|
| extinct | 38 / 60 | 46 / 60 |

Marginal counts are the wrong test on paired data. The discordant pairs are **13 extinct only with
the cost, 5 extinct only without** — McNemar χ² = 2.72 on 1 df, against 3.84 for p = .05.
**Consistent in direction, short of significance.** It wants more seeds, not a stronger claim.

## Everything else in the table is an artefact, and the control says so

Every gene column moved by roughly the same amount, −0.05 to −0.11, at |t| between 1.4 and 2.0 —
**including `neutral_marker`, at −0.064, t = −1.79.** The control gene responds to nothing by
construction. When it moves with the pack, the pack is not moving for genetic reasons: it is
composition. More worlds died in one arm, and a dead world's final gene means are whatever its last
survivors happened to carry.

Two columns crossed |t| = 2 (`water_efficiency` −2.05, `temperature_tolerance` −2.02). Both sit
inside the spread the control occupies. **Neither is a result.**

The same reasoning disposes of the headline `population` figure of −27.5 (t = −3.16): computed across
all 60 pairs, it counts extinct worlds as population zero, so it is the extinction signal wearing
different units rather than evidence that surviving populations are smaller.

## Two things this cost, stated plainly

1. **The cap of 200 made the ecology fragile.** 33 of 60 pairs went extinct in *both* arms, against 2
   and 3 out of 120 at a cap of 48. Raising the ceiling let populations overshoot and crash, so what
   is being measured is a stressed ecology being pushed further rather than a healthy one being
   selected on. A cap between the two is the obvious next condition.
2. **The occupancy metrics are underpowered.** `occupied_elevation` and `occupied_slope` need both
   arms to have survivors, and only 9 pairs did — t = 0.81 and 0.42 on nine pairs says nothing. The
   metric is sound and cost no new simulation state; it needs a condition where populations live.

A population comparison restricted to the 9 both-survived pairs gives −88.6, t = −2.27. It is
reported here for completeness and **should not be used**: conditioning on survival, which the
treatment affects, is exactly the selection bias that makes such comparisons untrustworthy.

## Where this leaves the flag

**Still off.** The honest summary is that slope cost plausibly hurts survival in a stressed ecology,
at 13 discordant pairs against 5, and that nothing has been shown about selection on any gene. The
next run is the same instrument at a cap around 100 with more seeds — enough for populations to
persist and for McNemar to have something to work with.
