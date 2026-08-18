# Plant Mortality Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give plant patches a finite, gene-linked lifespan so they die and free their site, letting the plant population actually turn over instead of freezing at generation 2.

**Architecture:** A new `PlantMortalitySystem` ages every patch each plant tick and removes those past a lifespan derived from their `Growth` gene. `PlantPatchStore` gains `AdvanceAge` and a swap-removing `RemoveAt`. Dying patches clear their food projection and deactivate their resource site, returning it to the dispersal pool. Removed biomass accrues to a cumulative counter that feeds the existing conservation residual. All of it sits behind a new default-off flag.

**Tech Stack:** C# (.NET), NUnit EditMode tests, `dotnet test` under `tools/HeadlessTests`.

## Global Constraints

- `LifespanSeconds = BaseLifespanSeconds * (1.5f - (0.75f * genome.Growth))` — slowest grower lives exactly twice as long as the fastest.
- Death rule is `Age >= phenotype.LifespanSeconds`. Fully deterministic — **no RNG draw, no new random domain.**
- `PlantMortalitySystem` iterates **backward** (`index = Count - 1` down to `0`) so swap-removal cannot skip the element moved into the vacated slot.
- On death, in this order: clear the food projection (`SetFoodProjection(id, 0f, 1f, 0f)`), deactivate the site (`SetActive(id, false)`), then `RemoveAt(index)`.
- Biomass held by a dying patch accrues to `_cumulativePlantBiomassLostToMortality` and **must be subtracted inside the existing conservation residual** at `SimulationWorld.cs:1536`, or the residual silently goes negative by exactly the mortality total.
- New `SimulationConfig.PlantMortalityEnabled`, default `false`, appended as the constructor's new last optional parameter after `plantSiteCompetitionEnabled`, with its `{ get; }` property immediately after `PlantSiteCompetitionEnabled` (`SimulationConfig.cs:179`).
- With the flag off, no patch ages and none is removed — behavior byte-identical, existing hash baselines unchanged. Current baseline for the standard `PredationVariation`/`Legacy` scenario is `13626802794646021369UL` (`CoreSimulationTests.cs:21,858`); derive it fresh from a throwaway worktree rather than assuming.
- No change to `PlantGrowthSystem`'s growth formula, `FindSite`'s dispersal/competition rules, or any existing plant genome trait. `SeedReserve` stays unwired.

---

## File Structure

- `Assets/Scripts/Simulation/Environment/PlantMortalitySystem.cs` — **new.** Ages patches, kills the expired, releases sites. Single responsibility.
- `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs` — add `AdvanceAge`, `RemoveAt`.
- `Assets/Scripts/Simulation/Environment/PlantGenome.cs` — `PlantPhenotype` gains `LifespanSeconds` (6th constructor parameter) and `BaseLifespanSeconds`.
- `Assets/Scripts/Simulation/Core/SimulationConfig.cs` — new flag.
- `Assets/Scripts/Simulation/Core/SimulationWorld.cs` — counter field, tick wiring, residual correction, statistics arg.
- `Assets/Scripts/Simulation/Core/SimulationTypes.cs` — new trailing statistics parameter + property.
- `Assets/Tests/EditMode/PlantGrowthTests.cs` — unit tests.
- `Assets/Tests/EditMode/ResourceExperimentTests.cs` — calibration + regression-guard integration tests.

## Task 1: Mortality mechanism

