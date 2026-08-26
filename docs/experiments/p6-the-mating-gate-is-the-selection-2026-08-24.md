# The mating gate was the selection: slacken it and five traits stop being selected at all

> **PARTLY CORRECTED by `p6-gate-dose-response-2026-08-24.md`.** The headline result -
> `urgency_exponent` from t = -14.55 to -0.44 - is confirmed and strengthened by five gate values.
> **"Five traits" is not.** Read across the whole curve, only `urgency_exponent` has a clean graded
> response; `travel_sensitivity` supports it but never passes |t| = 2.2; `movement_speed` is back to
> t = 3.16 at a gate of 0.55 and so is not a gate response at all; and `body_size` and
> `metabolic_pace` are noise across the curve, with marginal default-gate values to begin with. **The
> figure was an artefact of comparing two points instead of five.**

**2026-08-24. 80 seeds, 12,000 ticks, population cap 100, terrain join on, moderate resources.**
`tools/CreatureSweep --focused 80 100 --gate=0.45` against the identical seeds at the default gate
(`p6-dose-response-moderate-80seeds`). Death mix from `--deaths 30 100 --gate=0.45`.

The causal test for `p6-urgency-exponent-is-monotone-2026-08-24.md`, after
`p6-nothing-starves-2026-08-24.md` retired the survival explanation.

## The prediction, and it undershot

**Predicted:** if the gates drive it, `urgency_exponent` drift should shrink from −0.0353 (t = −14.55)
toward about −0.015 with |t| below 7.

**Measured: −0.0006 at t = −0.44.** It did not shrink. **It vanished** — to a fifth of the control's
own movement in the same run.

## What a slacker gate does to the whole table

Drift from founders, moderate resources, same seeds:

| gene | gate 0.70 / 0.80 | **gate 0.45 / 0.55** |
|---|---|---|
| **urgency_exponent** | **−0.0353 (t −14.55)** | **−0.0006 (t −0.44)** |
| movement_speed | +0.0279 (3.93) | +0.0041 (0.96) |
| body_size | −0.0133 (−2.01) | −0.0037 (−0.70) |
| travel_sensitivity | −0.0049 (−2.20) | +0.0002 (0.12) |
| metabolic_pace | +0.0055 (0.86) | −0.0010 (−0.18) |
| | | |
| lifespan_tendency | +0.2752 (17.44) | **+0.3136 (27.80)** |
| fertility_investment | +0.0593 (3.89) | **+0.0876 (7.36)** |
| temperature_tolerance | +0.2879 (26.03) | +0.2338 (21.46) |
| **neutral_marker (control)** | +0.0005 (0.17) | **+0.0037 (2.55)** |

**Five traits stop being selected. Two get stronger.**

The pattern is coherent rather than scattered. Everything that lost its pressure — how eagerly you
eat, how fast you move, how big you are, how much you mind travelling, how fast you burn — is a trait
whose route to fitness ran through **clearing the threshold**. What strengthened is what pays once the
threshold is cheap: **living longer** and **investing more per offspring**.

**The 0.80 mating gate was not a detail of the reproduction rules. It was the fitness bottleneck, and
most of the measurable selection in this model was creatures optimising to get over it.**

## The mechanism, seen directly

| | gate 0.70 / 0.80 | gate 0.45 / 0.55 |
|---|---|---|
| mean energy fraction | **0.8058** | **0.7165** |
| mean hydration fraction | 0.8593 | 0.7852 |
| starvation deaths | 8 (0.1%) | **163 (2.6%)** |
| age deaths | 5,443 (96.9%) | 5,704 (92.2%) |
| final population | 98.2 | 99.9 |

Mean energy tracks the gate down, which is the homeostat argument confirmed from the other side.
Starvation rises twentyfold — from negligible to still-small — because creatures now breed at lower
reserves and spend more time near empty. Nobody is starving *instead*; they are simply no longer held
at 80% by the need to court.

## The bug that found something

The first attempt threaded the gate into `CanReproduce` but not into `CanSeekMate`, whose call site
in `DecisionSystem` never passed it. Both output files came back **byte-identical** to the default-gate
runs, and were nearly reported as a null result.

They were not null. They were the correct answer to a different question: **lowering only the breeding
gate does exactly nothing, because the mate-seeking gate above it is what binds.** A creature that
cannot look for a mate below 0.80 is already past 0.70 by the time it finds one, so the lower
threshold is never consulted. Of the two literals, **0.80 is the one that matters**.

## What this does and does not say

**Does:** at moderate resources and a population cap of 100, the reproduction gates are the dominant
selective channel in this model, and `UrgencyExponent` is a healthy gene correctly reporting a strict
world rather than a broken one. There is nothing to fix in the gene.

**Does not:**

- **Say the gate should change.** It is a recorded design decision and it is used here as an
  instrument. What this shows is how much rests on it, which is an argument for choosing it
  deliberately, not for lowering it.
- **Rule out noise in the slack arm.** The control moves to **t = 2.55** at gate 0.45, against 0.17
  at the default — the noise floor in that arm is much higher, and columns near |t| = 2 there mean
  little. It does not touch the headline: `urgency_exponent` at |t| = 0.44 is *below* its own control,
  so "the pressure vanished" holds regardless of where the floor sits.
- **Generalise past one condition.** One scenario, one cap, one resource level, one alternative gate
  value. A dose-response across gate values is the obvious next run and has not been done.

## Consequence for two earlier docs

`p6-urgency-exponent-is-monotone-2026-08-24.md` — its corrected hypothesis is **confirmed**. Its
original hypothesis, that `ComputeNeedGain` saturation was to blame, is doubly dead: the repair was
backwards *and* the real cause is elsewhere.

`p6-metabolic-pace-is-a-pure-cost-2026-08-24.md` — unaffected as a statement about the source, but its
*measured* downward drift partly disappears at a slack gate (+0.0055 → −0.0010), which suggests some
of what looked like the population selling a costly gene was the same gate effect. The source fact —
no benefit-side reader — stands on its own and is not a measurement.
