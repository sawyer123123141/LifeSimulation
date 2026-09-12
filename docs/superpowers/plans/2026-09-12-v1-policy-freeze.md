# V1 Policy Freeze (P0) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pin the live `IntentUtilityV1` path at `65bae69` with literal state, behaviour, fingerprint and configuration hashes for four representative configurations, so P1 can prove the V2 seam changed nothing.

**Architecture:** One new NUnit characterization-test file and nothing else. Constants are captured from the untouched baseline by a transient probe and a real `CreatureSweep` run, then written into the test as exact literals. The tests first fail on deliberately impossible sentinel values, then pass on the captured values. The test file also exposes the pin-C replica, its canonical manifest and its capture arguments as `internal` members for P1's later reuse.

**Tech Stack:** C# / NUnit 4 via `tools/HeadlessTests` (`dotnet test`); `tools/CreatureSweep` (`dotnet run`); Windows PowerShell 5.1; git.

**Spec:** `docs/superpowers/specs/2026-09-11-v2-ruleset-seam-design.md` — §6.1 "P0 — freeze the live V1 path (tests only)", plus §2.4 finding (c), §9 and §10. Read §6.1 before starting.

## Global Constraints

- Work only in the existing worktree `.claude/worktrees/v2-seam-design` on branch `worktree-v2-seam-design`, starting at `ebab85b`.
- The implementation commit contains **exactly one file**: `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`. No production file, no existing test, no document, no other file.
- Transient `ZZZ*.cs` probes are allowed and must be deleted before committing. Any CSV the sweep tool writes under `docs/experiments/` must be deleted before committing. The tree must be clean but for the one file.
- **Do not invent a hash or a manifest.** Every literal in the test comes from an earlier numbered capture step in this plan. Until captured, the test carries sentinel values that cannot pass.
- P0 pins **four configurations, not the 98 construction sites**; the file header must say so.
- `AGENTS.md` §3 determinism rules apply to the test code: no `System.Random`, no clock, no LINQ over anything that orders simulation input, no float re-association.
- Never edit, weaken, `[Ignore]` or delete any existing test. Never change an expected value to match output — except the two documented sentinel-to-captured replacements this plan schedules.
- Two full-suite runs before committing. `LivenessTests` untouched and green.
- Completion is reported in chat. No completion file is written.
- Git through the Bash tool is blocked inside this worktree by the `rtk` hook; run every `git` command with the PowerShell tool from the worktree root. `dotnet` commands also run from the worktree root.

## Stop conditions (report, do not work around)

| condition | action |
|---|---|
| pin D does not reproduce the committed `663693199115149672` at tick 2,666 | stop; report the reproduced value; do not pin it |
| the sweep's `slope-off` row `seed` ≠ 42 | stop; report the emitted seed; do not change the replica to fit |
| the sweep cannot produce a header **and** a `slope-off` row with seed and hash | stop; report what it did produce |
| the normalised replica manifest ≠ the normalised sweep header | stop; report the differing lines |
| any test outside `PolicyVersionFreezeTests` fails in either full run | stop; report the failure verbatim |
| any pin needs a file other than the one authorised (a helper, a fixture, a `.meta`, an edit to `HeadlessTests.csproj`) | stop; report which and why |
| pin C's replica shows `AttackHitCount == 0` at every horizon up to 24,000 ticks | stop; report |
| `dotnet test` or `dotnet run` fails to build at `ebab85b` before any change | stop; report the first error line |

---

### Task 1: Baseline verification and the exact replica arguments

**Files:**
- none created or modified

**Interfaces:**
- Produces: confirmation that the worktree is at `ebab85b`, clean, and builds; the two replica argument lists (pin C, pin D) transcribed from `tools/CreatureSweep/Program.cs` for Tasks 2–4.

- [ ] **Step 1: Verify the worktree**

Run (PowerShell, from `C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim\.claude\worktrees\v2-seam-design`):

```powershell
git branch --show-current
git rev-parse --short HEAD
git status --porcelain
git log --oneline -1 main
```

Expected: `worktree-v2-seam-design`; `ebab85b`; no output from `status`; `main` at `65bae69`. Anything else: stop.

- [ ] **Step 2: Verify the baseline builds and the suite is green before any change**

```powershell
dotnet test tools/HeadlessTests --nologo
```

Expected: `Passed!` with `Failed: 0`. Record the passed count — it is the number the two later full runs must match plus five. If it fails: stop.

- [ ] **Step 3: Read the tool's configuration builder and transcribe it**

Open `tools/CreatureSweep/Program.cs` and read `CreateConfig` (search for `private static SimulationConfig CreateConfig`). Read the field defaults near the top of the class (`_join`, `_multiThreat`, `_kinRecognition`, `_mateSelection`, `_healthRecovery`, `_metabolicHealing`, `_metabolicIngestion`, `_terrainTemperature`, `_evasiveFleeing`, `_evasionStrength`, `_reproductionNeedFraction`, `_gradedFertility`, `_brakeStrength`, `_policy`, `Founders`, `FirstSeed`) and the argument parsing for `--brake=`, `--gate=`, `--predation`, `--mate-selection=off`, `--regen=`, `--deaths`, `--focused`.