**Files:**
- Create: `Assets/Scripts/Simulation/Environment/PlantMortalitySystem.cs`
- Modify: `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`, `Assets/Scripts/Simulation/Environment/PlantGenome.cs`, `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, `Assets/Scripts/Simulation/Core/SimulationTypes.cs`
- Test: `Assets/Tests/EditMode/PlantGrowthTests.cs`

**Interfaces:**
- Consumes: `PlantPatchState` (fields `Id`, `FoodResourceId`, `Position`, `Biomass`, `Capacity`, `GrowthRate`, `Nutrition`, `Defense`, `Genome`, `Lineage`, `Age`, `SeedReserve`, `ReproductionCooldownRemaining`). `ResourceStore.SetFoodProjection(ResourceId id, float amount, float nutritionMultiplier, float plantDefense = 0f)` and `ResourceStore.SetActive(ResourceId id, bool isActive)`. `PlantPhenotype.FromGenome(PlantGenome)`.
- Produces: `PlantPatchStore.AdvanceAge(int index, float deltaTime)`, `PlantPatchStore.RemoveAt(int index)`, `PlantPhenotype.LifespanSeconds`, `PlantPhenotype.BaseLifespanSeconds` (const), `PlantMortalitySystem.Step(PlantPatchStore, ResourceStore, float) : float` returning biomass removed, `SimulationConfig.PlantMortalityEnabled`.

- [ ] **Step 1: Write failing tests for `PlantPatchStore.RemoveAt` and `AdvanceAge`**

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs`, before the class's closing brace:

```csharp
        [Test]
        public void RemoveAtSwapsTheLastPatchIntoTheVacatedSlotWithEveryFieldIntact()
        {
            var patches = new PlantPatchStore(3);
            patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 1f, 10f, .1f, 1f, 0f);
            patches.Add(new ResourceId(2), new SimVector2(1f, 1f), 2f, 20f, .2f, .9f, .1f);
            int lastIndex = patches.Add(new ResourceId(3), new SimVector2(3f, 4f), 7f, 30f, .3f, .8f, .2f);
            var lastGenome = new PlantGenome(.9f, .1f, .2f, .3f, .4f, .5f, .6f, .7f);
            patches.SetGenomeAndLineage(lastIndex, lastGenome, new PlantLineage(patches.GetAt(lastIndex).Id, new PlantPatchId(42), 5));
            PlantPatchId survivingId = patches.GetAt(lastIndex).Id;

            patches.RemoveAt(1);

            Assert.That(patches.Count, Is.EqualTo(2));
            PlantPatchState moved = patches.GetAt(1);
            Assert.That(moved.Id, Is.EqualTo(survivingId));
            Assert.That(moved.FoodResourceId, Is.EqualTo(new ResourceId(3)));
            Assert.That(moved.Position.X, Is.EqualTo(3f));
            Assert.That(moved.Position.Y, Is.EqualTo(4f));
            Assert.That(moved.Biomass, Is.EqualTo(7f));
            Assert.That(moved.Capacity, Is.EqualTo(30f));
            Assert.That(moved.GrowthRate, Is.EqualTo(.3f));
            Assert.That(moved.Nutrition, Is.EqualTo(.8f));
            Assert.That(moved.Defense, Is.EqualTo(.2f));
            Assert.That(moved.Genome.Growth, Is.EqualTo(.9f));
            Assert.That(moved.Lineage.Generation, Is.EqualTo(5));
            Assert.That(patches.FindIndex(new ResourceId(1)), Is.EqualTo(0));
            Assert.That(patches.FindIndex(new ResourceId(3)), Is.EqualTo(1));
            Assert.That(patches.FindIndex(new ResourceId(2)), Is.EqualTo(-1));
        }

        [Test]
        public void AdvanceAgeAccumulatesElapsedTimeOnAPatch()
        {
            var patches = new PlantPatchStore(1);
            int index = patches.Add(new ResourceId(1), new SimVector2(0f, 0f), 1f, 10f, .1f, 1f, 0f);

            Assert.That(patches.GetAt(index).Age, Is.EqualTo(0f));
            patches.AdvanceAge(index, 1.5f);
            patches.AdvanceAge(index, 2.5f);

            Assert.That(patches.GetAt(index).Age, Is.EqualTo(4f).Within(.0001f));
        }
```

