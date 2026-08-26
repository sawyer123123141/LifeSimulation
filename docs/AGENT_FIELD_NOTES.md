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
| `Core/SimulationWorld.cs` (844 lines, **plus `.Ticking` 643, `.Hashing` 427, `.Statistics` 193 partials**) | The tick loop and all system wiring. Everything connects here. | `Step`, `TickDecisions`, `TickMovement`, `TickNeeds`, `ComputeStateHash`, `GetMovementTarget`, `TryScoreBestRememberedPlace` |
| `Core/SimulationConfig.cs` | Every tuning constant and feature flag; the `CreatePrototypeNDefaults` factories | `CreatePrototype4Defaults`, `DecisionPolicyVersion`, `ComputeMemorySlotCount` |
| `Core/SimulationTypes.cs` | All state structs and enums | `MemoryState`, `PlaceMemory`, `CreatureNeeds`, `MovementState`, `RandomDomain`, `SimulationStatistics` |
| `Core/CreatureStore.cs` | Struct-of-arrays creature storage; swap-remove on death | `GetNeedsRefAt`, `GetMemoryRefAt`, `GetPlaceMemoryRefAt`, `TryGetIndex` |
| `Core/DeterministicRandom.cs` | **Never edit.** All randomness. | `Float01` |
| `Behavior/DecisionSystem.cs` (592 lines, **plus `.Scoring` 324 and `.Legacy` 130 partials**) | Both decision policies | `DecideIntentUtilityV1` (P4 uses this), `DecideFromLearnedOutcomes`, `Decide` (Legacy), `ScoreResourceCandidates`, `ScoreRememberedResource` |
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

### World generation — `Assets/Scripts/Simulation/World/`

Pure C# with **no UnityEngine types**, which is what lets `tools/TerrainProbe` compile and measure it
without Unity. Prototyped in Presentation, promoted in `8c82c77`.

| File | Responsibility |
|---|---|
| `PlanetTerrain.cs` | signed elevation, moisture, temperature. **Requires a `TerrainSettings` argument** - there is deliberately no ambient default here |
| `PlateStructure.cs` | tectonic plates; each sample carries **two** candidate neighbours and a crossfade weight |
| `TerrainSettings.cs` | every tunable, one object |

### Elsewhere

- `Assets/Scripts/Presentation/Prototype1Presenter.cs` (1033 lines, **plus `.Hud` 194,
  `.Terrain` 536 and `.Views` 180 partials**) — Unity view, playtest
  hotkeys (`B/D/F/P/C/T/G/M/E`, `5/6/7/9`, `N`, `H`), the creature inspector,
  and the P5 evidence panel. `_world` is non-serialized;
  `EnsureInitialized()` guards domain reloads during Play mode.
- `Assets/Scripts/Presentation/TerrainSettings.cs` — every terrain tunable, in one object.
  Pure C#, no UnityEngine types, so `tools/TerrainProbe` can compile the generator without Unity.
  `PlanetTerrain.Active` is the instance everything uses; `ResetSettings()` restores the shipped
  values.
- `Assets/Scripts/Presentation/TerrainTuningPanel.cs` — the `J` panel of live sliders over it.
- `tools/TerrainProbe/` — `dotnet run` field measurement with no Unity in the loop. Use this when
  the editor is open, since both editor instruments fail on the project lock.
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

**2026-08-22 - Measure the thing before optimising it, and be willing to decline.** Two P1
performance items, same discipline, opposite outcomes. Genetic distance was a real hot spot with a
number attached - a constant **240 bytes per pair**, 120 MB and 126 ms per observation at 1,000
creatures - and got fixed (1,151x less allocation). Resource allocation was benchmarked and
**declined**: its cost is O(requests x distinct resources) so crowding is the *cheap* case (1,000
requests on one resource = 16.9 us), and a full 12,000-tick run at the largest reachable population
takes 2.72 s end to end. Optimising it would have risked a deterministic path that decides who eats
when resources run short, for an unmeasurable gain. **A review calling something a hot spot is a
hypothesis; the benchmark is the result, including when it says do nothing.**

**2026-08-22 - Pin a scaling property, not a magic number.** The regression guard for the clustering
fix asserts that allocation grows under 8x when pairs grow 16x, rather than asserting a byte count.
A byte threshold rots the moment anything else in the path changes; the scaling assertion fails
exactly when someone reintroduces per-pair allocation, which is the thing worth catching.

**2026-08-22 - A single source of truth for trait order, or inheritance and the hash will drift.**
`Genome.WriteTraits(destination, offset)` now writes the fields and `ToTraits()` is a wrapper around
it. Trait order feeds `FromTraits`, inheritance and analysis; two hand-maintained copies of that
order is a silent-corruption bug waiting to happen. If a trait is added, there is one place to
change.

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
  reproduces the recorded occupancy and not the recorded ecology - grazing pressure is **0.00261 against
  0.00699 at 24 sites, a ratio of 0.373**, because the free-site pool sits outside the grazed arena.
  The matched 24-site arm reproduced its recorded occupancy (0.9139 against 0.904-0.908) at 0/120
  extinct, so the harness is faithful and the deficit is geometric. Banners stay.
- **`PlantEstablishmentContestEnabled` costs 19/120 extinctions at low occupancy** against 4/120 in
  the base arm, where at 24 sites it cost nothing. Any future use of the contest at low site
  occupancy must report survival.
