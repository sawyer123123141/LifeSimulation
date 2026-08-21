# Low-occupancy re-audit: five remaining growth-rate traits stay null

> **Geometry confound.** The 168-site scenario changes both site count and spatial geometry. It establishes this operating point, not occupancy as the sole cause.

## Manipulation and survival

All arms held mean occupancy at 0.322–0.333. Every arm had 0/120 animal extinctions and 0/120 frozen plant runs.

## Result

The five remaining traits routed through `PlantPhenotype.GrowthRateMultiplier` remain null at low occupancy, against a disabled `SeedProductionRate` drift arm (+0.00523, t +1.06, 66/120 up):

| Trait | Delta | t | Sign test |
|---|---:|---:|---:|
| Nutrition | -0.00381 | -0.98 | 55/120 up |
| Defense | +0.00230 | +0.57 | 61/120 up |
| WaterEfficiency | +0.00449 | +1.00 | 62/120 up |
| MoistureTolerance | +0.00245 | +0.55 | 59/120 up |
| TemperatureTolerance | +0.00192 | +0.48 | 62/120 up |

None is more directionally consistent than drift. The gate conclusion therefore survives the low-occupancy re-audit. `Growth` is excluded from that conclusion: it declines at t -2.65 because it also sets lifespan, the separately measured mortality reversal.

Prediction: every trait would remain null (|t| < 2, 50–70/120 up). Held for all five. Raw data: `p4-low-occupancy-growth-trait-reaudit-2026-08-20.csv`.
