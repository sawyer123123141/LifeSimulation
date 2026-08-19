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

### Elsewhere

- `Assets/Scripts/Presentation/Prototype1Presenter.cs` — Unity view, playtest
  hotkeys (`N`, `E`), the creature inspector. `_world` is non-serialized;
  `EnsureInitialized()` guards domain reloads during Play mode.
- `Assets/Editor/PrototypeBatchEntry.cs` — batch experiment entry points.
- `Assets/Tests/EditMode/*.cs` — 354 tests. `ResourceExperimentTests.cs` holds
  the scenario/calibration tests; `CoreSimulationTests.cs` is the big one.
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
   mechanisms in this repo are written, tested, and never reached (§4).
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
Full evidence: `docs/experiments/halfway-wired-mechanism-audit-2026-08-17.md`.

**Never executes in production (as of 2026-08-17):**

| Mechanism | Note |
|---|---|
| `MemorySystem.ObservePlace` | Only writer of place-memory slots. Tests only. |
| `MemorySystem.TickPlaceMemoryDecay` | No production caller. |
| `DecisionSystem.PreferRememberedResource` | No production caller. |
| `PlantPatchState.SeedReserve` | Allocated, never written or read. |
| `Genome.Commitment` | Inherited, mutated, hashed, aggregated into statistics, exposed as an `ExperimentMetric` — and read by **zero** behavior code. `CommitmentBonus` takes `Persistence` instead. |

**Executes but always on empty data:** `TryScoreBestRememberedPlace`,
`RecordFailedPlaceSearch` — both depend on place-memory slots, which are never
populated.

**Unreachable under `IntentUtilityV1`** (the policy every P4 scenario uses):
`ForagingEconomics.CommitmentBonus` (Legacy foraging path only) and
`ForagingEconomics.ShouldAbandon` (`ForagingEconomicsEnabled && Legacy &&
!CognitionEnabled`). Consequence: **`Persistence` has no behavioral effect under
P4** even though its inheritance bug was fixed.

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

---

## 6. Standing project facts

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
- Phase order is fixed: P4 (ecosystem) → P5 (species/history) → P6 (terrain
  generation). Terrain is last, deliberately.
- Subagents: dispatch on `model: sonnet` explicitly. Ask before using opus.
- `python3` is unavailable in this environment. Large heredocs sometimes fail;
  use the Write tool.
