# V2 Ruleset Seam (P1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Open the `IntentUtilityV2` decision path behind an explicit, typed `SimulationV2Ruleset` whose only P1 field is `PredationMode`, so predation becomes a configuration choice instead of a side effect of `FounderProfile`, while every Legacy and `IntentUtilityV1` hash pinned by P0 and `CoreSimulationTests` stays bit-identical.

**Architecture:** One enum value (`DecisionPolicyVersion.IntentUtilityV2 = 2`), one `readonly struct` (`SimulationV2Ruleset`, schema-versioned, factory-constructed, no booleans) appended as the last optional constructor parameter of `SimulationConfig`, one dispatch edit at the head of the policy chain in `SimulationWorld.TickDecisions`, and two thin new partials: `SimulationWorld.DecidesV2` replicates the V1-only candidate and cognition blocks verbatim with `predationEnabled` read from the ruleset, and `DecisionSystem.DecideIntentUtilityV2` forwards to the unchanged V1 overload. The experiment manifest gains a `schema=2` branch for V2 configurations. Nine new tests in one file prove equivalence, divergence, validation, hashing and manifest versioning against P0's `internal` pin-C members.

**Tech Stack:** C# / NUnit 4 via `tools/HeadlessTests` (`dotnet test`); Windows PowerShell 5.1; git.

**Spec:** `docs/superpowers/specs/2026-09-11-v2-ruleset-seam-design.md` — §3 (decisions), §4 (`SimulationV2Ruleset`, hashing, validation, manifest), §5 (dispatch), §6.2 (P1 files, behaviour contract, the nine tests), §9 (stop conditions), §10 (compatibility guarantees). Read §4–§6.2 before starting. P0's plan (`docs/superpowers/plans/2026-09-12-v1-policy-freeze.md`) and its product, `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`, are the source of every V1 reference value.

## Global Constraints

