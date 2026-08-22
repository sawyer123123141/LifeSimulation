# Claude Handoff — 2026-08-22

This is the current lead-agent brief. It replaces stale status prose in older handoffs; historical
experiment documents remain authoritative for their raw numbers. Start with this file and use
`docs/AGENT_FIELD_NOTES.md` as the repository map and judgment ledger. Do not re-read the whole
repository.

## Start here

```text
Continue LifeSimulation from main. Expected head at handoff: f750345.

Read, in order:
1. docs/CLAUDE_HANDOFF_2026-08-22.md
2. docs/AGENT_FIELD_NOTES.md sections 1, 4, 5, and 6
3. docs/ROADMAP.md P4a and P5 only
4. the design/plan for the specific next task only

Do not treat docs/superpowers/plans as a backlog; it is an archive of work already planned and
usually already completed. The backlog is docs/ROADMAP.md. Check git log/status before acting.

The first next task is to make the already-implemented soft home-range affinity genuinely
Play-testable and measurable. Add a dedicated ordinary-key scenario (recommended R) that is a
matched copy of ObservationStable/5 except HomeRangeAffinityEnabled=true. Do not change 5, N,
factory defaults, or inert place memory. Then run a matched fixed-seed flag-off/on measurement
across stable/scarcity/migration conditions for route reuse, distance from the familiar centre,
food/water visits, survival, and births. The key design question is whether affinity creates
recognisable routes or merely makes one resource patch sticky. State numeric predictions before
running. If the manipulation does not actually change familiarity/route reuse, stop interpretation.

Also make the P5 panel hide routine ConfirmedContinuity rows by default and display a compact
"N routine continuities hidden" count. Keep continuity in the analytical history; this is a
presentation filter, not an analysis change. Bundle this small polish with the home-range
observability work rather than treating it as a phase milestone.

Use normal letter/number keys; the user does not have F-key keybinds. Work autonomously, question
premises, measure rather than narrate a mechanism story, narrate progress at useful intervals, and
ask only for a genuinely human design decision. Commit and push scoped finished work to main.
```

## Repository and working tree

- Workspace: `C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim`
- Branch: `main`, tracking `origin/main`.
- Expected handoff head: `f750345 docs: record Unity home-range compile`.
- Tracked tree was clean at handoff.
- There are many untracked Unity `.meta` files, `Assets/_Recovery/`, and
  `ProjectSettings/PackageManagerSettings.asset`. They pre-existed and/or are Unity-generated.
  Do not stage or delete them. Never use `git add -A`; add named files only.
- `graphify-out/graph.json` and a Graphify interpreter are not present. The durable substitute is
  this file plus `AGENT_FIELD_NOTES.md`. Do not spend usage rebuilding a whole-repo graph unless
  the user explicitly asks.
- `RTK.md` is referenced by the injected project instructions but is absent from this checkout.
  Follow `AGENTS.md` and report the missing include only if a task depends on it.

## How the user wants the work handled

- Keep moving without repeatedly checking back. Make ordinary implementation and sequencing
  decisions yourself.
- Doubt the request and your own design. A well-supported refusal is valued more than redundant or
  premise-invalid work. Two prior refusals were later verified correct.
- Do not silently stop after two minutes or after a tiny edit. If there is safe in-scope work left,
  continue. During long work, narrate what is running and why; silence reads as inactivity.
- Explain status and visible behavior in plain language. The user is interested in the simulation,
  not test-framework jargon.
- Bundle tiny polish with a meaningful goal. The user explicitly does not want a long detour for a
  cosmetic one-line issue while larger ecology work remains.
- When adding visible behavior, say exactly which key to press and what visible difference to look
  for. Use ordinary letters/numbers, not F5/F6-style keys.
- The user wants a realistic/watchable emergent simulation, not scripted lore. “Major ecology
  events” means analytical events such as population collapse, recovery, split, merge, and lineage
  extinction—not authored events like a fictional war.
- Direct scoped commits and pushes to `main` were standing-authorized. Report what was pushed.

