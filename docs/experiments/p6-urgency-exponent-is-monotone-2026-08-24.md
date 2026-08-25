# `UrgencyExponent` is a monotone benefit, and the saturation defect is why

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

## Why lower is monotonically better, which is the defect

The foraging score is `urgency * needGain * quality - travelBurden - dangerPenalty`. **`needGain`
saturates**, and the source already says so in a comment at `DecisionSystem.Scoring.cs:270`:

> every active food patch returns exactly 1.0, roughly 10x over the clamp, at every hunger level down
> to 5% energy … foraging reduces to urgency minus travel and danger

So the term that should punish over-eagerness — *this patch is not worth the trip for a need you
barely have* — **carries no information**. Food is always maximally valuable at every hunger level.
Nothing charges a creature for going to eat when it is only slightly hungry, beyond the travel it
would pay anyway.

**That is what makes the gene monotone.** The trade-off it was designed to express — react early and
waste time, react late and starve — only exists if the value of eating depends on how hungry you are.
It does not.

The **70%/70% reproduction gate** compounds it. A creature must be well fed *and* well watered to
breed, so prioritising needs is prioritising reproduction; there is no fitness left over for the
alternatives urgency competes against. (The gate is a recorded user decision and is not in question
here — it is the reason the gradient is one-directional rather than the thing to change.)

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
| `UrgencyExponent` | **all benefit, the punishing term is saturated away** | population buys it |

Neither is a trade-off, and neither is visible to a liveness harness, because both reach behaviour.
`p6-metabolic-pace-is-a-pure-cost-2026-08-24.md` is the other half of this.

## Deliberately not fixed, and what the fix would be

The honest repair is **not** to touch `UrgencyExponent`. It is to **unsaturate `ComputeNeedGain`**, so
that a patch is worth less to a creature that barely needs it. That single change would give the gene
its intended trade-off, and it would also restore the differential grazing that
`plantQualityPreferenceEnabled` exists to work around — the same comment names plant defense as a
casualty of uniform grazing.

**It would re-baseline essentially every creature and plant result on record**, which is why it is
written down rather than done. It is the largest single lever in the decision system that nobody has
pulled.

Recorded so the choice is made on purpose.
