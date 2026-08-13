# P0 resource-pressure calibration — 2026-08-13

This is calibration evidence, not a P0 evolution claim. All runs used the deterministic pure simulation with 50 paired founders and 20 seeds (`42`–`61`).

## Rejected configurations

- The original population-scaled baseline (`3` regeneration per resource) reached the 1,000-creature safety cap in long runs. It cannot be used as final evidence.
- The original drought and food-scarcity settings (`0.75` regeneration for the constrained resource) often produced identical outcomes to baseline: the renewable budget still sustained the tested populations.
- Extreme scarcity (`0.10` constrained-resource regeneration) created a broad bottleneck and correlated multi-gene shifts. It is unsuitable for claiming a trait-specific mechanism.

## Current cap-safe calibration

| Scenario | Food regeneration | Water regeneration | Cap contacts in 20 × 100,000-tick runs |
| --- | ---: | ---: | ---: |
| Baseline | 0.75 | 0.75 | 0 |
| Drought | 0.75 | 0.25 | 0 |
| Food scarcity | 0.25 | 0.75 | 0 |

With 50 founders, the baseline/drought comparison produced a mean water-efficiency shift of `+0.2284`, standardized paired effect `1.10`, and bootstrap interval `[+0.1395, +0.3177]`, but only 60% of pairs had the same direction. This identified founder drift as the remaining source of noise.

The cap-safe 100-founder rerun (20 seeds, 100,000 ticks, 1,500 maximum population) produced a mean water-efficiency shift of `+0.3134`, standardized paired effect `2.66`, direction consistency `100%`, and bootstrap interval `[+0.2642, +0.3634]`, with zero cap contacts. It satisfies the statistical part of the P0 drought criterion. The causal mechanism is the existing water-efficiency phenotype: lower hydration loss at a higher maintenance-energy cost.

Food scarcity reduced population and resource flow but did not produce a clean, causally attributable food-efficiency shift. It remains an open biology-design problem.

## Next evidence work

1. Add causal, trait-isolated fixtures for water and food physiology before modifying selection equations.
2. Re-run the calibrated paired suite after each demonstrated correction.
3. Freeze P0 only if a treatment meets every statistical criterion and has a measured survival/reproduction mechanism.
