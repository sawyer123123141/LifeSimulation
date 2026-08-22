# Field Notes for the Lead Agent

`AGENTS.md` is the contract for **implementation subagents** — narrow scope, do
only what the task names. This file is for the **lead agent**: the one holding
the session, deciding what to investigate, and dispatching the work.

Two purposes:

1. **A map, so you do not re-read the repository.** Reading the whole tree to
   answer "where is X" burns the user's usage for something already known.
2. **Accumulated judgment.** Every entry below was paid for with a wrong turn.
   Append to it; do not rewrite it.

**How to maintain this file:** when a session ends with a lesson that would have
saved time had you known it at the start, add it to §5 with a date. When the
live/dead status of a mechanism changes, update §4 in the same commit as the
code. Keep entries short and concrete — a rule nobody reads is worthless.

---

## 1. Where things are

Grep by **name**, not by line number — line numbers drift and this file will lie
to you within a week. Names below are stable.

### Simulation core — `Assets/Scripts/Simulation/`

| File | Responsibility | Entry points you will actually want |
|---|---|---|
| `Core/SimulationWorld.cs` (~1580 lines) | The tick loop and all system wiring. Everything connects here. | `Step`, `TickDecisions`, `TickMovement`, `TickNeeds`, `ComputeStateHash`, `GetMovementTarget`, `TryScoreBestRememberedPlace` |
| `Core/SimulationConfig.cs` | Every tuning constant and feature flag; the `CreatePrototypeNDefaults` factories | `CreatePrototype4Defaults`, `DecisionPolicyVersion`, `ComputeMemorySlotCount` |
| `Core/SimulationTypes.cs` | All state structs and enums | `MemoryState`, `PlaceMemory`, `CreatureNeeds`, `MovementState`, `RandomDomain`, `SimulationStatistics` |
| `Core/CreatureStore.cs` | Struct-of-arrays creature storage; swap-remove on death | `GetNeedsRefAt`, `GetMemoryRefAt`, `GetPlaceMemoryRefAt`, `TryGetIndex` |
| `Core/DeterministicRandom.cs` | **Never edit.** All randomness. | `Float01` |
| `Behavior/DecisionSystem.cs` (~980 lines) | Both decision policies | `DecideIntentUtilityV1` (P4 uses this), `DecideFromLearnedOutcomes`, `Decide` (Legacy), `ScoreResourceCandidates`, `ScoreRememberedResource` |
| `Behavior/MemorySystem.cs` | Scalar memory and the (dead) place memory | `RememberResource`, `RecordFailedSearch`, `LearnResourceOutcome`, `TickDecay` |
| `Behavior/ForagingEconomics.cs` | Patch scoring, commitment, give-up | `PatchScore`, `ThreatAvoidance`, `CommitmentBonus`, `ShouldAbandon` |
| `Behavior/HomeRangeSystem.cs` | P4a's dedicated, flag-gated soft home-range arithmetic. This is intentionally separate from inert place memory. | `RecordSuccess`, `TickDecay`, `GetCandidateBonus` |
| `Behavior/PerceptionSystem.cs` | Vision queries against the uniform grid | `FindNearestAvailableResource`, `FindAvailableResources`, `FindNearestOtherCreature` |
| `Biology/GenomePhenotype.cs` | The 24-gene genome and its derived phenotype | `Genome`, `Phenotype`, `PlantPhenotype` lives elsewhere |
| `Biology/GenomeInheritance.cs` | Crossover and mutation | `CreateChild`, `InheritTrait` (trait indices are positional — see §5) |
| `Biology/ReproductionSystem.cs` | Mating, birth, cost | `CanReproduce`, `CanSeekMate`, `ChargeCost`, `AdultAgeSeconds` |
| `Biology/NeedsSystem.cs` | Energy/hydration/health/rest per tick | `Step`, `RestCapacity` |
| `Environment/PlantGenome.cs` | Plant genome and `PlantPhenotype` | `BaseLifespanSeconds`, `FromGenome` |
| `Environment/PlantPatchStore.cs` | Struct-of-arrays plant storage | `Add`, `RemoveAt` (swap-remove), `FindIndex` |
| `Environment/PlantGrowthSystem.cs` | Logistic growth, sprout floor | `Step`, `SproutFloorFraction` |
| `Environment/PlantReproductionSystem.cs` | Dispersal and site competition | `Step`, `FindSite` |
| `Environment/PlantMortalitySystem.cs` | Age-based patch death | `Step` |
| `Environment/PlantSiteRegistry.cs` | Which sites are free to colonize | — |
| `Resources/ResourceStore.cs` | Food/water/carcass resources | `SetFoodProjection`, `SetActive`, `GetAt`, `FindIndex` |
| `Experiments/SimulationScenario.cs` | **All scenario definitions.** `Prototype4Scenarios` is here. | `ConsumerDefenseCalibrationModerate`, `CreateConsumerDefenseCalibrationScenario`, `ApplyTo` |
| `Experiments/ExperimentRunner.cs` | Headless run harness | `Run(config, scenario, ticks)` |
| `Experiments/PairedExperimentAnalysis.cs` | Statistics for paired experiments | `Assess`, `Summarize`, `ExperimentMetric` |
| `Diagnostics/GeneLivenessAnalysis.cs` | **The authority on whether a gene reaches behavior.** Perturbation, not caller-search. | `Analyze`, `Report`, `GeneLivenessResult` |
| `Diagnostics/FlagLivenessAnalysis.cs` | **The authority on whether a config flag does anything.** Reflection over the constructor, so new flags are covered without anyone remembering. | `Analyze`, `Report`, `FlagLivenessResult` |
| `Diagnostics/PlantGeneLivenessAnalysis.cs` | Same perturbation method for `PlantGenome`. The animal harness does not cover plant genes. | `Analyze`, `Report`, `PlantGeneLivenessResult` |
| `Diagnostics/LivenessRecorder.cs` | Runtime probe counters for code *paths*; covers the §4 "runs on empty data" class that perturbation cannot reach. Attach via `SimulationWorld.Liveness` (null by default); never touches any hash. | `LivenessProbe`, `RecordOutcome`, `IsInertlyExecuting` |
| `Analysis/PopulationGenomeSnapshot.cs` | Immutable full/sample genome capture with explicit provenance. | `Capture`, `CaptureSample` |
| `Analysis/GeneticClusters.cs` | Deterministic genetic-distance clustering for one snapshot and threshold. | `From` |
| `Analysis/AncestryHistory.cs` | Host-fed ancestry ledger with completeness watermark and permanent overflow/discontinuity semantics. It reads event batches; it never clears the world's event buffer. | `RecordFounders`, `RecordCompleteBatch` |
| `Analysis/GeneticClusterHistory.cs` (~1310 lines) | Conservative ancestry-supported cluster continuity/split/merge/extinction analysis. Analysis-only; never simulation truth. Known decomposition debt. | `Record` |
| `Analysis/P5HistoryPanelSession.cs` | Pure host-triggered bridge that samples the world at a fixed cadence and feeds the P5 panel. Lives in Simulation so headless tests can cover it; the presenter owns and advances it. | `CreateForWorld`, `Advance` |

### Elsewhere

- `Assets/Scripts/Presentation/Prototype1Presenter.cs` — Unity view, playtest
  hotkeys (`B/D/F/P/C/T/G/M/E`, `5/6/7/9`, `N`, `H`), the creature inspector,
  and the P5 evidence panel. `_world` is non-serialized;
  `EnsureInitialized()` guards domain reloads during Play mode.
- `Assets/Editor/PrototypeBatchEntry.cs` — batch experiment entry points.
- `Assets/Tests/EditMode/*.cs` — 377 tests. `ResourceExperimentTests.cs` holds
  the scenario/calibration tests; `CoreSimulationTests.cs` is the big one;
  `LivenessTests.cs` enforces the §4 gene ledger by perturbation.
- `tools/HeadlessTests/` — `dotnet test` runs the EditMode tests headlessly.
  This is how you run tests; Unity is only needed for Play mode.

### Documentation worth knowing exists

- `docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md` —
  **the known-defect register (B-1, B-4, B-7 …).** Check here before "fixing" a
  bug; it records which defects are deliberate deferrals.
- `docs/experiments/` — dated experiment records. Several supersede each other;
  read the banners at the top before trusting a conclusion.
- `docs/ARCHITECTURE.md`, `docs/PERFORMANCE.md`, `README.md`.

---

## 2. Before you fix a bug, ask whether it should be fixed

Finding a defect is not a mandate to repair it. Ask, in order:

