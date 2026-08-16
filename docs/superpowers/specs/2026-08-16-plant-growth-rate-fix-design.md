# Plant Growth-Rate Conversion Fix - Design

## Problem

Confirmed via systematic debugging this session (bug found while re-verifying
P4's "reliable multi-generation baseline" requirement, ahead of any
world-generation/terrain work): every `PlantCohortsEnabled` scenario under
real consumer grazing pressure ends in total extinction.

Root cause, fully verified:

1. **Growth-rate unit mismatch.** `SimulationScenario.cs` converts a
   `ResourceDefinition.RegenerationPerSecond` config value into a plant
   patch's logistic growth-rate constant via `growthRate =
   definition.RegenerationPerSecond / capacity`. Everywhere else in the
   codebase (`ResourceStore.Regenerate`), the same field means "absolute
   units added per second, flat, regardless of current amount." For a
   logistic curve `growth = r * B * (1 - B/K)`, peak throughput occurs at
   `B = K/2` and equals `r * K / 4` - so dividing by capacity alone (instead
   of `4 * RegenerationPerSecond / capacity`) makes peak regrowth 4x slower
   than the number implies under the codebase's existing "regen per second"
   convention.

2. **Zero biomass is a permanent trap.** `PlantGrowthSystem.Step` skips a
   patch entirely once `patch.Biomass <= 0f`, and growth is proportional to
   current biomass (`growth ∝ Biomass`), so a fully-grazed patch can never
   recover on its own - only `PlantReproductionSystem`'s seed dispersal from
   a still-living sibling patch can reseed it. Confirmed reachable:
   `Plants.ConsumeAt` (`SimulationWorld.cs`) really depletes biomass from
   animal grazing down to exactly zero.

3. **The calibration scenario has no recovery path at all.**
   `Prototype4Scenarios.ConsumerDefenseCalibrationControl`/`Moderate`
   register exactly 2 active food patches and zero dispersal-target sites
   (unlike `PlantBackedBaseline`, which registers 8 inactive sites for
   exactly this purpose) - so once both patches hit zero, there is no
   possible recovery.

Empirically confirmed via a throwaway experiment (not committed): even
scaling the *current, buggy* growth rate by 16x still produced total
extinction in 5/5 seeds of the calibration scenario (12 founders, 48 pop
cap, `IntentUtilityV1` + cognition + physiology + `PlantCohortsEnabled`,
12,000 ticks); 32x produced 0/5 extinctions. That 32x number is not a
principled constant - it reflects this specific scenario's demand (12-48
creatures grazing 2 patches), not a universal conversion factor. It
confirms cause 1 is *sufficient on its own* to explain and fix the
extinction, but the actual fix must be a corrected formula plus a properly
retuned scenario, not a hardcoded scalar.

This blocks P4's stated exit-gate requirement ("repeatable reciprocal
plant/consumer trait response") and the prerequisite "reliable baseline
animal survival/reproduction loop" that
`docs/experiments/p4-consumer-defense-calibration-2026-08-13.md` called for
before any further P4 coevolution work can proceed. P4's biology (genetics,
defense, dispersal mechanics, mutation) is otherwise substantially built and
tested - this is the single blocking issue.

## Fix

### 1. Dimensionally-correct the growth-rate conversion

In `SimulationScenario.cs`, `ApplyTo`'s plant-patch creation:

```csharp
float growthRate = capacity <= 0f ? 0f : (4f * definition.RegenerationPerSecond) / capacity;
```

(was `definition.RegenerationPerSecond / capacity`). This makes peak
logistic throughput at `B = K/2` equal `RegenerationPerSecond`, matching the
flat-regen path's meaning for the same field. This is universal - every
`PlantCohortsEnabled` scenario benefits, not just the calibration ones.

### 2. Add a small self-recovery floor to the growth formula

In `PlantGrowthSystem.cs`, `Step`:

```csharp
private const float SproutFloorFraction = 0.01f;

