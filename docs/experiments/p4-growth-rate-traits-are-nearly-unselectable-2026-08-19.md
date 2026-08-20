# Growth-Rate Traits Are Nearly Unselectable; Colonisation Traits Are Not — 2026-08-19

Third document of 2026-08-19, and the one that explains the other two. Adds
`NutrientUptake` and `PlantFertilityAdaptationEnabled`, measures them, and finds the
adaptation term works exactly as designed while barely mattering — for a reason that
applies to **every** plant growth trait in the model.

## What was built

`p4-fertility-binds-the-growth-limit-2026-08-19.md` found fertility binding the growth
`Min` at 82-90% of plant-reachable positions, as the only channel no gene could answer.
This closes that gap:

- **`PlantGenome.NutrientUptake`** — ninth plant trait, mutated on its own stream at trait
  index 8, hashed, and covered by `EveryPlantTraitTransmitsThroughCloneMutated`.
- **`PlantGrowthSystem`** — `fertilityLimit = Min(1, Fertility + (1 - Fertility) * NutrientUptake)`,
  mirroring the moisture and temperature terms.
- **`SimulationConfig.PlantFertilityAdaptationEnabled`** — defaults false, and gates the
  `-.10f` growth **charge** as well as the benefit, so flag-off is byte-identical to the
  world before the gene existed. An unconditional cost would have changed every plant run
  the moment the gene was added.

Pinned by four new tests: transmission, inert-when-disabled, helps-on-poor-soil, and
pure-cost-where-fertility-does-not-bind. 389/389 green.

## The prediction, and its refutation

The term was justified by a quantitative prediction: uptake buys `+14.8%` of growth limit
against `-3.9%` of cost at the margin, but self-limits, because once
`Fertility + (1-Fertility)*uptake` exceeds `moistureAdaptation` the gene buys nothing and
still pays. So the trait should **converge to an interior equilibrium near 0.56 from both
directions** — a stronger claim than "it goes up".

Tested by regressing **final** patch mean on **founder** mean across 120 seeds. Regressing
*delta* on founder mean would be invalid: founder mean appears on both sides with opposite
signs and manufactures a negative slope from nothing. Slope near 1 means the gene is carried
along neutrally; slope near 0 means every seed converges to a common value.

| arm | founder SD | slope | t vs 1 | mean delta | t |
|---|---:|---:|---:|---:|---:|
| control (flag off) | 0.1841 | 0.9995 ± 0.0412 | -0.01 | +0.0016 | +0.21 |
| fertility adaptation | 0.1841 | 0.9419 ± 0.0376 | **-1.55** | +0.0146 | **+2.11** |

0/120 extinct in both arms, ~15 plant generations.

**The convergence prediction is refuted.** Slope 0.94 is not distinguishable from 1. The
control lands on 0.9995 ± 0.04, which is as clean a drift baseline as this project has
produced and confirms the regression has the leverage to detect convergence if it existed.

What survives is a small upward shift, t 2.11, against an inert control at t 0.21. That is
**suggestive and does not clear the bar** — and it *shrank* from +0.0283 (t 2.40) when the
founder design was given more leverage, which is its own warning.

> A first attempt drew founders as six independent Uniform(.2,.8) draws, collapsing the
> founder-mean SD to 0.073 and leaving the slope undetermined (0.87 ± 0.16). The fix was a
> per-seed centre spanning .2 to .8 for between-seed leverage, plus per-site jitter so
> standing variance still exists inside each run. Both spreads are needed and they answer
> different questions.

## Why: the capacity gate damps every growth-rate trait

`growth = GrowthRate * GrowthRateMultiplier * sproutBiomass * (1 - Biomass/Capacity) * limit * deltaTime`.

Measured over 253,242 patch-ticks, 20 seeds, procedural fields, 12,000 ticks:

| quantity | value |
|---|---:|
| mean `(1 - Biomass/Capacity)` gate | **0.1711** |
| patch-ticks at fill >= 0.90 | 70.4% |
| patch-ticks at fill >= 0.99 | **39.8%** |

Patches spend most of their life close to capacity. A trait that changes growth *rate* by
X% therefore changes realised growth by roughly `0.17 * X%`, and by approximately nothing
for the 40% of the time the patch is within 1% of capacity.

**This is a single explanation for every plant-trait null this project has recorded.**
Sorted by the route each trait takes to fitness:

| route | traits | measured selection |
|---|---|---|
| `GrowthRateMultiplier`, gated by `(1 - B/K)` | Nutrition, Defense, WaterEfficiency, MoistureTolerance, TemperatureTolerance, NutrientUptake | null or weak, every one |
| colonisation — `DispersalRange`, `SeedInvestmentFraction` | Dispersal, SeedInvestment | t 14-17 and t 4.8-6.8 |

The traits that move are exactly the traits that do **not** go through the capacity gate.
Dispersal and seed investment act on establishment events, which are not damped by how full
the parent patch is. `Growth` is the partial exception that fits: it feeds both the gated
multiplier *and* `LifespanSeconds`, and it measured weakly positive (t 1.77).

## Consequences

1. **Adding adaptation terms will not make plant growth traits selectable.** Three sessions
   have now tried to find selection on a growth-rate trait by improving its benefit channel
   — defense deterrence, temperature adaptation, and now fertility adaptation. The benefit
   channel was never the binding constraint; the capacity gate is. A fourth attempt of the
   same shape should not be run.
2. **Keep `NutrientUptake` anyway.** It is correct, flag-gated, byte-identical when off, and
   it removes a real asymmetry — fertility was the only channel no gene could answer, and
   that asymmetry was actively misleading. But it should be described as *closing a gap*,
   not as *making tolerance selectable*, which it demonstrably does not.
3. **Selection on plants has to act somewhere other than growth rate.** Establishment
   probability, patch mortality, and seed production are all ungated. This is a design
   decision, not a calibration, and it is the plant-side analogue of the finding that
   defense had no route to fitness at all.
4. **The elevation field is unaffected by this** and remains justified on terrain and P6
   grounds. It was already established that it will not make tolerance genes meaningful.

## Method note

Four hypotheses of mine were refuted by measurement in this session: the generic
realised-growth-cost reading, the "field does not vary where plants live" reading, the
interior-equilibrium prediction here, and — earlier and most embarrassingly — a reported
calibration regression that was a reconstructed config of my own. The one that survived
contact, fertility binding the `Min`, was the one stated with numbers before it was
believed. The capacity gate above was measured rather than asserted for that reason.