1. **Is it already registered?** Check the defect register. Several entries are
   known and deliberately deferred, sometimes to a phase that will rewrite that
   code anyway. Re-fixing one wastes work and can conflict with a planned
   design.
2. **Is it load-bearing for a pending measurement?** Any behavior change
   invalidates calibrations and experiment baselines taken before it. If an
   experiment is queued, fixing a live-path bug first means re-running
   everything already measured. Sequence deliberately.
3. **Does the fix require a rework it does not admit to?** A one-line fix that
   only makes sense alongside a larger redesign is not a one-line fix. Say so
   and let the human sequence it, rather than landing the line and leaving the
   design half-migrated.
4. **Is it actually reachable?** A defect in code that never executes is not
   costing anything today. It is worth *recording*, not necessarily fixing. Six
   mechanisms in this repo are written, tested, and never reached (§4). Two of
   those were deleted on 2026-08-18; check §4, do not assume the count.
5. **Would fixing it perturb a phase boundary?** Terrain generation (P6) is
   explicitly last, gated behind P4 and P5. Do not "improve" things it will
   replace.

If the answer to any of these is uncertain, **report the defect and ask**. A
recorded defect costs nothing. An unplanned fix can cost a re-run of every
experiment.

The corollary: **do not describe a defect as fixed until you have shown the fix
reaches production behavior.** See the `Persistence` entry in §5.

---

## 3. How to investigate without burning usage

- **Measure before theorising.** Writing a plausible mechanism story and then
  building on it is the single most expensive failure mode in this project. It
  has produced four wrong conclusions, each caught only after work was built on
  it (§5).
- **Read the code before hypothesising about it.** Three hypotheses in one
  session were killed by measurement; the fourth was found in ten minutes by
  reading. Reading is cheaper than a 30-seed sweep.
- Throwaway probes go in `Assets/Tests/EditMode/ZZZ*.cs`, run via
  `dotnet test --filter "FullyQualifiedName~ZZZName"`, and are **deleted before
  committing**. Verify the tree is clean afterwards.
- 30-seed × 12,000-tick sweeps take a few minutes. Run them in the background
  and do something else. Five seeds is usually not enough to distinguish arms —
  a result that looks significant at n=5 has vanished at n=30 here before.
- Prefer one sweep that varies **one** variable over a sweep that varies several
  and hopes the effect is attributable.

---

## 4. Live / dead mechanism ledger

Maintained because "the code exists" is unrelated to "the code runs", and
rediscovering this is expensive. **Update in the same commit as any change.**
Full evidence: `docs/experiments/halfway-wired-mechanism-audit-2026-08-17.md`,
corrected by `docs/experiments/gene-liveness-perturbation-2026-08-18.md`.

> **What perturbation cannot tell you: reward.** All three harnesses detect
> *influence* — whether a gene or flag changes what the simulation does. A
> **pure-cost** gene passes: plant `TemperatureTolerance` reads live purely because
> `PlantPhenotype` charges `-.10f` growth for it, while having (before 2026-08-18)
> no channel to earn that back under any environment. "Live" is not "selectable
> for". When a trait will not evolve, check that it has a *benefit* path, not just
> a reader. See `docs/experiments/plant-gene-liveness-2026-08-18.md`.

**You should not need to re-audit this by hand.** `LivenessTests` enforces the
gene half of this table by perturbation and fails the build when it changes. Run
`dotnet test --filter "FullyQualifiedName~LivenessTests"` instead of grepping for
callers. Two entries below were wrong precisely because a caller-search was used.

**Verdicts are scoped to the scenario measured.** A gene reading dead in a narrow
scenario may simply have no occasion to fire — `RiskAversion` reads dead under
`CreatePrototype4Defaults` because the herbivore calibration produces no threats.
Always pin liveness against `CreateFullEcosystemDefaults`, which exists for this.

**Never executes in production (as of 2026-08-17):**

| Mechanism | Note |
|---|---|
| `MemorySystem.ObservePlace` | Only writer of place-memory slots. Tests only. |
| `MemorySystem.TickPlaceMemoryDecay` | No production caller. |
| `Genome.NeutralMarker` | Renamed from `Genome.Commitment` on 2026-08-18: the old name collided with the unrelated `ForagingEconomics.CommitmentBonus` and `SimulationConfig.CommitmentStrength` foraging machinery, which takes `Persistence`. That collision is exactly what led the 2026-08-17 audit to assume the gene fed the bonus. Inherited, mutated, hashed, aggregated into statistics, exposed as `ExperimentMetric.NeutralMarker` — and read by **zero** behavior code. **Kept deliberately** as the drift-control channel that validates the bootstrap pipeline; confirmed dead by perturbation under FULL ecosystem mode and pinned by `LivenessTests`. Do not wire it. |

**Executes but always on empty data:** `TryScoreBestRememberedPlace`,
`RecordFailedPlaceSearch` — both depend on place-memory slots, which are never
populated.

> **Now enforced.** Neither perturbation harness can reach this class — there is
> no gene and no flag to flip for place memory — so it was documented and nothing
> checked it. `SimulationWorld.Liveness` (an optional `LivenessRecorder`, null by
> default) probes both sites, and
> `LivenessTests.PlaceMemoryProbesRunButNeverTakeEffect` asserts they stay INERT.
> A live verdict there means someone wired `ObservePlace`, which is a real
> behavior change that invalidates baselines measured before it.
>
> Deliberately **not** behind a config flag: a diagnostics flag must be
> behavior-inert to be correct, and `FlagLivenessAnalysis` would then report it
> inert and fail the known-inert-flag assertion. An optional sink avoids that.

**Unreachable under `IntentUtilityV1`** (the policy every P4 scenario uses):
`ForagingEconomics.CommitmentBonus` (Legacy foraging path only) and
`ForagingEconomics.ShouldAbandon` (`ForagingEconomicsEnabled && Legacy &&
!CognitionEnabled`).

> **CORRECTED 2026-08-18.** This entry previously concluded "**`Persistence` has
> no behavioral effect under P4**". That is **withdrawn**. Perturbation diverges
> the behavior hash at tick 10 under plain P4 defaults: `GenomePhenotype.cs:351`
> adds `0.05f * genome.Persistence` into `bodyMass`, which sets energy capacity,
> speed and metabolic cost. Only the *foraging commitment* half is Legacy-only.
> `Persistence` is **not** an inert channel and must not be used as a placebo.

**Also live but easy to mis-audit:** `RiskAversion` has three real call sites in
`DecisionSystem`, all gated on a valid threat. It reads dead under P4's herbivore
calibration and live under FULL ecosystem mode. Not dead code.

### Inert config flags (measured 2026-08-18, `FlagLivenessAnalysis`)

Flipping any of these produces a **bit-identical run**, under P4 defaults *and*
under FULL ecosystem mode where all four are on:

| flag | why |
|---|---|
| `foragingEconomicsEnabled` | its behavioral consumers (`CommitmentBonus`, `ShouldAbandon`) are Legacy-only. **Nuance added 2026-08-19:** it also gates `AdvanceForagingActionTime` and `UpdateForagingIntakeRate`, which *do* run every tick on the live path and maintain foraging state nothing on that path consumes — the "executes but nothing consumes the result" class, not "unwired". |
| `multiThreatPerceptionEnabled` | **CORRECTED 2026-08-19 — the old reason ("`IntentUtilityV1` carries its own inline threat handling") is WITHDRAWN.** The flag is passed into `DecideIntentUtilityV1` and selects between `ScorePredationMulti` and `ScorePredation`. It is inert only because the pinned sweep runs a **herbivore** scenario with no threats, so both branches score nothing. |
| `kinRecognitionEnabled` | **CORRECTED 2026-08-19 — the old reason ("no reader on the `IntentUtilityV1` path") is WITHDRAWN.** It is passed in and read inside both predation scorers, gating `IsKin`. Same scenario-scoping as above. |
| `learnedResourceQualityEnabled` | single reader is inside `DecideFromLearnedOutcomes`, the Legacy+Cognition path |

> **Two of these four are unexercised, not unwired — do not delete them.** Every use site of
> `multiThreatPerceptionEnabled` and `kinRecognitionEnabled` sits inside `if (predationEnabled)`,
> and `CreateFullEcosystemDefaults` widens the *config* but not the *scenario*. Adjudicating them
> needs a **survivable predator-prey scenario**, which does not exist: `FounderProfile.PredationVariation`
> is extinct before 3,000 ticks with zero births on the plant calibration, so every verdict measured
> there — in both directions — is measured on a corpse. See
> `docs/experiments/p4-inert-flags-readjudicated-2026-08-19.md`.
| `plantTemperatureAdaptationEnabled` | **different reason — temporary.** Fully wired on the live path, but `EnvironmentField` returns `Temperature = 1` everywhere, and the adaptation expression collapses to the raw value at 1. **Move it out of `KnownInertFlags` when terrain fields land**; the test failing is the correct signal that it went live. |

