# Run-length validity: which recorded conclusions the collapse touches, and what to do

**Date:** 2026-09-03
**Status:** PLANNED — planning session only. Nothing implemented, nothing measured here.
**Spec:** `docs/superpowers/specs/2026-08-30-what-finished-means-design.md` (**FROZEN**). This plan
adds no goals to it and **proposes no change to it**. Section 4 gate-table verdicts are the user's to
make once evidence exists; this plan gathers evidence and stops there.
**Trigger:** `docs/experiments/p6-the-recorded-cell-is-a-transient-2026-09-03.md` — the cap-500 /
`--brake=1.0` / `--regen=2.0` / `--predation` / `--gate=0.45` cell is 22 of 24 surviving at 12,000
ticks and extinct in 21 of 24 (health off) / 23 of 24 (health on) at 36,000.

**The finding restated as a measurement claim, because that is what it is:** *no 12,000-tick
measurement in a cell where the population cap does not bind can distinguish a steady state from a
transient.* Selection during a collapse is still selection, so nothing below is retracted. What
changes is the **scope label** on a class of results, and the discovery that the project has no
record anywhere of how long each cell has been verified to persist.

---

## 1. The organising fact: two regulator regimes, not one corpus

Every recorded cell is in exactly one of two regimes, and the exposure is entirely different.

**Regime A — the cap binds.** `maximumPopulation` 12 / 24 / 48 / 96 / 100. The population sits on the
ceiling; `p4-cap-pinning-audit-2026-08-22.md` found 4,080 runs with the population column pinned at
48. These worlds **cannot** exhibit the collapse found on 2026-09-03, because the thing that would
have to fail — ecological self-regulation — is not what is holding the population up. The cap is.
They already carry the opposite scope qualification (*"measured with the population pinned"*), which
is recorded and unaffected.

**Regime B — the cap does not bind, and a brake is the only regulator.** cap 250 / 500 / 1000 with
`gradedFertilityEnabled`. `p6-the-cap-is-the-stabiliser-2026-08-24.md` measured 23 of 24 surviving at
cap 250 against 3 of 20 at cap 500, and named the cap as the regulator; graded fertility was built to
replace it. **The 2026-09-03 result says that at strength 1.0 the replacement does not hold, and that
12,000 ticks is too short to see that it does not.** Every Regime B cell is therefore of unknown
persistence until measured, and that is the whole of the exposure.

**Nothing in Regime B has ever been run past 12,000 ticks except the one cell that was, and it
collapsed.** That is the single sentence this audit exists to record.

### Cell-family ledger — the table this project did not have

Longest verified run per distinct cell, from the recorded documents.

> **SUPERSEDED as a live table, 2026-09-05.** The standing version is
> `docs/experiments/cell-family-persistence-ledger.md`, and every Regime B row in it now carries a
> verdict: **C1 through C7 are all COLLAPSING.** The table below is kept as the planning snapshot that
> named the exposure, and the UNKNOWN column is what it looked like before any of them was measured.

| # | cell | regime | longest run | persistence verdict |
|---|---|---|---|---|
| C1 | cap 500, regen 2.0, **brake 1.0**, predation, gate 0.45, proximity pairing | B | **36,000** | **COLLAPSES** — 21/24 and 23/24 extinct |
| C2 | cap 500, regen 2.0, **brake 1.5**, predation, gate 0.45 (the predator-prey cell) | B | 12,000 | **UNKNOWN** |
| C3 | cap 500, regen 2.0, **brake 1.4–1.6**, herbivore (the "pressured cell" plateau) | B | 12,000 | **UNKNOWN** |
| C4 | cap 500, regen 2.0, **brake 3.0–5.0**, proximity pairing (the clean controller comparison) | B | 12,000 | **UNKNOWN** |
| C5 | cap 250, **brake 1.0**, `PlantSweep`, herbivores unpinned | B | 12,000 | **UNKNOWN** |
| C6 | cap 250, **brake 3.0**, `PlantSweep` | B | 12,000 | UNKNOWN — already recorded as collapsing *within* 12,000 (21/60 extinct) |
| C7 | **shipped `Y`: cap 500, brake 0.75**, four-way split layout | B | 12,000 | **UNKNOWN, and the brake is weaker than C1's** |
| C8 | old `Y` / P4–P6 defaults: cap 48 / 96 / 100, no brake | A | 60,000 (`p5-one-species`) | cap-held; not at risk from this |

