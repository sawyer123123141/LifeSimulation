# The V2 ruleset seam: freezing the legacy paths and opening a corrected one

**Status:** DESIGN, approved in chat on 2026-09-10 and 2026-09-11. No code changed, no test written,
no run executed. Implementation plans for P0 and P1 follow from this document once it is reviewed.
**Date:** 2026-09-11
**Baseline:** `65bae69c8e0b42ae402cba8d82c2a330465e5d7a`. Every line number below was read at that
commit and will drift; grep by name.
**Governed by:** `2026-08-30-what-finished-means-design.md` (frozen). This spec adds no goal to it.
It serves §2 (information provenance), §5 (one complete causal loop) and §6 (the fitness chain).
**Inputs:** `AGENTS.md`; `docs/AGENT_FIELD_NOTES.md` §1–4; `docs/ARCHITECTURE.md`;
`2026-09-05-population-regulation-design.md`; `docs/experiments/p6-a2-not-integrated-2026-09-10.md`;
`docs/superpowers/plans/2026-09-02-measurement-validity-fitness.md`; and the simulation source.

**Not re-litigated.** The population does not persist in any ecological cell; A2 failed its own
criterion and is historical evidence, not code; the digestion loop fails at the fitness link. This
document starts after those facts.

---

## 1. What this decides

The project has good measurement infrastructure and no demonstrated evolutionary loop. Recovery
needs corrected behaviour without touching the behaviour every recorded number depends on. The
decision is a **versioned model**: the existing `Legacy` and `IntentUtilityV1` decision paths stay
byte-for-byte reproducible, and corrections enter through an explicit `IntentUtilityV2` path selected
by configuration. Nothing here duplicates the simulation. The boundary is one enum value, one typed
configuration property, one dispatch edit, and three new files.

Decided here: the boundary; the shape and versioning rules of the V2 ruleset; the two first bounded
tasks (P0 freeze, P1 seam) in enough detail to plan from; the order of everything after them; and
what is deliberately not fixed yet.

Not decided here: any provenance correction's replacement mechanism; Regulator E's competition key;
any parameter value; the defense experiment's predeclaration. Each of those gets its own spec.

---

## 2. What the source says (verified 2026-09-10)

### 2.1 How information reaches decisions today

Live path (`IntentUtilityV1`, every P4 scenario), per creature on its stagger tick, all inline in
`SimulationWorld.TickDecisions` (`Core/SimulationWorld.Ticking.cs:232-592`):

```
CreatureStore (needs, genome, phenotype, memory, lineage, reproduction)
  + ResourceStore  (the whole object)         ─┐
  + PerceptionSystem (grid query ≤ vision)     ├─ inline in TickDecisions
  + PredationSystem.Threat(exact phenotypes)   │
  + Climate field                              ─┘
        │  39-argument call, Ticking.cs:346-384
        ▼
DecisionSystem.DecideIntentUtilityV1     (Behavior/DecisionSystem.cs:391)
   ScoreResourceCandidates ← resources.GetAt(index)         exact ResourceState
   ScorePredation[Multi]   ← other Phenotype, Lineage, IsKin
   ScoreCarcass            ← resources.GetAt(carcass)
   ScoreMate               ← mate Needs, Phenotype, ReproductionState
        │  CreatureDecision
        ▼
shared post-processing: memory writes, arrival → Eat/Drink/FeedCarcass/Attack conversion
        ▼
execution: TickMovement (tracks target's live position), TickCombat, ResolveResourceInteractions,
           ReproductionSystem.Step (pairs by proximity, independent of the decision, when
           mateSelectionEnabled is false — the recorded cells)
```

The "observation" types are store handles, not percepts: `CreatureObservation.CreatureIndex` and
`ResourceObservation.ResourceIndex` (`Behavior/PerceptionSystem.cs:8-40`). The decision dereferences
the store through them. The only filters applied are distance ≤ vision and `Amount > 0`.

One nearest creature serves as threat, prey and mate candidate at once (`Ticking.cs:280`, reused at
`:357` and `:363`). A top-4 buffer exists only under `MultiThreatPerceptionEnabled` and only for
predation scoring.

### 2.2 Provenance of every decision input on the live path

| input | source | class |
|---|---|---|
| own needs, age, rest, cooldown, genome, phenotype | store, own index | internal state |
| own memory (scalar food/water/threat), home-range familiarity | `MemoryState`, `HomeRangeState` | learned memory |
| resource distance and existence | grid query ≤ vision | perceived cue |
| resource `Amount` | `resources.GetAt()` (`Scoring.cs:187`, `DecisionSystem.cs:559`) | **forbidden ground truth** — exact stock, no sampling |
| resource `NutritionMultiplier` | same, when `plantQualityPreferenceEnabled` (`Scoring.cs:283`) | **forbidden ground truth** |
| carcass `Amount` | `Scoring.cs:56-58` | forbidden ground truth |
| other creature distance, id | grid query | perceived cue |
| other `Phenotype` — AttackPower, Defense, Maneuverability, Aggression, EnergyCapacity, MeatYield | `GetPhenotypeAt(other.CreatureIndex)` (`Ticking.cs:283, :359, :365`) | **forbidden** for internal fields (Aggression, EnergyCapacity); body traits are an undocumented approximation |
| threat intensity | `PredationSystem.Threat` over exact phenotypes | derived from the above |
| mate `Needs`, `Phenotype`, `ReproductionState` | `Ticking.cs:364-366` → `CanSeekMate` on the mate (`Scoring.cs:34-37`) | **forbidden ground truth** — exact needs and cooldown |
| other lineage → `IsKin` | `Ticking.cs:377`, `DecisionSystem.cs:526` | documented load-bearing approximation (frozen spec §2) |
| parent position for juvenile following | `FindNearestAliveParent` by pedigree id, no range check (`Core/SimulationWorld.cs:552-575`) | **forbidden** — unperceived coordinates via pedigree |
| climate at own position | `ScoreThermalComfort` | internal (felt) |
| comfort target search | `FindNearbyComfortTarget` samples unvisited points | undocumented approximation |
| target live position each tick (pursuit, flee, mate) | `GetMovementAt(targetIndex)`, no range (`SimulationWorld.cs:434-451`) | undocumented approximation (tracking) |