> The 2026-08-17 audit cleared all sixteen flags because each "has at least one
> production reader". That is true of all four above and **insufficient** — the
> reader exists on a path `IntentUtilityV1` never takes. Same error shape as the
> `Persistence` entry.

**Do not turn one of these on expecting an effect**, and do not verify one by
grepping for its name. `LivenessTests.InertFlagsAreExactlyTheKnownSetUnderTheWidestConfiguration`
pins the set and fails the build if it changes — a flag becoming live is a real
behavior change that invalidates every baseline measured before it.

Corollary worth stating: FULL ecosystem mode is the widest surface *available*,
but for these four no configuration gives them a chance short of switching to
`Legacy`. "Widest available" is not "every mechanism gets its chance".

**Verified live, do not re-audit:** `CognitionRestCostMultiplier`,
`ReproductionCooldownSeconds`, `ReproductionEnergyCostFraction`, all
`SimulationConfig` flags, and every state-struct field.

---

## 5. Lessons log

Append with a date. Keep each entry to what a future session must not repeat.

**2026-08-17 — A mechanism story is not evidence.** Four conclusions were wrong
in one session because a plausible causal story was written before anything was
measured: place memory blamed as the cause of extinctions (the subsystem never
ran); "the calibration constraint set is unsatisfiable" (it was satisfiable);
"the Persistence fix resolved B-4" (Persistence is Legacy-only, so it did not);
and a site-count sweep read as proving site count when it also varied geometry.
Each was caught, but only after work had been built on it. Measure first.

**2026-08-17 — Transcribe measured configurations; never reconstruct one that
looks equivalent.** A probe measured four *clustered* plant sites; the
implementation used four *spread* sites on the reasoning that the count was what
mattered. Extinctions went from 0/30 to 16/30. Copy the exact numbers that were
measured, or measure the exact numbers you intend to ship.

**2026-08-17 — Confounded sweeps.** When a sweep generates its arms
procedurally, check what *else* changes between them. The site-count sweep moved
sites along a grid, so count and spatial spread moved together and the result
was credited entirely to count.

**2026-08-17 — Positional constructor arguments silently drop genes.**
`GenomeInheritance.CreateChild` passed 23 of `Genome`'s 24 positional
parameters, so `persistence` took its default for every creature ever born, and
nothing failed. When adding a gene, add it to the inheritance call, the
constructor, the hash, and a test that asserts *every* gene transmits with
pairwise-distinct values.

**2026-08-17 — A static caller-search is not a liveness check.** Grepping for
callers finds "nothing calls this". It does not find code that runs every tick
against permanently empty data (`RecordFailedPlaceSearch`), nor code that
computes a real value nothing consumes (`Commitment`). A real liveness probe
must record that a mechanism's output **entered creature state or a decision
score** — and must be excluded from `ComputeStateHash` so it does not change the
simulation it measures.

**2026-08-17 — Engine bounds are not ecological parameters.** The calibration
scenario ran two plant sites against a population cap of 48, so carrying
capacity sat below the array bound: population grew to the cap and collapsed
every time. Sweeping lifespan and cap both read as "unsatisfiable" because
neither could reach the binding variable. When every arm of a sweep fails,
suspect the variable is not in the sweep.

**2026-08-17 — A failing regression guard is usually right.** The six-site
change shipped with a guard that immediately failed. The guard was correct and
the implementation was wrong. Do not reach for the tolerance.

**2026-08-18 — When a trait will not move, check that it *can* move before
tuning pressure.** Three documents in a row treated flat plant defense as a
calibration problem and proposed raising grazing pressure. Reading the
consumption path took ten minutes and showed `ConsumeAt` removes biomass with no
defense term: defense protected zero tissue, so no pressure setting could have
produced a gradient. Before sweeping a parameter to make selection appear, verify
the trait has a path to fitness at all. `GeneLivenessAnalysis` answers this for
animal genes; for plant genes, read the consumption path.

**2026-08-18 — A prediction that fails is still a measurement; report both.**
The prediction here was that defense would be selected *down*, being a cost that
buys nothing. It was not — deltas were pure drift. The cost was unrealized too,
because patches sit at capacity and the penalty is charged against growth *rate*.
"Cost-free and benefit-free" is a different diagnosis with different fixes than
"costly and unrewarded", and only measurement separated them.

**2026-08-18 — Check the sign of a feedback, not just its magnitude.** Defense
lowers nutrition per unit eaten, so consumers compensate by eating more: realized
grazing rose 2.2x from defense 0.0 to 0.9. The mechanism was not weak, it was
*inverted* relative to the ecology being modelled. A metric reported without its
sign checked would have read as "pressure exists, so the null is real".

**2026-08-18 — A caller-search misses readers inside aggregate expressions.**
The 2026-08-17 audit reported `Persistence` as having exactly three readers and
concluded it was behaviorally inert under P4. It missed `0.05f * genome.Persistence`
inside the `bodyMass` weighted sum — a real production path found immediately by
perturbation. Grep finds *names*; it does not tell you whether the value flows
anywhere. Use `LivenessTests`.

**2026-08-18 — A trait that only moves while the ecosystem collapses has not
demonstrated a gradient.** Sweeping plant regeneration down finally moved defense
(+0.1157 at founder 0.6, regeneration 3) — in an arm where 30/30 seeds lost their
animal population and plants reached generation 0. That number is not a positive
control and must not be cited as one. When a long-flat trait suddenly responds,
check the survival columns in the same row before celebrating.

**2026-08-18 — A one-dimensional pressure lever moves two things here.**
`RegenerationPerSecond` sets both the food supply for the whole animal population
and how depletable an individual patch is. Sweeping it cannot find a window where
grazing bites locally without starving the population globally — the arms trade
one failure for the other, the same shape as the lifespan and population-cap
sweeps that were declared "unsatisfiable" in 2026-08-17. When every arm fails in
alternating directions, the lever is coupled and the fix is a scenario redesign,
not another sweep.

**2026-08-18 — Selection cannot act without standing variance, and a uniform
founder value provides none.** Every plant-defense sweep from 2026-08-17 onward
seeded *all* patches with the same defense, varying it only between arms. Response
to selection is proportional to within-population variance, so the flat results
measured drift and nothing else. Seeding three of six sites defended produced
deltas of -0.05, ten to thirty times any earlier arm. Before concluding a trait
"does not respond", check that the founders actually differ in it.

**2026-08-18 — `dotnet test` passing did not mean Unity compiles.**
`tools/HeadlessTests/GlobalUsings.cs` carried `global using System;`, which Unity
has no equivalent of. A file using `System.Math` without `using System;` passed all
385 headless tests and then failed in the editor with seven `CS0103`s. The file is
now empty and commented to stay that way — emptying it produced zero errors, so
nothing depended on it — and `<ImplicitUsings>disable</ImplicitUsings>` is set.
Verified by deleting a `using` and confirming the headless build now fails (14
errors) where it previously succeeded. **If a green headless run is ever
contradicted by the editor again, check for global usings first.**

**2026-08-18 — A liveness test cannot tell you a trait is worth carrying.**
Perturbation detects influence, not reward, so a **pure-cost** gene passes it.
Plant `TemperatureTolerance` read live only because it charges `-.10f` growth,
while temperature entered `PlantGrowthSystem` as a raw limit no gene could improve
against. Prediction that it would read *dead* was wrong for that reason. When a
trait refuses to evolve, ask what its **benefit path** is — a reader is not a
benefit. `MoistureTolerance` next to it is the reference for what a real trade-off
looks like.

**2026-08-18 — Environment constants silently disable whole gene channels.**
`EnvironmentField` returns `Fertility = 1` and `Temperature = 1` on every
production path, so the growth limit always collapsed to `moistureAdaptation` and
two of three environment channels did nothing. Before adding a trait that adapts
to an environment field, check the field actually varies — otherwise the trait ships
as a tax. This is why terrain work needs the field *and* the adaptation term
together; landing only the field makes such genes more costly, not more meaningful.

