# Regulation, first arms: read the plant side, then remove the ratchet

**Date:** 2026-09-05. **Status:** PLAN. Task 1 is done and its result is recorded below; Tasks 2-4 are
not started and nothing has been implemented.

**Derived from:** `docs/superpowers/specs/2026-09-05-population-regulation-design.md`, which is the
design document this executes. It in turn sits under the frozen
`2026-08-30-what-finished-means-design.md`.

**Non-goal, stated first because it is the tempting mistake:** *do not tune brake, cap, regeneration,
growth rate or any threshold to make a cell persist.* **No parameter value is proposed in this plan.**
Where an arm needs a value, the arm picks it, states it in its own predeclaration, and says why it
picked it. `AGENTS.md` rule 2 and the run-length audit's non-goal both apply unchanged.

---

## 0. The ruling this executes

Ruled by the user, 2026-09-05, against the design document's §4 recommendation:

1. **The instrument blocker is the first build task.** Plant columns in `Trajectory` plus the
   seed-eligible-fraction accumulator, then re-report the eleven triage cells and settle
   production-versus-access as a measurement.
2. **The free check runs before anything else.** Does `fertility_investment` rise across the recorded
   runs? Confirmable from committed columns, no new runs. Report either way.
3. **A2 before E**, and not on ease. Two reasons, both accepted:
   - **Testing E with the ratchet live is a confounded test of the best candidate.** Any regulator
     that lets the population reach the forage holds patches below `0.75 * Capacity` some of the time,
     which under the current seed gate is sterilisation. E would be measured against a resource base
     that fails under exactly the conditions E exists to handle.
   - **A2 costs less to establish.** E re-baselines every scenario with resource contention; A2 touches
     a resource system whose recorded results are mostly about creatures.

   E stays the next candidate after A2. B stays a control, not a candidate.

**Consequence of the reorder that the design document did not state, recorded here:** once A2 lands,
**the E arm must be measured against the post-A2 baseline, not the current one.** A2 changes the
resource base, so an E arm run against today's numbers would be measuring A2 and E together. This is
the price of the correct ordering and it is worth paying; it is written down so a later session does
not compare across it by accident.

**Every arm is predeclared before it runs, and arms run singly, never together.**

---

## 1. Progress ledger

*(Updated in the same commit as the work. A commit cannot contain its own hash, so a Commit cell is
filled in by the next task's commit; `pending` means the work landed and only the hash is
outstanding.)*

