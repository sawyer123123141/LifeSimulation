# Site abundance makes seed production selectable

> **AFFECTED EVIDENCE — 2026-08-22.** This result was measured with both
> `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled` on, before the
> `PlantPatchStore.ReplaceAt` takeover-age defect was fixed in `4cc9a47`. Until that fix, a seedling
> installed by a takeover carried the incumbent's accumulated age and was often aged out within a
> tick or two, which changes plant lifetime accounting directly.
>
> **It has no impact assessment**, because the 168-site scenario it ran on was never committed and
> cannot be recovered — see `p4-168-site-replication-2026-08-22.md` for the recovery attempt and the
> replication condition committed in its place. Treat the conclusions here as unverified on current
> code until that replication is measured. Nothing below has been edited or recomputed.


## Result

This is a conditional positive. Adding inactive food targets reduced mean plant-site occupancy from 0.908/0.904 to 0.332/0.322 (flag off/on), and `SeedProductionRate` then rose +0.02022 at t +4.32, 79/120 seeds up. Its matched flag-disabled drift arm was only +0.00523, t +1.06, 66/120 up. It is therefore selected when sites are abundant; the 91%-occupancy null was an operating-point artifact.

## Predictions made before the run

| Site abundance | Flag | Prediction (verbatim) | Held? |
|---|---|---|---|
| Current 24 | Off | `.90-.93; Δ+.010,t+1.5,70/120` | Held |
| Current 24 | On cost0 | `.90-.93; Δ+.020,t+3.2,68/120` | Held |
| More targets | Off | `.55-.70; Δ+.010,t+1.5,70/120` | Refuted: occupancy 0.332; delta +.00523, t +1.06, 66/120 |
| More targets | On cost0 | `.55-.70; Δ+.040,t+5,80/120` | Refuted in magnitude, held directionally: occupancy 0.322; delta +.02022, t +4.32, 79/120 |

The first 42-site version was deliberately discarded after a preflight: it remained ~0.88 occupied. The definitive abundant scenario used 168 food sites (6 active plus 162 inactive targets), which produced the required large occupancy change.

## Four-arm results

| Sites / flag | Mean occupancy | SeedProductionRate Δ, t, sign test | Dispersal Δ, t, sign test | Survival |
|---|---:|---|---|---|
| 24 / off | 0.908 | +0.00981, +1.51, 70/120 up | +0.07725, +13.94, 108/120 up | 0/120 extinct; 0/120 frozen |
| 24 / on | 0.904 | +0.01953, +3.22, 68/120 up | +0.06726, +13.23, 105/120 up | 0/120 extinct; 0/120 frozen |
| 168 / off | 0.332 | +0.00523, +1.06, 66/120 up | +0.11625, +26.39, 119/120 up | 0/120 extinct; 0/120 frozen |
| 168 / on | 0.322 | +0.02022, +4.32, 79/120 up | +0.12224, +29.59, 120/120 up | 0/120 extinct; 0/120 frozen |

The disabled arm prevents treating a positive t as selection by itself. At 24 sites, 68/120 is less directionally consistent than the 70/120 disabled drift arm: null. At 168 sites, 79/120 exceeds its 66/120 matched drift arm: selected. No extinction confounds the result.

Method: 120 deterministic seeds (42–161), 12,000 ticks, charge 0, and the calibration configuration transcribed from `ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds` (`maximumPopulation = 48`). Raw per-seed data: `p4-site-abundance-seed-production-rate-2026-08-20.csv`.