**2026-08-18 — Compare an effect against its own sampling error, never against
another arm whose spread is small for structural reasons.** The -0.05 defense
decline was called "measurable directional selection" because it was 10-30x the
deltas in the uniform-founder arms. But uniform founders have no standing
variance, so no lineage can fix and their SD is structurally tiny — the
comparison measured the presence of variance, not of selection. Against its own
SE the decline is t = -1.44, 95% CI [-0.122, +0.032]. **Retracted the same day.**
This is the recorded "n=5 looked real, vanished at n=30" failure in a subtler
form: the error was not sample size but a baseline chosen for the wrong reason.
Compute SD, SE and a bootstrap CI before calling anything a response.

**2026-08-18 — A six-patch population is drift-dominated; expect to need far more
than 30 seeds.** Standing variance triples the outcome SD (0.208 vs 0.069), which
is lineage fixation, not selection. At that SD an effect near -0.055 needs roughly
230 seeds for 80% power. Raising the **patch count** shrinks the spread far more
efficiently than raising the seed count, because drift scales inversely with
population size. Check the achievable power before designing another plant-trait
experiment at six sites.

**2026-08-18 — A control that moves the trait downward would still be a valid
positive control.** The principle holds and is worth keeping: the blocker asks for
proof the setup can detect selection at all, and a demonstrated *decline* answers
that as well as a rise. Do not keep hunting for an upward response to prove a
pipeline works. (The 2026-08-18 attempt to supply one failed on significance, not
on direction — see the entry above.)

**2026-08-18 — A clamped score term silently deletes a whole decision dimension.**
`ComputeNeedGain` ends in `Math.Min(1f, ..)` and returned exactly 1.0000 for 88 of
88 patch-and-hunger combinations, ~10x over the clamp even at 5% energy. Patch
quality was therefore invisible to `IntentUtilityV1` foraging — grazers chose on
hunger and distance alone. When a scoring term is bounded, measure its realized
distribution before assuming it discriminates.

**2026-08-18 — Identical results across an arm flip means the flag is dead, not
that the effect is small.** A sweep adding `learnedResourceQualityEnabled` to the
deterrence arm returned numbers matching the previous run to four decimals in
every cell. That is not a weak effect — floating-point ecology does not reproduce
by coincidence. Bit-identical output is a liveness signal; read it that way
immediately instead of interpreting the "difference".

**2026-08-18 — Per-patch drawdown and population survival cannot be separated by
site count.** Grazers-per-patch and regen-per-patch scale together, so their
ratio is `total_need / total_regen` regardless of how many sites exist. Drawing
the *average* patch below capacity therefore requires the population to need more
biomass than the system regrows — starvation by definition. Selection on a defense
trait needs **heterogeneity** (some patches grazed hard, others spared), not more
average pressure. Any future "raise grazing pressure" proposal should be checked
against this before it costs another sweep.

**2026-08-18 — Perturbation is blind to code that runs on empty data.** Flipping a
gene or flag answers "does this matter"; it cannot see a branch that executes every
tick and never does anything, because there is nothing to flip. That is the §4
Class B shape and it is what produced the retracted place-memory root cause. A
recorder with an explicit INERT verdict (reached > 0, effective == 0) is the only
thing that catches it — and it needs one probe on a known-live path, or a silently
broken recorder looks identical to a correct one reporting all-inert.

**2026-08-18 — Never run `perl -0pi` over these docs.** Without `-CSD` it reads
UTF-8 as latin1 and rewrites it, turning every em-dash and `§` into mojibake. It
corrupted 62 lines of this file and the damage was committed and pushed before
anyone looked at the result. Use the Edit tool for prose; reserve `sed`/`perl` for
ASCII-only source, and check the file after a bulk rewrite rather than trusting
the exit code.

**2026-08-18 — Perturbation must cover config flags, not just genes.** Four of
sixteen flags are inert under `IntentUtilityV1` and every one had passed a
caller-search audit. `FlagLivenessAnalysis` flips each flag and compares behavior
hashes; it enumerates flags by reflection so a newly added flag is covered without
anyone remembering to add it. Run it before trusting any flag.

**2026-08-18 — A name collision is a defect when it makes a wrong belief
plausible.** `Genome.Commitment` sat beside `ForagingEconomics.CommitmentBonus`
and `SimulationConfig.CommitmentStrength`, all unrelated — the bonus takes
`Persistence`. An audit read the shared word and concluded the gene fed the
bonus. Renaming it `NeutralMarker` cost nothing and removes the trap. When two
unrelated concepts share a name in this codebase, treat that as worth fixing
rather than worth a comment.

**2026-08-18 — Liveness verdicts are scoped to the scenario, and a narrow
scenario manufactures false deaths.** `RiskAversion` reads dead under P4 defaults
purely because the herbivore calibration never produces a threat for its three
guarded call sites. Had that been recorded as "dead code" it would have been
deleted. Always pin liveness against `CreateFullEcosystemDefaults`, and state the
scenario alongside any negative verdict.

**2026-08-19 — The measured config includes the engine bounds, and "looks equivalent" is
how you lose them.** A probe rebuilt the calibration config by hand and passed
`baseline.MaximumPopulation` (**1000**) where the committed guard
`ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds` pins
`maximumPopulation: 48`. The population ran to ~310, stripped every patch, and gave 30/30
extinct on a scenario whose own comment records 0/30 — which was briefly reported as
behavior drift before the cap was found. Transcribing 48 restored 0/30 exactly. This is the
2026-08-17 "transcribe, never reconstruct" lesson recurring in the one place easiest to miss:
the cap is not an ecological parameter, so it does not look like part of the configuration.
**Copy the config out of the committed guard, do not rebuild it from the factory defaults.**

**2026-08-19 — Answer the variance objection by measuring it, not by conceding it.** A null
on a trait is only as good as the standing variance it was measured at, because response to
selection is proportional to variance. Rather than caveat the tolerance null, a second pair of
arms widened *only* the two tolerance founders to Uniform(.2, .8) — doubling the outcome SD —
and the null held. One extra arm pair converts "suggestive, conditional on founder spread"
into a result. Widening *every* trait instead destroys the operating point: the first attempt
did that and lost the whole ecosystem.

**2026-08-19 — Report the sign test next to the t.** `Defense` under the moisture-gradient arm
gave t -2.05 with 50 of 120 seeds down — no directional consistency at all, a t driven by a
few large magnitudes. A t alone would have been read as a near-significant decline, which is
the shape of the claim retracted on 2026-08-18. The sign test costs one `awk` line over the
per-run CSV.

**2026-08-19 — Measure every trait in the same runs; the free ones are the controls.**
Recording all eight plant genes instead of the two under investigation cost nothing extra and
produced both the refutation of that session's own hypothesis and the strongest positive
control the project has (`Dispersal`, t 14-17 across four arms). A sweep scoped to the trait
you are curious about cannot tell you the effect belongs to that trait.

**2026-08-19 — A `Min` over channels hands all selection to the channel no gene can answer,
and adaptation makes it worse.** `PlantGrowthSystem` takes `limit = Min(moistureAdaptation,
Fertility, temperatureLimit)`. Fertility has no genome modulation, and it binds **82-90%** of
plant-reachable positions — *rising* as tolerance rises, because each adaptation term lifts its
own channel out of contention for the minimum. Raising both tolerances .35 to .65 buys +2.23%
growth limit against -7.76% on `GrowthRateMultiplier`, net **-5.7%** on a rate that is
multiplied by `(1 - Biomass/Capacity)` and so is barely paid at capacity. Before adding another
environment channel or another adaptation gene, check which channel actually binds — see
`docs/experiments/p4-fertility-binds-the-growth-limit-2026-08-19.md`.

**2026-08-19 — Plant growth-RATE traits are nearly unselectable, and no adaptation term fixes
that.** `growth` is multiplied by `(1 - Biomass/Capacity)`, and patches live near capacity:
measured mean gate **0.1711**, with 39.8% of patch-ticks within 1% of capacity. A trait that
changes growth rate by X% changes realised growth by roughly `0.17 * X%`, and by ~nothing for
two fifths of the time. That single fact covers every plant-trait null on record — Nutrition,
Defense, WaterEfficiency, both tolerances and `NutrientUptake` all route through
`GrowthRateMultiplier`, while `Dispersal` (t 14-17) and `SeedInvestment` (t 4.8-6.8) act on
colonisation and are ungated. **Three sessions have now tried to make a growth-rate trait
selectable by improving its benefit channel; the benefit channel was never the constraint.**
Do not run a fourth. Selection on plants has to act on establishment, mortality or seed
production. See `docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md`.