- Work only in the worktree `.claude/worktrees/v2-ruleset-seam-p1` on branch `v2-ruleset-seam-p1`, based on `d7e433b` (P0 merged; `main` and `origin/main` at `d7e433b`). Run every `git` and `dotnet` command with the PowerShell tool from the worktree root.
- **Authorised files (spec §6.2) and nothing else.** Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, `Assets/Scripts/Simulation/Core/SimulationWorld.Ticking.cs` (the one dispatch edit only), `Assets/Scripts/Simulation/Experiments/ExperimentManifest.cs`, `Assets/Tests/EditMode/StateFingerprintTests.cs` (only `PinnedConfigurationPropertyCount` 66 → 67). Create: `Assets/Scripts/Simulation/Core/SimulationV2Ruleset.cs`, `Assets/Scripts/Simulation/Core/SimulationWorld.DecisionsV2.cs`, `Assets/Scripts/Simulation/Behavior/DecisionSystem.V2.cs`, `Assets/Tests/EditMode/DecisionV2SeamTests.cs`.
- **Must not touch:** `DecisionSystem.cs`, `DecisionSystem.Scoring.cs`, `DecisionSystem.Legacy.cs`, `PerceptionSystem.cs`, `ReproductionSystem.cs`, any founder factory, `DeterministicRandom.cs`, `TemperatureField.cs`, the `RandomDomain` enum, anything under `Analysis/` or `Diagnostics/`, `tools/`, the presenter, `PolicyVersionFreezeTests.cs`, any other test, `.gitignore`, `Packages/`, `ProjectSettings/`, any `docs/` file other than this plan (this plan is the only document committed, and it is committed before implementation begins).
- **Legacy and V1 hashes are preserved bit-identically.** `ConfigurationHashVersion` stays `9`. The existing `ComputeConfigurationHash` sequence is not edited; the V2 block is appended after the last existing field and executes only under `IntentUtilityV2`. `ComputeStateHash`, `ComputeBehaviorHash` and `ComputeStateFingerprint` are not edited.
- **V2's only behavioural difference from V1 is the predation switch.** `PredationMode.Enabled` reproduces exactly what `FounderProfile == PredationVariation` implied for the `predationEnabled` argument of `DecideIntentUtilityV1`; nothing else in V2 reads `FounderProfile` for behaviour. Any second difference discovered during implementation is a stop, not something to absorb.
- **V2 is headless-only.** No presenter change, no tool change, no factory. The only V2 configurations in this task are built by the test-local `AsV2` reflection helper.
- **No booleans in the ruleset**, no public instance constructor with parameters on the struct, enum values append-only with `Unspecified = 0` reserved, schema 0 = `default`, schema 1 = `{ PredationMode }`.
- `AGENTS.md` §3 and §4 apply to every new production line: no new randomness, no reordered floating-point operation, no allocation in per-tick code beyond what the V1 block already performs (the two `ResourceCandidateBuffer` structs and the `PredationCandidateBuffer` struct are stack values, as they are today), no LINQ in `Assets/Scripts/Simulation/`, no `foreach` in per-tick paths, full words in names.
- `RandomDomain` gains no member and none is renumbered. Verified from the diff in Task 7.
- Never edit, weaken, `[Ignore]` or delete any existing test. The two documented replacements — `PinnedConfigurationPropertyCount` 66 → 67 (that test's own comment instructs it) and the sentinel-to-captured replacement of the two new V2 configuration-hash literals in test 9 (Task 3 step 9) — are the only expected-value edits, and both are scheduled here.
- `PolicyVersionFreezeTests.cs` is never edited. Its seven `internal` members — `FreezeSeed`, `PinCTicks`, `PinCSeedCount`, `PinCRevision`, `PinCCanonicalManifest`, `RecordedPredationScenario`, `RecordedPredationCell(int)` — are the only things P1 reuses from it. `DecisionV2SeamTests` never restates pin A/B/C construction, never restates a P0 hash literal, and never restates the canonical manifest.
- No `.meta` files are created. Every headless-only file added since 2026-09-01 (`Analysis/FitnessCohort.cs`, `Analysis/LifeHistoryLedger.cs`, `PolicyVersionFreezeTests.cs`, …) shipped without one; P1 follows that precedent and the spec's file list, which names none. Recorded as a known gap in the completion report, not fixed here.
- Full suite twice before the final commit; `LivenessTests` untouched and green; `git diff --check` clean on every commit; the tree clean after every commit.
- Commits are small and bounded: one per task, each leaving the suite green. Commit messages end with `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.
- Completion is reported in chat. No completion file is written.

## Stop conditions (report, do not work around)

| condition | action |
|---|---|
| Task 1's baseline suite is not green, or its passed count is not 769 | stop; report the count and the first failure |
| any grep in Task 1 step 3 prints something other than its expected output | stop; report the line — the source no longer agrees with the spec |
| `StateFingerprintTests.EveryConfigurationBoolConstructorParameterChangesTheConfigurationHash` fails after the constructor parameter is added (Task 2 step 6) | stop; report verbatim — the spec §9 reflection risk materialised; do not edit the test |
| `LivenessTests` fails at any point | stop; report verbatim |
| any P0 pin (`PolicyVersionFreezeTests`) or `CoreSimulationTests` `ExpectedLegacyHash` assertion fails at any point | stop; report which pin and which hash — a Legacy or V1 path was altered |
| test 1 or test 2 fails after Task 5 with a hash mismatch | stop; report the pin and the hash — V2 has a second difference from V1; find it by reading `TickDecisions`, do not tune V2 until it matches |
| test 4's Enabled arm lands no attack within 600 ticks with the creatures specified in Task 6 | stop; report `AttackHitCount`, both creatures' `Action` per tick for the first 100 ticks, and their distance; do not lower the diet or aggression genes below the `HasViableHuntingStrategy` thresholds or raise the horizon further |
| a change is needed in a file this plan does not authorise (a `.meta`, `HeadlessTests.csproj`, a helper in production, a founder factory, `DecisionSystem.cs`) | stop; report which file and why |
| any test outside `DecisionV2SeamTests` and the one authorised `StateFingerprintTests` line changes value or fails | stop; report verbatim |
| `dotnet test` fails to build before any change | stop; report the first error line |

---

### Task 1: Baseline verification and spec-assumption checks

**Files:**
- none created or modified

**Interfaces:**
- Produces: confirmation that the worktree is at `d7e433b` on the right branch with a green 769-test suite, and that every source fact the spec's P1 design relies on is still true at this commit.

- [ ] **Step 1: Verify the worktree**

```powershell
git branch --show-current
git status --porcelain
git rev-parse --short HEAD
git merge-base --is-ancestor d7e433b HEAD; "ancestor: $?"
git diff --stat 65bae69 HEAD -- Assets/Scripts tools
git log --oneline -1 origin/main
```

Expected: `v2-ruleset-seam-p1`; no output from `status`; HEAD is `d7e433b` or a descendant whose only additional commit is this plan; `ancestor: True`; **no output** from the `diff --stat` (production and tool code are byte-identical to the spec's baseline `65bae69`; only `docs/` and `Assets/Tests` changed between `65bae69` and `d7e433b`); `origin/main` at `d7e433b`. Anything else: stop.

- [ ] **Step 2: Build and run the full suite before any change**

```powershell
dotnet test tools/HeadlessTests --nologo
```

Expected: `Passed!` with `Failed: 0`, `Passed: 769`. Record the count. Every later full run must report `769 + 9 = 778`. If the count is not 769 or anything fails: stop.

- [ ] **Step 3: Verify every spec assumption against the source**

Each command's expected output is stated. Any deviation means the source has moved from the spec and the plan must not be executed until the contradiction is resolved.

```powershell
# (a) the policy enum has exactly Legacy = 0 and IntentUtilityV1 = 1
Select-String -Path Assets/Scripts/Simulation/Core/SimulationConfig.cs -Pattern "Legacy = 0,|IntentUtilityV1 = 1,|IntentUtilityV2"
```
Expected: two lines (`Legacy = 0,` and `IntentUtilityV1 = 1,`), no `IntentUtilityV2`.

```powershell
# (b) the constructor's last parameter is generatedPlantSiteAnchorCount
Select-String -Path Assets/Scripts/Simulation/Core/SimulationConfig.cs -Pattern "int generatedPlantSiteAnchorCount = DefaultGeneratedPlantSiteAnchorCount\)"
```
Expected: exactly one line, the closing parameter of the single public constructor (line 178 at `d7e433b`).

```powershell
# (c) ConfigurationHashVersion is 9 and the last hashed field is GeneratedPlantSiteAnchorCount
Select-String -Path Assets/Scripts/Simulation/Core/SimulationConfig.cs -Pattern "ConfigurationHashVersion = 9;|hash = Hash\(hash, unchecked\(\(ulong\)GeneratedPlantSiteAnchorCount\)\);"
```
Expected: two lines.

```powershell
# (d) Validate already applies Enum.IsDefined to FounderProfile and DecisionPolicyVersion
Select-String -Path Assets/Scripts/Simulation/Core/SimulationConfig.cs -Pattern "Enum.IsDefined\(typeof\((FounderProfile|DecisionPolicyVersion)\)"
```
Expected: two lines.

```powershell
# (e) the SimulationWorld constructor calls Config.Validate()
Select-String -Path Assets/Scripts/Simulation/Core/SimulationWorld.cs -Pattern "Config.Validate\(\);"
```
Expected: one line.

```powershell
# (f) the three FounderProfile-as-mechanism sites and the policy dispatch in TickDecisions
Select-String -Path Assets/Scripts/Simulation/Core/SimulationWorld.Ticking.cs -Pattern "FounderProfile == FounderProfile.PredationVariation|DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1"
```
Expected: exactly six lines — 273 (V1 candidate gate), 277 (tautology, first half), 278 (tautology, second half), 344 (dispatch), 360 (the `predationEnabled` argument), 481 (Legacy predation override). The line numbers are those at `d7e433b` and only need to agree in order and count.

```powershell
# (g) no site outside SimulationConfig and the tick loop switches on the policy other than by equality to Legacy or V1
Select-String -Path Assets/Scripts/Simulation/**/*.cs, tools/**/*.cs -Pattern "DecisionPolicyVersion\." | Where-Object { $_.Line -notmatch "DecisionPolicyVersion\.(Legacy|IntentUtilityV1)\b" -and $_.Path -notmatch "SimulationConfig\.cs$" }
```
Expected: exactly one line — `ExperimentManifest.cs:61`, `config.DecisionPolicyVersion.ToString()`, which prints the value's name and branches on nothing. Anything else (a `switch` with a throwing default, an `Enum.GetValues` over the policy, an inequality test) would break or change meaning when the value is added: stop.

```powershell
# (h) the manifest's first line is the schema constant and the last is ThreatFalloffDistance
Select-String -Path Assets/Scripts/Simulation/Experiments/ExperimentManifest.cs -Pattern "public const int SchemaVersion = 1;|Line\(builder, `"schema`",|Line\(builder, `"ThreatFalloffDistance`""
```
Expected: three lines.

```powershell
# (i) StateFingerprintTests pins 66 and the bool-constructor test rebuilds from ParameterInfo.DefaultValue
Select-String -Path Assets/Tests/EditMode/StateFingerprintTests.cs -Pattern "PinnedConfigurationPropertyCount = 66;|parameter.HasDefaultValue \? parameter.DefaultValue : false"
```
Expected: two lines.

```powershell
# (j) P0 exposes exactly the seven internal members this plan reuses
Select-String -Path Assets/Tests/EditMode/PolicyVersionFreezeTests.cs -Pattern "^\s+internal\s"
```
Expected: exactly seven lines — `FreezeSeed`, `PinCTicks`, `PinCSeedCount`, `PinCRevision`, `PinCCanonicalManifest`, `RecordedPredationScenario`, `RecordedPredationCell`.

```powershell
# (k) the V1 overload the V2 delegate forwards to: the full one begins with these parameters in this order
Select-String -Path Assets/Scripts/Simulation/Behavior/DecisionSystem.cs -Pattern "public static CreatureDecision DecideIntentUtilityV1\("
```
Expected: two lines (the short overload at 347 and the full overload at 391). Open the full overload and confirm its parameter list is, in order: `needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory, cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled, reproduction, mate, mateNeeds, matePhenotype, mateReproduction, reproductionEnabled, tick, out diagnostics, economicsEnabled, threatFalloffDistance, otherCandidates, multiThreatPerceptionEnabled, restBehaviorEnabled, selfId, selfLineage, otherLineage, kinRecognitionEnabled, plantQualityPreferenceEnabled, safetyGatedMateRendezvousEnabled, homeRange, homeRangeAffinityEnabled, climate, reproductionNeedFraction`. That is the list Task 5 forwards.

```powershell
# (l) the hunting-strategy thresholds test 4 relies on
Select-String -Path Assets/Scripts/Simulation/Behavior/PredationSystem.cs -Pattern "MinimumHuntingDiet = 0.58f;|MinimumHuntingAggression = 0.35f;"
```
Expected: two lines. (`MeatYieldMultiplier = 0.5 + DietSpecialization` in `Phenotype.FromGenome`, so the diet threshold is the gene value directly.)

- [ ] **Step 4: Report readiness**

State in chat: HEAD, branch, suite count 769, and that all twelve checks (a)–(l) matched. Do not proceed if any did not.

---

### Task 2: `SimulationV2Ruleset`, `PredationMode`, `IntentUtilityV2`, and the constructor seam

**Files:**
- Create: `Assets/Scripts/Simulation/Core/SimulationV2Ruleset.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (enum member; last constructor parameter; assignment; property)
- Modify: `Assets/Tests/EditMode/StateFingerprintTests.cs` (`PinnedConfigurationPropertyCount` 66 → 67, nothing else)
- Create: `Assets/Tests/EditMode/DecisionV2SeamTests.cs` (test 7 only in this task; later tasks append the other eight)

**Interfaces:**
- Produces, in namespace `LifeSimulation.Simulation.Core`:
  - `public enum PredationMode : byte { Unspecified = 0, Disabled = 1, Enabled = 2 }`
  - `public readonly struct SimulationV2Ruleset` with `public int SchemaVersion { get; }`, `public PredationMode PredationMode { get; }`, `public static SimulationV2Ruleset Schema1(PredationMode predationMode)`, `public void Validate(DecisionPolicyVersion policy)`, `public ulong HashInto(ulong hash)`, and a `private` field-setting constructor.
  - `DecisionPolicyVersion.IntentUtilityV2 = 2`.
  - `SimulationConfig` constructor parameter `SimulationV2Ruleset simulationV2Ruleset = default` (last), property `public SimulationV2Ruleset SimulationV2Ruleset { get; }`.
- `Validate` and `HashInto` are written complete in this task; they are wired into `SimulationConfig` in Task 3.

- [ ] **Step 1: Write the test file with test 7**

Create `Assets/Tests/EditMode/DecisionV2SeamTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// P1 of the V2 seam (docs/superpowers/specs/2026-09-11-v2-ruleset-seam-design.md §6.2):
    /// the explicit <c>IntentUtilityV2</c> policy, the typed <c>SimulationV2Ruleset</c>, and the
    /// predation switch decoupled from <c>FounderProfile</c>.
    ///
    /// <para>V1 reference values come from <see cref="PolicyVersionFreezeTests"/>'s
    /// <c>internal</c> members and from live V1 runs, never from literals restated here. The only
    /// literals in this file are the two V2 configuration-hash pins of
    /// <see cref="Schema1HashAndEnumValuesArePinned"/>, which no V1 test can supply.</para>
    ///
    /// <para><c>SimulationConfig</c> has no copy method. <see cref="AsV2"/> rebuilds a
    /// configuration through the single public constructor by reflection, filling each argument
    /// from the property of the same PascalCase name — the convention
    /// <c>FlagLivenessAnalysis.BuildArguments</c> already relies on, re-implemented here because
    /// that method is private — and overriding exactly the policy and the ruleset.</para>
    /// </summary>
    public sealed class DecisionV2SeamTests
    {
        [Test]
        public void RulesetExposesNoBooleans()
        {
            Type ruleset = typeof(SimulationV2Ruleset);
            const BindingFlags Everything = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            foreach (FieldInfo field in ruleset.GetFields(Everything))
            {
                Assert.That(field.FieldType, Is.Not.EqualTo(typeof(bool)), $"field {field.Name} is a bool");
            }

            foreach (PropertyInfo property in ruleset.GetProperties(Everything))
            {
                Assert.That(property.PropertyType, Is.Not.EqualTo(typeof(bool)), $"property {property.Name} is a bool");
            }

            foreach (ConstructorInfo constructor in ruleset.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.That(constructor.GetParameters(), Is.Empty, "the ruleset must expose no public instance constructor with parameters");
            }

            ConstructorInfo[] nonPublic = ruleset.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(nonPublic, Has.Length.EqualTo(1), "expected exactly one non-public field-setting constructor");
            Assert.That(nonPublic[0].IsPrivate, Is.True);
            foreach (ParameterInfo parameter in nonPublic[0].GetParameters())
            {
                Assert.That(parameter.ParameterType, Is.Not.EqualTo(typeof(bool)), $"constructor parameter {parameter.Name} is a bool");
            }

            ConstructorInfo configConstructor = typeof(SimulationConfig).GetConstructors()[0];
            ParameterInfo[] parameters = configConstructor.GetParameters();
            ParameterInfo last = parameters[parameters.Length - 1];
            Assert.That(last.Name, Is.EqualTo("simulationV2Ruleset"), "the ruleset must be the last constructor parameter");
            Assert.That(last.ParameterType, Is.EqualTo(ruleset));
            Assert.That(last.HasDefaultValue, Is.True);
            Assert.That(typeof(SimulationConfig).GetConstructors(), Has.Length.EqualTo(1), "no second constructor may be added");
        }
    }
}
```

- [ ] **Step 2: Run it and verify it fails to compile**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests"
```

Expected: build error `error CS0246: The type or namespace name 'SimulationV2Ruleset' could not be found`. That is the red state.

- [ ] **Step 3: Create the ruleset file**

Create `Assets/Scripts/Simulation/Core/SimulationV2Ruleset.cs`:

```csharp
using System;

namespace LifeSimulation.Simulation.Core
{
    /// <summary>
    /// Whether the <c>IntentUtilityV2</c> policy runs the predation mechanism. Under V1 this was
    /// implied by <see cref="FounderProfile.PredationVariation"/>, which made "predation on with
    /// physiology-varied founders" unconstructible; V2 reads only this value. Append-only:
    /// a value that has appeared in a committed experiment is never removed or renumbered, and
    /// <see cref="Unspecified"/> is reserved so that a <c>default</c> ruleset is rejected under V2.
    /// </summary>
    public enum PredationMode : byte
    {
        Unspecified = 0,
        Disabled = 1,
        Enabled = 2,
    }

    /// <summary>
    /// The V2-only settings of a <see cref="SimulationConfig"/>: mechanism versions as closed
    /// enums, never booleans and never tuning floats. Schema-versioned so that a later field
    /// cannot change the validation or the hash of a ruleset built today: schema N's factory,
    /// validation branch and hash branch are frozen once committed, and a new field means
    /// schema N+1 with its own factory. Schema 0 is <c>default</c>, the only value valid under
    /// Legacy and V1 and never valid under V2.
    /// </summary>
    public readonly struct SimulationV2Ruleset
    {
        /// <summary>0 for <c>default</c>; otherwise the schema the instance was built by.</summary>
        public int SchemaVersion { get; }

        public PredationMode PredationMode { get; }

        private SimulationV2Ruleset(int schemaVersion, PredationMode predationMode)
        {
            SchemaVersion = schemaVersion;
            PredationMode = predationMode;
        }

        /// <summary>Schema 1: <see cref="PredationMode"/> only. Frozen.</summary>
        public static SimulationV2Ruleset Schema1(PredationMode predationMode)
        {
            return new SimulationV2Ruleset(1, predationMode);
        }

        /// <summary>
        /// Rejects every composition the policy cannot run: a V2 policy with schema 0, an
        /// unspecified or undefined field for the instance's schema, or a schema this build does
        /// not define; and a Legacy or V1 policy with any schema but 0. Called from
        /// <see cref="SimulationConfig.Validate"/>, so an invalid pairing fails at world
        /// construction rather than silently selecting a mechanism.
        /// </summary>
        public void Validate(DecisionPolicyVersion policy)
        {
            if (policy != DecisionPolicyVersion.IntentUtilityV2)
            {
                if (SchemaVersion != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(SchemaVersion), SchemaVersion, "A V2 ruleset is only valid under the IntentUtilityV2 policy.");
                }

                return;
            }

            if (SchemaVersion == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(SchemaVersion), SchemaVersion, "The IntentUtilityV2 policy requires an explicit ruleset schema; default is not one.");
            }

            if (SchemaVersion != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(SchemaVersion), SchemaVersion, "Unknown V2 ruleset schema.");
            }

            // Schema 1 -- frozen. Do not edit this branch when adding schema 2.
            if (!Enum.IsDefined(typeof(PredationMode), PredationMode))
            {
                throw new ArgumentOutOfRangeException(nameof(PredationMode), PredationMode, "Not a defined PredationMode.");
            }

            if (PredationMode == PredationMode.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(PredationMode), PredationMode, "Schema 1 requires an explicit PredationMode.");
            }
        }

        /// <summary>
        /// Folds the schema number and then exactly that schema's fields, in declaration order,
        /// into a running FNV-1a hash. The step is the same as <c>SimulationConfig</c>'s private
        /// one, repeated here because the struct must hash itself without widening that class's
        /// surface. Adding schema 2 adds a branch; it never edits the schema-1 branch.
        /// </summary>
        public ulong HashInto(ulong hash)
        {
            hash = Mix(hash, unchecked((ulong)SchemaVersion));
            if (SchemaVersion == 1)
            {
                hash = Mix(hash, unchecked((ulong)(int)PredationMode));
            }

            return hash;
        }

        private static ulong Mix(ulong hash, ulong value)
        {
            return (hash ^ value) * 1099511628211UL;
        }
    }
}
```

- [ ] **Step 4: Add the enum value, the constructor parameter, the assignment and the property to `SimulationConfig`**

In `Assets/Scripts/Simulation/Core/SimulationConfig.cs`:

(a) The enum, lines 13–17, becomes:

```csharp
    public enum DecisionPolicyVersion : byte
    {
        Legacy = 0,
        IntentUtilityV1 = 1,
        /// <summary>The corrected path behind <see cref="SimulationV2Ruleset"/>. Headless-only until the defense loop is proven.</summary>
        IntentUtilityV2 = 2,
    }
