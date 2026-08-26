# Defense selection survives every neighbouring cell, and the sterilisation mechanism I proposed is refuted

**2026-08-26.** `tools/CreatureSweep --focused 30 <cap> --regen=<r> --brake=1.5 --predation --gate=<g>`
and `--deaths` in the same configurations, 12,000 ticks. Console artefact:
`p6-predation-robustness-2026-08-26.txt`.

Two checks on `p6-predation-selects-on-defense-2026-08-26.md`, whose entire result sat in **one cell**.

## 1. It is not a knife-edge

`defense` drift from founders, each cell moving one axis off gate 0.45 / cap 500 / regen 2.0:

| cell | surviving | **defense** | attack | control |
|---|---:|---:|---:|---:|
| gate 0.35 | 29 / 30 | **+6.15** | -1.23 | +2.36 |
| **gate 0.45 (origin)** | 25 / 30 | **+10.97** | -3.84 | +0.04 |
| gate 0.55 | 19 / 30 | **+4.97** | -4.02 | +2.68 |
| cap 250 | 25 / 30 | **+10.56** | -3.86 | +0.22 |
| regen 1.75 | 24 / 30 | **+8.33** | -2.68 | +0.84 |
| regen 2.25 | 27 / 30 | **+6.46** | -2.49 | -0.46 |

**Defense is positively selected in all six, t +4.97 to +10.97, and attack is negatively selected in
all six.** The result does not depend on the cell it was found in.

**One caution:** the control drifts to |t| 2.4–2.7 in the two gate-shifted cells, so those are noisier
runs and their defense figures should be read as "several times the control", not at face value. The
three cells where the control sits under 1.0 give defense t 8.3 to 11.0.

## 2. My proposed mechanism is wrong

`p6-predation-selects-on-defense-2026-08-26.md` proposed that predation selects through **fertility**:
attacks damage health, health never recovers, health gates mating, so a loser is *sterilised rather
than killed*. That predicts a large sterile share in the predator cell.

Measured, same configuration, `--deaths`:

| | below the health gate | surviving | births / run | attack hits / run |
|---|---:|---:|---:|---:|
| **predator, gate 0.45** | **1.8%** | 24 / 30 | 146.0 | 6.7 |
| herbivore, gate 0.45 | **77.5%** | 3 / 30 | 682.3 | 0.0 |

**1.8%. The hypothesis predicted the opposite and is refuted.** Almost nobody in the predator cell is
sterilised by injury — and the cell where three-quarters of the living *are* permanently sterile is the
one with **no combat in it at all**, where the sterility comes from starvation during overshoot.

**So the route is mortality after all**, despite predation being 1–2% of deaths: rare but lethal, and
concentrated on the low-defense tail, which is exactly the shape that produces strong selection from a
small death count. Dying removes every future offspring; being 2% likely to die is not a 2% effect on
fitness.

**Checked rather than assumed:** `attack` and `defense` carry the **same** maintenance cost — `0.10`
each at `GenomePhenotype.cs:441-442` — so the asymmetry between them cannot be a cost difference. It
has to come from what combat does.

## 3. The by-product worth keeping

The herbivore row above is the same slack gate that
`p6-the-gate-is-a-survival-mechanism-2026-08-26.md` found lethal, now with the mechanism attached:
**682 births per run, overshoot, starvation damage, 77.5% of the living permanently sterile, 3 worlds
of 30 surviving.** The predator profile at the same gate breeds a quarter as fast and does not
overshoot. **That is why the two profiles want opposite gate settings** — the tension recorded as
unattributed in the gate document, now attributed to breeding rate rather than to predation.

## Status of the mechanism question

- **Selection on defense: robust, six cells.**
- **Route: mortality, not fertility.** The fertility story is withdrawn.
- **Still unmeasured:** the attack-damage path itself. Nothing reports how much health combat removes,
  which is what would let the mortality route be confirmed rather than inferred from the refutation of
  the alternative. **`maneuverability` and `fear` also remain invisible** — no statistic exposes them,
  so two of the six combat genes cannot be tracked at all.
