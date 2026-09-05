# The C1 collapse is an overshoot, and its outcome is bimodal rather than a fade

**Date:** 2026-09-03
**Status:** finding. **No biological code changed.** The cell is the recorded one, held fixed; the
only difference from the 2026-08-30 measurement is run length.
**Harness:** `tools/CreatureSweep --deaths 24 500 --regen=2.0 --brake=1.0 --predation --gate=0.45
--mate-selection=off --ticks=36000`, and the same with `--health-recovery`.
**Raw output:** `p6-c1-collapse-curve-24seeds-36000-health{off,on}-2026-09-03.txt`, beside this file.
**Plan:** `docs/superpowers/plans/2026-09-03-run-length-validity-audit.md`, Task 3.

Answers the question `p6-the-recorded-cell-is-a-transient-2026-09-03.md` left open: *when* the
collapse happens, and whether it is a smooth decline or a late crash. The sweep that found the
collapse reported final populations only.

---

## Both arms reproduce the recorded result exactly

Before anything else in the curve is read: the endpoints match the record to the seed.

| arm | recorded 2026-09-03 | measured here |
|---|---|---|
| health recovery OFF | 3 of 24 surviving — seeds 48:173, 50:202, 52:410 | 3 of 24, populations 173 / 202 / 410 |
| health recovery ON | 1 of 24 surviving — seed 50, population 202 | 1 of 24, population 202 |

The instrument agrees with a known answer, so the eight rows leading up to that answer are the same
measurement continued rather than a different one.

## The curve: it is neither of the two shapes proposed

Health recovery OFF, 24 seeds, all-world mean population and the death mix within each interval:

```
  point     tick    alive    pop(all)  pop(alive)  energy |  starv  dehyd    age health   pred
  1/9        4000    24/24        40.0        40.0   0.631 |   4.5%   5.2%  81.5%   1.5%   7.3%
  2/9        8000    24/24       245.7       245.7   0.551 |  15.6%   6.3%  72.3%   1.2%   4.6%
  1/3 3/9   12000    22/24       198.4       216.4   0.373 |  52.1%   4.5%  27.7%   1.5%  14.1%
  4/9       16000    13/24        50.9        94.0   0.381 |  47.6%   6.4%  24.9%   1.8%  19.4%
  5/9       20000     6/24         8.6        34.3   0.568 |  57.8%   5.1%  15.5%   1.6%  20.0%
  2/3 6/9   24000     5/24        14.3        68.8   0.587 |  19.5%   4.1%  24.3%   3.0%  49.1%
  7/9       28000     4/24        15.9        95.5   0.537 |  30.8%   2.0%  21.2%   0.5%  45.5%
  8/9       32000     4/24        18.9       113.5   0.525 |  18.2%   0.6%   7.3%   0.1%  73.8%
  3/3 9/9   36000     3/24        32.7       261.7   0.539 |  10.5%   0.0%   8.9%   0.1%  80.4%
```

Health recovery ON is the same shape one step worse: 24 / 24 / 21 / 13 / 3 / 3 / 1 / 1 / 1 worlds
alive, peaking at 258.5 at tick 8,000.

**It is an overshoot-and-starve, and the crash is in the middle third.** The population roughly
sextuples to a peak near 250 by tick 8,000, then loses nine worlds between 12,000 and 16,000 and
another seven by 20,000. Starvation goes 4.5% -> 15.6% -> 52.1% -> 47.6% -> 57.8% of interval deaths
across that stretch while age deaths fall from 81.5% to 15.5%.

**The predeclared expectation was a smooth decline** — the reasoning being that a population with no
regulator should lose ground from the beginning. That is wrong, and recorded as wrong: the population
grows for the first quarter of the run. The overshoot is the mechanism, and a boom is what an absent
brake looks like before it is what a collapse looks like.

**The recorded 12,000-tick measurement sits one sample past the peak**, on the descending limb, with
two worlds already gone and starvation already the majority cause in that interval. Every number in
`p3-digestion-strategies-2026-08-30.md` was taken there.

## The outcome is bimodal, and no final population can say so

The three surviving worlds at 36,000 end at **173, 202 and 410**, with mean energy 0.539 — *larger*
than the alive-conditioned population at the recorded 12,000-tick reading. This is not a population
fading everywhere. Twenty-one worlds die and three do well.

**"3 of 24 surviving, mean final population 261.7" is compatible with both stories and distinguishes
neither.** A general fade in which every world shrinks and most cross zero, and a bimodal split in
which most worlds crash and the rest reach a working population, produce the same summary row. They
have different causes and different implications, and the difference is only visible in the
trajectory.

Which of the two is true here is now recorded: **bimodal**. What decides which side a seed lands on is
not established and is not investigated here.

## The criterion caught this on its second clause, not its first

The persistence criterion predeclared for this milestone has two clauses: a monotone decline of the
all-world mean across the last third, **or** worlds alive at the end below 0.85 of their one-third
count.

On this cell the **trend clause reads `no`**. By tick 28,000 only four worlds remain, they grow, and
the all-world mean rises 15.9 -> 18.9 -> 32.7 across the last third — an increase of 105%, in a cell
that has lost 21 of 24 worlds. The health-on arm is starker: one world, growing, +143%.

The survival clause is what returns COLLAPSING (`alive(9)=3 < 0.85 x alive(3)=22`).

**Had the criterion been stated as a trend alone — which is how the plan first proposed it — this cell
would have escaped its own test.** The reason is arithmetic and general: the alive-conditioned mean
averages only survivors, so extinction removes the worst worlds from the average, and even the
all-world mean can rise once the survivors' growth outweighs the small number still falling. A trend
statistic on a shrinking sample is not measuring what it appears to measure.

## What this pins down, and what it does not

**Established:**

- The collapse is an overshoot: growth to a peak near 250 by tick 8,000, crash between 12,000 and
  20,000, in both health arms.
- The recorded 12,000-tick reading is one sample past that peak.
- Starvation is the mechanism by interval share; predation's share rises late only because it is a
  larger fraction of a much smaller number of deaths.
- The outcome is bimodal, not a general fade.
- Both arms reproduce the recorded endpoint exactly.

**Not established:**

- **What separates a surviving seed from a dying one.** Three seeds survive in both arms' vicinity
  and nothing here explains why those.
- **Whether the survivors are stable or merely later.** Three worlds growing at 36,000 is not a
  steady state, and this run cannot see past its own horizon.
- **Any tuning conclusion.** No brake, cap or regeneration value is proposed or searched for; that is
  a biological change and belongs to separate work.
