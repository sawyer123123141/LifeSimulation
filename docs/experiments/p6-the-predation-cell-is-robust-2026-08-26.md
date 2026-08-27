# The predator-prey cell, off its one cell - gate, cap, regeneration and brake

2026-08-26, later. `CreatureSweep --focused 30 <cap> --predation`, 30 relief-selected seeds x two
slope arms = 60 runs per cell, **every cell run with `healthRecoveryEnabled` off and on** per the
standing decision. Baseline is the recorded cell: cap 500, regen 2.0, brake 1.5, gate 0.45,
`IntentUtilityV1`, `PredationVariation` founders. One parameter moves at a time.

The handoff called this "the first thing to check before anything is built on it". The whole
predator-prey result - a survivable scenario at all, and `defense` under t = 11 - lived at a single
point in a four-dimensional cell.

## Survivability

Extinct of 60, `health off / health on`:

| axis | value | extinct/60 |
|---|---|---|
| gate | 0.40 | **6 / 6** |
| gate | **0.45 (baseline)** | 10 / 10 |
| gate | 0.55 | 24 / 20 |
| gate | 0.65 | 52 / 50 |
| cap | 250 | 10 / 10 |
| cap | **500 (baseline)** | 10 / 10 |
| cap | 1000 | 10 / 10 |
| regen | 1.5 | 9 / 12 |
| regen | **2.0 (baseline)** | 10 / 10 |
| regen | 3.0 | **2 / 6** |
| brake | 1.0 | 9 / 8 |
| brake | **1.5 (baseline)** | 10 / 10 |
| brake | 3.0 | 17 / 18 |

**The cell is a plateau, not a knife-edge.** Nothing within one step of the baseline collapses it.
The gate is the only axis that can destroy it, and it does so smoothly - 6, 10, 24, 52 across 0.40 to
0.65 - which is the same accelerating curve `p6-gate-dose-response-2026-08-24.md` found without
predation, not a cliff.

**The baseline is not the best point in the cell.** Gate 0.40 survives 6 of 60 against 10, and
regen 3.0 survives 2 of 60. If more surviving runs are wanted for statistical power, they are
available for free and without redesign.

## The cap does not regulate this cell, and that is provable rather than inferred

**Cap 500 and cap 1000 produce byte-identical simulation hashes on all 60 runs.** Maximum population
reached at either is 486. On this seed set the ceiling is never touched, so raising it is a no-op -
not "a small effect", a *no* effect, demonstrated by hash equality rather than by a p-value.

> **Qualified the same day, by the follow-up below.** That hash equality holds on the *relief-selected*
> seed set the sweep uses. On the contiguous seed set `--deaths` uses, cap 1000 reaches a maximum
> population of **505** against cap 500's **500** - so at least one run there *is* clipped by the 500
> ceiling, and "the ceiling is never touched" is true of the sweep's seeds and not of every seed.
> The conclusion is unaffected: cap 250 binds hard and changes nothing, so the cap is still not what
> regulates this cell. The overstatement was in "never", not in the finding.

Cap 250 does bind (a run pins at exactly 250) and **still changes nothing**: 10 of 60 extinct, same
as the baseline, and `defense` at t +10.56 against +10.97.

`p6-the-cap-is-the-stabiliser-2026-08-24.md` found the cap supplying the regulation the model had
none of. **That is no longer true here.** The graded-fertility brake and predation together hold the
population at a mean of 129 under a ceiling of 500. The cap has been made redundant, which is what
closing the carrying-capacity debt was supposed to achieve, now confirmed on the predation substrate.

## Defense selection is robust

`defense` drift-from-founders t, `health off / health on`, with the surviving-run count the drift is
computed over:

| cell | surviving of 30 | `defense` t | `attack` t |
|---|---|---|---|
| gate 0.40 | 28 / 28 | +7.68 / +9.27 | -1.73 / -2.64 |
| **gate 0.45 (baseline)** | 25 / 25 | **+10.97 / +7.68** | -3.84 / -3.59 |
| gate 0.55 | 19 / 20 | +4.97 / +4.09 | -4.02 / -3.24 |
| gate 0.65 | **5 / 6** | +1.21 / +1.40 | -25.34 / -18.41 |
| cap 250 | 25 / 25 | +10.56 / +7.73 | -3.86 / -3.60 |
| cap 1000 | 25 / 25 | +10.97 / +7.68 | -3.84 / -3.59 |
| regen 1.5 | 24 / 22 | +7.47 / +9.48 | -2.24 / -1.82 |
| regen 3.0 | 29 / 28 | +5.86 / +7.85 | -2.51 / -2.94 |
| brake 1.0 | 26 / 27 | +9.35 / +8.28 | -1.50 / -2.12 |
| brake 3.0 | 21 / 19 | +2.48 / +3.55 | -3.79 / -4.21 |

**`defense` is positive in all twenty cells and never approaches zero except where the population
does.** Excluding gate 0.65, the range is +2.48 to +10.97 across a 4x span of cap, a 2x span of
regeneration, a 3x span of brake strength and gates from 0.40 to 0.55 - both health arms of every
one. **The headline result is not an artefact of the cell it was found in.**

`attack` is negative in nineteen of twenty, -1.5 to -4.2. That was reported once at -3.84 and is now
robust too.

**Gate 0.65 is excluded from every claim above**: 5 and 6 surviving runs of 30. Its `defense` of
+1.2 is not evidence that selection stops, and its `attack` of -25 and -18 is not evidence that
selection intensifies - both are what a near-extinct cell's drift table looks like. It is in the
table so nobody re-derives it and believes it.

## What varies, and the explanation I do not have