**2026-08-19 — An environment channel needs the binding channel lifted AND the patches below
capacity before it can matter; neither alone is enough.** `ElevationFieldEnabled` drops
temperature by 0.18 on average at plant positions and is nonetheless **bit-identical** under the
standard P4 plant config. It only goes live when the population cap is 1000 (grazing pulls
patches off capacity, so the growth limit has something to act on) **and**
`PlantFertilityAdaptationEnabled` is on (fertility stops binding the `Min`, so temperature can).
Two guesses at a single cause were both wrong before the two-factor test separated them. When a
new environment channel reads inert, check both conditions before concluding the wiring is
broken — and note that this makes the fertility adaptation term a **precondition for any other
channel mattering**, which is a better reason to keep it than the one it was built on. See
`docs/experiments/p4-elevation-field-2026-08-19.md`.

**2026-08-19 — "Widest configuration" is not "widest scenario", and a flag sweep inherits the
scenario's blind spots.** `FlagLivenessAnalysis` pins against `CreateFullEcosystemDefaults` on
`ConsumerDefenseCalibrationModerate`. That turns every flag on, and still cannot exercise
anything gated on `predationEnabled`, because the calibration is a herbivore world with no
threats. Two flags were recorded as unwired on that basis when both are fully wired into
`DecideIntentUtilityV1`. **Before writing down *why* a flag is inert, find its use sites and
check whether the scenario can reach them** — the conclusion "inert here" is cheap and correct;
the explanation is what gets code deleted.

**2026-08-19 — Print the survival columns on every arm, including the ones you expect to
work.** Switching to `PredationVariation` founders moved a flag out of the inert set and was
briefly reported as a confirmed prediction. The population was extinct before 3,000 ticks with
**zero births**: the flag was changing how the collapse unfolded, and three other flags went
"inert" purely because nothing lived to mate, parent or rest. The survival check that caught it
took one probe. Arms that appear to confirm a hypothesis need the check more than arms that
refute one.

**2026-08-19 — Regress the final value on the founder value, never the delta.** Testing whether
a trait converges to an equilibrium by regressing `delta` on `founderMean` is invalid: the
founder mean sits on both sides with opposite signs and manufactures a negative slope out of
pure noise. Regress **final on founder** instead — slope 1 is drift, slope 0 is full
convergence — and give it leverage, because six independent founder draws average out to an
SD of 0.073 and leave the slope undetermined. A per-seed centre for between-seed spread plus
per-site jitter for within-run standing variance answers both needs at once. The flag-off arm
came out at slope 0.9995 +/- 0.0412, which is what a correct drift baseline looks like.

**2026-08-19 — The P4 "flat environment control" is not flat.** `SimulationWorld.cs:62-64`
builds `EnvironmentField.CreateMoistureGradient()` whenever `plantCohortsEnabled` is on, which
`CreatePrototype4Defaults` sets. Moisture already varies with the procedural flag off; only
fertility and temperature are pinned at 1. Flipping `proceduralEnvironmentFieldsEnabled`
therefore changes three channels at once and is not a one-variable contrast. Do not describe
that arm as a flat or no-variation baseline.

**2026-08-20 — Share of variance is not share of available selection.** 51.9% of the variance in
per-patch plant offspring is the single binary of surviving newborn takeover, and that number was
read as if the whole 51.9% were up for grabs. It is not: most of it stays luck no matter what
gene is wired in, because a doomed seedling faces repeated attempts and the invader just retries
elsewhere. The cost of the new trait was priced against the imagined payoff and came out three
times too high, so the trait *declined* in its first sweep. **Decompose variance to find where to
look, then measure the response slope before pricing anything against it.**

**2026-08-20 — Sweep the COST, not just the benefit.** The first arm of a new plant trait read as
another null (t -2.10, 48/120 up). Three prior sessions had responded to that shape by improving
a benefit channel. Sweeping the *charge* instead — 6, 2, 0 — showed the benefit channel was fine
the whole time and the trait rises at t +6.24 uncharged, +4.03 at a charge it can afford. A
single-arm null on a trait with a cost term tells you nothing about which half is wrong.

**2026-08-20 — Pooling two death classes invented a route that does not exist.** Realised plant
lifespan looked like it explained 53% of offspring variance. It explains **2.4%**: the pooled
number came from mixing two-second infants killed by takeover with hundred-second adults. Before
attributing variance to a mechanism, split the sample by *how* the outcome happened — the
`endReason` column cost nothing and moved the conclusion from "mortality is the dominant route"
to "mortality has no headroom at all".

**2026-08-20 — A live genetic channel can convert at nearly zero.** `LifespanSeconds` gives
`Growth` a genuine 2x span and `r(Growth, lifespan) = -0.51` among patches that die of age. It
buys `r(Growth, offspring) = -0.07`, because reproduction here is site-limited, not time-limited:
91% site occupancy, a hard-coded 20-second cooldown, ~96 seconds of life, and a mean of 1.52
offspring achieved out of roughly four possible. **A strong correlation with an intermediate is
not evidence the intermediate reaches fitness.** Measure the last link.

---

**2026-08-20 — A genetic seeding-rate channel did not clear the directional bar.**
`SeedProductionRate` shortens the post-birth cooldown and bypasses the growth gate, but a
120-seed, 12,000-tick sweep found charge 0 at delta +0.01953, t +3.22, only 68/120 up;
charge 2 at delta -0.00355, t -0.62, 57/120 up; and charge 6 at delta -0.01172,
t -1.77, 53/120 up. Dispersal remained live in every arm (t +13.22 to +15.56) and
there were 0/120 extinctions, so this is a measured weak route, not a collapsed scenario.
Do not call the zero-charge arm selection or ship a nonzero price without a new, separately
predicted calibration.

**2026-08-20 — WHY that route is null, recorded so nobody retries it: the cooldown was never the
constraint.** With founders pinned so the cooldown is a constant per arm, halving it from 30s to
15s moves plant births only **203.7 to 221.8 (+8.9%)** and does not raise plant generations at
all. The lifetime budget of a 95.8-second patch is **6.7s growing to maturity, 30.4s on cooldown,
and 58.7s already mature, off cooldown, and failing to find a free site** at 91% occupancy.
Cooldown time freed by a gene is added to a pool of time that is already being wasted. **Two of
the three non-growth routes fail for the same reason, and it is not the growth gate: lifespan and
cooldown both buy TIME, and time is not the scarce resource here — free sites are.** Only
establishment acts on the scarce thing.
`docs/experiments/p4-seed-production-rate-is-not-the-constraint-2026-08-20.md`

**2026-08-20 — A task was set on an inference the same day's own data already refuted.** The
handoff asserted that `ReproductionCooldownSeconds` "is why a 96-second lifetime yields 1.52
offspring out of roughly four possible". The decomposition CSV collected hours earlier contained
`meanEligible = 58.7` against `meanBirths = 1.52`, which says a patch is *already* idle and
eligible for most of its life. Nobody did that subtraction before spending a session on it.
**When a handoff states a causal claim, check whether the last measurement already answers it —
the arithmetic here was one `awk` line over a CSV that was already committed.**

**2026-08-20 — A `t` that beats its own inert control while being LESS directionally consistent
than it is not a result.** The seeding-rate arm at charge 0 read t +3.22 with 68/120 seeds up; the
flag-disabled arm, where the gene does nothing whatsoever, read t +1.51 with **70/120** up. The
disabled arm is the null distribution, so it is the comparison that matters, and the "better" t
came with worse directional agreement. Run the flag-disabled arm as a fourth arm on every trait
sweep — it costs one arm and it calibrates what drift looks like for that trait in that harness.

**2026-08-22 — A flag-gated behavior is not Play-testable merely because it compiles and has
integration tests.** Soft home-range affinity passed its deterministic, flag-off, danger, and
liveness checks, but every shipped Play-mode scenario still leaves the new flag false — including
the `N` all-flags presenter constructor, which predates the new final optional parameter. The next
step is not more tuning from prose: add an explicit ordinary-key scenario (recommended `R`) that
differs from Stable only by enabling the flag, then measure and watch `5` versus `R`. Whenever a
new behavior flag lands, audit both factory defaults and actual presenter/scenario constructors;
an omitted optional argument silently produces a feature nobody can exercise.

**2026-08-22 — Analysis correctness and watchability are separate acceptance gates.** The P5
panel correctly reports confirmed continuity, but routine continuity can fill all eight visible
rows and hide the split/merge/extinction evidence a person actually cares about. Keep continuity
in the analysis record; presentation should hide routine continuity by default and report a compact
count such as “N routine continuities hidden.” Never weaken the analytical stream to repair UI
signal-to-noise.