- [ ] **Step 2: Run to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "RemoveAtSwapsTheLastPatchIntoTheVacatedSlotWithEveryFieldIntact|AdvanceAgeAccumulatesElapsedTimeOnAPatch"`
Expected: FAIL — `PlantPatchStore` has no `RemoveAt` / `AdvanceAge` (compile error).

- [ ] **Step 3: Implement `AdvanceAge` and `RemoveAt`**

In `Assets/Scripts/Simulation/Environment/PlantPatchStore.cs`, add immediately after the existing `SetBiomass` method:

```csharp
        public void AdvanceAge(int index, float deltaTime)
        {
            if ((uint)index >= (uint)Count || deltaTime <= 0f) return;
            _ages[index] += deltaTime;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)Count) return;
            int last = Count - 1;
            if (index != last)
            {
                _ids[index] = _ids[last];
                _foodResourceIds[index] = _foodResourceIds[last];
                _positions[index] = _positions[last];
                _biomass[index] = _biomass[last];
                _capacities[index] = _capacities[last];
                _growthRates[index] = _growthRates[last];
                _nutrition[index] = _nutrition[last];
                _defense[index] = _defense[last];
                _genomes[index] = _genomes[last];
                _lineages[index] = _lineages[last];
                _ages[index] = _ages[last];
                _seedReserves[index] = _seedReserves[last];
                _reproductionCooldowns[index] = _reproductionCooldowns[last];
            }

            _biomass[last] = 0f;
            _ages[last] = 0f;
            _seedReserves[last] = 0f;
            _reproductionCooldowns[last] = 0f;
            Count--;
        }
```

- [ ] **Step 4: Run to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "RemoveAtSwapsTheLastPatchIntoTheVacatedSlotWithEveryFieldIntact|AdvanceAgeAccumulatesElapsedTimeOnAPatch"`
Expected: PASS (2/2)

- [ ] **Step 5: Write failing test for `PlantPhenotype.LifespanSeconds`**

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs`:

```csharp
        [Test]
        public void SlowestGrowerLivesExactlyTwiceAsLongAsFastestGrower()
        {
            var slow = new PlantGenome(0f, .5f, .5f, .5f, .5f, .5f, .5f, .5f);
            var fast = new PlantGenome(1f, .5f, .5f, .5f, .5f, .5f, .5f, .5f);

            float slowLifespan = PlantPhenotype.FromGenome(slow).LifespanSeconds;
            float fastLifespan = PlantPhenotype.FromGenome(fast).LifespanSeconds;

            Assert.That(slowLifespan, Is.EqualTo(fastLifespan * 2f).Within(.0001f));
            Assert.That(slowLifespan, Is.EqualTo(PlantPhenotype.BaseLifespanSeconds * 1.5f).Within(.0001f));
            Assert.That(fastLifespan, Is.EqualTo(PlantPhenotype.BaseLifespanSeconds * .75f).Within(.0001f));
        }
```

- [ ] **Step 6: Run to verify it fails**

Run: `cd tools/HeadlessTests && dotnet test --filter "SlowestGrowerLivesExactlyTwiceAsLongAsFastestGrower"`
Expected: FAIL — `PlantPhenotype` has no `LifespanSeconds` / `BaseLifespanSeconds` (compile error).

- [ ] **Step 7: Add `LifespanSeconds` to `PlantPhenotype`**

In `Assets/Scripts/Simulation/Environment/PlantGenome.cs`, change the `PlantPhenotype` struct — add the constant, a 6th constructor parameter, the assignment, the property, and the `FromGenome` computation:

```csharp
    public readonly struct PlantPhenotype
    {
        /// <summary>
        /// Reference lifespan in seconds before the Growth-gene tradeoff is applied.
        /// Placeholder value; Task 2 replaces it with an empirically derived one.
        /// </summary>
        public const float BaseLifespanSeconds = 90f;

        public PlantPhenotype(float growthRateMultiplier, float nutritionMultiplier, float defense, float dispersalRange, float seedInvestmentFraction, float lifespanSeconds)
        {
            GrowthRateMultiplier = growthRateMultiplier;
            NutritionMultiplier = nutritionMultiplier;
            Defense = defense;
            DispersalRange = dispersalRange;
            SeedInvestmentFraction = seedInvestmentFraction;
            LifespanSeconds = lifespanSeconds;
        }

        public float GrowthRateMultiplier { get; }
        public float NutritionMultiplier { get; }
        public float Defense { get; }
        public float DispersalRange { get; }
        public float SeedInvestmentFraction { get; }
        public float LifespanSeconds { get; }

        public static PlantPhenotype FromGenome(PlantGenome genome)
        {
            float growth = .55f + (.90f * genome.Growth) - (.18f * genome.Nutrition) - (.15f * genome.Defense) - (.08f * genome.WaterEfficiency) - (.10f * genome.MoistureTolerance) - (.10f * genome.TemperatureTolerance);
            return new PlantPhenotype(
                Math.Max(.1f, growth),
                .55f + (.90f * genome.Nutrition) - (.25f * genome.Defense),
                genome.Defense,
                4f + (20f * genome.Dispersal),
                .02f + (.10f * genome.SeedInvestment),
                BaseLifespanSeconds * (1.5f - (.75f * genome.Growth)));
        }
    }
