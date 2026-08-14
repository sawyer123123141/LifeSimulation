# P1 predator removal/reintroduction checkpoint

## Setup

- Five paired seeds: 42–46
- 50 mixed P1 founders, 20,000 ticks each
- Same baseline resource scenario
- Intervention run: at tick 10,000 remove creatures meeting the explicit viable-hunter phenotype rule; at tick 15,000 introduce 10 varied P1 founders
- Control run: same seed and world, no intervention

## Result

| Measure | Uninterrupted mixed run | Removal/reintroduction run |
| --- | ---: | ---: |
| Predation deaths, range | 115–207 | 46–120 |
| Attack hits, range | 1,013–1,674 | 439–1,077 |
| Carcass food, range | 1,587–2,795 | 586–1,523 |
| Final population range | 122–151 | 119–179 |

All intervention runs persisted through the final tick. Removing viable hunters reduced predation mortality, attacks, and carcass-food consumption in every paired seed; reintroducing a small varied hunting cohort restored some pressure without forcing extinction.

## Interpretation

This is causal evidence that the P1 phenotype-defined hunting strategy influences survival and energy flow. The intervention does not use a predator flag: it queries the same diet/aggression feasibility criterion used by `PredationSystem`.

## Remaining P1 work

- Record population time series at a fixed sample interval to check for lagged coupled cycles.
- Add a paired summary for intervention deltas rather than only raw CSV rows.
- Longer runs and additional resource regimes are still needed before claiming a P1 exit gate.