## Honest phase status

The codebase has strong scientific foundations, but “P0–P4 completely finished” would overstate it.

- P0–P3: their core foundations are essentially built and tested. Some old README status prose is
  stale; use the product architecture and field notes instead.
- P4 ecosystem science: the core exit question is answered. Plant evolution has demonstrated
  selectable routes, operating-point effects are measured, and the principal nulls are understood.
- P4a watchable regional ecology: partially complete. Observation scenarios, resource-intent
  execution, two-parent mating machinery, a safety gate, action/need UI, and soft home-range code
  exist. Home-range has not been enabled in a Play-mode scenario or ecologically validated;
  clustered/changing resource patches and optional juvenile local bias remain; rendezvous is live
  but showed near-zero ecological effect in the tested capped habitat.
- P5 species/history: the analysis foundation and a basic evidence panel exist. Genetic distance,
  snapshots, deterministic clustering, ancestry completeness, conservative split/merge/continuity/
  extinction evidence, and a host-triggered Unity panel are implemented. Durable chunk storage,
  a graphical evolutionary tree, better panel filtering, and higher-level ecology-event summaries
  remain.
- P6 terrain/world scale: deliberately not started as the next default direction. Terrain comes
  after the regional ecology and history foundations are credible.

The shortest route to a noticeably better prototype is finishing P4a watchability and measuring
whether the new local-behavior mechanisms actually create recognisable movement patterns. Do not
jump to terrain to hide weak ecology.

## Architecture that must not drift

The permanent boundary is strict:

- `Assets/Scripts/Simulation/`: pure deterministic C#. It owns biology and world truth. No Unity
  types, GameObjects, clocks, tasks, threads, or environmental input.
- `Assets/Scripts/Presentation/`: Unity rendering, camera, input, and panels. It reads simulation
  state and owns host-triggered analysis sessions; it never owns biology.
- `Assets/Editor/`: batch/editor entry points.
- `Assets/Tests/EditMode/`: authoritative behavioral and determinism contracts.
- `tools/HeadlessTests/`: .NET mirror used for fast headless verification. Presentation is not
  compiled by this runner, which is why pure panel-session logic lives under Simulation/Analysis.

Simulation representation is struct-of-arrays, not one GameObject per creature. `SimulationWorld`
is the composition root and tick loop. Systems are mostly static pure operations. Stores own compact
state. Presentation creates views from state.

### High-value file map

Core:

- `Core/SimulationWorld.cs`: all production wiring, step order, decision/movement/needs/resource/
  reproduction integration, state hash, liveness sink. Large and load-bearing; make surgical edits.
- `Core/SimulationConfig.cs`: every flag/tuning constant and `CreatePrototypeNDefaults`. New
  behavior flags go at the final optional constructor position, default false. Audit every explicit
  constructor after adding one; optional omission caused the current home-range Play-mode gap.
- `Core/SimulationTypes.cs`: shared state structs/enums/events/statistics. Existing `RandomDomain`
  numeric values are frozen; only append a fresh number.
- `Core/CreatureStore.cs`: creature struct-of-arrays, swap-remove lifecycle, ref accessors.
- `Core/SimulationEventBuffer.cs`: bounded world event output.
- `Core/DeterministicRandom.cs`: frozen. Never edit.

Behavior/biology:

- `Behavior/DecisionSystem.cs`: Legacy and `IntentUtilityV1`. Every P4 scenario uses the latter.
  Resource candidate scoring and threat/mate gates live here.
- `Behavior/PerceptionSystem.cs`: uniform-grid nearest/resource/creature queries.
- `Behavior/MemorySystem.cs`: scalar learned outcomes plus deliberately inert place memory. Never
  wire `ObservePlace` or place decay as a shortcut for home-range.
