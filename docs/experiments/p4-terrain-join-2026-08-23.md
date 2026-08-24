# Terrain-driven environment: does the join move any plant conclusion?

**Date:** 2026-08-23
**Raw data:** `p4-terrain-join-2026-08-23.csv` (480 runs)
**Instrument:** `tools/PlantSweep` — committed, unlike the probe the recorded corpus came from.

## Design

120 seeds (42–161), 12,000 ticks, `maximumPopulation` 48, 12 founders, cognition and physiology on,
`IntentUtilityV1`, `ConsumerDefenseCalibrationModerate` (6 active sites plus 18 dispersal targets).
Every full-ecosystem flag on, with two of them made into arms:

- **field:** `flat` (`EnvironmentField.CreateProcedural`) versus `terrain`
  (`terrainDrivenEnvironmentEnabled`, `CreateTerrainDriven`).
- **arm:** `contest-off` versus `contest-on` (`plantEstablishmentContestEnabled` and its invader
  counterpart).

Founders differ from each other in every trait, rotated across 0.30–0.70 so no two traits correlate
across sites.

**Both field arms are run here.** The recorded corpus came from an uncommitted probe, so a
difference measured against *it* could be the terrain join or could be the harness. A difference
between two arms of this sweep can only be the flag.

The flag is live: state hashes differ in every seed (`10809610938925410602` versus
`17976642288762030700` at seed 42, contest-off). Parallel execution is deterministic — a repeat run
reproduces every hash.

## Headline: the join changes no plant conclusion

Paired terrain-minus-flat, per seed, contest-on:

| trait | mean | t | up |
|---|---:|---:|---:|
| Growth | −0.0103 | −1.17 | 47/120 |
| SeedInvestment | −0.0071 | −0.91 | 56/120 |
| **WaterEfficiency** | **−0.0233** | **−2.72** | 45/120 |
| Nutrition | −0.0150 | −1.83 | 56/120 |
| Defense | +0.0012 | +0.12 | 61/120 |
| Dispersal | −0.0058 | −0.74 | 54/120 |
| MoistureTolerance | +0.0223 | +1.54 | 61/120 |
| TemperatureTolerance | −0.0166 | −1.09 | 56/120 |
| NutrientUptake | +0.0127 | +0.95 | 70/120 |
| SeedlingResilience | +0.0019 | +0.16 | 61/120 |
| SeedProductionRate | −0.0056 | −0.67 | 58/120 |

Contest-off is flatter still: no trait reaches |t| = 1.2. **`WaterEfficiency` is the only cell past
|t| = 2 in twenty-two comparisons, which is roughly what twenty-two comparisons produce.** It is not
claimed here.

Survival does not move either. Paired by seed, extinctions are essentially uncorrelated between
fields: contest-on gives 2 both, 12 terrain-only, 6 flat-only, 100 neither — not a difference
(McNemar on 12 versus 6 is not significant), just different worlds failing.

## Why: the join *removes* within-arena climate variation

This is the finding that matters, and it is the opposite of what "terrain drives the environment"
sounds like. Sampled at 1,681 positions across the ±25 arena:

| | flat sd | terrain sd |
|---|---:|---:|
| moisture, seed 42 | 0.240 | **0.050** |
| moisture, seed 71 | 0.240 | **0.037** |
| moisture, seed 161 | 0.283 | **0.005** |
| temperature, seed 42 | 0.182 | 0.166 |
| temperature, seed 71 | 0.201 | **0.099** |
| temperature, seed 161 | 0.189 | **0.014** |
| fertility, seed 42 | 0.195 | 0.213 |
| fertility, seed 161 | 0.134 | 0.157 |

**The arena is 50 units wide, which is 0.1 radian on a 500-unit planet.** Terrain moisture and
temperature vary on continental scales, so across a window that narrow they are nearly constant —
at seed 161 the whole arena spans 0.031 of moisture and 0.050 of temperature. Fertility keeps its
variance only because it keeps its own noise term.

Terrain is also systematically **warmer**: mean temperature 0.75–0.79 against the flat field's
0.39–0.43, because the window sits at a low latitude and the lapse-rate deduction is small at arena
elevations.

So the null result above has a mechanism. The join did not hand selection a different landscape to
act on; it handed it a **more uniform** one, plus a warm offset. That is why nothing moved.

## What this means for the next decision

Two ways to make terrain matter to the simulation, and they are different claims:

1. **Widen what one simulation unit means.** At 1 unit = 1 metre the arena is a hillside. If a unit
   mapped to, say, 100 metres, the same 50-unit arena would span 5 km and cross real climate. This
   changes every recorded distance's meaning — see decision 15 in the handoff.
2. **Add a local band on top of the planetary climate.** Keep the arena at metre scale and let
   terrain set the *mean* while a local noise band supplies within-arena variation. Cheaper, does
   not disturb distances, and is closer to what the flat field already provides.

Neither is chosen here. The measurement says only that **option zero — turning the flag on and
expecting spatial structure — does not deliver it.**

## Two limitations, stated before anyone cites this

**1. This harness is not the recorded corpus's harness.** Its founder scheme is reconstructed, and
it runs with `proceduralEnvironmentFieldsEnabled` on. It reproduces the recorded results in
direction and rough magnitude — `Dispersal` +0.0844, t +13.38, 105/120 against a recorded +0.1119,
t +15.63, 110/120; `SeedInvestment` +0.0595, t +8.95, 93/120 against +0.0872, t +7.10, 91/120; the
`SeedProductionRate` null at −0.0249, t −3.58, **43/120 up against a recorded 43/120** — but it is
not the same instrument. Its own flat arm, not the recorded numbers, is the control for every
terrain claim above.

**2. It has extinctions where the recorded sweep had none.** 8–14 of 120 per cell, against 0/120
recorded. Whatever the recorded probe did differently kept every world alive; this one does not.
Extinct runs still contribute their surviving-plant means up to the point of collapse, and they
occur at similar rates in both fields, so they are not driving the null — but a sweep with 0/120
extinct would be a cleaner instrument than this one.

## Establishment contest, re-measured in passing

Paired on−off on `SeedlingResilience`: **flat +0.0220, t +1.88, 70/120**; **terrain +0.0330,
t +2.97, 70/120**, against a recorded +0.0362, t +3.22, 72/120. The sign and the 70/120 sign count
replicate in both fields; the flat arm's t falls short of the recorded one. The result is not
overturned and not strengthened.
