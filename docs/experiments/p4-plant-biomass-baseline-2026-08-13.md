# P4 Plant Biomass Baseline — 2026-08-13

## Purpose

Validate the first P4 compatibility milestone: food resources can be projections of authoritative plant-cohort biomass while animal simulation continues to use stable food resource IDs.

## Configuration

- `SimulationConfig.CreatePrototype4Defaults`
- `Prototype4Scenarios.PlantBackedBaseline`
- 4 initial creatures; 2 plant-backed food cohorts; 2 water sources
- 2,000 fixed ticks per seed
- Seeds: 42, 43, 44

## Results

| Seed | Final population | Plant biomass | Plant growth | Plant biomass consumed | Residual |
| --- | ---: | ---: | ---: | ---: | ---: |
| 42 | 1 | 19.6481 | 8.4480 | 12.7998 | -0.000038 |
| 43 | 1 | 20.9658 | 10.1667 | 13.2009 | 0.000065 |
| 44 | 2 | 11.8988 | 18.8143 | 30.9155 | 0.000034 |

All three residual magnitudes are below `0.0001`, the declared numerical tolerance. The 111-test EditMode suite passed before the smoke test. No plant cohorts became dormant in this short baseline.

## Interpretation

This proves resource compatibility and biomass accounting, not plant evolution or coevolution. Food has living producer-side state and animal consumption reduces that state, while current animal targeting and memory still operate through unchanged resource IDs.

## Next P4 slices

1. Plant genomes, lineage/generation data, trait trade-offs, and clonal mutation.
2. Seed budgets, deterministic dispersal, establishment, and biomass-conserving patch competition.
3. Plant defense/nutrition versus animal digestion experiments, then rainfall and spatial-disturbance controls.