| Task | Status | Commit | Notes a later session needs |
|---|---|---|---|
| 1 — free check | **done 2026-09-05** | pending | **The prediction failed in its causal half and held in its consequence.** `fertility_investment` rises hard in the mate-selection-off family (+0.17 to +0.22, direction consistency 0.84-0.92 against the repository's 0.75) and **not at all** in the predation family (drift ~0.00, consistency 0.48-0.59, indistinguishable from `neutral_marker`). There is **no dose-response in brake strength** across a 4x range, and it rises without a brake at all. So the brake is not what drives it, and §1.2's "the brake's authority is selected away" is **withdrawn**. What survives, and is now measured rather than bounded: at the evolved mean the brake's `headroom` is zero for effectively every birth. Full table in §2. |
| 2 — plant columns | **done 2026-09-06** | pending | `SeedEligiblePlantPatchCount` appended to `SimulationStatistics` at the END with a default, per the constructor's own recorded convention. `PlantReproductionSystem.MaturityFraction` promoted `private`->`public const`, value unchanged, so the statistic reads the same constant the gate does. `Trajectory` gained a **second table** rather than columns on the first, so every artefact recorded before today still diffs against the population table unchanged; the block is skipped entirely when no patch was ever seen, so non-plant sweeps are byte-identical in their output too. Growth and offtake are reported as **rates per biomass-second**, differenced per interval, plus their ratio - that ratio is what Task 3 adjudicates on. `PlantSweep`'s `frozen` renamed to `no_living_bred_lineage` (**renamed, not re-computed**: the values were always correct, the name was not, and every recorded figure was measured where no community had died). **Full suite 747 passed / 0 failed - no hash test moved, so no baseline moves.**
| 3 — re-report and adjudicate | **done 2026-09-06** | pending | **Production-limited in all eleven; the gate does not fire.** Instrument check passes - every population trajectory reproduces the recorded 24-seed triage cell for cell. **The predeclared criterion was ill-posed and is recorded as such:** `offtake/growth >= 1` is unreachable for any system with a second loss term, and this one loses ~1% of standing biomass per second to patch age mortality at every sample of every cell, so Q1-as-written is falsified everywhere. On total drain against gross production - the same statement as the criterion's own falling-biomass clause - drain exceeds growth at the peak in all eleven, grazing is 44-79% of the loss, and C3/C4 biomass ends 94-99.8% down. **The unanticipated finding is that age mortality is 78% of gross plant growth in the ungrazed phase**, so net production available to consumers is about a fifth of gross; no candidate in the design document addresses that. **Q2 falsified as written and its weakened branch fires** - the ratchet runs after the peak, so it is a consequence of the crash, not its cause; A2's rationale narrows to the recovery phase, which is what its predeclared signature already describes. Record: `docs/experiments/p6-what-limits-the-peak-2026-09-06.md`. |
| 4 — the A2 arm, 24k | **done 2026-09-09** | pending | **The prediction failed on the clause that mattered, and it failed upward.** `alive(9)` moves 0 of 24 -> **20 of 24** against a threshold of 20.4, so the arm lands **0.4 of a world** from PERSISTENT. **The triage predeclaration contradicts itself at exactly that point** - the formal rule appears twice (`alive(9) < 0.85 x alive(3)` condemns, so 20 COLLAPSES) and a 24-seed gloss says "20 of 24 passes"; this arm's own falsifier repeated the gloss as `>= 20`. **No threshold was changed.** Reported on the formal rule as COLLAPSING and recorded as unresolved by 24 seeds; the resolution is more seeds. Three of four specifics held: plant patch count flat at ~23 against the control's 1.0, final biomass 14x, peak 277.4 vs 261.6. The trend clause does not fire and **not** for the survivorship reason - the sample barely shrinks while the mean rises 67%. **Anomaly recorded, not explained:** starvation stays 33-49% in every interval of the last third instead of falling back, which is the design document's D2/E signature rather than A2's, and one world of twenty sits at the cap of 500. Control reproduces the recorded cell to the digit; full suite 752 passed / 0 failed. Record: `docs/experiments/p6-graded-seeding-arm-2026-09-06.md`. |
| 4b — the A2 arm at 72,000 ticks | **done 2026-09-10** | pending | **COLLAPSE, and the stabilisation is the cap.** `alive` 20 at tick 24,000 -> **16 of 24** at 72,000 against a threshold of `ceil(0.85*20)=17`; the alternative denominator of 24 gives 21 and also fails, so both readings agree. Trend clause does not fire. Validity: the tick-24,000 sample reproduces the recorded 24k arm exactly and **24 of 24 behaviour hashes match in both arms**, so run length changes nothing about the first 24,000 ticks at the bit level. **Section 3.4 CONFOUNDED:** 12 of 16 survivors touch the cap, 13 of 16 end at 475+ of 500, and excluding cap-contacting worlds leaves 4 survivors. Six cycles; amplitude collapses 156 -> 6 while both peaks and troughs rise, which section 3.3 calls SUSTAINED because it defined DAMPED on peak decline rather than on amplitude - **the third mis-specified criterion in this branch, reported not reinterpreted**. Casualties 8: one establishment failure, seven later collapses, four of them after tick 24,000. No world harmed. Record: `docs/experiments/p6-graded-seeding-extension-72000-2026-09-10.md`. |

---

## 2. Task 1 — the free check, done, and what it returned

**Question, as ruled:** does `fertility_investment` rise across the recorded runs? If it does, the
brake's headroom erodes under its own selection and the mechanism deletes itself.

**Method.** Committed `CreatureSweep` drift CSVs in `docs/experiments/`, read directly. No simulation
was run and no code was touched. Founders are centred at 0.5 (`PhysiologyFounderFactory`), so drift is
the surviving-population mean minus 0.5. **`neutral_marker` is the null** — read by zero behaviour code
and pinned dead by `LivenessTests` — and is reported beside every figure, because the fourth-pass
lesson of 2026-09-03 is that a result reported without its inert control is uninterpretable here.
`up-frac` is the fraction of surviving worlds above 0.5, read against
`PairedEvolutionCriterion.MinimumDirectionConsistency` = **0.75**.

### The brake axis: cap 500, regen 2.00, mate-selection off, 30 seeds x 2 arms, 12,000 ticks

| cell | n | FI mean | FI drift | FI up-frac | NM mean | NM drift | NM up-frac |
|---|---:|---:|---:|---:|---:|---:|---:|
| brake 1.5 | 10 | 0.6302 | +0.1302 | 0.60 | 0.5113 | +0.0113 | 0.70 |
| brake 3.0 | 57 | 0.6807 | +0.1807 | **0.84** | 0.5065 | +0.0065 | 0.63 |
| brake 4.0 | 60 | 0.7233 | +0.2233 | **0.92** | 0.5121 | +0.0121 | 0.63 |
| brake 4.5 | 59 | 0.6954 | +0.1954 | **0.86** | 0.5121 | +0.0121 | 0.69 |
| brake 5.0 | 58 | 0.6734 | +0.1734 | **0.84** | 0.5098 | +0.0098 | 0.62 |
| brake 6.0 | 54 | 0.6977 | +0.1977 | **0.89** | 0.5128 | +0.0128 | 0.65 |
| brake 12.0 | 33 | 0.6980 | +0.1980 | **0.85** | 0.5000 | +0.0000 | 0.45 |

### Comparison cells, including unbraked caps as the no-brake control

| cell | n | FI mean | FI drift | FI up-frac | NM drift | NM up-frac |
|---|---:|---:|---:|---:|---:|---:|
| brake 1.0, predation | 51 | 0.5055 | +0.0055 | 0.59 | +0.0045 | 0.57 |
| brake 1.5, predation | 50 | 0.4978 | **-0.0022** | 0.48 | +0.0019 | 0.60 |
| brake 3.0, predation | 43 | 0.5057 | +0.0057 | 0.56 | +0.0082 | 0.53 |
| brake 1.5, cap 250 | 50 | 0.4977 | **-0.0023** | 0.48 | +0.0023 | 0.64 |
| brake 1.5, cap 1000 | 50 | 0.4978 | **-0.0022** | 0.48 | +0.0019 | 0.60 |
| **cap 100, no brake** | 78 | 0.6003 | **+0.1003** | 0.76 | +0.0051 | 0.62 |
| **cap 200, no brake** | 36 | 0.5715 | +0.0715 | 0.58 | +0.0079 | 0.56 |

### What this says, in four statements

1. **Yes, it rises - in one family, and it is a real signal there.** +0.17 to +0.22 at 0.84-0.92
   direction consistency, against a neutral control at +0.01 and 0.62-0.69. That clears the
   repository's own 0.75 threshold with its own null beside it.
2. **No dose-response in brake strength.** From 3.0 to 12.0 - a **4x** change - the drift is flat and
   non-monotone: +0.181, +0.223, +0.195, +0.173, +0.198, +0.198. A mechanism driven by the brake would
   scale with the brake. This one does not.
3. **It does not rise where the brake is strongest-tested.** Every predation cell, at brakes 1.0, 1.5
   and 3.0 and at caps 250, 500 and 1000, returns drift within +/-0.006 at 0.48-0.59 consistency -
   the `neutral_marker` reading. The rise is scenario-specific, not brake-specific.
4. **It rises without a brake.** Cap 100 unbraked: +0.100 at 0.76 consistency. There is an obvious
   brake-independent reason - `ReproductionCooldownSeconds = 16 - 8 * FertilityInvestment`, so the gene
   directly shortens the base birth interval and pays wherever reproductive output is what is selected.

**Verdict: the causal claim is withdrawn.** The design document's §1.2 second bullet - "the brake's own
authority is under selection, in the direction that removes it" - is **not supported**. It is corrected
in the spec rather than left standing.

### What survives, and is stronger than what was claimed

The *state* the prediction described is reached; the brake is simply not what drives it there. Run the
headroom arithmetic at the measured means rather than at the bound:

`headroom > 0` requires post-charge condition above 0.70, so pre-charge energy above
`0.70 + ReproductionEnergyCostFraction`, where the cost is `0.15 + 0.20 * FertilityInvestment`.

| population | FI | cost | pre-charge energy needed for any headroom at all |
|---|---:|---:|---|
| founders | 0.50 | 0.250 | **> 0.950 of capacity** |
| braked cells, evolved | 0.68-0.72 | 0.286-0.294 | **> 0.986-0.994 of capacity** |

Recorded breeder energy is nowhere near either. `p6-nothing-starves-2026-08-24` records the population
homeostatting at **0.806 mean energy against the 0.80 mate-seeking gate**, and the 24-seed triage
artefact reads 0.6295 mean energy fraction in C2.

**So the brake is not a 8-16% controller that erodes to 0%. It is a constant multiplier of exactly
`1 + strength` for effectively every birth in every recorded cell, from the founders onward.** The
8-16% figure in the spec was an upper bound requiring a parent at full energy capacity, and it is
corrected there. The design document's conclusion is unchanged and its central argument is stronger:
the brake was never a feedback at all, in any cell, at any strength.

**Two limitations, stated rather than left implicit.** Every figure is **survivorship-conditioned** -
extinct worlds are all-zero rows and are excluded - which the 2026-09-03 fifth-pass lesson warns about
directly; the brake-1.5 mate-selection-off row at n=10 of 60 is the worst case and no weight is put on
it. And these are **12,000-tick** runs, so whether the drift continues to 24,000 is not measured. The
question asked was about the recorded runs and is answered on them.

**No experiment document is filed for this.** No run was made, the analysis is a read of committed
CSVs, and this section is the record. The throwaway script is not committed, per field notes §3.

---

## 3. Task 2 — plant columns in the trajectory

**This is the blocker, and it is the only task here that changes code before an arm is designed.**

Every signature in the design document's §3.2 table is read off the plant community. `Trajectory`
(`tools/CreatureSweep/Trajectory.cs`, shared with `SitePilot` by linked compile item) samples nine
points and prints population, alive count, mean energy and the five death shares. It prints **nothing
about the plants**, and the design document's §1.4 argues that the plant community is where the
collapse clock lives. Adding a mechanism before the instrument that would adjudicate it is the failure
this project has recorded twice.

### What lands

**One accumulator in the simulation, hash-inert.** `SimulationWorld.Statistics.cs` already loops
`Plants`; add a count of patches at or above `PlantReproductionSystem.MaturityFraction * Capacity` -
**the seed-eligible count, which is the ratchet's state variable** - exposed on `SimulationStatistics`
as a new constructor parameter with a default, so every existing caller still compiles. Everything else
needed already exists: `ActivePlantPatchCount`, `TotalPlantBiomass`, `CumulativePlantGrowth`,
`CumulativePlantBiomassConsumed`, `CumulativePlantBiomassLostToMortality`, `PlantBiomassSeconds`,
`PlantPatchSeconds`, `PlantBirthCount`.

**Four columns in `Trajectory`**, sampled at the same nine points: active patch count, seed-eligible
count, standing biomass, and biomass consumed within the interval. Plus a per-interval realised growth
rate, which is what settles Task 3.

**`MaturityFraction` must become readable** rather than being duplicated as a literal in the tools. It
is `private const` today. Promoting it to `public const` is the whole change; no value moves.

### Why this cannot invalidate anything

`SimulationStatistics` is not hashed - `ComputeStateHash` covers creatures and resources, and the
existing biomass accumulators are already documented as deliberately absent from it - and `Trajectory`
is read-only by construction, calling `CaptureStatistics` between steps. **Task 2 changes no simulation
behaviour and no hash, so no recorded baseline moves.** The full test suite must still pass unchanged,
and that is the check that this is true.

### Also in scope, because it stops being harmless here

`PlantSweep`'s `frozen` column is `HighestPlantGeneration == 0` computed by scanning the **living**
patch store, so it reads identically for "never bred" and "everything that bred has died" - recorded
2026-09-05 and deliberately not fixed because nothing depended on it. Plant columns are about to become
load-bearing. Either make it a watermark or rename it to what it computes; the choice is the
implementer's, and whichever is chosen must be stated, because every recorded `frozen` figure was
measured at 12,000 ticks where no plant community had died and so none of them changes.

---

## 4. Task 3 — re-report the eleven triage cells, and settle production versus access

**Needs Task 2. Runs the same eleven commands at the same seeds and the same length; changes nothing
except what is printed.** The triage's own instrument check applies in reverse: the population and
death-mix columns must reproduce the recorded artefact exactly, and if they do not, Task 2 perturbed
something and must be fixed before anything is read.

### The question it answers

The design document §1.4 leaves one quantity deliberately unasserted: is the peak population limited by
plant **production** or by **access** to six point sites of interaction radius 1.5? The arithmetic
depends on the realised growth `limit` - `min(moisture, fertility, temperature)` - which
`p4-fertility-binds-the-growth-limit-2026-08-19` records as fertility-bound for 82-90% of
plant-reachable positions but never quotes as a number.

It is not an estimate. `CumulativePlantGrowth` over `PlantBiomassSeconds` gives the realised
per-unit-biomass growth rate directly, and `CumulativePlantBiomassConsumed` over the same denominator
gives realised grazing pressure. **Production-limited** looks like offtake meeting realised growth at
the peak with biomass falling. **Access-limited** looks like offtake well below realised growth while
creatures starve - forage present and unreachable.

**This changes which candidate is even relevant.** If the peak is access-limited, A1 and A2 are aimed
at a constraint that is not binding, and the ordering above would need revisiting before Task 4 runs.
Predeclare the reading before running it.

### The predeclaration Task 3 owes

Written and committed before the runs, in this file or beside it:

- Which of production-limited and access-limited is expected, **and why**, so a failure is informative.
- What the seed-eligible fraction is expected to do across the nine samples in a collapsing cell. The
  design document predicts it falls to zero at or before the population peak; that is a falsifiable
  claim about the ratchet and it should be stated as one.
- What would **refute** the ratchet: seed-eligible fraction staying materially above zero through the
  collapse, which would mean recruitment was available and the community died for another reason.

---

## 5. Task 4 — the A2 arm

**Needs Task 3, including its adjudication.** Do not start it if Task 3 returns access-limited.

### What A2 is

`PlantReproductionSystem.Step:36` - `if (parent.Biomass < parent.Capacity * MaturityFraction) continue;`
with `MaturityFraction = .75f` - is an all-or-nothing seed gate. Below three quarters of capacity a
patch produces **no seeds**, not fewer. Combined with age-only patch mortality at 34-135 seconds, it
converts grazing pressure into a monotone loss of the plant community that grazing then prevents from
being rebuilt.

A2 replaces the step with seed output graded on biomass. **This plan proposes no threshold, no curve
and no value.** The arm chooses them, states them in its predeclaration, and says why - including why
the flag-off path is byte-identical, which is the condition for the arm to be a comparison rather than
a new world.

### Conditions on the arm

- **Behind a config flag whose off state is bit-identical**, following
  `plantFertilityAdaptationEnabled` and `establishmentContestEnabled`, which exist for this reason.
  `LivenessTests` and `FlagLivenessAnalysis` will see the new flag; a flag that is live is expected and
  must not be added to `KnownInertFlags` to quiet a failure.
- **`ComputeStateHash` tests fail by design** with the flag on. `AGENTS.md` §7 permits that only for a
  task that says it changes behaviour; this one does, in writing, here.
- **Baseline invalidation is scoped and must be listed.** A2 moves plant demography **and plant
  genetics**, because `seedBiomass` feeds `ConsumeAt` and site competition. The establishment-contest
  and seed-production-rate calibrations are downstream. Enumerate the affected recorded documents
  before the arm runs, and banner rather than retract, per the triage's precedent.
- **Reference cell: C3 at brake 1.5, herbivore, 24 seeds, 24,000 ticks** - currently 0 of 24, the only
  genuine zero in the triage, and the one clean single-variable axis in the corpus. One variable moves.
- **Verdict by the unchanged persistence criterion.** COLLAPSING if `mean(7) > mean(8) > mean(9)` or
  `alive(9) < 0.85 * alive(3)`; PERSISTENT needs neither and at least 20 seeds.

### The predeclaration the arm owes, from the design document's §3.2 row

Committed before the run, unedited afterwards:

> **A2 signature.** Population peak unchanged; the crash **arrests partway** rather than completing;
> a V-shaped recovery follows. Starvation spikes at the peak, falls back towards zero, and **recurs**.
> Seed-eligible patch fraction stops being monotone - it recovers. Verdict PERSISTENT, oscillating.

And the falsifiers, named in advance:

- **Peak moves.** A2 is producer-side and must not change the boom. A lower peak means it is acting
  through something other than recruitment and the attribution is lost.
- **Crash completes anyway.** Removes the ratchet as the sufficient explanation for the collapse, and
  promotes E and F ahead of any further producer-side work.
- **Population plateaus flat with starvation under 5% throughout.** That is B's signature, not A2's,
  and would mean A2 is regulating rather than de-ratcheting - which needs the §3.3 food-supply control
  before it can be believed.

### The control that must accompany any surviving arm

From the design document §3.3, and it is not optional: **re-run the surviving arm with the plant
capacity budget or the active site count raised.** A real ecological regulator moves its plateau with
the food supply; an imposed one does not. Without it, "the population is stable" is not evidence the
ecology is regulating it - the error `p6-the-cap-is-the-stabiliser-2026-08-24` already recorded once.

---

## 6. After this plan

**Not in scope here, listed so the sequence is on record.**

- **E - contest allocation - is the next candidate**, and it is measured against the **post-A2**
  baseline (§0). Its own predeclaration, its own arm, singly.
- **B - density-dependent mortality - stays a control, not a candidate.** It regulates by construction;
  its value is showing what an imposed regulator's trajectory looks like beside an ecological one.
- **The brake's status is untouched.** Frozen spec §6 rests that decision on reproductive-fitness
  instrumentation that does not exist. Nothing here proposes reverting it, keeping it, or changing its
  value. Task 1 removes one argument for it and creates no argument against it beyond what the triage
  already recorded.

## 7. What this plan does not claim

- That A2 will work. Its falsifiers are written above precisely because it may not.
- That the candidate list in the design document is complete. It is the list the source suggested.
- That one mechanism suffices. Arms run singly because that question cannot be answered from a
  factorial run, not because the answer is known.
- Any parameter value, anywhere, for anything.