C7 is the one to look at first. It is the world the user watches, it is the P4a acceptance surface,
and its brake strength — chosen on 2026-08-30 by measurement at 12,000 ticks — is **0.75, below the
1.0 that has now been shown to collapse**. That is not proof it collapses: it is a different layout
(plant-backed four-way split, not the consumer-defense calibration), it settles at 154 under a cap of
500, and brake strength has already been recorded as **not transferring between scenarios**
(`p6-graded-fertility-is-scenario-specific-2026-08-24.md`). It is proof that nobody knows.

---

## 2. Inventory — recorded results, and whether the conclusion needs a steady state

The test applied to each: **does the conclusion require the population to be at equilibrium, or does
it hold on a trajectory?** A selection statistic, a hash divergence, a mechanism refutation and a
determinism fact all hold on a trajectory. A *level*, a *survival count*, a *self-limiting* claim and
a *carrying capacity* claim do not.

### 2.1 Measured inside C1 — the cell that is known to collapse

| document | conclusion | needs steady state? |
|---|---|---|
| `p3-digestion-strategies-2026-08-30.md` | `DietSpecialization` does not reach reproductive fitness; lifetime intake → offspring `r +0.88`; the capacity clamp is not the explanation | **NO.** Already re-run at 36,000 in the same cell and reproduced (`+0.872 / +0.876`, 24/24 worlds positive). The banner is current and correct. |
| `p3-digestion-re-adjudicated-2026-09-03.md` | the same negative, at the allocation site, 24 seeds × 36,000 | **NO** — and it is the positive control for the whole class: an instrument returning `r +0.88` on one relationship in the same collapsing worlds is not an instrument that has been blinded by the collapse. |
| the intake table, the diet histogram, the per-run hunter share, `defense` at t +12.71, drift-to-fixation of `NeutralMarker` | as recorded | **NO**, but every **absolute level** in them is a level *during a decline*. Already labelled by the transient doc. |
| the intake-valley thread (three corrections in `5-lessons-log.md`, 2026-09-03) | the valley is phase-dependent, then withdrawn as a pooled-mean feature failing the 0.75 direction-consistency criterion | **NO.** The withdrawal is the stronger claim and it is unaffected. Note the finding *already* turned on run length and population phase — this audit is the same variable, found again from outside. |

**Nothing in C1 is retracted by this plan.**

> **Still true after the triage, 2026-09-05.** Nothing anywhere was retracted. Ten recorded documents
> gained run-length banners and every one of them keeps its comparison result; what the banners label
> is levels, survival counts and "this is a place a population can live" claims. Two headline sentences
> are withdrawn inside their documents - "there is a setting where starvation and survival coexist"
> and "a carrying-capacity-limited habitat, at every cap tried" - and both are level claims of exactly
> the class section 2.3 predicted would be the affected one.

### 2.2 Measured in Regime B cells never run past 12,000 — conclusions that HOLD

These are selection, liveness, mechanism and instrument claims. They are statements about *what
differs between arms*, measured on paired seeds, and a shared downward trajectory is common to both
arms.

- `p6-predation-selects-on-defense-2026-08-26.md` — predation selects `defense`. **Holds.**
- `p6-defense-selection-is-robust-and-my-mechanism-was-wrong-2026-08-26.md` — `defense` drift survives
  every neighbouring cell (+4.97 to +10.97); the proposed sterilisation mechanism is refuted.
  **Holds** — and it is already a robustness sweep, which is the right shape.
- `p6-death-is-concentrated-on-the-low-defense-tail-2026-08-26.md` — where the mortality falls.
  **Holds as a distributional claim.**
- `p6-the-combat-forces-are-too-small-2026-08-26.md` — predation is 0.53 kills per run against ~65
  deaths; combat damage 96.6 health per run. **Holds** — it is a ratio within a run, and a smaller
  population would shrink numerator and denominator together, so the direction of the conclusion is
  safe.