```

(b) The constructor's last parameter (line 178) `int generatedPlantSiteAnchorCount = DefaultGeneratedPlantSiteAnchorCount)` becomes:

```csharp
            int generatedPlantSiteAnchorCount = DefaultGeneratedPlantSiteAnchorCount,
            SimulationV2Ruleset simulationV2Ruleset = default)
```

Appended at the very end so every existing positional call remains valid unchanged. No other constructor is added.

(c) In the constructor body, directly after `HomeRangeAffinityEnabled = homeRangeAffinityEnabled;` (the last assignment, line 260), add:

```csharp
            SimulationV2Ruleset = simulationV2Ruleset;
```

(d) After `public bool HomeRangeAffinityEnabled { get; }` (line 294), add:

```csharp

        /// <summary>
        /// V2-only settings. <c>default</c> (schema 0) under Legacy and V1; a named schema under
        /// <see cref="DecisionPolicyVersion.IntentUtilityV2"/>. Hashed only when the policy is V2,
        /// so Legacy and V1 configuration hashes are untouched. See the struct for the
        /// versioning contract.
        /// </summary>
        public SimulationV2Ruleset SimulationV2Ruleset { get; }
```

The property name is the PascalCase form of the parameter name, which `FlagLivenessAnalysis.BuildArguments` and the test helper of Task 3 both depend on.

- [ ] **Step 5: Update the pinned property count**

In `Assets/Tests/EditMode/StateFingerprintTests.cs` line 272, change

```csharp
            const int PinnedConfigurationPropertyCount = 66;
```

to

```csharp
            const int PinnedConfigurationPropertyCount = 67;
```

That test's own comment instructs exactly this when a public instance property is added. Nothing else in the file changes.

- [ ] **Step 6: Run the new test, the fingerprint tests and the liveness tests**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests|FullyQualifiedName~StateFingerprintTests|FullyQualifiedName~LivenessTests"
```

Expected: all pass — `RulesetExposesNoBooleans`, every `StateFingerprintTests` test including `EveryConfigurationBoolConstructorParameterChangesTheConfigurationHash` (reflection reports `null` for the `= default` struct parameter and `ConstructorInfo.Invoke` converts it to `default(SimulationV2Ruleset)`) and `ConfigurationHashCoverageMatchesThePinnedPropertyCount` at 67, and every `LivenessTests` test (`EveryConfigFlagIsCoveredByTheLivenessSweep` still counts the same `bool` parameters; `BuildArguments` finds the `SimulationV2Ruleset` property). **If the bool-constructor test fails: stop** (spec §9); do not edit it.

- [ ] **Step 7: Run the P0 pins and the Legacy pin**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~PolicyVersionFreezeTests|FullyQualifiedName~CoreSimulationTests"
```

Expected: all pass. The constructor parameter defaults to schema 0 and nothing reads it yet, so no hash can have moved. Any failure: stop.

- [ ] **Step 8: Commit**

```powershell
git add Assets/Scripts/Simulation/Core/SimulationV2Ruleset.cs Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Tests/EditMode/StateFingerprintTests.cs Assets/Tests/EditMode/DecisionV2SeamTests.cs
git diff --check --cached
git diff --cached --stat
git commit -m "feat: add SimulationV2Ruleset, PredationMode and the IntentUtilityV2 policy value" -m "P1 of the V2 seam spec, first step. The ruleset is a schema-versioned readonly struct with a private constructor and one factory, Schema1(PredationMode); it is the last, defaulted parameter of the SimulationConfig constructor and a property of the same name. Validate and HashInto are complete but not yet called. No hash moves: the parameter defaults to schema 0 and nothing reads it. PinnedConfigurationPropertyCount 66 -> 67 per that test's own rule." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
git status --porcelain
```

Expected: `diff --check` prints nothing; the stat lists exactly four files; `status` empty.

---

### Task 3: Validation, the conditional configuration hash, the `AsV2` helper, and tests 5, 6 and 9

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs` (`Validate()`: one call; `ComputeConfigurationHash()`: one appended block)
- Modify: `Assets/Tests/EditMode/DecisionV2SeamTests.cs` (append `AsV2`, `Rebuild`, and tests 5, 6, 9)

**Interfaces:**
- Consumes: Task 2's struct, enum value, parameter and property; P0's `PolicyVersionFreezeTests.FreezeSeed` and `RecordedPredationCell(int)`.
- Produces, test-local and `private static`: `SimulationConfig Rebuild(SimulationConfig source, DecisionPolicyVersion policy, SimulationV2Ruleset ruleset)` and `SimulationConfig AsV2(SimulationConfig source, SimulationV2Ruleset ruleset)`; `SimulationConfig PinA()`, `PinB()`, `PinC()` returning the P0 configurations; `const int FreezeFounders = 12`. Tasks 4–6 use all of them.

- [ ] **Step 1: Append the helpers and tests 5, 6 and 9 to the test class**

Inside `DecisionV2SeamTests`, before `RulesetExposesNoBooleans`, add the helpers and the pin accessors. `FreezeFounders` is 12 because pins A and B are `Create…Defaults(42, 12)` in P0 and P0 keeps that constant private.

```csharp
        private const int FreezeFounders = 12;

        /// <summary>Two V2 configuration-hash pins: the executable form of spec §4.2. A future
        /// schema 2 must keep both green without editing them.</summary>
        private const ulong PinnedV2EnabledConfigurationHash = ulong.MaxValue;   // sentinel until Task 3 step 9
        private const ulong PinnedV2DisabledConfigurationHash = ulong.MaxValue;  // sentinel until Task 3 step 9

        private static SimulationConfig PinA() => SimulationConfig.CreatePrototype4Defaults(PolicyVersionFreezeTests.FreezeSeed, FreezeFounders);
        private static SimulationConfig PinB() => SimulationConfig.CreateFullEcosystemDefaults(PolicyVersionFreezeTests.FreezeSeed, FreezeFounders);
        private static SimulationConfig PinC() => PolicyVersionFreezeTests.RecordedPredationCell(PolicyVersionFreezeTests.FreezeSeed);

        /// <summary>
        /// Rebuilds <paramref name="source"/> through the single public constructor, each argument
        /// taken from the property of the same PascalCase name, overriding exactly the policy and
        /// the ruleset. A parameter with no matching property is an error, never a default: a
        /// silently defaulted argument is how the presenter's hand-kept replicas drifted.
        /// </summary>
        private static SimulationConfig Rebuild(SimulationConfig source, DecisionPolicyVersion policy, SimulationV2Ruleset ruleset)
        {
            ConstructorInfo constructor = typeof(SimulationConfig).GetConstructors()[0];
            ParameterInfo[] parameters = constructor.GetParameters();
            var arguments = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                string parameterName = parameters[index].Name;
                if (parameterName == "decisionPolicyVersion")
                {
                    arguments[index] = policy;
                    continue;
                }

                if (parameterName == "simulationV2Ruleset")
                {
                    arguments[index] = ruleset;
                    continue;
                }

                string propertyName = char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);
                PropertyInfo property = typeof(SimulationConfig).GetProperty(propertyName);
                if (property == null)
                {
                    throw new InvalidOperationException($"SimulationConfig parameter '{parameterName}' has no property '{propertyName}'; refusing to default it.");
                }

                arguments[index] = property.GetValue(source);
            }

            return (SimulationConfig)constructor.Invoke(arguments);
        }

        private static SimulationConfig AsV2(SimulationConfig source, SimulationV2Ruleset ruleset)
        {
            return Rebuild(source, DecisionPolicyVersion.IntentUtilityV2, ruleset);
        }
```

Then add the three tests after `RulesetExposesNoBooleans`:

