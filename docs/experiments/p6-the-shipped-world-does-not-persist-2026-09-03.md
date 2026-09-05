# The same layout is stable at cap 96 and extinct at cap 500 with the 0.75 brake

**Date:** 2026-09-03
**Status:** finding. **No biological code changed and nothing was tuned.** The only difference from
the recorded measurement is run length. **The shipped scenario has not been touched.**
**Harness:** `tools/SitePilot --seeds=24 --arms=23,32 --ticks=36000`. `--ticks=` was added to
`SitePilot` for this run (`364d176`); its default is unchanged at 12,000, so every committed SitePilot
output reproduces.
**Raw output:** `p6-y-persistence-24seeds-36000-2026-09-03.txt`, beside this file.
**Plan:** `docs/superpowers/plans/2026-09-03-run-length-validity-audit.md`, Task 4.

---

## The attribution, first

**The same four-way split layout that dies at cap 500 is stable at cap 96.** Three arms, 24 seeds
each, identical seeds, identical layout geometry between arms 23 and 32:

| arm | alive at 36,000 | population | mean energy | age share | last-third trend |
|---|---:|---:|---:|---:|---|
| control (`Y`, 6 food sites, cap 96) | **22 / 24** | 95.7 | 0.828 | 99.4% | +0.1%, flat |
| split-4 spread-6 layout, **cap 96** | **21 / 24** | 95.9 | 0.837 | 99.9% | +0.2%, flat |
| the same layout, **cap 500, brake 0.75** | **0 / 24** | — | — | — | **-100%** |

Both cap-96 arms are **level from 12,000 ticks to 36,000**: population steady near 96, worlds alive
unchanged after tick 8,000, mean energy drifting slightly *up* to 0.83, and 99.9% of deaths old age
with starvation at 0.0-0.1% of every interval. Whatever the third arm is, it is **not** the model
being unable to sustain a population over 36,000 ticks, and it is not the split layout.

That distinction is the reason both comparators were run. Without them the third arm reads as a
statement about the simulation. With them it is a statement about two configuration values.

**These arms are an attribution control and nothing else.** They are not a recommendation, and the
fact that cap 96 is also the pre-2026-08-30 configuration does not make this document a proposal to
return to it. **Nothing here proposes a brake or cap value, and none was searched for.**

## The third arm

`Y`'s shipped configuration — `maximumPopulation` 500, `gradedFertilityEnabled` at strength 0.75, on
the four-way split layout — over 36,000 ticks, 24 seeds:

```
  point     tick    alive    pop(all)  pop(alive)  energy | deaths within the interval
                                                          |  starv  dehyd    age health   pred
  1/9        4000    24/24        13.7        13.7   0.772 |   0.0%   1.1%  97.8%   1.1%   0.0%
  2/9        8000    21/24        48.7        55.7   0.785 |   0.0%   0.0%  99.4%   0.6%   0.0%
  1/3 3/9   12000    20/24       128.4       154.1   0.602 |  22.9%   0.1%  76.9%   0.1%   0.0%
  4/9       16000    12/24        65.4       130.8   0.513 |  65.6%   0.3%  34.1%   0.0%   0.0%
  5/9       20000     6/24        16.7        66.7   0.668 |  69.0%   0.8%  29.8%   0.4%   0.0%
  2/3 6/9   24000     3/24        29.5       235.7   0.599 |  43.0%   0.8%  55.8%   0.5%   0.0%
  7/9       28000     2/24         5.3        63.5   0.782 |  75.1%   0.4%  24.6%   0.0%   0.0%
  8/9       32000     1/24         0.2         4.0   0.839 |  53.7%   0.0%  46.3%   0.0%   0.0%
  3/3 9/9   36000     0/24         0.0           -       - |   0.0%   0.0% 100.0%   0.0%   0.0%
```

**Every world is dead by 36,000 ticks.** It fails both clauses of the persistence criterion
predeclared before the run: the all-world mean declines monotonically across the last third
(5.3 -> 0.2 -> 0.0), and worlds alive at the end are zero against 20 at one third.

It is also **worse than the cell recorded as a collapse**: C1, at brake 1.0, keeps 3 of 24
(`p6-the-recorded-cell-is-a-transient-2026-09-03.md`). This one keeps none. That ordering is the
direction the recorded brake dial predicts, and it places the shipped setting at the weak end of it.

## The positive control that licenses the rest of the curve