- `p6-two-flags-adjudicated-at-last-2026-08-26.md` — `kinRecognitionEnabled` and
  `multiThreatPerceptionEnabled` are LIVE when exercised (56/60 and 60/60 hash divergences).
  **Holds absolutely.** A hash divergence is not a population statistic. The §4 ledger entry in
  `AGENT_FIELD_NOTES.md` needs no change. (The *29% population cost* quoted from it, and carried into
  the frozen spec's section 2 as the kin-recognition load-bearing-approximation finding, is a
  population level and belongs in 2.3's class — the LIVE verdict does not.)
- `p6-a-survivable-predator-prey-scenario-exists-2026-08-26.md`, *founder half* — the founder profile
  zeroed eighteen of twenty-four traits and that, not predation, was the blocker. **Holds.**
- `p6-predation-never-failed-its-founders-cannot-breed-2026-08-26.md` — same. **Holds.**
- `p6-the-founder-fix-was-necessary-and-not-sufficient-2026-08-26.md` — **Holds.**
- `p6-patch-quality-is-not-a-free-parameter-2026-08-26.md` (C5) — turning the quality channel off
  moves everything; the "nothing to be smarter about" constraint is refuted. **Holds** (paired,
  240 runs, flag on/off).
- `p6-plant-corpus-revalidated-unpinned-2026-08-26.md` (C5) — the contest and join **nulls** survive
  an unpinned population. **Holds as a null**, with one caveat worth a line: a null measured in a cell
  whose persistence is unknown is a weaker null than one measured in a persistent cell, because both
  arms may be dominated by the same decline. The positive-control discipline from 2026-09-03 applies
  — this document carries no known-live relationship through it.
- `p6-the-controller-comparison-2026-08-26.md` / `p6-the-clean-controller-comparison-2026-08-26.md`
  (C4) — intent beats Legacy on matched machinery; the brake is a precondition, not the payoff.
  **Holds on the comparison.** See 2.3 for the survival numbers inside them.
- `p6-urgency-exponent-is-monotone-2026-08-24.md`, `p6-the-mating-gate-is-the-selection-2026-08-24.md`,
  `p6-gate-dose-response-2026-08-24.md` (the selection curve) — cap-100, Regime A. **Unaffected.**

### 2.3 Conclusions that DO depend on a steady state — the actual affected set

Five claims, and they are all the same claim wearing different clothes: *this configuration is a
place a population can live.*

1. **`p6-the-pressured-cell-is-a-plateau-2026-08-26.md`** — "nine of nine cells survive 29 or 30 of
   30", "the collapse cliff is not nearby". **Affected.** Survival at 12,000 is now known not to
   imply persistence, and C1 sits one brake step below this grid. The *shape* of the plateau
   (smooth, monotone in brake and regen) probably survives; the *verdict* "no cell collapses" does
   not, because the run was too short to see the collapse a neighbouring cell demonstrably has.
2. **`p6-the-predation-cell-is-robust-2026-08-26.md`** — "robust off its one cell" on gate, cap,
   regeneration and brake, extinct 10/60 at baseline. **Affected the same way, and it contains C1's
   brake as one of its axis points.** The selection results inside it hold (2.2); the survivability
   table is a 12,000-tick reading.
3. **`p6-starvation-is-a-dial-2026-08-26.md`** — "there is a setting where starvation and survival
   coexist"; "cap 500 and cap 1000 are the same ecology, above 500 the cap stops mattering".
   **Affected.** The dial itself (starvation share monotone in brake) is a within-run composition and
   holds; "survival coexists" and "the same ecology" are equilibrium statements.
4. **`p6-graded-fertility-closes-the-cap-debt-2026-08-24.md`** — "a graded fertility brake produces a
   **carrying-capacity-limited habitat**, at every cap tried". **Most affected of all.** This is
   literally the claim that a steady state exists, measured only inside 12,000 ticks, and C1 is the
   counterexample. It already carries a scenario-specificity banner; it needs a run-length one.
5. **`p6-y-is-food-limited-2026-08-30.md`** — "`Y` is now limited by its food instead of by a
   number"; "the population **self-limits** at 154 under a cap of 500 that never binds"; brake 0.75
   chosen because survival is non-monotone (16 / 17 / 20 / 20 / 16 of 24). **Affected, and this is
   the shipped configuration.** Every number is a 12,000-tick reading, the survival column is a
   survival *count at a horizon*, and the chosen strength is below C1's.

Also affected, lower stakes: **`p6-the-gate-is-a-survival-mechanism-2026-08-26.md`** — "the gate is
the model's density brake", and the survival column 4 / 11 / 24 / 38 of 40 across gate values. The
*selection* half reproduces and holds; the survival half is the same 12,000-tick reading. Its
brake-sweep row at 1.0 (33.6% starvation, `fertility_investment` t 4.69) is C1's brake, measured
before anyone knew what that cell does.

**Bystanders, listed so nobody re-checks them:** `p5-one-species-2026-08-30.md` (old `Y`, cap 96,
Regime A — and the project's only 60,000-tick run, so its plateau-in-genetic-distance conclusion is
*strengthened* by long-run evidence, not weakened); `p6-generated-plant-placement-2026-08-30.md`
(control reproduces cap-96 `Y` at population 95.833, Regime A); every P4 plant and P4a document
(Regime A, pinned); every liveness, determinism and instrument document.

---

## 3. A milestone or a single experiment?

**Recommendation: neither a milestone nor one experiment — one short session of the lead agent's own
work, structured as the five tasks below. If forced to choose between the two named options, the
single experiment, and the inventory is what says so.**

The argument for the smaller option:

- **Nothing is retracted.** Section 2.2 is the large majority and holds. Section 2.3 is six documents
  and one repeated claim. A milestone exists to sequence work that would otherwise conflict; there is
  no conflicting work here, and no code whose correctness is in question.
- **The measurement-validity milestone is the comparison.** It had twelve tasks, an Execution
  Contract, a Progress Ledger, one production edit and a hash-inertness test per instrument, because
  it was building instruments that could silently be wrong. This builds none. Two runs, one console
  print, one predeclaration and a table. Milestone machinery on that is overhead that produces no
  additional confidence.
- **The one decision-relevant unknown is a single measurement**: does shipped `Y` persist. Everything
  else in this document is bookkeeping around it.

Where the inventory would have said otherwise, and does not: if section 2.3 contained a *selection*
result rather than only survival and level claims, or if a recorded conclusion had to be **withdrawn**
rather than labelled, the re-adjudication would be milestone-shaped. It does not, and none does.

---

## 4. Tasks

**Ruled by the user, 2026-09-03.** Five tasks, in this order, then stop.

### Progress ledger

*(Updated in the same commit as the work. A commit cannot contain its own hash, so a Commit cell is
filled in by the next task's commit; `pending` means the work landed and only the hash is
outstanding.)*

| Task | Status | Commit | Notes a later session needs |
|---|---|---|---|
| 1 | done | `364d176` | `Trajectory` is shared by `CreatureSweep` and `SitePilot` through a linked compile item, so its namespace is the parent `LifeSimulation.Tools`. Nine samples, not three: the persistence criterion needs three points inside the last third. The trend verdict reads the **all-world** mean because the alive-conditioned mean rises as a cell collapses. Wired into `--deaths`, the main sweep, and SitePilot; **not** into `--intake`, `--diet`, `--life-history` or `--thermal`, which report per-creature cohorts rather than populations and are used by no task here. `SitePilot --ticks=` landed in the same commit as a dependency of Task 4, per the user's ruling; `PlantSweep`'s stays deferred. |
| 2 | done | `8c77f86` | Criterion and both expected signs predeclared above, committed **before** either run started. Both predictions about *shape* and *outcome* failed; both failures are recorded rather than dropped. |
| 3 | done | `0ccc915` | Both arms reproduce the recorded endpoint exactly (3 of 24 at 173/202/410; 1 of 24 at 202), which is what licenses the curve. **Overshoot, not a fade**: peak ~250 at tick 8,000, crash 12,000-20,000. Predeclared "smooth decline" **failed**. Outcome is **bimodal** — survivors end larger than the recorded 12,000-tick population. The criterion fired on its **survival** clause; its trend clause read `no`, because the all-world mean rises 105% across the last third of a cell that lost 21 of 24 worlds. Record: `p6-the-c1-collapse-curve-2026-09-03.md`. |
| 4 | done | `bdb7004` | **Shipped `Y` is 0 of 24 alive at 36,000** — worse than C1, which keeps 3. Fails both clauses. Predeclared **PERSISTENT** and that **failed**. The 12,000-tick row reproduces the recorded 20/24 and population 154.1 to the digit, so the curve after it is the same measurement continued. **Attribution**: the same split layout at cap 96 is level 12,000→36,000 in two arms (21/24 and 22/24, 99.9% age deaths), so the collapse belongs to the cap-and-brake change, not the layout or the model. The five survival counts the 0.75 brake was chosen from are all counts at the boom peak. Record: `p6-the-shipped-world-does-not-persist-2026-09-03.md`. **No brake or cap value proposed; shipped scenario untouched.** |
| 5 | done | `ad33419` | `docs/experiments/cell-family-persistence-ledger.md`. Rows C9 and C10 are the cap-96 attribution controls and are labelled in the file as controls, **not** recommendations. Two lessons appended to `5-lessons-log.md`: the trend-clause escape, and final population versus bimodality. Derived triage length recorded in section 5 of this plan; **triage not run**. |

| 6 (deferred triage) | done 2026-09-05 | `0c3f424`, `c82fb15`, `0f9a61b`, `a19df08` | `PlantSweep --ticks=` landed first, verified bit-identical at the default. Eleven rows at 24,000 ticks, **all COLLAPSING at six seeds and again at 24**, plus a 12,000-tick control per cell that reproduces the recorded reading. The 24-seed run is the first in the triage that could have returned PERSISTENT and returned it for nothing. **Two of four predeclared verdicts failed**, both by predicting survival. Every Regime B row in the ledger now carries a verdict and every one is COLLAPSING. The p4a mechanism claim was checked on the same data and **holds at all eight brake strengths**. Banners added to ten recorded documents; **nothing retracted**. Record: `p6-regime-b-triage-2026-09-05.md`. **No configuration value moved; no brake value proposed.** |

**Non-goal, stated first because it is the tempting mistake:** *do not tune brake, cap or
regeneration to make anything here persist.* That is a biological change; it invalidates every
baseline measured before it, and the transient document, the measurement-validity milestone and
`AGENTS.md` rule 2 all forbid it inside a measurement task. If a persistent regime is wanted, that is
separate, explicitly biological work with its own spec.

### Task 1 — trajectory reporting in the sweeps

**This is the root cause, and it comes first.** C1 was invisible for a week not because nobody ran it
long enough but because **the sweeps print only the final population**. A cell that is 22 of 24
surviving at 12,000 and falling steeply looks identical, in the recorded artefact, to one that is
stable — the two differ only in a number the tool never printed.

Every sweep prints, per cell: its **run length**, and the **population trajectory at thirds** (and the
same for mean energy where the sweep already computes it). Nothing else changes; no existing column
moves; the tools are `tools/`-only, touch no simulation code and no hash.

Doing this first makes Tasks 3 and 4 read out a curve instead of a survival count, which is the whole
difference between the two readings this audit exists to separate.

### Task 2 — predeclare the persistence criterion

Needs Task 1 to exist, because the criterion is stated over a trajectory the tool must first report.
Proposal, to be ruled on:

> A cell is **persistent** if the population trajectory over the last third of the run has no monotone
> downward trend, **and** the surviving-world fraction at the end is within replication noise of its
> fraction at one third.

Predeclared in writing before the runs, per the frozen spec's section 5 rule. A survival count at a
horizon is explicitly **not** the criterion: that is the statistic that produced the wrong reading in
the first place.

**PREDECLARED 2026-09-03, before either run, in commit order.** Stated against the instrument built in
Task 1, which samples nine evenly spaced points and reports the all-world mean population (extinct
worlds counted as zero), the alive-conditioned mean, and the death mix within each interval.

Let `alive(k)` be worlds with a living population at sample `k` of 9, and `mean(k)` the all-world mean
population there. The last third is samples 7, 8, 9.

- **COLLAPSING** if `mean(7) > mean(8) > mean(9)` — a monotone decline over the last third — **or**
  `alive(9) < 0.85 x alive(3)`.
- **PERSISTENT** if neither of those holds **and** `alive(9) >= 0.85 x alive(3)` **and** the run
  carried at least 20 seeds.
- **INDETERMINATE** otherwise, which includes every verdict at fewer than 20 seeds. A low-seed run may
  return COLLAPSING; it may never return PERSISTENT. This is the same rule the triage carries in
  section 5 and it is stated here so the two cannot drift apart.

The 0.85 is a declared threshold, not a derived one: at 24 seeds it makes a fall of four or more
worlds a real fall. It is written down before the numbers exist so that it cannot be chosen after
them.

**The expected signs, declared before the runs, because a criterion with no prediction attached
cannot fail.**

- **C1 (Task 3): COLLAPSING, monotone.** It is already known extinct in 21 of 24 worlds at 36,000, so
  this is a reproduction rather than a prediction; what is genuinely open is the *shape*, and the
  declared expectation there is a **smooth decline rather than a late crash** — a population that
  never had a regulator should lose ground from the beginning, not fall off a step.
- **`Y` (Task 4): PERSISTENT.** Reasoning, so the prediction is falsifiable rather than a hedge: the
  brake is weaker than C1's, but the layout is different (plant-backed four-way split, not the
  consumer-defense calibration), the recorded starvation share is only 5.4% against C1's 33.6%, the
  population settles at 154 under a cap of 500 that never binds, and brake strength is already
  recorded as **not transferring between scenarios**. If `Y` returns COLLAPSING instead, that is a
  finding about the shipped world and it is reported first, before anything else in this plan is
  written up.

