# P3 physiology checkpoint — 2026-08-13

## Implemented physiology slices

- Temperature: deterministic spatial/seasonal field, inherited tolerance, health pressure, and allocation-free thermal-comfort seeking.
- Life history: inherited fertility investment trades higher reproductive energy cost for a shorter cooldown; lifespan tendency trades higher maintenance for a longer maximum age. Both are P3-gated.
- Digestion: plant-food nutritional quality changes energy recovery without changing biomass consumed. Existing diet specialization changes plant versus meat return.

P3 founders use `PhysiologyVariation`: centered variation in P0 core traits plus broad cognition/physiology variation, with attack traits at zero. This prevents P1 predation from masking physiology experiments.

## Nutrition smoke evidence

Configuration: five paired seeds (42–46), 50 founders, 4,000 fixed ticks, P3 enabled, `p3-plant-nutrition-poor` (0.5x nutrition) versus `p3-plant-nutrition-rich` (1.5x nutrition).

| Seed | Poor final population / births | Rich final population / births |
| --- | ---: | ---: |
| 42 | 75 / 130 | 335 / 395 |
| 43 | 69 / 108 | 297 / 354 |
| 44 | 64 / 119 | 360 / 396 |
| 45 | 64 / 116 | 299 / 380 |
| 46 | 74 / 109 | 325 / 389 |

The treatment changed only plant nutritional return and increased final population and births in all five paired seeds. This is a calibration checkpoint, not a claim of a completed evolutionary-selection result.

## Remaining P3 exit evidence

- Run larger paired sweeps for thermal selection and high/low mortality life-history treatments.
- Demonstrate two persistent strategies exploiting different conditions, rather than a single higher-fitness condition.
- Verify Unity-mode behavior and benchmark P3 throughput against P0/P1 baselines.

The simulation implementation is ready for these experiments; P4 remains blocked until this evidence is recorded.