```csharp
        [Test]
        public void ConfigurationHashDistinguishesV2FromV1()
        {
            SimulationConfig[] pins = { PinA(), PinB(), PinC() };
            string[] names = { "A", "B", "C" };
            for (int index = 0; index < pins.Length; index++)
            {
                ulong v1 = pins[index].ComputeConfigurationHash();
                ulong enabled = AsV2(pins[index], SimulationV2Ruleset.Schema1(PredationMode.Enabled)).ComputeConfigurationHash();
                ulong disabled = AsV2(pins[index], SimulationV2Ruleset.Schema1(PredationMode.Disabled)).ComputeConfigurationHash();

                Assert.That(enabled, Is.Not.EqualTo(v1), $"pin {names[index]}: V2 Enabled must not hash like V1");
                Assert.That(disabled, Is.Not.EqualTo(v1), $"pin {names[index]}: V2 Disabled must not hash like V1");
                Assert.That(enabled, Is.Not.EqualTo(disabled), $"pin {names[index]}: the ruleset field must reach the hash");
            }
        }

        [Test]
        public void ValidateRejectsInconsistentPolicyAndRuleset()
        {
            SimulationConfig pinA = PinA();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => Rebuild(pinA, DecisionPolicyVersion.IntentUtilityV2, default).Validate(),
                "V2 with schema 0");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => AsV2(pinA, SimulationV2Ruleset.Schema1(PredationMode.Unspecified)).Validate(),
                "V2 with an unspecified mode");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => AsV2(pinA, SimulationV2Ruleset.Schema1((PredationMode)255)).Validate(),
                "V2 with an undefined mode value");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Rebuild(pinA, DecisionPolicyVersion.Legacy, SimulationV2Ruleset.Schema1(PredationMode.Enabled)).Validate(),
                "Legacy with a schema-1 ruleset");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Rebuild(pinA, DecisionPolicyVersion.IntentUtilityV1, SimulationV2Ruleset.Schema1(PredationMode.Enabled)).Validate(),
                "V1 with a schema-1 ruleset");

            // The same rejections reach world construction, which is where a real run would hit them.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SimulationWorld(Rebuild(pinA, DecisionPolicyVersion.IntentUtilityV2, default)));

            Assert.DoesNotThrow(() => AsV2(pinA, SimulationV2Ruleset.Schema1(PredationMode.Enabled)).Validate());
            Assert.DoesNotThrow(() => AsV2(pinA, SimulationV2Ruleset.Schema1(PredationMode.Disabled)).Validate());
            Assert.DoesNotThrow(() => pinA.Validate(), "V1 with the default ruleset is unchanged");
        }

        [Test]
        public void Schema1HashAndEnumValuesArePinned()
        {
            Assert.That(Enum.GetNames(typeof(PredationMode)), Is.EqualTo(new[] { "Unspecified", "Disabled", "Enabled" }));
            Assert.That((byte)PredationMode.Unspecified, Is.EqualTo(0));
            Assert.That((byte)PredationMode.Disabled, Is.EqualTo(1));
            Assert.That((byte)PredationMode.Enabled, Is.EqualTo(2));

            SimulationV2Ruleset enabled = SimulationV2Ruleset.Schema1(PredationMode.Enabled);
            Assert.That(enabled.SchemaVersion, Is.EqualTo(1));
            Assert.That(enabled.PredationMode, Is.EqualTo(PredationMode.Enabled));
            Assert.DoesNotThrow(() => enabled.Validate(DecisionPolicyVersion.IntentUtilityV2));
            Assert.That(default(SimulationV2Ruleset).SchemaVersion, Is.EqualTo(0));

            Assert.Multiple(() =>
            {
                Assert.That(AsV2(PinA(), SimulationV2Ruleset.Schema1(PredationMode.Enabled)).ComputeConfigurationHash(),
                    Is.EqualTo(PinnedV2EnabledConfigurationHash), "pin A, V2 Enabled, configuration hash");
                Assert.That(AsV2(PinA(), SimulationV2Ruleset.Schema1(PredationMode.Disabled)).ComputeConfigurationHash(),
                    Is.EqualTo(PinnedV2DisabledConfigurationHash), "pin A, V2 Disabled, configuration hash");
            });
        }
```

- [ ] **Step 2: Run the three new tests and verify they fail for the right reasons**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests"
```

Expected: `RulesetExposesNoBooleans` passes; the three new tests fail:

- `ConfigurationHashDistinguishesV2FromV1`: fails on the third assertion for pin A — `the ruleset field must reach the hash` — because the policy value already separates V2 from V1 but the ruleset is not hashed yet.
- `ValidateRejectsInconsistentPolicyAndRuleset`: fails on the first `Assert.Throws` — `V2 with schema 0` — because `SimulationConfig.Validate` does not call the ruleset yet.
- `Schema1HashAndEnumValuesArePinned`: the enum assertions pass; `Assert.Multiple` reports two mismatches, each `Expected: 18446744073709551615 But was: <actual>`. Those two actuals are **not** yet the values to pin — the hash block does not exist yet.

If `ConfigurationHashDistinguishesV2FromV1` fails on the first or second assertion instead, the policy value is not reaching the hash: stop and report.

- [ ] **Step 3: Wire `Validate`**

In `SimulationConfig.Validate()`, directly after the existing

```csharp
            if (!Enum.IsDefined(typeof(DecisionPolicyVersion), DecisionPolicyVersion))
            {
                throw new ArgumentOutOfRangeException(nameof(DecisionPolicyVersion));
            }
```

add:

```csharp

            SimulationV2Ruleset.Validate(DecisionPolicyVersion);
```

- [ ] **Step 4: Append the conditional hash block**

In `SimulationConfig.ComputeConfigurationHash()`, after the last existing line

```csharp
            hash = Hash(hash, unchecked((ulong)GeneratedPlantSiteAnchorCount));
```

and before `return hash;`, add:

```csharp

            // V2-only fields enter the hash only under the V2 policy, so every Legacy and V1
            // configuration executes exactly the sequence above and keeps its recorded hash.
            // ConfigurationHashVersion stays 9 for the same reason; SchemaVersion inside HashInto
            // is the V2 field-set discriminator.
            if (DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV2)
            {
                hash = SimulationV2Ruleset.HashInto(hash);
            }
```

`ConfigurationHashVersion` is not changed.

- [ ] **Step 5: Run the seam tests again**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests"
```

Expected: `RulesetExposesNoBooleans`, `ConfigurationHashDistinguishesV2FromV1` and `ValidateRejectsInconsistentPolicyAndRuleset` pass. `Schema1HashAndEnumValuesArePinned` fails with exactly two lines under `Assert.Multiple`:

```
  pin A, V2 Enabled, configuration hash
  Expected: 18446744073709551615
  But was:  <ENABLED_ACTUAL>
  pin A, V2 Disabled, configuration hash
  Expected: 18446744073709551615
  But was:  <DISABLED_ACTUAL>
```

Write both actuals down. They must differ from each other.

- [ ] **Step 6: Confirm the two actuals are stable and not a V1 value**

Run the same filter a second time and confirm the same two numbers. Then confirm neither equals P0's pin-A configuration hash: open `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`, read `CapturedAConfiguration`, and check both actuals differ from it. (They must, since `ConfigurationHashDistinguishesV2FromV1` passed; this is the reviewer's eyeball check.)

- [ ] **Step 7: Run the P0 pins and the Legacy pin**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~PolicyVersionFreezeTests|FullyQualifiedName~CoreSimulationTests|FullyQualifiedName~StateFingerprintTests"
```

Expected: all pass. The new block is skipped under Legacy and V1. Any failure: stop; a V1 hash moved.

- [ ] **Step 8: Verify the hash block is the only edit to `ComputeConfigurationHash`**

```powershell
git diff -U0 -- Assets/Scripts/Simulation/Core/SimulationConfig.cs
```

Expected: exactly two hunks — the `SimulationV2Ruleset.Validate(DecisionPolicyVersion);` insertion and the conditional block insertion. No removed line (`-` lines) anywhere. If a hunk touches an existing hash line: stop.

- [ ] **Step 9: Replace the two sentinels with the captured values**

In `DecisionV2SeamTests.cs`, replace

```csharp
        private const ulong PinnedV2EnabledConfigurationHash = ulong.MaxValue;   // sentinel until Task 3 step 9
        private const ulong PinnedV2DisabledConfigurationHash = ulong.MaxValue;  // sentinel until Task 3 step 9
```

with the two captured numbers from step 5, each with a `UL` suffix, and drop the sentinel comments:

```csharp
        private const ulong PinnedV2EnabledConfigurationHash = <ENABLED_ACTUAL>UL;
        private const ulong PinnedV2DisabledConfigurationHash = <DISABLED_ACTUAL>UL;
```

These are the only two hash literals in the file.

- [ ] **Step 10: Run the seam tests and verify all four pass**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests"
```

Expected: 4 passed, 0 failed.

- [ ] **Step 11: Commit**

```powershell
git add Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Tests/EditMode/DecisionV2SeamTests.cs
git diff --check --cached
git diff --cached --stat
git commit -m "feat: validate and hash the V2 ruleset only under the IntentUtilityV2 policy" -m "SimulationConfig.Validate now calls SimulationV2Ruleset.Validate(policy), so V2 with a default ruleset, an unspecified or undefined PredationMode, or Legacy/V1 with a schema-1 ruleset all fail at world construction. ComputeConfigurationHash appends HashInto after the last existing field, inside a V2-only branch; ConfigurationHashVersion stays 9 and every Legacy/V1 hash executes the old sequence verbatim. Tests: the AsV2 reflection helper, ConfigurationHashDistinguishesV2FromV1, ValidateRejectsInconsistentPolicyAndRuleset, and Schema1HashAndEnumValuesArePinned with two captured V2 literals." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
git status --porcelain
```

Expected: `diff --check` prints nothing; two files; `status` empty.

---

### Task 4: Manifest schema 2 for V2 configurations, and test 8

**Files:**
- Modify: `Assets/Scripts/Simulation/Experiments/ExperimentManifest.cs`
- Modify: `Assets/Tests/EditMode/DecisionV2SeamTests.cs` (append test 8)

**Interfaces:**
- Consumes: `AsV2`, `PinC()` from Task 3; P0's `PinCRevision`, `RecordedPredationScenario`, `RecordedPredationCell`, `FreezeSeed`, `PinCSeedCount`, `PinCTicks`, `PinCCanonicalManifest`.
- Produces: `ExperimentManifest.V2SchemaVersion = 2`; V2 configurations emit `schema=2`, the schema-1 field set in the same order, then `V2RulesetSchema=<n>` and `PredationMode=<name>`.

- [ ] **Step 1: Append test 8**

After `Schema1HashAndEnumValuesArePinned`, add:

