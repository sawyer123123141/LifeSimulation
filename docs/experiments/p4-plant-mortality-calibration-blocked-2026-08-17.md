# P4 Plant Mortality — Mechanism Works, Calibration Blocked on a Memory Defect — 2026-08-17

Follows `p4-coevolution-null-2026-08-17.md`, which found that plants reach
generation 2 and stop reproducing forever because sites are never released.
Age-based plant mortality was designed and built to fix that
(`docs/superpowers/specs/2026-08-17-plant-mortality-design.md`).

## The mechanism works

With `PlantMortalityEnabled` on, plants reach **10-11 generations** in a
12,000-tick run instead of freezing at 2. The generation-2 blocker is genuinely
broken. The implementation is merged and reviewed clean at 351/351 tests, with
its flag defaulting off.

## The calibration is blocked

`BaseLifespanSeconds` could not be calibrated. Both constraints — at least 8
plant generations, and zero animal extinctions across seeds 42-71 — were never
satisfiable at the same time. The two move together monotonically across an 8x
range rather than trading off:

| BaseLifespanSeconds | min plant generations | animal extinctions (of 30) |
| ---: | ---: | ---: |
| 90 | 10 | 30 |
| 180 | 5 | 30 |
| 360 | 3 | 8 |
| 450 | 3 | 2 |
| 540 | 2 | 0 |
| 720 | 1 | 0 |

By the time extinctions reach zero, turnover has collapsed back to the freeze
this work exists to fix.

## Three hypotheses tested and rejected

Recorded because each is a plausible-sounding explanation that the evidence
kills, and re-proposing them later would waste time.

1. **Patches never re-reach the 75% reproduction maturity threshold under
   grazing.** Rejected by direct measurement: 5 to 7 of 8 patches sit at or
   above 75% capacity at every sampled tick from 3,000 to 24,000, many at 100%.

2. **Mortality destroys biomass, so faster turnover starves the consumers.**
   Rejected by measuring the conservation residual with mortality on and off at
   matched tick counts. The residual is *smaller* with mortality enabled
   (0.00079 vs 0.00150 at 6,000 ticks; relative 3.0e-6 vs 5.7e-6). Mortality
   biomass is correctly accounted, and standing food remains abundant while
   animals starve.

3. **Water is co-located only with the two original food sites, so animals
   tethered to water starve when the adjacent patch dies.** Rejected by adding
   water beside all six dispersal targets and re-running: extinctions moved only
   30 to 29 at lifespan 90, 30 to 27 at 180, 8 to 2 at 360. A real but marginal
   effect, not the cause. The change was reverted rather than banked.

## Root cause: place memory has no invalidation for vanished resources

A/B on the cognition flag, 30 seeds, `ConsumerDefenseCalibrationModerate`,
12,000 ticks, `BaseLifespanSeconds = 90`, everything else identical:

| cognition | animal extinctions (of 30) | min plant generations |
| --- | ---: | ---: |
| enabled | **29** | 10 |
| disabled | **14** | 11 |

Disabling place memory halves the extinction rate while leaving plant turnover
unchanged. Memory is actively reducing survival.

Plant mortality is the first mechanic in this project that makes a remembered
location **permanently invalid**. Resources previously depleted and regrew, but
the place itself always remained real, so the memory system has never required
an invalidation path for a location that is simply gone. Supporting measurement:
at lifespan 90 the animals die with 28 starvation deaths, 0 dehydration deaths,
and roughly 288 units of food standing available elsewhere in the world — they
starve beside food, not for lack of it.

This is a latent defect that mortality **exposed rather than caused** — the same
shape as the three halfway-wired fields found earlier the same day
(`Persistence` consumed but never inherited, `Commitment` inherited but never
consumed, plant `Age`/`SeedReserve` allocated but never written). Each was
invisible until something finally exercised its path.

Note that 14 of 30 seeds still go extinct with memory disabled, so memory is the
dominant factor but not the only one. The calibration scenario is genuinely
harsh once patches start dying.

## Consequence

Calibration numbers gathered before the memory defect is fixed are measuring the
memory bug, not the lifespan. `BaseLifespanSeconds` remains at its placeholder
`90f`, explicitly uncalibrated, with the flag defaulting off so nothing in the
existing suite is affected.

Order of work: fix place-memory invalidation first, then re-run this
calibration, then re-run the coevolution experiment.