Confirm against the source that the two replicas below are what the tool builds. If any line of `CreateConfig` or any default differs from this transcription, the transcription is wrong, not the source — fix the transcription and note the difference in the completion report.

Pin C — the tool's `CreateConfig(seed, slope: false)` after `--focused 1 500 --regen=2.0 --brake=1.0 --predation --gate=0.45 --mate-selection=off`:

```csharp
SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, 12);
return new SimulationConfig(
    worldSeed,
    12,
    defaults.Schedule,
    500,
    FounderProfile.PredationVariation,
    cognitionEnabled: true,
    physiologyEnabled: true,
    decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
    plantCohortsEnabled: true,
    foragingEconomicsEnabled: true,
    predationEconomicsEnabled: true,
    decisionStaggerEnabled: true,
    multiThreatPerceptionEnabled: true,
    restBehaviorEnabled: true,
    juvenileCapabilityEnabled: true,
    parentalFollowingEnabled: true,
    kinRecognitionEnabled: true,
    learnedResourceQualityEnabled: true,
    mateSelectionEnabled: false,
    plantSiteCompetitionEnabled: true,
    plantMortalityEnabled: true,
    plantDefenseDeterrenceEnabled: true,
    plantQualityPreferenceEnabled: true,
    plantTemperatureAdaptationEnabled: true,
    proceduralEnvironmentFieldsEnabled: true,
    plantFertilityAdaptationEnabled: true,
    elevationFieldEnabled: true,
    plantEstablishmentContestEnabled: true,
    plantInvaderEstablishmentContestEnabled: true,
    plantSeedProductionRateEnabled: true,
    terrainDrivenEnvironmentEnabled: true,
    slopeMovementCostEnabled: false,
    terrainDrivenTemperatureEnabled: false,
    metabolicIngestionEnabled: false,
    reproductionNeedFraction: 0.45f,
    healthRecoveryEnabled: false,
    metabolicHealingEnabled: false,
    gradedFertilityEnabled: true,
    gradedFertilityStrength: 1.0f,
    evasiveFleeingEnabled: false,
    evasiveFleeingStrength: SimulationConfig.DefaultEvasiveFleeingStrength);
```

Scenario for pin C: `Prototype4Scenarios.ConsumerDefenseCalibrationModerate.WithRegeneration("p6-defense-calibration-regen2.00", 2f)` — the tool builds the id as `"p6-defense-calibration-" + "regen" + regen.ToString("0.00")`.

Pin D — the tool's `CreateConfig(seed, slope: false)` after `--deaths 24 500 --regen=2.0 --brake=1.5 --ticks=24000`. Identical to pin C except:

```csharp
    FounderProfile.PhysiologyVariation,          // no --predation
    ...
    mateSelectionEnabled: true,                   // default
    ...
    reproductionNeedFraction: SimulationConfig.DefaultReproductionNeedFraction,   // no --gate
    ...
    gradedFertilityStrength: 1.5f,                // --brake=1.5
```

Same scenario (`--regen=2.0`). `--deaths` runs seeds `FirstSeed + index` sequentially, so seed 42 is the first world; its trajectory samples close on `(int)((long)24000 * (index + 1) / 9)`, so sample 1 closes on tick 2,666, after the 2,666th `Step`, with `Events.Clear()` after every step.

- [ ] **Step 4: Confirm the committed artefact's value**

```powershell
Select-String -Path "docs/experiments/p6-deaths-perseed-cap500-regen2.00-24seeds-brake1.5-24000ticks-9samples-2026-09-10.csv" -Pattern "^control,42,1," | ForEach-Object { $_.Line }
```

Expected: one row whose last field is `663693199115149672` and whose `tick` field is `2666`. Confirm the header's last column is `hash` (run `Get-Content <path> -TotalCount 1`). If the row or value differs from the spec: stop.

---

### Task 2: Real `CreatureSweep` cross-check for pin C

**Files:**
- temporary, deleted in this task: one CSV the tool writes under `docs/experiments/`
- scratch, outside the repository: `C:\Users\sawye\AppData\Local\Temp\claude\C--Users-sawye-OneDrive-Documents-ChatGPT-life-sim\62504969-75d6-4623-b34d-c99a7d1ec118\scratchpad\p0\sweep-header.txt` and `sweep-row.txt`