```csharp
        /// <summary>Splits a manifest into its non-empty lines, preserving order.</summary>
        private static List<string> ManifestLines(string manifest)
        {
            var lines = new List<string>();
            foreach (string line in manifest.Split('\n'))
            {
                if (line.Length > 0) lines.Add(line);
            }

            return lines;
        }

        private static bool IsVersionedLine(string line)
        {
            return line.StartsWith("schema=", StringComparison.Ordinal)
                || line.StartsWith("DecisionPolicyVersion=", StringComparison.Ordinal)
                || line.StartsWith("V2RulesetSchema=", StringComparison.Ordinal)
                || line.StartsWith("PredationMode=", StringComparison.Ordinal);
        }

        [Test]
        public void ManifestSchemaIsVersionedByPolicy()
        {
            string v1 = ExperimentManifest.Describe(
                PolicyVersionFreezeTests.PinCRevision,
                PolicyVersionFreezeTests.RecordedPredationScenario,
                PinC(),
                PolicyVersionFreezeTests.FreezeSeed,
                PolicyVersionFreezeTests.PinCSeedCount,
                PolicyVersionFreezeTests.PinCTicks);

            // V1 unchanged: the complete canonical text, byte for byte, no normalisation.
            Assert.That(v1, Is.EqualTo(PolicyVersionFreezeTests.PinCCanonicalManifest));

            string v2 = ExperimentManifest.Describe(
                PolicyVersionFreezeTests.PinCRevision,
                PolicyVersionFreezeTests.RecordedPredationScenario,
                AsV2(PinC(), SimulationV2Ruleset.Schema1(PredationMode.Enabled)),
                PolicyVersionFreezeTests.FreezeSeed,
                PolicyVersionFreezeTests.PinCSeedCount,
                PolicyVersionFreezeTests.PinCTicks);

            List<string> v1Lines = ManifestLines(v1);
            List<string> v2Lines = ManifestLines(v2);

            // V2 versioned: exactly these lines change or appear, in these positions.
            Assert.That(v1Lines[0], Is.EqualTo("schema=1"));
            Assert.That(v2Lines[0], Is.EqualTo("schema=2"));
            Assert.That(v1Lines, Does.Contain("DecisionPolicyVersion=IntentUtilityV1"));
            Assert.That(v2Lines, Does.Contain("DecisionPolicyVersion=IntentUtilityV2"));
            Assert.That(v1Lines.IndexOf("DecisionPolicyVersion=IntentUtilityV1"), Is.EqualTo(v2Lines.IndexOf("DecisionPolicyVersion=IntentUtilityV2")), "the policy line keeps its position");
            Assert.That(v2Lines.Count, Is.EqualTo(v1Lines.Count + 2));
            Assert.That(v2Lines[v2Lines.Count - 2], Is.EqualTo("V2RulesetSchema=1"));
            Assert.That(v2Lines[v2Lines.Count - 1], Is.EqualTo("PredationMode=Enabled"));
            Assert.That(v1, Does.Not.Contain("V2RulesetSchema="));
            Assert.That(v1, Does.Not.Contain("PredationMode="));

            // After removing the changed and added lines from both, everything left is identical
            // in content and order.
            var v1Remaining = new List<string>();
            var v2Remaining = new List<string>();
            int v1Removed = 0;
            int v2Removed = 0;
            for (int index = 0; index < v1Lines.Count; index++)
            {
                if (IsVersionedLine(v1Lines[index])) v1Removed++; else v1Remaining.Add(v1Lines[index]);
            }

            for (int index = 0; index < v2Lines.Count; index++)
            {
                if (IsVersionedLine(v2Lines[index])) v2Removed++; else v2Remaining.Add(v2Lines[index]);
            }

            Assert.That(v1Removed, Is.EqualTo(2), "V1 carries only schema and DecisionPolicyVersion among the versioned lines");
            Assert.That(v2Removed, Is.EqualTo(4), "V2 carries schema, DecisionPolicyVersion, V2RulesetSchema and PredationMode");
            Assert.That(v2Remaining, Is.EqualTo(v1Remaining));
        }
```

- [ ] **Step 2: Run it and verify it fails on the V2 half**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests.ManifestSchemaIsVersionedByPolicy"
```

Expected: fails at `Assert.That(v2Lines[0], Is.EqualTo("schema=2"))` with `But was: "schema=1"`. The V1 half — the byte-for-byte comparison against `PinCCanonicalManifest` — must already pass; if it does not, stop: the manifest or P0's constant moved before this task touched anything.

- [ ] **Step 3: Implement the V2 branch in `Describe`**

In `Assets/Scripts/Simulation/Experiments/ExperimentManifest.cs`:

(a) After `public const int SchemaVersion = 1;` add:

```csharp

        /// <summary>
        /// Emitted instead of <see cref="SchemaVersion"/> for an <c>IntentUtilityV2</c>
        /// configuration: the schema-1 field set in the same order, then the ruleset's own
        /// schema number and one line per field of that schema. Legacy and V1 keep emitting
        /// schema 1 byte-identically; a V2 result must never be mistaken for one.
        /// </summary>
        public const int V2SchemaVersion = 2;
```

(b) Replace the first emitted line

```csharp
            Line(builder, "schema", SchemaVersion.ToString(CultureInfo.InvariantCulture));
```

with

```csharp
            bool isV2 = config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV2;
            Line(builder, "schema", (isV2 ? V2SchemaVersion : SchemaVersion).ToString(CultureInfo.InvariantCulture));
```

(c) After the last existing line

```csharp
            Line(builder, "ThreatFalloffDistance", config.ThreatFalloffDistance.ToString("R", CultureInfo.InvariantCulture));
```

and before `return builder.ToString();`, add:

```csharp
            if (isV2)
            {
                SimulationV2Ruleset ruleset = config.SimulationV2Ruleset;
                Line(builder, "V2RulesetSchema", ruleset.SchemaVersion.ToString(CultureInfo.InvariantCulture));
                Line(builder, "PredationMode", ruleset.PredationMode.ToString());
            }
```

Nothing between those two points changes. The `config == null` check above already guarantees `config` is non-null before `isV2` is read.

- [ ] **Step 4: Run the seam tests and the manifest tests**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests|FullyQualifiedName~ExperimentManifestTests|FullyQualifiedName~PolicyVersionFreezeTests.PinC_CanonicalManifestUnderTheFixedRevision"
```

Expected: all pass — five seam tests, every `ExperimentManifestTests` test unchanged, and P0's canonical-manifest pin.

- [ ] **Step 5: Commit**

```powershell
git add Assets/Scripts/Simulation/Experiments/ExperimentManifest.cs Assets/Tests/EditMode/DecisionV2SeamTests.cs
git diff --check --cached
git diff --cached --stat
git commit -m "feat: emit manifest schema 2 with the ruleset lines for IntentUtilityV2 configurations" -m "Legacy and V1 manifests are byte-identical to before (schema=1, same lines). A V2 configuration emits schema=2, the same field set in the same order, then V2RulesetSchema and one line per field of that ruleset schema. ManifestSchemaIsVersionedByPolicy pins P0's canonical pin-C text verbatim and proves the V2 text differs in exactly the four versioned lines." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
git status --porcelain
```

Expected: `diff --check` prints nothing; two files; `status` empty.

---

### Task 5: The V2 dispatch and delegate, and tests 1, 2 and 3