The frozen spec listed mate selection, multi-threat, learned resource quality, carcass detection and
range rules as unaudited. Mate selection and carcass are exact truth. The learned-resource-quality
reader is Legacy-only (`Ticking.cs:407`): the live path has no sampling channel at all.

### 2.3 Configuration and founder coupling

```
FounderProfile (enum)
 ├─ Prototype1          6 core + urgency/travel/risk/neutral varied; 13 genes at the constructor's 0f
 ├─ CognitionVariation  6 core + 4 cognition varied; 9 at 0f (lifespan, fertility, temperature among them)
 ├─ PhysiologyVariation 13 varied; the combat six at 0f  ⇒ Attack = Defense = 0, pure herbivores
 └─ PredationVariation  combat six varied; everything else fixed at 0.5 (no physiology variation)
        ├─ SimulationWorld.CreateFounderGenome           founder genetics — legitimate
        ├─ Ticking.cs:360   predationEnabled = (FounderProfile == PredationVariation)   ← a MECHANISM switch
        ├─ Ticking.cs:481   Legacy predation override, same gate                          ← a MECHANISM switch
        └─ Ticking.cs:277   `FounderProfile == PredationVariation || policy == V1` inside `if (policy == V1)` — tautology
```

Consequences: predation can only ever run on `PredationVariation` founders, so "predation on with
physiology-varied founders" and "predation off with the same founders" are both unconstructible.
`CreateFullEcosystemDefaults` (`SimulationConfig.cs:933`) uses `PhysiologyVariation`, so the widest
liveness surface exercises no predation code, and could not even if the gate were removed, because
those founders have `Attack = 0`.

`SimulationConfig`: 1,096 lines, 67 constructor parameters (37 `bool`), no copy helper, `Validate()`
checks finiteness and ranges only. Construction sites: 15 non-test (5 factories, 6 presenter,
4 tools), 82 in tests, 1 reflective — 98. The hand-kept-replica risk is stated in the source itself
(`tools/HistoryProbe/Program.cs:155`: "Kept in step by hand, like tools/SitePilot"), and realised in
`Prototype1Presenter.cs:792/831`, which build `PredationVariation` + `IntentUtilityV1` with
`physiologyEnabled` silently defaulted to false.

`FlagLivenessAnalysis` flips constructor `bool`s only (`Diagnostics/FlagLivenessAnalysis.cs:69`).
Enums are invisible to it, so the founder-profile coupling has never had a liveness verdict.

`ComputeConfigurationHash` (`SimulationConfig.cs:795+`, `ConfigurationHashVersion = 9`) already
hashes both `FounderProfile` and `DecisionPolicyVersion`. `ComputeStateHash` and
`ComputeBehaviorHash` do not include the configuration.

### 2.4 Verdicts on the ten suspected problems

| # | claim | verdict | evidence | blocks the defense loop? |
|---|---|---|---|---|
| 1 | decisions receive exact hidden state | **confirmed**, all five kinds | §2.2 | partly — kin and mate readiness are load-bearing per frozen spec §2 condition 3 |
| 2 | no `World → perception → Observation → decision` boundary | **confirmed** | §2.1 | prerequisite for auditing 1 |
| 3 | `FounderProfile` gates predation | **confirmed** | `Ticking.cs:360, :481` | **yes** — the paired predation-on/off experiment cannot be built |
| 4 | full-ecosystem defaults exercise no predation | **confirmed** | §2.3 | indirectly |
| 5 | `Genome.Neutral` six-gene initialisation | **partially true** — `Neutral` reaches production only through parameterless `CreatureStore.Add()` and `SimulationWorld.Spawn()`, which have zero production callers; the real hazard is the constructor's thirteen `0f` defaults reaching founders through each factory's omissions | `GenomePhenotype.cs:7-59`, `PhysiologyFounderFactory.cs` | indirectly — V2 founder presets |
| 6 | `SimulationConfig` composition risk | **confirmed** (98 sites, not 100+) | §2.3 | partly — V2 construction |
| 7 | `ComputeNeedGain` saturates | **confirmed at typical energy**; founder capacity 120, per-unit gain 21.5, patch 12 units → 258 against ~23 missing at the recorded 0.806 mean energy: 11× over the clamp; amount is informative only below ~1 unit, or below ~44% of a patch at starvation | `DecisionSystem.cs:553-560`, `Scoring.cs:271-279` | not for defense; yes for digestion |
| 8 | mate = nearest creature only | **confirmed for the decision**; breeding follows the decision only when `mateSelectionEnabled`; the recorded cells run it off, where `FindNearestReadyMate` scans every ready neighbour ≤ 2 units regardless of the decision | `Ticking.cs:280`, `ReproductionSystem.cs:84-86, :119-179` | not under proximity pairing |
| 9 | flags behave differently across policies | **confirmed** | field notes §4 ledger; Legacy-only and V1-only parameter lists | no |
| 10 | no persistent ecology, no closed loop | **confirmed** | regulation spec §5; A2 record; digestion record | **yes**, and outside this spec |

