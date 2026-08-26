# The gate does not switch selection on and off — it squeezes a margin, and the squeeze accelerates

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

## Scope

One scenario, one population cap, one trait's response tracked across the curve. The other four
traits that went quiet at 0.45 were not tracked at the intermediate values — that is the obvious
extension and it costs nothing but the reading, since the corpora are committed.
