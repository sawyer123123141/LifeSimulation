# DRAFT — extension of the graded-seeding arm past 24,000 ticks

**Status: DRAFT, NOT APPROVED, NOT RUN.** Awaiting the user's ruling. Nothing in this document has
been executed and no code has been changed for it. On approval it is committed **unedited** before the
first run, and results are appended in a later commit.

**Plan:** `docs/superpowers/plans/2026-09-05-regulation-first-arms.md`, Task 4 follow-up.
**Extends:** `docs/experiments/p6-graded-seeding-arm-2026-09-06.md`, which returned `alive(9) = 20 of
24` against a threshold of 21 — **COLLAPSING** under the 2026-09-09 ruling — while taking the plant
community from 1.0 patches to 23.0 and final biomass from 70.2 to 985.4.

**No biological value is tuned here.** Brake, cap, regeneration, gate, scenario and seeds are held at
the arm's recorded values. The only thing that moves is run length. **No parameter value is proposed.**

---

## 1. Two blockers that must be cleared before this run is worth making

Both are reporting defects, not biology. Neither can be fixed inside this document; both need approval.

### 1.1 There are no per-seed rows

`CreatureSweep --deaths` prints aggregates only: all-world mean trajectories, and a final-population
five-number line computed over **surviving worlds only** (`Deaths.cs` does `continue` on extinction
before adding to the sample). There is no seed column anywhere in its output.

Consequently the 24,000-tick arm cannot answer, from its committed artefact: which seeds survived,
paired per-seed differences against the control, per-seed plant patch count or biomass, whether the
survivors fall into distinct regimes, or how many worlds touch the cap. **Running a longer version of
the same reporter reproduces exactly that blindness at three times the cost.**

The main (non-`--deaths`) sweep mode already writes a per-seed CSV — `arm,seed,hash,population,
extinct,energy,...` — so the pattern exists and is not new work. What it lacks is the plant columns.

### 1.2 Nine samples cannot resolve an oscillation at this length

`Trajectory.SampleCount` is a fixed 9 regardless of run length. At 24,000 ticks that is one sample per
2,666 ticks. **The arm's entire crash occupies at most one such sample** — 277.4 at tick 13,333, 121.4
at 16,000.

| run length | ticks per sample |
|---:|---:|
| 24,000 (recorded) | 2,666 |
| 48,000 | 5,333 |
| 72,000 | 8,000 |

At 5,333 or 8,000 ticks per sample a full crash-and-partial-recovery fits **inside one sample** and is
invisible. A longer run with nine samples returns a smoother curve, not a better-resolved one, and
cannot distinguish sustained oscillation from a damped approach to equilibrium — which is the main
question the extension exists to answer.

**Recommendation:** hold sample spacing near the recorded 2,666 ticks rather than sample count, so the
extension is comparable to the arm it extends. That is a tools-only change to `Trajectory`, touches no
simulation code and no hash, and must be approved separately.

**If neither blocker is cleared**, this extension should be scoped down in writing to the survival
question alone, and the oscillation, regime and cap-confounding criteria below struck rather than
reported against inadequate data.

---

## 2. How long, and why not 48,000

**Recommended: 72,000 ticks.** 48,000 is defensible only under the most optimistic reading of the one
cycle that has been observed, and is likely to return inconclusive.

**What is actually measured about the arm's cycle**, and it is one incomplete cycle:

- Crash: peak 277.4 at tick 13,333 to trough 121.4 at 16,000. **At most 2,666 ticks**, and the true
  duration is shorter than the sampling grid can show.
- Recovery: 121.4 at 16,000, rising through 127.5, 198.3 to 213.0 at 24,000. **At least 8,000 ticks
  and still rising at the horizon** — the recovery never completed, so the period was never measured.

That gives a **lower bound on the period of roughly 11,000 ticks and no upper bound.** Everything
below follows from refusing to pretend the upper bound is known.

Distinguishing sustained oscillation from a damped approach, and both from a delayed collapse,
requires at least **three complete peak-to-peak cycles** after the first peak at 13,333:

| assumed period | ticks needed for three cycles |
|---:|---:|
| 11,000 (the measured lower bound) | 46,300 |
| 15,000 | 58,300 |
| 19,500 | 71,800 |

**48,000 clears only the lower-bound case.** If the period is anything above about 11,500 — and the
unfinished 8,000-tick recovery suggests it is — 48,000 buys two cycles or fewer and the run returns
inconclusive on its central question. **72,000 covers the range up to about 19,500.**