Three findings the prompt did not name: (a) juvenile following is a second pedigree-as-knowledge
channel with unbounded range; (b) the other creature's phenotype at `Ticking.cs:283/:359` is the raw
store value while combat uses `GetEffectivePhenotype`, so juvenile scaling applies in combat but not
in threat and hunt scoring; (c) **no literal-pinned hash exists for the `IntentUtilityV1` path** —
all ten `const ulong` pins in `CoreSimulationTests.cs` are Legacy, seed 99, two founders, 50 ticks.
The live path is protected only by relative flag-off tests. That is why P0 exists.

---

## 3. Decisions taken

1. **Boundary: a policy-version seam.** `DecisionPolicyVersion.IntentUtilityV2 = 2`. Legacy (0) and
   V1 (1) code paths are not edited. This is the precedent Legacy → V1 already set.
2. **Legacy and V1 hashes are preserved bit-identically**: state hash, behaviour hash, state
   fingerprint and configuration hash. `ConfigurationHashVersion` stays 9. V2-only fields are hashed
   only when the policy is V2.
3. **V2-only settings live in one typed property**, `SimulationV2Ruleset`, on `SimulationConfig`.
   Enums and small value types only; never booleans. See §4.
4. **Predation becomes an explicit V2 mechanism choice**, `PredationMode { Disabled, Enabled }`,
   decoupled from `FounderProfile`. V2 never reads `FounderProfile` for behaviour.
5. **Existing proximity pairing stays shared.** Reproduction is not redesigned in the seam.
6. **V2 is headless-only** until the defense loop is proven. No presenter wiring.
7. **Regulator E is V2-only**, gets a new predeclaration, and its competition key must not read
   `Defense` or any score containing it (§7.2).
8. **The first causal-loop trait is `defense`**, identical founders, `PredationMode` Enabled versus
   Disabled. Digestion waits.
9. **Tests.** New tests only where this spec names the file. Existing tests are not changed to
   accommodate behaviour. One documented maintenance edit is authorised (§6.2).
10. **P0 and P1 are separate tasks, commits and reviews.**

---

## 4. `SimulationV2Ruleset`

### 4.1 Shape

```csharp
public enum PredationMode : byte { Unspecified = 0, Disabled = 1, Enabled = 2 }

public readonly struct SimulationV2Ruleset
{
    public int SchemaVersion { get; }          // stored per instance; 0 for `default`
    public PredationMode PredationMode { get; }

    public static SimulationV2Ruleset Schema1(PredationMode predationMode);

    private SimulationV2Ruleset(int schemaVersion, PredationMode predationMode);   // private

    public void Validate(DecisionPolicyVersion policy);   // throws ArgumentOutOfRangeException
    public ulong HashInto(ulong hash);                    // hashes only the selected schema's fields
}
```

**Construction paths.** Exactly two: `default` (schema 0) and the named schema factories
(`Schema1(...)`, later `Schema2(...)`). The field-setting constructor is `private`, so no caller can
assemble a ruleset with an unnamed field set or a schema number that does not match its fields.

**Placement on `SimulationConfig`.** One optional constructor parameter, `simulationV2Ruleset =
default`, **appended at the very end** of the existing constructor's parameter list — after
`generatedPlantSiteAnchorCount` — so every existing positional call remains valid unchanged. One
public property `SimulationV2Ruleset`. No other constructor is added.

### 4.2 Schema versioning — the reproducibility contract

- **Schema 0 is `default`** and is invalid under V2. It is the only valid value under Legacy and V1.
- **P1 creates schema 1 only**, containing `PredationMode`.
- **A schema is a frozen field set.** Adding a field means adding schema N+1 with its own static
  factory. Schema N's factory, validation and hash are never changed afterwards.
- **Validation depends on the instance's schema.** Schema 1 requires `PredationMode != Unspecified`.
  When schema 2 adds a field, a schema-1 instance stays valid without it.
- **Hashing depends on the instance's schema.** `HashInto` hashes `SchemaVersion` first, then exactly
  the fields that schema declares, in declaration order. Adding schema 2 cannot change any schema-1
  hash, because the schema-1 branch is not edited.
- **Enum values are append-only.** No value that has appeared in a committed experiment is ever
  removed or renumbered. A losing mode is marked historical in its XML doc and stays runnable —
  `Validate` never rejects a historical value. `Unspecified = 0` is reserved in every V2 enum.
- **Every enum's names and values are pinned by test** (§6.2, test 9). The pinned list may only
  grow; growing it is the documented maintenance step, not a weakening.

### 4.3 Why it stays small

`SimulationConfig` grew to 67 parameters because every mechanism arrived as a boolean plus its
tuning floats. The ruleset refuses both:

