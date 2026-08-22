# P4 — Defense Decline Is Grazing-Driven: Dose-Response — 2026-08-18

> **AFFECTED EVIDENCE — 2026-08-22.** Its standing conclusion (defense decline is grazing-driven) rests on calibration runs. This document's runs used both
> `PlantSiteCompetitionEnabled` and `PlantMortalityEnabled`, so they are on the path the
> `PlantPatchStore.ReplaceAt` takeover-age defect changed (fixed in `4cc9a47`): before the fix, a
> seedling installed by a takeover carried the incumbent's accumulated age and was frequently aged
> out within a tick or two.
>
> Revalidation on fixed code is tracked in `p4-postfix-revalidation-2026-08-22.md`. Until it lands,
> treat the figures here as unverified on current code. Nothing below has been edited or recomputed.


Confound removal for `p4-defense-selection-demonstrated-2026-08-18.md` (whose significance claim
was retracted) and `defense-drift-report.html`.

## The question

Plant defense costs growth (`-.15f * Defense`) whether or not anything eats it. So the observed
decline might be entirely a growth cost with no grazing contribution — in which case coevolution is
impossible at these settings regardless of statistical power, and spending compute on more seeds
would be wasted.

## Design

Mixed founders throughout (three sites at 0.60, three at 0.00, so standing variance exists), seeds
42-71, 12,000 ticks, quality preference on, site competition and plant mortality on. The only
variable is grazer carrying capacity. **Zero grazers is the pre-specified null control.**

Baseline is sampled at tick 200, not tick 0: `world.Statistics` is stale before the first `Step`,
so reading it at tick 0 returns `MeanPlantDefenseGene = 0` and the delta degenerates into the final
value. The first run of this probe did exactly that and reported `+0.29, t = 9.45, 30/30 up` before
the error was caught by noticing the cap-48 "delta" equalled the known cap-48 *endpoint*.

## Result

| grazer cap | n | mean Δ defense | SD | SE | t | 95% bootstrap CI | grazing pressure | plant gens | down/up |
| ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: |
| **0** | 30 | **-0.0015** | 0.1612 | 0.0294 | **-0.05** | **[-0.0636, +0.0506]** | 0.000000 | 16.5 | 16/14 |
| 12 | 30 | **-0.0957** | 0.1611 | 0.0294 | **-3.25** | **[-0.1482, -0.0325]** | 0.001026 | 15.5 | 21/9 |
| 24 | 30 | **-0.0804** | 0.1669 | 0.0305 | **-2.64** | **[-0.1338, -0.0203]** | 0.002076 | 15.5 | 23/7 |
| 48 | 30 | -0.0548 | 0.2083 | 0.0380 | -1.44 | [-0.1229, +0.0303] | 0.004536 | 15.6 | 20/10 |

**With no grazers, plant defense does not move.** The interval is centred on zero and the sign split
is 16/14 — a clean null, exactly what a control should look like.

**With grazers, defense declines and the interval excludes zero** at caps 12 and 24.

This refutes the hypothesis offered in the previous document, that the growth-rate cost was the
likely driver. It is not: remove grazing and the decline disappears entirely.

## Mechanism, consistent with the first probe of the day

`Plants.ConsumeAt` removes biomass with no defense term, so defense protects no tissue. It only
lowers nutrition density, so a grazer meeting a fixed energy need must eat **more** biomass from a
defended patch. The first measurement of 2026-08-18 recorded exactly this: raising founder defense
from 0.0 to 0.9 raised total biomass consumed 2.2x.

So in this model defense makes a plant get eaten harder, and grazing selects it away. The
dose-response is the direct confirmation.

## This is the positive control

`p4-lifespan-derived-2026-08-17.md` demanded a demonstration that the setup can detect selection on
plant defense before any coevolution null could be interpreted. This supplies it: an effect present
only when the causal agent is present, against a pre-specified control that comes back flat.

**A coevolution null is now interpretable.** The machinery is not inert.

## Caveats held deliberately

- **Cap 48 is not significant** (t = -1.44). The effect weakens as grazing rises rather than
  strengthening, and its SD is the largest of the four (0.208 vs ~0.161). No explanation is offered
  here; do not invent one. Worth investigating before the coevolution run.
- **Multiple comparisons.** Four arms puts a Bonferroni bar at t ≈ 2.7. Cap 12 clears it; cap 24
  (t = -2.64) sits just under. The cap-0 control is pre-specified rather than fished, which is what
  carries the argument.
- **Direction is downward.** Defense being selected *away* is a valid positive control for detection,
  but it also means P4's exit gate — a reciprocal plant/consumer response — still has no route to a
  defense *rise* under the current consumption model.

Raw per-seed data: `grazing-dose.csv`.