### Task 3 — the C1 collapse curve

`tools/CreatureSweep --deaths 24 500 --regen=2.0 --brake=1.0 --predation --gate=0.45 --ticks=36000`,
both health arms. With Task 1 in place this returns the death-cause mix **over time** alongside the
trajectory, which is what separates a **smooth decline** from a **late crash** — the distinction the
transient document records that it cannot make. `--ticks=` is already global on `CreatureSweep`, so no
code change beyond Task 1.

**This task also sets the run length for any later triage.** 36,000 was chosen for a fitness-cohort
censoring window (`MaximumLifespanTicks = 5,400`, births admitted in `[0, 30,600]`); it says nothing
about how long a collapse takes. The curve does. Until it exists, any triage length is a guess.

### Task 4 — does shipped `Y` persist

**The highest-value single measurement in this audit.** `Y` is cap 500 / brake **0.75** / four-way
split layout: the population the user watches, the P4a acceptance surface, a cap that never binds, and
a brake *weaker* than the 1.0 that collapses. Its persistence is unknown and everything visible about
the project's current world rests on it.

Run `Y`'s exact configuration — `tools/SitePilot`, arm 1, the layout-fingerprint-identical control —
past 12,000 ticks, at the length Task 3 justifies, and report the trajectory against Task 2's
criterion.