**Interfaces:**
- Produces: `SWEEP_SEED` (must be 42), `SWEEP_HASH` (the `slope-off` row's `hash`), and the normalised tool header saved to `sweep-header.txt`, consumed by Task 3 step 6.

- [ ] **Step 1: Run the tool at the untouched baseline**

```powershell
New-Item -ItemType Directory -Force "C:\Users\sawye\AppData\Local\Temp\claude\C--Users-sawye-OneDrive-Documents-ChatGPT-life-sim\62504969-75d6-4623-b34d-c99a7d1ec118\scratchpad\p0" | Out-Null
git status --porcelain
dotnet run --project tools/CreatureSweep -c Release -- --focused 1 500 --regen=2.0 --brake=1.0 --predation --gate=0.45 --mate-selection=off --ticks=2000
```

`git status` must be empty first. The tool prints `selecting seeds with at least 5 m of climb`, `1 seeds, highest <seed>`, `2 runs of 2000 ticks`, and `wrote docs/experiments/p6-slope-cost-focused-cap500-regen2.00-1seeds-mateseloff-predation-…csv` on stderr. Note the printed path.

- [ ] **Step 2: Extract the seed and hash**

```powershell
$csv = Get-ChildItem docs/experiments -Filter "p6-slope-cost-focused-cap500-regen2.00-1seeds-mateseloff-predation-*.csv" | Sort-Object LastWriteTime | Select-Object -Last 1
$lines = Get-Content $csv.FullName
$row = $lines | Where-Object { $_ -match '^slope-off,' }
$row
```

The CSV columns are `arm,seed,hash,population,extinct,energy,occupied_elevation,occupied_slope,<genes…>`. Read `seed` (second field) and `hash` (third field) off the single `slope-off` row. Write both down as `SWEEP_SEED` and `SWEEP_HASH`. Save the row:

```powershell
$row | Set-Content -Encoding utf8 "$env:LOCALAPPDATA\Temp\claude\C--Users-sawye-OneDrive-Documents-ChatGPT-life-sim\62504969-75d6-4623-b34d-c99a7d1ec118\scratchpad\p0\sweep-row.txt"
```

**Stop condition:** `SWEEP_SEED` ≠ `42`. Report the seed the relief filter chose. Do not continue. (The replica's `WorldSeed` is 42 by the spec; a different emitted seed means the recorded cell's runs and the focused tool disagree, which is a finding, not a parameter.)

- [ ] **Step 3: Extract and normalise the manifest header**

The header is every line before the first empty line. Remove exactly the `code_revision=` and `SlopeMovementCostEnabled=` lines; keep everything else in order.

```powershell
$blank = [array]::IndexOf($lines, "")
$header = $lines[0..($blank - 1)] | Where-Object { $_ -notmatch '^(code_revision|SlopeMovementCostEnabled)=' }
$header | Set-Content -Encoding utf8 "$env:LOCALAPPDATA\Temp\claude\C--Users-sawye-OneDrive-Documents-ChatGPT-life-sim\62504969-75d6-4623-b34d-c99a7d1ec118\scratchpad\p0\sweep-header.txt"
$header.Count
$header | Select-Object -First 8
```

Expected: the first lines are `schema=1`, `scenario_id=p6-defense-calibration-regen2.00`, `scenario_layout_fingerprint=…`, `scenario_resource_count=…`, `first_seed=42`, `seed_count=1`, `ticks=2000`, `WorldSeed=42`. If there is no header, no `slope-off` row, or no `hash` column: stop and report what the tool produced.

- [ ] **Step 4: Delete the temporary CSV and confirm the tree is clean**

```powershell
Remove-Item $csv.FullName
git status --porcelain
```

Expected: no output. The CSV is never committed.

---

### Task 3: Transient probe — capture every constant from the untouched baseline

**Files:**
- Create (transient, deleted in Task 5): `Assets/Tests/EditMode/ZZZFreezeCapture.cs`
- scratch, outside the repository: `…\scratchpad\p0\replica-manifest-normalised.txt`, `…\scratchpad\p0\capture.txt`

**Interfaces:**
- Consumes: the replica argument lists from Task 1 step 3; `SWEEP_HASH` and `sweep-header.txt` from Task 2.
- Produces: the captured values consumed by Task 4 step 4 — `A_STATE, A_BEHAVIOR, A_FINGERPRINT, A_CONFIG`, same four for `B`, `C`, `D`; `C_HORIZON`; `C_ATTACK_HITS`; `C_CANONICAL_MANIFEST` (complete text under revision `p0-pin-c`).

- [ ] **Step 1: Write the probe**

Create `Assets/Tests/EditMode/ZZZFreezeCapture.cs`:

```csharp
using System.IO;
using System.Text;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    // TRANSIENT PROBE. Deleted before the P0 commit. Captures the freeze constants from the
    // untouched baseline by failing with a report, so the values appear in the test output.
    public sealed class ZZZFreezeCapture
    {
        private const string Scratch =
            @"C:\Users\sawye\AppData\Local\Temp\claude\C--Users-sawye-OneDrive-Documents-ChatGPT-life-sim\62504969-75d6-4623-b34d-c99a7d1ec118\scratchpad\p0";

        private static SimulationScenario RegenScenario =>
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.WithRegeneration("p6-defense-calibration-regen2.00", 2f);

        private static SimulationConfig RecordedPredationCell(int worldSeed)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, 12);
            return new SimulationConfig(
                worldSeed, 12, defaults.Schedule, 500, FounderProfile.PredationVariation,
                cognitionEnabled: true, physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true, foragingEconomicsEnabled: true, predationEconomicsEnabled: true,
                decisionStaggerEnabled: true, multiThreatPerceptionEnabled: true, restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true, parentalFollowingEnabled: true, kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true, mateSelectionEnabled: false,
                plantSiteCompetitionEnabled: true, plantMortalityEnabled: true, plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true, plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true, plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true, plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true, plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true, slopeMovementCostEnabled: false,
                terrainDrivenTemperatureEnabled: false, metabolicIngestionEnabled: false,
                reproductionNeedFraction: 0.45f, healthRecoveryEnabled: false, metabolicHealingEnabled: false,
                gradedFertilityEnabled: true, gradedFertilityStrength: 1.0f,
                evasiveFleeingEnabled: false, evasiveFleeingStrength: SimulationConfig.DefaultEvasiveFleeingStrength);
        }

        private static SimulationConfig C3Control(int worldSeed)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, 12);
            return new SimulationConfig(
                worldSeed, 12, defaults.Schedule, 500, FounderProfile.PhysiologyVariation,
                cognitionEnabled: true, physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true, foragingEconomicsEnabled: true, predationEconomicsEnabled: true,
                decisionStaggerEnabled: true, multiThreatPerceptionEnabled: true, restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true, parentalFollowingEnabled: true, kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true, mateSelectionEnabled: true,
                plantSiteCompetitionEnabled: true, plantMortalityEnabled: true, plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true, plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true, plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true, plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true, plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true, slopeMovementCostEnabled: false,
                terrainDrivenTemperatureEnabled: false, metabolicIngestionEnabled: false,
                reproductionNeedFraction: SimulationConfig.DefaultReproductionNeedFraction,
                healthRecoveryEnabled: false, metabolicHealingEnabled: false,
                gradedFertilityEnabled: true, gradedFertilityStrength: 1.5f,
                evasiveFleeingEnabled: false, evasiveFleeingStrength: SimulationConfig.DefaultEvasiveFleeingStrength);
        }

        private static SimulationWorld Run(SimulationConfig config, SimulationScenario scenario, int ticks)
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

        private static void Report(StringBuilder report, string pin, SimulationWorld world, SimulationConfig config)
        {
            SimulationStatistics statistics = world.CaptureStatistics();
            report.Append(pin).Append("_STATE=").Append(world.ComputeStateHash()).Append('\n');
            report.Append(pin).Append("_BEHAVIOR=").Append(world.ComputeBehaviorHash()).Append('\n');
            report.Append(pin).Append("_FINGERPRINT=").Append(world.ComputeStateFingerprint()).Append('\n');
            report.Append(pin).Append("_CONFIG=").Append(config.ComputeConfigurationHash()).Append('\n');
            report.Append(pin).Append("_TICK=").Append(world.CurrentTick).Append('\n');
            report.Append(pin).Append("_ATTACK_HITS=").Append(statistics.AttackHitCount).Append('\n');
            report.Append(pin).Append("_PREDATION_DEATHS=").Append(statistics.PredationDeathCount).Append('\n');
        }

        [Test]
        public void Capture()
        {
            var report = new StringBuilder();

            SimulationConfig a = SimulationConfig.CreatePrototype4Defaults(42, 12);
            Report(report, "A", Run(a, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, 2000), a);

            SimulationConfig b = SimulationConfig.CreateFullEcosystemDefaults(42, 12);
            Report(report, "B", Run(b, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, 2000), b);

            // Pin C: 2,000 ticks first. If no attack has landed, extend to the earliest horizon
            // that shows one, in 500-tick steps, and report that horizon as C_HORIZON.
            SimulationConfig c = RecordedPredationCell(42);
            int horizon = 2000;
            SimulationWorld cWorld = Run(c, RegenScenario, horizon);
            while (cWorld.CaptureStatistics().AttackHitCount == 0 && horizon < 24000)
            {
                horizon += 500;
                cWorld = Run(c, RegenScenario, horizon);
            }

            report.Append("C_HORIZON=").Append(horizon).Append('\n');
            Report(report, "C", cWorld, c);

            SimulationConfig d = C3Control(42);
            Report(report, "D", Run(d, RegenScenario, 2666), d);

            // Canonical pin-C manifest under the fixed revision, complete text. The normalised copy
            // is for the Task 2 comparison only.
            string canonical = ExperimentManifest.Describe("p0-pin-c", RegenScenario, c, 42, 1, horizon);
            File.WriteAllText(Path.Combine(Scratch, "canonical-manifest.txt"), canonical);
            var normalised = new StringBuilder();
            foreach (string line in canonical.Split('\n'))
            {
                if (line.Length == 0) continue;
                if (line.StartsWith("code_revision=") || line.StartsWith("SlopeMovementCostEnabled=")) continue;
                normalised.Append(line).Append('\n');
            }

            File.WriteAllText(Path.Combine(Scratch, "replica-manifest-normalised.txt"), normalised.ToString());
            File.WriteAllText(Path.Combine(Scratch, "capture.txt"), report.ToString());
            Assert.Fail(report.ToString());
        }
    }
}
```

- [ ] **Step 2: Run the probe**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~ZZZFreezeCapture"
```

Expected: 1 failed, with the report in the failure message, and three files in `…\scratchpad\p0\`: `capture.txt`, `canonical-manifest.txt`, `replica-manifest-normalised.txt`. Note: the sweep tool is built `-c Release` and the probe runs under `dotnet test`'s default configuration; pin D in step 5 is the check that this does not matter.

- [ ] **Step 3: Record the constants**

```powershell
Get-Content "$env:LOCALAPPDATA\Temp\claude\C--Users-sawye-OneDrive-Documents-ChatGPT-life-sim\62504969-75d6-4623-b34d-c99a7d1ec118\scratchpad\p0\capture.txt"
```

Write down every `X_STATE`, `X_BEHAVIOR`, `X_FINGERPRINT`, `X_CONFIG` for A, B, C, D, plus `C_HORIZON`, `C_ATTACK_HITS`, `A_PREDATION_DEATHS`, `B_PREDATION_DEATHS`, `D_TICK`.

Expected: `A_PREDATION_DEATHS=0`, `B_PREDATION_DEATHS=0`, `C_ATTACK_HITS>0`, `D_TICK=2666`. If `C_HORIZON` is 24000 and `C_ATTACK_HITS` is still 0: stop.

- [ ] **Step 4: Cross-check the pin-C behaviour hash against the sweep**

If `C_HORIZON` is 2000: `C_BEHAVIOR` must equal `SWEEP_HASH` from Task 2. If they differ: stop and report both values.

If `C_HORIZON` is not 2000, the Task 2 run was at the wrong horizon: rerun Task 2 with `--ticks=<C_HORIZON>` (steps 1–4 of Task 2 in full, including the CSV deletion), then require `C_BEHAVIOR == SWEEP_HASH`.

- [ ] **Step 5: Cross-check pin D against the committed artefact**

`D_BEHAVIOR` must equal `663693199115149672`. If it does not: **stop**. Report `D_BEHAVIOR`, and name the candidates for the mismatch without testing them — replica transcription error, Release-versus-test-configuration floating point, or the A2 revert not being byte-identical to the recorded control arm. Do not pin the reproduced value.

- [ ] **Step 6: Cross-check the manifests**

```powershell
$p0 = "$env:LOCALAPPDATA\Temp\claude\C--Users-sawye-OneDrive-Documents-ChatGPT-life-sim\62504969-75d6-4623-b34d-c99a7d1ec118\scratchpad\p0"
Compare-Object (Get-Content "$p0\sweep-header.txt") (Get-Content "$p0\replica-manifest-normalised.txt") -SyncWindow 0
```

Expected: no output (every remaining line identical, in order). Any output: stop and report the differing lines. `ticks=` is the one line that legitimately depends on `C_HORIZON`; Task 2 must have been run at `C_HORIZON` for it to match.

- [ ] **Step 7: Read the canonical manifest**

```powershell
Get-Content "$p0\canonical-manifest.txt"
```

Expected: begins `schema=1`, second line `code_revision=p0-pin-c`, contains `SlopeMovementCostEnabled=false`, `DecisionPolicyVersion=IntentUtilityV1`, `ticks=<C_HORIZON>`. This complete text, with `\n` line endings, is `C_CANONICAL_MANIFEST`.

---

### Task 4: Write the freeze tests — first failing on sentinels, then passing on captured values

**Files:**
- Create: `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`

**Interfaces:**
- Consumes: every constant from Task 3 step 3 and step 7.
- Produces, for P1 (all `internal`): `PolicyVersionFreezeTests.FreezeSeed`, `FreezeFounders`, `PinCTicks`, `PinCSeedCount`, `PinCRevision`, `PinCCanonicalManifest`, `RecordedPredationCell(int worldSeed)`, `RecordedPredationScenario`, `C3Control(int worldSeed)`, `RunSettled(SimulationConfig, SimulationScenario, int)`.

- [ ] **Step 1: Write the test file with impossible sentinels**

Create `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`. Every `Captured…` constant starts as the sentinel `ulong.MaxValue`, the manifest as `"SENTINEL-NOT-CAPTURED"`, and `PinCTicks` as the `C_HORIZON` from Task 3 (a tick count, not a hash — write it now).

```csharp
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// P0 of the V2 seam (docs/superpowers/specs/2026-09-11-v2-ruleset-seam-design.md §6.1):
    /// literal pins of the live <c>IntentUtilityV1</c> path, captured at commit 65bae69.
    ///
    /// <para><b>Four configurations are pinned, not the 98 construction sites.</b> A, the P4
    /// baseline every one-flag arm varies against; B, the widest surface the inert-flag set is
    /// pinned on; C, the representative recorded predation configuration; D, the C3 control
    /// whose value reproduces a committed artefact rather than a fresh capture. Legacy is pinned
    /// separately in <c>CoreSimulationTests</c> at 50 ticks.</para>
    ///
    /// <para>Pins C and D are hand replicas of <c>tools/CreatureSweep/Program.cs</c>
    /// <c>CreateConfig(seed, slope: false)</c> as of 65bae69, cross-checked once at capture
    /// time against the real tool: the emitted seed, the emitted behaviour hash, and the emitted
    /// manifest normalised by removing exactly <c>code_revision</c> and
    /// <c>SlopeMovementCostEnabled</c>. A later change to the tool silently desynchronises the
    /// replica; this file does not track it.</para>
    ///
    /// <para>The <c>internal</c> members exist for the P1 seam tests, which must not
    /// re-replicate anything pinned here.</para>
    /// </summary>
    public sealed class PolicyVersionFreezeTests
    {
        internal const int FreezeSeed = 42;
        internal const int FreezeFounders = 12;
        internal const int PinCTicks = 2000;            // C_HORIZON from the capture
        internal const int PinCSeedCount = 1;
        internal const string PinCRevision = "p0-pin-c";
        private const int PinDTicks = 2666;             // sample 1 of a 24,000-tick, 9-sample trajectory
        private const int DefaultTicks = 2000;

        // --- captured constants (sentinels until Task 4 step 4 of the plan) ---
        private const ulong CapturedAState = ulong.MaxValue;
        private const ulong CapturedABehavior = ulong.MaxValue;
        private const ulong CapturedAFingerprint = ulong.MaxValue;
        private const ulong CapturedAConfiguration = ulong.MaxValue;
        private const ulong CapturedBState = ulong.MaxValue;
        private const ulong CapturedBBehavior = ulong.MaxValue;
        private const ulong CapturedBFingerprint = ulong.MaxValue;
        private const ulong CapturedBConfiguration = ulong.MaxValue;
        private const ulong CapturedCState = ulong.MaxValue;
        private const ulong CapturedCBehavior = ulong.MaxValue;
        private const ulong CapturedCFingerprint = ulong.MaxValue;
        private const ulong CapturedCConfiguration = ulong.MaxValue;
        private const ulong CapturedDState = ulong.MaxValue;
        private const ulong CapturedDFingerprint = ulong.MaxValue;
        private const ulong CapturedDConfiguration = ulong.MaxValue;
        internal const string PinCCanonicalManifest = "SENTINEL-NOT-CAPTURED";

        /// <summary>Not captured: the committed artefact's value, arm control, seed 42, tick 2,666.</summary>
        private const ulong CommittedC3ControlBehaviorHash = 663693199115149672UL;

        internal static SimulationScenario RecordedPredationScenario =>
            Prototype4Scenarios.ConsumerDefenseCalibrationModerate.WithRegeneration("p6-defense-calibration-regen2.00", 2f);

        /// <summary>Mirror of the sweep tool's CreateConfig(seed, slope: false) for
        /// <c>--focused 1 500 --regen=2.0 --brake=1.0 --predation --gate=0.45 --mate-selection=off</c>.</summary>
        internal static SimulationConfig RecordedPredationCell(int worldSeed)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, FreezeFounders);
            return new SimulationConfig(
                worldSeed,
                FreezeFounders,
                defaults.Schedule,
                500,
                FounderProfile.PredationVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: false,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: false,
                terrainDrivenTemperatureEnabled: false,
                metabolicIngestionEnabled: false,
                reproductionNeedFraction: 0.45f,
                healthRecoveryEnabled: false,
                metabolicHealingEnabled: false,
                gradedFertilityEnabled: true,
                gradedFertilityStrength: 1.0f,
                evasiveFleeingEnabled: false,
                evasiveFleeingStrength: SimulationConfig.DefaultEvasiveFleeingStrength);
        }

        /// <summary>Mirror of the sweep tool's CreateConfig(seed, slope: false) for
        /// <c>--deaths 24 500 --regen=2.0 --brake=1.5 --ticks=24000</c>.</summary>
        internal static SimulationConfig C3Control(int worldSeed)
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(worldSeed, FreezeFounders);
            return new SimulationConfig(
                worldSeed,
                FreezeFounders,
                defaults.Schedule,
                500,
                FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: true,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true,
                terrainDrivenEnvironmentEnabled: true,
                slopeMovementCostEnabled: false,
                terrainDrivenTemperatureEnabled: false,
                metabolicIngestionEnabled: false,
                reproductionNeedFraction: SimulationConfig.DefaultReproductionNeedFraction,
                healthRecoveryEnabled: false,
                metabolicHealingEnabled: false,
                gradedFertilityEnabled: true,
                gradedFertilityStrength: 1.5f,
                evasiveFleeingEnabled: false,
                evasiveFleeingStrength: SimulationConfig.DefaultEvasiveFleeingStrength);
        }

        /// <summary>The sweep tools' loop, exactly: Step, then clear the event buffer, per tick.</summary>
        internal static SimulationWorld RunSettled(SimulationConfig config, SimulationScenario scenario, int ticks)
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

        private static void AssertPinned(SimulationWorld world, SimulationConfig config, ulong state, ulong behavior, ulong fingerprint, ulong configuration)
        {
            Assert.That(world.ComputeStateHash(), Is.EqualTo(state), "state hash");
            Assert.That(world.ComputeBehaviorHash(), Is.EqualTo(behavior), "behavior hash");
            Assert.That(world.ComputeStateFingerprint(), Is.EqualTo(fingerprint), "state fingerprint");
            Assert.That(config.ComputeConfigurationHash(), Is.EqualTo(configuration), "configuration hash");
        }

        [Test]
        public void PinA_Prototype4DefaultsOnTheModerateCalibration()
        {
            SimulationConfig config = SimulationConfig.CreatePrototype4Defaults(FreezeSeed, FreezeFounders);
            SimulationWorld world = RunSettled(config, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, DefaultTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(DefaultTicks));
            Assert.That(world.CaptureStatistics().PredationDeathCount, Is.EqualTo(0), "A is not a predation pin");
            AssertPinned(world, config, CapturedAState, CapturedABehavior, CapturedAFingerprint, CapturedAConfiguration);
        }

        [Test]
        public void PinB_FullEcosystemDefaultsOnTheModerateCalibration()
        {
            SimulationConfig config = SimulationConfig.CreateFullEcosystemDefaults(FreezeSeed, FreezeFounders);
            SimulationWorld world = RunSettled(config, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, DefaultTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(DefaultTicks));
            Assert.That(world.CaptureStatistics().PredationDeathCount, Is.EqualTo(0), "B is not a predation pin");
            AssertPinned(world, config, CapturedBState, CapturedBBehavior, CapturedBFingerprint, CapturedBConfiguration);
        }

        [Test]
        public void PinC_RecordedPredationCell()
        {
            SimulationConfig config = RecordedPredationCell(FreezeSeed);
            SimulationWorld world = RunSettled(config, RecordedPredationScenario, PinCTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(PinCTicks));
            Assert.That(world.CaptureStatistics().AttackHitCount, Is.GreaterThan(0), "a predation pin in which no attack landed pins nothing about predation");
            AssertPinned(world, config, CapturedCState, CapturedCBehavior, CapturedCFingerprint, CapturedCConfiguration);
        }

        [Test]
        public void PinC_CanonicalManifestUnderTheFixedRevision()
        {
            string manifest = ExperimentManifest.Describe(
                PinCRevision, RecordedPredationScenario, RecordedPredationCell(FreezeSeed), FreezeSeed, PinCSeedCount, PinCTicks);

            Assert.That(manifest, Is.EqualTo(PinCCanonicalManifest));
        }

        [Test]
        public void PinD_C3ControlReproducesTheCommittedArtefact()
        {
            // docs/experiments/p6-deaths-perseed-cap500-regen2.00-24seeds-brake1.5-24000ticks-9samples-2026-09-10.csv,
            // arm control, seed 42, sample 1 of 9, tick 2,666. The behaviour hash is the committed
            // number; the other three are captured beside it.
            SimulationConfig config = C3Control(FreezeSeed);
            SimulationWorld world = RunSettled(config, RecordedPredationScenario, PinDTicks);

            Assert.That(world.CurrentTick, Is.EqualTo(PinDTicks));
            AssertPinned(world, config, CapturedDState, CommittedC3ControlBehaviorHash, CapturedDFingerprint, CapturedDConfiguration);
        }
    }
}
```

Set `PinCTicks` to `C_HORIZON` now if it is not 2000.

- [ ] **Step 2: Run the new tests and verify they fail on the sentinels**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~PolicyVersionFreezeTests"
```

