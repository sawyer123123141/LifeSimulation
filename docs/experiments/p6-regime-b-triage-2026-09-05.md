# Regime B triage: do C2, C3, C4 and C5 persist past 12,000 ticks?

> **READ THE LAST SECTION FIRST.** This document has three parts in run order: a six-seed screen
> (predeclared, then run), and a **24-seed full-power re-run** at the same length that supersedes it
> as the evidence of record. **All eleven rows are COLLAPSING at both seed counts.** The 24-seed run
> is the one that could have returned PERSISTENT; none did.

**Date:** 2026-09-05. **Status:** PREDECLARATION — written and committed **before any run started**.
Results are appended below in a later commit; nothing in this section is edited afterwards.

**Plan:** `docs/superpowers/plans/2026-09-03-run-length-validity-audit.md`, section 5 (deferred
triage). **Ledger:** `docs/experiments/cell-family-persistence-ledger.md`.

**No biological value is tuned here.** Brake, cap, regeneration and gate are held at each cell's
recorded values. No brake value is proposed. The frozen spec is untouched.

## What is being screened, and at what length

The four Regime B cells the ledger records as INDETERMINATE, at **24,000 ticks** — the length derived
in section 5 of the plan from the only two measured collapse curves (peak at tick 8,000, crash
12,000–20,000, outcome settled by 24,000).

