# Halfway-Wired Mechanism Audit — 2026-08-17

Prompted by the place-memory false root cause
(`p4-memory-root-cause-retracted-2026-08-17.md`), which cost a full
investigation cycle because dead code reads as though it runs. This is a
systematic sweep for the same failure shape: a mechanism that is written,
tested, and either never invoked or invoked on only one of the two
decision-policy paths.

Method: extract every public method, config flag, genome/phenotype property and
state-struct field under `Assets/Scripts`, then count production references
excluding the defining file. Anything with zero production callers, or callers
reachable only under one `DecisionPolicyVersion`, is listed below. Test-only
references are reported separately, since "tested but unwired" is the exact
pattern being hunted.

## Class A — never executes in production, under any configuration

| mechanism | evidence |
| --- | --- |
| `MemorySystem.ObservePlace` | only writer of `PlaceMemory` slots; callers exist only in `PlaceMemoryObservationTests.cs` (7 refs) |
| `MemorySystem.TickPlaceMemoryDecay` | no production caller; 5 test refs |
| `DecisionSystem.PreferRememberedResource` | no production caller; 1 test ref |
| `PlantPatchState.SeedReserve` | constructor parameter and property in `PlantTypes.cs` only; never written with a meaningful value, never read |
| `Genome.Commitment` / `Phenotype.Commitment` | inherited at `traitIndex: 22`, hashed at `SimulationWorld.cs:369`, aggregated at `SimulationWorld.cs:1455`, exposed as `ExperimentMetric.Commitment` — and read by **zero** behavior code |

`Commitment` deserves emphasis: `ForagingEconomics.CommitmentBonus` does not take
it. Both call sites (`DecisionSystem.cs:865`, `:869`) pass
`phenotype.Persistence`. The gene is inherited, mutated, hashed, and reported in
experiment metrics while having no path to behavior at all — a free statistical
placebo that any experiment sweeping it would report on.

## Class B — executes, but always on empty data

| mechanism | evidence |
| --- | --- |
| `SimulationWorld.TryScoreBestRememberedPlace` | Legacy-only guard, and its loop `continue`s on every slot because no slot is ever populated |
| `MemorySystem.RecordFailedPlaceSearch` | Legacy-only guard, and cannot match a slot for the same reason |

## Class C — unreachable under `IntentUtilityV1` (the configuration P4 runs)

| mechanism | guard | consequence |
| --- | --- | --- |
| `ForagingEconomics.CommitmentBonus` | called only from `DecisionSystem.Decide`, the Legacy foraging path | never runs under `IntentUtilityV1` |
| `ForagingEconomics.ShouldAbandon` | `ForagingEconomicsEnabled && Legacy && !CognitionEnabled` (`SimulationWorld.cs:889-892`) | never runs under `IntentUtilityV1`, and never under Legacy-with-cognition either |

### Correction to the 2026-08-17 persistence fix

`GenomeInheritance.CreateChild` genuinely dropped the `persistence` parameter,
and that fix (`027b5ff`) is correct. But the conclusion drawn from it — that
B-4's foraging economics now "run on a live input" — does not hold for P4.

`phenotype.Persistence` is read in exactly three places: `CommitmentBonus`
(Legacy path), `ShouldAbandon` (Legacy and cognition-disabled only), and the
state hash. Under P4's actual configuration — `IntentUtilityV1` with
`CognitionEnabled` — **`Persistence` still has no behavioral effect.** It is
live only in Legacy configurations. B-4 should not be considered resolved for
P4 on the strength of that commit.

## Class D — checked and clean

Verified as genuinely wired, listed so they are not re-audited:
`CognitionRestCostMultiplier` (`NeedsSystem.cs:62`),
`ReproductionCooldownSeconds` and `ReproductionEnergyCostFraction`
(`ReproductionSystem.cs:242`, `:235`), `Persistence` inheritance itself, and
every `SimulationConfig` flag — all fifteen have at least one production reader.
No state-struct field in `SimulationTypes.cs` is unwritten once compound
assignment (`+=`, `++`) is accounted for.

The remaining Legacy-only guards in `SimulationWorld` (`:745`, `:937`, `:971`)
are not defects: `IntentUtilityV1` carries its own equivalents inline
(candidate-buffer population, `ScorePredation`, `ScoreThermalComfort`).

## Recommended disposition

Deletion is preferred to wiring for most of these, on two grounds. First, every
wiring change alters foraging or selection behavior immediately before the
plant-mortality recalibration and the coevolution experiment, which is poor
experimental hygiene. Second, dead code that reads as live is precisely what
manufactured the retracted root cause; removing it prevents recurrence, whereas
leaving it wired-but-untuned adds new confounds.

Proposed, in order of independence:

1. **Delete `Commitment`** (gene, phenotype property, inheritance slot, hash
   contribution, statistics aggregation, `ExperimentMetric` case). It is
   redundant with `Persistence`, which already occupies its intended role. This
   changes the genome arity from 24 to 23 and rederives every hash baseline.
2. **Delete `SeedReserve`** and `DecisionSystem.PreferRememberedResource`. Both
   are inert; removal is behavior-neutral.
3. **Decide place memory** (`ObservePlace`, `TickPlaceMemoryDecay`,
   `TryScoreBestRememberedPlace`, `RecordFailedPlaceSearch`, the `PlaceMemory`
   struct, and `CreatureStore._placeMemories`) — delete, or wire it fully and
   port both consumers to `IntentUtilityV1`. Wiring is a genuine behavior
   change and should be its own spec, sequenced **after** the coevolution
   measurement, not before.
4. **Decide `Persistence` under `IntentUtilityV1`** — either port
   `ShouldAbandon`/`CommitmentBonus` into the intent-utility path, or accept
   that foraging commitment is a Legacy-only feature and record that in B-4.

Items 1 and 2 are behavior-neutral and safe to land immediately. Items 3 and 4
are behavior changes and should wait until the P4 measurements are done.

No production code was changed by this audit.
