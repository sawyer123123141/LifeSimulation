# Field Notes for the Lead Agent

> **This file was 1,752 lines** - a session-opening read of thousands of lines before any
> work began. The bulk now lives in `docs/field-notes/`, lifted whole by
> `tools/split_doc.py`: **nothing was summarised or rewritten**, so nothing can have been
> lost in the move. The index below says what is in each file.
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

## Sections held in separate files

Lifted whole, nothing rewritten. Read the one you need.

- [5. Lessons log](field-notes/5-lessons-log.md) — 535 lines. Append with a date. Keep each entry to what a future session must not repeat
- [6. Standing project facts](field-notes/6-standing-project-facts.md) — 953 lines. explicit clustering threshold and immutable snapshot provenance; confirmation requires the one

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
