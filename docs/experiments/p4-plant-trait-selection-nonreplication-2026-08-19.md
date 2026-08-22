# Plant Tolerance Selection Does Not Replicate; Dispersal Does — 2026-08-19

> **REVALIDATED ON FIXED CODE — 2026-08-22.** The Dispersal positive control reproduces at +0.1119, t +15.63, 110/120 (recorded t +14 to +19.6, 105-119/120), and SeedInvestment at +0.0872, t +7.10, 91/120 (recorded t +4.8 to +6.8). Measured over 120 seeds with varying founders
> after `4cc9a47`: see `p4-postfix-revalidation-2026-08-22.md`. The banner below is retained for the
> record; the conclusion it questioned has been re-measured and holds.


> **AFFECTED EVIDENCE — 2026-08-22.** This carries the project's plant positive control (Dispersal at t +14 to +19.6), which every other plant null is read against, so its revalidation is load-bearing for the whole corpus. This document's runs used both
> `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled`, so they are on the path the
> `PlantPatchStore.ReplaceAt` takeover-age defect changed (fixed in `4cc9a47`): before the fix, a
> seedling installed by a takeover carried the incumbent's accumulated age and was frequently aged
> out within a tick or two.
>
> Revalidation on fixed code is tracked in `p4-postfix-revalidation-2026-08-22.md`. Until it lands,
> treat the figures here as unverified on current code. Nothing below has been edited or recomputed.


Follow-up to the unwritten 2026-08-18 measurement recorded in `docs/SESSION_HANDOFF.md`
open item 1: procedural environment fields were reported to move both
`MoistureTolerance` and `TemperatureTolerance` by **-0.086** (t -2.45 and -2.57, CIs
excluding zero) against a flat-environment control showing nothing (t 0.29, 0.64). Four
comparisons put the Bonferroni bar near 2.6, both sat just under, and it was deliberately
not written up.

**Result: the tolerance effect does not replicate at n=120, at either of two founder
variances. Two other traits move very hard, in every arm, and they are the ones with no
growth-rate cost at all.**

## Configuration, transcribed

Six active plant sites and eighteen dormant ones, geometry copied verbatim from
`Prototype4Scenarios.CreateConsumerDefenseCalibrationScenario`. Founder placement
`(-12, -8)`. 12 founders, **`maximumPopulation: 48`**, 12,000 ticks, seeds 42-161.

Config is `CreatePrototype4Defaults` plus `plantSiteCompetitionEnabled`,
`plantMortalityEnabled` and `plantTemperatureAdaptationEnabled`, with
**`proceduralEnvironmentFieldsEnabled` as the only flag that differs between arms.**
`plantDefenseDeterrenceEnabled` and `plantQualityPreferenceEnabled` are off, so `Defense`
and `Nutrition` are pure growth-rate costs here.

Founder genomes are the calibration genome `(.55, .5, .5, .65, .3, .5, .5, .5)` plus
Uniform(-.15, +.15) per trait, drawn independently per site per seed. The `+wideTolerance`
arms instead draw **only** `MoistureTolerance` and `TemperatureTolerance` from
Uniform(.2, .8), leaving every other trait at the calibration operating point.

Delta is the unweighted final patch mean minus the founder mean, per seed.

### The control arm is not a flat environment

`SimulationWorld.cs:62-64`: with `plantCohortsEnabled` on — which `CreatePrototype4Defaults`
sets — the non-procedural path builds `EnvironmentField.CreateMoistureGradient()`, not a
constant field. **Moisture already varies in the control**; only fertility and temperature
are pinned at 1. The arms are labelled `moistureGradient` and `procedural` here for that
reason, and flipping the flag changes three things at once: moisture ramp to domain-warped
fBm, fertility 1 to varying, temperature 1 to varying. It is not a one-variable contrast,
but it is the only contrast the flag offers.

## Survival, reported first

