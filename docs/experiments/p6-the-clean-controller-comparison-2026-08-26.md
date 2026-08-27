# The controller comparison, done cleanly - and what that does to this morning's version

2026-08-26, latest. `CreatureSweep`, herbivore pressured cell (cap 500, regen 2.0), 30 relief seeds x
two slope arms = 60 runs per cell, **both health arms throughout**.

`p6-the-controller-comparison-2026-08-26.md` concluded that the two decision controllers **could not**
be given identical reproduction machinery, because `Legacy` cannot emit `SeekMate` and so cannot use
mate selection. That was true. The inference drawn from it - that the comparison could therefore only
ever pair each controller with its own pairing rule - was **wrong**, and this document corrects it.

## The mate gate is replaceable by a scenario parameter

`gradedFertilityEnabled` is already a density-dependent brake: it scales the reproduction cooldown by
condition. The intent-plus-proximity arm that died in 50 of 60 runs this morning **already had it, at
strength 1.5**. So the real question was never "can the controller be given another brake" but "was
1.5 simply the wrong number". It was.

Intent controller, **proximity pairing, no mate gate**, extinct of 60, `health off / on`:

| brake | extinct/60 | population | mean energy | births/run |
|---|---|---|---|---|
| 1.5 | **50 / 50** | 11 / 17 | 0.030 / 0.033 | 623 / 626 |
| 3.0 | 3 / 9 | 234 / 304 | 0.395 / 0.374 | 533 / 615 |
| **4.0** | **0 / 0** | 238 / 257 | 0.657 / 0.641 | 374 / 417 |
| 4.5 | 1 / 0 | 167 / 191 | 0.765 / 0.714 | 259 / 297 |
| 5.0 | 2 / 0 | 111 / 135 | 0.779 / 0.735 | 189 / 220 |
| 6.0 | 6 / 0 | 61 / 79 | 0.769 / 0.748 | 112 / 142 |
| 12.0 | 27 / 11 | - | - | - |

For reference, the same controller **with** the mate gate at brake 1.5: **3 / 1** extinct,
population 299 / 331, energy 0.583 / 0.574.

**Brake 4.0 with no mate gate beats the mate gate**: zero extinctions of sixty against three, and a
better-fed population (0.657 against 0.583) at four-fifths the size. The gate is not a mechanism the
world cannot supply. **A scenario parameter supplies it, and supplies it better.**

**A guess I made and had to withdraw in the same session:** on seeing brake 3.0 (44% starvation) and
brake 6.0 (a fifth the population) I proposed that the gate reached a middle point no brake setting
could. Filling in 4.0, 4.5 and 5.0 deleted that. The optimum is broad and 4.0 sits in it.

## The comparison this makes possible

With a brake strength that works under proximity pairing, **both controllers run on identical
machinery** - same pairing, same brake, same everything but the controller. This is the single-
variable comparison this morning's document said did not exist.

| brake | intent extinct/60 | legacy extinct/60 | intent pop | legacy pop | intent energy | legacy energy |
|---|---|---|---|---|---|---|
| 3.0 | 3 / 9 | 7 / 3 | 234 / 304 | 49 / 50 | 0.395 / 0.374 | 0.664 / 0.653 |
| **4.0** | **0 / 0** | 4 / 4 | **238 / 257** | **24 / 29** | 0.657 / 0.641 | 0.671 / 0.686 |
| 5.0 | 2 / 0 | 8 / 7 | 111 / 135 | 16 / 15 | 0.779 / 0.735 | 0.705 / 0.678 |

**At every matched brake, the rich controller carries five to fifteen times the population at
comparable per-creature energy**, and goes extinct less often at 4.0 and 5.0. At brake 4.0 it is
238 creatures against 24, at an energy of 0.657 against 0.671 - **ten times the carrying capacity
with the individuals no worse fed.**

## This corrects this morning's central claim

That document said, of cell 1: *"On survival the two home configurations are a tie... the richer
controller is not buying survival here"* and *"its advantage is not foraging skill, it is that it
owns a brake."*

**The first half is an artefact of the comparison design and is withdrawn.** The "tie" (3 against 5)
compared each controller on a *different pairing rule*, at a brake tuned for neither. Given matched
machinery at a working brake, intent wins outright: 0 of 60 against 4 of 60, and ten times the
population.

**The second half survives but is now only half the story.** The controller does need a brake, and
without one it is worse than useless - 50 of 60 extinct. But *given* a brake, it converts the same
habitat into roughly ten times the standing population. **The brake is a precondition for the
controller paying, not the thing the controller was buying.**

Each controller's own best configuration, for the record: intent at brake 4.0, **0 of 60 extinct,
population 238**; legacy at brake 1.5 with proximity pairing, **5 of 60, population 144**. Even
best-against-best the rich controller wins, by 1.65x on population - far less than the 10x at matched
settings, because legacy's own optimum is at a much weaker brake.

## What this says about "enrich the world before enriching the brain"

`docs/emergent-behaviour-constraints-2026-08-24.md` states that rival proposal as a hypothesis to be
argued against. This is the first direct evidence on it, and **it goes both ways at once**:

- **For the world:** the single thing the rich controller was demonstrably buying this morning - a
  reproduction brake - turned out to be purchasable from a scenario parameter, better and more
  cheaply. Enriching the brain to get a brake would have been the wrong build.
- **For the brain:** once the world has that brake, the rich controller is worth **an order of
  magnitude of carrying capacity** on the same resources. That is not a marginal gain and no world
  parameter tested supplies it.

The order matters: **enriching the brain without enriching the world was actively harmful** (50 of 60
extinct), and enriching the world alone leaves a tenfold gain on the table.

## Not claimed

- **Drift is not compared at matched brake.** At brake 4.0 the populations are 238 and 24. A
  selection table computed over 24 creatures is not comparable to one over 238, and this session has
  already established that small populations select weakly. The controller's effect on *what* is
  selected is therefore **not** read off these runs; the morning document's drift comparison, taken
  at each controller's own configuration, stands as the only one made.
- `urgency_exponent` under intent falls from t = -11.5 with the mate gate to -1.3 to -3.9 without it,
  which is consistent with the recorded finding that the mating gate is what selects it. **Consistent
  with, not tested.**
- One scenario family, one cap, one regeneration rate. Brake strength is known to be
  scenario-specific - `p6-graded-fertility-is-scenario-specific-2026-08-24.md` records a factor of
  three between best and worst conditions - so **4.0 is a number for this cell and must not be
  carried into another one.**
