# Cell-family persistence ledger

**Started:** 2026-09-03. **Standing document — keep it current.**

## Why this exists

On 2026-08-30 the cap-500 / `--brake=1.0` cell was recorded as 22 of 24 worlds surviving at 12,000
ticks, and every conclusion in it was read as a description of an ecology. On 2026-09-03 the same
cell, unchanged in every biological respect, was found extinct in 21 of 24 worlds at 36,000 ticks.
The only thing that differed was run length
(`p6-the-recorded-cell-is-a-transient-2026-09-03.md`).

Nobody could have caught that, because **nowhere in this repository was it written down how long any
cell had been verified to persist.** A survival count at the end of a run looks the same whether the
population is standing still or falling through the window. This file is the missing record.

## The rule

> **A cell may not be cited as a baseline — a place other results are measured — until its longest
> verified run is recorded here and exceeds the run length of the results resting on it.**

Two supporting rules, both learned the expensive way:

- **A low-seed screen may condemn a cell and may never clear one.** At 3-6 seeds the screen cannot
  distinguish 22 of 24 from 18 of 24, so "not obviously dying" is an absence of evidence. Only a run
  of at least 20 seeds, judged against the criterion below, may record PERSISTENT.
- **Do not tune brake, cap or regeneration to make a cell persist as part of a measurement task.**
  That is a biological change; it invalidates every baseline measured before it and needs to be
  decided as such.

## The criterion

Predeclared on 2026-09-03 in
`docs/superpowers/plans/2026-09-03-run-length-validity-audit.md`, before any run.

The sweeps sample nine evenly spaced points (`Trajectory`, shared by `CreatureSweep` and
`SitePilot`). Let `alive(k)` be the worlds with a living population at sample `k`, and `mean(k)` the
**all-world** mean population there, extinct worlds counted as zero.

- **COLLAPSING** — `mean(7) > mean(8) > mean(9)`, or `alive(9) < 0.85 x alive(3)`.
- **PERSISTENT** — neither of those, `alive(9) >= 0.85 x alive(3)`, and at least 20 seeds.
- **INDETERMINATE** — anything else, including every verdict at fewer than 20 seeds.

The all-world mean is the one that carries the trend: the alive-conditioned mean *rises* as a cell
collapses, because the worlds that die stop contributing to it.

## Two regimes, and only one of them is exposed

- **Regime A — the cap binds.** `maximumPopulation` 12 / 24 / 48 / 96 / 100. The population sits on
  its ceiling and the cap, not the ecology, is holding it up. These cells cannot fail the way C1 did.
  They carry the opposite qualification, already recorded: results measured with the population
  pinned.
- **Regime B — the cap does not bind and a brake is the only regulator.** cap 250 / 500 / 1000 with
  `gradedFertilityEnabled`. Every cell here is of unknown persistence until it appears below with a
  verdict.

## The ledger

| # | cell | regime | longest verified run | verdict | evidence |
|---|---|---|---|---|---|
| C1 | cap 500, regen 2.0, brake 1.0, predation, gate 0.45, proximity pairing | B | 36,000 | **COLLAPSING** | 3 of 24 (health off) and 1 of 24 (health on) alive at 36,000, from 22 and 21 at one third. Overshoot: peak ~250 at tick 8,000, crash 12,000-20,000. Bimodal — survivors end at 173/202/410. `p6-the-c1-collapse-curve-2026-09-03.md` |
| C2 | cap 500, regen 2.0, brake 1.5, predation, gate 0.45 | B | 12,000 | INDETERMINATE | never run longer |
| C3 | cap 500, regen 2.0, brake 1.4-1.6, herbivore | B | 12,000 | INDETERMINATE | never run longer |
| C4 | cap 500, regen 2.0, brake 3.0-5.0, proximity pairing | B | 12,000 | INDETERMINATE | never run longer |
| C5 | cap 250, brake 1.0, `PlantSweep` | B | 12,000 | INDETERMINATE | `PlantSweep` has no `--ticks=` yet |
| C6 | cap 250, brake 3.0, `PlantSweep` | B | 12,000 | COLLAPSING | 21 of 60 worlds extinct inside 12,000 (`p6-graded-fertility-is-scenario-specific-2026-08-24.md`) |
| C7 | **shipped `Y`: cap 500, brake 0.75, four-way split** | B | 36,000 | **COLLAPSING** | **0 of 24 alive at 36,000**, from 20 at one third. Fails both clauses. The 12,000-tick row reproduces the recorded 20/24 and population 154.1 exactly. `p6-the-shipped-world-does-not-persist-2026-09-03.md` |
| C8 | old `Y` and P4-P6 defaults: cap 48 / 96 / 100, no brake | A | 60,000 | cap-held | `p5-one-species-2026-08-30.md` |
| C9 | the four-way split layout at **cap 96**, no brake | A | 36,000 | PERSISTENT | 21 of 24 alive, level from 12,000 to 36,000, energy drifting up, 99.9% age deaths. Run as C7's **attribution control** — it is what shows the collapse belongs to the cap-and-brake change and not to the layout. **Not a recommendation.** `p6-the-shipped-world-does-not-persist-2026-09-03.md` |
| C10 | `Y`'s six-site control layout at cap 96, no brake | A | 36,000 | PERSISTENT | 22 of 24 alive, level over the last two thirds. Same run, same role as C9. |

**A PERSISTENT verdict here means "persistent at the run length in the column", never "persistent".**
C9 and C10 are level over their last two thirds at 36,000 ticks; that is what the criterion asks and
it is not a claim about 100,000.

**A Regime A row is not a recommendation.** C9 and C10 exist because a collapse with no comparator
reads as a statement about the model. They say the layout is not the cause. They say nothing about
what any cap or brake should be, and no such value is proposed anywhere in this audit.

## How to add a row

Run the cell with the trajectory reporter attached, at a length longer than anything resting on it,
and paste the verdict here with the artefact that produced it. If the verdict is INDETERMINATE, say
what would settle it. A blank cell and a cell that was measured and came back uncertain are opposite
readings and must not look alike.