**Files:**
- Create: `Assets/Scripts/Simulation/Behavior/DecisionSystem.V2.cs`
- Create: `Assets/Scripts/Simulation/Core/SimulationWorld.DecisionsV2.cs`
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.Ticking.cs` (one `if` inserted at the head of the policy chain, line 344 at `d7e433b`; nothing else)
- Modify: `Assets/Tests/EditMode/DecisionV2SeamTests.cs` (append a run loop and tests 1, 2, 3)

**Interfaces:**
- Consumes: `AsV2`, `PinA/B/C()` from Task 3; P0's `RecordedPredationScenario`, `PinCTicks`; `Prototype4Scenarios.ConsumerDefenseCalibrationModerate`.
- Produces:
  - `DecisionSystem.DecideIntentUtilityV2(...)` — same parameter list as the full V1 overload (Task 1 step 3(k)), every parameter required, forwarding to `DecideIntentUtilityV1`.
  - `SimulationWorld.DecideV2(int index, long tick, MovementState movement, Phenotype phenotype, CreatureLineage selfLineage, ResourceObservation carcass, PredationCandidateBuffer otherCandidates, out DecisionDiagnostics diagnostics)` — `private`, in the new partial.
  - Test-local `private static SimulationWorld RunSettled(SimulationConfig config, SimulationScenario scenario, int ticks)` and `const int DefaultTicks = 2000`.

- [ ] **Step 1: Append the run loop and tests 1, 2 and 3**

Add the loop next to the other helpers. It mirrors P0's private `RunSettled` — `Step`, then `Events.Clear()`, per tick — so a V2 world and the P0 pin world run the same loop:

```csharp
        private const int DefaultTicks = 2000;

        /// <summary>The sweep tools' loop, and P0's: Step, then clear the event buffer, per tick.</summary>
        private static SimulationWorld RunSettled(SimulationConfig config, SimulationScenario scenario, int ticks)
        {
            var world = new SimulationWorld(config);
            scenario.ApplyTo(world);
            for (int tick = 0; tick < ticks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            return world;
        }
```

Then add the three tests after `ManifestSchemaIsVersionedByPolicy`:

```csharp
        [Test]
        public void V2Disabled_MatchesV1_WithoutPredation()
        {
            SimulationConfig[] pins = { PinA(), PinB() };
            string[] names = { "A", "B" };
            for (int index = 0; index < pins.Length; index++)
            {
                SimulationWorld v1 = RunSettled(pins[index], Prototype4Scenarios.ConsumerDefenseCalibrationModerate, DefaultTicks);
                SimulationWorld v2 = RunSettled(
                    AsV2(pins[index], SimulationV2Ruleset.Schema1(PredationMode.Disabled)),
                    Prototype4Scenarios.ConsumerDefenseCalibrationModerate,
                    DefaultTicks);

                Assert.Multiple(() =>
                {
                    Assert.That(v2.ComputeStateHash(), Is.EqualTo(v1.ComputeStateHash()), $"pin {names[index]}: state hash");
                    Assert.That(v2.ComputeBehaviorHash(), Is.EqualTo(v1.ComputeBehaviorHash()), $"pin {names[index]}: behavior hash");
                });
            }
        }

        [Test]
        public void V2Enabled_MatchesV1_WithPredation()
        {
            SimulationWorld v1 = RunSettled(PinC(), PolicyVersionFreezeTests.RecordedPredationScenario, PolicyVersionFreezeTests.PinCTicks);
            SimulationWorld v2 = RunSettled(
                AsV2(PinC(), SimulationV2Ruleset.Schema1(PredationMode.Enabled)),
                PolicyVersionFreezeTests.RecordedPredationScenario,
                PolicyVersionFreezeTests.PinCTicks);

            Assert.That(v1.CaptureStatistics().AttackHitCount, Is.GreaterThan(0), "V1 pin C must land an attack, or this proves nothing about predation");
            Assert.That(v2.CaptureStatistics().AttackHitCount, Is.GreaterThan(0), "V2 Enabled must land an attack");
            Assert.Multiple(() =>
            {
                Assert.That(v2.ComputeStateHash(), Is.EqualTo(v1.ComputeStateHash()), "state hash");
                Assert.That(v2.ComputeBehaviorHash(), Is.EqualTo(v1.ComputeBehaviorHash()), "behavior hash");
            });
        }

        [Test]
        public void V2Disabled_PredationFoundersCannotActivatePredation()
        {
            SimulationWorld v1 = RunSettled(PinC(), PolicyVersionFreezeTests.RecordedPredationScenario, PolicyVersionFreezeTests.PinCTicks);
            SimulationWorld v2 = RunSettled(
                AsV2(PinC(), SimulationV2Ruleset.Schema1(PredationMode.Disabled)),
                PolicyVersionFreezeTests.RecordedPredationScenario,
                PolicyVersionFreezeTests.PinCTicks);

            SimulationStatistics statistics = v2.CaptureStatistics();
            Assert.Multiple(() =>
            {
                Assert.That(statistics.AttackHitCount, Is.EqualTo(0), "attack hits");
                Assert.That(statistics.PredationDeathCount, Is.EqualTo(0), "predation deaths");
                Assert.That(statistics.FleeDecisionCount, Is.EqualTo(0), "flee decisions");
                Assert.That(v2.ComputeStateHash(), Is.Not.EqualTo(v1.ComputeStateHash()), "state hash must diverge from V1 pin C");
                Assert.That(v2.ComputeBehaviorHash(), Is.Not.EqualTo(v1.ComputeBehaviorHash()), "behavior hash must diverge from V1 pin C");
            });
        }
```

- [ ] **Step 2: Run the three and verify they fail because no V2 path exists**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests.V2"
```

Expected: all three fail. With no dispatch, a V2 configuration falls through the policy chain to `else if (Config.CognitionEnabled)` — `DecideFromLearnedOutcomes`, the Legacy-plus-cognition path — so:

- `V2Disabled_MatchesV1_WithoutPredation`: both hashes differ for pin A.
- `V2Enabled_MatchesV1_WithPredation`: fails at `V2 Enabled must land an attack` (the fall-through path never scores predation) or at the hash comparison.
- `V2Disabled_PredationFoundersCannotActivatePredation`: the counts are zero and the hashes diverge, so this one may **pass** for the wrong reason. That is acceptable at this step; its meaning is established by test 2 passing in step 6 with the same configuration but `Enabled`.

Record which assertions failed.

- [ ] **Step 3: Create the delegate**

Create `Assets/Scripts/Simulation/Behavior/DecisionSystem.V2.cs`:

```csharp
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Behavior
{
    public static partial class DecisionSystem
    {
        /// <summary>
        /// The <c>IntentUtilityV2</c> decision. In P1 of the V2 seam this forwards to the frozen
        /// V1 overload unchanged: V2's only difference from V1 is that the caller derives
        /// <paramref name="predationEnabled"/> from <c>SimulationV2Ruleset.PredationMode</c>
        /// rather than from <c>FounderProfile</c>. Every parameter is required — no defaults —
        /// so a V2 caller can never omit an argument the V1 path receives. Later steps replace
        /// the body with an Observation-fed decision; the signature is the seam.
        /// </summary>
        public static CreatureDecision DecideIntentUtilityV2(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            SimVector2 origin,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            ResourceObservation carcass,
            MemoryState memory,
            bool cognitionEnabled,
            CreatureObservation threat,
            float threatIntensity,
            Phenotype otherPhenotype,
            bool predationEnabled,
            bool physiologyEnabled,
            ReproductionState reproduction,
            CreatureObservation mate,
            CreatureNeeds mateNeeds,
            Phenotype matePhenotype,
            ReproductionState mateReproduction,
            bool reproductionEnabled,
            long tick,
            out DecisionDiagnostics diagnostics,
            bool economicsEnabled,
            float threatFalloffDistance,
            PredationCandidateBuffer otherCandidates,
            bool multiThreatPerceptionEnabled,
            bool restBehaviorEnabled,
            CreatureId selfId,
            CreatureLineage selfLineage,
            CreatureLineage otherLineage,
            bool kinRecognitionEnabled,
            bool plantQualityPreferenceEnabled,
            bool safetyGatedMateRendezvousEnabled,
            HomeRangeState homeRange,
            bool homeRangeAffinityEnabled,
            ClimateField climate,
            float reproductionNeedFraction)
        {
            return DecideIntentUtilityV1(
                needs,
                genome,
                phenotype,
                resources,
                origin,
                foodCandidates,
                waterCandidates,
                carcass,
                memory,
                cognitionEnabled,
                threat,
                threatIntensity,
                otherPhenotype,
                predationEnabled,
                physiologyEnabled,
                reproduction,
                mate,
                mateNeeds,
                matePhenotype,
                mateReproduction,
                reproductionEnabled,
                tick,
                out diagnostics,
                economicsEnabled,
                threatFalloffDistance,
                otherCandidates,
                multiThreatPerceptionEnabled,
                restBehaviorEnabled,
                selfId,
                selfLineage,
                otherLineage,
                kinRecognitionEnabled,
                plantQualityPreferenceEnabled,
                safetyGatedMateRendezvousEnabled,
                homeRange,
                homeRangeAffinityEnabled,
                climate,
                reproductionNeedFraction);
        }
    }
}
```

If the compiler reports a `using` it does not need, remove that line; if it reports a missing type, add the namespace that declares it (check the `using` block of `DecisionSystem.cs`, which is the model). Do not add any other code.

- [ ] **Step 4: Create the world-side V2 partial**

Create `Assets/Scripts/Simulation/Core/SimulationWorld.DecisionsV2.cs`. The body is the V1-only prefix block of `TickDecisions` (`SimulationWorld.Ticking.cs:273-286` at `d7e433b`), the V1 call (`:346-384`) and the V1-branch cognition post-processing (`:385-403`), copied verbatim except for the three marked lines. Copy from the file, not from this document, so the operation order is the source's.

```csharp
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Core
{
    public sealed partial class SimulationWorld
    {
        /// <summary>
        /// The <c>IntentUtilityV2</c> branch of <c>TickDecisions</c>. Everything computed before
        /// the dispatch — nearest food, water and carcass, the multi-threat candidate buffer, the
        /// cognition memory reads and writes — is shared and passed in; nothing is recomputed.
        /// What follows is the V1-only prefix and post-processing of <c>TickDecisions</c>,
        /// replicated verbatim so that V2 is hash-equivalent to V1 in P1, with exactly one
        /// difference: <c>predationEnabled</c> comes from the ruleset, never from
        /// <c>FounderProfile</c>. The nearest other creature and its threat intensity are computed
        /// unconditionally, as V1 does — the V1 gate on them is a tautology — because
        /// <c>ScoreMate</c> reads them even when predation is off.
        /// </summary>
        private CreatureDecision DecideV2(
            int index,
            long tick,
            MovementState movement,
            Phenotype phenotype,
            CreatureLineage selfLineage,
            ResourceObservation carcass,
            PredationCandidateBuffer otherCandidates,
            out DecisionDiagnostics diagnostics)
        {
            var foodCandidates = new ResourceCandidateBuffer();
            var waterCandidates = new ResourceCandidateBuffer();
            PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Food, ref foodCandidates);
            PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Water, ref waterCandidates);
            CreatureObservation other = PerceptionSystem.FindNearestOtherCreature(Creatures, CombatGrid, movement.Position, phenotype.VisionRange, Creatures.GetIdAt(index));
            float threatIntensity = 0f;
            if (other.IsValid)
            {
                threatIntensity = PredationSystem.Threat(Creatures.GetPhenotypeAt(other.CreatureIndex), phenotype, other.Distance, Config.PredationEconomicsEnabled);
            }

            // The one V2 difference in P1. V1 reads FounderProfile == PredationVariation here.
            bool predationEnabled = Config.SimulationV2Ruleset.PredationMode == PredationMode.Enabled;

            CreatureDecision decision = DecisionSystem.DecideIntentUtilityV2(
                Creatures.GetNeedsAt(index),
                Creatures.GetGenomeAt(index),
                phenotype,
                Resources,
                movement.Position,
                foodCandidates,
                waterCandidates,
                carcass,
                Creatures.GetMemoryRefAt(index),
                Config.CognitionEnabled,
                other,
                threatIntensity,
                other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                predationEnabled,
                Config.PhysiologyEnabled,
                Creatures.GetReproductionRefAt(index),
                other,
                other.IsValid ? Creatures.GetNeedsAt(other.CreatureIndex) : default,
                other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                other.IsValid ? Creatures.GetReproductionRefAt(other.CreatureIndex) : default,
                true,
                tick,
                out diagnostics,
                Config.PredationEconomicsEnabled,
                Config.ThreatFalloffDistance,
                otherCandidates,
                Config.MultiThreatPerceptionEnabled,
                Config.RestBehaviorEnabled,
                Creatures.GetIdAt(index),
                selfLineage,
                other.IsValid ? Creatures.GetLineageAt(other.CreatureIndex) : default,
                Config.KinRecognitionEnabled,
                Config.PlantQualityPreferenceEnabled,
                Config.SafetyGatedMateRendezvousEnabled,
                Config.HomeRangeAffinityEnabled ? Creatures.GetHomeRangeRefAt(index) : default,
                Config.HomeRangeAffinityEnabled,
                Climate,
                Config.ReproductionNeedFraction);

            if (Config.CognitionEnabled)
            {
                ref MemoryState memory = ref Creatures.GetMemoryRefAt(index);
                memory.HasActiveRememberedTarget = false;
                if (decision.TargetResourceIndex < 0 && decision.Action == CreatureAction.SeekFood)
                {
                    memory.ActiveRememberedTarget = memory.FoodPosition;
                    memory.HasActiveRememberedTarget = true;
                }
                else if (decision.TargetResourceIndex < 0 && decision.Action == CreatureAction.SeekWater)
                {
                    memory.ActiveRememberedTarget = memory.WaterPosition;
                    memory.HasActiveRememberedTarget = true;
                }
                if (decision.Action == CreatureAction.Flee && other.IsValid)
                {
                    MemorySystem.RememberThreat(ref memory, Creatures.GetMovementAt(other.CreatureIndex).Position);
                }
            }

            return decision;
        }
    }
}
```

Three lines differ from the V1 text and only these: the `predationEnabled` local; the call target `DecideIntentUtilityV2`; and `predationEnabled` in place of `Config.FounderProfile == FounderProfile.PredationVariation` as the fourteenth argument. Everything else — argument order, the ternaries, the `true` for `reproductionEnabled`, the cognition block — is the V1 text. Before continuing, diff by eye against `Ticking.cs:273-286` and `:346-403`.

Fix `using` lines to whatever compiles (`SimVector2`, `ResourceKind`, `PerceptionSystem`, `PredationSystem`, `MemorySystem`, `MemoryState`, `Phenotype` live in the namespaces `Ticking.cs` already imports: `Behavior`, `Biology`, `Resources`, `Environment`, `Spatial`). Add nothing else.

- [ ] **Step 5: Insert the dispatch**

In `Assets/Scripts/Simulation/Core/SimulationWorld.Ticking.cs`, the policy chain at line 344 currently begins:

```csharp
                DecisionDiagnostics diagnostics;
                CreatureDecision decision;
                if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                {
                    decision = DecisionSystem.DecideIntentUtilityV1(
```

Change it to:

```csharp
                DecisionDiagnostics diagnostics;
                CreatureDecision decision;
                if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV2)
                {
                    decision = DecideV2(index, tick, movement, phenotype, selfLineage, carcass, otherCandidates, out diagnostics);
                }
                else if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                {
                    decision = DecisionSystem.DecideIntentUtilityV1(
```

That is the whole edit to this file: one `if` block of four lines inserted, and `if` → `else if` on the existing V1 line. `otherCandidates` is the `PredationCandidateBuffer` built at `:287-297` under `MultiThreatPerceptionEnabled`, passed by value exactly as the V1 call passes it at `:372`; `carcass` is the observation computed at `:258`. Nothing shared is computed twice.

- [ ] **Step 6: Run the seam tests**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests"
```

Expected: 8 passed, 0 failed. If test 1 or 2 fails on a hash: **stop** and report the pin and hash; do not tune. The likely causes, in order of probability, are a transcription difference in `DecideV2` (compare against `Ticking.cs` line by line), or a shared block that turned out to be V1-gated (re-read the table in spec §5 against `TickDecisions`).

- [ ] **Step 7: Verify the dispatch edit is exactly as specified**

```powershell
git diff -U0 -- Assets/Scripts/Simulation/Core/SimulationWorld.Ticking.cs
```

Expected: one hunk; `+` lines are the four-line V2 block and the `else if` line; exactly one `-` line, the original `if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)`. Anything else: revert the extra and re-run.

- [ ] **Step 8: Run the P0 pins, the Legacy pin, the fingerprint tests and the liveness tests**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~PolicyVersionFreezeTests|FullyQualifiedName~CoreSimulationTests|FullyQualifiedName~StateFingerprintTests|FullyQualifiedName~LivenessTests"
```