**Cost is not the constraint and must not be allowed to choose the horizon.** The triage records seven
cells at 24,000 x 6 seeds running concurrently in under a minute; the two 24-seed arms here took a few
minutes. Tripling the length is minutes, not hours.

**The other reason to over-provision.** This project has now twice chosen a horizon and had the answer
change at the next one — 12,000 for the whole Regime B corpus, then 24,000 for the shipped `Y` cell.
Both times the horizon was chosen from a boom that had not finished. **The arm's last third is rising
at 24,000**, which is the same shape, and choosing 48,000 from an unmeasured period would be the third
instance of the same error rather than a correction of the first two.

**Secondary reading available at no cost:** at 72,000 the sample at tick 24,000 must reproduce the
recorded arm exactly. That is the instrument check, and if it fails nothing else may be read.

**Non-negotiable regardless of horizon:** the run reports whether the trajectory is still rising at its
final sample. If it is, the horizon was again placed inside a boom and the result is inconclusive by
construction — see §3.5. **A horizon is not a verdict.**

---

## 3. Criteria, defined before any result exists

All read on the all-world mean, extinct worlds counted as zero, at 24 seeds. `alive(k)` is worlds with
a living population at sample `k`; `first` and `last` denote the first and last samples of the final
third. The persistence rule is the 2026-09-03 declaration as ruled on 2026-09-09: **the integer
threshold is `ceil(0.85 x alive(3))`.**

### 3.1 SUCCESS — persistence

**All three, together:**

1. `alive(last) >= ceil(0.85 x alive(3))`.
2. No monotone decline across the final third of the all-world mean.
3. **The final sample is not a local maximum** — the trajectory is flat or falling into the horizon,
   not rising. A rising endpoint is §3.5, not success, whatever the alive count.

Note that `alive(3)` is now measured at tick 24,000 rather than 8,000. This is stated so the
denominator cannot be argued about afterwards.

### 3.2 COLLAPSE

**Either** `alive(last) < ceil(0.85 x alive(3))`, **or** a monotone decline across the final third.
Reported in two sub-cases, because they have different causes: **total** (`alive(last) = 0`) and
**partial**. The arm's own 24,000-tick result is a partial collapse under this rule.

### 3.3 OSCILLATION

Structural, so it needs no amplitude threshold and no tuned number. A **cycle** is a local maximum,
then a local minimum below it, then a local maximum above that minimum, each separated by at least one
sample.

- **SUSTAINED** — at least two complete cycles, and successive peak values do **not** decline
  monotonically.
- **DAMPED** — at least two complete cycles, and successive peak values decline monotonically while
  successive troughs rise. Damped toward a non-zero level is compatible with §3.1 success.
- **DIVERGENT** — at least two complete cycles with successive troughs falling monotonically. This is
  a collapse in progress and is reported as §3.2 regardless of the alive count at the horizon.
- **NOT RESOLVABLE** — fewer than two complete cycles visible. **This is an outcome, not a gap**, and
  it is the outcome if §1.2 is not cleared.

### 3.4 CAP CONFOUNDING

The cap is `maximumPopulation` = 500, and the source's own test is quoted rather than invented: *"A
carrying capacity produces a distribution; a cap produces a constant."*

Reported, per seed, and requiring the §1.1 per-seed rows:

- **count of worlds whose population equals 500 at any sample**, and at how many consecutive samples;
- **the aggregate recomputed with cap-contacting worlds excluded**, printed beside the full aggregate.

**Confounded** if cap-contacting worlds are more than a quarter of the survivors, **or** if excluding
them moves the verdict across the §3.1 threshold. A confounded PERSISTENT is reported as
**PERSISTENT (cap-confounded)** and does **not** count as success. The 24,000-tick arm already has at
least one world at exactly 500 out of 20 survivors; whether there are more is unknown for want of
per-seed rows.

### 3.5 INCONCLUSIVE

Any of:

- the all-world mean is **rising across the final two samples** and no second peak has occurred — the
  horizon is inside a boom again;
- fewer than two complete cycles are resolvable (§3.3);
- the tick-24,000 sample does not reproduce the recorded arm.

**An inconclusive result is reported as inconclusive and the horizon is extended, not reinterpreted.**

### 3.6 What runs only on success

The §3.3 food-supply control from the design document, unchanged and not optional: re-run the
surviving arm with the plant capacity budget or the active site count raised. A real ecological
regulator moves its plateau with the food supply; an imposed one does not.

---

## 4. Predeclared expectation

To be filled in and committed before the run, so the extension can fail. Not stated here because the
per-seed analysis that would inform it does not yet exist (§1.1), and a prediction made without it
would be a guess dressed as a hypothesis.
