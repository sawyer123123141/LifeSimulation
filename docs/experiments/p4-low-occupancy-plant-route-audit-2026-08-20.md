# Low-occupancy audit: mortality and establishment reverse; NutrientUptake remains null

> **Geometry confound.** The 168-site scenario changes both target count and geometry. These results establish a changed operating point, not site occupancy as the sole cause.

## Manipulation and survival first

| Arm | Mean occupancy | Survival |
|---|---:|---|
| mortality off | 0.478 | 0/120 extinct; 0/120 frozen |
| mortality on | 0.332 | 0/120 extinct; 0/120 frozen |
| NutrientUptake off/on | 0.332 / 0.332 | 0/120 extinct; 0/120 frozen in each |
| contest off/on | 0.332 / 0.276 | 0/120 extinct; 0/120 frozen in each |

## Result

Lifespan has headroom at the low-occupancy operating point: with mortality active, `Growth`—which shortens lifespan—moves down (−0.01131, t −2.65, 46/120 up), while the mortality-off control moves +0.00450, t +2.25, 61/120 up. The prior “no headroom” conclusion is scenario-bound.

`SeedlingResilience` reverses: contest on reads −0.01184, t −2.56, 44/120 up versus contest-off drift +0.00245, t +0.58, 68/120 up. With many free sites, establishment protection loses its scarce-site benefit while retaining its dispersal charge.

The tested gated growth trait, `NutrientUptake`, stays null: enabled −0.00369, t −0.91, 61/120 up versus disabled −0.00381, t −0.81, 60/120 up. This supports the capacity-gate account for this trait, but it does **not** re-audit the other five growth-rate traits.

Predeclared predictions: mortality-on `Growth` −0.025/t−3.5/45 up (direction held, magnitude weaker); NutrientUptake null (held); contest-on resilience −0.010/t−1.5/55 up (direction held, stronger). Raw data: `p4-low-occupancy-plant-route-audit-2026-08-20.csv`.
