# The two invisible genes are now visible, combat damage is measured — and neither mechanism explains defense selection

> **ANSWERED SAME DAY** — `p6-death-is-concentrated-on-the-low-defense-tail-2026-08-26.md`. The
> remaining candidate named below was the right one. Mean `defense` of the **predated** is **0.2479**
> against **0.7190** for the living and a founder mean of 0.489. **Predation kills rarely and kills
> almost only the weakest-defended**, which is how 0.53 deaths per run produce t = 11. The mechanism
> is selective mortality on the tail.

**2026-08-26.** `tools/CreatureSweep --deaths 30 500 --regen=2.0 --brake=1.5 --predation --gate=0.45
[--health-recovery]` and `--focused` in the same cell, 12,000 ticks.

Closes the two instrument gaps named in
`p6-defense-selection-is-robust-and-my-mechanism-was-wrong-2026-08-26.md`, and the answer is not the
one that was being looked for.

## What was added

- **`MeanManeuverabilityGene` and `MeanFearGene`.** No statistic exposed them, so two of the six
  combat genes were invisible to every instrument in the project.
- **`CumulativeCombatDamage`** — total health removed by combat, the quantity the
  mortality-versus-fertility question turns on.

All three are appended to `SimulationStatistics` **at the end of a forty-argument positional
constructor, with defaults**. Inserting mid-list would silently reassign every later argument — which
is precisely how the predation founder profile got sterilised. **611 tests pass unchanged**, so the
additions are behaviour-inert: no pinned hash moved.

## The two newly-visible genes

| gene | drift | t |
|---|---:|---:|
| defense | +0.267 | **+10.97** |
| attack | -0.147 | **-3.84** |
| **maneuverability** | +0.081 | **+2.00** |
| **fear** | +0.032 | +0.73 |
| control | +0.0002 | +0.04 |

**Neither of the previously-invisible genes is strongly selected.** Maneuverability sits exactly on
|t| = 2 — the threshold the report itself says one column of fourteen will cross by chance — and fear
is null. **The predation result is about `defense` specifically**, not about the combat family, and it
could not have been said that way before today.

## The measurement that undercuts my own replacement mechanism

| | combat damage / run | attack hits / run | predation deaths / run | below health gate |
|---|---:|---:|---:|---:|
| ratchet | **96.6** | 6.7 | **0.53** | 1.8% |
| health recovery | **133.3** | 9.3 | **0.83** | 1.3% |

I had already withdrawn the fertility route (1.8% sterile refuted it) and concluded **mortality by
elimination**. That conclusion now looks weak too:

- **Predation kills about one creature every two runs** — 16 deaths across 30 runs — against roughly
  65 deaths per run from all causes.
- **Combat damage totals 96.6 health per run**, spread over a population near 130 across 12,000 ticks.
- **Neither channel is visibly large enough** to move a gene from 0.489 to 0.756 with t = 11.

**So both of my mechanisms are now in doubt, and I am not proposing a third.** The honest state is:
`defense` is under strong, robust, six-cell selection, and **the route by which it is selected is
unexplained.** Elimination was never a strong argument; measuring the remaining channel and finding it
also small is what makes that explicit.

## Candidates, explicitly not tested

- **Concentration.** A small number of deaths falling entirely on the low-defense tail can outrun a
  large number falling randomly. Nothing here measures the defense of the creatures that die.
- **A non-combat reader of `defense`.** Checked: `attack` and `defense` carry the *same* 0.10
  maintenance cost, so cost asymmetry is excluded — but that only rules out one alternative.
- **Selection through mate choice or wound state.** `WoundSeverity` is written at the damage site and
  nothing here follows where it is read.

**The next measurement is the defense value of the creatures that die**, which no instrument reports.
That is one number, and it would separate concentration from the rest.