- `Behavior/HomeRangeSystem.cs`: deterministic centre/familiarity updates, decay, bounded bonus.
- `Behavior/ForagingEconomics.cs`: Legacy-only commitment/give-up pieces plus shared economics.
- `Behavior/PredationSystem.cs`: threat intensity and attack logic.
- `Biology/GenomePhenotype.cs`: 24 animal genes and phenotype derivation.
- `Biology/GenomeInheritance.cs`: positional crossover/mutation. Every new gene needs constructor,
  inheritance, trait list, hash, aggregation, and distinct-value transmission coverage.
- `Biology/ReproductionSystem.cs`, `NeedsSystem.cs`, `JuvenileSystem.cs`: reproduction, physiology,
  juvenile rules.

Plants/environment/resources:

- `Environment/PlantGenome.cs`: 11 plant traits and phenotype/cost routing.
- `Environment/PlantGrowthSystem.cs`: logistic `(1 - Biomass/Capacity)` gate and environmental
  `Min` constraint. This explains the six growth-rate nulls.
- `Environment/PlantReproductionSystem.cs`: dispersal, free-site search, takeover/establishment.
- `Environment/PlantMortalitySystem.cs`: age death.
- `Environment/PlantSiteRegistry.cs`, `PlantPatchStore.cs`: site availability and plant SoA.
- `Environment/EnvironmentField.cs`, `EnvironmentNoise.cs`: procedural fields.
- `Environment/TemperatureField.cs`: frozen P3 evidence; never edit.
- `Resources/ResourceStore.cs`, `ResourceAllocationSystem.cs`: visible consumable resources and
  deterministic intake allocation.

Experiments/diagnostics:

- `Experiments/SimulationScenario.cs`: all scenario data, including Stable/Scarcity/Migration/
  Mating and the 24-site calibration. Add scenario data here; scenario data needs no behavior flag.
- `Experiments/ExperimentRunner.cs`: headless runs.
- `Experiments/PairedExperimentAnalysis.cs`: paired statistics.
- `Diagnostics/GeneLivenessAnalysis.cs`, `PlantGeneLivenessAnalysis.cs`,
  `FlagLivenessAnalysis.cs`: perturbation-based authority on whether a value reaches behavior.
- `Diagnostics/LivenessRecorder.cs`: optional hash-excluded path probes for code that executes on
  empty data. Caller search is not a substitute.

P5 analysis:

- `Analysis/PopulationGenomeSnapshot.cs`: immutable capture with full/sample provenance
  (`IsSampled`, source count, sample limit).
- `Analysis/GeneticDistance.cs`, `GeneticClusters.cs`, `GeneticClusterSensitivity.cs`: distance,
  deterministic threshold clustering, sensitivity analysis.
- `Analysis/GeneticClusterObservation.cs`: binds exact snapshot, threshold, and clustering result;
  callers cannot accidentally compare mismatched inputs.
- `Analysis/AncestryHistory.cs`: founders, births/deaths, completeness watermark, and permanent
  incompleteness after overflow/discontinuity. Reads host event buffers and never clears them.
- `Analysis/GeneticClusterRelation.cs`: direct identity then bounded two-parent ancestry support,
  with counts/fractions and explicit incomplete coverage.
- `Analysis/ClusterHistoryPolicy.cs`: minimum support counts/fractions, ancestry depth, confirmation
  and absence windows.
- `Analysis/GeneticClusterHistory.cs`: conservative tracks/events. It is ~1,310 lines and known
  technical debt. Do not casually extend it; plan a behavior-preserving decomposition separately.
- `Analysis/P5HistoryPanelSession.cs`: fixed-cadence host bridge. It must advance after every world
  step and before the presenter clears events, including at 8x batched stepping. It surfaces missed
  cadence rather than silently fabricating completeness.
- `Presentation/Prototype1Presenter.cs`: owns the session and draws the panel. UI strings and
  formatting can allocate; simulation tick code cannot.

## Scientific conclusions worth carrying forward

### The plant operating point changes what evolves

At 24 sites, mean occupancy is about 0.90–0.91. At the 168-site experimental geometry it is
about 0.27–0.33. Both conditions survived in the reported 120-seed sweeps, but 168 changes both
count and geometry, so do not attribute effects to occupancy alone.