public static float Step(PlantPatchStore patches, EnvironmentField field, float deltaTime)
{
    float addedBiomass = 0f;
    for (int index = 0; index < patches.Count; index++)
    {
        PlantPatchState patch = patches.GetAt(index);
        if (patch.Biomass >= patch.Capacity) continue;
        EnvironmentSample sample = field.Sample(patch.Position);
        PlantPhenotype phenotype = PlantPhenotype.FromGenome(patch.Genome);
        float moistureAdaptation = sample.Moisture <= 0f
            ? 0f
            : Math.Min(1f, sample.Moisture + ((1f - sample.Moisture) * (.7f * patch.Genome.WaterEfficiency + .3f * patch.Genome.MoistureTolerance)));
        float limit = Math.Max(0f, Math.Min(moistureAdaptation, Math.Min(sample.Fertility, sample.Temperature)));
        float sproutBiomass = patch.Biomass + (SproutFloorFraction * patch.Capacity);
        float growth = patch.GrowthRate * phenotype.GrowthRateMultiplier * sproutBiomass * (1f - (patch.Biomass / patch.Capacity)) * limit * deltaTime;
        float next = Math.Min(patch.Capacity, patch.Biomass + growth);
        patches.SetBiomass(index, next);
        addedBiomass += next - patch.Biomass;
    }

    return addedBiomass;
}
```

Two changes from today: the early-out guard drops `patch.Biomass <= 0f`
(keeping only the "already full" guard), and the logistic term uses
`patch.Biomass + (SproutFloorFraction * patch.Capacity)` instead of bare
`patch.Biomass`. `SproutFloorFraction = 0.01` (1% of capacity) is small
enough that it does not meaningfully change growth dynamics away from zero,
but guarantees a nonzero, capped, self-recovery path from full depletion -
without it, a patch at exactly zero biomass produces `growth = r * 0 * (1 -
0) * limit * dt = 0` forever. Dispersal (Fix 3, and the existing mechanic
for every other plant scenario) remains a faster/better recovery path where
it exists; this floor is a safety net, not a replacement.

This is a general fix - every `PlantCohortsEnabled` scenario in the
codebase becomes structurally recoverable from full depletion, not only the
calibration one.

### 3. Retune the calibration scenario and add dispersal targets

`Prototype4Scenarios.ConsumerDefenseCalibrationControl`/`Moderate`
(`SimulationScenario.cs`) get:

- Additional inactive `ResourceDefinition` entries registered as dispersal
  targets, mirroring `CreatePlantSites`' pattern (a handful of `isActive:
  false`, zero-amount `Food` definitions at new positions, so
  `PlantReproductionSystem` has somewhere to disperse seeds to beyond the 2
  active patches).
- A `RegenerationPerSecond` value re-derived empirically under the corrected
  formula (Fix 1) and floor (Fix 2) - not guessed. The implementation task
  runs the scenario across seeds 42-46 for 12,000 ticks at increasing
  `RegenerationPerSecond` candidates (starting from the current value,
  doubling each iteration) until all 5 seeds survive to a nonzero final
  population, and commits that value with a comment recording the
  derivation (same "derive, don't assume" methodology this project already
  uses for hash-regression baselines).

This is scenario-specific tuning, not a formula change - `DefendedPlants`,
`UndefendedPlants`, `PlantBackedBaseline`, and `WatchableStarterHabitat` are
untouched by this fix beyond automatically benefiting from Fixes 1 and 2.

## Scope boundary

- No change to the logistic growth *shape* itself (still `r * B * (1 -
  B/K)`, just with a floor term and a corrected `r` derivation) - a
  different growth curve family was considered and rejected as overkill for
  this defect (see design discussion).
- No change to `EnvironmentField`'s hardcoded fertility/temperature (`=
  1f` always) - that's B-7, explicitly deferred to the separate
  world-generation spec per the project roadmap. Not touched here.
- No change to `PlantReproductionSystem`'s dispersal mechanics themselves,
  only to which scenario registers dispersal targets.
- `DefendedPlants`/`UndefendedPlants`/`PlantBackedBaseline`/
  `WatchableStarterHabitat` scenario values are not retuned - only the
  calibration scenarios, which is what's currently blocking the P4
  baseline requirement.

## Hash safety

`PlantCohortsEnabled` already defaults to `false` in `SimulationConfig`, and
every one of this session's established hash-regression baselines (the
`PredationVariation`/`Legacy` scenario) never sets it to `true` - so the
existing `12050501592762519865UL` baseline is trivially unaffected and a
regression test confirming this should still be added, matching this
project's established paranoia. Runs where `PlantCohortsEnabled: true`
*will* produce different results than before - that is the intended fix,
not a regression to guard against.

## Testing

1. `PlantGrowthTests.LogisticGrowthIsLimitedByTheEnvironmentAndCapacity`
   (existing test) - its expected value changes from `1.6f *
   GrowthRateMultiplier` to `1.68f * GrowthRateMultiplier`, per the new
   floor term (`growthRate=1f, biomass=2f, capacity=10f`: `sproutBiomass =
   2 + (0.01*10) = 2.1`; `growth = 1 * mult * 2.1 * (1 - 2/10) * 1 * 1 =
   1.68 * mult`) - a precise, computed update, not a placeholder.
2. New unit test: a patch at exactly zero biomass, under
   `PlantGrowthSystem.Step`, produces nonzero growth (proves the floor
   fixes the permanent-trap defect).
3. New unit test: the floor's contribution is small relative to normal
   growth at moderate biomass levels (proves it's a safety net, not a
   dominant term) - e.g. compare growth at `Biomass = Capacity/2` with and
   without the floor and assert the relative difference is small.
4. New unit test on `SimulationScenario.ApplyTo`: for a
   `PlantCohortsEnabled` world, the resulting patch's `GrowthRate` equals
   `4 * RegenerationPerSecond / (Capacity * populationScale)` (proves the
   conversion formula).
5. Integration test: `ConsumerDefenseCalibrationControl`, run across seeds
   42-46 for 12,000 ticks with the corrected formula, retuned
   `RegenerationPerSecond`, and added dispersal targets, produces a nonzero
   final population in every seed (the actual regression guard for this
   defect - fails if the calibration ever regresses back to guaranteed
   extinction).
6. Hash-regression test: standard `PredationVariation`/`Legacy` scenario
   (never sets `PlantCohortsEnabled`) still produces
   `12050501592762519865UL`.
7. Full existing suite stays green (`cd tools/HeadlessTests && dotnet
   test`).
