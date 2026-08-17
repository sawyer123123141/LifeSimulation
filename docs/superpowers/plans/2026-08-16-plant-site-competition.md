# Plant Site Competition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a fitter disperser displace a struggling (low-biomass) occupied plant patch instead of being restricted to empty sites only, so plant genome/defense actually has a spatial fitness consequence.

**Architecture:** `PlantReproductionSystem.FindSite` gains an opt-in second acceptance path for occupied-but-vulnerable candidates, reusing the exact same distance/establishment-roll logic already used for empty sites. `PlantReproductionSystem.Step` branches on whether the found site was empty or occupied: empty keeps the existing `patches.Add` path, occupied calls a new `PlantPatchStore.ReplaceAt` that overwrites the resident's traits/genome/lineage in place while carrying its existing biomass forward (biomass-conserving takeover). A new `SimulationConfig.PlantSiteCompetitionEnabled` flag (default `false`) gates all of it; `false` is byte-identical to today's behavior.

**Tech Stack:** C# (.NET), NUnit (EditMode tests), this project's existing `PlantPatchStore` / `PlantReproductionSystem` / `SimulationConfig` classes.

## Global Constraints

- Vulnerability threshold: a patch is vulnerable to takeover when `Biomass / Capacity < 0.25f` (`VulnerabilityFraction`). At or above 25%, a patch can never be displaced.
- No new RNG domain — occupied candidates reuse `RandomDomain.PlantDispersal` and `RandomDomain.PlantEstablishment` identically to empty candidates. Takeover is exactly as likely as ordinary empty-site establishment at the same distance, gated purely by the resident's vulnerability.
- Biomass-conserving: on takeover, the new occupant's starting biomass is `transferred seed biomass + resident's prior biomass`, capped at the site's capacity. Nothing is created or destroyed.
- `PlantPatchStore.ReplaceAt` preserves the existing patch's `Id`, `FoodResourceId`, `Position`, and `Capacity` (site-owned, not occupant-owned) and explicitly zeroes `ReproductionCooldownRemaining` on the new occupant.
- New flag `SimulationConfig.PlantSiteCompetitionEnabled`, default `false`, appended as the constructor's new last optional parameter (after `mateSelectionEnabled`) with a matching `{ get; }` property placed immediately after `MateSelectionEnabled`'s property (`SimulationConfig.cs:176`).
- When the flag is `false`, behavior is byte-identical to today: the standard `PredationVariation`/`Legacy` hash-regression scenario (`SimulationSchedule(60,60,30,10,10,10,5,1)`, `worldSeed:99`, `initialPopulation:2`, `founderProfile:FounderProfile.PredationVariation`, 50 `Step()` calls, no other flags set) must still produce `12050501592762519865UL` — this scenario never sets `PlantCohortsEnabled` at all, so it is trivially unaffected, but the regression test must still exist per this project's established convention. Derive it fresh from a throwaway worktree at the pre-task commit rather than assuming.
- A parent must never contest its own site (`candidate.Id` must not equal `parent.FoodResourceId`) — prevents a degenerate self-takeover edge case where a parent's own patch, if drawn as a dispersal candidate while critically low on biomass, could overwrite itself.

---

## File Structure

- `Assets/Scripts/Simulation/Core/SimulationConfig.cs` — add `PlantSiteCompetitionEnabled` flag (constructor param + property).
- `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs` — add `ReplaceAt` method.
- `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs` — extend `FindSite` and `Step` to support occupied-candidate takeover.
- `Assets/Scripts/Simulation/Core/SimulationWorld.cs` — thread `Config.PlantSiteCompetitionEnabled` into the existing `PlantReproductionSystem.Step` call site (`SimulationWorld.cs:246`).
- `Assets/Tests/EditMode/PlantGrowthTests.cs` — add new tests for `ReplaceAt` and takeover behavior (existing file already holds all `PlantReproductionSystem`/`PlantPatchStore` tests — follow its established patterns).
- `Assets/Tests/EditMode/ResourceExperimentTests.cs` (or a hash-regression test file if one already exists elsewhere — verify at execution time via `grep -rl "12050501592762519865" Assets/Tests` before choosing a file; if none exists, add to `PlantGrowthTests.cs`) — hash-regression test.

## Task 1: Plant site competition (flag, store method, dispersal takeover)

**Files:**
- Modify: `Assets/Scripts/Simulation/Core/SimulationConfig.cs:87-147` (constructor), `:149-177` (properties)
- Modify: `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs` (add `ReplaceAt` after existing `SetGenomeAndLineage`, around line 82)
- Modify: `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs:1-77` (whole file — `FindSite` and `Step`)
- Modify: `Assets/Scripts/Simulation/Core/SimulationWorld.cs:246`
- Test: `Assets/Tests/EditMode/PlantGrowthTests.cs`

**Interfaces:**
- Consumes: `PlantPatchState` (existing, `Assets/Scripts/Simulation/Environment/PlantTypes.cs:12-45` — fields `Id: PlantPatchId`, `FoodResourceId: ResourceId`, `Biomass: float`, `Capacity: float`, `GrowthRate: float`, `Nutrition: float`, `Defense: float`, `Genome: PlantGenome`, `Lineage: PlantLineage`). `PlantLineage(PlantPatchId lineageId, PlantPatchId parentId, int generation)` (`PlantTypes.cs:49`). `ResourceState.Id: ResourceId`, `.IsActive: bool`, `.Capacity: float` (`ResourceTypes.cs:38-74`). `PlantPatchStore.FindIndex(ResourceId foodResourceId): int` (existing, `PlantPatchStore.cs:84-92`).
- Produces: `PlantPatchStore.ReplaceAt(int index, PlantGenome genome, PlantLineage lineage, float biomass, float growthRate, float nutrition, float defense): void`. `SimulationConfig.PlantSiteCompetitionEnabled: bool`. `PlantReproductionSystem.Step(..., bool competitionEnabled = false): int` (new trailing optional parameter — existing call sites without it are unaffected).

- [ ] **Step 1: Write failing tests for `SimulationConfig.PlantSiteCompetitionEnabled`**

Open `Assets/Tests/EditMode/PlantGrowthTests.cs` and add at the end of the class, before the final closing brace:

```csharp
        [Test]
        public void PlantSiteCompetitionEnabledDefaultsToFalse()
        {
            var config = SimulationConfig.CreatePrototype4Defaults(42, 4);
            Assert.That(config.PlantSiteCompetitionEnabled, Is.False);
        }

        [Test]
        public void PlantSiteCompetitionEnabledCanBeSetTrue()
        {
            var defaults = SimulationConfig.CreatePrototype4Defaults(42, 4);
            var config = new SimulationConfig(
                42,
                4,
                defaults.Schedule,
                defaults.MaximumPopulation,
                defaults.FounderProfile,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                plantSiteCompetitionEnabled: true);

            Assert.That(config.PlantSiteCompetitionEnabled, Is.True);
        }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "PlantSiteCompetitionEnabledDefaultsToFalse|PlantSiteCompetitionEnabledCanBeSetTrue"`
Expected: FAIL — `SimulationConfig` does not contain a definition for `PlantSiteCompetitionEnabled` / no parameter named `plantSiteCompetitionEnabled` (compile error).

- [ ] **Step 3: Add the flag to `SimulationConfig`**

In `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, change the constructor signature (currently ending at line 116 with `bool mateSelectionEnabled = false)`):