| cell | command |
|---|---|
| C2 | `CreatureSweep --deaths 6 500 --regen=2.0 --brake=1.5 --predation --gate=0.45 --ticks=24000` |
| C3 | the same without `--predation --gate=`, at **brake 1.4, 1.5 and 1.6** (the ledger's range) |
| C4 | the same without predation, `--mate-selection=off`, at **brake 3.0 and 5.0** (the ledger's range) |
| C5 | `PlantSweep -- 6 --cap=250 --brake=1.0 --ticks=24000` |

**Six seeds, 42–47.** That is the maximum the plan's screen rule allows, and the rule governs how the
output may be read:

> A 3–6 seed screen may flag a cell for follow-up. **It may never clear one.** At that seed count the
> screen cannot distinguish 22 of 24 from 18 of 24, so "not obviously dying" is an absence of evidence
> and must be recorded as exactly that.

So the only verdicts available here are **COLLAPSING** and **INDETERMINATE**. PERSISTENT requires 20+
seeds and is out of reach by construction. A cell that survives this screen has shown one thing only:
it does not crash on C1's clock.

## The criterion, unchanged

From the ledger, predeclared 2026-09-03. Nine samples; `alive(k)` is worlds with a living population
at sample `k`; `mean(k)` is the **all-world** mean population, extinct worlds counted as zero.

- **COLLAPSING** — `mean(7) > mean(8) > mean(9)`, or `alive(9) < 0.85 × alive(3)`.
- **INDETERMINATE** — anything else at fewer than 20 seeds, which is every cell here.

At six seeds `0.85 × alive(3)` is not an integer for most values of `alive(3)`; the clause is applied
as written, so from 6 alive at one third, 5 at the end passes (5 ≥ 5.1 is false — **5 fails**) and
from 5 alive at one third, 4 passes (4 ≥ 4.25 is false — **4 fails**). Stated here before the numbers
exist: at six seeds the survival clause condemns on the loss of **one** world after the one-third
mark. That is a strict screen, and it is strict in the direction the screen is allowed to be.

## Predeclared verdicts

Stated so they can fail. The reasoning is given so that a failure is informative rather than a coin
landing the other way.

| cell | prediction | why |
|---|---|---|
| **C2** — brake 1.5, predation | **COLLAPSING** | C1 is this cell at brake 1.0 and it collapses. The brake acts on fertility only, and at 12,000 ticks brake 1.5 carries a **larger** mean population than brake 1.0 (299.4 vs 262.4, `p6-starvation-is-a-dial`) because more of its worlds are still growing. A stronger brake that permits a bigger standing crop has not removed the overshoot; the plan's own condition 1 says it may only postpone it. |
| **C3** — brake 1.4 / 1.5 / 1.6, herbivore | **COLLAPSING at all three** | Same mechanism, and predation is the one thing that removes consumers before they eat the food out, so the herbivore cell should if anything be worse than C2. The 12,000-tick reading for this family is 29–30 of 30 surviving, which is exactly the reading C1 and C7 both had at 12,000. |
| **C4** — brake 3.0 / 5.0, proximity pairing | **INDETERMINATE — not obviously dying** | This is a different regime, not a stronger version of the same one: at brake 3.0 the population self-limits at **100 under a cap of 500** with **0.0% starvation** and 98.9% age deaths. It never reaches its food supply, so there is no overshoot to unwind. The failure mode to watch for is not a crash but a **slow bleed** — a fertility brake strong enough to suppress recruitment below the age-mortality rate would show as a level, gently falling all-world mean rather than a step. If C4 collapses, that is what it will look like, and the trend clause rather than the survival clause will catch it. |
| **C5** — cap 250, brake 1.0, plants | **INDETERMINATE — not obviously dying** | Cap 250 takes starvation to 0.0% (`p6-starvation-is-a-dial`) and the population settles at 63–67, a quarter of its cap, with 10–15% of worlds extinct by 12,000. Nothing is food-limited, so the C1 mechanism is absent. Note before the fact: **seeds 42 and 43 are already extinct at 12,000 in the recorded corpus**, reproduced exactly by this build, so this screen starts with two dead worlds and the survival clause is judged from `alive(3)`, not from 6. |

**Two of four predicted to collapse and two to survive the screen.** If all four collapse the screen
has learned less than it looks like it has — that is the outcome a screen this strict produces by
being strict — and the write-up must say so rather than read four condemnations as four findings.

## The mechanism claim being checked at the same time

`docs/p4a-acceptance-window-2026-09-03.md` rules that resource recovery cannot be verified in any
measured configuration, on this claim:

> hunger in this configuration arrives **with** the collapse, not before it … the brake produced
> hunger by letting the population outgrow its food, which is also how it produced the collapse.

The triage samples the death mix at all nine trajectory points in every cell, so the brake axis
0.75 / 1.0 / 1.4 / 1.5 / 1.6 / 3.0 / 5.0 comes with a starvation share per interval at no extra run.

**Predeclared: they move together.** The brake acts only on fertility, and the only channel this model
has for producing chronic hunger is a population above its food supply — a state with no negative
feedback except death. So the prediction is that every cell showing a sustained non-zero starvation
share also fails the criterion, and every cell that survives the screen shows a starvation share at or
near zero throughout.

**The falsifier, named in advance:** a cell whose starvation share is materially above zero in *every*
interval including the last third, while `alive(9) ≥ 0.85 × alive(3)`. **Brake 1.5 is the candidate**
— it is recorded at 16.2% starvation with 30 of 30 surviving at 12,000, which is the "starvation and
survival coexist" claim, and it is a 12,000-tick reading. C3 is therefore the decisive cell for this
question as well as for its own row.

If they move together, the deferred brake experiment is not a matter of picking a value and the p4a
note should say so. If they separate at some strength, the p4a claim is wrong and this document says
that plainly.

---

# Results

**Run 2026-09-05**, appended in a later commit than the predeclaration above, which is unedited.
Console artefact: `p6-regime-b-triage-24000-2026-09-05.txt`. Plant raw:
`p6-plant-cap250-brake1.0-6seeds-24000ticks-2026-09-05.csv`.

## Every cell collapses. Four of four.

| cell | brake | `alive(3)` | `alive(9)` | `0.85 x alive(3)` | trend clause | verdict |
|---|---:|---:|---:|---:|---|---|
| C2, predation, gate 0.45 | 1.5 | 4 / 6 | **2 / 6** | 3.40 | no | **COLLAPSING** |
| C3, herbivore | 1.4 | 6 / 6 | **0 / 6** | 5.10 | no | **COLLAPSING** |
| C3, herbivore | 1.5 | 6 / 6 | **0 / 6** | 5.10 | **YES** | **COLLAPSING** |
| C3, herbivore | 1.6 | 6 / 6 | **0 / 6** | 5.10 | no | **COLLAPSING** |
| C4, proximity pairing | 3.0 | 6 / 6 | **0 / 6** | 5.10 | no | **COLLAPSING** |
| C4, proximity pairing | **4.0** | 6 / 6 | **0 / 6** | 5.10 | **YES** | **COLLAPSING** |
| C4, proximity pairing | 5.0 | 6 / 6 | **2 / 6** | 5.10 | no | **COLLAPSING** |
| C5, cap 250, contest-off / flat | 1.0 | 6 / 6 | **1 / 6** | 5.10 | no | **COLLAPSING** |
| C5, cap 250, contest-off / terrain | 1.0 | 6 / 6 | **2 / 6** | 5.10 | no | **COLLAPSING** |
| C5, cap 250, contest-on / flat | 1.0 | 6 / 6 | **1 / 6** | 5.10 | **YES** | **COLLAPSING** |
| C5, cap 250, contest-on / terrain | 1.0 | 6 / 6 | **1 / 6** | 5.10 | **YES** | **COLLAPSING** |

**Two of four predicted verdicts failed, both in the same direction.** C2 and C3 were predicted
COLLAPSING and are. **C4 and C5 were predicted "not obviously dying" and both collapse** - C4 at
brake 3.0 loses every world, and C5 loses four or five of six in all four of its arms. The reasoning
that produced those two predictions was that neither cell reaches its food supply at 12,000 ticks,
and that reasoning was measuring the wrong thing: **neither cell has reached its food supply *yet* at
12,000.** Both do by 16,000.

**The trend clause fired in 4 of 11 rows; the survival clause condemned all 11.** A criterion written
on the trend alone - which is how it was first proposed on 2026-09-03 - would have cleared seven of
these eleven. The escape is the one already recorded: a cell whose worlds die stops averaging them, so
the all-world mean can rise while the cell empties. C5 contest-off / flat is the clean example: its
last third reads **3.0 to 13.5, +350%**, on one surviving world of six.

## Each cell reads healthy at 12,000 and is gone by 24,000

The same seven commands at `--ticks=12000`, same seeds, same build. This is the instrument check: the
short-horizon reading has to reproduce before the long one can be called a continuation of it.

| cell | brake | alive at 12,000 | starvation, whole run at 12,000 | alive at 24,000 | starvation, whole run at 24,000 |
|---|---:|---:|---:|---:|---:|
| C2 predation | 1.5 | 4 / 6 | 4.5% | 2 / 6 | **48.7%** |
| C3 herbivore | 1.4 | **6 / 6** | **0.0%** | **0 / 6** | **50.5%** |
| C3 herbivore | 1.5 | **6 / 6** | 7.6% | **0 / 6** | **52.8%** |
| C3 herbivore | 1.6 | **6 / 6** | 9.6% | **0 / 6** | **51.5%** |
| C4 proximity | 3.0 | **6 / 6** | 44.5% | **0 / 6** | **59.8%** |
| C4 proximity | **4.0** | **6 / 6** | 29.4% | **0 / 6** | **58.1%** |
| C4 proximity | 5.0 | **6 / 6** | 0.0% | 2 / 6 | **47.1%** |

The C3 rows reproduce the recorded plateau: `p6-the-pressured-cell-is-a-plateau-2026-08-26.md` has
29-30 of 30 surviving with starvation 4.2-27.2% across brake 1.4-1.6, and six seeds here give 6 of 6
with 0.0-9.6%. **So the recorded reading is not being contradicted. It is being continued**, and the
same worlds are all dead 12,000 ticks later.

The starvation columns are shares of a whole-run death mix, but the shift is not compositional:
C3 brake 1.4 goes from **0** starvations in 1,025 deaths at 12,000 to **2,104** in 4,168 at 24,000.

One reporting limitation to note before the next table is read: at `--ticks=24000` the nine samples
land on multiples of 2,666, so **there is no sample at tick 12,000**. Comparisons against recorded
12,000-tick figures use the separate 12,000-tick runs above, not an interpolation of the trajectory.

## The mechanism: hunger onset and collapse onset are within one sample of each other, at every brake

The question this run was asked to settle, from `docs/p4a-acceptance-window-2026-09-03.md`:

> hunger in this configuration arrives **with** the collapse, not before it ... the brake produced
> hunger by letting the population outgrow its food, which is also how it produced the collapse.

Per cell, from the nine-point trajectories: the last sample at which starvation is under 5% of deaths
in the interval, the sample at which the all-world population peaks, and the first sample at which a
world is lost after the one-third mark.

| cell | brake | last sample under 5% starvation | population peak | first world lost after 1/3 | starvation in the interval after the peak |
|---|---:|---:|---:|---:|---:|
| C7 shipped `Y` (recorded) | 0.75 | 8,000 | 12,000 | 16,000 | 65.6% |
| C2 | 1.5 | 13,333 | 13,333 | 21,333 | 47.7% |
| C3 | 1.4 | 10,666 | 13,333 | 16,000 | 72.1% |
| C3 | 1.5 | 10,666 | 13,333 | 16,000 | 71.2% |
| C3 | 1.6 | 10,666 | 13,333 | 16,000 | 69.2% |
| C4 | 3.0 | 8,000 | 10,666 | 13,333 | 68.7% |
| C4 | **4.0** | 10,666 | 13,333 | 13,333 | 74.3% |
| C4 | 5.0 | 13,333 | 16,000 | 16,000 | 75.7% |
| C5 (contest-off / flat) | 1.0 | 13,333 | 13,333 | 18,666 | 56.3% |

**They move together. There is no separation at any brake strength measured - 0.75, 1.0, 1.4, 1.5,
1.6, 3.0, 4.0 and 5.0, across four different scenario families.** In every cell starvation is at or near
zero right up to the sample at which the population peaks, is 47-76% of deaths in the very next
sample, and worlds begin disappearing in that sample or the one after it. **The gap between "nothing
is hungry" and "worlds are dying" is one to two trajectory samples - 2,666 to 5,333 ticks out of
24,000 - everywhere.**

**The predeclared falsifier was not met by any cell.** It required a cell with a materially non-zero
starvation share in *every* interval including the last third, while `alive(9) >= 0.85 x alive(3)`.
Every cell has intervals at exactly 0.0% starvation, and every cell fails the survival clause. The
candidate named in advance - brake 1.5, recorded at 16.2% starvation with 30 of 30 surviving - is
**0 of 6 alive at 24,000**, and its own hunger-free window runs to tick 10,666.

**So the p4a note's mechanism claim holds, and it is broader than the note stated it.** The note
scoped it to `Y`'s configuration. It is a property of every Regime B cell measured: the brake acts on
fertility alone, and the only channel this model has for producing hunger is a population that has
overshot its food. A population above its food supply has no negative feedback in this model except
death, so hunger and collapse are not two states that a brake value chooses between. **The deferred
brake experiment is not a matter of picking a value**, and this document says so plainly.

**Two things this does not establish**, stated because the temptation to over-read is what the
falsifier was written against:

- **It does not prove no brake value can separate them.** Eight strengths were measured, at six seeds
  each, spanning 0.75 to 5.0 - both ends, the middle, and the one value another document
  independently identified as the optimum. A strength between two measured ones
  behaving differently is not excluded by anything here. What has changed is where the burden sits: a
  proposal that some strength produces chronic non-fatal hunger now has to say why it would, given
  that the two ends and the middle behave identically.
- **It does not identify the missing mechanism.** "There is no negative feedback except death" is a
  reading of the death mix and the trajectory, not a code audit. It is the shape the data has.

## Brake strength barely moves the clock, and within one family it does not move it at all

C3 is the one clean brake axis here - 1.4, 1.5 and 1.6 differ in nothing else. All three peak at
sample 5 of 9 (tick 13,333) at 280-326, all three lose their first worlds at tick 16,000, and all
three are at 0 of 6 by tick 18,666. **Across a 14% change in brake strength the collapse does not move
by a single sample.**

Across families the timing does differ - C4 at brake 3.0 collapses *earlier* than C3 at brake 1.4 -
but **C4 differs from C3 in more than the brake**: it runs `--mate-selection=off`, proximity pairing,
which raises the birth rate. That is why its 12,000-tick starvation is 44.5% where the dial's
recorded brake-3.0 row (mate selection on) is 0.0%. **No cross-family brake comparison is licensed
here**, and the monotone brake ordering the 12,000-tick dial reported is not reproduced as an ordering
of outcomes at 24,000.

## What the ledger's screen rule permits us to say

**Nothing here clears a cell, and nothing here was meant to.** Eleven rows, all COLLAPSING, all at six
seeds. Under the rule a low-seed screen may condemn and may never clear, so a COLLAPSING verdict at
six seeds is a verdict, and there are no other verdicts in this run to weigh.

**Brake 4.0 is the row that matters most in that table.** `p6-the-clean-controller-comparison-2026-08-26.md`
searched the brake axis under proximity pairing and settled on **4.0 as the optimum** - **0 of 60**
extinct in both health arms at 12,000, population 238-257, energy 0.657. Six seeds here reproduce
that picture at 12,000 (6 of 6) and the same six worlds are **0 of 6 at 24,000**, on both clauses.
The best-performing brake value anybody in this project has identified by search does not survive
twice the horizon it was searched at.

**That is also this screen's weakness, and it is worth saying out loud rather than counting four
condemnations as four findings.** A screen strict enough to condemn on the loss of one world after
the one-third mark will condemn a great deal. What makes these eleven more than an artefact of
strictness is that **five of them lose every world**, and three more end on a single world of six.

**Cost was not the constraint the plan assumed.** The seven `CreatureSweep` cells at 24,000 ticks x 6
seeds ran concurrently in under a minute; `PlantSweep`'s 24 runs took about two. The deferred triage
was scoped at six seeds because the plan budgeted 36,000-tick compute. At these speeds a full 24-seed
run of any of these cells is minutes, so **a follow-up wanting a PERSISTENT verdict on anything should
simply run 24 seeds**; nothing about the seed count here was forced by cost.

## An instrument caveat found in passing, recorded not fixed

`PlantSweep`'s `frozen` column is `HighestPlantGeneration == 0`, and
`SimulationWorld.Statistics.cs` computes that statistic by scanning the **living** patch store, not as
a watermark (`highestPlantGeneration = Math.Max(...)` inside the loop over `Plants`). So it means "no
living patch descends from a reproduction event", which is true both when nothing ever reproduced and
when everything that did has since died. Seed 45 at 24,000 reports `frozen = 1` with **259 plant
births** and occupancy 0.0 - the whole plant community is gone, which is a different and worse finding
than "the plants never bred". The `plant_births` column disambiguates it and is already in the CSV.

**Not fixed, per field notes section 2:** it is a tools-side reading, it costs nothing today because
the disambiguating column sits beside it, and the recorded plant corpus was measured at 12,000 ticks
where no plant community had died, so no recorded `frozen` figure is affected.

## What follows for the ledger and for the recorded corpus

- **Every Regime B cell in the ledger now carries a verdict, and every one of them is COLLAPSING.**
  C1, C2, C3, C4, C5, C6 and C7. There is no cell in this project that has been shown to persist
  without the cap holding it up.
- **Nothing is retracted.** Every affected document's *comparison* result - paired arms, hash
  divergences, selection statistics, nulls - is unaffected by a shared trajectory. What is affected is
  every claim of the form *this configuration is a place a population can live*, and those get
  banners rather than withdrawals.

---

# Full-power re-run at 24 seeds — predeclaration

**Written and committed 2026-09-05, before any 24-seed run started.** Unedited afterwards.

The screen above ran at six seeds because the plan's screen rule caps a screen there, and a six-seed
run **may condemn and may never clear**. Cost turned out not to be the constraint, so all eleven rows
are re-run at **24 seeds, 24,000 ticks** — the seed count the persistence criterion requires for a
**PERSISTENT** verdict. This is the first run in the triage that *could* clear a cell.

Same commands, `--deaths 24 500 ...` and `PlantSweep -- 24 ...`, same brake strengths, same layouts,
same `--ticks=24000`. **No configuration value moves.**

## What changes about what may be said

At 24 seeds the criterion's clauses become fully available:

- **COLLAPSING** — `mean(7) > mean(8) > mean(9)`, or `alive(9) < 0.85 x alive(3)`.
- **PERSISTENT** — neither, **and** `alive(9) >= 0.85 x alive(3)`, **and** at least 20 seeds. Now
  reachable.
- **INDETERMINATE** — otherwise.

From `alive(3) = 24`, the survival clause condemns at `alive(9) < 20.4`, so 20 of 24 passes and 20 is
the first passing value. That threshold is the 2026-09-03 declaration applied unchanged.

## Predeclared verdicts

**All eleven rows COLLAPSING again**, with none upgraded to PERSISTENT or INDETERMINATE. The reason
this is a prediction rather than a formality: five of the eleven lost **every** world of six, which
twenty-four seeds cannot rescue, but three rows ended on one world of six and three on two, and at six
seeds the difference between "this cell keeps a fifth of its worlds" and "this cell keeps none" is not
resolvable. **A cell that comes back at 20 or more of 24 alive would be a cell the six-seed screen got
wrong, and it would be the headline.**

The specific rows where that could happen, named in advance so a surprise cannot be presented as
expected: **C4 at brake 5.0** and **C2 at brake 1.5** are the two creature cells that kept worlds
(2 of 6 each), and **C5 contest-off / terrain** kept 2 of 6. Those three are the candidates. C3 at all
three strengths and C4 at 3.0 and 4.0 lost everything and are predicted to stay lost.

## The mechanism falsifier, unchanged

A cell whose starvation share is materially non-zero in **every** interval including the last third,
while `alive(9) >= 0.85 x alive(3)`. Predeclared outcome: **not met at 24 seeds either**, and the
per-cell gap between the last hunger-clean sample and the first sample losing worlds stays at one to
two samples. It is reported **per cell** below, not only as a range, because a range can hide a single
cell that separates.

---

# Full-power results — 24 seeds, 24,000 ticks

**Run 2026-09-05**, after the predeclaration above, which is unedited. Console artefact:
`p6-regime-b-triage-24seeds-24000-2026-09-05.txt`. Plant raw:
`p6-plant-cap250-brake1.0-24seeds-24000ticks-2026-09-05.csv`.

**The headline is that there is no headline: no cell reads differently at 24 seeds.** All eleven rows
are **COLLAPSING** again, now on a run that could have returned PERSISTENT and did not for any of
them. The predeclared verdicts held, including for the three rows named in advance as the ones the
six-seed screen might have got wrong.

## The verdicts, at the seed count the criterion requires

| cell | brake | `alive(3)` | `alive(9)` | threshold `0.85 x alive(3)` | trend clause | verdict | 6-seed `alive(9)` |
|---|---:|---:|---:|---:|---|---|---|
| C2, predation, gate 0.45 | 1.5 | 18 / 24 | **7** | 15.30 | no | **COLLAPSING** | 2 / 6 |
| C3, herbivore | 1.4 | 24 / 24 | **2** | 20.40 | no | **COLLAPSING** | 0 / 6 |
| C3, herbivore | 1.5 | 24 / 24 | **0** | 20.40 | **YES** | **COLLAPSING** | 0 / 6 |
| C3, herbivore | 1.6 | 24 / 24 | **1** | 20.40 | **YES** | **COLLAPSING** | 0 / 6 |
| C4, proximity pairing | 3.0 | 24 / 24 | **2** | 20.40 | **YES** | **COLLAPSING** | 0 / 6 |
| C4, proximity pairing | 4.0 | 24 / 24 | **1** | 20.40 | **YES** | **COLLAPSING** | 0 / 6 |
| C4, proximity pairing | 5.0 | 24 / 24 | **11** | 20.40 | **YES** | **COLLAPSING** | 2 / 6 |
| C5, cap 250, contest-off / flat | 1.0 | 22 / 24 | **3** | 18.70 | **YES** | **COLLAPSING** | 1 / 6 |
| C5, cap 250, contest-off / terrain | 1.0 | 23 / 24 | **7** | 19.55 | **YES** | **COLLAPSING** | 2 / 6 |
| C5, cap 250, contest-on / flat | 1.0 | 24 / 24 | **8** | 20.40 | no | **COLLAPSING** | 1 / 6 |
| C5, cap 250, contest-on / terrain | 1.0 | 22 / 24 | **6** | 18.70 | no | **COLLAPSING** | 1 / 6 |

**Every row misses its threshold by a wide margin.** The closest is C4 at brake 5.0 — 11 of 24 against
a threshold of 20.4 — and it fails the trend clause as well, its all-world mean running
137.3 -> 124.5 -> 83.1 over the last third.

**No verdict changed, and no row was close enough that six seeds had been lucky.** What did change is
the *degree*, and in the direction of "less total" rather than "less severe":

- **Four rows that lost every world of six keep one or two of 24** — C3 at 1.4 and 1.6, C4 at 3.0 and
  4.0. Six seeds happened to draw no survivor in cells whose true survivor fraction is 4-8%.
- **C3 at brake 1.5 is the one genuine zero: 0 of 24.**
- **C4 at brake 5.0 is the least-collapsing cell in the project**, at 11 of 24. That is worth saying
  plainly rather than burying in a table of condemnations: it is nearly half its worlds, it is far
  more than the six-seed screen suggested, and **it is still a failure on both clauses**. A cell that
  loses thirteen of twenty-four worlds and whose mean population falls 40% across its last third is
  not a place a population lives.
- **C2's establishment losses are real, not a small-sample artefact**: 18 of 24 alive at one third,
  reproducing the 4 of 6 seen at six seeds. The predation cell loses a quarter of its worlds before
  the collapse starts, which is a separate phenomenon and is why its threshold is 15.3 rather
  than 20.4.

**The trend clause fired in 7 of 11 rows at 24 seeds, against 4 of 11 at six.** That is the opposite
of the drift one might expect and it has a mechanical cause: the all-world mean over 24 worlds is not
dominated by a single lucky survivor the way a mean over 6 is, so the survivorship rise that let cells
escape the trend clause at six seeds is diluted. **The survival clause still condemns all eleven and
is still doing the decisive work** — but the gap between the two clauses narrows with seed count, and
that is a fact about the criterion worth recording.

## Whole-run death mix at 24 seeds

| cell | brake | deaths | starvation |
|---|---:|---:|---:|
| C2 predation | 1.5 | 8,805 | **49.3%** |
| C3 herbivore | 1.4 | 17,011 | **45.9%** |
| C3 herbivore | 1.5 | 16,996 | **49.8%** |
| C3 herbivore | 1.6 | 16,446 | **52.0%** |
| C4 proximity | 3.0 | 17,048 | **56.3%** |
| C4 proximity | 4.0 | 17,307 | **54.7%** |
| C4 proximity | 5.0 | 13,263 | **42.0%** |

Every cell reproduces its six-seed figure to within a few points, on roughly four times the deaths.

## The mechanism, per cell, at 24 seeds

The falsifier, unchanged: a cell whose starvation share is materially non-zero in **every** interval
including the last third, while `alive(9) >= 0.85 x alive(3)`. **Not met by any of the eleven.** Every
cell has intervals at or below 0.5% starvation, and every cell fails the survival clause by a margin.

Reported per cell rather than as a range, because a range can hide one cell that separates. `hunger`
is the first sample whose interval carries **5% or more** starvation; `peak` is the sample at which
the all-world mean population is highest; `loss` is the first sample at or after the peak at which the
alive count falls; `gap` is `loss - hunger` in samples, one sample being 2,666 ticks.

| cell | brake | last clean sample | `hunger` | `peak` | `loss` | **gap** |
|---|---:|---:|---:|---:|---:|---:|
| C2 predation | 1.5 | 10,666 (2.4%) | 13,333 | 13,333 | 16,000 | **1** |
| C3 herbivore | 1.4 | 8,000 (0.0%) | 10,666 | 13,333 | 13,333 | **1** |
| C3 herbivore | 1.5 | 10,666 (0.4%) | 13,333 | 10,666 | 13,333 | **0** |
| C3 herbivore | 1.6 | 10,666 (0.3%) | 13,333 | 13,333 | 13,333 | **0** |
| C4 proximity | 3.0 | 8,000 (0.0%) | 10,666 | 10,666 | 13,333 | **1** |
| C4 proximity | 4.0 | 10,666 (1.3%) | 13,333 | 13,333 | 13,333 | **0** |
| C4 proximity | 5.0 | 13,333 (1.4%) | 16,000 | 16,000 | 16,000 | **0** |
| C5 off / flat | 1.0 | 10,666 (0.0%) | 13,333 | 13,333 | 16,000 | **1** |
| C5 off / terrain | 1.0 | 10,666 (0.0%) | 13,333 | 16,000 | 18,666 | **2** |
| C5 on / flat | 1.0 | 10,666 (0.0%) | 13,333 | 13,333 | 16,000 | **1** |
| C5 on / terrain | 1.0 | 10,666 (0.0%) | 13,333 | 13,333 | 16,000 | **1** |

**Gap 0 in four cells, 1 in six, 2 in one. Nothing above two samples — 5,333 ticks out of 24,000 —
anywhere.** And `hunger` never precedes `peak` by more than one sample and never follows it by more
than one: **hunger arrives at the top of the boom in every cell**, which is the claim.

**`hunger` and `peak` coincide exactly in seven of eleven cells.** In the other four they differ by one
sample in whichever direction the 2,666-tick sampling grid happens to fall, and the two orderings both
occur (C3 at 1.4 has hunger one sample early, C3 at 1.5 one sample late), which is what a sampling
artefact looks like rather than a mechanism.

**So the mechanism claim survives at full power. Hunger onset does not separate from collapse onset at
any brake strength measured** — 0.75, 1.0, 1.4, 1.5, 1.6, 3.0, 4.0 and 5.0, across four scenario
families, now at 24 seeds per cell.

**One honest complication, visible only at 24 seeds.** In three of the four C5 arms the world count
starts falling at tick 10,666, *before* hunger appears at 13,333. Those are **establishment failures**,
not the collapse: this cell is recorded at 10-15% extinction by 12,000 ticks in a corpus measured
before any of this, and the same phenomenon is the reason `p4a-acceptance-window-2026-09-03.md`
excludes `Y`'s three early losses from its collapse. The `loss` column above is measured **at or after
the population peak** precisely so that an establishment failure cannot be counted as the collapse
arriving early. Counting them would make the gaps *negative* in those arms and would be wrong.

## What the full-power run licenses that the screen did not

- **The eleven verdicts now rest on runs that could have cleared a cell.** Under the criterion,
  PERSISTENT was reachable — 20 or more of 24 alive at the end, and no monotone decline over the last
  third — and no cell came within nine worlds of it.
- **Every Regime B cell in this project is now COLLAPSING at 20+ seeds or better**: C1 and C7 at 24
  seeds and 36,000 ticks from the earlier tasks, C2-C5 here at 24 seeds and 24,000, C6 already
  recorded as collapsing inside 12,000. **Nothing in Regime B persists.**
- **The screen rule was doing its job and was not costing accuracy here.** Six seeds condemned all
  eleven and twenty-four condemned all eleven. The rule's asymmetry remains right — a six-seed run
  could not have told C4 at brake 5.0 (11 of 24) apart from a cell keeping 20 — but on these cells it
  did not mislead.