Expected: **5 failed, 0 passed.** Each of the four hash pins fails at the first `AssertPinned` line with `Expected: 18446744073709551615 But was: <captured value>` — the `But was` value must equal the Task 3 capture for that pin (a second, independent confirmation of the capture). The manifest test fails with the sentinel string. `PinD` fails at its **state hash** line; its behaviour-hash assertion, which follows and carries the committed value rather than a sentinel, is not reached in this run. If any of the five passes, the sentinel did not bite: stop and report.

- [ ] **Step 3: Confirm the failure values match the capture**

Compare each `But was:` value in the output to `capture.txt`. All must match. A mismatch means the probe and the test do not build the same world: stop and report which pin.

- [ ] **Step 4: Replace every sentinel with the captured value**

Edit the constants block only. For each pin, replace `ulong.MaxValue` with the exact `X_STATE`, `X_BEHAVIOR`, `X_FINGERPRINT`, `X_CONFIG` value from `capture.txt` with a `UL` suffix. Replace `PinCCanonicalManifest` with the complete text of `canonical-manifest.txt` as a C# string: each line followed by `\n`, `"` escaped, no `\r`. Example shape (values illustrative of format only — use the captured text):

```csharp
        internal const string PinCCanonicalManifest =
            "schema=1\n"
            + "code_revision=p0-pin-c\n"
            + "scenario_id=p6-defense-calibration-regen2.00\n"
            + /* …every remaining captured line, in order, each ending in \n… */
            + "ThreatFalloffDistance=…\n";
```