```csharp
            bool mateSelectionEnabled = false,
            bool plantSiteCompetitionEnabled = false)
```

In the constructor body, immediately after `MateSelectionEnabled = mateSelectionEnabled;` (line 146):

```csharp
            MateSelectionEnabled = mateSelectionEnabled;
            PlantSiteCompetitionEnabled = plantSiteCompetitionEnabled;
```

Immediately after the `MateSelectionEnabled` property (line 176):

```csharp
        public bool MateSelectionEnabled { get; }
        public bool PlantSiteCompetitionEnabled { get; }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "PlantSiteCompetitionEnabledDefaultsToFalse|PlantSiteCompetitionEnabledCanBeSetTrue"`
Expected: PASS (2/2)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationConfig.cs Assets/Tests/EditMode/PlantGrowthTests.cs
git commit -m "feat: add PlantSiteCompetitionEnabled config flag"
```

- [ ] **Step 6: Write failing test for `PlantPatchStore.ReplaceAt`**

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs`:

```csharp
        [Test]
        public void ReplaceAtOverwritesTraitsAndGenomeButPreservesSiteIdentity()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(7), new SimVector2(3f, 4f), 5f, 20f, .1f, 1f, 0f);
            var newGenome = new PlantGenome(.9f, .1f, .2f, .3f, .4f, .5f, .6f, .7f);
            var newLineage = new PlantLineage(patches.GetAt(index).Id, new PlantPatchId(11), 3);

            patches.ReplaceAt(index, newGenome, newLineage, biomass: 15f, growthRate: .3f, nutrition: .6f, defense: .2f);

            PlantPatchState result = patches.GetAt(index);
            Assert.That(result.Id, Is.EqualTo(patches.GetAt(index).Id));
            Assert.That(result.FoodResourceId, Is.EqualTo(new ResourceId(7)));
            Assert.That(result.Position.X, Is.EqualTo(3f));
            Assert.That(result.Position.Y, Is.EqualTo(4f));
            Assert.That(result.Capacity, Is.EqualTo(20f));
            Assert.That(result.Biomass, Is.EqualTo(15f));
            Assert.That(result.GrowthRate, Is.EqualTo(.3f));
            Assert.That(result.Nutrition, Is.EqualTo(.6f));
            Assert.That(result.Defense, Is.EqualTo(.2f));
            Assert.That(result.Genome.Dispersal, Is.EqualTo(newGenome.Dispersal));
            Assert.That(result.Lineage.Generation, Is.EqualTo(3));
            Assert.That(result.ReproductionCooldownRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void ReplaceAtClampsBiomassToCapacity()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(7), new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);

            patches.ReplaceAt(index, PlantGenome.Neutral, new PlantLineage(patches.GetAt(index).Id, default, 1), biomass: 999f, growthRate: .1f, nutrition: 1f, defense: 0f);

            Assert.That(patches.GetAt(index).Biomass, Is.EqualTo(10f));
        }
```

