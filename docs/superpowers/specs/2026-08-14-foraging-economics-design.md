# Foraging Economics Design

**Status:** design approved. First of four behaviour-layer specs.

**Scope:** how a creature values a resource and decides whether to keep pursuing it. Resolves defects **B-3** (resource quality absent from scoring) and **B-4** (no commitment, endless oscillation) from `2026-08-14-simulation-defects-and-behavior-gaps.md`.

**Not in scope:** place memory, mating, juveniles. Each has its own spec.

## Why this is first

The P4a milestone asks for animals that visibly learn, breed, migrate, and diverge. None of that is currently visible, because decisions are recomputed wholesale twice a second with no commitment — a bold creature and a timid one both render as jitter. Behaviour that does not persist cannot look like personality.

Commitment is therefore the substrate. Once a creature sticks with a choice, trait differences become *spatial* and readable at population scale without an inspector: bold lineages spread, cautious ones cluster. Mating also depends on it, because a creature that abandons its approach halfway never arrives.

## Behaviour-layer sequence

| Spec | Contents | Depends on |
|---|---|---|
| **1. Foraging economics** (this) | patch value, travel cost, commitment, give-up | — |
| 2. Place memory | capacity-N sidecar, per-place learned value | 1 |
| 3. Mating behaviour | `SeekMate` as a scored action, mate perception | 1, 2 |
| 4. Juvenile behaviour | parental following, reduced capability | 1, 2, 3 |

## Design

### Value a patch by what it yields, not by how near it is

`DecisionSystem.Availability` is currently `1 / (1 + distance)`, and `ResourceState.Amount` never enters any score. Creatures therefore always choose the nearest resource and never the richest, which is the direct cause of starvation beside a stripped patch.

Expected gain from a patch:

```text
expectedGain = min(Amount, IngestionRate × HandlingSeconds)
             × PlantFoodYieldMultiplier
             × NutritionMultiplier
```

`HandlingSeconds` is a configuration constant representing how long a creature commits to feeding once it arrives. Capping by `Amount` is what makes a depleted patch score low.

### Charge travel in energy, using the real movement formula

`NeedsSystem.Tick` already charges movement as:

```csharp
movementDistance * phenotype.BodyMass * 0.5f
```

The score estimates travel cost with that **same expression**, so a creature predicts its actual expenditure rather than using an invented constant, and there is no second number to keep in sync.

```text
travelEnergy = distance × BodyMass × 0.5
netGain      = expectedGain − travelEnergy
score        = urgency × Clamp01(netGain / ReferenceGain)
```

`ReferenceGain` is a configuration constant that normalizes score into `[0, 1]` so actions remain comparable.

**A patch costing more than it yields produces a negative `netGain`, so its score clamps to zero and the creature will not make the trip.** No special rule is needed to stop suicidal foraging; it falls out of the arithmetic. Large-bodied creatures pay more to travel and so forage closer, which is a real biological consequence obtained for free.

### Commitment: a decaying bonus for continuing

Add to the score of whichever action the creature is already performing:

```text
commitmentBonus = CommitmentStrength × Persistence × decay(secondsInCurrentAction)
```

`decay(t)` is exponential: `0.5 ^ (t / CommitmentHalfLifeSeconds)`. It breaks ties early and stops mattering once a choice is clearly failing, so it never locks a creature into an action indefinitely.

### Give-up: the marginal value theorem

Each creature keeps a running average of its recent energy intake rate. It abandons its current patch when the patch's instantaneous rate falls below that average:

```text
abandon when currentPatchRate < RecentIntakeRate × (1 − Persistence) × GiveUpSensitivity
```

This is the standard optimal-foraging departure rule: leave when here is worse than the habitat average. It is what produces visible migration — creatures drain a patch, its rate falls, and they leave together.

### One new gene: `Persistence`

`Persistence` scales both the commitment bonus and the give-up threshold. One gene, two effects, so the genome does not bloat.

Per the project rule that every heritable trait needs an explicit cost, benefit, and falsifiable experiment:

- **Benefit:** avoids oscillation, avoids abandoning good patches prematurely, completes long journeys.
- **Cost:** a maintenance term in `Phenotype.FromGenome`, matching the existing pattern, plus the implicit cost of staying too long on a declining patch.
- **Falsifiable experiment:** persistence should rise under patchy, slowly-renewing resources and fall when resources relocate frequently. If it drifts the same way under both, the trait is not carrying its cost and the design is wrong.

### Enablement and evidence safety

Gated behind `SimulationConfig.ForagingEconomicsEnabled`, default `false` — the same pattern as `CognitionEnabled` and `PhysiologyEnabled`.

Gating means that with the flag off, **behaviour is identical** — the same decisions, the same positions, the same outcomes — so existing scenarios keep producing the results they always produced.

**The state hash still changes, and this spec does not pretend otherwise.** `ComputeStateHash` hashes every genome field unconditionally, so adding `Persistence` mixes an extra value into the hash whether or not the flag is set. This is the same situation the project already handled when the cognition and physiology genes were added: the genome schema version increments, older scenarios default the new gene, and the migration fixture records it.

The practical consequence is narrower than a behaviour break. Recorded *results* — trait shifts, survival, population curves — remain valid because behaviour is unchanged with the flag off. Only recorded *hashes* need regenerating, which is a mechanical step, not a re-run of the science.

## Components

| File | Change |
|---|---|
| `Behavior/ForagingEconomics.cs` | **New.** Pure scoring: `ExpectedGain`, `TravelEnergy`, `PatchScore`, `ShouldAbandon` |
| `Behavior/DecisionSystem.cs` | Calls `ForagingEconomics` when enabled; unchanged path when not |
| `Core/SimulationTypes.cs` | `ForagingState` struct: `SecondsInCurrentAction`, `RecentIntakeRate` |
| `Core/CreatureStore.cs` | Dense `ForagingState[]` sidecar, following the existing array pattern |
| `Biology/GenomePhenotype.cs` | `Persistence` gene, maintenance cost, phenotype passthrough |
| `Core/SimulationConfig.cs` | `ForagingEconomicsEnabled` flag and the four constants |
| `Core/SimulationWorld.cs` | Update `ForagingState`; include `Persistence` in the state hash |

Configuration constants: `HandlingSeconds`, `ReferenceGain`, `CommitmentStrength`, `CommitmentHalfLifeSeconds`, `GiveUpSensitivity`.

`ForagingEconomics` is a static class of pure functions taking structs, so it allocates nothing and is directly unit-testable without constructing a world.

## Testing

1. A rich far patch outscores a depleted near one.
2. A patch whose travel cost exceeds its yield scores zero.
3. A heavier creature's foraging range is measurably shorter than a lighter one's, all else equal.
4. Given two near-tied options, a creature with the commitment bonus does not alternate between them across successive decision ticks.
5. Commitment decays: after `CommitmentHalfLifeSeconds`, the bonus is at most half its initial value.
6. A creature abandons a patch once its intake rate falls below the recent average, and a high-`Persistence` creature abandons later than a low-`Persistence` one.
7. `Persistence` carries a maintenance cost: two genomes differing only in `Persistence` have different `BasalEnergyCostMultiplier`.
8. With `ForagingEconomicsEnabled = false`, behaviour is identical: over 1,000 ticks every creature's position, needs, and decision match a run built before this change. Hashes are expected to differ because the genome gained a field; results are not.
9. Allocation guard: 100,000 `PatchScore` calls allocate zero managed bytes.

## Exit gate

- All nine fixtures pass.
- With the flag off, every existing test passes and every frozen scenario produces unchanged behaviour, with regenerated hashes recorded under an incremented genome schema version.
- With the flag on, creatures demonstrably leave exhausted patches instead of dying on them — measured as a fall in starvation deaths within interaction radius of a depleted resource.
- A paired-seed experiment shows `Persistence` shifting in opposite directions under patchy versus relocating resource treatments, or the trait is reconsidered.