```

- [ ] **Step 8: Run to verify it passes**

Run: `cd tools/HeadlessTests && dotnet test --filter "SlowestGrowerLivesExactlyTwiceAsLongAsFastestGrower"`
Expected: PASS

- [ ] **Step 9: Write failing tests for `PlantMortalitySystem`**

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs`:

```csharp
        [Test]
        public void PatchIsRemovedOnTheStepItsAgeReachesItsLifespanAndNotBefore()
        {
            var resources = new ResourceStore(1);
            ResourceId site = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 10f, 0f);
            var patches = new PlantPatchStore(1);
            int index = patches.Add(site, new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);
            float lifespan = PlantPhenotype.FromGenome(patches.GetAt(index).Genome).LifespanSeconds;

            float elapsed = 0f;
            while (elapsed + 1f < lifespan)
            {
                PlantMortalitySystem.Step(patches, resources, 1f);
                elapsed += 1f;
                Assert.That(patches.Count, Is.EqualTo(1), $"patch died early at age {elapsed}");
            }

            float removedBiomass = PlantMortalitySystem.Step(patches, resources, 1f);

            Assert.That(patches.Count, Is.EqualTo(0));
            Assert.That(removedBiomass, Is.EqualTo(5f).Within(.0001f));
        }

        [Test]
        public void DyingPatchClearsItsFoodProjectionAndFreesItsSite()
        {
            var resources = new ResourceStore(1);
            ResourceId site = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 10f, 0f);
            var patches = new PlantPatchStore(1);
            patches.Add(site, new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);

            PlantMortalitySystem.Step(patches, resources, 10000f);

            Assert.That(patches.Count, Is.EqualTo(0));
            Assert.That(resources.GetAt(0).IsActive, Is.False);
            Assert.That(resources.GetAt(0).Amount, Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void FastGrowingPatchDiesBeforeSlowGrowingPatchCreatedAtTheSameTime()
        {
            var resources = new ResourceStore(2);
            ResourceId fastSite = resources.Add(ResourceKind.Food, new SimVector2(0f, 0f), 1f, 5f, 10f, 0f);
            ResourceId slowSite = resources.Add(ResourceKind.Food, new SimVector2(5f, 0f), 1f, 5f, 10f, 0f);
            var patches = new PlantPatchStore(2);
            int fast = patches.Add(fastSite, new SimVector2(0f, 0f), 5f, 10f, .1f, 1f, 0f);
            patches.SetGenomeAndLineage(fast, new PlantGenome(1f, .5f, .5f, .5f, .5f, .5f, .5f, .5f), patches.GetAt(fast).Lineage);
            int slow = patches.Add(slowSite, new SimVector2(5f, 0f), 5f, 10f, .1f, 1f, 0f);
            patches.SetGenomeAndLineage(slow, new PlantGenome(0f, .5f, .5f, .5f, .5f, .5f, .5f, .5f), patches.GetAt(slow).Lineage);

            int stepsUntilFirstDeath = 0;
            while (patches.Count == 2 && stepsUntilFirstDeath < 10000)
            {
                PlantMortalitySystem.Step(patches, resources, 1f);
                stepsUntilFirstDeath++;
            }

            Assert.That(patches.Count, Is.EqualTo(1));
            Assert.That(patches.GetAt(0).Genome.Growth, Is.EqualTo(0f), "the surviving patch should be the slow grower");
        }
```