| arm | extinct | plantless | plant generations (mean / min) | realized grazing pressure |
|---|---:|---:|---:|---:|
| `moistureGradient` | 0/120 | 0/120 | 15.4 / 12 | 0.0047 |
| `procedural` | 0/120 | 0/120 | 14.7 / 11 | 0.0056 |
| `moistureGradient+wideTolerance` | 0/120 | 0/120 | 15.6 / 12 | 0.0048 |
| `procedural+wideTolerance` | 1/120 | 1/120 | 14.4 / 0 | 0.0055 |

Eleven to sixteen plant generations of turnover in every arm. The tolerance nulls below are
genuine nulls, not "nothing had time to happen". The one collapsed seed in
`procedural+wideTolerance` is excluded as NaN, giving n=119 for that arm.

## Results

`d` is mean delta, `up` the count of seeds with a positive delta.

| arm | trait | growth-rate cost | d | t | 95% CI | up |
|---|---|---:|---:|---:|---|---:|
| `moistureGradient` | Dispersal | none | +0.1121 | +16.80 | [+0.0990, +0.1252] | 115/120 |
| `procedural` | Dispersal | none | +0.0981 | +14.16 | [+0.0845, +0.1117] | 110/120 |
| `moistureGradient+wideTolerance` | Dispersal | none | +0.1253 | +17.21 | [+0.1110, +0.1395] | 113/120 |
| `procedural+wideTolerance` | Dispersal | none | +0.0976 | +14.10 | [+0.0840, +0.1111] | 105/119 |
| `moistureGradient` | SeedInvestment | none | +0.0387 | +5.01 | [+0.0236, +0.0539] | 82/120 |
| `procedural` | SeedInvestment | none | +0.0467 | +6.80 | [+0.0332, +0.0601] | 84/120 |
| `moistureGradient+wideTolerance` | SeedInvestment | none | +0.0471 | +5.75 | [+0.0310, +0.0631] | 81/120 |
| `procedural+wideTolerance` | SeedInvestment | none | +0.0360 | +4.83 | [+0.0214, +0.0506] | 81/119 |
| `moistureGradient` | MoistureTolerance | -.10 | -0.0141 | -1.79 | [-0.0295, +0.0013] | 51/120 |
| `procedural` | MoistureTolerance | -.10 | +0.0006 | +0.08 | [-0.0134, +0.0145] | 55/120 |
| `moistureGradient+wideTolerance` | MoistureTolerance | -.10 | -0.0049 | -0.39 | [-0.0299, +0.0200] | 58/120 |
| `procedural+wideTolerance` | MoistureTolerance | -.10 | -0.0146 | -1.20 | [-0.0385, +0.0093] | 53/119 |
| `moistureGradient` | TemperatureTolerance | -.10 | +0.0025 | +0.33 | [-0.0122, +0.0172] | 60/120 |
| `procedural` | TemperatureTolerance | -.10 | +0.0021 | +0.29 | [-0.0122, +0.0165] | 65/120 |
| `moistureGradient+wideTolerance` | TemperatureTolerance | -.10 | -0.0033 | -0.25 | [-0.0296, +0.0230] | 59/120 |
| `procedural+wideTolerance` | TemperatureTolerance | -.10 | +0.0105 | +0.82 | [-0.0146, +0.0355] | 61/119 |
| `procedural` | Growth | +.90 benefit | +0.0130 | +1.77 | [-0.0014, +0.0274] | 63/120 |
| `procedural` | Nutrition | -.18 | -0.0065 | -0.83 | [-0.0219, +0.0089] | 54/120 |
| `procedural` | WaterEfficiency | -.08 | -0.0078 | -1.05 | [-0.0226, +0.0069] | 52/120 |
| `procedural` | Defense | -.15 | -0.0102 | -1.32 | [-0.0254, +0.0050] | 51/120 |
| `procedural+wideTolerance` | Defense | -.15 | -0.0225 | -3.04 | [-0.0370, -0.0080] | 48/119 |

Full per-run and summary tables: `p4-plant-trait-selection-2026-08-19.csv` and
`p4-plant-trait-selection-summary-2026-08-19.csv`.

## Three conclusions

### 1. The -0.086 tolerance result does not replicate

Under procedural fields both tolerances sit at zero: +0.0006 (t 0.08) and +0.0021 (t 0.29),
with CIs of [-0.013, +0.015] and [-0.012, +0.016]. **-0.086 lies far outside both.** Sign
counts are at chance in all four arms (51-65 of 120).