1. **Fields are mechanism *versions*, not knobs.** Each field is a closed enum answering "which rule
   does V2 run for mechanism X". Tuning floats stay where they are today; none enter the struct.
2. **No booleans**, enforced by test (§6.2, test 7). Tri-state enums with `Unspecified = 0` make
   every composition explicit; `Validate` refuses an unspecified field under V2.
3. **Modes retire.** A provenance correction lands as a new enum value beside the exact-truth value,
   runs its sensitivity experiment, and the losing value becomes historical. Booleans accumulate;
   modes are adjudicated. The struct carries the mechanisms currently under comparison plus the
   permanent version choices, nothing else.
4. **Construction is by factory** (§7 step 5). Ad-hoc V2 construction is caught by `Validate`.
5. **Ceiling.** If any schema passes about eight fields, that is a signal to split the ruleset by
   system — reported, not silently done. Same discipline as AGENTS.md's 300-line rule.
6. **Reflection harnesses ignore it by type**, so it never inflates `KnownInertFlags`,
   `FlagLivenessAnalysis`, or the constructor-bool hash test. V2 liveness gets its own harness later
   (§8).

### 4.4 Hashing

`ComputeConfigurationHash`'s existing sequence is not edited. After the last existing field:

```csharp
if (DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV2)
{
    hash = SimulationV2Ruleset.HashInto(hash);   // SchemaVersion, then that schema's fields
}
```

Legacy and V1 configurations execute the exact old path. V2 configurations differ from V1 already
through the policy value; `SchemaVersion` inside `HashInto` is the explicit V2 hash-schema
discriminator. `ComputeStateFingerprint` needs no change: it already folds in
`ComputeConfigurationHash`.

### 4.5 Validation

`SimulationConfig.Validate()` (already called by the `SimulationWorld` constructor,
`SimulationWorld.cs:80`) calls `SimulationV2Ruleset.Validate(DecisionPolicyVersion)`:

- policy V2 and schema 0 → throw;
- policy V2 and any field `Unspecified` for that schema → throw;
- policy V2 and any field holding a numeric value the enum does not define — for example
  `(PredationMode)255` — → throw, using the `Enum.IsDefined` check `Validate()` already applies to
  `FounderProfile` (`SimulationConfig.cs:1012`); "explicit" means a named value, not merely
  non-zero;
- an unknown `SchemaVersion` (neither 0 nor a schema this build defines) → throw;
- policy Legacy or V1 and schema ≠ 0 → throw.

Invalid compositions fail at world construction, never silently.

### 4.6 Manifest

`ExperimentManifest` carries `SchemaVersion = 1` with the contract "bump when the field set changes,
never redefine fields silently" (`Experiments/ExperimentManifest.cs`). V2 adds fields, so it takes
a new manifest schema rather than emitting extra lines under schema 1:

- Legacy and V1 configurations emit `schema=1` and exactly the lines they emit today,
  byte-identically. `SchemaVersion` stays 1 and continues to mean the Legacy/V1 field set.