Do not retype from memory; paste from the file. Delete the "sentinels until…" comment.

- [ ] **Step 5: Run the new tests and verify they pass**

```powershell
dotnet test tools/HeadlessTests --nologo --filter "FullyQualifiedName~PolicyVersionFreezeTests"
```

Expected: **5 passed, 0 failed.** Any failure: the pasted value is wrong — compare against `capture.txt` again; if it matches and still fails, stop and report.

---

### Task 5: Remove the probe, run the full suite twice, review the diff, commit one file

**Files:**
- Delete: `Assets/Tests/EditMode/ZZZFreezeCapture.cs`
- Commit: `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs` only

- [ ] **Step 1: Delete the probe and confirm the working tree contains only the one file**

```powershell
Remove-Item Assets/Tests/EditMode/ZZZFreezeCapture.cs
git status --porcelain
```

Expected exactly one line: `?? Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`. Anything else (a `.meta`, a CSV under `docs/experiments/`, a `ZZZ*` file, a modified file): remove or restore it, then re-check. If a file that is *required* for the tests appears here, stop and report — the authorised scope is one file.

- [ ] **Step 2: Full suite, first run**

```powershell
dotnet test tools/HeadlessTests --nologo
```

Expected: `Failed: 0`, passed count = Task 1 step 2 count + 5. Any failure outside `PolicyVersionFreezeTests`: stop and report verbatim.