- **Non-finite values are rejected at two boundaries (2026-08-22).** `SimulationConfig.Validate()`
  checks all ten float tuning values are finite, and `ResourceDefinition`'s constructor checks
  position, radius, amount, capacity, regeneration and nutrition multiplier. **Clamping is not a NaN
  filter** - `Math.Max(0f, Math.Min(1f, NaN))` is NaN - so a non-finite value that gets past a
  boundary propagates through every later operation and surfaces as an unreproducible run rather
  than as an error. Convention preserved: config constructs freely and `Validate()` is the gate
  (called by `SimulationWorld`'s constructor); scenario data has no deferred gate so it rejects at
  construction.
- **Every experiment CSV must carry a manifest (2026-08-22).** Use
  `ExperimentManifest.Describe(codeRevision, scenario, config, firstSeed, seedCount, ticks)` and
  `ExperimentCsv.Compose(manifest, header, rows)`; the composer **refuses** to write without one.
  The manifest records the schema version, a caller-supplied code revision, the scenario id, its
  **layout fingerprint**, resource count, seeds, ticks and all 26 behaviour flags plus the key
  numerics. A test uses reflection to assert every public bool on `SimulationConfig` appears, so a
  new flag that is not added to the manifest fails the build - a manifest that silently omits a
  flag is worse than none, because it looks complete.
  **`SimulationScenario.ComputeLayoutFingerprint()` is the part that matters most**: an identifier
  is not provenance, two scenarios can share a name and differ in geometry, and that is exactly how
  the 168-site condition was lost. `ExperimentManifest` is deliberately environment-free (no clock,
  no git call, no file system) because it lives in Simulation; the caller supplies the revision.
- **P5 clustering allocation is now linear in population, measured both before and after
  (2026-08-22).** `GeneticClusters.From` flattens every genome into one shared trait buffer once,
  then compares offsets into it via `GeneticDistance.Between(traits, offsetA, offsetB, count)`.
  Before: a constant **240 bytes per pair** (two `ToTraits` arrays), giving 187 KB at 40 creatures,
  4.8 MB at 200 and **120 MB / 126 ms at 1,000**. After: 4.3 KB, 21 KB and **104 KB / 50 ms** - a
  1,151x reduction and 2.5x faster at 1,000. `Genome.WriteTraits(destination, offset)` is the single
  source of trait order; `ToTraits()` is now the allocating wrapper around it. A test pins that
  allocation grows under 8x when pairs grow 16x, so a regression to per-pair allocation fails.
- **Resource allocation is NOT a hot spot, and its cost shape is the opposite of the obvious one
  (measured 2026-08-22).** `ResourceAllocationSystem.Resolve` costs
  **O(requests x distinct resources)**, not O(requests^2): the expensive branch runs once per
  distinct resource, so **crowding is the cheap case**. 1,000 requests on one resource resolve in
  16.9 us; the same 1,000 across 24 resources cost 165 us. End to end, a 12,000-tick run at the
  largest population a committed scenario reaches (523 creatures) takes **2.72 s total, 0.227
  ms/tick including every other system**. **Do not optimise this** - it would mean touching a
  deterministic path that decides who eats when resources run short, for an unmeasurable gain.
  Revisit only if populations in the thousands and site counts in the hundreds coincide; the fix is
  then to bucket requests by resource index in one pass.
  `docs/experiments/p1-resource-allocation-benchmark-2026-08-22.md`.
- Phase order is fixed: P4 (ecosystem) → P5 (species/history) → P6 (terrain
  generation). Terrain is last, deliberately.
- Subagents: dispatch on `model: sonnet` explicitly. Ask before using opus.
- `python3` is unavailable in this environment. Large heredocs sometimes fail;
  use the Write tool.
- **Three hashes, three jobs — never merge them (2026-08-22, implemented in `7343653`).**
  `ComputeStateHash` is **V1, frozen**: a historical identifier that many tests pin as literals. It
  is incomplete and that is now fine; never "complete" it and never recompute a recorded V1 value.
  `ComputeStateFingerprint` is **V2**: every future-determining field *plus* configuration, versioned
  so a field-set change is a new number rather than a silent redefinition, and valid **only at a
  settled step boundary** (it throws if deaths are queued). `ComputeBehaviorHash` answers a different
  question — did this gene or flag reach behavior — and **must never include configuration**:
  `FlagLivenessAnalysis` flips a flag and compares that hash, so folding config in would make every
  flag read live by definition and destroy the harness. If you are ever tempted to unify them, that
  interaction is the reason not to.
- **A fingerprint whose field set depends on a flag cannot do its job.** V1 hashes home-range state
  only when `HomeRangeAffinityEnabled` is on. V2 hashes it unconditionally. Two worlds differing in
  a flag would otherwise be compared on different fields, which is exactly the comparison a
  fingerprint exists to make valid.
- **Decide "should this hash cover more?" by measurement, not by argument.** Adding plant `Age` and
  `ReproductionCooldownRemaining` to `BehaviorHash` was the open question; the prediction (stated
  first) was that the inert set would not move, since all four inert flags are inert for a
  *reachability* reason, not a sensitivity one. Measured with the lines in and out: identical inert
  set, identical plant gene verdicts, 33 / 19 / 1 either way. Kept because it is strictly more
  sensitive at no cost. `BehaviorHash` is never pinned as a literal anywhere — only ever compared
  against itself — so extending it invalidates no baseline. Check that property before extending any
  hash.
- **Absence from every hash is why a real defect stayed invisible.** Plant `Age` was in no hash, so
  the `ReplaceAt` takeover-age bug passed every hash regression the project had. When a fix reveals
  that a field was unhashed, hashing it is part of the fix, not a follow-up.
- **A detector must not be keyed on the thing it is testing.** The takeover control keys on the
  lineage-parent change, not on the age reset — keying on age would key the detector on the very fix
  under test. Same lesson as the 2026-08-22 lifetime detector, now pinned in a committed test.
- **Assert the manipulation, or the test name is an overclaim.** A 2,000-tick equality test named
  "...AcrossBirthsDispersalDeathAndTakeover" passes just as happily in a run where no patch ever
  died. It now carries a positive control per named path. A green test that proves nothing happened
  is worse than no test, because it reads as evidence.
- **Guard drift with a pinned count, not with a comment.** `ComputeConfigurationHash` is enforced by
  two tests: every `bool` constructor parameter must move the hash, and `SimulationConfig`'s public
  property count is pinned (44 hashed of 46; `FixedDeltaTime` and `MaximumMemorySlots` are derived
  from inputs already covered). Adding a field without hashing it fails a test instead of quietly
  producing a fingerprint that no longer means what a baseline assumed.
- **Put per-entity observation OUTSIDE `SimulationWorld` (2026-08-22, `CreatureActionHistory`).**
  Anything that watches creatures — histories, timelines, per-entity diagnostics — should sample the
  world from outside rather than live inside it, following the `LivenessRecorder` precedent. Held
  inside, it is future-determining state by the letter of the fingerprint design and has to be
  re-argued every time a hash changes; held outside, the question never arises, it is absent from
  every hash by construction, and it is still fully deterministic and testable headlessly. Do not
  gate such a thing behind a `SimulationConfig` flag: a diagnostics flag must be behavior-inert to
  be correct, and `FlagLivenessAnalysis` then reports it inert and fails the known-inert assertion.
- **The V2 fingerprint's first real job is proving an observer is an observer.** Fingerprint a
  watched world and an unwatched world after N ticks and assert equality. That is a much stronger
  claim than "I did not intend to mutate anything", and it is one line. Pair it with an assertion
  that the observer actually recorded something, or the equality proves nothing.
- **Sample observers per simulated step, not per frame.** Frame-rate and the speed multiplier would
  otherwise change what the player sees, which makes an on-screen history non-reproducible and
  useless as evidence.
- **Show the need delta across an episode, not just the action.** "SeekFood 12.4s, food -6%" reads
  as a failed trip; "SeekFood" alone reads as normal behavior. The instantaneous inspector cannot
  show this, which is why the reproduction gate stayed invisible for so long.
- **A population pinned at its cap cannot measure survival (2026-08-22).** The 2026-08-21 rendezvous
  experiment reported "0/120 extinct, both arms 48/48" — and **all 240 runs ended at exactly 48**,
  zero variance. That is a ceiling, not a null. Before believing any survival result, check the
  spread of the outcome variable; if it has none, the experiment answered a different question than
  it claimed. The same check applies to any metric sitting against a clamp.
- **The population cap is load-bearing ecology in this project, not a guard rail.** Raising it does
  not free growth, it causes overshoot and collapse: extinct 0/8 at cap 72, 5/8 at 84, 8/8 at 96+
  (≈293 births then starvation). Pick an operating point where the outcome is partial — cap 84 —
  when the outcome under test *is* survival.
- **Normalise decision-tick counts by creature-ticks before calling them a manipulation check.** An
  arm whose population survives longer accumulates more of every count. Raw rendezvous births rose
  t +2.04; per creature-tick the same difference is t +1.24, and among seeds where both arms survived
  it is t +1.01. The raw number measured exposure, not fertility.
- **Use McNemar, not a proportion comparison, for paired binary outcomes.** Extinction 75/120 versus
  66/120 looks like a benefit; the discordant pairs are 26 versus 17, χ² 1.49, not significant. The
  aggregate hid the pairing that the design deliberately created.
- **"Wrong sign" and "right sign, no payoff" are different verdicts — record them differently.**
  Home-range affinity was closed because its effect ran backwards. Safety-gated rendezvous was closed
  because its effect is real, correctly signed and well powered (predation deaths t −4.64) but
  reaches no outcome that matters, since starvation rather than predation limits the population.
  Filing both as "measured negative" would lose the fact that one mechanism works and is waiting for
  a habitat that rewards it.
- **A mechanism that does not pay off may be limited by the scenario, not broken.** Before tuning a
  mechanism or building architecture around it, ask what actually limits the population. Saving 2.3
  creatures per run from predators changes nothing when the binding constraint is food.
- **Cap-pinning audit result: the pattern was systemic, the damage was not (2026-08-22).** Eleven
  CSVs and 4,080 runs have a zero-variance population or extinction column. Exactly **one**
  conclusion was a ceiling artefact — the rendezvous survival null. In the other ten, zero extinction
  was a *control against differential survival*, and that control is valid exactly as stated. **A
  zero-variance column invalidates a conclusion only if that column was the outcome under test.**
  Do not let a scary-looking scan turn into a blanket retraction; classify first.
- **"0/120 extinct" is not evidence of a healthy world.** Those populations are cap-stabilised, and
  removing the cap causes overshoot and collapse in both harnesses tested — predation config
  0/8 extinct at cap 48 but 8/8 at 96; herbivore plant config 0/6 at cap 48 **and still pinned at
  95.8 at cap 96**, then 5/6 at 200 and 6/6 at 600. Same cliff, different position. Check the second
  configuration before claiming a finding generalises.
- **The plant corpus is scoped to constant cap-saturated grazing.** Every plant trait result was
  measured with the herbivore population pinned at 48. That is a fine controlled variable, but
  whether the gradients survive a freely fluctuating herbivore population is untested — and raising
  the cap does not test it, because that yields a boom-and-collapse rather than a free-running
  population. It needs a habitat limited by carrying capacity instead of by a cap; none exists yet.
- **Terrain that reads as terrain needs separated scales, not more octaves (2026-08-23).** One band
  of fBm doing everything gives a uniform gravel field: every peak the same size, no continents, no
  plains. The structure that works is a hierarchy combined **multiplicatively** - a very-low-frequency
  continent mask deciding where land exists, ridged mountain belts modulated by that mask *and* by a
  second low-frequency belt mask so ranges cover only part of a continent, then small-amplitude
  detail. The belt mask being zero over most of a continent is what makes plains exist.
- **Never sample noise finer than the render resolution.** Octaves below the sample spacing do not
  add detail, they add aliasing. A globe drawn with 192 columns resolves frequencies to 192/4pi ~ 15;
  it was being handed a field whose finest octave carried ~4,000 features around the equator, and it
  rendered as television static. Derive octave count from the caller's resolution
  (`PlanetTerrain.OctavesUnder`), so the same generator gives a globe fewer octaves than a close-up.
- **When a field looks wrong, evaluate the formula on typical inputs before rewriting it.** Twice the
  structure read correctly while the numbers did not: land came out at 0.51 against a 0.38 waterline
  because the belt mask multiplying the relief term is zero over most of a continent; and patch
  relief reused the arena's 14 units for a 400-unit patch. Both rendered as a coloured plane, and
  neither was visible from reading the code.
- **A smoothing filter with a fixed tap count is an aliasing filter.** Averaging three samples spread
  over a radius undersamples harder as the radius grows - more smoothing produced more roughness.
  Sample density must scale with radius; blurring on the grid instead is correct at any radius and
  costs the same.
- **Prototype generation in Presentation before promoting it to Simulation.** `PlanetTerrain` lives
  in Presentation, so iterating on it moves no hash and needs no re-measure. Promotion is a
  deliberate step: flag defaulting false, prove flag-off byte-identical, then re-measure every result
  scoped to the old field. Do not shortcut it by pointing the renderer at a field the simulation does
  not use - visuals disagreeing with the simulation is the failure this project is built to avoid.
- **The 50-unit arena is too small for planet-scale terrain, and possibly for the ecology too.**
  Continents are ~500 units; the arena fits inside a fraction of one. The same smallness is the
  likeliest reason the population cap is load-bearing (2026-08-22 cap audit). Growing the arena is a
  terrain decision *and* the most plausible route to a carrying-capacity-limited habitat.
- **Read the reference implementation BEFORE writing the system (2026-08-23).** Fifteen rounds of
  first-principles tuning on terrain produced six wrong diagnoses; twenty minutes reading
  SebLague/Procedural-Planets from source produced the architectural answer. The user asked three
  times for research before it was done. Read the SOURCE, not only the explanation: the video gives
  intent, the source gives composition order, masks and clamps - which is where every defect was.
  `docs/reference-implementations.md` lists what is worth reading and what each maps to.
- **A bounded field with an interior threshold is a terrace generator.** Elevation as 0..1 with sea
  level at 0.38 inside it forces a clamp, the clamp forces a knee, and the threshold forces a branch
  at the waterline. Three slope discontinuities, and a terrace *is* a slope discontinuity. Signed
  displacement from the threshold has none of them, and the coast becomes the zero crossing rather
  than a tuning problem.
- **Any piecewise-constant lookup is a cliff in whatever it feeds.** A spherical Voronoi plate lookup
  stepped elevation by **0.825 between samples one unit apart against a median of 0.00093 - a ratio
  of 885**. Blending the two nearest cells took it to 0.0417. If a field reads a cell property,
  interpolate across the seam or accept a wall there.
- **Measure CONTINUITY, not just distribution.** Deciles, land fraction, biome counts and saturation
  were all identical whether the field was smooth or cliffed. The instrument that found the 885x step
  was adjacent-sample gradient along a line. Distribution statistics cannot see a discontinuity.
- **A render and a field statistic are blind to different things.** A statistic cannot see colour
  quantised per triangle, unlit faces, or z-fighting; a render cannot see a step discontinuity in a
  field. Build both, and expect each to catch what the other missed.
- **When a symptom is invariant under every change, stop changing things and run a splitting test.**
  Six terrain diagnoses were wrong and four changed nothing at all. Rendering the same mesh **unlit**
  separated shading from geometry in one image and should have been the first move, not the seventh.
- **Check units on any "maximum" constant.** `MaximumSlope = 0.55` in elevation-per-radian was a
  **3% grade** - I had capped terrain at "gently sloping field" and crushed every band above ~10
  cycles/radian to centimetres. Elevation 1.0 is 30 m and a radian is 500 m; the conversion has to be
  written down or the constant is meaningless.
- **A view-relative scale silently breaks physical quantities.** Height scale as a fraction of view
  width rendered the same ground **eight times flatter the closer you looked**. Thirty metres does
  not shrink because the camera moved: make physical scales constant and put artistic exaggeration
  in a separately named factor.
- **Terrain needs bands at the scale of the thing living on it.** Every band was planet-scale - the
  hill band is a 77 m wavelength, so **less than one hill spanned the 50 m arena** and terrain was
  flat everywhere a creature could walk. Add local bands, gate them on whether the view can resolve
  them, and slope-limit their amplitude rather than picking it.
- **Never sample noise finer than the render resolution, and cap amplitude by SLOPE not frequency.**
  Octaves below the sample spacing add aliasing, not detail - a globe drawn with 192 columns was fed
  a band carrying ~4,000 features around the equator and rendered as television static. Separately,
  doubling mesh resolution DOUBLED the stripe count without removing it, proving the binding limit
  was representable slope.
- **A capture that cannot reproduce the runtime is a second implementation, not an instrument.** The
  offline PNG tool and the live preview drifted - different resolution, different triangulation,
  water in one and not the other - so the diagnostics described a mesh nobody was looking at, and
  missed a reported bug entirely. One shared build path, and anything that differs must differ there
  visibly.
- **"Are these two views the same world?" is worth a marker, not an argument.** Tinting the vertices
  inside the flat views window on the globe refuted a confident explanation of mine in one image: the
  two views were showing different parts of the planet, which looks exactly like a level-of-detail
  difference and is not one.
- **Prototype generation in Presentation, promote deliberately.** `PlanetTerrain`, `PlateStructure`,
  `IcoSphere` and `TerrainMeshBuilder` all live in Presentation, so fifteen rounds of iteration moved
  no hash and needed no re-measure. Promotion into Simulation is a flag defaulting false, proof that
  flag-off is byte-identical, then re-measuring every result scoped to the old behaviour.
- **Transparency is not an alpha value.** A Unity Standard material stays opaque however low its
  alpha until it is switched to the transparent blend path explicitly. And transparent water was the
  wrong call anyway: it revealed the finite sea bed patch as a lighter rectangle mid-ocean, an
  artefact worse than the one it solved.

- **A slope ceiling is not an amplitude (2026-08-23).** `SlopeLimited` clips a band to the steepest
  slope the mesh can represent. Two bands whose chosen amplitudes were both **above** that ceiling
  were therefore both clipped to it and summed - measured median land grade **0.243** in the 200-unit
  view against **0.085** for the planet-scale bands alone. When a limiter is doing the choosing, the
  number written in the source is fiction. Check whether any amplitude is above its own limit before
  believing it.
- **"Bumpy at one zoom, fine at another" is a band-gating question, not a noise-quality one.** Patch
  resolution is fixed at 193 samples, so the resolvable frequency is set entirely by how wide the
  window is: **120.6** cycles/radian at 400 units, **241.2** at 200 units. A band at 150 is therefore
  absent from one view and present at full strength in the next. Before tuning noise, work out which
  bands each view can actually carry.
- **A hard resolution gate is a pop.** `if (maximumFrequency >= BandFrequency)` gives a band full
  amplitude the instant the camera crosses the threshold, so zooming changes the *character* of the
  ground rather than its detail - the world appears to be rebuilt rather than approached. Fade the
  band in across half an octave instead.
- **Build the instrument that does not need the editor.** Both terrain instruments are Unity menu
  items, and both fail while the editor holds the project lock - which is exactly when someone is
  looking at terrain and wants a number. `PlanetTerrain`, `PlateStructure` and `TerrainSettings` are
  pure C# by design, so `tools/TerrainProbe` compiles them directly and answers in two seconds with
  nothing closed. Keeping generation free of engine types is what made that possible; it is worth
  protecting.
- **An instrument must sample at the resolution of the thing it describes.** The flat-view statistics
  passed the *globe's* maximum frequency to every window, which silenced the two creature-scale bands
  completely. It was therefore reporting on a field that no flat view renders, and could not have
  seen the relief that turned out to be the complaint. Same failure as the capture-versus-runtime
  drift, one level down: the instrument had quietly become a description of something else.
- **Put the tunables behind sliders once a thing is judged by eye.** Terrain is judged against a
  one-metre creature at three zoom levels. That is not a judgement anything makes from source, and
  edit-recompile-look is why the previous round took fifteen passes. Mutable global settings are the
  right trade here - there is one terrain - as long as an explicit parameter survives for probes and
  tests, and a Reset restores the shipped values exactly.
- **A biome that exists globally and appears in no view is absent (2026-08-23).** Global counts said
  the planet had ice, tundra, desert, marsh and scrub. The flat views had been parked on one computed
  coastline at latitude -15 degrees for the whole of the terrain work, where the mix is 61% ocean and
  35% grassland and **contains no ice or tundra at all**. "Just green and some sand and water" was an
  accurate report about the view and said nothing about the generator. Walk the viewpoint before
  concluding anything about variety - at +75 degrees the same window is 79% ice.
- **Name the categories, do not count them.** The window statistics reported "biomes 5", which says
  a window is varied and cannot say whether any of the five is the one somebody reported never
  seeing. Counting distinct kinds is the cheapest possible summary and it hid a reachability problem
  for days.
- **A global statistic and a local view answer different questions, and the local one is what a
  person sees.** Ice at 0.116 of the surface is true and was no help; ice at 0.00 of the window in
  front of the user is the fact that mattered.
- **Ranking is a lookup, and a lookup is a cliff (2026-08-23).** Blending the shelf between the two
  nearest plates fixed the seam, and a wall survived it: boundary **kind and intensity belong to a
  pair of plates**, so they change discontinuously the moment a different plate becomes
  second-nearest - along a line through the cell interior, far from any seam, where the seam blend
  has already saturated to 1.000 and smooths nothing. Measured at latitude 48.7: elevation stepped
  **0.277 to 0.528 across 1.04 metres, a grade of 7.24 - an 82 degree wall** - with identical shelf
  and identical seam distance on both sides, reading Divergent on one and ContinentalCollision on the
  other. Carrying both candidate neighbours and crossfading on how close they are to swapping fixed
  it: max grade 7.24 to 2.80, medians and biome mix unchanged to within a point.
- **A median cannot see a wall.** Land grade median at that latitude was 0.069 - the smoothest window
  measured anywhere - while it contained an 82 degree cliff. Report a maximum alongside every median,
  and when the maximum is bad, print what the lookup was doing on each side of it rather than
  guessing. The first guess here was wrong (a suspected off-by-one in the seam smoothstep) and the
  measurement took two minutes.
- **Fix the discontinuity, not the symptom.** The crossfade only acts where the ranking is close to
  changing hands, so away from those lines the field is bit-for-bit what it was. That is why the
  biome mix survived a change to the plate machinery - worth aiming for deliberately, because a
  global retune would have invalidated every window measurement taken this session.
- **A round world is a display transform, not a spatial model (2026-08-23).** Creature positions stay
  two floats on a flat 50-unit square with Euclidean distances; `ArenaProjection` maps them onto a
  sphere *after* the tick. No hash moves, no flag, nothing re-measured. The trick that keeps the
  camera working is putting the planet's centre at `(0, -R, 0)`, so the arena's centre lands on the
  origin with its normal pointing up - there the mapping is the identity, and a rig written for flat
  ground with up = +Y needs a zoom ceiling and a far clip and nothing else. **Ask what has to be true
  for the existing code to keep working, and then arrange for it.**
- **Check whether two views are already the same shape before "fixing" the scale.** I predicted that
  drawing the planet at true radius would shrink the mountains. It does not: the preview's relief is
  a *fraction* of radius, so 0.06 at radius 60 is 3.6 units per elevation unit, and at radius 500 it
  is 30 - exactly what the arena uses. The globe and the ground were always the same shape at
  different sizes. A ratio that looks like a discrepancy may be the same number twice.
- **Curve the existing mesh, do not write a curved mesh builder.** The patch is remapped from the
  flat builder's output. A spherical copy would be a second implementation of the same geometry, and
  the last two times this project had those they drifted until the diagnostics described a mesh
  nobody was looking at.
- **Cache derived quantities in the coordinates they are asked about.** Ground heights are taken from
  the flat vertices before curving, because "how high is the ground at this arena position" is a
  question in simulation coordinates; reading it off curved vertices would fold the planet's radius
  into every creature's height.
- **Derive a shared limit, never pick it twice (2026-08-23).** When the simulation began reading
  terrain it had to sample at the frequency the arena mesh resolves - computed the same way
  `BuildPatch` computes it, not chosen to look similar. A blunter or sharper field means a creature
  climbing a bump nobody drew, and the join would be a lie in the small while looking correct in
  every screenshot.
- **Hold the output ranges when you replace the source of a field.** Moisture .15 to 1, temperature
  .20 to 1, fertility .20 to 1 were kept exactly when terrain took over from independent noise, so
  the coming re-measure reports the **shape** of the field changing rather than its scale. Rescaling
  at the same time would move every plant result for a reason that has nothing to do with terrain,
  and nothing could be attributed afterwards.
- **A mutable static is fine in Presentation and forbidden in Simulation.** The tuning panel's
  settings object was the right trade while generation was presentation-only. Moving generation into
  Simulation, the same static becomes behaviour outside `SimulationConfig` - invisible to the
  configuration hash, so two worlds with equal hashes could diverge and every fingerprint guarantee
  quietly stops holding. Promotion means **removing the ambient default**, not carrying it along.
- **Presentation has no instrument that means anything.** Four bugs shipped in a row on the spherical
  view, every one compiling cleanly and passing 503 tests: behaviour wired into a terrain path most
  scenarios never take, a `Camera.main` lookup that never resolves when the presenter builds its own
  camera, a rig with no yaw at all, and a focus that panning clamped and zooming did not. **Say
  plainly that a presentation feature is unverified**, name the two or three things most likely wrong,
  and let the human look - do not report it as working.
- **Use the reference the file already uses.** `Camera.main` only finds a camera tagged MainCamera;
  the rest of the presenter used `_simulationCamera`, the one it builds itself. The new method's null
  check swallowed the mismatch and the feature silently did nothing. When adding to an existing file,
  copy how its neighbours reach the same object.
- **One clamp, used by everything that moves the value.** Panning clamped the camera focus; zooming
  toward the cursor did not, so a shallow ray meeting the ground far away flung the focus out of the
  world in a single notch. Two writers, one invariant, one of them enforcing it.
- **A big file can be split without reading it (2026-08-23).** A scanner that tracks brace depth -
  ignoring braces inside strings and comments - lifts whole members with their doc comments into
  `partial class` files. Nothing is rewritten and no member changes class, so behaviour cannot change.
  `Prototype1Presenter` 1886 to 1033, `SimulationWorld` 2058 to 844, `DecisionSystem` 1021 to 592, in
  one pass each, compiling first time, **with none of the files entering context**. Simulation splits
  are additionally proved by 503 green tests including every pinned hash literal.
- **Oversized files cost usage on every unrelated search.** The presenter was 95 KB and every grep for
  one hook paid for all of it; two of this session's bugs came from wiring behaviour into paths inside
  it that had not been read. Split mechanically **before** working in a file that big, not after.
- **A mechanical split has a limit, and it should be reported rather than forced.**
  `GeneticClusterHistory` (1324 lines) has almost nothing at class indent - the bulk is nested types -
  so the pass finds nothing to move. That is a decomposition needing comprehension, which is a
  different and larger job. Say so instead of half-doing it.
- **A field can be correct, live, and still deliver nothing (2026-08-23).** Terrain-driven
  environment passed every equality test against the generator and changed the state hash in every
  seed, and moved no plant conclusion - because a 50-unit arena is 0.1 radian on a 500-unit planet,
  and continental climate is nearly constant across it. The terrain field is *more uniform* than the
  hand-written one it replaced (moisture sd 0.005 against 0.283 at seed 161). **Measure the field's
  variance over the window before concluding anything from a null result**, or "no difference" and
  "no signal" are indistinguishable.
- **Run the control arm yourself when the original instrument is gone.** The recorded plant corpus
  came from an uncommitted probe, so any difference against it is unattributable between the change
  and the harness. Running flat and terrain arms in one sweep makes the comparison internal - and the
  flat arm doubles as a fidelity check against the recorded numbers (`SeedProductionRate` reproduced
  43/120 up exactly). **Commit the instrument**, which is why `tools/PlantSweep` exists.
- **Check the scenario's constants, not just its flags.** The sweep first ran at
  `SimulationConfig`'s default `maximumPopulation` of 1,000 rather than the recorded 48, and the cap
  never bound: populations ran to 134 with extinctions in half the seeds. Every flag was right and
  the ecology was wrong.
- **A null result is only worth having once the signal exists (2026-08-23).** "Terrain changes no
  plant conclusion" meant nothing while the terrain field was flatter than the field it replaced.
  After a local band restored comparable spread (moisture sd .207 against .240), the same null became
  a finding: the corpus is robust to where its environment comes from. **Re-run the null after
  fixing the thing that made it uninformative** - do not bank the first one.
- **Regional signal plus local band, not one field doing both.** Terrain climate sets the arena's
  mean and a zero-centred band supplies within-arena variation. The band's strength is chosen to
  MATCH the previous field's spread, not to maximise it: past that point the regional value stops
  mattering and the join is decorative again.
- **A feature that is not visible has not landed (2026-08-23).** Rivers were carved correctly - the
  probe measured a 1 m channel across 2.4% of the window, and four tests passed - and the render
  showed nothing. Terrain relief runs to tens of metres, so a metre of cut is invisible at every zoom
  anyone uses. **Render it before believing it**; the fix was a colour, not a depth.
- **Walk a coarse field, carve a fine one.** A downhill walk at full detail stops in the first
  metre-wide hollow it meets. Rivers follow the shape of a continent, so the walk reads a deliberately
  blunt field (`WalkFrequency = 24`) and the channel is applied to the sharp one. The cost is that a
  river can sit slightly off the finest valley floor; the alternative is erosion, which is a different
  and much larger feature.
- **Store a path as segments, not as the points you sampled.** Distance-to-nearest-point rises between
  samples, so a channel measured that way scallops at the sampling frequency of its own path -
  measured at 0.999 / 0.848 / 0.999 along one straight reach. Point-to-segment is four extra lines and
  removes the artefact entirely.
- **Subtracting a feature is not the same as inscribing one (2026-08-23).** A river cut as a fixed
  slot keeps the hillside it crosses and reads as a groove scratched across a slope. Inscribing means
  the surrounding ground is **blended toward** the feature over a compact support, so the land slopes
  into it. The same distinction applies to any feature meant to belong to terrain rather than sit on
  it - roads, craters, lake beds.
- **A modifier that can raise ground will raise it somewhere you did not look.** Blending terrain
  toward a profile built from a coarser field filled every dip the coarse field could not see, and the
  river ended up on a raised bank. `min(profile, terrain)` is the whole fix. Ask of any elevation
  modifier: which sign is it allowed to have?
- **Discrete direction choices show up as right angles.** Steepest descent over eight compass points
  makes staircases wherever the ground is nearly flat across one step. Take the gradient from all the
  samples and carry momentum between steps - the standard particle-erosion trick - and the same walk
  produces curves.
- **Structure that must exist has to be generated deliberately.** Rivers spaced far enough apart not
  to corrugate the land never meet, so the network had zero confluences and read as parallel
  scratches. Tributaries are a second pass with their own spacing, kept only if they join. Hoping an
  emergent property emerges is not a plan.
- **Ask what a feature must DO before choosing how to build it (2026-08-23).** Rivers were written
  twice and reverted. The second attempt fixed every visual artefact by name - monotone profile,
  valley blend, cut-only modifier, momentum instead of compass directions, tributary pass, tapered
  heads - and still did not read as a river, because the acceptance test was never "is the geometry
  right". It was drain, erode, animate: three properties a pure function of one direction cannot have
  at any quality of tuning. **Three questions asked at the start would have saved both attempts.**
- **"Get it visible and see if it reads" is not always the cheap first step.** That was the recorded
  reasoning for doing painted rivers before real hydrology, and it cost two implementations to
  discover something derivable from the data structure. Prototype first when the *unknown* is
  perceptual; reason first when the unknown is structural.
- **When the user asks whether to wait for a prerequisite, answer about the GOAL, not the patch
  (2026-08-23).** Asked "if we should wait and revisit after something else is built, do that
  instead", the answer given was "this fix needs no prerequisite" - true of the valley blend, false of
  what was actually wanted, which was rivers that drain, erode and animate. Those need the chunk
  system, and the doc said so before either attempt started. **A question about sequencing is a
  question about the objective.**
- **A new control scheme has to be checked against the keys already bound (2026-08-24).** The free
  camera wanted WASD; `D` resets to the drought scenario, `E` to the starter habitat, `F` to food
  scarcity. Gating movement behind a held right mouse button - Unity's own scene-view idiom - kept
  both, and cost nothing. Grep the input handler before choosing keys, not after someone reports that
  flying left restarted the simulation.
- **Put the arithmetic where a test can reach it.** Every camera bug this project shipped was found
  by a human in Play mode, because the rules lived inside `LateUpdate` next to `Input`. The free
  camera's speed, height bounds and pitch limit are pure functions in `FreeCameraMotion` with no
  Unity types, so the headless project compiles them. **A MonoBehaviour is not a place to keep
  rules.**
- **Measure an artefact before fixing it (2026-08-24).** A sawtooth seam in the chunked planet looked
  exactly like an oversized skirt. Shrinking the skirt fourfold changed the render by zero bytes -
  the seam was a flat-shaded coastline quantised to the triangle grid, which is the art style. Two
  renders and a measurement cost less than the fix would have.
- **A cost model has to be calibrated against the thing it models.** `ApproximateLeafCount` predicted
  150 chunks; the renderer drew 908, because a chunk exists when its *parent* splits, not when it
  does. The test passed the whole time and asserted nothing true. **An estimator nobody has checked
  against a measurement is a comment with an assert in it.**
- **Check whether an open worry is still true before acting on it (2026-08-24).** `PatchLift` had been
  flagged for two sessions as "almost certainly too small". It was - against a backdrop that no longer
  exists. Once the backdrop had level of detail, the two surfaces measured identical and the item
  closed without a line of code. **A stale worry costs more than a stale fact, because it asks for
  work.**
- **An optimisation is a hypothesis until it is measured.** Doubling the chunk grid and dropping a
  tree level looked like a strict win - same finest triangle, fewer chunks, one fewer seam. It
  tripled the triangle count, because raising the band limit by a level makes the coarse chunks
  denser too and those are most of the sphere. Reverted, and the number is in the comment so nobody
  tries it twice.
- **A liveness test against the fingerprint proves nothing (2026-08-24).** `ComputeStateFingerprint`
  folds in `ComputeConfigurationHash`, so two worlds differing only by a flag have different
  fingerprints whether or not the flag does anything. The test passes, the flag could be a no-op, and
  the matching inertness test cannot be satisfied at all. **`ComputeBehaviorHash` is the config-free
  one** and is what "did these evolve the same way" means.
- **The project's own guards are the fastest reviewer.** Adding one flag failed three tests
  immediately: the manifest did not mention it, the configuration hash did not cover it, and the
  pinned property count disagreed. None of that needed a person to notice.
- **Carry a control column in every selection sweep (2026-08-24).** `CreatureSweep` reports
  `NeutralMarker`, a gene that responds to nothing by construction. In the four-seed smoke test it
  came out at t = 2.44 - the largest mover in the table - which said "this is drift" before anyone
  could read a story into the real columns. At 120 seeds it sat mid-pack, which is what made the null
  believable rather than merely quiet.
- **Check that the arms diverged at all before interpreting a null.** Half the slope-cost pairs were
  byte-identical, because half the arenas are flat. That could have been dilution hiding an effect;
  restricting to the pairs that diverged doubled the means and left every t identical, which settles
  it. Two lines of analysis, no rerun.
- **A note saying "this cannot be done mechanically" deserves the same suspicion as any other stale
  worry (2026-08-24).** `GeneticClusterHistory` sat at 1324 lines across two sessions because the
  handoff said its members were not at class indent and the bulk was nested types. Neither was true -
  the nested types were the last 55 lines. Thirty seconds of looking beat two sessions of trusting
  the note. Same shape as the `PatchLift` closure.
- **When every column moves together, including the control, it is composition and not effect
  (2026-08-24).** The focused slope run moved all fourteen gene means by about the same amount at
  |t| around 1.4 to 2.0, two of them past 2. `NeutralMarker` moved with them - and it cannot respond
  to anything - so the cause was differential extinction changing who was left to average, not
  selection. **Without the control column that table reads as a finding.**
- **Removing a ceiling can replace one problem with a worse one.** Raising the population cap from 48
  to 200 gave survival room to move, and also let populations overshoot and crash: 33 of 60 pairs
  went extinct in both arms, against 2 or 3 in 120 at the old cap. The metric that was saturated
  became the metric that is now mostly zero. Change a limit by a step, not by a factor of four.
- **Do not report a comparison conditioned on a post-treatment variable as if it were evidence.**
  Population among the pairs where both arms survived came out at t = -2.27, which looks like the
  clearest number in the run. Survival is what the treatment affects, so conditioning on it selects
  the sample. It is in the write-up marked as not to be used.
- **"Looks high" is a question about distribution, not about a total (2026-08-24).** Ice cover was on
  the open list as too heavy. The total was never the deciding quantity: 3% of a surface is Earth-like
  and 7% is plausible, while ice on every tropical mountain reads wrong at any total. Measuring
  *where* it was - 98.31% beyond 60 degrees - closed the item without touching a coefficient. Third
  stale worry closed by measurement in one session, after `PatchLift` and the claim that
  `GeneticClusterHistory` could not be split.
- **Measure the control's own noise floor before calling a result significant (2026-08-24).** The
  cap-100 slope run put `occupied_slope` at sign z = 2.41 - but the `NeutralMarker` control, which
  cannot respond to anything, came in at z = -1.64 on the same test. The floor is not zero. A result
  is only as impressive as its distance from what the control does, and reporting 2.41 without 1.64
  beside it would have been the more flattering half of the truth.
- **A behavioural readout beats a genetic one for showing a mechanism works.** Where an animal is
  standing responds within a lifetime; a gene needs selection, generations and a strong enough
  pressure. The slope cost moved `occupied_slope` and no gene at all, which is the coherent result -
  and it was measurable with no new simulation state, from positions that already existed.
- **A paired arm-against-arm design cannot see selection (2026-08-24).** Three creature sweeps
  reported flat gene tables, and every one of them was blind to the question by construction: a trait
  under strong selection in both arms cancels exactly. Asked the other way - drift from founders
  against a neutral control - two traits moved by a quarter of their range at t = 11 and t = 7.9.
  **Check what a design is capable of detecting before reading its null as an absence.**
- **Print the baseline, not just the change.** Drift toward the centre of a bounded range is what
  symmetric mutation does on its own, so a shift is only selection if you know where the gene
  started. Every gene here began at 0.50, which is what rules the artefact out - and it is the exact
  failure that got `p4-defense-selection-demonstrated` retracted on the day it was written.
- **A dead run has no gene means, and including it corrupts every column (2026-08-24).** Extinct
  worlds report every gene as zero, so their "drift" is minus the founder value on all thirteen
  columns at once. With them in, the lean scarcity arm showed everything down ~0.21 with the control
  moving identically and every ratio at 1.0. **Exclude them - and then say out loud that doing so
  conditions on survival**, which the treatment affects, so magnitudes stop being comparable between
  conditions even though directions remain sound.
- **Vary one thing by deriving it, not by swapping in a neighbour.** A "scarcity" arm built by
  substituting a scenario from another family killed 30 of 30 runs in both arms - those layouts are
  calibrated against different founder counts and flags. `SimulationScenario.Scaled(id, factor)`
  multiplies the amounts of the *calibrated* layout instead, so scarcity differs from abundance in
  exactly one respect and a difference is attributable.
- **Check the mechanism in the source before predicting or denying an outcome.** "Do creatures shrink
  when food is scarce" was answerable by reading three lines: mass is `0.6 * 4^BodySize`, it is
  charged against energy per distance and water per second, and nothing pays for being large. The
  prediction followed from the code, and the measurement then confirmed it with a dose-response.
- **Trace the call path before testing a causal story (2026-08-24).** The queued explanation for the
  strongest selection in the model was "the terrain join introduced the temperature field", and one
  grep killed it: creature thermoregulation reads `TemperatureField.Sample`, a fixed sine, while the
  join builds the `EnvironmentField` that feeds plants. Both arms of that experiment would have been
  the same experiment. **A hypothesis about a mechanism is checkable against the wiring for free, and
  the run only becomes worth doing once the wiring permits the answer.**
- **An endpoint hides the shape; checkpoints show it.** Every creature measurement until now read the
  gene means once, at the end. Sampled twelve times instead, temperature tolerance moved 0.28 in the
  first 8,000 ticks and **0.004 in the last 4,000** - a plateau, which is a completely different
  claim from a drift and points straight at a saturating mechanism. Endpoint drift and trajectory are
  different instruments; a flat tail is invisible to the first.
- **Derive the equilibrium, then check the number.** Tolerance is `2 + 8*gene` against a field
  bounded at 8 degrees, so the benefit runs out at gene 0.75 exactly and the residual cost is ~1% of
  upkeep. Predicted 0.750; measured plateau 0.7475 with the join off and 0.7790 at 40 seeds with it
  on. **An arithmetic prediction that lands is worth more than a larger sample of an unexplained
  effect** - and the slight overshoot is itself informative, because an asymmetric landscape (health
  loss below, 1% upkeep above) rests its mean above the point where the benefit ended.
- **Measure the environment the animals actually experienced, not the one on paper.** Sampling
  `|T - 20|` under every living creature is what fixed the ceiling at 8.000 and turned "0.75 in
  principle" into "0.75 in this world". It needed no new simulation state - positions and the field
  were both already there. Same trick as `occupied_slope`.
- **The strongest signal in a model can be an artefact of a placeholder.** Temperature tolerance
  dominates selection because it is adapting to a decorative sine with no seasons, altitude, latitude
  or terrain. The gene is fine. **When a result is much larger than everything around it, ask what it
  is responding to before asking why it is strong.**
- **"No detectable selection" is a claim about n, and it expires (2026-08-24).** Forty seeds put ten
  of thirteen traits in the undetected bucket. Eighty seeds pulled three of them out, each consistent
  across three resource levels against a control under |t| = 1.13. **Before recording a null, record
  the sample size beside it** - and prefer "not here" to "inert", which is the phrasing that made
  this correction cheap rather than embarrassing.
- **A sweep run for one question often answers another for free.** The dose-response sweeps were run
  for body size and happened to contain the decisive test of the thermal ceiling: three resource
  levels, survival from 79 worlds down to 12, and the temperature-tolerance *endpoint* moving 0.02
  while the drift moved 0.07. **Read the whole table of a run you already paid for** before designing
  the next one.
- **A placeholder can be replaced without re-baselining anything (2026-08-24).** Creature temperature
  moved from a fixed sine to the world's own climate field, and every recorded thermal result still
  reproduces, because a `default ClimateField` **is** the sine. Making the neutral value of a new type
  the old behaviour is cheaper than a branch at every call site and impossible to get half-right - one
  test comparing `default` against `TemperatureField` over the arena pins it.
- **Write the prediction down even when it is wrong, especially then.** The prediction was that a
  terrain-driven temperature would be nearly uniform over a 50-unit arena and so produce smaller
  deviations. **Deviations barely moved** - mean 3.92 against 4.28, the full 8-degree span reached in
  both. What changed was between worlds, not within one: the endpoint's standard deviation across 40
  worlds went 0.0744 to 0.1454. Having the wrong prediction on the page is what made the right
  distinction obvious.
- **Look at the spread across runs, not only the mean of them.** Both arms above have a perfectly
  reasonable mean and they are completely different environments. A uniform field applies the same
  pressure to every world; a real one gives one arena a temperate continent and another a cold one,
  and only the variance shows it.
- **Liveness is not the same as having a benefit (2026-08-24).** `GeneLivenessAnalysis` asks whether
  perturbing a gene changes the behaviour hash. A gene that only ever costs something changes the
  behaviour hash, so it passes - and `MetabolicPace` has been passing while raising two drains 2.14x
  across its range and returning nothing. **A caller search finds the readers; only reading them
  tells you which side of the ledger they are on.**
- **A column that keeps crossing in one condition is a question about the mechanism, not about
  seeds.** `metabolic_pace` crossed |t| = 2 at lean and only at lean, twice. The reflex is more runs;
  the answer took one reader search and needed **no new simulation at all** - six corpora already on
  disk had the dose-response in them.
- **Build the flag to answer the question, then let it say no (2026-08-24).** `MetabolicPace` was a
  pure cost, so it got the obvious benefit behind a flag and a written prediction of a sign flip
  across the resource ladder. **There was no sign flip** - the bleed halved and stopped there. The
  flag, the tests and the corpus stay committed anyway, because the next person now starts from a
  measurement rather than from the same argument.
- **A benefit routed through a shared resource is diluted by everyone else having it.** Faster
  ingestion sounds like a private gain and is not: contested sites are divided between requesters, so
  every competitor getting faster partly cancels it. **When adding a trait benefit, ask whether the
  channel is private or common before predicting its sign.**
- **The control is what makes a row discardable rather than tempting.** The scarce condition produced
  `metabolic_pace` at t = -3.12, which looks like the strongest result in the run until you see
  `neutral_marker` at **t = +3.31** on 4 surviving worlds of 80. Without the control that row would
  have been written up.
- **A huge t with a tiny shift means mutation-limited, not weak (2026-08-24).** `UrgencyExponent`
  drifts -0.04 at t = -19.4, which reads as contradictory until you notice **founder is exactly
  0.5000 in every run**: four genes are monomorphic in the founder profile, so selection has nothing
  standing to act on and waits for mutation to supply it. **Read the founder column before calling a
  gradient weak** - the gradient and the response are different quantities.
- **The same defect looks different from the gene side and the system side.** "Grazing is uniform
  because `ComputeNeedGain` saturates" was already a comment in the source and a known plant-side
  problem. From the gene side it is why `UrgencyExponent` has no trade-off and is under the most
  reproducible selection in the model. **When a gene turns out to be monotone, the missing punishment
  is usually somewhere else and already known about.**
- **Nine corpora on disk answered a question that would have cost nine new sweeps.** Two genes were
  fully characterised tonight without running anything new. **Before designing a run, grep the
  corpora already committed for the column you care about.**
- **Ask which channel is open before building the instrument (2026-08-24).** Two hypotheses both
  predicted the `UrgencyExponent` sign - fertility via the reproduction gates, or survival via
  starvation. Counting causes of death cost **one run** and retired the survival one outright:
  starvation and dehydration were 15 of 5,619 deaths against 96.9% old age. The configuration value,
  hash bump and guard updates were then spent on the one surviving explanation instead of on both.
- **Byte-identical output is a bug report until proven otherwise.** A gate change produced two files
  identical to the baseline, which looked like a clean null. It was a wiring miss - the threshold
  reached `CanReproduce` but not `CanSeekMate`. **It was also a finding**: lowering only the lower
  gate does nothing, because the higher one binds first. Chase identity before writing it up.
- **A parameter you cannot vary is a hypothesis you cannot test.** The 0.7/0.8 literals had been
  stable for the life of the project and turned out to be the dominant selective channel in it -
  slackening them removed selection on five traits at once. **When a constant is load-bearing, make
  it a knob before arguing about it.**
- **Watch the control in every arm separately.** The slack-gate arm's control moved to t = 2.55 while
  the default arm's sat at 0.17. Same code, same seeds, different noise floor. A t of 2 does not mean
  the same thing in two arms of the same experiment.
- **A quantity that is only ever subtracted from is worth checking for (2026-08-24).** `Health` had
  five subtractions and no addition anywhere in the simulation, which is invisible to every test
  because nothing asserts a value can go up. It mattered because health gates reproduction, so the
  ratchet meant permanent sterility rather than injury. **Grep for the writes to a field, not the
  reads, and look at their signs.**
- **Two mechanisms found in the source tonight, two overstatements.** The `ComputeNeedGain` saturation
  was not the cause of the urgency drift, and the health ratchet explains only 19% of the thermal
  selection. **Finding a real mechanism in the code is not the same as finding the dominant one** -
  say "contributes" until the measurement says otherwise.
- **t-statistics are not comparable across arms with different survival.** The health-recovery arm has
  zero extinctions against one, and every column's |t| rose. None of that is attributable to healing;
  a cleaner arm has less composition noise. **Compare the column the run was designed to test, and say
  out loud that the rest moved for a different reason.**
- **A ratio against a near-zero control is a display artefact.** With the control at -0.0000 the "vs
  control" column printed 30,988x. Ratios need a denominator with a floor, or the column needs to be
  read as t.
- **A continuous cost needs a continuous benefit (2026-08-24).** `MetabolicPace` charges energy and
  water *every second* and both benefits tried pay only sometimes - ingestion while eating (and it is
  shared, so competitors cancel it), healing while injured (and mean health is 0.9939, so nearly
  never). Neither moved the gene. **Before designing a trade-off, check that the two sides are
  collected on the same schedule**, not just that they point in opposite directions.
- **Predicting a null and getting it is worth as much as predicting an effect.** The healing benefit
  was predicted to do nothing, for a stated reason - the channel is idle - and it did nothing. That
  turned a third failed attempt into the explanation for all three.
- **A flag that is inert for a benign reason belongs on the known-inert list with the reason.**
  `metabolicHealingEnabled` cannot act without `healthRecoveryEnabled`, the same shape as slope cost
  needing elevation. Pinning it there means the guard now fails if it ever becomes live *without*
  healing, which would mean something is healing creatures unasked.
- **Count the blast radius before a rename, and include the corpora (2026-08-24).** Renaming
  `MetabolicPace` looked cheap - 18 code files, one script. The real cost was **10 committed CSV
  corpora carrying `metabolic_pace` as a column header** plus 82 mentions across 38 docs: the rename
  would have severed every recorded result from the code it describes. **An authoritative doc comment
  at the definition captures the entire benefit of a rename at none of its cost**, whenever the
  problem is "this name misleads" rather than "this name is used wrongly".
- **"Adds realism" and "removes an artefact" deserve different defaults.** The slope cost and the
  terrain temperature add something a designer chose. Health recovery removes something nobody chose -
  a quantity that only decrements happened to gate a quantity that decides fitness. That distinction
  is the argument for which flags should eventually flip to default on, and it is worth recording per
  flag rather than rediscovering.
- **A threshold's effect can be a margin rather than a switch (2026-08-24).** The mate-seeking gate
  was predicted to bind or not bind, with a cliff where it crossed the population's natural energy
  level. There is no such level: **raising the gate raises the population's energy**, because
  creatures work harder to clear a higher bar. The margin above the gate ran 0.167 down to **0.006**
  across five values and selection intensity tracked it, accelerating. **Before predicting a
  threshold effect, ask whether the thing being crossed is itself free to move.**
- **Five points beat one, and the shape was the finding.** One alternative gate value said "the gate
  is the driver". Five said "and the default sits on the steepest part of the curve, where a small
  change to the parameter is a large change to how hard the model selects" - which is the part that
  matters for tuning and was invisible from a single comparison.
- **Two points make a slope out of anything (2026-08-24).** Comparing gate 0.70 against 0.45 said
  "five traits stop being selected". Five gate values said one trait does, one might, and three were
  noise whose default-gate values were marginal to begin with. **The extension cost a grep against
  corpora already on disk and deleted a claim that had been written up and committed.** When a result
  comes from exactly two conditions, the cheapest possible next thing is a third.
- **An instrument that varies everything together varies nothing (2026-08-24).**
  `SimulationScenario.Scaled` multiplies amount, capacity and regeneration by one factor, so the
  ratios between them - which are what set an ecology's dynamics - never move. Five resource levels
  from 0.40x to 1.00x all collapsed identically, and could not have done otherwise. **Before sweeping
  a knob, check that it changes a ratio and not just a unit.**
- **When a bound turns out to be load-bearing, say so.** The population cap was treated as a ceiling
  on a self-regulating ecology for the life of the project. 2.0x regeneration survives 23 of 24 runs
  at cap 250 and 3 of 20 at cap 500 - same ecology, same forage. **The cap was supplying the
  regulation, not bounding it.** Same shape as the reproduction gate turning out to be the dominant
  selective channel: the constants nobody questions are the ones doing the work.
- **Report the spread, not the mean, whenever a ceiling might be involved.** Eleven committed corpora
  and 4,080 runs have a population column with zero variance, and no summary statistic in them says
  so. **A carrying capacity produces a distribution; a cap produces a constant** - printing sd would
  have caught it years of runs earlier.
- **A step function where a curve belongs produces boom and bust (2026-08-24).** Reproduction was
  gated at 70% and 80% of three needs, so nothing told a population that resources were *tightening* -
  only that they were gone. Replacing the step with a graded cooldown took starvation from 55-64% of
  deaths to **exactly zero** and survival from 3 of 20 to 19 of 20. **When an ecology overshoots and
  crashes, look for the threshold that should have been a slope.**
- **Scale the cooldown, not the probability.** A graded breeding *chance* needs a random source inside
  a deterministic tick, which would have cost a seeded stream and a hash change. Scaling how long a
  creature waits gives the identical negative feedback with no randomness at all. **Check whether a
  rate can carry what a probability was wanted for.**
- **Measure headroom against the threshold that matters, not against zero.** A creature that cannot
  breed below 0.70 is not "half fed" at 0.50 - it is simply out. Normalising `(condition - gate) /
  (1 - gate)` puts the whole curve in the range where it can act; normalising against zero would leave
  the brake fully applied nearly everywhere.
- **A liveness test can only see a flag whose machinery has had time to run.** The graded-fertility
  liveness check failed at 600 ticks because `AdultAgeSeconds` is 20 seconds - 1,200 ticks - so
  nothing had bred once, let alone twice. **Before calling a flag inert, check the run is longer than
  the slowest thing it touches.**