- V2 configurations emit `schema=2` (a second constant, `V2SchemaVersion = 2`), the same field
  set as schema 1 in the same order — where `schema` and `DecisionPolicyVersion` necessarily carry
  their V2 values — and then `V2RulesetSchema` (the instance's schema number) and one line per
  field of that ruleset schema — for schema 1, `PredationMode`.
- The branch is on `config.DecisionPolicyVersion`; nothing else in `Describe` changes.

Proven by a test in `DecisionV2SeamTests.cs` (§6.2, test 8), not by editing
`ExperimentManifestTests.cs`, which is untouched.

---

## 5. Dispatch

One edit in `Core/SimulationWorld.Ticking.cs`, at the head of the policy chain
(`Ticking.cs:344`):

```csharp
if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV2)
{
    decision = DecideV2(index, tick, movement, phenotype, selfLineage,
                        food, water, carcass, otherCandidates, out diagnostics);
}
else if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)   // existing, verbatim
```

`DecideV2` lives in the new partial `Core/SimulationWorld.DecisionsV2.cs`. Which of the code
around the dispatch is shared and which is V1-only decides what V2 must replicate to be
hash-equivalent in P1:

| `TickDecisions` block | gate | V2 |
|---|---|---|
| nearest food / water / carcass (`:246-263`) | none | shared; passed in |
| Legacy foraging-economics candidates (`:268-272`) | Legacy | skipped by its own gate |
| V1 candidate buffers, nearest other, threat intensity (`:273-286`) | V1 | **replicated verbatim inside `DecideV2`**, with `predationEnabled` read from the ruleset instead of `FounderProfile` |
| multi-threat candidate buffer (`:288-297`) | `MultiThreatPerceptionEnabled` | shared; passed in, not recomputed |
| cognition memory reads and writes (`:298-313`) | `CognitionEnabled` | shared |
| Legacy place-memory failed search (`:314-341`) | Legacy | skipped |
| the V1 call (`:346-384`) | V1 | replaced by the `DecisionSystem.DecideIntentUtilityV2` call |
| cognition post-processing — active remembered target, `RememberThreat` (`:385-403`) | V1 branch, then `CognitionEnabled` | **replicated verbatim inside `DecideV2`** — `CreatePrototype4Defaults` has cognition on, so omitting it breaks test 1 |
| Legacy-only overrides (`:405-517`) | Legacy | skipped |
| arrival conversion, `SetDecisionAt`, diagnostics, trace (`:519-590`) | none | shared |

**Contract for the shared inputs.** `otherCandidates` is the `PredationCandidateBuffer` built at
`:287-297`; `DecideV2` receives it by value exactly as the V1 call does at `:372` and neither
rebuilds it nor reorders its construction. `food`, `water` and `carcass` are
likewise the values computed before the dispatch. Nothing shared is computed twice, so the shared
prefix of `TickDecisions` is identical for V1 and V2 by construction.

`DecideV2` computes `predationEnabled = Config.SimulationV2Ruleset.PredationMode == PredationMode.Enabled`,
never reads `FounderProfile`, and calls `DecisionSystem.DecideIntentUtilityV2` in
`Behavior/DecisionSystem.V2.cs`, which in P1 delegates to the unchanged V1 overload with that one
argument substituted. The V1 blocks, the tautology at `:277`, and every Legacy-only block are
gated on their own enum values, so V2 falls through them without edits. Post-decision shared
execution is unchanged.

The presenter never constructs a V2 configuration.

---

## 6. The first two bounded tasks

### 6.1 P0 — freeze the live V1 path (tests only)

**Authorised file:** create `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`. No production code,
no other test. Transient `ZZZ*.cs` probes may be used to capture constants and must be deleted; the
tree must be clean but for the one file before committing.

**Purpose.** No literal hash pins the `IntentUtilityV1` path today (§2.4 finding c). P0 pins four
representative V1 configurations so P1 can prove it changed nothing. **P0 pins four configurations,
not the 98 construction sites.** The file header must say so.

| pin | configuration | scenario | representative because | horizon |
|---|---|---|---|---|
| A | `CreatePrototype4Defaults(42, 12)` | `Prototype4Scenarios.ConsumerDefenseCalibrationModerate` | the baseline every one-flag P4 arm varies against (`SimulationConfig.cs:923-931`) | 2,000 ticks |
| B | `CreateFullEcosystemDefaults(42, 12)` | same | widest surface; the scenario `LivenessTests` pins the inert-flag set on | 2,000 ticks |
| C | hand replica of `tools/CreatureSweep/Program.cs:404` `CreateConfig(seed, slope: false)` for the recorded predation cell: `WorldSeed` 42 (`FirstSeed`), 12 founders, `PredationVariation`, cap 500, `gradedFertilityEnabled` strength 1.0, `reproductionNeedFraction` 0.45, `mateSelectionEnabled` false, kin and multi-threat on, terrain join on | `ConsumerDefenseCalibrationModerate.WithRegeneration("p6-defense-calibration-regen2.00", 2.0)` | the representative recorded V1 predation configuration (several others exist); the Task 9 lifetime-reproductive-success family | 2,000 ticks, or the earliest horizon with a deterministic predation event (below) |
| D | hand replica of the C3 control: `CreatureSweep --deaths 24 500 --regen=2.0 --brake=1.5 --ticks=24000` — `WorldSeed` 42, 12 founders, `PhysiologyVariation`, cap 500, brake 1.5, mate selection on, default gate | same scenario | **reproduces a committed artefact**: `docs/experiments/p6-deaths-perseed-cap500-regen2.00-24seeds-brake1.5-24000ticks-9samples-2026-09-10.csv`, arm `control`, seed 42, sample 1, tick 2,666, hash `663693199115149672` | 2,666 ticks |

Per pin, as `const ulong` literals: `ComputeStateHash`, `ComputeBehaviorHash`,
`ComputeStateFingerprint`, `ComputeConfigurationHash`. A and B additionally assert
`Statistics.PredationDeathCount == 0`, so nobody later mistakes them for predation pins. C asserts
`Statistics.AttackHitCount > 0`; if no attack lands by 2,000 ticks, C's horizon is extended to the
earliest tick at which one deterministically does, and the record says which tick and why.
The predation assertion is not forced.

**Cross-check for C (required).** A hand replica is a 99th hand-kept configuration site; it is not
trusted on its own. At untouched `65bae69` the implementer runs the real tool:

```
tools/CreatureSweep --focused 1 500 --regen=2.0 --brake=1.0 --predation --gate=0.45 --mate-selection=off --ticks=<horizon>
```

Main mode writes a CSV whose header is the complete emitted `ExperimentManifest.Describe(...)`
text and whose rows carry, per arm and seed, the seed and that run's final `ComputeBehaviorHash`.
Three requirements, in order:

1. **Seed.** Focused mode selects its seeds through `Relief.WithRelief(FirstSeed, …)`
   (`Program.cs:338`), so the emitted seed is not guaranteed to be 42. The produced `slope-off`
   row's `seed` must equal the replica's `WorldSeed`. If it does not, **stop**; do not adjust the
   replica to fit.
2. **Behaviour.** The replica's `ComputeBehaviorHash` at the horizon equals that row's hash.
3. **Configuration.** Take the tool's emitted header (it describes the `slope: true` arm) and
   `ExperimentManifest.Describe` over the `slope: false` replica configuration and scenario with
   the same `firstSeed`, `seedCount` and `ticks`. Normalise **both** texts by removing exactly two
   lines — `code_revision` and `SlopeMovementCostEnabled` — and nothing else. Everything remaining
   must match byte-for-byte, every line in order. No line count is hard-coded.

This cross-check is temporary: it verifies the replica once, at capture time, and its artefacts are
not what P1 compares against. `--deaths` mode with `--samples=` placing a boundary on the horizon
may be used as a second source for the hash; it prints no manifest, so requirements 1 and 3 still
need main mode. The tool writes its CSV under `docs/experiments/`; that file is captured and
deleted, never committed. If the tool cannot expose enough to satisfy all three requirements,
**stop and report** rather than pin an unverified replica.

**The permanent V1 manifest pin.** After the cross-check succeeds, P0 stores a canonical manifest
produced directly from the `slope: false` pin-C replica:

```csharp
ExperimentManifest.Describe("p0-pin-c", replicaScenario, replicaConfig, firstSeed, seedCount, ticks)
```

with the fixed revision string `p0-pin-c`, kept as an `internal const string` holding the
**complete** text — including `code_revision=p0-pin-c` and `SlopeMovementCostEnabled=false` —
alongside the `firstSeed`, `seedCount` and `ticks` used as `internal const` values and the replica
configuration as an `internal static` factory. P1's tests reuse all of them (§6.2, test 8) and
never compare a slope-on text against a slope-off one.

**Cross-check for D.** The pinned value is the committed artefact's, not a fresh capture. If the
replica does not reproduce `663693199115149672` at tick 2,666, that is a stop-and-report finding.
A fresh number is not pinned over it.

**Completion.** All pins green; full suite run twice; `LivenessTests` untouched and green; no
`ZZZ*` file; no stray CSV; `git status` shows only the one new file; **the P0 commit contains
exactly `PolicyVersionFreezeTests.cs` and nothing else**; own commit and review. Completion is
reported in chat — captured seed, horizons, which cross-check source was used, suite counts. No
completion file is written: `.claude/completions/` does not exist and is not ignored, and the
one-file commit rule takes precedence over the general CLAUDE.md completion-note habit for this
task.

### 6.2 P1 — the V2 seam and the explicit predation switch

**Precondition:** P0 merged.

**Files — modify:** `Core/SimulationConfig.cs` (`IntentUtilityV2` enum value; ruleset constructor
parameter and property; the conditional hash block; `Validate`), `Core/SimulationWorld.Ticking.cs`
(the one dispatch edit of §5, nothing else), `Experiments/ExperimentManifest.cs` (manifest schema 2 for V2,
§4.6), `Assets/Tests/EditMode/StateFingerprintTests.cs` — **only** `PinnedConfigurationPropertyCount`
from 66 to 67, which is that test's documented maintenance rule (its own comment instructs it).
**Files — create:** `Core/SimulationV2Ruleset.cs` (the struct **and** the `PredationMode` enum),
`Core/SimulationWorld.DecisionsV2.cs`, `Behavior/DecisionSystem.V2.cs`,
`Assets/Tests/EditMode/DecisionV2SeamTests.cs`.
**Must not touch:** `DecisionSystem.cs`, `.Scoring.cs`, `.Legacy.cs`, `PerceptionSystem`,
`ReproductionSystem`, founder factories, `DeterministicRandom.cs`, `TemperatureField.cs`,
`RandomDomain`, anything under `Analysis/` or `Diagnostics/`, `tools/`, the presenter,
`PolicyVersionFreezeTests.cs`, any other test.

**Behaviour contract.** V2's only behavioural difference from V1 is the predation switch. With
`PredationMode` mirroring what `FounderProfile` would have implied, V2 and V1 produce identical state
and behaviour hashes. `FounderProfile` still selects founder genetics under V2; it selects no
behaviour.

**How V2 configurations are built in tests.** `SimulationConfig` has no copy method, and the P1
tests must not add hand-maintained rewrites of pins A, B or C. `DecisionV2SeamTests` carries one
test-local helper:

```csharp
static SimulationConfig AsV2(SimulationConfig source, SimulationV2Ruleset ruleset)
```

It reflects over the single public constructor's parameters, fills every argument from the source
configuration's property of the same PascalCase name — the convention `FlagLivenessAnalysis.BuildArguments`
already relies on (`FlagLivenessAnalysis.cs:97-116`; that method is private, so the helper
re-implements the convention rather than calling it) — and overrides exactly two arguments:
`decisionPolicyVersion = IntentUtilityV2` and `simulationV2Ruleset = ruleset`. Any parameter
without a matching property is an error, not a default. Pins A and B come from their factories;
pin C comes from the `internal static` replica factory `PolicyVersionFreezeTests` exposes (§6.1).

**`DecisionV2SeamTests`** (2,000 ticks unless stated):

1. `V2Disabled_MatchesV1_WithoutPredation` — `AsV2(pin, Schema1(Disabled))` equals the V1 pin on
   state and behaviour hash, for pins A and B (`PhysiologyVariation` founders).
2. `V2Enabled_MatchesV1_WithPredation` — `AsV2(pinC, Schema1(Enabled))` equals V1 pin C at P0's
   horizon; `AttackHitCount > 0` in both.
3. `V2Disabled_PredationFoundersCannotActivatePredation` — `AsV2(pinC, Schema1(Disabled))`:
   `AttackHitCount == 0`, `PredationDeathCount == 0`, `FleeDecisionCount == 0`; hash diverges
   from V1 pin C.
4. `V2Enabled_PredationExecutesWithControlledCreatures` — `InitialPopulation = 0`,
   `PhysiologyVariation` profile; `Spawn(Genome)` identical genomes with nonzero attack, defense
   and aggression, `dietSpecialization ≥ 0.58` and `aggression ≥ 0.35` (so
   `PredationSystem.HasViableHuntingStrategy` holds), positions set through `GetMovementRefAt`,
   energy lowered so hunger is positive. Enabled → `AttackHitCount > 0`; Disabled, same
   creatures → 0. Founders with zero combat genes are not accepted as evidence for the switch.
5. `ConfigurationHashDistinguishesV2FromV1` — for pins A, B and C: `AsV2(pin, Schema1(Enabled))`
   and `AsV2(pin, Schema1(Disabled))` each hash differently from the V1 pin, and differently from
   each other; the V1 pin's hash is computed live, not restated as a literal. P0's literals and
   `CoreSimulationTests`' Legacy pin are the single source of truth for "unchanged" and are not
   duplicated here — they stay in the full suite.
6. `ValidateRejectsInconsistentPolicyAndRuleset` — V2 + schema 0 throws; V2 + `Schema1(Unspecified)`
   throws; V2 + `Schema1((PredationMode)255)` throws; Legacy or V1 + `Schema1(Enabled)` throws.
7. `RulesetExposesNoBooleans` — reflection over `SimulationV2Ruleset` and the constructor parameter:
   no `bool` field, property or parameter; `SimulationV2Ruleset` exposes **no public instance
   constructor with parameters** (runtimes differ on whether the implicit parameterless struct
   constructor is reported, so the test does not count public constructors). The private
   field-setting constructor may additionally be located through non-public reflection to confirm
   it is non-public.
8. `ManifestSchemaIsVersionedByPolicy` — two assertions.
   *V1 unchanged:* `ExperimentManifest.Describe("p0-pin-c", …)` over pin C's replica, with P0's
   `firstSeed`, `seedCount` and `ticks`, equals P0's canonical `internal const string`
   byte-for-byte, complete text, no normalisation.
   *V2 versioned:* `Describe("p0-pin-c", …)` over `AsV2(pinC, Schema1(Enabled))` differs from the
   V1 text in exactly these lines and no others — `schema=1` becomes `schema=2`;
   `DecisionPolicyVersion=IntentUtilityV1` becomes `DecisionPolicyVersion=IntentUtilityV2`;
   `V2RulesetSchema=1` and `PredationMode=Enabled` are added. After removing those changed and
   added lines from both texts, every remaining line is identical in content and order.
   `ExperimentManifestTests.cs` is not edited.
9. `Schema1HashAndEnumValuesArePinned` — `const ulong` pins of `ComputeConfigurationHash` for
   `AsV2(pinA, Schema1(Enabled))` and `AsV2(pinA, Schema1(Disabled))`; `PredationMode` names and
   values pinned as `{Unspecified=0, Disabled=1, Enabled=2}`; `Schema1(...)` reports
   `SchemaVersion == 1` and validates under V2. This is the test a future schema 2 must keep green
   without editing: it is the executable form of §4.2.

**Determinism.** Delegation only; no new randomness; no reordered floating-point operation. The
reviewer verifies from the diff that `RandomDomain` gained no member and none was renumbered; no
test pins the member count. `LivenessTests` untouched and green. Full suite twice. Own commit and
review.

---

## 7. Recovery order after P1

Ordered by dependency. Each step is its own bounded task or spec.

3. **`Observation` structure in V2**, value-preserving: a struct built by a V2 perception step and
   consumed by `DecideIntentUtilityV2`, carrying exactly the values V1 reads today. Test: every V2
   hash unchanged against P1.
4. **One provenance correction per task**, V2-only, as a new enum mode beside the exact-truth mode,
   with a predeclared sensitivity arm and no target value, in this order:
   1. predation cues, including the juvenile-scaling inconsistency (§2.4 finding b);
   2. kin recognition;
   3. target tracking;
   4. parental following range;
   5. resource perception (amount and quality sampling);
   6. mate perception and candidate search.

   **Gate before step 8.** Items 5 and 6 may be postponed during seam work, but not past the defense
   experiment. Before it, each is either replaced in V2, or cleared by source tracing **plus** a
   controlled sensitivity test showing it cannot materially affect defense fitness under predation
   Enabled and Disabled. "Both arms use the same shortcut" is not an argument: predation changes
   movement, feeding, survival and mating opportunity, so a shortcut can bias the arms differently.
5. **V2 named construction.** `SimulationConfig.CreateV2...` factories that fill every ruleset
   field, and a V2 founder preset with all 24 genes varied. The 98 legacy construction sites are not
   rewritten.
6. **Regulator E** — see §7.2.
7. **Persistent V2 truth scenario.** A persistence criterion is predeclared and must be met in
   **both** arms (Enabled and Disabled) before the loop is read. A comparison between a persisting
   arm and a collapsing one is not a comparison.
8. **The defense causal loop.** Identical founders; `PredationMode` Enabled versus Disabled;
   predeclared sign — defense rises under predation and falls without it, because
   `Phenotype.FromGenome` charges `0.10 * Defense` maintenance (`GenomePhenotype.cs:449`);
   lifetime reproductive success from `LifeHistoryLedger` / `FitnessCohort` /
   `PerWorldRelationship`; allele frequency from `Trajectory` gene columns and
   `PopulationGenomeSnapshot`; `PopulationCapDiagnostic` must report the skew reading safe;
   replicated seeds; `neutral_marker` beside every drift column.
9. Only then: cognition, niches, divergence, scale, presentation.

### 7.1 What happens to reproduction

Proximity pairing stays shared (decision 5). Consequence, stated so nobody rediscovers it: under
`mateSelectionEnabled == false` the birth mechanism is `FindNearestReadyMate`, which scans every
ready neighbour within 2 units using exact `IsReady`, independent of any decision. Step 4.6
therefore changes only movement toward mates, not who breeds. The pairing shortcut itself is a
reproduction redesign and is outside this spec; the §7 step-4 gate covers what the defense
experiment needs from it.

### 7.2 Regulator E under V2

E (`2026-09-05-population-regulation-design.md` §2) is contest allocation in
`ResourceAllocationSystem.Resolve`, a shared system. It enters V2 as a ruleset field of a new schema
— `ResourceAllocationMode { Unspecified, Scramble, Contest }` — never as a constructor boolean, so
V1 stays frozen and V1's flag ledger does not grow. E's 2026-09-05 predeclaration targeted a V1 cell
and is superseded; E gets a new predeclaration with a V2 control arm.

Restriction for the defense experiment: E's competition ordering must not read `Defense`, or any
score that includes it, or the regulator selects the trait the experiment attributes to predation.
Note that the `Defense` phenotype is `gene.Defense * (0.75 + 0.5 * BodyMass)`, so a key built on
`BodyMass` is gene-independent of `Defense` but a key built on the `Defense` phenotype is not. The E
spec must: state its competition key exactly; justify its biological meaning; run E on with
predation Disabled and test defense drift against the `neutral_marker` null; and **reject the truth
scenario if E independently moves defense materially**. E remains a candidate, not an assumed
solution.

---

## 8. Deliberately not fixed now

- The `SimulationConfig` constructor (98 sites). V2 factories bypass it; nothing is rewritten.
- The tautology at `Ticking.cs:277`. Dead under V1, frozen.
- `CreateFullEcosystemDefaults`' founder profile. Changing it moves `KnownInertFlags`; the V2 truth
  scenario takes over its role for predation.
- B-5's hunting threshold (a role label). A step-4.1 candidate.
- `Genome.Neutral`'s six-gene initialisation. No production caller; test-only; a V2 founder preset
  (step 5) is the fix for the real hazard.
- A V2 liveness harness over enum modes. Needed once step 4 has more than one mode to compare; not
  part of P1.
- Any persistence mechanism other than E's own spec.

---

## 9. Risks and stop conditions

- **Replica drift.** Pin C mirrors `CreatureSweep.CreateConfig` as of `65bae69`. Later edits to the
  tool silently desynchronise it. The P0 file header records the mirrored line and the cross-check
  performed.
- **Indirect coupling of E to defense.** Excluding `Defense` from the key does not exclude the
  maintenance-cost route (Defense → energy → condition). Step 6's predation-Disabled drift test is a
  required gate, not advice.
- **Thin delegation.** Every P1 equivalence claim rests on V2's only difference being the predation
  switch. Any second difference invalidates tests 1 and 2 and must be reported, not absorbed.
- **Reflection over a struct parameter.** `StateFingerprintTests.EveryConfigurationBoolConstructorParameterChangesTheConfigurationHash`
  rebuilds the constructor from `ParameterInfo.DefaultValue`; for a value-type parameter declared
  `= default` reflection reports `null`, which `ConstructorInfo.Invoke` converts to `default(T)`.
  `FlagLivenessAnalysis.BuildArguments` copies the property value instead. Both must pass
  unchanged in P1; if either does not, that is a stop, not a test edit.
- **Sweep seed and founders.** `CreatureSweep` runs `FirstSeed = 42` with `Founders = 12`
  (`Program.cs:45-46`); pins C and D use those values, and the P0 file says so.
- **Stop and report** if: pin D does not reproduce the committed artefact; the sweep tool cannot
  expose enough to verify pin C; any `ComputeStateHash` assertion outside the new files moves; a
  V2 change needs a file this spec does not name.

---

## 10. Compatibility guarantees, in one place

| surface | guarantee |
|---|---|
| `Legacy` and `IntentUtilityV1` behaviour | code paths not edited; state and behaviour hashes pinned by P0 and `CoreSimulationTests` |
| `ComputeConfigurationHash` for Legacy/V1 | old sequence executed verbatim; `ConfigurationHashVersion` stays 9; pinned by P0 |
| `ComputeStateFingerprint` for Legacy/V1 | unchanged, via the above |
| committed manifests | Legacy/V1 emit `schema=1` byte-identically; V2 emits `schema=2` with its ruleset lines |
| `KnownInertFlags`, `FlagLivenessAnalysis`, constructor-bool hash test | unaffected: the ruleset is not a `bool` and adds no constructor |
| `RandomDomain` | no member added or renumbered in P0 or P1 |
| V2 schema 1 | validation and hash frozen once P1 lands; later schemas append; enum values append-only; historical modes stay runnable |
| recorded experiment evidence | untouched; every recorded cell remains constructible and reproduces |