Expected: all pass. The V1 branch is reached by the same condition as before through the `else if`; Legacy takes the same later branches. Any failure: stop.

- [ ] **Step 9: Commit**

```powershell
git add Assets/Scripts/Simulation/Behavior/DecisionSystem.V2.cs Assets/Scripts/Simulation/Core/SimulationWorld.DecisionsV2.cs Assets/Scripts/Simulation/Core/SimulationWorld.Ticking.cs Assets/Tests/EditMode/DecisionV2SeamTests.cs
git diff --check --cached
git diff --cached --stat
git commit -m "feat: dispatch IntentUtilityV2 through DecideV2 with predation read from the ruleset" -m "One if inserted at the head of TickDecisions' policy chain; V1 and Legacy branches unchanged. SimulationWorld.DecideV2 replicates the V1-only candidate block, the V1 call and the V1 cognition post-processing verbatim, substituting SimulationV2Ruleset.PredationMode == Enabled for FounderProfile == PredationVariation. DecisionSystem.DecideIntentUtilityV2 forwards to the frozen V1 overload with every parameter required. Tests: V2 Disabled equals V1 on pins A and B; V2 Enabled equals V1 pin C with attacks landing; V2 Disabled on predation founders lands no attack, no predation death, no flee, and diverges from pin C." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
git status --porcelain
```

Expected: `diff --check` prints nothing; four files; `status` empty.

---

### Task 6: Test 4 — predation executes with controlled creatures under V2 Enabled and not under Disabled

**Files:**
- Modify: `Assets/Tests/EditMode/DecisionV2SeamTests.cs` (append test 4)

**Interfaces:**
- Consumes: `AsV2`, `RunSettled` is not used here (no scenario, hand-placed creatures); `SimulationWorld.Spawn(Genome)`, `world.Creatures.GetMovementRefAt`, `GetNeedsRefAt`, `GetPhenotypeAt`, `TryGetCreatureIndex`.
- Produces: the ninth test. No production change is expected in this task; if one is needed, that is a stop.

- [ ] **Step 1: Append test 4**

After `V2Disabled_PredationFoundersCannotActivatePredation`, add:

```csharp
        private const int ControlledPredationTicks = 600;

        /// <summary>
        /// Two identical creatures that PredationSystem.HasViableHuntingStrategy accepts
        /// (diet >= 0.58, aggression >= 0.35) with nonzero attack and defense, one unit apart,
        /// hungry, in an arena with no resources. Not founders: InitialPopulation is 0, so the
        /// PhysiologyVariation profile seeds nothing and FounderProfile cannot be what turns
        /// predation on. Founders with zero combat genes are not evidence for the switch.
        /// </summary>
        private static SimulationWorld RunControlledPredation(PredationMode mode)
        {
            SimulationConfig config = AsV2(
                SimulationConfig.CreatePrototype4Defaults(PolicyVersionFreezeTests.FreezeSeed, 0),
                SimulationV2Ruleset.Schema1(mode));
            Assert.That(config.FounderProfile, Is.EqualTo(FounderProfile.PhysiologyVariation));
            Assert.That(config.InitialPopulation, Is.EqualTo(0));

            var world = new SimulationWorld(config);
            Assert.That(world.Creatures.Count, Is.EqualTo(0), "no founders");

            var genome = new Genome(
                0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                attack: 1f, defense: 0.1f, maneuverability: 0.1f, fear: 0f, aggression: 1f, dietSpecialization: 1f);
            CreatureId first = world.Spawn(genome);
            CreatureId second = world.Spawn(genome);
            Assert.That(world.TryGetCreatureIndex(first, out int firstIndex), Is.True);
            Assert.That(world.TryGetCreatureIndex(second, out int secondIndex), Is.True);

            world.Creatures.GetMovementRefAt(firstIndex).Position = new SimVector2(0f, 0f);
            world.Creatures.GetMovementRefAt(secondIndex).Position = new SimVector2(1f, 0f);
            world.Creatures.GetNeedsRefAt(firstIndex).Energy = 0.3f * world.Creatures.GetPhenotypeAt(firstIndex).EnergyCapacity;
            world.Creatures.GetNeedsRefAt(secondIndex).Energy = 0.3f * world.Creatures.GetPhenotypeAt(secondIndex).EnergyCapacity;

            for (int tick = 0; tick < ControlledPredationTicks; tick++)
            {
                world.Step(config.FixedDeltaTime);
                world.Events.Clear();
            }

            return world;
        }

        [Test]
        public void V2Enabled_PredationExecutesWithControlledCreatures()
        {
            SimulationWorld enabled = RunControlledPredation(PredationMode.Enabled);
            SimulationWorld disabled = RunControlledPredation(PredationMode.Disabled);

            Assert.Multiple(() =>
            {
                Assert.That(enabled.CaptureStatistics().AttackHitCount, Is.GreaterThan(0), "Enabled: an attack must land");
                Assert.That(disabled.CaptureStatistics().AttackHitCount, Is.EqualTo(0), "Disabled: no attack may land");
                Assert.That(disabled.CaptureStatistics().FleeDecisionCount, Is.EqualTo(0), "Disabled: no flee decision");
            });
        }
```

`SimVector2` lives in `LifeSimulation.Simulation.Core`; `Genome` in `LifeSimulation.Simulation.Biology`, already imported.

- [ ] **Step 2: Run it**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests.V2Enabled_PredationExecutesWithControlledCreatures"
```

Expected: **PASS.** This is the one test whose red state is not the natural absence of the feature — the dispatch already exists after Task 5 — so its evidence is the Disabled arm: same creatures, same positions, same hunger, zero hits. If the Enabled arm reports `AttackHitCount == 0` with the creatures as specified: **stop** (see the stop table); do not lower `dietSpecialization` or `aggression` and do not extend past 600 ticks.

Sanity of the arrangement, so the executor knows why it should pass: `CreatePrototype4Defaults` has `predationEconomicsEnabled` false, so `PredationSystem.Threat` takes the legacy branch — `HasViableHuntingStrategy` holds at diet 1.0 and aggression 1.0; the advantage is `1 / (1 + 0.1 + 0.025 + 0.01)` at equal phenotypes; distance attenuation over one unit leaves it well above the hunt threshold. `TickCombat` requires `Attack` within 1.1 units and a roll under `0.20 + 0.70 × Threat`, with a 0.75 s recovery; 600 ticks at the P4 schedule give each creature many attempts.

- [ ] **Step 3: Run the whole seam file**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~DecisionV2SeamTests"
```

Expected: 9 passed, 0 failed.

- [ ] **Step 4: Commit**

```powershell
git add Assets/Tests/EditMode/DecisionV2SeamTests.cs
git diff --check --cached
git diff --cached --stat
git commit -m "test: prove the V2 predation switch with hand-placed hunters, not founders" -m "Two identical creatures that pass HasViableHuntingStrategy, spawned into a founderless PhysiologyVariation world one unit apart and hungry: PredationMode.Enabled lands attacks, Disabled lands none and never flees. FounderProfile cannot be what turned predation on, because it seeded nothing." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
git status --porcelain
```

Expected: `diff --check` prints nothing; one file; `status` empty.

---

### Task 7: Full-suite verification twice, diff review, and the completion report

**Files:**
- none modified

- [ ] **Step 1: Full suite, first run**

```powershell
dotnet test tools/HeadlessTests --nologo
```

Expected: `Failed: 0`, `Passed: 778` (769 + 9). Any failure: stop and report verbatim.

- [ ] **Step 2: Full suite, second run**

```powershell
dotnet test tools/HeadlessTests --nologo
```

Expected: identical counts. `LivenessTests` included and green.

- [ ] **Step 3: Review the whole branch diff against the authorised file list**

```powershell
git diff --stat d7e433b HEAD
git diff --stat d7e433b HEAD -- Assets/Scripts Assets/Tests tools | Select-String -Pattern "Simulation/Core/SimulationConfig.cs|SimulationWorld.Ticking.cs|Experiments/ExperimentManifest.cs|StateFingerprintTests.cs|SimulationV2Ruleset.cs|SimulationWorld.DecisionsV2.cs|DecisionSystem.V2.cs|DecisionV2SeamTests.cs|files changed" -NotMatch
```

Expected: the first command lists exactly nine paths — this plan (`docs/superpowers/plans/2026-09-12-v2-ruleset-seam-p1.md`) and the eight authorised files; the second prints **nothing** (no unauthorised file under `Assets/` or `tools/`).

- [ ] **Step 4: Confirm `RandomDomain`, `DecisionSystem.cs`, the P0 file and the tools are untouched**