If `PlantGenome`'s constructor signature or field names (`Dispersal`, etc.) differ from what's shown above, read `Assets/Scripts/Simulation/Environment/PlantTypes.cs` (or wherever `PlantGenome` is actually declared — search for `struct PlantGenome` or `class PlantGenome` first) and adjust the test to use its real constructor parameters and a real field name before proceeding; do not guess.

- [ ] **Step 7: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "ReplaceAtOverwritesTraitsAndGenomeButPreservesSiteIdentity|ReplaceAtClampsBiomassToCapacity"`
Expected: FAIL — `PlantPatchStore` does not contain a definition for `ReplaceAt` (compile error).

- [ ] **Step 8: Implement `PlantPatchStore.ReplaceAt`**

In `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`, add immediately after `SetGenomeAndLineage` (after line 82, before `FindIndex`):

```csharp
        public void ReplaceAt(int index, PlantGenome genome, PlantLineage lineage, float biomass, float growthRate, float nutrition, float defense)
        {
            if ((uint)index >= (uint)Count) return;
            _genomes[index] = genome;
            _lineages[index] = lineage;
            _growthRates[index] = growthRate;
            _nutrition[index] = nutrition;
            _defense[index] = defense;
            _biomass[index] = Math.Max(0f, Math.Min(_capacities[index], biomass));
            _reproductionCooldowns[index] = 0f;
        }
```

- [ ] **Step 9: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "ReplaceAtOverwritesTraitsAndGenomeButPreservesSiteIdentity|ReplaceAtClampsBiomassToCapacity"`
Expected: PASS (2/2)

- [ ] **Step 10: Commit**

```bash
git add Assets/Scripts/Simulation/Environment/PlantPatchStore.cs Assets/Tests/EditMode/PlantGrowthTests.cs
git commit -m "feat: add PlantPatchStore.ReplaceAt for in-place patch takeover"
```