**One dependency, named rather than assumed:** `Y`'s layout exists only in `SitePilot`
(`SplitSites`, `tools/SitePilot/Program.cs:325`). `CreatureSweep` cannot produce it — it runs
`ConsumerDefenseCalibrationModerate` and its transforms only. So this task requires `--ticks=` on
`SitePilot`, whose `Ticks` is a `const` at `tools/SitePilot/Program.cs:30`.

That is a **subset** of the deferred tools change and is justified by *this* task rather than by the
triage: without it Task 4 cannot run at all. **`PlantSweep`'s `--ticks=` stays deferred** with the
triage that would need it. Default stays 12,000 in both, so every recorded output is unchanged — the
rule the measurement-validity milestone's Task 8 followed.

### Task 5 — the standing cell-family persistence ledger

Section 1's table promoted to `docs/experiments/cell-family-persistence-ledger.md`, maintained, with
one rule:

> A cell may not be cited as a **baseline** — a place other results are measured — until its longest
> verified run is recorded here and exceeds the run length of the results resting on it.

That converts a lesson into a check. Without it, the next brake value gets chosen at 12,000 ticks
again, which is exactly how 0.75 was chosen.

---

## 5. Deferred, and on what condition

**Triage of the remaining Regime B cells (C2, C3, C4, C5).** Deferred until Task 3 reports, because
the triage length must be **chosen from the collapse curve**. 36,000 is a fitness-cohort number and
carries no information about collapse timing; fixing the triage at it would repeat this audit's own
error one horizon further out.

