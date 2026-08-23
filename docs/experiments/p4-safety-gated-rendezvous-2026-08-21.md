# P4 safety-gated mate rendezvous — 2026-08-21

> **SUPERSEDED IN PART, 2026-08-22.** The survival conclusion here is not a measurement: **all 240
> runs ended at exactly 48**, the population cap, with zero variance, so survival could not have
> differed in either direction. The birth null (285.93 vs 286.77, t +0.93) had real variance and
> stands at this operating point. Re-measured with survival free to move in
> `p4a-rendezvous-headroom-2026-08-22.md`, which finds the gate **does** cut predation deaths
> (t −4.64) — a real effect that does not propagate to survival or birth rate.
>
> The configuration behind this file **could not be recovered** (probe never committed; 81
> candidates tried against the recorded state hash and births, none matched), so the 2026-08-22 run
> is a **new condition**, not a rerun of this one.

## Question

Does declining mate rendezvous while a nearby creature is an actual perceived threat improve
survival, at an observable cost to births?

## Predictions made before the run

| Prediction | Held? |
| --- | --- |
| With the safety gate on, births will be 10–25% lower than gate-off. | Refuted: +0.29%, not lower. |
| With the safety gate on, final survivors will be 0–10% higher. | Refuted: both arms were 48/48 at 12000 ticks. |
| Neither arm will suffer total extinction. | Held: 0/120 extinct in each arm. |

## Design

120 paired seeds (42–161), 12,000 ticks each, `PredationVariation`, `IntentUtilityV1`,
mate selection enabled, and the `WatchableStarterHabitat` scenario. The only experimental
toggle was `SafetyGatedMateRendezvousEnabled`. The gate is false by default. A gate blocks
mate scoring only where the nearest observed creature has positive `PredationSystem.Threat`
intensity and is inside `ThreatFalloffDistance` (10 units); proximity alone is not a threat.

The manipulation check is flee-decision activity: 5,992,644 gate-off and 4,383,031 gate-on.
Both numbers are far from zero, so this is a threat-bearing harness rather than the known
threat-free herbivore calibration. The changed state hashes and the 26.9% fall in flee
decision-ticks also establish that the flag reaches production behaviour.

Raw per-seed data: `p4-safety-gated-rendezvous-2026-08-21.csv`.

## Results

| Arm | Mean births | Final population | Extinct | Flee decision-ticks |
| --- | ---: | ---: | ---: | ---: |
| Gate off | 285.93 | 48.0 | 0/120 | 5,992,644 |
| Gate on | 286.77 | 48.0 | 0/120 | 4,383,031 |

Paired birth difference (on minus off) was +0.842 births/seed, t +0.93, with 57/120 seeds
up and 5 tied. There is no evidence of a birth or survival benefit in this operating point.
The gate is live and safety-scoped, but its ecological effect is near zero under the tested
threat-bearing, population-capped habitat.

## Implementation contract

`SimulationConfig.SafetyGatedMateRendezvousEnabled` defaults to false. With false (including
the constructor default), the added branch is skipped and state hashes remain identical to the
mate-selection baseline. With true, a nearby non-threatening mate remains eligible; a positive
threat within the configured avoidance radius suppresses the mate candidate for that decision.