**2026-08-22 - A tie-break bonus built on distance-from-recent-position cannot beat a travel
burden that already charges distance.** Soft home-range affinity was measured off/on across 30
paired seeds in stable, scarcity and migration. The learning half works (familiarity 0.80-0.89,
hashes diverge), but same-site fidelity moved by **+0.0000** in migration where 20.8% of
creature-ticks held two visible food patches, and by **+0.0001** at ten times the bonus. The
centre is dragged toward wherever the creature just fed, so distance-to-centre is nearly collinear
with distance-to-candidate, and `ResourceUtility` already penalises that. At 10x the only real
effect is clinging: stable loses 7.6% of site re-entries and 2.7% of food intake for no births.
**Before adding a term to a utility function, ask what information it carries that the existing
terms do not.** `docs/experiments/p4a-home-range-affinity-2026-08-22.md`

**2026-08-22 - Two of the three observation scenarios cannot contain a route, by geometry.**
Stable and scarcity co-locate food and water at each cluster and place the two clusters 28.8 apart
against a maximum `Phenotype.VisionRange` of 16. Measured patch fidelity is **1.0000 with the flag
off**: a creature picks one cluster at birth and never visits the other. No affinity mechanism can
improve on 1.0000. Scarcity additionally goes **30/30 extinct in both arms**, so it judges nothing.
Check that a scenario can physically express the behavior before measuring a mechanism in it.

**2026-08-22 - Building the scenario a mechanism needs is a legitimate rescue attempt, and it can
still kill the mechanism.** Soft home-range affinity read null in scenarios where the route metric
was saturated, so `ObservationRouteRing` was built specifically to give it opportunity: 90.6% of
creature-ticks with a genuine equidistant choice, familiarity 0.88, an off-arm route metric at
0.7955 with headroom in both directions. The flag then moved route repeatability **down** (t -2.87)
and same-site re-entry **up** (t +4.93). **When you rescue a null with a better test bed, commit in
advance to accepting a negative from it** - otherwise the rescue is just a search for a scenario
that flatters the code. And note the general lesson about the mechanism: a bonus proportional to
proximity-to-recent-success can only ever reward staying put; nothing in that shape rewards
completing a circuit between complementary resources.

**2026-08-22 - A changing map, not a smarter creature, is what varies routes.** With every
optional behavior flag off, turning on plant mortality in a clustered layout with dormant dispersal
sites left the *amount* of route behaviour statistically unchanged (445 versus 441 cross-kind legs)
while cutting route permanence (pair repeat -0.0935, t -6.47, 3/30 seeds up) and raising distinct
routes per creature by 27% (+0.628, t +4.48, 22/30 up) at no survival cost. The closed home-range
mechanism was trying to do from inside the creature what the environment does for free. **Check
whether an existing system can be placed in a scenario before designing a new one** - both halves
of this backlog item turned out to be already implemented and merely never switched on together.

**2026-08-22 - Extinction in a fixed-seed sweep is not a distribution when the demo runs one seed.**
`ObservationShiftingPatches` looked acceptable at 6/30 extinct until seed 42 was checked
individually: it dies in all four arms. Play mode runs seed 42, so the aggregate was irrelevant to
the actual watchability decision. **Before binding a Play key to a scenario, check the Play seed by
itself.**

**2026-08-22 - "They keep dying" was a reproduction bug, not a mortality bug, and the death causes
proved it in one run.** Founder extinction under separated food and water was assumed for two
sessions to be a survival problem, and four calibration levers were spent on it. Tallying the actual
`DeathCause` values showed **zero dehydration deaths and 0.07 starvation deaths per run** - all four
founders died of age at tick ~2500 in every arm including the healthy one. The real cause was the
joint 70%-energy-AND-70%-hydration reproduction gate, satisfied 95% of the time with co-located
resources and 33.5% when food sits 7 units from water. **When a population fails, tally death causes
and check the reproduction eligibility window before tuning the environment.** Both numbers are
cheap; the four refuted calibration arms were not.

**2026-08-22 - A behaviour bug hid for weeks because the thing it changed was not in the hash.**
`PlantPatchStore.ReplaceAt` never reset plant age, so every takeover installed a seedling that died
on the incumbent's clock. No hash regression caught it: plant `Age` is absent from
`ComputeStateHash`, and the pinned regression runs a scenario with no plant competition. **A passing
hash regression proves only that the hashed fields did not move in the pinned scenario.** Before
trusting one, check that the field you changed is actually hashed and that some pinned scenario
actually exercises the path.

**2026-08-22 - Measure the blast radius before retracting anything.** The correctness fixes above
could in principle have invalidated a season of plant conclusions. A paired old-versus-fixed run in a
detached worktree at the pre-fix commit settled it in two probe runs: every competition-off
**trajectory** was identical - state hashes matched, and home-range, route ring and shifting patches
were cleared outright - while the statistics fix corrected at least one final death count without
changing any trajectory. Only the competition path moved. Retraction by assumption would have thrown away good evidence; clearing by
assumption would have kept bad evidence. **Run the paired sweep.**

**2026-08-22 - An experiment whose scenario was never committed cannot be re-audited.** The 168-site
low-occupancy geometry lived in a deleted throwaway probe, so when a correctness fix landed there was
no way to assess its impact on those conclusions. Promote any geometry a conclusion depends on into
committed scenario data.

**2026-08-22 - A "control" that does not disable the trait is not a control.** A revalidation sweep
labelled its competition-off arm `drift`. Turning site competition off disables no plant trait:
dispersal, seed investment and the rest keep acting, so `Dispersal` read *larger* there (t +26.7)
than with competition on (t +15.6). Read naively that says the project's strongest positive control
is not selected. It says nothing of the kind. **A drift control must disable the trait's channel** -
as the seed-production sweeps did with a charge of zero - not merely change the ecology.

**2026-08-22 - Compare the manipulation the conclusion is about, not the absolute delta.** Absolute
`SeedlingResilience` fell in every arm of the post-fix sweep, which looks like a refutation of the
establishment result. The establishment claim is about what *enabling the contest* does, and the
paired contest-on-minus-contest-off comparison replicates it closely: +0.0362, t +3.22, 72/120
against the recorded t +4.03, 76/120. Match the comparison to the claim before concluding anything.

**2026-08-22 - More sites is not the same lever as lower occupancy.** The 168-site replication
matched the original's site count exactly (6 active plus 162 inactive) and produced occupancy
**0.84, not 0.32**, while the 24-site arm in the same sweep reproduced its recorded occupancy to
within 0.006. Site count was never the mechanism; target *spacing* is. A dense regular lattice
saturates. Any future attempt should spread targets far apart rather than add more of them.

**2026-08-22 - Plant site occupancy is a cliff in target spacing, not a gradient.** Holding the six
active sites, the config and the 162-target count fixed and varying only the lattice span: spacing 4
gives occupancy 0.833, spacing 8 gives 0.528, spacing 9.5 gives 0.311, spacing 11 gives 0.085 with
3/10 seeds extinct, and spacing 13.3 collapses the ecosystem (9/10 extinct, 3 plant generations).
The window reproducing the recorded 0.32 operating point with clean survival is roughly spacing
9.3-9.7 - about four percent of the swept range. **`DispersalRange` is `4 + 20 * Dispersal` and
Dispersal evolves upward, so a mature patch throws seeds 14-24 units; any lattice tighter than that
saturates.** Do not guess a spacing; sweep it and read occupancy before drawing any conclusion.

**2026-08-22 - "Disabling the flag" is not always a clean control either.** The low-occupancy
adjudication used `plantMortalityEnabled: false` as the matched control for the lifespan-headroom
claim, on the reasoning that lifespan has no channel without mortality. True, and useless: the same
comparison moved `Dispersal` +0.0834 (t +21.40, 118/120), `NutrientUptake` -0.0466 (t -7.62) and
`WaterEfficiency` -0.0445 (t -8.61). Turning mortality off removes site turnover and rewrites the
whole selective regime. **Check what else your control disables before reading its numbers** - a
control is only clean if the manipulation is the *only* thing that changes.

**2026-08-22 - Some conclusions are not recoverable, and saying so is the result.** The three
low-occupancy plant conclusions cannot be audited. Their scenario was never committed; the
occupancy condition was reproduced by calibration, but only by placing free sites outside the +/-25
creature arena, where nothing grazes them (`RealizedGrazingPressure` 0.0026). Free sites no herbivore
can reach are not the resource those claims are about. Putting 162 targets at non-saturating spacing
*inside* the arena is geometrically impossible. **If the original achieved it in-arena, its layout
was something a lattice cannot express, and it is gone.** The honest next move is to re-derive the
underlying question as a new experiment with a committed scenario, not to keep auditing an
unrecoverable one.

