# `UrgencyExponent` is a monotone benefit — and my first explanation of why was wrong

> **CORRECTION, same day, before anyone acted on it.** The original version of this doc blamed the
> `ComputeNeedGain` saturation and proposed "unsaturate it" as the repair. **Both halves are wrong**,
> and the proposed repair is backwards. The measured result — nine conditions, nine times negative —
> is unaffected and stands. The corrected reasoning is below, along with what the original said, so
> the error is on the record rather than quietly edited away.

**2026-08-24. No new runs — read off nine corpora already committed.** 80 seeds each, 12,000 ticks,
population cap 100.

`metabolic_pace` was a gene that only ever costs. This is the opposite failure in the same family, and
it is **the most reproducible selection signal in the model**: nine conditions out of nine, every one
of them negative.

## The mechanism: two readers, one expression, no trade-off

`UrgencyExponent` never reaches the phenotype. It is read straight from the genome in exactly two
places (`DecisionSystem.Scoring.cs:232` and `:267`), and both are the identical line:

```
urgency = Urgency(current, capacity) ^ (0.5 + 2.5 * gene)
```

`Urgency` is the **need shortfall**, `clamp01(1 - current / capacity)`, so it lives in `[0, 1]`.
Raising a number in `[0, 1]` to a larger power makes it smaller. Therefore **a higher gene means less
urgency at every partial need**, and the gene has one clean meaning: *how empty do I have to be
before I care.*

| gene | exponent | urgency at half-empty |
|---|---|---|
| 0.0 | 0.5 | **0.707** |
| 0.5 (founder) | 1.75 | 0.297 |
| 1.0 | 3.0 | 0.125 |

## What I said first, and why it is wrong

The original claim was that `ComputeNeedGain` saturating is what removes the gene's trade-off, and
that the repair is to unsaturate it. **The repair is backwards.**

```
ComputeNeedGain = min(1, resource.Amount * perUnitGain / missing)
```

That is *what fraction of my shortfall can this patch fill*. It pins at 1 because patches hold far
more than one creature's shortfall — which is correct behaviour. **Removing the clamp would let the
term exceed 1 and make food MORE attractive to a nearly full creature, not less.** The clamp is the
thing keeping it sane.

It was also the wrong term to accuse. `needGain` is a **patch-adequacy filter** — *is this patch big
enough to be worth it* — and was never the diminishing-returns term.

## Why lower actually looks better

**The diminishing-returns term is `urgency` itself**, which is to say the gene is its own punishment
term and controls how sharply it bites. And the punishments that sit outside it do exist:

- `travelBurden` is absolute, in fractions of energy and hydration capacity, and a low-urgency score
  genuinely loses to it — `Math.Max(0f, ...)` clamps the whole score to zero.
- **opportunity cost is real**: the decision picks a single highest-scoring candidate, so foraging
  displaces mating, fleeing and resting.

So the trade-off is not missing. The likelier reason it does not bind is the **reproduction gates**,
which are far higher than they look:

| gate | threshold | source |
|---|---|---|
| can reproduce | energy, hydration **and health all ≥ 70%** | `ReproductionSystem.cs:215` |
| can even *seek* a mate | all three **≥ 80%** | `ReproductionSystem.cs:224` |

**A creature below 80% cannot go looking for a mate at all.** So topping up is not competing with
breeding — it is a *precondition* for it, and the opportunity cost of foraging early is close to
zero for any creature that is not already near full. Under gates that high, eagerness is not a bug in
the gene. It may be the correct answer to the world as designed.

The gates are a recorded user decision and are not being questioned. The point is that **they, not a
saturated term, are the most likely explanation** — and that is a hypothesis, which is exactly what
the first version of this doc failed to say about its own.

## Nine conditions, nine times negative

Drift from founders, baseline arm:

| corpus | drift | t | control t |
|---|---|---|---|
| moderate, sine | −0.0353 | **−14.55** | +0.17 |
| lean, sine | −0.0474 | **−13.32** | +0.07 |
| scarce, sine | −0.0474 | −3.51 | −1.13 |
| moderate, terrain | −0.0346 | **−16.79** | +0.37 |
| lean, terrain | −0.0445 | −9.15 | −1.91 |
| scarce, terrain | −0.0470 | −3.22 | −0.51 |
| moderate, metabolic ingestion | −0.0399 | **−19.38** | +0.94 |
| lean, metabolic ingestion | −0.0490 | −13.65 | +0.81 |
| scarce, metabolic ingestion | −0.0553 | −3.62 | *+3.31 — row unreadable* |

**Every sign the same, across three temperature conditions, three resource levels and an ingestion
mechanic.** The magnitude also tightens with scarcity, −0.035 to −0.055, which is what a
one-directional pressure getting stronger looks like.

## The founder column is doing something important here

**Founder is exactly `0.5000` in all nine runs.** `UrgencyExponent` is one of four genes the founder
profile does not vary — with `TravelSensitivity`, `RiskAversion` and `NeutralMarker` — so the
population starts **monomorphic** and every variant selection acts on has to be supplied by mutation
first.

That resolves what would otherwise look contradictory: **t = −19.4 producing a shift of only 0.04.**
The response is **mutation-limited, not selection-limited.** The gradient is steep and utterly
consistent; there is simply almost nothing for it to act on at any moment. A gene with standing
variation and this gradient would have moved far further.

It also means the `NeutralMarker` control is measuring the right thing for exactly these four genes —
same starting point, same mutation supply, no selection — which is why the control columns are so
quiet in the healthy rows.

## Two failures, one family

| gene | shape | consequence |
|---|---|---|
| `MetabolicPace` | **all cost, no reader on the benefit side** | population sells it |
| `UrgencyExponent` | **monotone benefit in this world** — cause not yet established | population buys it |

Neither is a trade-off *as measured*, and neither is visible to a liveness harness, because both reach
behaviour. **The two are not equally settled**: `MetabolicPace` having no benefit-side reader is a
fact about the source, while `UrgencyExponent` being monotone is a measurement whose cause is still a
hypothesis.
`p6-metabolic-pace-is-a-pure-cost-2026-08-24.md` is the other half of this.

## So is there anything to fix?

**Possibly nothing.** If the gates are the driver, the gene is reporting the truth about this world:
when you cannot even look for a mate below 80% of three separate needs, being quick to eat is simply
correct, and a population discovering that is the simulation working.

**The experiment that would settle it** needs no new mechanism — only the gate thresholds made
configurable, and the urgency drift re-measured at a lower gate:

- if the downward pressure **weakens**, the gates are the cause and the gene is healthy
- if it **persists unchanged**, something else is driving it and this doc is still not finished

That is a config value and one sweep, against nine corpora that already provide the comparison. It is
the next thing to do, and it is a much smaller and better-aimed change than the one this doc
originally proposed.

**The uniform-grazing problem is real and separate.** `ComputeNeedGain` pinning at 1 does mean patches
are not differentiated by size, which is a genuine issue for plant defense and is why
`plantQualityPreferenceEnabled` exists. That stands on its own and is not the explanation for this
gene.