- [ ] **Step 3: Full suite, second run**

```powershell
dotnet test tools/HeadlessTests --nologo
```

Expected: identical counts. `LivenessTests` included and green.

- [ ] **Step 4: Diff review**

```powershell
git diff --stat ebab85b -- Assets/Scripts tools docs
git status --porcelain
Select-String -Path Assets/Tests/EditMode/PolicyVersionFreezeTests.cs -Pattern "ulong.MaxValue|SENTINEL" 
```

Expected: the first command prints nothing (no production, tool or doc change); the second prints only the one untracked test file; the third prints nothing (no sentinel survives).

- [ ] **Step 5: Commit exactly one file**

```powershell
git add Assets/Tests/EditMode/PolicyVersionFreezeTests.cs
git commit -m "test: freeze the live IntentUtilityV1 path at 65bae69 with four literal pins" -m "P0 of the V2 seam spec. Pins state, behaviour, fingerprint and configuration hashes for the P4 baseline, the full-ecosystem surface, the recorded predation cell (cross-checked against a real CreatureSweep run: emitted seed, behaviour hash and normalised manifest all matched) and the C3 control, whose behaviour hash reproduces the committed 2026-09-10 artefact. Also stores the canonical pin-C manifest under the fixed revision p0-pin-c for P1. Tests only; no production file touched." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
git show --stat HEAD
git status --porcelain
```