The obvious objection was answered rather than argued: response to selection is proportional
to standing variance, so a null measured at one founder spread does not carry to another.
The `+wideTolerance` arms roughly double the outcome SD (0.086 to ~0.14), confirming the
wider founders reached the measurement — and the tolerances stay null there too, with
-0.086 still outside both CIs.

This is scoped to the configuration above. The original measurement's configuration was not
recorded, so this is a **non-replication under a documented configuration, not proof the
original number was wrong.** Anyone reviving it should record the config first.

### 2. The "generic realized growth cost" hypothesis is also refuted

This sweep was built to test a specific alternative reading. `PlantGrowthSystem.Step` takes
the growth limit as `Min(moistureAdaptation, Fertility, temperatureLimit)`, and
`PlantPhenotype` charges every cost gene against growth *rate*, which is unrealized while
patches sit at logistic capacity. Procedural fields depress that limit below 1, so the
prediction was that they un-saturate the patches and realize the cost of **every**
cost-bearing gene at once — with `Nutrition` (-.18) and `Defense` (-.15) falling harder than
the two tolerances (-.10 each). Both tolerances carrying an identical `-.10f` also explained
a suspiciously identical -0.086 on both.

**That prediction failed.** The cost genes barely move, and they move *less* under procedural
fields than under the gradient, not more: `Nutrition` -0.012 to -0.007, `WaterEfficiency`
+0.007 to -0.008. Whatever the growth limit is doing, it is not converting cost coefficients
into measurable selection here.

### 3. Dispersal and SeedInvestment are a positive control

`Dispersal` moves **+0.098 to +0.125, t 14.10 to 17.21, 105-115 of 120 seeds up**, and
`SeedInvestment` **+0.036 to +0.047, t 4.83 to 6.80, 81-84 of 120 up** — in every arm,
including both control arms. These are the only two traits with **no growth-rate cost term
in `PlantPhenotype`**, and they are precisely the traits that determine colonization success
under `plantSiteCompetitionEnabled`.

This matters beyond the tolerance question. The P4 blocker has repeatedly asked for proof
that the setup can detect selection at all, and previous answers were fragile: the 2026-08-18
defense decline was retracted, and the one arm that moved defense did so while 30/30 seeds
lost their animal population. **This one is not fragile.** It is an order of magnitude past
any Bonferroni bar this project has used, it reproduces across all four arms, its sign test
is overwhelming, and it comes from runs with 0/120 extinctions and 15 plant generations. Any
future plant-trait null can be read against it: the pipeline detects selection when selection
is there.

## Not claimed

`Defense` under `procedural+wideTolerance` is -0.0225, t -3.04, CI [-0.0370, -0.0080], 71 of
119 seeds down (sign test p ~ 0.04). **This is not claimed as a result.** It is one cell of
32; the Bonferroni bar for 32 comparisons sits near t 3.2 and it is under. `Defense` founders
were not widened in that arm, so there is no reason for it to appear there and not in
`procedural`, where the same gene gives t -1.32. A claim of exactly this shape was retracted
on 2026-08-18. It is recorded here as an open thread, not a finding.

## Method notes

- The first version of this probe reconstructed a config that "looked equivalent" instead of
  transcribing the measured one, passing `baseline.MaximumPopulation` (**1000**) where the
  committed regression guard pins **48**. That produced ~310 animals, stripped the patches,
  and gave 30/30 extinct on a scenario whose own comment records 0/30. Transcribing the cap
  restored 0/30 exactly. The recorded calibration is intact; the reconstruction was not.
- The eight traits were measured in the **same** runs, so `Growth`, `SeedInvestment` and
  `Dispersal` act as internal controls at no extra cost in arms.
- Sign tests are reported alongside every t. They caught one over-read: `Defense` under
  `moistureGradient` has t -2.05 but only 50/120 seeds down, so that t is magnitude-driven
  and carries no directional consistency.
- Both re-runs reproduced bit-identically, including after adding
  `HighestPlantGeneration` to the recorded columns.
