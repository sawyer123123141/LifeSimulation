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

---

**UPDATE 2026-08-30 — the digestion half of that evidence has now been recorded, and it is
negative.** `docs/experiments/p3-digestion-strategies-2026-08-30.md`. Across four cells including the
project's highest-combat one, `diet_specialization` drift is **t +0.59 / -0.21 / +0.52 / +1.56**
against a neutral control that moves as much (t +1.62 in the last), while `defense` in the same runs
reaches **t +12.71**. Scarcer plants did not make meat-eating pay, and the sign is non-monotone.

Two strategies cannot persist while one of them has almost nothing to eat: predation is **1.1% of
deaths** in the cap-48 predation cell and **8.4%** in the best cell on record. Diet is pure cost below
the `MinimumHuntingDiet = 0.58` threshold and both yield curves are linear, so the plan's
**"generalist cost"** was never built — and building it alone would push the population to the
herbivore end rather than to two ends.

**P4 through P7 were built anyway.** This line was written on 2026-08-13 and no document between then
and 2026-08-30 records the evidence it demanded.