Expected: `git show --stat HEAD` lists exactly `Assets/Tests/EditMode/PolicyVersionFreezeTests.cs`, `1 file changed`; `status` empty.

- [ ] **Step 6: Report completion in chat**

State: the commit hash; `C_HORIZON`; the emitted sweep seed (42) and that hash and manifest matched; that pin D reproduced `663693199115149672`; both full-suite counts; that the probe and CSV were deleted; that the scratch capture files remain under `%LOCALAPPDATA%\Temp\claude\…\scratchpad\p0\` for the reviewer. Do not write a completion file. Do not push and do not start P1.

---

## Self-review against the spec (done while writing)

- §6.1 authorised file → Task 4/5 create and commit only `PolicyVersionFreezeTests.cs`; probe transient (Task 3, deleted Task 5). ✔
- Four pins A–D with the spec's configurations, scenarios and horizons → Task 3 capture, Task 4 tests. Horizon rule for C (earliest deterministic attack, else 2,000) → Task 3 step 1 loop, `PinCTicks`. ✔
- Per pin: state, behaviour, fingerprint, configuration hash literals → `AssertPinned`. A/B `PredationDeathCount == 0`; C `AttackHitCount > 0`. ✔
- Sweep cross-check: real tool at baseline (Task 2); seed must be 42 (Task 2 step 2 stop); behaviour hash equality (Task 3 step 4); both manifests normalised by exactly `code_revision` and `SlopeMovementCostEnabled` and compared in full (Task 2 step 3, Task 3 step 6). CSV deleted (Task 2 step 4). ✔
- Canonical manifest under `p0-pin-c`, complete text including `code_revision=p0-pin-c` and `SlopeMovementCostEnabled=false`, stored as `internal const string` with `PinCTicks`, `PinCSeedCount`, `FreezeSeed` as `internal const` and `RecordedPredationCell` as `internal static` → Task 3 step 7, Task 4. ✔
- Pin D reproduces the committed artefact, never a fresh number → `CommittedC3ControlBehaviorHash` is a spec constant, not a capture; mismatch is a stop (Task 3 step 5). ✔
- Characterization discipline → sentinels fail first (Task 4 step 2), independent confirmation against the capture (step 3), then pass (step 5). ✔
- Two full-suite runs, diff review, one-file commit check → Task 5. ✔
- "Four configurations, not 98" in the file header → Task 4 doc comment. ✔
- Completion in chat, no completion file → Task 5 step 6. ✔
- Windows PowerShell: all commands are PowerShell; `git` via the PowerShell tool because the Bash hook blocks it in this worktree. ✔

## Ambiguities found while planning (for the reviewer)

1. **Test configuration versus `-c Release`.** The sweep tool is built Release; `dotnet test` builds the test project in its default configuration. The plan does not assume they agree — pin D (committed Release artefact reproduced under test) and Task 3 step 4 (probe hash equals tool hash) are the checks, and both are stop conditions if they disagree.
2. **`ticks=` in the manifest depends on `C_HORIZON`.** If the horizon extends past 2,000, Task 2 must be rerun at that horizon so the tool header's `ticks=` line matches; the plan says so in Task 3 step 4, but the order of discovery (probe after sweep) means one extra sweep run in that case.
3. **`Events.Clear()` per tick.** The sweep loops clear the event buffer every tick; the existing test precedent (`IngestionRecorderTests`) does not. Neither hashing method reads the buffer and overflow does not touch simulation state, so both loops should hash identically; the plan mirrors the tools' loop anyway so pin D is a faithful reproduction rather than an argument.