- [ ] **Step 11: Write failing tests for dispersal takeover behavior**

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs`:

```csharp
        [Test]
        public void CompetitionDisabledNeverConsidersAnOccupiedCandidateEvenIfVulnerable()
        {
            var resources = new ResourceStore(1);
            ResourceId occupiedSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 1f, 20f, 0f);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            int occupantIndex = patches.Add(occupiedSite, new SimVector2(1f, 0f), 1f, 20f, .2f, .8f, .1f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal, competitionEnabled: false);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(patches.Count, Is.EqualTo(2));
            Assert.That(patches.GetAt(occupantIndex).Biomass, Is.EqualTo(1f));
            Assert.That(patches.GetAt(parentIndex).Biomass, Is.EqualTo(10f));
        }

        [Test]
        public void CompetitionEnabledLetsAVulnerableOccupiedSiteBeTakenOverByADisperser()
        {
            var resources = new ResourceStore(1);
            ResourceId occupiedSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 1f, 20f, 0f);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            int occupantIndex = patches.Add(occupiedSite, new SimVector2(1f, 0f), 1f, 20f, .2f, .8f, .1f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal, competitionEnabled: true);

            Assert.That(births, Is.EqualTo(1));
            Assert.That(patches.Count, Is.EqualTo(2));
            PlantPatchState takenOver = patches.GetAt(occupantIndex);
            Assert.That(takenOver.GrowthRate, Is.EqualTo(.1f));
            Assert.That(takenOver.Nutrition, Is.EqualTo(1f));
            Assert.That(takenOver.Defense, Is.EqualTo(0f));
            Assert.That(takenOver.Lineage.Generation, Is.EqualTo(patches.GetAt(parentIndex).Lineage.Generation + 1));
            Assert.That(takenOver.ReproductionCooldownRemaining, Is.EqualTo(0f));
            // Biomass conservation: nothing created or destroyed, only moved.
            float totalAfter = patches.GetAt(parentIndex).Biomass + takenOver.Biomass;
            Assert.That(totalAfter, Is.EqualTo(10f + 1f).Within(.0001f));
        }

        [Test]
        public void CompetitionEnabledNeverDisplacesANonVulnerableOccupiedSite()
        {
            var resources = new ResourceStore(1);
            ResourceId occupiedSite = resources.Add(ResourceKind.Food, new SimVector2(1f, 0f), 1f, 5f, 20f, 0f);
            var sites = new PlantSiteRegistry(1);
            sites.Register(0);
            var patches = new PlantPatchStore(2);
            int parentIndex = patches.Add(new ResourceId(99), new SimVector2(0f, 0f), 10f, 10f, .1f, 1f, 0f);
            int occupantIndex = patches.Add(occupiedSite, new SimVector2(1f, 0f), 5f, 20f, .2f, .8f, .1f);
            long ordinal = 0;

            int births = PlantReproductionSystem.Step(patches, resources, sites, 42, 20, 1f, ref ordinal, competitionEnabled: true);

            Assert.That(births, Is.EqualTo(0));
            Assert.That(patches.Count, Is.EqualTo(2));
            Assert.That(patches.GetAt(occupantIndex).Biomass, Is.EqualTo(5f));
            Assert.That(patches.GetAt(occupantIndex).GrowthRate, Is.EqualTo(.2f));
            Assert.That(patches.GetAt(parentIndex).Biomass, Is.EqualTo(10f));
        }
```

Note: `CompetitionDisabledNeverConsidersAnOccupiedCandidateEvenIfVulnerable` and `CompetitionEnabledLetsAVulnerableOccupiedSiteBeTakenOverByADisperser` reuse `worldSeed: 42, tick: 20` — this exact combination is already proven in this file's existing `MaturePlantTransfersBiomassToADeterministicClonalSeedling` test (line 82 as read this session) to make the parent's first dispersal attempt succeed its establishment roll at distance 1 against a Neutral-genome dispersal range of 14. Reusing it here guarantees the establishment roll succeeds deterministically without needing to replay `DeterministicRandom`'s output by hand. `CompetitionEnabledNeverDisplacesANonVulnerableOccupiedSite` does not depend on the roll succeeding — occupant immunity is checked before the distance/roll logic runs, so the site is rejected on every attempt regardless of seed/tick.

- [ ] **Step 12: Run tests to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "CompetitionDisabledNeverConsidersAnOccupiedCandidateEvenIfVulnerable|CompetitionEnabledLetsAVulnerableOccupiedSiteBeTakenOverByADisperser|CompetitionEnabledNeverDisplacesANonVulnerableOccupiedSite"`
Expected: FAIL — `Step` has no overload taking a `competitionEnabled` named argument (compile error).

- [ ] **Step 13: Implement takeover logic in `PlantReproductionSystem`**

Replace the entire contents of `Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs` with:

```csharp
using System;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantReproductionSystem
    {
        private const float MaturityFraction = .75f;
        private const float MutationStandardDeviation = .03f;
        private const int SiteAttempts = 4;
        private const float ReproductionCooldownSeconds = 20f;
        private const float VulnerabilityFraction = .25f;

        public static int Step(PlantPatchStore patches, ResourceStore resources, PlantSiteRegistry sites, int worldSeed, long tick, float deltaTime, ref long seedOrdinal, bool competitionEnabled = false)
        {
            int parentCount = patches.Count;
            int births = 0;
            for (int parentIndex = 0; parentIndex < parentCount; parentIndex++)
            {
                PlantPatchState parent = patches.GetAt(parentIndex);
                if (parent.ReproductionCooldownRemaining > 0f)
                {
                    float remaining = Math.Max(0f, parent.ReproductionCooldownRemaining - deltaTime);
                    patches.SetReproductionCooldown(parentIndex, remaining);
                    if (remaining > 0f) continue;
                }
                if (parent.Biomass < parent.Capacity * MaturityFraction) continue;
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(parent.Genome);
                float seedBiomass = parent.Biomass * phenotype.SeedInvestmentFraction;
                int siteIndex = FindSite(resources, sites, patches, parent, worldSeed, tick, seedOrdinal, phenotype.DispersalRange, competitionEnabled);
                if (siteIndex < 0) continue;

                ResourceState site = resources.GetAt(siteIndex);
                float transferred = patches.ConsumeAt(parentIndex, seedBiomass);
                if (transferred <= 0f) continue;
                PlantGenome childGenome = PlantGenome.CloneMutated(parent.Genome, worldSeed, seedOrdinal++, MutationStandardDeviation);

                if (site.IsActive)
                {
                    int occupantIndex = patches.FindIndex(site.Id);
                    if (occupantIndex < 0) continue;
                    PlantPatchState occupant = patches.GetAt(occupantIndex);
                    float takenOverBiomass = Math.Min(site.Capacity, transferred + occupant.Biomass);
                    var takeoverLineage = new PlantLineage(occupant.Id, parent.Id, parent.Lineage.Generation + 1);
                    patches.ReplaceAt(occupantIndex, childGenome, takeoverLineage, takenOverBiomass, parent.GrowthRate, parent.Nutrition, parent.Defense);
                }
                else
                {
                    int childIndex = patches.Add(site.Id, site.Position, transferred, site.Capacity, parent.GrowthRate, parent.Nutrition, parent.Defense);
                    PlantPatchState child = patches.GetAt(childIndex);
                    patches.SetGenomeAndLineage(childIndex, childGenome, new PlantLineage(child.Id, parent.Id, parent.Lineage.Generation + 1));
                }

                resources.SetActiveAt(siteIndex, true);
                patches.SetReproductionCooldown(parentIndex, ReproductionCooldownSeconds);
                births++;
            }

            return births;
        }

        public static float EstablishmentSuccessProbability(float distance, float dispersalRange)
        {
            float range = Math.Max(.01f, dispersalRange);
            float normalizedDistance = Math.Min(1f, Math.Max(0f, distance / range));
            return 1f - normalizedDistance;
        }

        private static int FindSite(ResourceStore resources, PlantSiteRegistry sites, PlantPatchStore patches, PlantPatchState parent, int seed, long tick, long ordinal, float range, bool competitionEnabled)
        {
            if (sites.Count == 0) return -1;

            for (int attempt = 0; attempt < SiteAttempts; attempt++)
            {
                int slot = (int)(DeterministicRandom.Float01(seed, RandomDomain.PlantDispersal, tick, parent.Id.Value, ordinal, attempt) * sites.Count);
                int index = sites.GetResourceIndexAt(slot);
                ResourceState candidate = resources.GetAt(index);
                if (candidate.Kind != ResourceKind.Food) continue;

                if (candidate.IsActive)
                {
                    if (!competitionEnabled) continue;
                    if (candidate.Id.Equals(parent.FoodResourceId)) continue;
                    int occupantIndex = patches.FindIndex(candidate.Id);
                    if (occupantIndex < 0) continue;
                    PlantPatchState occupant = patches.GetAt(occupantIndex);
                    if (occupant.Capacity <= 0f) continue;
                    if (occupant.Biomass / occupant.Capacity >= VulnerabilityFraction) continue;
                }

                float distance = SimVector2.Distance(parent.Position, candidate.Position);
                if (distance > range) continue;

                float establishmentRoll = DeterministicRandom.Float01(seed, RandomDomain.PlantEstablishment, tick, parent.Id.Value, ordinal, attempt);
                if (establishmentRoll > EstablishmentSuccessProbability(distance, range)) continue;

                return index;
            }
            return -1;
        }
    }
}
```