- `SeedProductionRate`: 24-site null, delta +0.01953, t +3.22, 68/120 up versus disabled drift
  70/120. At 168 sites it becomes selected, delta +0.02022, t +4.32, 79/120 versus drift 66/120.
- Lifespan: at high occupancy a live 2x genetic span converted to only R²=0.024 on offspring.
  At the low-occupancy operating point, `Growth` (which shortens lifespan) moves downward:
  -0.01131, t -2.65, only 46/120 up versus mortality-off drift +0.00450, t +2.25, 61/120 up.
  The prior “mortality has no headroom” statement is scenario-bound.
- `SeedlingResilience`: selected at 24 sites, t +4.03, 76/120 up at a real dispersal charge. At
  168 sites it reverses: -0.01184, t -2.56, 44/120 up versus contest-off drift +0.00245,
  68/120 up. Protection is useful when sites are scarce and costly when free sites abound.
- All six dedicated growth-rate traits remain null at 168 sites. `NutrientUptake` was checked in
  the route audit; Nutrition, Defense, WaterEfficiency, MoistureTolerance, and
  TemperatureTolerance were checked in the follow-up. None beat 62/120 up or |t|=1.0 against
  drift 66/120. The `(1 - Biomass/Capacity)` gate conclusion survives.
- The invader-side symmetric establishment term is a closed route. At 24 sites it weakened the
  incumbent signal from t +4.16/75 up to t +1.95/74 up; at 168 it made the negative selection
  stronger, t -3.46/41 up. Do not retry it without a materially different hypothesis.

The causal lesson at high occupancy: lifespan and seed cooldown both buy time, while free sites
are scarce. A ~95.8-second patch already spent 58.7 seconds mature, off cooldown, and unable to
find a site. Halving cooldown moved births only 203.7 to 221.8 and raised generations not at all.

The current design recommendation remains awaiting a human decision: keep 24 sites as the
reliability calibration and 168 as an explicit experimental operating point. Do not promote the
168-site rectangle to the default regional ecology merely because it makes more traits selectable.

### Positive and negative controls

- Dispersal is the robust plant positive control: roughly t +14 to +19.6 and 105–119/120 seeds up
  depending on the recorded arm, with survival intact.
- SeedInvestment is the weaker second positive control, roughly t +4.8 to +6.8.
- `SeedProductionRate` at 24 sites is the live negative/conditional control. Keep the gene; do
  not delete it merely because it is null under that operating point.
- `Genome.NeutralMarker` is a deliberately behavior-dead drift channel. Do not wire it.

### Rendezvous

`SafetyGatedMateRendezvousEnabled` is live and correctly threat-scoped, but ecologically near-null
in `WatchableStarterHabitat`: 120/120 seeds survived both arms; births were 285.93 off versus
286.77 on; paired t +0.93, 57/120 up; flee decision-ticks fell 26.9%. Keep the implementation
as a narrow safety rule, but do not claim it creates meaningful groups from this result.

## P5 semantics—do not overclaim “species”

Clusters are threshold-dependent analytical groupings, not authoritative species labels.
History uses ancestry evidence conservatively:

- same IDs and bounded ancestors support continuity;
- split/merge require positive support counts and fractions on the relevant sides and persistence;
- many-to-many strong relations are ambiguous rather than forced into a story;
- sampled or incomplete ancestry cannot prove lineage extinction;
- a weak living descendant blocks extinction;
- a history segment binds one ancestry object and one threshold/provenance regime, preventing a
  caller from crossing overflow or re-root discontinuities with a fresh object.

The Unity panel is titled `P5 history evidence`, samples every 300 ticks at genetic threshold
0.25, uses full-population snapshots, and shows the newest eight records plus policy/completeness.
The current flaw is visual flooding by `confirmed: ConfirmedContinuity`. That means the same
cluster track was supported across observations—a routine heartbeat, not an exciting event.
Filter it in presentation only.

