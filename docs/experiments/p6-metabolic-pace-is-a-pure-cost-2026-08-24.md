# `MetabolicPace` buys nothing, and the population is selling it

> **Two benefits were tried and both failed** - see
> `p6-metabolic-pace-has-no-benefit-that-fits-2026-08-24.md`. Faster ingestion is shared and gets
> diluted; faster healing is private but almost never collectable, because mean health is 0.9939. The
> costs are paid continuously and no available benefit is. The conclusion below stands and is now
> better supported than when it was written.

**2026-08-24. No new runs — this is read off six corpora already committed.**
`p6-dose-response-{moderate,lean,scarce}-80seeds` and
`p6-terrain-temperature-sweep{,-lean,-scarce}-80seeds`, 80 seeds each, baseline arm.

`metabolic_pace` kept crossing |t| = 2 at the lean level and only there, in both temperature
conditions. It was recorded as "a candidate worth another look". This is that look, and the answer
was in the source rather than in more seeds.

## The mechanism: two costs, zero readers on the benefit side

`MetabolicPace` reaches the phenotype in exactly two places (`GenomePhenotype.cs:410,412`):

| phenotype field | formula | who reads it |
|---|---|---|
| `DigestionRate` | `0.7 + 0.8 * pace` | `NeedsSystem.cs:49` — **water drain per second**; `DecisionSystem.cs:564` — the *estimate* of that same drain |
| `BasalEnergyCostMultiplier` | `bodyMass^0.75 * (0.7 + 0.8 * pace) * maintenance` | `NeedsSystem.cs:45` — **energy drain per second** |

An exhaustive reader search finds no third site. **Nothing anywhere converts a higher metabolic pace
into more food, faster eating, better yield, quicker recovery or any other gain.** `IngestionRate`
and `FoodYield` — the two fields that would carry that benefit — are driven by `FoodEfficiency`,
not by pace.

So `DigestionRate` is misnamed: it does not make digestion faster. It only makes a creature thirstier.

**Across its full range the gene raises both drains by a factor of `1.5 / 0.7 = 2.14` and returns
nothing.** That is a stronger version of the body-size shape, where at least a large carcass feeds
whoever eats it.

## The prediction, before the table

A pure-cost gene should fall, and it should fall harder where the cost bites — which is where food
and water are scarce, exactly as body size does
(`p6-body-size-shrinks-under-scarcity-2026-08-24.md`).

## What the six corpora already said

Drift from founders, baseline arm, survivors only:

| resource level | temperature | drift | t | control t | surviving |
|---|---|---|---|---|---|
| moderate (1.0x) | sine | **+0.0055** | +0.86 | +0.17 | 79 / 80 |
| lean (0.6x) | sine | **−0.0252** | **−2.99** | **+0.07** | 55 / 80 |
| scarce (0.35x) | sine | −0.0329 | −1.25 | −1.13 | 12 / 80 |
| moderate (1.0x) | terrain | −0.0097 | −2.00 | +0.37 | 80 / 80 |
| lean (0.6x) | terrain | −0.0248 | −2.88 | −1.91 | 47 / 80 |
| scarce (0.35x) | terrain | −0.0598 | −1.47 | −0.51 | 8 / 80 |

**Downward in five of six conditions**, and **monotonic in scarcity in the terrain column** —
−0.010, −0.025, −0.060, the same doubling-per-step shape body size shows. The one positive cell is
moderate/sine at t = 0.86, which is inside the noise.

**The strongest single cell is lean/sine: t = −2.99 against a control at t = +0.07**, which is 139
times the control's movement. That one is clean.

## Why the other cells are weaker, said plainly

- **Both scarce cells are underpowered.** 12 and 8 surviving worlds. The drift is largest there and
  the t is smallest, which is what a big effect measured on almost no runs looks like.
- **The lean/terrain control is noisy at t = −1.91.** So the −2.88 beside it clears its own noise
  floor by only about 1.5×, and that cell should not be leaned on. It is the *agreement* with
  lean/sine, whose control sits at +0.07, that makes the pair persuasive.
- **Six conditions are not six independent tests.** They share seeds and a scenario family. This is
  one result seen under six lightings, not six results.

## The part that matters more than the statistic

**A pure-cost gene passes every liveness test by construction.** `GeneLivenessAnalysis` asks whether
perturbing a gene changes `ComputeBehaviorHash` — whether the gene *reaches* behaviour. A cost
reaches behaviour. `MetabolicPace` is live, has always been live, and the harness was never capable
of noticing that it is live in one direction only.

This is the same shape `PlantGeneLivenessAnalysis` already documents for plant
`TemperatureTolerance`: *"A pure-cost gene is the `Defense` shape all over again, and a caller-search
does not reveal it because the readers exist — they are just fed constants."* Here the readers exist
and are fed real values; they are simply all on the debit side.

**Liveness is not the same as having a benefit**, and nothing in the project currently tests for the
second thing.

## Deliberately not fixed

Giving `MetabolicPace` a benefit — the obvious one being that `DigestionRate` should raise
`IngestionRate` or shorten the time to convert food into energy — **would change every creature
result on record**. It is a design decision about what the gene is supposed to mean, not a bug fix,
and it belongs to whoever owns the ecology rather than to whoever noticed the asymmetry.

Recorded here so the choice is made on purpose. The measurement that would settle whether the current
behaviour is a problem is whether the population drives the gene to zero given long enough — at
12,000 ticks it has moved 0.06 at worst, so it is a slow bleed rather than a collapse.
