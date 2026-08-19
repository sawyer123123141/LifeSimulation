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
| `Diagnostics/GeneLivenessAnalysis.cs` | **The authority on whether a gene reaches behavior.** Perturbation, not caller-search. | `Analyze`, `Report`, `GeneLivenessResult` |
| `Diagnostics/FlagLivenessAnalysis.cs` | **The authority on whether a config flag does anything.** Reflection over the constructor, so new flags are covered without anyone remembering. | `Analyze`, `Report`, `FlagLivenessResult` |
| `Diagnostics/LivenessRecorder.cs` | Runtime probe counters for code *paths*; covers the §4 "runs on empty data" class that perturbation cannot reach. Attach via `SimulationWorld.Liveness` (null by default); never touches any hash. | `LivenessProbe`, `RecordOutcome`, `IsInertlyExecuting` |

### Elsewhere

- `Assets/Scripts/Presentation/Prototype1Presenter.cs` — Unity view, playtest
  hotkeys (`N`, `E`), the creature inspector. `_world` is non-serialized;
  `EnsureInitialized()` guards domain reloads during Play mode.
- `Assets/Editor/PrototypeBatchEntry.cs` — batch experiment entry points.
- `Assets/Tests/EditMode/*.cs` — 368 tests. `ResourceExperimentTests.cs` holds
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
| `foragingEconomicsEnabled` | its consumers (`CommitmentBonus`, `ShouldAbandon`) are Legacy-only |
| `multiThreatPerceptionEnabled` | `IntentUtilityV1` carries its own inline threat handling |
| `kinRecognitionEnabled` | no reader on the `IntentUtilityV1` path |
| `learnedResourceQualityEnabled` | single reader is inside `DecideFromLearnedOutcomes`, the Legacy+Cognition path |

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

**2026-08-18 — A control that moves the trait downward is still a valid positive
control.** The blocker demanded proof the setup could detect selection on plant
defense before a null would mean anything. The demonstration came out negative
(defense is selected away), which serves exactly as well: the machinery is proven
live. Do not keep searching for an upward response to "prove" a pipeline works.

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
- `PlantDefenseDeterrenceEnabled` defaults `false`. With it off, plant defense
  protects no biomass and cannot be selected on at all — see
  `docs/experiments/p4-defense-no-gradient-2026-08-18.md`. Any coevolution run
  must enable it, and must report `SimulationStatistics.RealizedGrazingPressure`
  so a null can be read against the pressure that actually existed.
- `SimulationStatistics.RealizedGrazingPressure` and the `PlantBiomassSeconds` /
  `PlantPatchSeconds` integrals behind it are read-only accumulators, deliberately
  absent from `ComputeStateHash`. Same rule applies to anything in `Diagnostics/`.
- Phase order is fixed: P4 (ecosystem) → P5 (species/history) → P6 (terrain
  generation). Terrain is last, deliberately.
- Subagents: dispatch on `model: sonnet` explicitly. Ask before using opus.
- `python3` is unavailable in this environment. Large heredocs sometimes fail;
  use the Write tool.
