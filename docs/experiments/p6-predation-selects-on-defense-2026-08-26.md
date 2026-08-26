# Predation selects — hard, on defense, and against attack

> **MECHANISM WITHDRAWN, RESULT CONFIRMED — same day.**
> `p6-defense-selection-is-robust-and-my-mechanism-was-wrong-2026-08-26.md`. The selection result holds
> in **six cells** (defense t +4.97 to +10.97 across gate, cap and regeneration). **The fertility route
> proposed below is refuted:** the predator cell has **1.8%** of the living under the health gate, not a
> majority — while the herbivore cell at the same gate, with **no combat at all**, has **77.5%**. The
> route is mortality, concentrated on the low-defense tail. Also checked: attack and defense carry the
> **same** maintenance cost, so the asymmetry between them is not a cost difference.

**2026-08-26.** `tools/CreatureSweep --focused 30 500 --regen=2.0 --brake=1.5 --predation --gate=0.45
[--health-recovery] [--kin=off]`, 12,000 ticks, 60 runs per cell.

## The instrument could not see the question

The drift table tracked thirteen genes and **not one of them was a combat gene**. "Does predation
select" was being asked of an instrument that reports body size, vision, urgency and a control — none
of which predation touches. `attack`, `defense`, `aggression` and `diet_specialization` are added here
from the means `SimulationStatistics` already exposed. **`maneuverability` and `fear` are still
invisible: no statistic exposes them**, so this covers four of the six combat genes.

## Result

Drift from founders, predation profile at gate 0.45, both health arms:

| gene | ratchet | health recovery | kin recognition OFF |
|---|---:|---:|---:|
| **defense** | **+0.267, t +10.97** | **+0.221, t +7.68** | +0.182, t +3.67 |
| **attack** | **-0.147, t -3.84** | **-0.173, t -3.59** | **-0.260, t -11.32** |
| aggression | -0.015, t -0.32 | -0.022, t -0.47 | **-0.132, t -3.64** |
| diet specialization | -0.015, t -0.46 | -0.047, t -1.21 | +0.077, t +1.42 |
| risk aversion | -0.027, t -3.37 | -0.031, t -3.91 | -0.028, t -2.28 |
| `neutral_marker` (control) | +0.0002, t +0.04 | +0.0055, t +1.15 | +0.0030, t +0.37 |
| surviving | 25 / 30 | 25 / 30 | **17 / 30** |

**Defense is under the strongest selection this project has measured on an animal gene**, and
**attack is selected against**. Both health arms agree in sign and magnitude, so neither is an artefact
of the health ratchet.

## The kin-recognition arm is the mechanism check

Turn kin recognition off and creatures stop sparing relatives. **Selection against attack more than
doubles** (t -3.84 → **-11.32**), **aggression goes from null to clearly negative** (-0.32 →
**-3.64**), and eight more worlds die (25/30 → 17/30).

That is the expected direction if the cost of attacking is being paid by the attacker's own lineage:
remove the kin brake and aggressive lineages spend their fitness on their relatives. **A prediction
that came out in the right direction on an arm run for a different reason.**

## Why this happens with predation at 1–2% of deaths

`p6-a-survivable-predator-prey-scenario-exists-2026-08-26.md` recorded that predation kills almost
nobody here. It still selects, and the likely route is **fertility, not mortality**: attacks damage
health, health never recovers with the ratchet on, and health is one of the three conditions on the
mate-seeking gate. A creature that loses a fight is not killed — **it is sterilised**.

**Hypothesis, not a result.** The health-recovery arm weakens it a little (defense t 10.97 → 7.68,
which is the right direction if part of the pressure is permanent injury) but does not remove it, so
some of the pressure survives recoverable health. **Separating the two would need the attack-damage
path measured directly**, which nothing currently reports.

## Two cautions on reading the table

- **Only the combat family has standing founder variance.** This profile varies combat traits and
  sets everything else to neutral 0.5, so the non-combat rows start with no lineage lottery at all.
  Their nulls are therefore **much weaker evidence than the combat positives are** — the same
  asymmetry that got `p4-defense-selection-demonstrated-2026-08-18.md` retracted, in the other
  direction. Do not read "predation selects on combat and nothing else" off this table.
- **Ignore the `|mean| vs control` column here.** The control drifted 0.0002 in the ratchet cell, so
  the ratios run to 1,316 and mean nothing. **The t-statistic is the readable one**, and it is a
  consistent direction across 25 seeds, which a lineage lottery does not produce — a lottery widens
  the spread and *lowers* t.