## 6. Standing project facts

- **P5 ancestry-aware cluster history is analysis-only.** Each segment is scoped to its
  explicit clustering threshold and immutable snapshot provenance; confirmation requires the one
  ancestry source bound to that segment to be complete through the observation. This does not
  alter simulation state or hashes. A host-triggered session and an eight-row Unity evidence panel
  are built; durable chunk storage, a graphical evolutionary tree, and continuity filtering remain
  unbuilt. `GeneticClusterHistory.cs` is ~1,310 lines and should receive a separately approved,
  behavior-preserving decomposition before substantial new classification logic is added.
- Two decision policies exist: `Legacy` and `IntentUtilityV1`. **Every P4
  scenario, `CreatePrototype4Defaults`, and both playtest hotkeys use
  `IntentUtilityV1`.** Anything gated on `Legacy` is inert for P4. This asymmetry
  is the source of most half-wired mechanisms — check both paths.
- Arena is hard-coded `(-25, 25)` on both axes in `SimulationWorld`.
- New behavior goes behind a `SimulationConfig` bool defaulting `false`, added as
  the constructor's last optional parameter with its property immediately after
  the previous flag's. Flag-off must be byte-identical, proven by a hash
  regression on the `PredationVariation`/`Legacy` scenario. Scenario-data changes
  are not behavior changes and need no flag.
- `PlantMortalityEnabled` defaults `false`. Any ecosystem experiment must enable
  it explicitly or plants freeze at generation 2.
- `CreateFullEcosystemDefaults` turns every P4 mechanism on at once. It is a
  **liveness and integration** configuration, not an experimental one: every flag
  moves together, so any difference it produces is unattributable. Use it to pin
  liveness against the widest surface; run experiment arms against
  `CreatePrototype4Defaults` varying one flag.
- `PlantQualityPreferenceEnabled` defaults `false`. With it off, `ComputeNeedGain`
  saturates at its `Math.Min(1f, ..)` clamp for every active patch, so patch
  quality is invisible to `IntentUtilityV1` foraging and grazing is uniform.
- **Seed plant founders with *varying* defense when testing selection.** A uniform
  founder value gives zero standing variance and every result is drift. See
  `docs/experiments/p4-defense-selection-demonstrated-2026-08-18.md`.
- **Growth-rate traits are the wrong place to look for plant selection.** `growth` is gated by
  `(1 - Biomass/Capacity)`, measured mean **0.1711**. Everything through
  `PlantPhenotype.GrowthRateMultiplier` measures null or weak even at the 168-site operating point;
  all six dedicated rate traits were re-audited there. `Growth` is the exception only because it
  also controls lifespan and moves downward when mortality has headroom. `SeedInvestmentFraction`
  and `DispersalRange` skip the gate and are the strong positive controls. Before wiring a new plant
  trait, check which side of that gate it lands on.
- **`ElevationFieldEnabled` defaults `false`** and is **inert under the standard P4 plant
  config**. It needs `maximumPopulation: 1000` *and* `PlantFertilityAdaptationEnabled` together
  before it changes anything — grazing has to pull patches off capacity, and fertility has to
  stop binding the `Min`. Requires `ProceduralEnvironmentFieldsEnabled`.
- **`PlantFertilityAdaptationEnabled` defaults `false`** and gates `NutrientUptake`'s growth
  charge as well as its benefit, so flag-off is byte-identical to the world before the gene
  existed. The constructor's optional positional parameters are the shape that once silently
  dropped `persistence`, so
  `EveryPlantTraitTransmitsThroughCloneMutated` pins it.
- **All three non-growth-rate plant routes are operating-point dependent.** At the 24-site,
  ~91%-occupied calibration, establishment is selected (`SeedlingResilience`, t +4.03, 76/120 up),
  while lifespan and seed production lack free-site headroom. At the 168-site, ~27-33%-occupied
  count-plus-geometry condition, lifespan comes alive (`Growth` moves down: t -2.65, 46/120 up),
  `SeedProductionRate` becomes selected (t +4.32, 79/120 up versus 66/120 drift), and incumbent
  `SeedlingResilience` reverses (t -2.56, 44/120 up). Do not call occupancy the sole mediator: the
  168-site scenario also changes geometry. Keep 24 sites as calibration and 168 as an explicit
  experimental operating point until the human resolves the design question.
- **The liveness suite is the slow half and it grows with every flag and gene.** `dotnet test` is
  dominated by simulation-per-gene/flag fixtures, and a single test —
  `LivenessTests.RiskAversionIsLiveOnlyWhenThreatsExist` — is **16 s** on its own, more than
  several preceding tests combined on the reference machine. `FlagLivenessAnalysis` runs a
  simulation pair per config flag, so the cost rises every time a flag is added. In a throttled
  container this reads as a hung runner. Build once, run the non-liveness shard, PlantLiveness,
  liveness excluding RiskAversion, then RiskAversion alone with a generous timeout.
- **`PlantEstablishmentContestEnabled` stays OUT of `CreatePrototype4Defaults`** (decided
  2026-08-20). That factory sets exactly one plant flag, `plantCohortsEnabled`; every other plant
  mechanism is opted into explicitly at each experiment's call site. Defaulting the contest on
  would make it the only plant mechanism ever enabled by the factory and would invalidate every
  P4 baseline on record.
- **`PlantSeedProductionRateEnabled` defaults `false` and is kept as the project's live NEGATIVE
  control.** It reaches behaviour and demonstrably is not selected, which `Genome.NeutralMarker`
  cannot supply because that one is unwired. `PlantGenome.TraitCount` is now **11**. Its dispersal
  charge is a `float` config value, not a bool, so `FlagLivenessAnalysis` does not cover it —
  perturb it by hand if you need a verdict on the charge.
- **Site abundance is the mediator for `SeedProductionRate`** (2026-08-20): at 24 sites,
  occupancy is 0.904-0.908 and the enabled arm is null (+0.01953, t +3.22, 68/120 up versus
  70/120 disabled drift). At 168 sites, occupancy falls to 0.322 and it becomes selected
  (+0.02022, t +4.32, 79/120 versus 66/120 drift), with no extinctions. The gene is thus a
  conditional positive, not a universal negative; see the site-abundance writeup.
- **The P4 plant-selection blocker is answered: the route is ESTABLISHMENT.**
  `PlantEstablishmentContestEnabled` (default `false`) lets a seedling below
  `VulnerabilityFraction` resist takeover with `PlantGenome.SeedlingResilience`, the tenth plant
  trait. It rises at t +4.03, 76/120 seeds up, 0/120 extinct, paying a real `DispersalRange`
  charge of 2. Requires `PlantSiteCompetitionEnabled`, which is what creates the contest. This
  is a 24-site conclusion and reverses at the 168-site operating point. See
  `docs/experiments/p4-establishment-contest-2026-08-20.md`.
- **Site competition is infanticide, not competition between established patches.** Only patches
  below `VulnerabilityFraction = .25f` can be taken over, and newborns start at 1.5-9% of
  capacity, so newborns are the only class it ever reaches. Without the contest flag it destroys
  **34% of every patch ever born** inside a median two seconds, and that binary is **51.9% of the
  variance** in per-patch lifetime offspring. Site occupancy is 91% of 24 sites, so reproduction
  is site-limited: realised lifespan explains only **2.4%** of offspring variance among survivors.
  `docs/experiments/p4-where-plant-fitness-is-decided-2026-08-20.md`.
- **The plant-selection positive control is `Dispersal`.** It moves +0.098 to +0.125 at
  t 14-17 with 105-115 of 120 seeds up, across four arms, at 0/120 extinctions —
  `docs/experiments/p4-plant-trait-selection-nonreplication-2026-08-19.md`. Read any plant-trait
  null against it. `SeedInvestment` is a weaker second (t 4.8-6.8). Both are the traits with no
  growth-rate cost term in `PlantPhenotype`.
- **`maximumPopulation: 48` is part of the P4 plant calibration**, not a default. The factory
  default is 1000, which collapses the calibration scenario entirely. Copy the config from
  `ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds`.
- `PlantDefenseDeterrenceEnabled` defaults `false`. With it off, plant defense
  protects no biomass and cannot be selected on at all — see
  `docs/experiments/p4-defense-no-gradient-2026-08-18.md`. Any coevolution run
  must enable it, and must report `SimulationStatistics.RealizedGrazingPressure`
  so a null can be read against the pressure that actually existed.
