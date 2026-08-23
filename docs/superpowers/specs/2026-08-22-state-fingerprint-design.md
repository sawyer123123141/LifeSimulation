# State Fingerprints — Design

**Status:** IMPLEMENTED in `7343653`. See "As built" at the foot of this file for the two places
the implementation went beyond the design.
**Date:** 2026-08-22
**Motivation:** an evidence-integrity review found `ComputeStateHash` incomplete as a future-state
fingerprint. Confirmed, and the audit below found more omissions than the review named.

## Why this is not one hash

The review recommended completing `ComputeStateHash`. Doing that as a single undifferentiated hash
would break two existing contracts at once, so this design separates three hashes with three
different jobs.

| | job | must include | must exclude |
|---|---|---|---|
| **StateHash V1** | historical continuity with every recorded baseline | exactly what it includes today | any change at all |
| **StateFingerprint V2** | "will these two worlds evolve identically from here?" | all future-determining state **and configuration** | derived caches, reporting accumulators |
| **BehaviorHash** | gene/flag liveness: did this value reach behavior? | all behavior-side state | genomes, phenotypes, **and configuration** |

The critical interaction: **V2 must include configuration and BehaviorHash must not.**
`FlagLivenessAnalysis` decides a flag is live by flipping it and comparing `BehaviorHash`. If
configuration were folded into that hash, flipping any flag would change it by definition and
**every flag would read live**, destroying the only harness that distinguishes wired mechanisms
from unreachable ones. Two flags are currently and correctly pinned as inert on scenario grounds;
that verdict must survive this work.

`BehaviorHash` already exists and already excludes genome and phenotype. It needs an audit against
the omission list below, not a redesign.

## Audit: future-determining state absent from `ComputeStateHash`

`ComputeStateHash` today covers `Config.WorldSeed`, `CurrentTick`, `CreatureCount`, `_spawnOrdinal`,
per-creature genome/needs/movement/decision/reproduction/combat/memory (and home-range when
enabled), all resource fields, and per-plant biomass/capacity/traits/genome/lineage.

Missing, in descending severity:

1. **`_birthOrdinal`** — feeds the deterministic RNG stream for every birth's mutation draw. Two
   worlds identical on V1 but differing here produce different offspring genomes on the next birth.
   *The review did not identify this.*
2. **`_plantSeedOrdinal`** — feeds the RNG stream for plant dispersal site selection and seed
   mutation. Same failure mode on the next dispersal. *The review did not identify this.*
3. **Plant `Age`** — determines when `PlantMortalitySystem` removes a patch. Its absence is why the
   `ReplaceAt` takeover-age defect fixed on 2026-08-22 was invisible to every hash regression: a
   genuine behavior change that no pinned hash could detect.
4. **Plant `ReproductionCooldownRemaining`** — determines when a patch may next disperse.
5. **`CreatureStore._nextId`, `ResourceStore._nextId`, `PlantPatchStore._nextId`** — determine the
   identity of everything created from here. Identity feeds ancestry, P5 clustering and lineage
   evidence.
6. **`PlantSiteRegistry` contents and order** — `FindSite` iterates it by index, so its contents and
   order determine dispersal outcomes.
7. **Configuration beyond `WorldSeed`** — every flag and tuning constant. Two worlds with identical
   state and different configs diverge immediately.

Deliberately **excluded** from V2, with reasons:

- **Reporting accumulators** — `_birthCount`, `_deathCount`, cause counters, cumulative consumption,
  `_plantBiomassSeconds`, `_plantPatchSeconds`, `RealizedGrazingPressure`. Nothing reads them back
  into behavior. Including them would make V2 change when only reporting changed.
- **`LivenessRecorder` counters** — must never affect any hash; that is their correctness condition.
- **Derived caches** — `ResourceGrid`, `CombatGrid`, `CreatureGrid`, `_resourceAllocations`,
  `_resourceRequestCount`. These are pure functions of state that is already included, *provided
  V2 is only sampled at a settled tick* (below). If that invariant is ever violated they must be
  included instead, and the stale-grid question in the review becomes a correctness bug rather than
  a design question.
