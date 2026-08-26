# The gate does not switch selection on and off — it squeezes a margin, and the squeeze accelerates

> **QUALIFIED 2026-08-26** — `p6-the-gate-is-a-survival-mechanism-2026-08-26.md`. Every point of this
> curve was measured at **cap 100**, and "80 of 80 surviving at every gate value" is a property of the
> **cap**, not of the gate. In a cell where the ecology limits the population instead
> (cap 500, 2.0x regeneration, brake 1.5), survival across the same gate values runs
> **4 / 11 / 24 / 38 of 40** at 0.45 / 0.55 / 0.65 / 0.70 — while the same slack gate at cap 100 leaves
> **40 of 40** alive. **The gate is also the model's density brake**, which a cap-limited sweep cannot
> see. The selection curve below is unaffected and **its shape reproduces** in the new cell
> (-1.48 / -3.91 / -14.19 at 0.55 / 0.65 / 0.70).

**2026-08-24. Five gate values, 80 seeds each, 12,000 ticks, cap 100, moderate resources.**
`tools/CreatureSweep --focused 80 100 --gate=X` and `--deaths 30 100 --gate=X`.

`p6-the-mating-gate-is-the-selection-2026-08-24.md` established that the mate-seeking gate is the
dominant selective channel, from **one** alternative value. This is the curve.

## The prediction, and it was the wrong shape

**Predicted a cliff.** Reasoning: the gate can only apply pressure while it *binds*. Mean energy was
0.806 at a seek gate of 0.80 (pinned to it) and 0.717 at a seek gate of 0.55 (comfortably above it),
so the population's unconstrained level looked like ~0.72 — and any gate under that should be free.
The pressure should therefore hold up at 0.65, collapse between 0.60 and 0.55, and be flat below.

**It is not a cliff. It is a smooth, accelerating curve**, and there is no unconstrained level to
cross.

## The curve

| breed gate | seek gate | `urgency_exponent` drift | t | control t | mean energy | **margin over the seek gate** |
|---|---|---|---|---|---|---|
| 0.45 | 0.55 | −0.0006 | −0.44 | +2.55 | 0.7165 | **+0.167** |
| 0.55 | 0.65 | −0.0016 | −1.02 | +0.88 | 0.7387 | +0.089 |
| 0.60 | 0.70 | −0.0029 | −2.01 | +0.94 | 0.7638 | +0.064 |
| 0.65 | 0.75 | −0.0125 | **−7.13** | +1.06 | 0.7912 | +0.041 |
| **0.70** | **0.80** | **−0.0353** | **−14.55** | +0.17 | 0.8058 | **+0.006** |

**80 of 80 worlds survive at every value.** Controls are quiet everywhere except the 0.45 arm, whose
t = 2.55 was already flagged; the three new arms sit between 0.88 and 1.06.

## What is actually happening

**The population always sits slightly above the seek gate, and the gate decides how slightly.** The
margin runs 0.167, 0.089, 0.064, 0.041, **0.006** — at the default gate the population is living
six thousandths above the threshold that decides whether it can breed at all.

So there is no fixed "natural" energy level for a gate to be under or over. **Raising the gate raises
the population's energy**, because creatures work harder to clear a higher bar — it is a feedback
loop, not a constraint switching on. That is why the cliff prediction failed: the thing I expected the
gate to cross moves with the gate.

And selection intensity tracks the margin. Each 0.05 of gate multiplies the drift by roughly 2.7x:
0.0006, 0.0016, 0.0029, 0.0125, 0.0353. **Accelerating, not linear** — the last two steps carry most
of it.

## Why this matters for tuning

**The default gate sits on the steepest part of the curve.** Between 0.65 and 0.70 the drift nearly
triples and |t| doubles. That is not a comfortable place for a parameter to live if the intention is
a stable amount of selection: a small change to the gate is a large change to how hard the model
selects.

It is, however, exactly where the interesting behaviour is. **Below 0.60 the model barely selects on
this trait at all** (|t| ≤ 2 with a control near 1), and four other traits went quiet with it at 0.45.
The strict setting is what makes those traits mean anything.

**No recommendation to change it.** The gate is a recorded design decision, and this says what it
buys, not what it should be. What it does say is that **the choice is consequential and sits on a
knee**, so if it is ever moved it should be moved deliberately and re-measured, not nudged.

## The other four traits — and a correction to the previous doc

`p6-the-mating-gate-is-the-selection-2026-08-24.md` says **five traits stop being selected** when the
gate is slackened to 0.45. That is true *at 0.45*. Read across the whole curve, it overstates the
case, and only one other trait has a graded gate response:

| gene | 0.45 | 0.55 | 0.60 | 0.65 | **0.70** |
|---|---|---|---|---|---|
| **urgency_exponent** | −0.44 | −1.02 | −2.01 | **−7.13** | **−14.55** |
| travel_sensitivity | +0.12 | −0.24 | −0.21 | −1.53 | −2.20 |
| movement_speed | +0.96 | **+3.16** | **+2.53** | **+3.55** | **+3.94** |
| body_size | −0.70 | −0.34 | −1.11 | +0.34 | −2.01 |
| metabolic_pace | −0.18 | −1.11 | −1.10 | −2.17 | +0.86 |

- **`urgency_exponent` is the result.** Monotone across five values, spanning t = −0.44 to −14.55.
- **`travel_sensitivity` supports it** — consistent direction, roughly monotone — but **never exceeds
  |t| = 2.2**, so it is suggestive rather than established.
- **`movement_speed` is not a gate response at all.** It is back to t = 3.16 at a gate of 0.55, where
  urgency is still at 1.02. It goes quiet only at the extreme value, which is a different claim.
- **`body_size` and `metabolic_pace` are noise across the curve**, and their default-gate values
  (−2.01 and +0.86) were marginal to begin with — in a fourteen-column table where one crossing
  |t| = 2 is what chance produces.

**So the honest headline is narrower than the previous doc's:** the gate is demonstrably the driver
for `urgency_exponent`, plausibly for `travel_sensitivity`, and the "five traits" figure was an
artefact of comparing two points instead of five. **Reading the curve cost a grep and caught an
overclaim that a single comparison had produced.**

## Scope

One scenario, one population cap. The correction above is the reason to distrust two-point
comparisons in this project generally, not only this one.
