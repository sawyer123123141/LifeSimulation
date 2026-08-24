# Charging creatures for climbing moves nothing measurable

**2026-08-24. 240 runs, 120 paired seeds, 12,000 ticks each. `tools/CreatureSweep`.**
Corpus: `p6-slope-cost-2026-08-24.csv`, with an `ExperimentManifest` provenance header.

## The question

`slopeMovementCostEnabled` charges a climb as extra distance —
`climb_metres x SlopeClimbCost`, uphill only, with `SlopeClimbCost = 4`. Energy drain is
`basal x dt + distance x bodyMass x 0.5`, so charged distance is a real term in the energy budget,
not a decoration. Does making relief cost something select for anything?

Paired on the seed: same world, same founders, the terrain join on in **both** arms, the slope flag
the only difference. Without elevation the flag is inert by construction, so an arm without the join
would be a comparison of a flag against itself.

## The answer: no, and the control says so

Nothing crosses |t| = 2. Fourteen columns are reported, so one crossing by chance would be expected;
none did.

| column | mean | t |
|---|---|---|
| population | +0.64 | 1.34 |
| energy | +0.0053 | 0.82 |
| movement_speed | +0.0093 | 1.35 |
| temperature_tolerance | +0.0102 | 1.48 |
| water_efficiency | +0.0064 | 1.19 |
| **neutral_marker (control)** | **+0.0041** | **1.10** |

**The control gene is the point.** `NeutralMarker` responds to nothing by construction, and it sits
mid-pack among the columns that supposedly could respond. The largest movers are not distinguishable
from a gene that cannot move for any reason. Extinctions: 2 of 120 with the cost, 3 without.

## Half the corpus had nothing to climb — and it does not change the answer

**58 of 120 pairs were byte-identical** on `ComputeBehaviorHash`. The flag did not perturb those
worlds at all, which raises the obvious worry that the null is dilution rather than a finding.

It is not. Restricting to the **62 pairs that actually diverged** doubles every mean and leaves every
t-statistic unchanged to two decimal places — which is what removing exact zeros does. The signal is
the same signal; there is just less of nothing around it.

Why half: the arena window is 0.1 radian on a coastal centre, and what lands in it varies enormously
by seed. `--relief` measures it:

| seed | elevation range | sd | climb per 25 m traverse |
|---|---|---|---|
| 42 | 20.7 m | 7.2 | 11.5 m |
| 55 | 24.9 m | 7.9 | 22.2 m |
| 71 | 13.6 m | 4.4 | **0.07 m** |
| 100 | 6.4 m | 0.65 | 0.87 m |
| 120 | 19.7 m | 6.2 | 9.1 m |
| 161 | **0.0 m** | 0.0 | 0.0 m |

Seed 161's arena is perfectly flat — ocean floor, which the generator clamps. Seed 71 has 13 m of
range and almost no *climb*, because the relief is one smooth ramp: range is not the quantity the
cost is charged on. **This table is the reason the null is readable**, and it exists because the
terrain join's first null was banked before anyone asked the same question.

## Two limitations, both real

1. **Population is saturated.** 96 of 120 pairs finished at the cap of 48, and 46 of the 62 diverged
   pairs did. A survival effect smaller than the headroom cannot appear. The cap is inherited from
   `tools/PlantSweep` so the two corpora are comparable, and that comparability cost the ability to
   see this.
2. **Nothing measures how far creatures walked.** `SimulationStatistics` has no distance travelled,
   so it is not known whether creatures responded behaviourally — avoided hills, moved less — or
   simply paid more and carried on. Energy trends *upward* with the cost on (t = 0.82), which is the
   direction a behavioural response would produce and also the direction noise produces.

## What this does and does not license

**It does not license turning the flag on.** It licenses saying that at `SlopeClimbCost = 4`, in
these arenas, over 12,000 ticks, with population capped, the cost selects for nothing detectable.

A decisive version needs a scenario built for the question rather than inherited from the plant
corpus: population uncapped so survival can move, resources placed so that reaching them requires
climbing, and a distance-travelled statistic so behaviour is observable rather than inferred. Until
then `slopeMovementCostEnabled` stays off, and this file is the reason.