> **DERIVED LENGTH, recorded 2026-09-03 from Tasks 3 and 4. Not run.**
>
> Both measured collapses have the same clock. The population peaks at **tick 8,000**, the crash runs
> from **12,000 to 20,000**, and the outcome is settled by **24,000** — C1 is at 5 of 24 worlds by
> then and `Y` at 3 of 24, against 22 and 20 at one third. Nothing after 24,000 changed either
> verdict; the last 12,000 ticks of both runs cost a third of the compute and moved C1 by two worlds
> and `Y` by three.
>
> **Recommended screen length: 24,000 ticks** — twice the recorded horizon, and it contains the whole
> of the crash window in the only two cells where that window has been measured. It is a third cheaper
> than 36,000 per cell, which is what makes screening four cells affordable.
>
> **Two conditions on that number, both of which the deferred triage must carry:**
>
> 1. **It is derived from two cells at brake 0.75 and 1.0, both cap 500.** A stronger brake may simply
>    postpone the overshoot rather than prevent it, and C4 sits at brake 3.0-5.0. A screen length
>    calibrated on the weakest brakes is a floor, not a schedule.
> 2. **24,000 can condemn a cell; it cannot clear one.** A cell still level at 24,000 has only shown
>    that it does not crash on C1's clock. PERSISTENT still requires the full criterion — 20+ seeds,
>    and a run long enough that the last third is genuinely past the risk window, which for a cell that
>    survives the screen means longer than 24,000, not equal to it.
>
> This is a recommendation for whoever runs the triage.
>
> **RUN 2026-09-05 at exactly this length, twice.** `docs/experiments/p6-regime-b-triage-2026-09-05.md`.
> Eleven rows across C2, C3, C4 and C5 at 24,000 ticks - first at six seeds, with a 12,000-tick control
> on the same seeds for every creature cell, then **re-run at 24 seeds**, the count the criterion needs
> for a PERSISTENT verdict. **All eleven are COLLAPSING at both seed counts, and no row read
> differently at 24 seeds.** The full-power run could have cleared a cell; the closest, C4 at brake
> 5.0, keeps 11 of 24 against a threshold of 20.4 and fails the trend clause too. Condition 1 was the right
> warning and it turned out not to matter: a stronger brake does not postpone the overshoot either -
> brake 4.0, the strongest value any recorded document endorses, loses every world by 24,000. The
> screen length held: nothing needed longer than 24,000 to be condemned. **Cost was not the binding
> constraint** - the seven creature cells ran concurrently in under a minute at six seeds and in a few
> minutes at 24 - so the six-seed limit was the rule's and not the budget's, and the 24-seed run that
> settles it was done the same day.

