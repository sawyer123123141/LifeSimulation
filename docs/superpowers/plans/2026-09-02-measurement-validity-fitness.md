# Measurement Validity and Fitness Re-Adjudication — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended)
> or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`)
> syntax for tracking. **Read the Execution Contract below before starting: this plan runs to
> completion without checking in after every task.**

**Date:** 2026-09-02
**Status:** PLANNED — not implemented.
**Spec:** `docs/superpowers/specs/2026-08-30-what-finished-means-design.md` (**FROZEN**; this plan
does not modify it and adds no goals to it).
**Serves:** section 8 ("Fitness measurement — PARTIAL, and this is the important gap") and section 6
(the `trait → ecological performance → lifetime reproductive success → allele-frequency change`
chain). It is instrumentation for the section 5 causal loop, not a step in the loop itself.

**Goal:** repair the measurement layer enough that the next biological decision can be trusted.
Nothing in this milestone changes biology. Every recorded result must still reproduce bit-identically
with the new instrumentation switched off, and — where the instrument is a pure external observer —
with it switched on.

**Explicitly NOT in this milestone:** the full Ne system, any change to reproduction ordering,
digestion coefficients, population size, mutation, brake strength, resource layout, learning, or kin
recognition. Health recovery appears **only** as a paired sensitivity arm in Task 9.

---

## What the source actually says (read before planning further work)

Established by reading the repository on 2026-09-02. These are the facts the plan is built on; if one
of them turns out to be wrong during implementation, that is a stop condition.

| Fact | Source |
|---|---|
| Base tick rate 20 Hz, `FixedDeltaTime` 0.05 s | `SimulationConfig` schedule `(20, 20, 4, 2, 2, 1, 1, 1)` |
| Needs (and therefore all drains, ageing, health damage, healing) tick at **2 Hz** — once per 10 base ticks, with `deltaTime = 0.5` | `SimulationWorld.TickNeeds` |
| Reproduction ticks at **1 Hz** — once per 20 base ticks | `Step`, `Config.Schedule.ReproductionHz` |
| `ResolveResourceInteractions` runs **every base tick**, ungated | `Step` |
| Ingestion fires on `SeekFood`/`SeekCarcass` as well as `Eat`/`FeedCarcass`, whenever the creature is inside the interaction radius | `SimulationWorld.ResolveResourceInteractions` |
| Decisions are staggered: each creature re-decides once per 10 ticks (`(tick + index) % interval`) | `TickDecisions` |
| The `Seek* → Eat/FeedCarcass` conversion happens **only** on a creature's decision tick | `TickDecisions`, arrival block |
| `NeedsSystem.ConsumeFood` clamps to `EnergyCapacity`; surplus is discarded silently | `NeedsSystem.ConsumeFood` |
| Gross energy from one bite is `amount * 20 * phenotype.FoodYield`, where `amount` is already yield- and defence-scaled | `ResolveResourceInteractions` + `ConsumeFood` |
| `AdultAgeSeconds = 20` s = **400 ticks**, and is a constant, not genetic | `ReproductionSystem.AdultAgeSeconds` |
| `MaximumAgeSeconds = 90 + 180 * LifespanTendency`, and `LifespanTendency` is `Clamp01` | `GenomePhenotype` |
| Therefore **no creature can live past 5,400 ticks**, for any genotype reachable by mutation | derived from the two rows above |
| Reproduction candidates are sorted by `CreatureId` and the loop `break`s when `Count >= maximumPopulation` | `ReproductionSystem.Step` |
| `CanReproduce` / `CanSeekMate` are `public static` and can be evaluated by an outside observer with the exact production logic | `ReproductionSystem` |
| Gate is `needFraction` on **all three** of energy, hydration, health; mate-seeking is `needFraction + 0.1` | `ReproductionSystem`, `SimulationConfig.MateSeekingNeedMargin` |
| `Events` is a 1,024-entry buffer that `SimulationWorld` **never clears** — the host drains it | `SimulationWorld` ctor, `SimulationEventBuffer` |
| Birth events carry tick, child, both parents. Death events carry tick, subject, cause | `SimulationEvent` |
| Inheritance is per-trait random-parent choice plus `N(0, 0.03)` mutation, then `Clamp01` — not blending | `GenomeInheritance.InheritTrait` |
| `NeutralMarker` defaults to **0.5** and neither founder factory varies it: founders are monomorphic | `Genome` ctor, `PhysiologyFounderFactory`, `PredationFounderFactory` |
| `tools/HeadlessTests` compiles `Assets/Scripts/Simulation/**` and `Assets/Tests/EditMode/**`. It does **not** compile `tools/CreatureSweep` | `tools/HeadlessTests/*.csproj` |

### The ledger already exists and must not be rebuilt

`Assets/Scripts/Simulation/Analysis/AncestryHistory.cs` already records, per creature: birth tick,
both parent IDs, death tick, death cause, and a children-by-parent index — with a completeness
watermark and permanent overflow semantics. That is six of the seven things section A of the task
asks for. **The only missing field is the genome.**

So section A is *"add a genome companion and a cohort layer on top of `AncestryHistory`"*, not *"design
a life-history ledger"*. Building a second ledger would duplicate the watermark and overflow
semantics that were already argued through once, and would be the third instrument in this repository
measuring the same events.

Two contracts of the existing type that the cohort layer must respect:

- `deathTick == 0 && deathCause == None` is the sentinel for **still alive**. It is not a death at
  tick 0.
- `Record` silently ignores a death for an unknown creature, so `RecordFounders` must be called before
  the first drain or founders are absent from the pedigree.

### Four defects in the current intake instrument, not three

The adversarial review named two of these. The source shows four, and the two it did not name may be
the larger ones.

1. **Right-censoring.** `Intake.Report` filters on `AliveTicks > 200` and includes creatures still
   alive at run end and creatures born near it. Lifetime totals are partly a measure of observation
   time. *(Named in the review.)*
2. **Capacity clamping.** Energy is clamped at `EnergyCapacity`, so a bite taken while nearly full is
   invisible. The loss is largest exactly for well-fed creatures — a bias correlated with the quantity
   being measured. *(Named in the review.)*
3. **Drain-tick erasure.** Drains land in a 0.5-second lump once per 10 ticks while ingestion happens
   every tick. On a needs tick the drain usually exceeds the bite, the delta goes negative, and the
   whole tick's ingestion is discarded rather than blurred. *(Not named in the review.)*
4. **Stale-action erasure.** Ingestion fires under `SeekFood`/`SeekCarcass`, but `Intake.cs` credits
   gains only under `Eat`/`FeedCarcass`. Every tick between physically entering the interaction radius
   and the creature's next decision tick — up to 9 ticks per foraging trip — is dropped from both the
   numerator and the `PlantTicks` denominator. This also means the recorded *"12.4% of ticks eating,
   flat across the gene"* is an undercount of feeding time by an unmeasured amount, and the
   *"they just eat more"* hypothesis was refuted using a statistic that cannot see part of the eating.
   *(Not named in the review, and it is the one that most threatens a recorded conclusion.)*

Defect 4 is the reason section C must instrument the allocation path itself rather than improve the
delta proxy.

---

## Architecture

Three layers, and the split is load-bearing:

- **Analysis types live in `Assets/Scripts/Simulation/Analysis/`.** That directory is compiled by
  `tools/HeadlessTests`, so every cohort rule, every statistic and every diagnostic is covered by
  EditMode tests. `tools/CreatureSweep` is **not** compiled by the test project, so nothing with a
  contract may live there.
- **`tools/CreatureSweep` stays a thin driver**: argument parsing, run loop, table printing.
- **One production edit only** (Task 5): an optional, nullable ingestion sink on `SimulationWorld`,
  following the `Liveness` / `LivenessRecorder` precedent exactly — null by default, never read by
  simulation logic, never in a hash.

Everything else in this milestone is an outside observer that reads the world between `Step` calls and
writes nothing.

## Global constraints

- Work only in `Assets/Scripts/Simulation/Analysis/`, `Assets/Scripts/Simulation/Diagnostics/`, the
  two files named in Task 5, `Assets/Tests/EditMode/`, `tools/CreatureSweep/`, and the documents named
  in Tasks 10–12.
- **Determinism:** no `System.Random`, no clock, no LINQ or dictionary iteration order driving
  anything ordered, no float re-association. Analysis code may use LINQ **only** outside
  `Assets/Scripts/Simulation/` — i.e. in `tools/`, which is where the existing sweeps use it.
  Inside `Simulation/`, index loops.
- **No allocation in per-tick code.** The ingestion recorder accumulates into amortised-growth arrays
  keyed by `CreatureId.Value`; creature indices are not stable across death (swap-remove), so indices
  must never be used as accumulator keys.
- **Hash independence is a test, not a claim.** Every instrument added here must have a test asserting
  identical `ComputeStateHash` with the instrument attached and detached, across at least 2,000 ticks.
- Never change a test to pass. Never add `[Ignore]`. Delete every `ZZZ*.cs` probe before committing;
  never `git add -A`; never stage generated `.meta` files.
- Do not touch `DeterministicRandom.cs`, `TemperatureField.cs`, `RandomDomain` numbering, founder
  genetics, or any biological constant.

---

## Execution Contract

**This section is the workflow authority for this milestone. It overrides any habit of stopping after
each task.**

1. Once this plan is approved, **execute tasks sequentially without waiting for the user between
   tasks or between commits.** Finishing Task N is not a reason to stop; it is a reason to start
   Task N+1.
2. After each task's tests pass, commit, tick the task's checkboxes in this file, update the
   **Progress Ledger** below in the same commit, and continue immediately.
3. **Routine implementation decisions are yours** — naming, file splits, test fixtures, table layout,
   argument spelling, how to structure a loop. Do not ask.
4. **Stop and report only for:**
   - a genuine blocker (an API that does not exist, a build that cannot be made to work);
   - a conflict with the frozen governing spec;
   - an unexpected change in biological behaviour — any state-hash test failing, or a run's population
     or death mix moving when nothing biological was supposed to change;
   - a test failure you cannot explain (an explained failure you caused is not a stop condition — fix
     it and continue);
   - a design choice materially different from what this plan describes, including any case where you
     would need to edit a file outside the allowed list.
5. **A stop is a report, not a silence.** Say what you were doing, what happened, and what you
   recommend.
6. **Progress lives in this file**, in the Progress Ledger, so a compacted or fresh session can resume
   without re-deriving anything. Record the task number, the commit, and any decision a later session
   would otherwise have to re-make.
7. **Milestone completion means the Milestone Verification section passes in full** — not that the
   last task's tests passed.
8. Experiment output (Task 9) is written to `docs/experiments/` by the lead agent. `AGENTS.md` rule 2
   forbids implementation subagents from writing under `docs/`; if you are a subagent, produce the
   numbers and hand them up rather than writing the record yourself.

### Progress Ledger

*(Update in the same commit as the work. One line per task.)*

*A commit cannot contain its own hash, so each task's Commit cell is filled in by the **next** task's
commit. A row reading `pending` means the work landed and only the hash is outstanding.*

| Task | Status | Commit | Notes / decisions a later session needs |
|---|---|---|---|
| 1 | done | `d58dd9b` | `LifeHistoryLedger` wraps `AncestryHistory`; genomes captured by `Observe`, so a creature never observed alive reports `HasGenome` false rather than a fabricated genome. Enumeration is first-observation order, not dictionary order. |
| 2 | done | `0b9c941` | Horizons derived by evaluating `Phenotype.FromGenome` at the top of the `LifespanTendency` clamp, never as literals; a test pins 400 / 5,400 so a change to `AdultAgeSeconds`, the lifespan expression or `BaseFrequencyHz` fails here. `LifeHistoryLedger` gained `PedigreeCount` / `GetPedigreeIdAt` so the cohort can count pedigreed creatures whose genome was never observed. |
| 3 | done | `434c76f` | Calls `CanReproduce` / `CanSeekMate` rather than copying them. Juveniles are counted as skipped, never as blocked, so age can never appear as a need. Several needs may block one sample and each is counted; `BlockedMinimumNeedCount` is the conditioning that answers whether an energy-side trait can reach fitness. |
| 4 | done | `7f8d1d0` | Cap-blocked requires two or more ready creatures: one ready creature at the cap has nobody to breed with, so the cap explains nothing about it. Threshold is a required constructor argument, pinned by a reflection test that no constructor parameter has a default. |
| 5 | done | `12e192a` | `NeedsSystem.GrossEnergyFrom` is the extracted expression, order unchanged. `SimulationWorld.Recorder` is null by default, follows `Liveness`, and the recorder also flags bites taken under a stale `Seek*` action so Task 6 can measure defect 4. **The hash-inertness test needs a world with food in it**: the bare constructor creates no resources, so it applies `Prototype4Scenarios.ConsumerDefenseCalibrationModerate` and asserts non-zero gross ingestion so the hash comparison cannot pass vacuously. Full suite after the edit: 734 passed, 0 failed. |
| 6 | done | `cbe070d` | **Step 4 measured** (8 seeds x 12,000 ticks, `CreateFullEcosystemDefaults` + `ConsumerDefenseCalibrationModerate`, plant only - no carcass ingestion occurred): recorder-measured gross plant energy **886,559** against the retired delta proxy's **642,506**, a ratio of **1.380**. **19.55%** of measured gross plant energy is taken under a stale `Seek*` action (defect 4). Feeding ticks 1,126,276 against 1,070,364 proxy `Eat` ticks, ratio **1.052** - so most of the 38% gap is **drain-tick erasure**, not stale actions. **Surplus lost to the capacity clamp is 0.0000** in this cell: a bite is worth about 1 energy against roughly 24 of headroom, so defect 2 is real in principle and negligible here. Task 9 re-measures in its own cell. `EnergyDeltaProxy` lives beside the ledger so the retired instrument stays reproducible under test. |
| 7 | done | `7faad56` | Returns the existing `PairedBootstrapInterval` type, and the bootstrap mirrors `PairedBootstrapAnalysis.EstimateMeanDifferenceInterval` exactly - same resampling rule, same `RandomDomain.ExperimentSampling` draws, same percentile indices. It is written in `PerWorldRelationship` rather than called because the existing method takes `ExperimentResult` lists and a per-world correlation is not one; `Experiments/` is outside this milestone's allowed files, so it was not refactored. No new statistical method. An empty diet bin reports NaN, not zero. |
| 8 | done | `eea0f3f` | `--life-history <seeds> <cap>`; `--ticks=` is global and defaults to 12,000 so every other mode's recorded output is unchanged. The mode ends by running one seed 2,000 ticks with the recorder attached and detached and printing whether the hashes match, so a perturbing instrument announces itself in the output rather than only in the test suite. `LifeHistoryLedger` gained `OffspringAt` so offspring-surviving-to-adulthood can be counted from the pedigree. Smoke run at 3 seeds / 8,000 ticks: hashes identical, proxy ratio 1.231, stale share 10.2%, surplus 0.01% of gross. |
| 9 | not started | — | — |
| 10 | not started | — | — |
| 11 | done | `90e5682` | Appended as an appendix at the end of this file. The correction implementation revealed: option 4's stated main cost - silent failure on an incomplete pedigree - is closed, because `FitnessCohort.Select` and `IngestionLedger.Join` both throw on an incomplete ledger and a replay inherits that. Still a note; nothing was built. |
| 12 | done | pending | Content moved unaltered under a one-line provenance header; the root file is now a three-step pointer at `AGENTS.md`, the frozen spec, then the current plan. Nothing deleted. |

---

## File structure

| File | Responsibility |
|---|---|
| `Analysis/LifeHistoryLedger.cs` | Genome-at-birth companion to `AncestryHistory`; the joined per-creature life record. |
| `Analysis/FitnessCohort.cs` | Genotype-independent cohort selection and the censoring horizons. |
| `Analysis/IngestionLedger.cs` | Per-creature gross/stored/surplus ingestion totals, drained from the recorder. |
| `Diagnostics/IngestionRecorder.cs` | The optional sink. Passive, nullable, hash-inert. |
| `Analysis/ReproductionBottleneck.cs` | Which normalised need is binding, at gate-relevant moments. |
| `Analysis/PopulationCapDiagnostic.cs` | Cap saturation and ready-but-unbred accounting; the run safety flag. |
| `Analysis/PerWorldRelationship.cs` | Per-world statistic plus cross-seed summary; no pooling as a headline. |
| `tools/CreatureSweep/LifeHistory.cs` | The `--life-history` driver: run loop, drain, tables. |
| `Assets/Tests/EditMode/*` | One focused test file per task, named for the type under test. |

---

## Task 1: Genome companion to the existing pedigree

**Files:** create `Assets/Scripts/Simulation/Analysis/LifeHistoryLedger.cs`; create
`Assets/Tests/EditMode/LifeHistoryLedgerTests.cs`.

**Produces:** a joined per-creature record — id, birth tick, death tick, death cause, both parents,
genome, and a births-credited count — assembled from `AncestryHistory` plus a genome captured at the
first observation of each creature.

- [x] **Step 1: Write failing tests.** Founders recorded at tick 0 have their genome captured and no
      parents. A creature born at tick `t` has its genome captured on the first drain after `t`. A
      creature that dies has `DeathTick`/`DeathCause` set and its genome retained. `IsAlive` is true
      only for `deathTick == 0 && deathCause == None`, and a creature that died at tick 0 is
      impossible by construction — assert the sentinel is documented, not that it is safe.
- [x] **Step 2: Run `cd tools/HeadlessTests && dotnet test --filter "FullyQualifiedName~LifeHistoryLedgerTests"`.** Expected: compilation failure.
- [x] **Step 3: Implement.** Wrap, do not replace, `AncestryHistory`. `Observe(world)` captures
      genomes for ids not yet seen; `RecordCompleteBatch(events, throughTick)` forwards to the
      underlying ancestry and mirrors its `IsComplete` / `CompleteThroughTick`. Expose
      `OffspringCredited(id)` from `AncestryHistory.GetChildCount` — do **not** add a counter.
- [x] **Step 4: Test the incompleteness path.** An overflowed batch must make the ledger permanently
      incomplete, and every downstream analysis must refuse to report on an incomplete ledger rather
      than reporting a smaller number.
- [x] **Step 5: Commit.** `analysis: join genomes to the recorded pedigree`

**Note for the implementer:** both parents are credited for the same birth, so population-mean
offspring is ~2 at replacement. That is correct for this monoecious model and matches the recorded
mean of 1.99. Do not "fix" it to 1.

## Task 2: The censoring horizons, derived not guessed

**Files:** create `Assets/Scripts/Simulation/Analysis/FitnessCohort.cs`; create
`Assets/Tests/EditMode/FitnessCohortTests.cs`.

**Produces:** `MaximumLifespanTicks`, `AdultAgeTicks`, and cohort predicates.

The horizons, and why they are safe:

```
AdultAgeTicks          = 20 s  * 20 Hz = 400 ticks          (a constant, identical for every genotype)
MaximumLifespanTicks   = 270 s * 20 Hz = 5,400 ticks        (90 + 180*1, and LifespanTendency is Clamp01)

complete-life cohort            : birthTick <= endTick - 5,400
offspring-to-adulthood cohort   : birthTick <= endTick - 5,800
```

**The horizon is a property of the phenotype map, not of the observed population.** It is computed
from `GenomePhenotype`'s coefficients and the clamp, so it does not depend on which genotypes happened
to be present — which is exactly what makes it a non-biasing filter. A creature is admitted or
excluded on its **birth tick alone**. Its own `LifespanTendency` is never consulted. That is the
property that makes this defensible and it must be asserted by a test.

- [x] **Step 1: Write failing tests.** Two creatures with `LifespanTendency` 0.0 and 1.0 and the same
      birth tick are both admitted or both excluded — never one of each. A creature born at exactly
      `endTick - 5,400` is admitted to the complete-life cohort; one tick later is not. The
      offspring-to-adulthood cohort is a strict subset. A cohort built from an incomplete ledger
      throws rather than returning a partial set.
- [x] **Step 2: Run the filter.** Expected: compilation failure.
- [x] **Step 3: Implement,** deriving the constants from `ReproductionSystem.AdultAgeSeconds`,
      `GenomePhenotype`'s lifespan expression and `SimulationSchedule.BaseFrequencyHz` rather than
      writing 400 and 5,400 as literals. A test must fail if any of those three change.
- [x] **Step 4: Add founder and expansion-phase handling.** Founders are parentless, start
      simultaneously with full needs, and carry a founder-distribution genome rather than a
      mutation-derived one. Provide `ExcludeFounders` (parentlessness is genotype-independent, so this
      is safe) and a `birthWindow` split so the analysis can report whether an effect is confined to
      the population's expansion phase. Do not filter on the expansion phase by default; report it.
- [x] **Step 5: Commit.** `analysis: genotype-independent fitness cohort horizons`

**Run-length finding to carry into Task 9.** At 12,000 ticks the clean window is births in
`[0, 6,200]` — 52% of the run, and disproportionately the expansion phase, when density, forage and
competition are all unlike the rest of the run. That is not a censoring bias but it is an ecological
one. At 36,000 ticks the window is 84% of the run; at 60,000 it is 90%, and 60,000 is the length at
which the frozen spec's 29–37 generation figure was measured. **Recommend 36,000 ticks as the
scientific run length for Task 9, with 60,000 if the time budget allows.** No survival-analysis
machinery is needed: Kaplan-Meier and Cox exist to extract information from censored observations, and
a longer run plus a genotype-independent horizon removes the censoring instead of modelling it. Do not
add them.

## Task 3: Which need is binding

**Files:** create `Assets/Scripts/Simulation/Analysis/ReproductionBottleneck.cs`; create
`Assets/Tests/EditMode/ReproductionBottleneckTests.cs`.

**Produces:** per-world counts of which normalised need is the minimum, and which need fails the gate,
at reproduction-relevant moments.

- [x] **Step 1: Write failing tests.** A creature at energy 0.9, hydration 0.9, health 0.4 of capacity
      reports health as both minimum and blocking at `needFraction` 0.7. A creature above the gate on
      all three reports a minimum but no blocker. Age and cooldown are reported as separate
      non-need blockers and never miscounted as a need.
- [x] **Step 2: Run the filter.** Expected: compilation failure.
- [x] **Step 3: Implement as an external observer.** Call `ReproductionSystem.CanReproduce` and
      `CanSeekMate` — the production predicates, not a copy — and compute the three ratios directly
      from `GetNeedsAt` / `GetPhenotypeAt`. Sample only on reproduction ticks
      (`tick % (BaseFrequencyHz / ReproductionHz) == 0`) and only for creatures at or past adult age;
      sampling every tick would weight the answer by lifespan and by juvenile time.
- [x] **Step 4: Report both conditionings,** because they answer different questions: the distribution
      over all adult creature-samples, and the distribution restricted to creatures blocked from
      reproducing. The second is the one that says whether an energy-side trait can reach fitness.
- [x] **Step 5: Commit.** `analysis: reproduction bottleneck diagnostic`

## Task 4: Is the cap binding

**Files:** create `Assets/Scripts/Simulation/Analysis/PopulationCapDiagnostic.cs`; create
`Assets/Tests/EditMode/PopulationCapDiagnosticTests.cs`.

**Produces:** cap-saturation fraction, ready-but-unbred accounting, and a single
`ReproductiveSkewInterpretationIsUnsafe` flag per run.

The decomposition, all of it observable after `Step` on a reproduction tick:

- population `>= cap` **and** two or more creatures still pass `CanReproduce` → the cap is the
  sufficient explanation for their not breeding. Count these as cap-blocked.
- population `< cap` and creatures still pass `CanReproduce` → they failed to find a mate in range.
  That is ecology, not the cap.

- [x] **Step 1: Write failing tests.** A world held at its cap with ready creatures reports
      cap-blocked samples and sets the unsafe flag. A world well below its cap with ready creatures
      reports zero cap-blocked samples and leaves the flag clear. The flag's threshold is an explicit
      constructor argument with no default — a silent default here would become a fact nobody chose.
- [x] **Step 2: Run the filter.** Expected: compilation failure.
- [x] **Step 3: Implement.**
- [x] **Step 4: Commit.** `analysis: population cap binding diagnostic`

**Do not change `ReproductionSystem`.** The `CreatureId`-ordered scheduler is a real source of
artificial reproductive skew when the cap binds, and it stays exactly as it is in this milestone. The
diagnostic exists so that a future skew result can be labelled interpretable or not, and so that the
decision to change the scheduler is made on evidence.

## Task 5: Real ingestion instrumentation

**Files:** create `Assets/Scripts/Simulation/Diagnostics/IngestionRecorder.cs`; modify
`Assets/Scripts/Simulation/Core/SimulationWorld.cs` (the `Liveness` property pattern and one call site
inside `ResolveResourceInteractions`); modify `Assets/Scripts/Simulation/Biology/NeedsSystem.cs`
(extract the gross-energy expression, no value change); create
`Assets/Tests/EditMode/IngestionRecorderTests.cs`.

**This is the only task that edits production code.** It exists because the quantity is only knowable
at the allocation site: defects 3 and 4 above make any outside-the-world energy-delta proxy
unrepairable in principle.

Three quantities per bite, all of which the call site already has or can compute without new state:

| quantity | definition |
|---|---|
| resource amount consumed | `allocatedAmount` after deterrence, in resource units |
| gross energy obtained | `nutrition * 20 * phenotype.FoodYield` — what the bite was worth |
| energy actually stored | `needs.Energy` after `ConsumeFood` minus before |
| surplus lost to the cap | gross minus stored, by definition non-negative |

- [x] **Step 1: Write failing tests.** (a) A creature at full energy that eats records positive gross,
      zero stored, and gross-equals-surplus. (b) A hungry creature records stored equal to gross and
      zero surplus. (c) Plant and carcass are recorded separately. (d) **A 2,000-tick run produces an
      identical `ComputeStateHash` with the recorder attached and with it null** — this is the test
      that makes the instrument trustworthy, and it must exist before the recorder does.
- [x] **Step 2: Run the filter.** Expected: compilation failure.
- [x] **Step 3: Extract the gross-energy expression** into a `public static float` helper on
      `NeedsSystem`, and make `ConsumeFood` call it. **Preserve the operation order exactly** —
      `amount * FoodEnergyPerUnit * phenotype.FoodYield`, in that order, with no re-association. The
      existing state-hash tests are the check that this refactor was value-preserving; if any of them
      moves, stop.
- [x] **Step 4: Implement the recorder.** Nullable `IngestionRecorder Recorder { get; set; }` on
      `SimulationWorld`, null by default, following `Liveness` exactly. Accumulate per
      `CreatureId.Value` into amortised-growth arrays — **not** per creature index, which the
      swap-remove in `CreatureStore.Remove` invalidates. No allocation in the steady state, no
      dictionary, no string work, no logging on the tick path.
- [x] **Step 5: Add the one call site** in `ResolveResourceInteractions`, immediately around the
      existing `NeedsSystem.ConsumeFood` call, guarded by `Recorder?.`. Record for both `Food` and
      `Carcass`. **Do not change feeding behaviour, do not add RNG, do not reorder any existing
      statement.**
- [x] **Step 6: Re-run the whole EditMode suite,** not just the filter. Any `ComputeStateHash`
      assertion failing anywhere is a stop condition under the Execution Contract.
- [x] **Step 7: Commit.** `diagnostics: observational ingestion recorder`

## Task 6: Ingestion ledger and the retirement of the delta proxy

**Files:** create `Assets/Scripts/Simulation/Analysis/IngestionLedger.cs`; create
`Assets/Tests/EditMode/IngestionLedgerTests.cs`.

- [x] **Step 1: Write failing tests** for the join: per-creature lifetime gross ingestion, stored
      energy, surplus lost, and feeding-tick counts, keyed to the life-history ledger and restricted
      to a cohort.
- [x] **Step 2: Run the filter.** Expected: compilation failure.
- [x] **Step 3: Implement.**
- [x] **Step 4: Quantify what the old proxy was missing.** Compute, over one run, the ratio of
      recorder-measured gross ingestion to the old positive-energy-delta estimate, and the share of
      ingestion occurring under a stale `Seek*` action. **This number is the evidence for or against
      the retraction in Task 10 and must be recorded, not estimated.**
- [x] **Step 5: Commit.** `analysis: gross ingestion ledger`

**Do not delete `tools/CreatureSweep/Intake.cs` in this milestone.** It is the instrument that produced
a recorded result; the correct end state is that the new mode supersedes it and the experiment record
says so. Deleting it would remove the ability to reproduce the number being retracted.

## Task 7: Per-world statistics, and no pooled headline

**Files:** create `Assets/Scripts/Simulation/Analysis/PerWorldRelationship.cs`; create
`Assets/Tests/EditMode/PerWorldRelationshipTests.cs`.

- [x] **Step 1: Write failing tests, including a Simpson's-paradox fixture:** two worlds each with a
      negative within-world slope, arranged so the pooled slope is positive. The type must report the
      per-world slopes as negative, the cross-seed summary as negative, and the pooled figure — if it
      reports one at all — explicitly labelled as pseudo-replicated.
- [x] **Step 2: Run the filter.** Expected: compilation failure.
- [x] **Step 3: Implement:** per-world correlation and per-world diet-bin means, then a cross-seed
      summary of the per-world values — mean, the count of worlds by sign, and a bootstrap interval.
      **Reuse `PairedExperimentAnalysis` / `PairedBootstrapAnalysis`**, which already provide paired
      differences, bootstrap intervals and direction consistency; the world is the replicate and the
      seed is the pairing key. Add no new statistical machinery. No mixed models, no Kaplan-Meier,
      no Cox.
- [x] **Step 4: Require a minimum cohort size per world** and report worlds excluded for being below
      it, rather than silently dropping them.
- [x] **Step 5: Commit.** `analysis: per-world relationships with a cross-seed summary`

## Task 8: The `--life-history` sweep mode

**Files:** create `tools/CreatureSweep/LifeHistory.cs`; modify `tools/CreatureSweep/Program.cs` and
`tools/CreatureSweep/CreatureSweep.csproj` (the csproj lists files explicitly — a new file must be
added or it will not compile).

- [x] **Step 1: Add a `--ticks=` argument** defaulting to the existing 12,000, so other modes'
      recorded outputs stay reproducible while Task 9 can run long.
- [x] **Step 2: Implement the driver.** Per seed: construct the world, `RecordFounders`, attach the
      ingestion recorder, then per tick `Step`, drain `Events` into the ledger with a completeness
      watermark, observe genomes, sample the bottleneck and cap diagnostics on reproduction ticks, and
      `Events.Clear()`. Follow the drain discipline in `Intake.cs`: the world never clears the buffer
      and it holds only 1,024 entries.
- [x] **Step 3: Print, per world and then across worlds:** cohort sizes and how many creatures each
      censoring rule excluded; gross/stored/surplus ingestion by diet bin; offspring and
      offspring-surviving-to-adulthood by diet bin; the binding-need distribution; the cap
      diagnostic and the safety flag; per-world correlations with the cross-seed summary; and the
      pooled correlation labelled as pseudo-replicated.
- [x] **Step 4: Verify against a known quantity.** Run one seed with the recorder detached and with it
      attached and assert the reported final state hash is identical. A sweep that perturbs its own
      subject is worse than no sweep.
- [x] **Step 5: Commit.** `tools: life-history sweep mode`

## Task 9: Re-adjudicate the digestion evidence

**Files:** none in code. Produces `docs/experiments/p3-digestion-re-adjudicated-2026-09-XX.md`
(lead agent writes it; see Execution Contract item 8).

**This is a measurement experiment, not tuning.** Hold the recorded digestion cell fixed: cap 500,
`--regen=2.0`, `--brake=1.0`, `--predation`, `--gate=0.45`, `--mate-selection=off` (proximity
pairing). Change **nothing** else — not digestion coefficients, population size, mutation, brake
strength, resource layout, the reproduction scheduler, learning, or kin recognition.

**Arms:** health recovery OFF and ON, paired on seed. That is the only arm, and it is permitted
because it is specifically testing whether the health ratchet changes the conclusion.

**Scale:** 24–30 seeds per arm at 36,000 ticks (see Task 2). The original intake result rests on 8
seeds; the field notes record that an effect significant at n=5 has vanished at n=30 in this project
before.

**Predeclare the expected signs in the plan file before running** — the frozen spec's section 5 makes
predeclaration a standing requirement, and a prediction written afterwards is not one.

### Predeclared before the runs (2026-09-03)

Committed before a single Task 9 tick was run, because a prediction written afterwards is not one.
The user's decisions, taken at the Task 9 boundary:

- **Run length: 36,000 ticks.** The clean birth window is then 84% of the run.
- **Cap-binding threshold: 0.25.** A quarter of reproduction ticks cap-blocked is enough that the
  `CreatureId`-ordered scheduler is shaping who breeds, so any skew reading above it is labelled
  unsafe. This is `LifeHistory.UnsafeCapBlockedFraction`.

**The one predeclared sign:**

> **Gross ingestion falls with diet and then recovers.** Total intake is highest at the herbivore
> end, lowest in the 0.6-0.8 band, and partially recovers at the carnivore end - the recorded 12%
> valley survives correction by the new instrument.

**Deliberately not predeclared.** The user was offered predictions on offspring staying flat across
diet, on energy being the usually-binding need, and on health recovery not changing answers 1-3, and
**declined all three**. Those questions are therefore open: whatever Task 9 measures for them is a
first observation, not a confirmed or failed prediction, and the record must not describe them as
either.

The six questions, each with the number that answers it:

- [ ] **1. Is there a diet-dependent difference in gross ingestion?** Gross ingestion by diet bin, per
      world, cross-seed summary. Compare against the retired delta-proxy table.
- [ ] **2. Does it reach reproductive fitness?** Offspring and offspring-surviving-to-adulthood by
      diet bin, complete-life cohort only.
- [ ] **3. Which need is usually binding?** The Task 3 distribution, restricted to blocked creatures.
- [ ] **4. Does health recovery change the result?** The paired arm, judged on whether the answers to
      1–3 differ, not on whether the population differs.
- [ ] **5. Is the cap binding?** The Task 4 flag. If it is set, say so before interpreting anything
      about reproductive skew.
- [ ] **6. Do per-world effects agree, or was the pooled correlation misleading?** Per-world
      correlations, sign counts, and the pooled figure alongside them.
- [ ] **7. Record the surplus-lost fraction.** How much ingested energy is discarded by the capacity
      clamp, by diet bin. This is the quantity the old proxy could not see at all.
- [ ] **8. Write the record,** including an explicit statement of what the old `r +0.88` figure is now
      worth.

**A negative result is a result.** If gross ingestion still fails to reach fitness, that is the answer
and it should be recorded as one. Do not tune anything to produce a positive.

## Task 10: Retract or confirm the standing digestion claims

**Files:** modify `docs/experiments/p3-digestion-strategies-2026-08-30.md` (banner and correction
only — do not rewrite the history); append to `docs/field-notes/5-lessons-log.md`.

- [ ] **Step 1: Add a status banner** to the 2026-08-30 record pointing at the new one, naming which
      of its numbers survived and which did not. The repository's convention is that superseding
      records carry banners and superseded conclusions are named rather than quietly deleted.
- [ ] **Step 2: State the four instrument defects explicitly,** including the two the adversarial
      review did not find (drain-tick erasure and stale-action erasure), with the measured magnitude
      from Task 6 Step 4.
- [ ] **Step 3: Append the lesson.** The candidate: *an instrument that reads a state variable to infer
      a flow is measuring the flow minus everything else that touched that variable, and the tick
      schedule decides how much that is.* Date it.
- [ ] **Step 4: Commit.**

## Task 11: The drift / Ne note — a note, not an implementation

**Files:** append a section to this plan file.

**Do not implement the Ne system in this milestone. Do not modify founder genetics.**

What inspection of the current architecture establishes:

- The marker is a **single locus, effectively haploid** — `InheritTrait` picks one parent's value
  outright rather than averaging, so there is no blending collapse of variance. That part of the
  architecture is *good* for drift work and should not be described as broken.
- Founders are **monomorphic at exactly 0.5**. There is therefore **no standing variation at t = 0**,
  and every unit of between-replicate variance early in a run is mutational input rather than drift
  acting on existing variation. This, not the continuous-trait nature, is the reason the proposed
  "between-replicate marker means → Ne" method is too simplistic.
- Mutation adds `N(0, 0.03²)` at **every birth**, so mutational input scales with birth count, which
  is itself a fitness quantity — the confound is not additive noise, it is correlated with the thing
  being measured.
- `Clamp01` makes the process a bounded random walk with reflecting-ish barriers; the recorded U-shaped
  distribution is a signature of the clamp, not only of drift.

Four candidate next steps, judged on merit rather than cost:

1. **Reproductive-skew / opportunity-for-selection summaries.** Variance in lifetime reproductive
   success over mean squared, computed on the Task 2 cohort. Cheapest, needs nothing new after this
   milestone, and is a real demographic quantity — but it is a *demographic proxy* in an
   age-structured, overlapping-generations population, where the several definitions of Ne do not
   coincide. Report it as skew, never as an Ne.
2. **Neutral-marker standing variation.** Track within-world variance of the marker over time rather
   than between-world means. Sees the clamp and the mutational input directly. Weak because there is
   exactly one marker: one locus is one realisation of a stochastic process, and no amount of seeds
   fixes the fact that the marker's own trajectory is a single draw per world.
3. **Within-world variance trajectories** for real traits. Useful as pattern validation alongside
   anything else, useless alone — selection and drift both move variance.
4. **Offline synthetic markers replayed over the recorded pedigree.** Because nothing in the
   simulation reads `NeutralMarker`, the pedigree is *causally independent* of any neutral locus. So
   an arbitrary number of independent synthetic neutral loci — biallelic, unclamped continuous,
   whatever architecture the question needs — can be replayed over the exact recorded pedigree
   offline, with the real mutation model or a cleaner one, and no simulation change, no founder
   change, and no new RNG in the tick path. **This is the strongest option on merit, not merely the
   convenient one:** it removes the single-realisation problem (many loci), removes the clamp
   artifact (choose the architecture), separates mutational input from drift (run a zero-mutation
   arm), and costs nothing in determinism because it happens entirely outside the world.
   Its real cost is that it depends on a complete pedigree — which is exactly what Task 1 delivers,
   and which fails silently if the ledger is ever incomplete. Any implementation must refuse to
   report on an incomplete ledger.

**Recommendation to carry forward, not to act on now:** option 4 as the drift diagnostic, with option
1 reported beside it as an independent demographic check, and both explicitly labelled as diagnostics
of the strength of drift rather than as an authoritative Ne. The frozen spec already says the goal is
"a useful diagnostic of the strength of drift relative to census size, not a definitive Ne"; there is
no authoritative Ne to be had here and the plan should stop pretending otherwise.

- [x] **Step 1: Append the section above to this file, with any corrections implementation revealed.**
- [x] **Step 2: Commit.**

## Task 12: `CODEX_TASK.md` cleanup

**Files:** create `docs/handoff/2026-08-12-codex-bootstrap-task.md`; rewrite `CODEX_TASK.md`.

`CODEX_TASK.md` is the original bootstrap task: "create the actual Unity 6 project", a folder layout
that no longer matches the repository (`Simulation/Systems/`, `Simulation/Genetics/`, a top-level
`Tests/`), and the instruction *"Read `README.md` and everything in `docs/` before implementing"* —
which is now directly contrary to `AGENTS.md` and to the field notes, both of which exist to stop
agents reading the whole tree. It sits at the repository root with a name that reads like a current
assignment. That is the risk: not that it is wrong, but that it is wrong and prominent.

- [x] **Step 1: Move the content, unaltered, to `docs/handoff/2026-08-12-codex-bootstrap-task.md`**
      with a one-line header saying what it was and that it completed. Nothing is deleted.
- [x] **Step 2: Replace `CODEX_TASK.md`** with a short notice: this task completed in August 2026 and
      is preserved at the path above; current agents should read `AGENTS.md`, then the frozen spec
      `docs/superpowers/specs/2026-08-30-what-finished-means-design.md`, then the current approved
      plan under `docs/superpowers/plans/`. No other content.
- [x] **Step 3: Commit.** `docs: retire the bootstrap task to a historical notice`

Deleting the file outright was considered and rejected: a root-level file that other documents or
external notes may reference should become a pointer rather than a 404.

---

## Milestone Verification

The milestone is complete when **all** of the following hold. Passing one task is not completion.

- [ ] `cd tools/HeadlessTests && dotnet test` — the full EditMode suite passes, with no test changed,
      weakened, or ignored.
- [ ] Every `ComputeStateHash` assertion in the suite is unchanged from before this milestone.
- [ ] A 2,000-tick run produces an identical state hash with the ingestion recorder attached and
      detached, asserted by a committed test.
- [ ] `LivenessTests` still reports the known inert flag set and the known-dead mechanism set
      unchanged — nothing in this milestone made a mechanism live.
- [ ] `tools/CreatureSweep --life-history` runs to completion and prints per-world results, a
      cross-seed summary, cohort exclusion counts, the binding-need distribution, and the cap flag.
- [ ] Task 9's experiment record exists in `docs/experiments/` and answers all six questions, with the
      predeclared signs visible in this plan file from before the runs.
- [ ] The 2026-08-30 digestion record carries a banner naming what survived re-measurement.
- [ ] `CODEX_TASK.md` is a pointer and the original is preserved.
- [ ] `git status` is clean: no `ZZZ*.cs` probes, no stray `.meta` files.

## What this milestone deliberately does not do

Recorded so the next session does not have to re-derive the boundary:

- No change to the reproduction scheduler, even though `CreatureId` ordering under a binding cap is a
  real skew source. Task 4 measures whether it matters; the change is a separate decision.
- No Ne implementation, no founder-genetics change, no new neutral markers.
- No survival-analysis machinery. The horizons in Task 2 remove the censoring rather than modelling
  it, and the burden of proof is on anyone who wants Kaplan-Meier here.
- No change to feeding behaviour, digestion coefficients, or any biological constant.
- No verdict update in the frozen spec's section 4 gate table. This milestone produces evidence; the
  gate verdict is the user's call once the evidence exists.

---

## Appendix (Task 11): the drift / Ne note

Appended 2026-09-03, after Tasks 1-8 were implemented. **This is a note, not an implementation.** No
Ne system was built and no founder genetics were touched.

### What inspection of the current architecture establishes

- The marker is a **single locus, effectively haploid** — `GenomeInheritance.InheritTrait` picks one
  parent's value outright rather than averaging, so there is no blending collapse of variance. That
  part of the architecture is *good* for drift work and should not be described as broken.
- Founders are **monomorphic at exactly 0.5**. There is therefore **no standing variation at t = 0**,
  and every unit of between-replicate variance early in a run is mutational input rather than drift
  acting on existing variation. This, not the continuous-trait nature, is why the proposed
  "between-replicate marker means → Ne" method is too simplistic.
- Mutation adds `N(0, 0.03²)` at **every birth**, so mutational input scales with birth count, which
  is itself a fitness quantity — the confound is not additive noise, it is correlated with the thing
  being measured.
- `Clamp01` makes the process a bounded random walk with reflecting-ish barriers; the recorded
  U-shaped distribution is a signature of the clamp, not only of drift.

### Four candidate next steps, judged on merit

1. **Reproductive-skew / opportunity-for-selection summaries.** Variance in lifetime reproductive
   success over mean squared, computed on the Task 2 cohort. Cheapest, needs nothing new after this
   milestone, and is a real demographic quantity — but it is a *demographic proxy* in an
   age-structured, overlapping-generations population, where the several definitions of Ne do not
   coincide. Report it as skew, never as an Ne.
2. **Neutral-marker standing variation.** Track within-world variance of the marker over time rather
   than between-world means. Sees the clamp and the mutational input directly. Weak because there is
   exactly one marker: one locus is one realisation of a stochastic process, and no number of seeds
   fixes that the marker's own trajectory is a single draw per world.
3. **Within-world variance trajectories** for real traits. Useful as pattern validation alongside
   anything else, useless alone — selection and drift both move variance.
4. **Offline synthetic markers replayed over the recorded pedigree.** Because nothing in the
   simulation reads `NeutralMarker`, the pedigree is *causally independent* of any neutral locus. An
   arbitrary number of independent synthetic neutral loci — biallelic, unclamped continuous, whatever
   the question needs — can be replayed over the exact recorded pedigree offline, with the real
   mutation model or a cleaner one, and no simulation change, no founder change, and no new RNG in
   the tick path. **The strongest option on merit, not merely the convenient one:** it removes the
   single-realisation problem (many loci), removes the clamp artifact (choose the architecture),
   separates mutational input from drift (run a zero-mutation arm), and costs nothing in determinism
   because it happens entirely outside the world.

### Corrections that implementing Tasks 1-8 revealed

- **"It fails silently if the ledger is ever incomplete" is no longer true, and that was the main
  cost held against option 4.** `LifeHistoryLedger.IsComplete` mirrors the ancestry watermark and
  permanent-overflow semantics, and **both** `FitnessCohort.Select` and `IngestionLedger.Join` throw
  on an incomplete ledger rather than reporting a smaller number. A replay built on the same ledger
  inherits that refusal for free. Option 4 is now cheaper than the plan assumed, not more expensive.
- **The pedigree walk option 4 needs already exists.** `LifeHistoryLedger` exposes `PedigreeCount`,
  `GetPedigreeIdAt`, `OffspringCredited` and `OffspringAt`, and every record carries both parents and
  a birth tick. Replaying a synthetic locus is a walk over that, in birth order, with no new
  traversal machinery.
- **One new caveat, from how genomes are captured.** The ledger captures a genome by *observation*,
  so a creature born and dead between two `Observe` calls is in the pedigree with
  `HasGenome == false`. That does not affect a synthetic marker, which is assigned by the replay
  rather than read from the world — but any replay that wants to *compare* a synthetic locus against
  the real `NeutralMarker` must skip those creatures explicitly, and `FitnessCohort` already counts
  them as `ExcludedWithoutGenome` so the number is visible rather than assumed to be zero.
- **The founder-monomorphism point is confirmed in source, not inferred.** `Genome`'s constructor
  defaults `neutralMarker` to `0.5f` and neither founder factory varies it.

### Recommendation to carry forward, not to act on now

Option 4 as the drift diagnostic, with option 1 reported beside it as an independent demographic
check, and **both explicitly labelled as diagnostics of the strength of drift rather than as an
authoritative Ne.** The frozen spec already says the goal is "a useful diagnostic of the strength of
drift relative to census size, not a definitive Ne"; there is no authoritative Ne to be had in an
age-structured overlapping-generations population of this size, and no plan should pretend otherwise.