- **Pending deaths** — `_pendingDeaths` is filled and fully drained inside a single `Step`. It is
  always empty at a settled tick. This is now structurally guaranteed: as of 2026-08-22 the commit
  loop runs before the statistics sample and before `CurrentTick` advances.

## When V2 is valid to sample

**Only between completed `Step` calls**, i.e. when `CurrentTick` has just been advanced and no
`Step` is in progress. At that boundary: pending deaths are empty, resource allocation buffers are
consumed, and grids are either current or will be rebuilt from included state before next use.

Sampling mid-`Step` is undefined and must not be exposed. The implementation should therefore live
on `SimulationWorld` as a method that cannot be called from inside the tick path, and the invariant
should be asserted by a test that steps to a boundary, fingerprints, clones, and compares.

## Versioning

V2 carries an explicit version constant hashed as its first field. Any change to the field set is a
new version number, never a silent redefinition — the failure this whole exercise exists to prevent
is a fingerprint that quietly stops meaning what a recorded baseline assumed it meant.

V1 stays frozen and keeps its name and value. Historical experiment files reference V1 values; they
remain valid as *V1* values and must not be recomputed or overwritten.

## Acceptance

- Two worlds with equal V2 fingerprints, stepped identically, stay equal for at least 2,000 ticks —
  tested across the ordinal-sensitive paths specifically: a birth, a plant dispersal, a plant death
  and a takeover.
- Two worlds differing **only** in `_birthOrdinal`, only in `_plantSeedOrdinal`, only in plant age,
  or only in one config flag each produce different V2 fingerprints. One test per omission above.
- `FlagLivenessAnalysis` still reports exactly the known inert set. **If adding configuration
  anywhere makes an extra flag read live, the change is wrong**, not the pinned set.
- V1 values are unchanged for every scenario with a recorded baseline.

## Non-goals

- Making V1 complete. It is a historical identifier now, not a correctness tool.
- Using V2 for liveness. That is `BehaviorHash`'s job and the two must not be merged.
- Changing what `BehaviorHash` excludes on the genome/phenotype/config side.


---

## As built (`7343653`)

Implemented as designed, with two decisions the design left open:

**Home-range state is hashed unconditionally.** V1 hashes it only when `HomeRangeAffinityEnabled`
is set. V2 must not do that: a fingerprint whose *field set* depends on a flag cannot answer
"will these two worlds evolve identically", because two worlds differing in that flag would be
compared on different fields.

**`BehaviorHash` gained plant `Age` and `ReproductionCooldownRemaining`.** The design listed these
as V2 omissions and said `BehaviorHash` needed "an audit, not a redesign". The audit found they
belong there too: both are behavior state rather than genome, both decide *when* something happens,
and their absence is precisely why the `ReplaceAt` takeover-age defect was invisible to every hash
regression.

This was decided by measurement, not by argument. Prediction stated first: the inert set would not
change, because all four inert flags are inert for a *reachability* reason (readers on the Legacy
path), not a sensitivity one. Measured with the two lines added and removed: the inert flag set is
identical, and every plant gene liveness verdict is identical — 33 / 19 / 1 green either way. No
`BehaviorHash` value is pinned as a literal anywhere in the repo; it is only ever compared against
itself, so extending it invalidates no baseline. Strictly more sensitive at no cost to the pinned
set, so it was kept.

**Config hash coverage.** 44 of `SimulationConfig`'s 46 public properties are hashed. The two
excluded are derived: `FixedDeltaTime` (from `Schedule.BaseFrequencyHz`) and `MaximumMemorySlots`
(from `MinimumMemorySlots + AdditionalMemorySlots`), both already covered through their inputs.
Two drift guards enforce this: every `bool` constructor parameter must move the config hash, and
the public property count is pinned, so adding a field without hashing it fails a test rather than
silently producing a fingerprint that no longer means what a baseline assumed.

**Acceptance, as measured.** 2,000-tick equality holds, with positive controls proving the run
actually exercised all four named paths — births, dispersal, plant death and takeover. The takeover
control is keyed on the **lineage parent**, not on the age reset, because keying it on age would key
the detector on the very fix that motivated this work. One test per omission. 489 / 19 / 33 / 1
green, up from 480; the three liveness counts are unchanged, which is the acceptance criterion that
mattered.