- [ ] **Step 14: Run tests to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "CompetitionDisabledNeverConsidersAnOccupiedCandidateEvenIfVulnerable|CompetitionEnabledLetsAVulnerableOccupiedSiteBeTakenOverByADisperser|CompetitionEnabledNeverDisplacesANonVulnerableOccupiedSite"`
Expected: PASS (3/3)

- [ ] **Step 15: Run the full existing suite to confirm no regressions**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: All tests pass, including every pre-existing `PlantReproductionSystem`/`PlantGrowthTests` test (they all call `Step` without the new trailing parameter, which defaults to `false` — behavior for them is unchanged).

- [ ] **Step 16: Commit**

```bash
git add Assets/Scripts/Simulation/Environment/PlantReproductionSystem.cs Assets/Tests/EditMode/PlantGrowthTests.cs
git commit -m "feat: allow plant dispersal to take over vulnerable occupied sites"
```

- [ ] **Step 17: Thread the flag through `SimulationWorld`'s call site**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, verify the current call site is still at (or near) line 246 by reading the file fresh — line numbers may have shifted from earlier commits in this task. Find:

```csharp
                    _plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, resourceDeltaTime, ref _plantSeedOrdinal);
```

Replace with:

```csharp
                    _plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, resourceDeltaTime, ref _plantSeedOrdinal, Config.PlantSiteCompetitionEnabled);
```

- [ ] **Step 18: Run the full suite again**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: All tests still pass (this call site has no dedicated test beyond the full-suite integration coverage already exercised by `ExperimentRunner`-based tests, which never set `PlantSiteCompetitionEnabled: true`, so results are unchanged).

- [ ] **Step 19: Commit**

```bash
git add Assets/Scripts/Simulation/Core/SimulationWorld.cs
git commit -m "feat: wire PlantSiteCompetitionEnabled into the simulation tick"
```

- [ ] **Step 20: Derive and add the hash-regression test**

First, record the current commit (this is the pre-task commit for the throwaway-worktree hash derivation):

Run: `git log --oneline -1 main`

Note the hash printed (call it `PRE_TASK_COMMIT` below).

Check whether a hash-regression test for the standard `PredationVariation`/`Legacy` scenario already exists in this codebase and can simply be reused/extended, rather than duplicated:

Run: `grep -rl "12050501592762519865" Assets/Tests`

If a file is found, read it and confirm it already covers this exact scenario with no flags set (in which case this step needs no new test — skip to Step 21 and note in the commit message that the existing hash-regression test already covers this). If no file is found, add this test to `Assets/Tests/EditMode/PlantGrowthTests.cs`:

```csharp
        [Test]
        public void PlantSiteCompetitionFlagDoesNotAffectTheStandardHashRegressionScenario()
        {
            var config = new SimulationConfig(
                99,
                2,
                new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);
            world.Spawn();

            for (int i = 0; i < 50; i++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.ComputeStateHash(), Is.EqualTo(12050501592762519865UL));
        }
```

Run: `cd tools/HeadlessTests && dotnet test --filter "PlantSiteCompetitionFlagDoesNotAffectTheStandardHashRegressionScenario"`
Expected: PASS — confirms the hash is unaffected, since this scenario never sets `PlantCohortsEnabled` (let alone `PlantSiteCompetitionEnabled`), matching every other hash-regression test derived this session.

If the test fails with a different hash, STOP — this means something in this task's changes altered behavior even with the new flag at its default `false`, which violates the Global Constraints. Do not adjust the expected hash to match; find and fix the behavioral change instead.

- [ ] **Step 21: Run the full suite one final time**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: All tests pass.

- [ ] **Step 22: Commit**

```bash
git add Assets/Tests/EditMode/PlantGrowthTests.cs
git commit -m "test: confirm PlantSiteCompetitionEnabled leaves the standard hash-regression scenario unchanged"
```

---

## Self-Review Notes

- **Spec coverage:** Vulnerability threshold (Step 13's `VulnerabilityFraction`) ✅. Extended site search reusing existing RNG domains (Step 13's `FindSite`) ✅. Biomass-conserving takeover via `ReplaceAt` (Steps 8, 13) ✅. Flag default-false byte-identical behavior (Step 20's hash test) ✅. Self-takeover exclusion (Step 13's `candidate.Id.Equals(parent.FoodResourceId)` check — added during planning as a Global Constraint not originally enumerated as its own spec section, but directly serves the spec's "Scope boundary" intent of a narrowly-bounded mechanic) ✅.
- **Placeholder scan:** No TBD/TODO; the one conditional step (Step 20's existing-test-file check) gives an explicit, concrete instruction for either branch, not a placeholder.
- **Type consistency:** `ReplaceAt`'s signature in Step 8 matches its call in Step 13 exactly (`int, PlantGenome, PlantLineage, float, float, float, float`). `Step`'s new trailing `bool competitionEnabled = false` parameter matches its threading in Step 17 and its use in every new test in Step 11.