**`--ticks=` for `PlantSweep`.** **Landed 2026-09-05** with the triage that justified it, on the same
terms `SitePilot`'s did: default 12,000, a non-default length encoded into the CSV filename so no
recorded corpus can be overwritten, and a bit-identical before/after check on hashes, populations and
occupancies at the default.

**When the triage does happen, one rule governs how its results may be read:**

> **A 3-6 seed screen may flag a cell for follow-up. It may never clear one.** At that seed count the
> screen cannot distinguish 22 of 24 from 18 of 24, so "not obviously dying" is an absence of evidence
> and must be recorded as exactly that. Only a full-seed run against the Task 2 criterion can call a
> cell persistent.

**Gate-table verdicts.** Not in this plan. Section 4 of the frozen spec is the user's to update once
evidence exists.

---

## 6. What this plan explicitly does not claim

- **Not** that any recorded conclusion is wrong. Section 2.2 is the majority and it holds.
- **Not** that C2-C7 collapse. They are unmeasured; that is the whole point, and guessing the sign
  would be the error the field notes name repeatedly.
- **Not** that 36,000 ticks is the right horizon for anything but the cohort window it was chosen for.
  A cell persistent at 36,000 and gone at 100,000 is the same finding one level up, which is why
  Task 2's criterion is a trend rather than a survival count.
- **Not** a proposal to change any biological value.
