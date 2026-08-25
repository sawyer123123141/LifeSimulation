# Three benefits tried for `MetabolicPace`, three failures, and the reason they all fail

**2026-08-24. 80 seeds, 12,000 ticks, cap 100, moderate resources, terrain join on.** Baseline arm,
identical seeds throughout.

`MetabolicPace` raises the energy drain and the water drain by `0.7 + 0.8*pace` — a factor of 2.14
across its range — and had **no reader on the benefit side at all**
(`p6-metabolic-pace-is-a-pure-cost-2026-08-24.md`). This records what happened when it was given one,
twice, and why neither worked.

## The scoreboard

| condition | `metabolic_pace` drift | t | control t |
|---|---|---|---|
| pure cost (default) | +0.0055 | +0.86 | +0.17 |
| **+ faster ingestion** | −0.0129 (lean) / −0.0013 (moderate) | −1.55 / −0.21 | +0.81 / +0.94 |
| **+ faster healing** | −0.0050 | −1.01 | −0.23 |

**Not one of them makes the gene worth having.** Every cell is inside the noise, and the best any
benefit achieved was to stop the bleeding rather than to reverse it.

## Attempt one: faster ingestion. Shared, therefore diluted

`p6-metabolic-ingestion-2026-08-24.md`. Scaling `IngestionRate` by the same factor halved the
downward pressure at lean resources and left moderate flat. It never went positive.

**Diagnosis at the time:** ingestion is a **shared** channel. Contested sites are divided between
requesters, so every competitor eating faster cancels anyone eating faster. That was recorded as an
untested hypothesis, and it motivated the second attempt.

## Attempt two: faster healing. Private, therefore predicted to work — and it did not

Healing did not exist to accelerate (`p6-health-recovery-2026-08-24.md` — health had five
subtractions and no addition anywhere), so it had to be built first. Once it existed, scaling recovery
by pace gave the gene a benefit that is **private** — nobody can consume someone else's healing — and
that feeds the **mate-seeking gate**, which is where fitness is decided in this model.

**The prediction, written before the run: little or no effect, because mean health is 0.9939 and
almost nobody is ever injured.** A benefit that only pays while damaged, in a world where damage is
rare, is worth nothing.

**Measured: −0.0050 at t = −1.01, against −0.0020 at t = −0.36 with healing but no scaling.** No
effect, exactly as predicted. Being private was necessary and nowhere near sufficient.

## The reason all three fail, which is the actual result

**The costs are paid every second. Every benefit tried is paid only sometimes.**

| channel | when it pays | why that is fatal |
|---|---|---|
| energy drain | **continuously** | — |
| water drain | **continuously** | — |
| faster ingestion | only while eating at a patch | and the patch is shared, so it is cancelled |
| faster healing | only while injured | and mean health is 0.9939, so it is nearly never |

A gene charged continuously cannot be balanced by a benefit that is only occasionally collectable.
**The benefit has to be as constant as the cost**, and every continuously-paying channel in this
model — movement speed, perception range, food yield, water efficiency — **is already another gene's
job.** Doubling up would make the two non-identifiable, which is the mistake
`FoodEfficiency` and `MetabolicPace` were deliberately kept apart to avoid.

## Where that leaves the gene

Two honest options, and **renaming is now one of them on merit** rather than as the give-up I
originally offered it as:

1. **Delete `MetabolicPace`.** A trait axis with no coherent upside is a slot in the genome, a line
   in the hash and a column in every drift table, earning none of them. Deleting it is a real change
   with a real cost — every recorded genome layout shifts — but it is defensible.
2. **Keep it and rename it honestly.** If it is a cost gene, `MetabolicPace` and `DigestionRate`
   should stop promising a trade-off. `BasalCostMultiplier` and `WaterLossRate` would describe what
   they actually are.

**Recommendation: option 2.** The gene does something real — it is a live axis of variation that
selection acts on, which is more than several genes in the table manage — and the population sensibly
selling it is not a bug once the name stops implying otherwise. Deleting it buys tidiness at the cost
of disturbing every recorded genome.

**Both flags stay committed and default false.** `metabolicIngestionEnabled` and
`metabolicHealingEnabled` are the record of what was tried, and re-running either costs one command
rather than one rediscovery.

## What was gained anyway

The second attempt required building health recovery, which closed a genuine defect on its own terms:
health was a one-way ratchet and, because it gates reproduction, a fifth of health lost meant
permanent sterility rather than injury. **That fix stands regardless of what happens to
`MetabolicPace`**, and it would not have been found without chasing this.