`defense` t moves between +2.5 and +11.0 across the surviving cells. It does **not** track the
surviving-run count - regen 3.0 has the most survivors (29) and one of the lower t values (+5.86) -
so this is real variation, not power.

Predation exposure was measured to explain it and does not:

| cell | attack hits/run | predation deaths | population | `defense` t |
|---|---|---|---|---|
| gate 0.45 | 6.7 | 16 | 129 | +10.97 |
| gate 0.40 | 18.9 | 50 | 106 | +7.68 |
| gate 0.55 | 2.8 | 9 | 59 | +4.97 |
| brake 3.0 | 3.1 | 12 | 25 | +2.48 |
| brake 1.0 | 9.8 | 29 | 130 | +9.35 |
| regen 3.0 | 13.2 | 23 | 170 | +5.86 |

**Non-monotone in exposure**: the strongest selection is at 6.7 attacks per run and the weakest at
3.1, but 18.9 gives only +7.68. The two lowest t values are the two smallest populations (25 and 59),
which is suggestive and nothing more. **No predictor was found and none is proposed.** The previous
session withdrew two mechanisms for this same result; a third is not offered.

## Also recorded

- `aggression` reaches -2.4 and -2.1 at gate 0.40 and -2.5 at regen 1.5, and is null elsewhere.
  `maneuverability` reaches +3.8 and +4.4 at gate 0.40 and sits at 0.9-2.6 everywhere else. The
  `neutral_marker` control crosses |t| = 2 once in twenty cells (+2.68 at gate 0.55), which is the
  chance expectation. **Nothing is claimed for either gene.**
- A column-alignment trap in the sweep's own output: when a t value is <= -10 and the mean is
  negative, the mean and t columns run together (`-0.2729-18.4080`) and a naive split reads the
  *next* column as t. This produced a "+87.68" for `attack` that was really -18.41. Parse the drift
  table with a number regex, not on whitespace.

## Replication, unplanned

Two of these cells already existed as committed corpora from an earlier session (`0914369`) and were
rewritten by these runs: cap 250 at gate 0.45, and cap 500 at gate 0.55. **Every hash, population and
energy value is identical**; only `code_revision` changed. Together with the two byte-identical
replications recorded in `p6-the-controller-comparison-2026-08-26.md`, four committed predation
corpora have now reproduced exactly across three commits.

## Follow-up: what sets the size of `defense` selection - a measured negative

The open question above was pursued the same day. `--deaths` reports the mean `defense` of the
predated against the living, so the **selection differential** can be read per cell; births per run
give **turnover**. Nine cells, both health arms.

| cell | population off / on | differential off / on | `defense` t off / on |
|---|---|---|---|
| brake 3.0 | 25 / 22 | 0.399 / 0.448 | +2.48 / +3.55 |
| gate 0.55 | 59 / 79 | 0.472 / 0.367 | +4.97 / +4.09 |
| gate 0.40 | 106 / 136 | 0.358 / 0.409 | +7.68 / +9.27 |
| cap 250 | 112 / 108 | 0.471 / 0.369 | +10.56 / +7.73 |
| gate 0.45 | 129 / 126 | 0.471 / 0.369 | +10.97 / +7.68 |
| cap 1000 | 130 / 127 | 0.471 / 0.369 | +10.97 / +7.68 |
| brake 1.0 | 130 / 145 | 0.441 / 0.437 | +9.35 / +8.28 |
| regen 1.5 | 154 / 160 | 0.335 / 0.416 | +7.47 / +9.48 |
| regen 3.0 | 170 / 174 | 0.501 / 0.512 | +5.86 / +7.85 |

**The selection differential is near-constant and explains nothing.** Across all eighteen cells the
predated carry `defense` 0.18-0.34 and the living 0.61-0.73, a differential of **0.335 to 0.512** -
against a `defense` t that ranges 2.5 to 11. Spearman rho of differential against t is **+0.08 with
health off and +0.33 with it on**. **Predation kills the same kind of creature in every cell**; the
variation in measured selection is not variation in how selectively it kills. That retires the
obvious explanation.

**Population and turnover are an arm-conditional result and are therefore withdrawn.** Rho of
population against t is **+0.83 with health recovery on and +0.30 with it off**; births per run give
**+0.80 and +0.15**. An inverted-U in the health-off arm, peaking near population 130, does not
appear in the health-on arm at all. **One arm would have produced a confident finding here and the
second arm deletes it** - which is the entire reason the standing decision requires both.

**The one statement that replicates:** the two smallest populations - 22-25 and 59-79 - give the two
lowest `defense` t values in both arms. Below roughly a hundred creatures the response is weak.
Above that, across a range of 108 to 174, nothing measured here orders it.

**So: no predictor, and now that is a measured negative rather than an unexamined gap.** Differential
is ruled out. Population and turnover are ruled out as *replicated* predictors. A third mechanism is
not offered.

## What this settles and what it opens

- **Settled: the predator-prey result travels.** Survivability, and `defense` and `attack` selection,
  hold across every one-step move from the baseline in four parameters and both health arms. Work can
  be built on this cell.
- **Settled: the cap is inert here**, by hash equality, and the brake plus predation are what regulate.
- **Closed as a negative: what sets the size of `defense` selection.** The selection differential is
  near-constant and rules itself out; population and turnover predict it in one health arm and not
  the other, so they are withdrawn. Only "small populations select weakly" replicates.
- **Open, unchanged:** whether attackers *target* low-defense creatures or merely *succeed* against
  them more often.
- **Available for free:** gate 0.40 or regen 3.0 give 6 and 2 extinctions of 60 against the
  baseline's 10, if a future experiment wants more surviving runs. Both change the ecology and would
  need to be stated, not silently adopted.
