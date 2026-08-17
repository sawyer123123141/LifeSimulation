# Plant Site Competition — Design

## Problem

P4's roadmap delivery plan (`docs/ROADMAP.md`) names "plant competition" as a
required capability. It does not exist: `PlantReproductionSystem.FindSite`
only considers candidate dispersal sites that are inactive
(`candidate.IsActive == false`) — an already-occupied patch, however weak,
can never be displaced. `PlantPatchStore` has no removal method at all;
patches only ever get created or mutated in place.

This was surfaced concretely today: a coevolution-signal experiment
(`ConsumerDefenseCalibrationControl` vs `...Moderate`, 0.3 plant defense, 5
seeds, population caps swept at 48/32/24/16) found no measurable
food-efficiency selection signal in any configuration. Part of the reason
defense currently costs nothing in the long run is that a struggling,
heavily-grazed patch simply persists forever at its own site — losing
biomass never costs it the site itself, so there is no spatial mechanism by
which a fitter lineage can actually displace a less-fit one.

The original 2026-08-13 P4 planning note
(`docs/experiments/p4-plant-biomass-baseline-2026-08-13.md`) already named
the target mechanic precisely: "seed budgets, deterministic dispersal,
establishment, and **biomass-conserving patch competition**." This design
implements that slice.

## Fix

### Vulnerability threshold

A patch is *vulnerable* to takeover when `Biomass / Capacity` falls below a
new constant, `VulnerabilityFraction = 0.25f`. A healthy patch (at or above
25% of capacity) cannot be displaced under any circumstance — this bounds
the mechanic to only affect patches already losing the fight against
grazing/environment, not arbitrary contest between equals.

### Extended site search

`PlantReproductionSystem.FindSite` currently skips any `candidate.IsActive`
site. Under the new flag, when the normal empty-site search finds nothing
in a given attempt, it also considers active `Food` candidates: skip unless
the corresponding plant patch (looked up via
`patches.FindIndex(candidate.Id)`) is vulnerable per the threshold above.
The existing distance check (`> range` skip) and establishment roll
(`EstablishmentSuccessProbability`, same formula, same RNG domains) apply
identically to both empty and occupied candidates — no new randomness
domain, no new probability model. This keeps takeover exactly as likely as
ordinary empty-site establishment at the same distance, gated purely by the
resident's vulnerability.

### Biomass-conserving takeover

On a successful contested roll, instead of `patches.Add` (which creates a
new patch entry), the existing vulnerable patch is overwritten in place:

- New `PlantPatchStore.ReplaceAt(int index, PlantGenome genome, PlantLineage lineage, float biomass, float growthRate, float nutrition, float defense)` 
  method — sets all of a patch's identity/trait fields at once (genome,
  lineage, growth rate, nutrition, defense) while preserving its existing
  `Id`, `FoodResourceId`, `Position`, and `Capacity` (those belong to the
  site, not the occupant).
- The child's starting biomass is `transferred seed biomass + resident's
  existing biomass`, capped at the site's capacity — the resident's
  existing biomass is carried into the new occupant rather than destroyed,
  matching "biomass-conserving" from the original design note. Total
  system biomass is unchanged: `transferred` was already subtracted from
  the parent via `ConsumeAt` (existing code, unchanged), and the resident's
  biomass simply changes ownership instead of vanishing.
- Lineage: same as ordinary dispersal (`new PlantLineage(childId, parent.Id,
  parent.Lineage.Generation + 1)`) — the takeover is genetically a normal
  child of the disperser, it just happens to occupy a site instead of an
  empty one.
- `ReproductionCooldownRemaining` on the new occupant is left at whatever
  `ReplaceAt` sets it to (0, i.e. no fresh cooldown penalty beyond the
  parent's own cooldown) — the resident's specific cooldown value is
  irrelevant since it's a different lineage/individual now.

### Flag

New `SimulationConfig.PlantSiteCompetitionEnabled` bool, default `false`,
appended as the constructor's new last optional parameter with a matching
`{ get; }` property placed immediately after `MateSelectionEnabled` (the
current last flag), per this project's established flag convention. When `false`, `FindSite`'s behavior is
byte-identical to today (never considers active candidates) — the
hash-regression baseline (`PredationVariation`/`Legacy` scenario, hash
`12050501592762519865UL`) is unaffected, since that scenario never sets
`PlantCohortsEnabled` at all, let alone this new flag.

## Scope boundary

- No new RNG domain — reuses `RandomDomain.PlantDispersal` /
  `RandomDomain.PlantEstablishment` identically for both empty and occupied
  candidates.
- No change to `PlantGrowthSystem`'s growth formula, `PlantReproductionSystem.Step`'s
  maturity/cooldown gating for the *parent*, or `ConsumeAt`'s semantics.
- No removal method added to `PlantPatchStore` — takeover is overwrite-in-place,
  which is sufficient and matches the store's existing add-only, mutate-in-place
  design. A true removal API is not needed for this mechanic.
- No density-dependent capacity/crowding model — that was the rejected
  alternative (bigger surface, less directly tied to the defense-selection
  question this was motivated by).
- Does not itself guarantee a measurable coevolution signal — it removes a
  structural blocker (patches can now actually lose ground), but proving an
  actual reciprocal-selection result is a separate follow-up experiment,
  not part of this fix's testing scope.

## Testing

1. `PlantPatchStore.ReplaceAt` unit test: overwrites genome/lineage/growthRate/
   nutrition/defense/biomass, preserves Id/FoodResourceId/Position/Capacity.
2. `PlantReproductionSystem.FindSite` unit test (flag false): an occupied
   vulnerable candidate is never returned, even when no empty site exists in
   range (byte-identical to current behavior).
3. `PlantReproductionSystem.FindSite` / `Step` unit test (flag true): a
   vulnerable occupied candidate (biomass below 25% capacity) within range
   can be selected and taken over — resulting patch has the disperser's
   genome, and its biomass equals `transferred + resident's prior biomass`
   (within capacity).
4. `PlantReproductionSystem.Step` unit test (flag true): a non-vulnerable
   occupied candidate (biomass at or above 25% capacity) is never taken
   over, even when it is the only candidate within range and an empty
   candidate exists nowhere else (dispersal simply fails that attempt, `births`
   does not increase for that parent).
5. Biomass-conservation test: sum of all patch biomass plus parent's
   remaining biomass before and after a successful takeover differs only by
   floating-point tolerance (`Within(0.0001f)`), proving nothing was created
   or destroyed.
6. Hash-regression test: standard `PredationVariation`/`Legacy` scenario
   (flag never set) still produces `12050501592762519865UL`.
7. Full existing suite stays green (`cd tools/HeadlessTests && dotnet test`).