## Soft home-range—what exists and what is unproven

Implemented contract:

- `HomeRangeAffinityEnabled` defaults false.
- Each creature has a centre and familiarity; newborns start blank and inherit no territory.
- Successful food, water, or reproduction moves the centre and increases familiarity; familiarity
  decays.
- Only valid ordinary food/water candidates that already meet the action threshold receive a
  bounded distance/familiarity bonus.
- It cannot manufacture hunger/thirst from a zero-utility resource; cannot affect fleeing, mate
  scoring, fallback wandering, carcasses, unavailable targets, or inert place memory.
- Threat gating uses the same filtered single/multi-threat logic as the actual flee decision.
- Flag-off output is byte-identical and omits home-range state from the hash.

Two real review bugs were already caught and fixed: the first version could make a satisfied
creature seek a familiar resource from zero utility, and it gated affinity using a nearest-creature
shortcut inconsistent with filtered threat scoring. Do not reintroduce either.

**Route formation was measured on 2026-08-22 and the answer is no.** Key `R` now enables the flag
on `ObservationStable`. Across 30 paired seeds in stable, scarcity and migration, same-site
fidelity changed by +0.0000 (and +0.0001 at a 10x bonus), while stable and scarcity already sit at
fidelity 1.0000 with the flag off because food and water are co-located and the second cluster is
outside vision range. The only effect a large bonus produces is patch clinging at a food-intake
cost. The blockers are scenario geometry and a bonus term collinear with the existing travel
burden — not the constants. Do not re-run this sweep or tune the constants; read
`docs/experiments/p4a-home-range-affinity-2026-08-22.md` and fix the geometry first.

## Play-mode controls and what they mean

- `Space`: pause/resume.
- `1`, `2`, `4`, `8`: speed.
- `B`: baseline; `D`: drought; `F`: food scarcity; `P`: predator/prey; `C`: cognition;
  `T`: physiology; `G`: foraging-memory demo; `M`: mating demo; `E`: starter habitat.
- `5`: observation stable; `6`: scarcity; `7`: migration; `9`: mating.
- `R`: home-range affinity - `ObservationStable` again, identical seed and config except
  `HomeRangeAffinityEnabled=true`. **It looks the same as `5`; that is the measured result, not a
  bug** (see `docs/experiments/p4a-home-range-affinity-2026-08-22.md`).
- `N`: broad all-flags playtest, but still omits home-range (left unchanged deliberately).
- `H`: temperature/biome/off ground overlay cycle.
- Left-click selects a creature; resource dragging is mouse-driven.

For P5 history, press `5`, then `8`, and watch after tick 600. Routine confirmed-continuity rows
are now hidden and counted as "N routine continuities hidden"; the analytical history still keeps
them. For home-range, press `R`.

## Test and verification workflow

From `tools/HeadlessTests`:

```powershell
dotnet build
dotnet test --no-build --filter "FullyQualifiedName!~LivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~PlantLivenessTests"
dotnet test --no-build --filter "FullyQualifiedName~LivenessTests&FullyQualifiedName!~RiskAversionIsLiveOnlyWhenThreatsExist"
dotnet test --no-build --filter "FullyQualifiedName~RiskAversionIsLiveOnlyWhenThreatsExist"
```

The RiskAversion test runs full ecosystem simulations and can be silent for a while. It completed
alone in 18 seconds on the latest reference run; a throttled environment can take much longer. Do
not call silence a hang. If necessary, use `--blame-hang --blame-hang-timeout 15m`; a named test is
a defect, while a slow shard without a named hang is environmental slowness.

Latest observed home-range verification:

- build: success, 15 existing warnings, 0 errors;
- fast non-liveness shard: 450/450;
- PlantLiveness: 19/19;
- liveness excluding RiskAversion: 33/33;
- RiskAversion alone: 1/1 in 18 seconds;
- Unity 6000.2.14f1 batch compile: exit 0 after the editor/project lock was released.

