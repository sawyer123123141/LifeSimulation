# The predation founder fix is necessary and not sufficient — the profile zeroes every trait family but its own

**2026-08-26**, later the same day than
`p6-predation-never-failed-its-founders-cannot-breed-2026-08-26.md`, which diagnosed zero births as
`fertilityInvestment` and `lifespanTendency` falling through to the constructor's `0f`. **The fix was
applied, approved, and it did not restore reproduction.** This records what it did do and where the
remaining blocker sits.

## What the fix changed

`PredationFounderFactory` now sets both reproductive traits. Predation cells re-run, both health arms,
30 seeds each:

| | before the fix | after the fix |
|---|---|---|
| age deaths | 82% | **32%** |
| **health deaths** | 3.9% | **47%** |
| starvation | 4.2% | 6.9% |
| births per run | **0.0** | **0.0** |
| surviving | 0 / 30 | **0 / 30** |

**The founders now live differently and still do not breed.** The death mix moved a long way, so the
change is live and load-bearing; the outcome did not move at all.

**My earlier diagnosis was therefore right about a defect and wrong about the cause.** "The founders
cannot breed because they die at the lifespan floor" is not what the measurement says once the floor
is removed.

## Where the blocker actually sits

Health deaths going from 4% to 47% is the clue. `Phenotype` derives temperature comfort as
`2 + 8 * temperatureTolerance`, and **`PredationFounderFactory` leaves `temperatureTolerance` at 0**,
so every predator founder has the narrowest possible comfort band and takes continuous stress damage
through `NeedsSystem.ApplyTemperatureStress`. Health is a one-way ratchet and one of the three
mate-seeking gate conditions.

Probed by temporarily setting that trait too (**probe reverted, not committed**):

| | fertility+lifespan only | plus temperature tolerance |
|---|---:|---:|
| health deaths | 47% | **5.8%** |
| age deaths | 32% | **83.9%** |
| births per run | 0.0 | **0.6** |
| surviving | 0 / 30 | **0 / 30** |

**Temperature tolerance is the health-death cause and it is not the reproduction cause.** With both
traits set the founders live out full lifespans in comfort and still produce 0.6 births per run,
against **492** for `PhysiologyVariation` in the same world.

## The actual shape of the problem

`PredationFounderFactory` starts from `Genome.Neutral` — which itself passes **six of twenty-four**
traits — and sets only the six combat traits. **Everything else is zero:** memory capacity, memory
retention, learning rate, exploration, temperature tolerance, and (until today) fertility and
lifespan. `PhysiologyFounderFactory` sets every family except combat.

So the profile is not "herbivores plus predation variation". It is **a genome with one family
populated and five zeroed**, and at least two of those zeros are load-bearing. The remaining birth
gap — 0.6 against 492 — has not been attributed and the obvious candidates are the zeroed cognition
family (`exploration` at 0 in particular, for whether founders ever meet) and the combat family's
aggression itself.

## Status

- **The approved fix is committed.** It is correct, it removes a real defect, and it is **not enough
  to make a predator-prey scenario testable.**
- **Predator-prey remains unadjudicated**, and so do `multiThreatPerceptionEnabled` and
  `kinRecognitionEnabled` — the 2026-08-19 position is unchanged, for a better-understood reason.
- **The next step is a decision, not a measurement:** either the profile populates the remaining
  families (which makes it "physiology variation plus combat variation" and is a larger behaviour
  change), or a new founder profile is built for predator-prey deliberately. **Not taken unilaterally.**
- Eleven pinned tests moved with the fix. Ten shared one behaviour hash which was rederived to a
  single new value — they still agree with each other, which is the property they exist to assert —
  and `PredationFounderProfileSeedsUnlabeledPredationVariation` had pinned `FertilityInvestment == 0`
  as a side effect of asserting family isolation; it now asserts the two traits are set while keeping
  the isolation checks that are its actual subject. Same rederivation convention as the recorded
  `Persistence` shift.
