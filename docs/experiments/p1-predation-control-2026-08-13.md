# P1 predator-prey control checkpoint

## Setup

- Five paired world seeds: 42–46
- 50 founders, 20,000 simulation ticks per run
- Same baseline plant/water scenario in both conditions
- `prey-only`: Prototype 1 founder profile; predation traits remain dormant
- `mixed-predation`: varied attack, defense, maneuverability, fear, aggression, and diet genes; no predator label exists in simulation state

## Result

| Measure | Prey-only | Mixed-predation |
| --- | ---: | ---: |
| Final population range | 213–260 | 122–151 |
| Birth range | 801–1,104 | 500–614 |
| Predation deaths | 0 | 115–207 |
| Attack hits | 0 | 1,013–1,674 |
| Carcass food consumed | 0 | 1,587–2,795 |

All mixed-predation runs persisted through 20,000 ticks after threat scoring was restricted to creatures with a viable hunting strategy. Before that correction, the same runs collapsed because ordinary nearby creatures triggered costly flee behavior.

The final mixed populations retained substantial attack, defense, aggression, and diet-specialization values, while those traits drifted close to zero in prey-only control runs. This is evidence that hunting, carcass feeding, and threat response are materially active in the P1 configuration.

## What this does not prove yet

- It does not establish repeatable long-term predator/prey population cycles.
- It does not separate every causal contribution of plant scarcity, predation mortality, and P1 trait maintenance costs.
- It is a control checkpoint, not the final P1 evidence gate.

The next P1 experiment should add longer paired runs and a predator-removal/reintroduction schedule while preserving these control results.
