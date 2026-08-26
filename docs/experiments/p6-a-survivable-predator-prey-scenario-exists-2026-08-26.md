# A survivable predator-prey scenario exists — the blocker was never the genome, it was the reproduction gate

**2026-08-26.** `tools/CreatureSweep --deaths 30 500 --regen=2.0 --brake=1.5 --predation [--gate=X]
[--health-recovery]`, 12,000 ticks, 30 seeds per cell. Console artefact:
`p6-predator-prey-viable-2026-08-26.txt`.

Closes a blocker standing since `p4-inert-flags-readjudicated-2026-08-19.md`: **no survivable
predator-prey scenario existed**, so `multiThreatPerceptionEnabled` and `kinRecognitionEnabled` could
only ever be adjudicated on a corpse.

## The founder profile is now neutral-plus-combat

`PredationFounderFactory` set six of twenty-four traits and left the rest at the constructor's `0f` —
arriving through two positional constructors, since `Genome.Neutral` also passes six. It now sets
every non-combat family to **0.5**, so the profile is what its name says: a neutral genome with the
combat family varied, and nothing else varied. **Varying them instead would make it physiology
variation plus combat variation and confound the two**, which is the property
`PredationFounderProfileSeedsUnlabeledPredationVariation` exists to protect.

Effect of removing the zeros, at the default gate:

| | zeros | fertility+lifespan set | all non-combat neutral |
|---|---:|---:|---:|
| health deaths | 3.9% | 47% | **0.3%** |
| age deaths | 82% | 32% | **86.7%** |
| births per run | 0.0 | 0.0 | **0.3** |
| surviving | 0/30 | 0/30 | **0/30** |

**Two rounds of genome fixes, and reproduction still did not happen.** The founders were now healthy,
long-lived and fertile, and they still would not breed. **The genome was never the blocker.**

## It is the mate-seeking gate

Same cell, gate swept:

| gate | surviving | births / run | attack hits / run | predation share |
|---:|---:|---:|---:|---:|
| 0.20 | **25 / 30** | **279.8** | 90.1 | — |
| 0.35 | **26 / 30** | **183.6** | 21.6 | 2.0% |
| 0.45 | **24 / 30** | **146.0** | 6.7 | 1.0% |
| 0.55 | 14 / 30 | 40.7 | 2.8 | 1.2% |
| 0.70 (default) | **0 / 30** | 0.3 | 3.2 | 3.3% |

**A predator-prey world is viable at a gate of 0.45 or slacker** — 24 to 26 worlds of 30, well over a
hundred births per run, and predation actually firing at 7 to 90 attack hits per run against **zero**
for the herbivore profile.

The mechanism: the gate requires energy, hydration **and** health each above the threshold
*simultaneously*. Combat-varied founders with fixed mid physiology cannot hold all three at 0.80 at
once, and one need dipping is enough to block mating for as long as it lasts. It is not a survival
problem — 86.7% of them die of old age — **it is a fertility problem produced by a conjunction of
three conditions.**

## Both health arms, as standing practice

At gate 0.45: **24 / 30 surviving and 146.0 births with the health ratchet, 25 / 30 and 148.5 with
`--health-recovery`.** The result does not depend on the ratchet, which is worth knowing precisely
because health is one of the three gate conditions and was the obvious suspect.

## What this unblocks and what it does not

- **`multiThreatPerceptionEnabled` and `kinRecognitionEnabled` can finally be adjudicated.** Both sit
  inside `if (predationEnabled)` and have never had a living population to be measured in. This cell
  is one. **They remain in `KnownInertFlags` until that measurement is actually taken** — this
  document does not take it.
- **Predation is still a minor mortality source** — 1% to 2% of deaths even where attacks are
  frequent. A world where predators *exist* is not yet a world where predation *selects*. That is the
  next question, not a claim being made here.
- **The gate value is a configuration, not a discovery.** Running predator-prey at 0.45 means running
  it in a configuration no recorded corpus uses, and results there are not comparable to the recorded
  ones.

## One tension worth recording rather than resolving

`p6-the-gate-is-a-survival-mechanism-2026-08-26.md` found that slackening the gate to 0.45 at cap 500
**kills** the herbivore profile — 4 of 40 surviving — because breeding while depleted causes overshoot.
Here the same slack gate is what **saves** the predation profile. **The two profiles want opposite gate
settings in the same world.** The obvious candidate is that they differ in their core physiology
traits — varied in one, fixed at 0.5 in the other — so this is not yet an ecological finding about
predators. **Not attributed. Do not build on it without separating founder variation from the profile.**