An earlier P5 milestone had 456/456 full headless tests. Do not translate these historical totals
into a claim about a new diff; report only commands actually observed after that diff.

The six CS8632 warnings in `AncestryHistory.cs`, `GeneticClusterHistory.cs`, and
`ClusterHistoryPolicy.cs` were fixed with local `#nullable enable annotations`. This deliberately
preserved `?` API annotations without enabling a wave of warnings. About 15 unrelated nullable
warnings remain in older core/resource/diagnostic/test code; do not claim a warning-free build.

Headless green does not prove Unity presentation compiles. The headless project excludes
Presentation, and Unity does not inherit the same global usings. Run/ask for Unity compile after
presentation changes.

## Determinism and performance rules that are non-negotiable

- Never edit `DeterministicRandom.cs` or `TemperatureField.cs` without an explicit task naming it.
- Never use `System.Random`, Unity random, clock/time/environment/thread identity, GUIDs, async,
  tasks, parallel loops, or machine state in Simulation.
- Use `DeterministicRandom` with a declared `RandomDomain`; never renumber existing domains.
- Do not use dictionary/hash-set iteration to drive logic. Iterate deterministic arrays by index.
- Do not reorder floating-point arithmetic during refactors.
- No allocation, LINQ, strings, logging, or avoidable enumerators in per-tick code. Use ref store
  accessors for mutation.
- Flag-off must be byte-identical. New behavior gets a default-false flag unless it is scenario
  data. Hash new state only while its feature is enabled if preserving the off hash is required.
- Presentation never becomes biological truth; diagnostics/analysis never feed decisions.
- Do not weaken tests or update expected hashes merely to match a surprise. Understand it first.

## Experiment method—the expensive lessons

- State numeric predictions before a run: manipulation metric, expected delta/t/sign count, and
  survival. A plausible mechanism story is not evidence.
- Save raw per-seed CSV in `docs/experiments/` with a dated name. A four-row summary is not enough
  to re-analyse.
- Report the manipulation check first. If it did not move, stop and call the downstream numbers
  noise.
- Print survival/extinction/frozen columns for every arm, including confirming arms.
- Put the sign count beside every t. A trait is not selected when it is less directionally
  consistent than its matched disabled/drift arm, whatever the t statistic says.
- Use matched flag-disabled/drift controls in the same harness.
- Vary founder trait values. Uniform founders have no standing variance and produce drift.
- Measure all traits in the same run when cheap; free traits provide controls.
- Sweep costs as well as benefits. A one-arm null cannot identify which side is wrong.
- Copy the exact calibration configuration from
  `ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds`.
  `maximumPopulation` is 48 there; the factory default 1000 collapses that calibration.
- Check procedural arms for confounds. The 168-site scenario changes count and geometry.
- Read `world.Statistics` after stepping; its tick-0 value is stale.
- Regress final trait on founder trait, never delta on founder. Slope 1 is drift; 0 is convergence.
- Split outcomes by how they happened before attributing pooled variance. Infant takeover and age
  death once manufactured a false lifespan conclusion.
- Liveness means influence, not fitness reward. A pure cost can be live and evolutionarily null.
- Bit-identical output after an arm flip means dead in that scenario, not merely small.
- `CreateFullEcosystemDefaults` is a wide configuration, not a scenario that exercises every
  mechanism. It still used a threat-free herbivore calibration for several liveness questions.
- Throwaway probes are `Assets/Tests/EditMode/ZZZ*.cs`; delete them before commit and verify none
  remain.

## Live/inert traps not to rediscover

- Place memory (`MemorySystem.ObservePlace`, decay, remembered-place effects) has no production
  writer and is deliberately enforced inert by liveness tests. Tests that directly call it are
  not evidence of production behavior.
- `ForagingEconomics.CommitmentBonus`/`ShouldAbandon` are Legacy-path behavior; P4 uses
  `IntentUtilityV1`.