```powershell
git diff d7e433b HEAD -- Assets/Scripts/Simulation/Core/SimulationTypes.cs Assets/Scripts/Simulation/Core/DeterministicRandom.cs Assets/Scripts/Simulation/Behavior/DecisionSystem.cs Assets/Scripts/Simulation/Behavior/DecisionSystem.Scoring.cs Assets/Scripts/Simulation/Behavior/DecisionSystem.Legacy.cs Assets/Scripts/Simulation/Behavior/PerceptionSystem.cs Assets/Scripts/Simulation/Biology/ReproductionSystem.cs Assets/Scripts/Simulation/Analysis Assets/Scripts/Simulation/Diagnostics Assets/Scripts/Presentation Assets/Tests/EditMode/PolicyVersionFreezeTests.cs tools .gitignore
git diff d7e433b HEAD -- Assets/Tests/EditMode/StateFingerprintTests.cs
```

Expected: the first prints nothing. The second prints one hunk: `-            const int PinnedConfigurationPropertyCount = 66;` / `+            const int PinnedConfigurationPropertyCount = 67;` and nothing else.

- [ ] **Step 5: Confirm the V1 hash sequence and version are untouched**

```powershell
git diff d7e433b HEAD -- Assets/Scripts/Simulation/Core/SimulationConfig.cs | Select-String -Pattern "^-" | Where-Object { $_.Line -notmatch "^---" }
Select-String -Path Assets/Scripts/Simulation/Core/SimulationConfig.cs -Pattern "ConfigurationHashVersion = 9;"
```

Expected: the first prints exactly one removed line — the old last constructor parameter line `int generatedPlantSiteAnchorCount = DefaultGeneratedPlantSiteAnchorCount)` (re-emitted with a trailing comma) — and nothing else; the second prints one line.

- [ ] **Step 6: Confirm no sentinel, no LINQ or `foreach` in the new production files, and no `bool` on the struct**

```powershell
Select-String -Path Assets/Tests/EditMode/DecisionV2SeamTests.cs -Pattern "ulong.MaxValue|sentinel"
Select-String -Path Assets/Scripts/Simulation/Core/SimulationV2Ruleset.cs, Assets/Scripts/Simulation/Core/SimulationWorld.DecisionsV2.cs, Assets/Scripts/Simulation/Behavior/DecisionSystem.V2.cs -Pattern "System.Linq|foreach|\bbool\b.*\{ get;|new List|new Dictionary"
```

Expected: both print nothing. (The delegate's `bool` *parameters* are V1's, forwarded; the pattern above matches only a `bool` property.)

- [ ] **Step 7: Report completion in chat**

State: the five commit hashes on `v2-ruleset-seam-p1`; both full-suite counts (778, 778); the two captured V2 configuration-hash literals; that every P0 pin, the Legacy pin, `StateFingerprintTests` (including the bool-constructor reflection test) and `LivenessTests` passed unchanged; that `RandomDomain` gained no member; that `DecideV2` differs from the V1 text in exactly the three marked lines; the `.meta` gap; and anything the executor was unsure about. Do not write a completion file. Do not push and do not merge — the branch waits for review.

---

## Self-review against the spec

- §3.1 boundary: `IntentUtilityV2 = 2`; Legacy and V1 code paths not edited → Task 2 step 4(a); Task 5 step 5 inserts an `if` and demotes the V1 `if` to `else if`, nothing inside either branch changes; Task 7 steps 4–5 verify. ✔
- §3.2 hashes preserved; `ConfigurationHashVersion` 9; V2 fields hashed only under V2 → Task 3 step 4, verified by P0 pins in Task 3 step 7 and Task 7. ✔
- §3.3/§4.1 one typed property, enums and small value types only, private constructor, `Schema1`, `Validate`, `HashInto`, last optional constructor parameter, no other constructor → Task 2 steps 3–4; test 7. ✔
- §3.4 predation decoupled from `FounderProfile`; V2 never reads it for behaviour → Task 5 step 4 (`predationEnabled` local); `FounderProfile` still selects founders (`SimulationWorld.cs:169`, untouched). ✔
- §3.5 proximity pairing untouched → `ReproductionSystem.cs` in the must-not-touch list; Task 7 step 4. ✔
- §3.6 headless-only → no presenter, tool or factory change; only `AsV2` builds V2 configurations. ✔
- §4.2 schema rules: schema 0 = `default`, invalid under V2, only valid under Legacy/V1; schema 1 = `PredationMode`; validation and hash branch on the instance's schema; enum append-only with `Unspecified = 0`; names and values pinned by test 9 → Task 2 step 3, Task 3 tests 6 and 9. ✔
- §4.3 no booleans (test 7); modes not knobs; reflection harnesses ignore it by type — `FlagLivenessAnalysis` flips `bool` parameters only and `BuildArguments` copies the property → Task 2 step 6 runs `LivenessTests`. ✔
- §4.4 hash block appended after the last field, conditional on V2, `ComputeStateFingerprint` untouched → Task 3 step 4, step 8. ✔
- §4.5 all five validation rules → `SimulationV2Ruleset.Validate`; called from `SimulationConfig.Validate`, which the world constructor calls → Task 3 steps 3 and test 6 (including the `new SimulationWorld` case). The "unknown `SchemaVersion`" rule is written but cannot be exercised in P1 because the private constructor admits only schema 1; noted below. ✔
- §4.6 manifest `schema=2`, same fields same order, then `V2RulesetSchema` and one line per field; Legacy/V1 byte-identical; `ExperimentManifestTests` untouched → Task 4; test 8 compares the V1 text against P0's constant verbatim and P0's own `PinC_CanonicalManifestUnderTheFixedRevision` runs in Task 4 step 4. ✔
- §5 dispatch: one edit at the head of the chain; the shared/V1-only table — nearest food/water/carcass shared and passed in (`carcass`), multi-threat buffer shared and passed in (`otherCandidates`), cognition reads/writes shared, V1 candidate block and V1 cognition post-processing replicated verbatim, Legacy blocks skipped by their own gates → Task 5 steps 4–5. The V1 tautology at `:277` is not edited (§8). ✔
- §6.2 files: exactly the four modified and four created; `StateFingerprintTests` only the count → Global Constraints; Task 7 steps 3–4. ✔
- §6.2 behaviour contract and `AsV2` → Task 3 step 1: reflection over the single public constructor, PascalCase property convention, error (not default) on a missing property, exactly two overrides. Pins from factories and from P0's `internal` replica. ✔
- §6.2 tests 1–9, one each: 1 → Task 5; 2 → Task 5 (`AttackHitCount > 0` in both); 3 → Task 5; 4 → Task 6 (`InitialPopulation = 0`, `PhysiologyVariation`, identical genomes with nonzero attack/defense/aggression, diet 1.0 ≥ 0.58, aggression 1.0 ≥ 0.35, positions via `GetMovementRefAt`, energy lowered); 5 → Task 3 (V1 hash computed live, not restated); 6 → Task 3 (all four rejections plus V1's); 7 → Task 2 (no `bool` field/property/parameter; no public instance constructor with parameters; private constructor located non-publicly); 8 → Task 4 (both assertions); 9 → Task 3 (two `const ulong` pins, enum names and values, `SchemaVersion == 1`, validates under V2). ✔
- §6.2 determinism: delegation only; the only new arithmetic is `HashInto`'s integer FNV step in configuration hashing, not simulation; `RandomDomain` verified from the diff in Task 7 step 4. ✔
- §9 stop conditions carried into the stop table: reflection over the struct parameter (Task 2 step 6), thin delegation (Task 5 step 6), any hash outside the new file moving, any unauthorised file. ✔
- §10 guarantees each have a verifying step: P0 pins and `CoreSimulationTests` (Tasks 2, 3, 5, 7), `ConfigurationHashVersion` (Task 7 step 5), manifests (Task 4), `KnownInertFlags`/`FlagLivenessAnalysis`/bool-constructor test (Tasks 2, 5, 7), `RandomDomain` (Task 7 step 4). ✔
- Placeholder scan: no TBD/TODO; every code step carries its code; the only `<…>` tokens are the two captured hash values, whose capture step precedes their use. ✔
- Type consistency: `Rebuild`/`AsV2`/`PinA`/`PinB`/`PinC`/`RunSettled`/`DefaultTicks`/`FreezeFounders` are defined in Task 3 or Task 5 before use; `DecideIntentUtilityV2`'s parameter list equals the V1 full overload verified in Task 1 step 3(k); `DecideV2`'s signature in Task 5 step 4 matches its call in step 5. ✔

## Deviations from the spec text, stated so they are not silent

1. **`DecideV2` takes no `food` or `water` parameter.** Spec §5 writes `DecideV2(index, tick, movement, phenotype, selfLineage, food, water, carcass, otherCandidates, out diagnostics)`. The V1 call (`Ticking.cs:346-384`) and the replicated cognition block read neither `food` nor `water` — they read the candidate buffers, `carcass`, and `other`. Passing two unread arguments would be dead parameters in per-tick code. The plan omits them; nothing shared is recomputed and the equivalence tests are unaffected. If the reviewer prefers the spec's literal signature, adding the two parameters is a two-line change with no behavioural consequence.
2. **`HashInto` carries its own FNV-1a step.** `SimulationConfig.Hash` is `private static`. Rather than widen `SimulationConfig`'s surface, the struct repeats the one-line `(hash ^ value) * 1099511628211UL` step with a comment naming the origin. Same constants, same operation; the spec is silent on where the step lives.
3. **`DecideIntentUtilityV2` has no optional parameters.** The V1 overload defaults sixteen of its parameters. The V2 delegate requires all of them, so no V2 caller can silently drop an argument V1 receives. This is stricter than, not different from, the spec.
4. **The "unknown `SchemaVersion`" branch is unreachable in P1.** The private constructor admits only schema 1; test 6 cannot construct an instance with another schema. The branch exists so that schema 2's factory lands against a validation that already refuses schemas it does not know.
5. **No `.meta` files.** See Global Constraints. Consistent with every headless-only file added since 2026-09-01 and with the spec's authorised list, which names none. A future Unity-editor session generates them; the plan reports the gap rather than adding unauthorised files.

## Ambiguities found while planning (for the reviewer)

1. **Test 3 may pass before Task 5's dispatch exists** (Task 5 step 2): a V2 configuration falling through to `DecideFromLearnedOutcomes` also produces zero attacks and a divergent hash. Its meaning comes from test 2 passing with the same configuration and `Enabled`. Recorded so the red/green sequence is not misread.
2. **Test 4 has no natural red state** (Task 6 step 2): the dispatch already exists. The Disabled arm is its control. If the reviewer wants a red state, the test could be written in Task 5 before the dispatch, where the Enabled arm would fail on the fall-through path; the plan keeps it in its own task so the controlled-creature arrangement can be adjusted (positions, energy — never the gene thresholds) without touching the dispatch commit.
3. **`ControlledPredationTicks = 600`** is a bounded budget, not a measurement; the first landed hit will come much earlier. The stop rule forbids extending it, so a failure surfaces as a report rather than a longer horizon.
4. **Runtime.** Tests 1, 2, 3 and 5 each run two or more 2,000-tick worlds at cap 500; expect the seam file to take on the order of a minute. Acceptable for a freeze-equivalence file; not a reason to shorten the horizon, which must equal P0's.
