# Mating Behaviour Design

**Status:** design approved. Third of four behaviour-layer specs.

**Scope:** make reproduction a behaviour creatures perform rather than a spatial coincidence. Resolves defect **C-2**.

**Depends on:** `2026-08-14-foraging-economics-design.md` for approach commitment, `2026-08-14-place-memory-design.md` for perception patterns.

**Not in scope:** juveniles.

## This triggers a condition the P3 spec already wrote

P3 trait slice 4 reads: "Optional mate signaling: add only if two-parent choice remains behaviorally impoverished after the prior slices." The architecture spec repeats it: "sexual-selection signals only if two-parent mating needs richer choice."

The condition is met, and not marginally. `CreatureAction.SeekMate` is declared and never produced. `ReproductionSystem.Step` scans the grid, pairs any two ready creatures within `MateDistance = 2f`, and writes `CreatureAction.Reproduce` onto both *retroactively*. No creature ever seeks, approaches, or chooses a mate. Reproduction is proximity luck.

This spec therefore activates slice 4 rather than inventing scope. It changes mating from an outcome into a decision, and leaves the two-parent genetics, crossover, mutation, and ancestry exactly as they are.

## Design

### `SeekMate` becomes a scored action

Readiness is unchanged from `ReproductionSystem.IsReady`: energy, hydration, and health at or above 70% of capacity, adult age reached, cooldown elapsed.

A ready creature scores mate candidates like any other destination:

```text
mateScore = ownReadiness
          × partnerReadiness
          × mateQuality(self, candidate)
          × Clamp01(1 − travelEnergy / EnergyBudgetForCourtship)
```

Travel cost uses the same energy expression as foraging, so distant mates are worth pursuing only when the creature can afford the trip. Commitment from the foraging spec is what lets the approach complete instead of being abandoned halfway — the reason this spec depends on that one.

### Perception must return more than one candidate

`PerceptionSystem.FindNearestOtherCreature` returns exactly one creature, which makes choice impossible: you cannot select among candidates you cannot see. This spec adds:

```text
int FindNearbyCreatures(..., Span<CreatureObservation> results)
```

filling up to `MateCandidateLimit` entries, nearest first. The existing single-result method stays for predation, so nothing currently working changes.

This is the same query group behaviour will later need, which is why it is written as a general top-K rather than a mating-specific call.

### Mate quality, derived and unlabelled

```text
mateQuality = vigour × (1 − woundPenalty) × choosinessTerm(geneticDistance)
```

- **vigour** — the candidate's energy and health fractions. Cheap, honest, and always available.
- **woundPenalty** — from `CombatState.WoundSeverity`. An injured mate is a worse bet.
- **choosinessTerm** — how a creature weighs genetic similarity, scaled by a new `MateChoosiness` gene. Above neutral prefers similarity, below prefers difference, at neutral ignores it.

Genetic distance is computed directly from genome fields. **No species label, cluster, or category enters this calculation** — required by permanent architectural principle 4 and by P5's rule that analysis output never feeds back into mating.

`MateChoosiness` follows the project's trait rule:

- **Benefit:** avoids wasting a scarce reproductive opportunity on a poor partner.
- **Cost:** a maintenance term in `Phenotype.FromGenome`, plus the real risk of rejecting adequate mates and reproducing later or never.
- **Falsifiable experiment:** choosiness should rise when mates are plentiful and fall when they are scarce. If it drifts identically under both, it is not paying for itself.

### Courtship makes it watchable

A pair whose mutual `SeekMate` scores exceed `CourtshipThreshold` and who are within `MateDistance` enter `Courting` for `CourtshipSeconds` before conceiving. Either may abandon if a threat appears or a need becomes urgent.

This exists for legibility. Instantaneous conception at contact is invisible; a brief, interruptible pause is the moment an observer can actually watch. It also creates a real cost — time spent courting is time not feeding or fleeing — which is what makes choosiness a genuine trade-off rather than free.

### Reproduction stops manufacturing pairs

`ReproductionSystem.Step` no longer searches for pairs. It confirms pairs that courtship produced, then runs the existing crossover, mutation, placement, cost, and cooldown logic unchanged. Ancestry and two-parent inheritance are untouched.

## Components

| File | Change |
|---|---|
| `Behavior/PerceptionSystem.cs` | `FindNearbyCreatures` top-K query |
| `Behavior/MatingSystem.cs` | **New.** Mate scoring, quality, genetic distance, courtship state machine |
| `Core/SimulationTypes.cs` | `CourtshipState`; `CreatureAction.Courting` |
| `Core/CreatureStore.cs` | Dense `CourtshipState[]` sidecar |
| `Biology/ReproductionSystem.cs` | Confirm courted pairs instead of searching for proximate ones |
| `Biology/GenomePhenotype.cs` | `MateChoosiness` gene, maintenance cost, phenotype passthrough |
| `Core/SimulationConfig.cs` | `MateCandidateLimit`, `CourtshipThreshold`, `CourtshipSeconds`, `EnergyBudgetForCourtship`, `MatingBehaviourEnabled` |

Gated behind `MatingBehaviourEnabled`, default `false`, so existing scenarios keep the current reproduction path.

## Testing

1. A ready creature with a visible ready partner produces `SeekMate`, not `Wander`.
2. A creature that is not ready never produces `SeekMate`.
3. Two mutually seeking creatures converge and enter `Courting`.
4. Courtship completing produces exactly one child with both parents recorded in lineage.
5. Courtship interrupted by a threat produces no child and leaves both cooldowns untouched.
6. Given two equally distant candidates, the higher-vigour one is chosen.
7. A wounded candidate scores below an unwounded one, all else equal.
8. `MateChoosiness` above neutral prefers genetically similar partners; below neutral prefers dissimilar.
9. No species or cluster identifier appears anywhere in the mating path.
10. `FindNearbyCreatures` returns candidates nearest-first, never exceeds the limit, and allocates nothing.
11. Distant mates are not pursued when travel energy exceeds the courtship budget.
12. With `MatingBehaviourEnabled = false`, reproduction behaves exactly as it does today.

## Exit gate

- All twelve fixtures pass.
- Births arise from completed courtships, and the count of proximity-only pairings is zero.
- Birth rate under the new path is within a documented tolerance of the old path in a baseline scenario, so the change is a mechanism change rather than a fertility change.
- A paired-seed experiment shows `MateChoosiness` shifting in opposite directions under mate-abundant and mate-scarce treatments, or the trait is reconsidered.