- `Genome.NeutralMarker` is deliberately dead and retained as drift control.
- `RiskAversion` is live only when threats exist; the herbivore calibration cannot exercise it.
- `multiThreatPerceptionEnabled` and `kinRecognitionEnabled` are wired in IntentUtilityV1 but were
  unexercised in the threat-free liveness scenario. Do not delete them.
- `foragingEconomicsEnabled` and `learnedResourceQualityEnabled` are genuine Legacy/wire-or-delete
  candidates, but do not change them casually because baseline effects must be sequenced.
- `PlantTemperatureAdaptationEnabled` needs actual varying temperature; elevation itself needs
  both population cap 1000 and fertility adaptation before it affects the standard plant setup.
- Explicit resource-intent execution already exists in
  `SimulationWorld.ResolveResourceInteractions`. A plan proposing it is marked withdrawn; do not
  implement it again. Place-memory tests are not the evidence—the production guards are.

## Recommended queue after the immediate home-range task

1. ~~Add `R` matched home-range Play mode and measure it.~~ **Done 2026-08-22. Result: null for
   route formation, and not a tuning problem.** Do not re-run it; read the experiment writeup.
2. ~~Filter routine P5 continuity in the presenter.~~ **Done 2026-08-22.**
3. Design clustered, changing resource patches as scenario data, with fixed-seed route/travel/
   survival measurements. This is likely the largest remaining visual-ecology gain.
4. Reassess whether safety-gated rendezvous plus existing two-parent mating produces any local
   groups. Its first ecological experiment was null; do not build pack architecture to force it.
5. Improve selected-creature history/action feedback only where it helps a person distinguish
   foraging, drinking, mating, fleeing, resting, births, deaths, depletion, and recovery.
6. For P5, plan durable chunk storage and a graphical tree as separate work. Decompose
   `GeneticClusterHistory.cs` behavior-preservingly before adding much more classification logic.
7. Leave P6 terrain/world scale until P4a routes and P5 evidence are genuinely useful.

Potential later scientific work: a survivable predator-prey scenario is still needed to adjudicate
threat-gated flags and real cycles. The old `PredationVariation` plant calibration can go extinct
before 3,000 ticks with zero births, so it is not a valid liveness/ecology judge.

## Documents to trust

Read banners before old experiment conclusions; several were superseded or retracted.

- `docs/AGENT_FIELD_NOTES.md`: map, mechanism ledger, append-only lessons, standing facts.
- `docs/ROADMAP.md`: actual backlog. Plans are an archive, not a backlog.
- `docs/superpowers/specs/2026-08-12-product-architecture.md`: permanent P0–P7 principles.
- `docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`: known defects and
  deliberate deferrals; consult before “fixing” one.
- `docs/superpowers/specs/2026-08-21-soft-home-range-affinity-design.md` and matching plan.
- `docs/superpowers/specs/2026-08-21-p5-history-panel-design.md` and matching plan.
- `docs/superpowers/specs/2026-08-21-conservative-cluster-history-design.md` and
  `docs/superpowers/plans/2026-08-21-ancestry-aware-cluster-history.md`.
- `docs/experiments/p4-low-occupancy-plant-route-audit-2026-08-20.md`.
- `docs/experiments/p4-low-occupancy-growth-trait-reaudit-2026-08-20.md`.
- `docs/experiments/p4-site-abundance-seed-production-rate-2026-08-20.md`.
- `docs/experiments/p4-operating-point-decision-2026-08-20.md`.
- `docs/experiments/p4-invader-establishment-contest-2026-08-21.md`.
- `docs/experiments/p4-safety-gated-rendezvous-2026-08-21.md`.

## Final judgment

The project does not need more speculative mechanisms merely because they sound realistic. It
needs its existing mechanisms placed in scenarios that exercise them, measured against matched
controls, and surfaced clearly enough that the user can see the ecology without reading logs.
The next success criterion is not “home-range code exists”; it is “fixed-seed worlds visibly and
repeatably form useful local routes, while hunger, danger, scarcity, mating, and exploration still
pull creatures away.”