- [ ] **Step 10: Run to verify they fail**

Run: `cd tools/HeadlessTests && dotnet test --filter "PatchIsRemovedOnTheStepItsAgeReachesItsLifespanAndNotBefore|DyingPatchClearsItsFoodProjectionAndFreesItsSite|FastGrowingPatchDiesBeforeSlowGrowingPatchCreatedAtTheSameTime"`
Expected: FAIL — `PlantMortalitySystem` does not exist (compile error).

- [ ] **Step 11: Create `PlantMortalitySystem`**

Create `Assets/Scripts/Simulation/Environment/PlantMortalitySystem.cs`:

```csharp
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Environment
{
    public static class PlantMortalitySystem
    {
        /// <summary>
        /// Ages every patch and removes those past their lifespan, releasing each dead patch's
        /// resource site back to the dispersal pool. Returns the total biomass removed, which the
        /// caller accumulates so the plant biomass conservation residual stays balanced.
        /// </summary>
        public static float Step(PlantPatchStore patches, ResourceStore resources, float deltaTime)
        {
            float removedBiomass = 0f;

            // Iterate backward: RemoveAt swaps the last element into the vacated slot, so a
            // forward loop would skip whatever got moved down into the current index.
            for (int index = patches.Count - 1; index >= 0; index--)
            {
                patches.AdvanceAge(index, deltaTime);
                PlantPatchState patch = patches.GetAt(index);
                PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
                if (patch.Age < phenotype.LifespanSeconds) continue;

                removedBiomass += patch.Biomass;
                resources.SetFoodProjection(patch.FoodResourceId, 0f, 1f, 0f);
                resources.SetActive(patch.FoodResourceId, false);
                patches.RemoveAt(index);
            }

            return removedBiomass;
        }
    }
}
```

- [ ] **Step 12: Run to verify they pass**

Run: `cd tools/HeadlessTests && dotnet test --filter "PatchIsRemovedOnTheStepItsAgeReachesItsLifespanAndNotBefore|DyingPatchClearsItsFoodProjectionAndFreesItsSite|FastGrowingPatchDiesBeforeSlowGrowingPatchCreatedAtTheSameTime"`
Expected: PASS (3/3)

- [ ] **Step 13: Add the `PlantMortalityEnabled` flag**

In `Assets/Scripts/Simulation/Core/SimulationConfig.cs`, change the constructor's last parameter line from `bool plantSiteCompetitionEnabled = false)` to:

```csharp
            bool plantSiteCompetitionEnabled = false,
            bool plantMortalityEnabled = false)
```

In the constructor body, immediately after `PlantSiteCompetitionEnabled = plantSiteCompetitionEnabled;` (line 148):

```csharp
            PlantSiteCompetitionEnabled = plantSiteCompetitionEnabled;
            PlantMortalityEnabled = plantMortalityEnabled;
```

Immediately after the `PlantSiteCompetitionEnabled` property (line 179):

```csharp
        public bool PlantSiteCompetitionEnabled { get; }
        public bool PlantMortalityEnabled { get; }
```

- [ ] **Step 14: Add the statistics counter**

In `Assets/Scripts/Simulation/Core/SimulationTypes.cs`, add a new trailing constructor parameter after `float meanPlantDefenseGene = 0f`:

```csharp
            float meanPlantDefenseGene = 0f,
            float cumulativePlantBiomassLostToMortality = 0f)
```

Add its assignment alongside the other plant assignments:

```csharp
            CumulativePlantBiomassLostToMortality = cumulativePlantBiomassLostToMortality;
```

Add the property after `MeanPlantDefenseGene`:

