# The population cap is not a ceiling on the ecology — it is the only thing stabilising it

**2026-08-24.** `tools/CreatureSweep --deaths <seeds> <cap> [--scale=X | --regen=X]`, moderate layout,
12,000 ticks.

The oldest measurement debt on this project is a habitat limited by **carrying capacity** rather than
by `MaximumPopulation`. `p4-cap-pinning-audit-2026-08-22.md` found **4,080 recorded runs with the
population column pinned at 48**, and recorded that raising the cap does not free it — it produces
boom-and-collapse. This asks *why*, and the answer narrows the problem considerably.

## Scarcity is not the cause

`Scaled(factor)` multiplies amount, capacity and regeneration together. At a cap of 250, across
resource levels from 0.40x to 1.00x:

| resource scale | surviving of 24 | final population (mean / median / sd) |
|---|---|---|
| 0.40x | 3 | 88.7 / 124 / 75.5 |
| 0.55x | 1 | 185.0 / 185 / — |
| 0.70x | 3 | 76.0 / 25 / 108.0 |
| 0.85x | 3 | 107.3 / 70 / 128.2 |
| 1.00x | 3 | 20.0 / 5 / 29.5 |

**Every level collapses, and the level makes no difference to whether it does.** That is not a
coincidence: scaling all three quantities together leaves the *ratio* between them unchanged, so the
dynamics are **scale-invariant by construction.** `Scaled` cannot reach this problem, and a scarcity
sweep was never going to answer it.

## Regeneration reaches it — and then just finds the new ceiling

`WithRegeneration(factor)` multiplies regeneration **only**, leaving standing stock and capacity
alone, which is the ratio `Scaled` cannot move. At a cap of 250:

| regeneration | surviving of 24 | starvation share | population sd |
|---|---|---|---|
| 1.0x (baseline) | 3 | — | 29.5 |
| **1.5x** | **15** | 6.3% | **66.9** |
| 2.0x | 23 | 0.3% | 6.0 |
| 3.0x | 24 | 0.3% | **0.65** |
| 5.0x | 24 | 0.0% | 31.9 |

**Faster regrowth converts collapse into survival**, which confirms the ratio is the operative
quantity. But 2.0x and above simply pin at the new cap — sd 0.65 at 3.0x is the same zero-variance
ceiling as before, moved up. Only 1.5x shows real spread, and it is **bimodal** (9 of 24 still
extinct, median 236 against a cap of 250) rather than an equilibrium.

## Raise the ceiling out of the way and it collapses again

If the ecology had a carrying capacity of its own, a cap it never reaches should be irrelevant. At a
cap of **500**:

| regeneration | surviving of 20 | **starvation share of deaths** | population sd |
|---|---|---|---|
| 1.50x | 3 | **55.1%** | 56.0 |
| 1.75x | 3 | 35.4% | 221.3 |
| 2.00x | 3 | **64.2%** | 224.0 |
| 2.50x | 6 | 36.0% | 232.1 |

**2.0x regeneration survives 23 of 24 at a cap of 250 and 3 of 20 at a cap of 500.** Same ecology,
same resources, same regrowth. The only thing that changed is a number that is supposed to be an
upper bound.

And the death mix names the mechanism: **starvation is 35–64% of deaths at cap 500 against 0.1% at
cap 100.** Populations overshoot, strip the forage, and die en masse.

## The conclusion, stated at the strength it is earned

**Within everything tested — resource levels 0.40x to 1.00x, regeneration 1.0x to 5.0x, caps of 100,
250 and 500 — no combination produces a population that settles below a high cap.** The cap is not
bounding a self-regulating ecology. **It is supplying the regulation**, by preventing the overshoot
that causes the crash.

That is a stronger and more useful statement than "raising the cap produces boom-and-collapse", which
was already known. It says the model has **no density-dependent brake of its own.**

## The candidate explanation, which is a hypothesis

Nothing slows reproduction as conditions tighten. Births are gated by `CanReproduce` and
`CanSeekMate`, which are **step functions** — 70% and 80% of three needs, satisfied easily while food
is abundant and then failed by everyone at once when it is not. There is no "resources are getting
tight, breed less" signal, only "resources are gone, nobody breeds and many starve."

**A graded fertility response** — breeding probability rising with condition rather than switching on
at a threshold — is the standard shape that produces equilibrium instead of overshoot, and its absence
is consistent with everything above.

**This is not established.** It is consistent with the measurements and it names a mechanism that
exists in the source, which is exactly the strength at which two other explanations tonight turned out
to be contributing factors rather than causes. The test is to build a graded gate behind a flag and
see whether a population then settles below a cap it can reach.

## What is now available for that work

- `SimulationScenario.WithRegeneration(id, factor)` — the axis `Scaled` cannot move.
- `--regen=X`, `--scale=X`, `--deaths <seeds> <cap>` on `tools/CreatureSweep`, with population
  **spread** reported (min, median, max, sd) rather than only the mean, because **a carrying capacity
  produces a distribution and a cap produces a constant**, and eleven committed corpora failed to
  distinguish them.
