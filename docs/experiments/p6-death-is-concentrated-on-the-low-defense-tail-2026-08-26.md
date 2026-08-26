# Why `defense` is selected: predation kills rarely and kills almost only the weakest-defended

**2026-08-26.** `tools/CreatureSweep --deaths 30 500 --regen=2.0 --brake=1.5 --predation --gate=0.45
[--health-recovery]`, 12,000 ticks. Console artefact: `p6-defense-tail-2026-08-26.txt`.

Answers the question left open by `p6-the-combat-forces-are-too-small-2026-08-26.md`: `defense` is
under **t = +11** selection in a world where predation kills **0.53 creatures per run**, and neither
proposed mechanism accounted for it. The remaining candidate was **concentration** — a few deaths
falling entirely on the low-defense tail beating many deaths falling at random. Nothing reported it,
so `MeanDefenseAtDeath` and `MeanDefenseAtPredationDeath` were added at the one place that still has
the dying creature's index.

## The result

| | mean `defense` of the living | of all the dead | **of the predated** |
|---|---:|---:|---:|
| ratchet | 0.7190 | 0.6135 | **0.2479** |
| health recovery | 0.6858 | 0.5934 | **0.3169** |

**Founder mean was 0.489.**

**Predation kills creatures at defense 0.25 in a world whose living mean is 0.72.** That is the
answer. A death rate of half a creature per run is not a weak selective force when it lands almost
exclusively on the bottom of the distribution — **0.53 deaths per run, each one removing an
individual roughly a third as defended as the average survivor, is enough to produce t = 11.**

Both health arms agree, and the arm with more combat (133.3 damage per run against 96.6) has the
higher predated defense — more attacks reach further up the distribution, which is the right
direction.

## The confound, and why the result survives it

`defense` rises through the run, so **creatures that die early have lower defense simply because the
population mean was lower then**. That timing effect fully explains the "all dead" column — 0.61
against a founder mean of 0.489 and a living mean of 0.719 — and **no claim is made from it**: general
mortality is *not* shown here to be defense-biased.

**It does not explain the predated column.** At 0.248 the predated sit **below the founder mean**, so
they were drawn from the bottom of the distribution that existed at every point in the run, not merely
from an earlier and lower one.

## Status of the mechanism

- **Resolved: mortality, concentrated.** Not fertility (1.8% sterile, refuted), not diffuse mortality
  (too small), but **selective mortality on the low-defense tail.**
- **Two earlier mechanisms of mine stay withdrawn.** This is the third and it is the one with a direct
  measurement behind it rather than an elimination argument.
- **Still not measured:** whether attack behaviour targets low-defense creatures deliberately or
  simply succeeds more often against them. `PredationSystem` computes success as
  `attackPower / (attackPower + defense + …)`, so success-rate concentration is sufficient and
  targeting need not be invoked — but the two are not separated here.