```csharp
        public float CumulativePlantBiomassLostToMortality { get; }
```

- [ ] **Step 15: Wire mortality into the simulation tick**

In `Assets/Scripts/Simulation/Core/SimulationWorld.cs`, add a field beside the other cumulative plant counters (near line 45):

```csharp
        private float _cumulativePlantBiomassLostToMortality;
```

In the plant tick block, add the mortality step immediately after the reproduction step and before `ProjectFoodResources` — mortality must run before projection so a dead patch's site is already cleared when projections are rewritten:

```csharp
                    _plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, resourceDeltaTime, ref _plantSeedOrdinal, Config.PlantSiteCompetitionEnabled);
                    if (Config.PlantMortalityEnabled)
                    {
                        _cumulativePlantBiomassLostToMortality += PlantMortalitySystem.Step(Plants, Resources, resourceDeltaTime);
                    }

                    PlantGrowthSystem.ProjectFoodResources(Plants, Resources);
```

- [ ] **Step 16: Correct the conservation residual**

Still in `SimulationWorld.cs`, the statistics construction currently computes the plant biomass residual as:

```csharp
                plantBiomass - (_initialPlantBiomass + _cumulativePlantGrowth - _cumulativePlantBiomassConsumed),
```

Biomass removed by mortality leaves the system too, so it must be subtracted as well or the residual goes negative by exactly the mortality total. Change it to:

```csharp
                plantBiomass - (_initialPlantBiomass + _cumulativePlantGrowth - _cumulativePlantBiomassConsumed - _cumulativePlantBiomassLostToMortality),
```

Then pass the counter as the new trailing statistics argument, after the `meanPlantDefenseGene` argument:

```csharp
                Plants.Count == 0 ? 0f : plantDefenseTotal / Plants.Count,
                _cumulativePlantBiomassLostToMortality);
```

- [ ] **Step 17: Write the biomass-conservation test**

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs`:

```csharp
        [Test]
        public void BiomassRemovedByMortalityIsReportedSoTheResidualStaysBalanced()
        {
            SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(42, 4);
            var config = new SimulationConfig(
                42, 4, defaults.Schedule, defaults.MaximumPopulation, defaults.FounderProfile,
                cognitionEnabled: true, physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true, plantMortalityEnabled: true);
            var world = new SimulationWorld(config);
            Prototype4Scenarios.PlantBackedBaseline.ApplyTo(world);

            for (int tick = 0; tick < 6000; tick++)
            {
                world.Step(config.FixedDeltaTime);
            }

            SimulationStatistics stats = world.Statistics;
            Assert.That(stats.CumulativePlantBiomassLostToMortality, Is.GreaterThan(0f), "no patch died, so this test proves nothing");
            Assert.That(stats.PlantBiomassResidual, Is.EqualTo(0f).Within(.0001f));
        }
```

- [ ] **Step 18: Run the full suite**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all tests pass. The flag defaults to `false`, so every pre-existing test is unaffected — no hash baseline should move. **If any hash baseline changes, STOP and report BLOCKED**: it means mortality is running on a flag-off path, which violates the Global Constraints.

- [ ] **Step 19: Add the flag-off hash regression test**

First record the pre-task commit: `git log --oneline -1 main`. Derive the baseline fresh from a throwaway worktree at that commit rather than assuming, per project convention — though it should match the current `13626802794646021369UL`.

Add to `Assets/Tests/EditMode/PlantGrowthTests.cs`:

```csharp
        [Test]
        public void PlantMortalityFlagOffLeavesTheStandardHashScenarioUnchanged()
        {
            var config = new SimulationConfig(
                99,
                2,
                new SimulationSchedule(60, 60, 30, 10, 10, 10, 5, 1),
                founderProfile: FounderProfile.PredationVariation);
            var world = new SimulationWorld(config);

            for (int i = 0; i < 50; i++)
            {
                world.Step(config.FixedDeltaTime);
            }

            Assert.That(world.ComputeStateHash(), Is.EqualTo(13626802794646021369UL));
        }
