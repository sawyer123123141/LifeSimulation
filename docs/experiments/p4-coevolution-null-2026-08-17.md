# P4 Coevolution — Powered Null Result and Root Cause — 2026-08-17

> **AFFECTED EVIDENCE — 2026-08-22.** This is a powered null measured on the calibration. This document's runs used both
> `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled`, so they are on the path the
> `PlantPatchStore.ReplaceAt` takeover-age defect changed (fixed in `4cc9a47`): before the fix, a
> seedling installed by a takeover carried the incumbent's accumulated age and was frequently aged
> out within a tick or two.
>
> Revalidation on fixed code is tracked in `p4-postfix-revalidation-2026-08-22.md`. Until it lands,
> treat the figures here as unverified on current code. Nothing below has been edited or recomputed.


Supersedes `p4-consumer-defense-calibration-2026-08-13.md`, which could not
interpret its own result because every run went extinct. Extinction was fixed
on 2026-08-16 (plant growth-rate conversion). This is the first calibration run
where populations actually survive, so it is the first one that measures
anything.

## Configuration

- `SimulationConfig.CreatePrototype4Defaults`, 12 founders, `maximumPopulation: 48`
- `cognitionEnabled`, `physiologyEnabled`, `IntentUtilityV1`, `plantCohortsEnabled`,
  `plantSiteCompetitionEnabled`
- `Prototype4Scenarios.ConsumerDefenseCalibrationControl` (plant defense 0.0)
  vs `...Moderate` (plant defense 0.3)
- **30 seeds** (42–71), 12,000 ticks each — previous runs used 5 seeds
- Bootstrap: 2,000 resamples

## Results

| Measure | n | mean diff | direction | effect | 95% interval | meets criterion |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| Treatment — FoodEfficiency | 30 | +0.0154 | 0.57 | +0.218 | [-0.0075, +0.0416] | No |
| Placebo — Commitment | 30 | +0.0004 | 0.57 | +0.020 | [-0.0063, +0.0069] | No |
| Reciprocal — PlantDefense | 30 | +0.2858 | 1.00 | — | — | — |

### The placebo validates the statistics pipeline

`Commitment` is inherited, mutated, hashed, and reported, but **no behavior
system reads it** (verified by exhaustive grep). Selection therefore cannot act
on it, so its measured difference is pure drift by construction. It came back at
effect +0.020 with a tight interval around zero — exactly what a correct pipeline
should report for a null. The analysis machinery does not manufacture false
positives, so its "no signal" verdict on FoodEfficiency can be trusted.

### The earlier n=5 result was noise, not a near-miss

A 5-seed run had reported effect **-0.70** with direction consistency 0.80, which
was interpreted at the time as a real signal that was merely underpowered. At
n=30 the effect regressed to **+0.218 and flipped sign**, with direction
consistency 0.57 — statistically indistinguishable from the placebo's 0.57. The
n=5 result was sampling noise. Recorded explicitly so it is not cited later as
supporting evidence.

### PlantDefense did not evolve; it stayed where it started

The +0.2858 difference is not a response — it is the initial condition surviving.
Control starts at defense 0.0 and ends near 0.007; Moderate starts at 0.3 and ends
near 0.296. Neither moved. (Control's small upward drift from exactly 0.0 is the
clamp-boundary artifact: symmetric mutation against a floor at 0 truncates
negative draws and keeps positive ones.)

## Root cause: plants have no generational turnover

Generation counts, `ConsumerDefenseCalibrationModerate`:

| ticks | seed | animal generations | **plant generations** | final pop |
| ---: | ---: | ---: | ---: | ---: |
| 12,000 | 42 | 12 | **2** | 48 |
| 12,000 | 43 | 9 | **2** | 48 |
| 12,000 | 44 | 9 | **2** | 48 |
| 48,000 | 42 | 27 | **2** | 3 |
| 48,000 | 43 | 22 | **2** | 48 |
| 48,000 | 44 | 27 | **2** | 48 |

Animals turn over normally and scale with run length. **Plants reach generation 2
and stop permanently** — quadrupling the run length adds zero plant generations.

Two hypotheses were tested and rejected before the real cause was found:

1. *Statistical power.* Rejected — the n=30 run above is adequately powered and
   the effect vanished rather than sharpening.
2. *Patches never re-reach the 75% maturity threshold under grazing.* Rejected by
   direct measurement — 5 to 7 of 8 patches sit at or above 75% capacity at every
   sampled tick from 3,000 to 24,000, many at 100%. Plants are mature and eligible
   to reproduce the entire time.

The actual mechanism:

- The scenario has a **fixed pool of 8 sites** (2 initially active + 6 dispersal
  targets). `PlantReproductionSystem.FindSite` can only place offspring into
  registry sites.
- All 8 are colonized by generation 2, and regrowth (`RegenerationPerSecond = 12`)
  outpaces grazing, so occupants stay at 75–100% capacity.
- Dispersal needs an **inactive** site — none remain. Site competition needs a
  resident **below 25%** biomass — none qualify, since they sit far above it.
- `PlantPatchStore` has **no removal method**, and since the 2026-08-16 sprout-floor
  fix a fully-grazed patch regrows from a 1%-of-capacity floor instead of staying
  at zero. Patches are therefore effectively immortal: a site, once occupied, is
  never released.

So the plant layer is not an evolving population. It is 8 permanent patches whose
genomes were fixed at generation 2. Plant defense cannot be selected on because
plants neither die nor are born. Coevolution is structurally impossible here — no
amount of tuning defense values, population caps, or run length can produce it.

## What this means

**Plant mortality is the missing mechanic**, not plant competition and not stronger
defense. Site competition (added 2026-08-16) was aimed at the right problem but
cannot fire: it only contests residents below 25% biomass, and healthy occupants
never fall that low.

P4's exit gate ("repeatable reciprocal plant/consumer trait response") is not close,
and adding further P4 mechanics will not move it until plant demography exists —
patches that can die and free their site, letting the population turn over.

## Reproduction

Throwaway NUnit probes under `Assets/Tests/EditMode/` driving `ExperimentRunner.Run`
plus a direct `SimulationWorld` stepping loop for the maturity measurement; deleted
after use per project convention. All numbers above are from those runs against
commit `20b8511`.