- `SimulationStatistics.RealizedGrazingPressure` and the `PlantBiomassSeconds` /
  `PlantPatchSeconds` integrals behind it are read-only accumulators, deliberately
  absent from `ComputeStateHash`. Same rule applies to anything in `Diagnostics/`.
- **Soft home-range affinity is CLOSED as a measured negative (2026-08-22). Do not tune it, do not
  reopen it.** `HomeRangeAffinityEnabled` stays default `false`; the implementation, its tests and
  key `R` stay in the tree because they are correct and flag-off is byte-identical. Measured across
  two experiments, five conditions, 240 fixed-seed runs. Shipped observation scenarios: route
  metric saturated at 1.0000 flag-off, delta +0.0000, +0.0001 at a 10x bonus, and the 10x arm cost
  2.7% food intake for no births. `ObservationRouteRing`, built so a route *can* exist and
  delivering **90.6% decision opportunity at 0.88 familiarity**: route repeatability **fell**
  -0.0345 (t -2.87, 8/30 up) while same-site re-entry **rose** +0.0594 (t +4.93, 26/30 up).
  Geometry was tested as the rescue hypothesis and was not the blocker. Spec and plan carry
  SUPERSEDED banners. Evidence: `docs/experiments/p4a-home-range-affinity-2026-08-22.md`,
  `docs/experiments/p4a-route-ring-home-range-2026-08-22.md`.
- **`Prototype4Scenarios.ObservationRouteRing` is the repository's only route-capable geometry.**
  Eight sites on a radius-8 ring alternating Food and Water: adjacent opposite-kind separation
  6.12, same-kind 11.31, founders at the centre, total capacity/regeneration matched to
  `ObservationStable`. Every site has two equidistant opposite-kind neighbours, so travel burden
  alone cannot break the choice. **It is a harsh survival condition, not a calibration: 11/30 and
  9/30 seeds go extinct** even at matched productivity, because splitting the same output across
  eight sites is harder to live on than two co-located pairs. Use it as the harness for
  clustered/changing-patch work; never as a survival baseline.
- **`Prototype4Scenarios.ObservationShiftingPatches` is the changing-map scenario, on Play key `V`
  at world seed 45.** Three clusters 23-28 apart, each with a permanent water site at its centre,
  two active food sites 7 units out, and four dormant food sites as dispersal targets. With
  `plantMortalityEnabled: true` it sustains ~29 patch deaths and ~33 establishments per 6,000-tick
  run at an equilibrium of 11.96 active food sites. **Its honest extinction rate is 6/30 and the key
  runs seed 45 because seed 42 is one of the six failures** - that is a demonstration choice, and
  every published statistic comes from the full 30-seed sweeps, never from seed 45.
- **Separated food and water does not kill creatures, it STERILISES them (explained 2026-08-22).**
  `ReproductionSystem.CanReproduce` requires energy AND hydration AND health each at or above 70% of
  that creature's own capacity. Adults satisfy that joint gate **95.0% of adult-ticks in
  `ObservationStable`** (co-located) and only **33.5% in `ObservationShiftingPatches`** and 56.8% in
  `ObservationRouteRing`. Two effects: the marginals collapse (energy above 0.7 falls from 95.1% to
  46.3%) and a genuine simultaneity penalty of 8.6-12.8 points sits on top, because a creature tops
  one need up while the other drains. **Nothing starves and nothing dehydrates**: all four founders
  die of age at tick ~2500 in every arm, and the minimum hydration fraction reached averages 0.445
  even in the worst arm. Extinction is failure to replace, not mortality.
  `docs/experiments/p4a-founder-mortality-2026-08-22.md`.
- **The P4a "optional juvenile local-area bias" item is NOT the fix for separated-resource
  extinction.** Juveniles are not the failing class and mortality is not the failure mode. A bias
  keeping young creatures near their birth area cannot raise the joint reproduction window and might
  lower it by keeping them beside whichever single resource they were born next to. Every refuted
  calibration lever is explained by the joint gate: founders on food are worse because that lowers
  the binding hydration marginal; mature founders do nothing because age was never the constraint;
  eight founders are worse because more grazers depress both marginals; 1.5x productivity does
  almost nothing because refilling faster does not make two needs peak together.
- **DECIDED 2026-08-22: the joint 70%/70% reproduction gate STAYS. Do not change
  `ReproductionSystem.CanReproduce`.** The user chose to treat reduced fertility while commuting
  between separated resources as real ecology. No re-baseline is needed and every result on record
  stands. The standing consequence: **a spatially separated scenario must be calibrated to be
  viable under the gate** - more productivity, more founders, or accepted extinction - rather than
  the gate being relaxed to suit the scenario. `ObservationStable`'s 0/30 extinct is the outlier,
  not the norm. Do not re-open this as a bug; it is a decided design property.
- **Plant dispersal does not need plant mortality, so "mortality off" is NOT a frozen map.** With
  mortality off, plants colonise every dormant site within ~20 ticks and stay there: 12
  activations, 0 deactivations, 17.37 of 18 sites active. Any experiment wanting a genuinely static
  resource map must declare no dormant sites at all, not merely leave mortality off. This
  mis-specified a control on 2026-08-22 and inflated a scenario's real productivity from a declared
  1200 food capacity to roughly 3600.
- **`SimulationWorld.CaptureStatistics()` is the correct way to read end-of-run statistics.** The
  `Statistics` property is refreshed only on statistics-cadence ticks (every 20 ticks at the P1
  schedule) and its constructor-time value predates any applied scenario. `ExperimentRunner` uses
  `CaptureStatistics`. Per-tick simulation code must not call it - it walks every creature.
- **Statistics are sampled after the tick's deaths are committed (since 2026-08-22).** Any figure
  taken from a pre-`9763374` run understates deaths at run boundaries and can report a surviving
  population on an extinction tick. Trajectories are unaffected: the audit found a seed whose death
  count changed while its state hash stayed identical.
- **Post-fix revalidation status of the plant corpus (2026-08-22).** Confirmed on fixed code with
  varying founders over 120 seeds: the `Dispersal` positive control (t +15.63, 110/120),
  `SeedInvestment` (t +7.10, 91/120), the establishment-contest manipulation (+0.0362, t +3.22,
  72/120 versus recorded t +4.03, 76/120), the 24-site `SeedProductionRate` null (43/120 up), and
  the whole plant lifetime decomposition. **Still un-auditable:** every 168-site low-occupancy
  conclusion and the six growth-rate nulls, because the committed replication geometry produces
  occupancy 0.84 rather than 0.32. Survival was clean throughout: 0/120 extinct, 0/120 frozen in all
  six combinations. `docs/experiments/p4-postfix-revalidation-2026-08-22.md`.
- **`AbundantSiteReplicationModerate` is calibrated to span 114 / spacing 9.5, measuring occupancy
  0.311 (recorded 0.322-0.332) at 0/10 extinct.** It is a replication *condition*, never a re-run:
  the original's target coordinates are unrecoverable. **Known ecological difference: the lattice
  spans +/-57 while the creature arena is hard-coded to +/-25, so outer patches are never grazed.**
  Any trait result from this scenario must report `RealizedGrazingPressure` and state that
  difference - it reproduces the recorded occupancy, not necessarily the recorded ecology.
- **The three low-occupancy plant conclusions are UNVERIFIABLE, not merely unverified (2026-08-22).**
  Measured at the calibrated replication condition (occupancy 0.28-0.35, 120 seeds, varying founders,
  disabled-channel controls): `SeedProductionRate` is null (+0.00424, t +0.72, 64/120 against a
  recorded +0.02022, t +4.32, 79/120); the `SeedlingResilience` reversal is not demonstrated
  (-0.00248, t -0.34, 53/120) though the +0.0362 advantage measured at 24 sites is abolished; the
  lifespan-headroom claim is not adjudicated because its only available control is confounded. The
  six growth-rate nulls hold. **None of this refutes the recorded results**, because the replication
  reproduces the recorded occupancy and not the recorded ecology - grazing pressure is 0.0026 with
  the free-site pool outside the grazed arena. Banners stay.
- **`PlantEstablishmentContestEnabled` costs 19/120 extinctions at low occupancy** against 4/120 in
  the base arm, where at 24 sites it cost nothing. Any future use of the contest at low site
  occupancy must report survival.
- Phase order is fixed: P4 (ecosystem) → P5 (species/history) → P6 (terrain
  generation). Terrain is last, deliberately.
- Subagents: dispatch on `model: sonnet` explicitly. Ask before using opus.
- `python3` is unavailable in this environment. Large heredocs sometimes fail;
  use the Write tool.