```

If the derived value differs from `13626802794646021369UL`, use the derived value and note the discrepancy in the report — do not adjust the code to fit the constant.

- [ ] **Step 20: Run the full suite and commit**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all pass.

```bash
git add Assets/Scripts/Simulation/Environment/ Assets/Scripts/Simulation/Core/ Assets/Tests/EditMode/PlantGrowthTests.cs
git commit -m "feat: add age-based plant mortality behind a flag"
```

---

## Task 2: Calibrate lifespan and guard the regression

**Files:**
- Modify: `Assets/Scripts/Simulation/Environment/PlantGenome.cs` (`BaseLifespanSeconds` value + derivation comment)
- Test: `Assets/Tests/EditMode/ResourceExperimentTests.cs`

**Interfaces:**
- Consumes: everything Task 1 produced — `PlantMortalitySystem.Step`, `PlantPhenotype.BaseLifespanSeconds`, `SimulationConfig.PlantMortalityEnabled`.
- Produces: an empirically derived `BaseLifespanSeconds`; a committed integration test guarding against the generation-2 freeze.

- [ ] **Step 1: Write the throwaway calibration probe**

Create `Assets/Tests/EditMode/ZZZLifespanCalibration.cs` (deleted in Step 4 — it is a search tool, not a committed test):

```csharp
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class ZZZLifespanCalibration
    {
        [Test]
        public void ReportTurnoverAndSurvivalAcrossSeeds()
        {
            int minPlantGen = int.MaxValue;
            int extinctAnimals = 0;
            int extinctPlants = 0;

            for (int seed = 42; seed <= 71; seed++)
            {
                SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(seed, 12);
                var config = new SimulationConfig(
                    seed, 12, defaults.Schedule, maximumPopulation: 48, defaults.FounderProfile,
                    cognitionEnabled: true, physiologyEnabled: true,
                    decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                    plantCohortsEnabled: true, plantSiteCompetitionEnabled: true,
                    plantMortalityEnabled: true);

                ExperimentResult r = ExperimentRunner.Run(config, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, ticks: 12000);
                if (r.FinalStatistics.HighestPlantGeneration < minPlantGen) minPlantGen = r.FinalStatistics.HighestPlantGeneration;
                if (r.FinalStatistics.Population <= 0) extinctAnimals++;
                if (r.FinalStatistics.ActivePlantPatchCount <= 0) extinctPlants++;
            }

            TestContext.WriteLine($"BaseLifespanSeconds={PlantPhenotype.BaseLifespanSeconds}: minPlantGen={minPlantGen} animalExtinctions={extinctAnimals} plantExtinctions={extinctPlants}");
        }
    }
}
```

If `ActivePlantPatchCount` is not the correct `SimulationStatistics` property name for the live patch count, read `Assets/Scripts/Simulation/Core/SimulationTypes.cs` and use the real one — do not guess.

- [ ] **Step 2: Run the doubling search**

Run the probe, read the printed line, then edit `PlantPhenotype.BaseLifespanSeconds` and re-run. Both constraints must hold simultaneously:

1. `minPlantGen >= 8`
2. `animalExtinctions == 0` and `plantExtinctions == 0`

Search procedure: start at the Task 1 placeholder `90f`. If `minPlantGen < 8`, plants live too long to turn over — **halve** the value. If there are extinctions, plants die too fast — **double** it. Continue until both hold, then record the smallest satisfying value.

Run: `cd tools/HeadlessTests && dotnet test --filter "ReportTurnoverAndSurvivalAcrossSeeds" --logger "console;verbosity=detailed"`

If no value satisfies both constraints after 6 iterations, STOP and report BLOCKED with the full table of values tried and their results — that would mean the tradeoff needs redesign, not more search.

- [ ] **Step 3: Commit the derived constant with its derivation recorded**

Update the constant in `Assets/Scripts/Simulation/Environment/PlantGenome.cs`, replacing the placeholder comment:

```csharp
        /// <summary>
        /// Reference lifespan in seconds before the Growth-gene tradeoff is applied.
        /// Derived empirically (docs/superpowers/plans/2026-08-17-plant-mortality.md, Task 2):
        /// smallest value where all 30 seeds (42-71) of ConsumerDefenseCalibrationModerate reach
        /// at least 8 plant generations in 12,000 ticks with no animal or plant extinction.
        /// </summary>
        public const float BaseLifespanSeconds = <derived value>;
