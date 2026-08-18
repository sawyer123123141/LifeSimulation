# Plant Mortality — Design

## Problem

Established by experiment on 2026-08-17
(`docs/experiments/p4-coevolution-null-2026-08-17.md`): plants reach
generation 2 and stop reproducing permanently. Quadrupling run length to
48,000 ticks adds zero additional plant generations, while animals turn over
normally (9 to 27 generations).

Root cause, verified:

- The calibration scenario has a **fixed pool of 8 sites**, all colonized by
  generation 2.
- Regrowth outpaces grazing, so occupants sit at 75-100% of capacity
  (measured directly at every sampled tick from 3,000 to 24,000).
- Dispersal (`PlantReproductionSystem.FindSite`) requires an **inactive**
  site — none remain. Site competition requires a resident below **25%**
  biomass — none qualify.
- `PlantPatchStore` has **no removal method**, and since the 2026-08-16
  sprout-floor fix even a fully-grazed patch regrows from a 1%-of-capacity
  floor. A site, once occupied, is never released.

The plant layer is therefore not an evolving population — it is 8 permanent
patches whose genomes froze at generation 2. Plant defense cannot be selected
on because plants neither die nor are born, so P4's exit gate (repeatable
reciprocal plant/consumer trait response) is structurally unreachable.

**Plant mortality is the missing mechanic.** Note specifically that a
health-based death rule would *not* solve this — patches are healthy, which is
exactly why nothing currently fires. Only a rule that can kill a thriving
plant produces turnover here.

## Design

### Aging

A new `PlantMortalitySystem` accrues `_ages[index] += deltaTime` for every
patch on each plant tick.

`PlantPatchStore._ages` and `._seedReserves` are currently allocated, resized,
and read into `PlantPatchState` but **never written anywhere** — the third
halfway-wired field found on 2026-08-17, after `Persistence` (consumed but not
inherited) and `Commitment` (inherited but not consumed). This design makes
`Age` live. `SeedReserve` remains unwired and is explicitly out of scope.

### Lifespan

New derived property on `PlantPhenotype`:

```csharp
LifespanSeconds = BaseLifespanSeconds * (1.5f - (0.75f * genome.Growth));
```

The slowest grower (`Growth = 0`) lives twice as long as the fastest
(`Growth = 1`). This makes `Growth` a genuine strategic choice — fast biomass
accumulation is paid for in longevity — giving selection a real tradeoff to
act on rather than a free parameter.

`BaseLifespanSeconds` is **derived empirically, not guessed**, following the
project's established derive-don't-assume convention. The implementation task
runs a doubling search over candidate values against
`ConsumerDefenseCalibrationModerate`, seeds 42-71, 12,000 ticks, and selects
the smallest value satisfying both calibration constraints below.

### Calibration constraints

1. **Plant turnover:** `HighestPlantGeneration` at least 8 in a 12,000-tick
   run (comparable to the 9-12 animal generations observed over the same
   span).
2. **No extinction:** final animal population above 0 and final plant count
   above 0 in every seed 42-71.

Constraint 2 is not optional bookkeeping. Mortality removes food-bearing
patches, and a 48,000-tick run already showed an animal population crash to 3
in one seed before mortality existed. The derived lifespan must not compound
that.

### Death and removal

A patch dies when `Age >= phenotype.LifespanSeconds`. The rule is fully
deterministic — no RNG draw, so no new random domain and no desync risk.

New `PlantPatchStore.RemoveAt(int index)` performs a swap-remove: copy the
last live element into `index` across every parallel array, then decrement
`Count`.

Swap-remove is safe here, verified rather than assumed: no plant index is
retained across ticks anywhere in the codebase. Every access is either
loop-local (`PlantGrowthSystem.Step`, `PlantReproductionSystem.Step`,
`ProjectFoodResources`, `ComputeStateHash`, statistics aggregation) or a fresh
`Plants.FindIndex(resource.Id)` lookup by ID (`SimulationWorld.cs:1166`).

`PlantMortalitySystem` iterates **backward** (`index = Count - 1` down to `0`)
so a swap-removal cannot cause the loop to skip the element moved into the
vacated slot.

### Site release

Before removing a patch, its bound resource site is released so dispersal can
recolonize it:

```csharp
resources.SetFoodProjection(patch.FoodResourceId, 0f, 1f, 0f);
resources.SetActive(patch.FoodResourceId, false);
```

Clearing the projection first prevents a dead patch from leaving phantom food
behind at a site that no longer has an occupant. Deactivating returns the site
to the pool `FindSite` draws from — the entire purpose of this mechanic.

### Biomass accounting

A dying patch still holds biomass. That amount accumulates into a new
`SimulationWorld._cumulativePlantBiomassLostToMortality`, surfaced through
`SimulationStatistics`, so the project's existing biomass-conservation
invariant continues to balance instead of silently leaking mass.

### Interaction with the sprout floor

None, by construction. Death is age-based; the 2026-08-16
`SproutFloorFraction = 0.01` recovery is biomass-based. A patch grazed to zero
still regrows — it simply cannot outlive its lifespan. The two rules are
orthogonal and neither overrides the other.

### Flag

New `SimulationConfig.PlantMortalityEnabled`, default `false`, appended as the
constructor's new last optional parameter after `plantSiteCompetitionEnabled`,
with its `{ get; }` property placed immediately after
`PlantSiteCompetitionEnabled`. With the flag off, no patch ages and none is
removed, so behavior is byte-identical and existing hash baselines are
unaffected.

## Scope boundary

- No plant corpse, decay stage, or seed bank — death removes the patch
  immediately.
- `SeedReserve` stays unwired; wiring it is separate work.
- No change to `PlantGrowthSystem`'s growth formula, `FindSite`'s dispersal or
  competition rules, or any existing plant genome trait.
- No new RNG domain.
- This design removes the structural blocker to plant evolution. It does not
  by itself claim a coevolution result — that requires re-running the paired
  experiment afterward and is deliberately not part of this fix's scope.

## Testing

1. `PlantPatchStore.RemoveAt` unit test: removing a middle index moves the
   last patch into that slot with every field intact (id, resource id,
   position, biomass, capacity, growth rate, nutrition, defense, genome,
   lineage, age, cooldown), decrements `Count`, and leaves the remaining
   patches findable by `FindIndex`.
2. `PlantPhenotype` unit test: `LifespanSeconds` at `Growth = 0` is exactly
   twice `LifespanSeconds` at `Growth = 1`.
3. `PlantMortalitySystem` unit test: a patch's `Age` accrues by `deltaTime`
   each step.
4. `PlantMortalitySystem` unit test: a patch is removed on the step where
   `Age` reaches `LifespanSeconds`, and not before.
5. `PlantMortalitySystem` unit test: a fast-growth genome patch dies strictly
   earlier than a slow-growth genome patch created at the same time.
6. Site-release unit test: after a patch dies, its resource is inactive with
   zero projected amount, and a subsequent `PlantReproductionSystem.Step` can
   recolonize that site.
7. Biomass-accounting unit test: biomass held by a dying patch appears in the
   cumulative mortality counter, so total accounted mass is unchanged within
   `0.0001f`.
8. **Regression guard for the 2026-08-17 finding:** integration test asserting
   `HighestPlantGeneration > 2` after a 12,000-tick run with the flag enabled.
   This fails against pre-fix code and passes after.
9. Hash-regression test: standard `PredationVariation`/`Legacy` scenario with
   the flag unset still produces its current baseline, derived fresh from a
   throwaway worktree at the pre-task commit.
10. Full existing suite stays green (`cd tools/HeadlessTests && dotnet test`).