The 12,000-tick row is **the recorded measurement, reproduced to the digit**: 20 of 24 worlds alive,
population 154.1, mean energy 0.602 — the numbers in
`p6-y-is-food-limited-2026-08-30.md` that the shipping decision was made on. Same tool, same arm, same
seeds from 42. The only thing that differs is that the run continues past the point where it used to
stop.

That matters more than it looks. An instrument that had drifted, or an arm that had quietly stopped
being the recorded one, would show up here as a disagreement at 12,000. It agrees exactly, so the rows
after 12,000 are the same measurement continued rather than a different measurement.

One statistic that must not be conflated: the recorded **5.4% starvation** is a share of all deaths
over a whole 12,000-tick run. The 22.9% in the table above is the share **within the third interval
alone**, 8,000 to 12,000. Both are true of the same run; they are different denominators. The interval
figures are what show the change — 0.0%, 0.0%, 22.9%, 65.6%, 69.0% — and a whole-run share cannot,
because it averages the quiet beginning into the collapse.

## What the recorded reading was actually looking at

The population peaks between 8,000 and 12,000 ticks and is falling by the next sample. **The recorded
12,000-tick reading sits on the peak**, one sample before the crash, with starvation already at 22.9%
of deaths in that interval and rising to 65.6% in the next.

`p6-y-is-food-limited-2026-08-30.md` chose strength 0.75 from five survival counts at 12,000 —
16 / 17 / 20 / 20 / 16 of 24 across no brake / 0.5 / 0.75 / 1.0 / 1.5 — and chose it because
**survival is not monotone** in brake strength. The dial itself was real and is not in question: the
starvation column 43.1 / 15.9 / 5.4 / 0.7 / 0.3 is a clean monotone response and it reproduces.

**What fails is the horizon, not the dial.** All five of those survival counts are counts at a boom
peak. None of them is evidence about whether a population lives, because at 12,000 ticks in this cell
family no world has yet had the chance to die. A number that cannot distinguish survival from
pre-collapse cannot be the statistic a survival decision rests on, whichever value it favours.

## The prediction, declared before the run, and failed

Predeclared in the plan on 2026-09-03, before this run started: **`Y` PERSISTENT.** The reasoning was
that the brake is weaker than C1's but the layout is different (plant-backed four-way split rather
than the consumer-defense calibration), that the recorded starvation share is only 5.4% against C1's
33.6%, that the population settles at 154 under a cap of 500 that never binds, and that brake strength
is already recorded as **not transferring between scenarios**
(`p6-graded-fertility-is-scenario-specific-2026-08-24.md`).

**The prediction was wrong, and it was wrong in the safe-sounding direction.** Every clause of that
reasoning is individually true and the conclusion drawn from them is not. "Settles at 154" was the
thing being tested and was assumed in the argument for it; the scenario-specificity of brake strength
cuts both ways and was read as though it only protected this cell. This is recorded rather than
quietly dropped because a criterion with no prediction attached cannot fail, and this one did.

## What this does and does not establish

**Established:**

- The shipped configuration is extinct in 24 of 24 worlds at 36,000 ticks, from a 24-seed run.
- The same layout at cap 96 is level from 12,000 to 36,000 in two independent arms, so this is a
  property of the cap-and-brake change, not of the layout, the terrain, or the model.
- The recorded 12,000-tick numbers reproduce exactly, so the curve after them is trustworthy.
- Starvation is the mechanism by interval share: 0.0% before the peak, 65-75% through the crash.

**Not established, and worth saying:**

- **Whether any brake strength in this configuration persists.** Not measured, not searched for, and
  deliberately so: it is a biological tuning question, it invalidates baselines, and it belongs to a
  separate explicitly biological piece of work.
- **When exactly a world dies.** The trajectory samples every 4,000 ticks; per-world extinction ticks
  are recorded in the raw output but are not analysed here.
- **Whether 36,000 is long enough to clear the cap-96 arms.** They are flat over the last two thirds,
  which is what the criterion asks, but "persistent at 36,000" is not "persistent".

## What follows from it, and what does not

The **P4a watchable** gate is measured in this configuration — it is the acceptance surface for
"resource depletion/recovery" — and the P5 history panel measurements at cap 96 are not. That is a
verdict for the gate table in the frozen spec, and **the gate table is the user's to update**. This
document does not touch it.

**Not** a proposal to change the shipped scenario, its brake, or its cap.