```

Replace `<derived value>` with the number found in Step 2, and update the `SlowestGrowerLivesExactlyTwiceAsLongAsFastestGrower` test if it hardcoded the placeholder anywhere (it references `BaseLifespanSeconds` symbolically, so it should not need changing — verify).

- [ ] **Step 4: Delete the throwaway probe**

```bash
rm Assets/Tests/EditMode/ZZZLifespanCalibration.cs
```

- [ ] **Step 5: Write the committed regression guard**

This is the test that would have caught the 2026-08-17 finding. Add to `Assets/Tests/EditMode/ResourceExperimentTests.cs`:

```csharp
        [Test]
        public void PlantMortalityProducesGenerationalTurnoverBeyondTheFrozenGenerationTwo()
        {
            int[] seeds = { 42, 43, 44 };
            foreach (int seed in seeds)
            {
                SimulationConfig defaults = SimulationConfig.CreatePrototype4Defaults(seed, 12);
                var config = new SimulationConfig(
                    seed, 12, defaults.Schedule, maximumPopulation: 48, defaults.FounderProfile,
                    cognitionEnabled: true, physiologyEnabled: true,
                    decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                    plantCohortsEnabled: true, plantSiteCompetitionEnabled: true,
                    plantMortalityEnabled: true);

                ExperimentResult result = ExperimentRunner.Run(config, Prototype4Scenarios.ConsumerDefenseCalibrationModerate, ticks: 12000);

                Assert.That(result.FinalStatistics.HighestPlantGeneration, Is.GreaterThan(2),
                    $"Seed {seed} froze at generation {result.FinalStatistics.HighestPlantGeneration} - plants stopped reproducing.");
                Assert.That(result.FinalStatistics.Population, Is.GreaterThan(0), $"Seed {seed} animals went extinct.");
            }
        }
```

- [ ] **Step 6: Run the full suite and commit**

Run: `cd tools/HeadlessTests && dotnet test`
Expected: all pass.

```bash
git add Assets/Scripts/Simulation/Environment/PlantGenome.cs Assets/Tests/EditMode/ResourceExperimentTests.cs
git commit -m "feat: calibrate plant lifespan and guard against the generation-2 freeze"
```

---

## Self-Review Notes

- **Spec coverage:** aging (T1 S3) ✅; gene-linked lifespan with exact 2x spread (T1 S7) ✅; deterministic death rule, no RNG (T1 S11) ✅; backward iteration (T1 S11) ✅; swap-remove (T1 S3) ✅; site release in the specified order (T1 S11) ✅; biomass accounting *including the residual correction* (T1 S16) ✅; flag (T1 S13) ✅; sprout-floor non-interaction — holds by construction, no code needed ✅; empirical calibration with both constraints (T2 S2) ✅; all 10 spec test items covered across T1 S1/S5/S9/S17/S19 and T2 S5 ✅.
- **Placeholder scan:** the one `<derived value>` is an explicit output of T2 S2, not an unfilled blank. `BaseLifespanSeconds = 90f` in T1 is a deliberately labelled placeholder that T2 replaces.
- **Type consistency:** `RemoveAt(int)`, `AdvanceAge(int, float)`, `PlantMortalitySystem.Step(PlantPatchStore, ResourceStore, float) : float`, and the 6-parameter `PlantPhenotype` constructor are used identically everywhere they appear across both tasks.
- **Task split rationale:** a reviewer could approve the mechanism while rejecting the calibration (or vice versa), and each task ends with an independently testable deliverable — so the boundary is real, not cosmetic.
