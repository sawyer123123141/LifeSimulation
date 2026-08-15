# Place Memory Design

**Status:** design approved. Second of four behaviour-layer specs.

**Scope:** replace the three fixed memory slots with the capacity-N sidecar the P2 plan already specifies. Resolves defects **C-1** (memory is three singleton slots), **B-2** (learned value is per resource kind, not per place), **B-1** (learning signal saturates), and **B-6** (threat memory written and never read).

**Depends on:** `2026-08-14-foraging-economics-design.md`, which supplies the scoring a remembered place is valued with.

**Not in scope:** mating, juveniles.

## This is written scope, not new scope

The P2 data phase in `2026-08-12-p0-p7-program-plan.md` specifies "a fixed-capacity aligned memory sidecar for resource, threat, and encounter observations" with "confidence, observation age, decay, and deterministic replacement metadata". The architecture spec adds that "Fixed per-creature capacity avoids per-creature collections" and that "Increasing capacity is a heritable benefit with a proportional memory and metabolism cost".

What exists is one food slot, one water slot, one threat slot, and no replacement policy. The `MemoryCapacity` gene affects only `CognitionRestCostMultiplier`, so creatures pay a metabolic cost for capacity they never receive. This spec builds what was written.

## Design

### A fixed-capacity ring of place memories

```text
PlaceMemory
├── Position        SimVector2
├── Kind            Food | Water | Threat
├── LastKnownAmount float
├── OutcomeValue    float    running average of what this place actually gave
├── VisitCount      int
├── Confidence      float
└── LastSeenTick    long
```

Capacity per creature:

```text
capacity = MinimumMemorySlots + round(MemoryCapacity × AdditionalMemorySlots)
```

Slots are allocated once as a single dense `PlaceMemory[]` sized `maximumPopulation × maximumSlots`, indexed by `creatureIndex × maximumSlots + slot`. Per-creature capacity is a count, not a separate allocation, so memory cost is fixed per configured capacity and the hot loop stays allocation-free — both P2 exit requirements.

`MemoryCapacity` now buys slots and still costs metabolism, so the gene finally has the benefit its cost was already paying for.

### Deterministic replacement

When a creature observes a place and its ring is full, evict the entry with the lowest `Confidence × OutcomeValue`. Ties break by lowest `LastSeenTick`, then by lowest slot index. No randomness, so replacement is reproducible.

### Learned value is per place, and no longer saturated

Defect B-1: the current outcome is `min(1, nutrition × FoodYield × 20)`, which clamps on essentially every feeding event, so every creature's learned value converges to 1.0 and carries no information.

Replace with intake measured against expectation:

```text
outcome = Clamp01(actualIntakeRate / ExpectedIntakeRate)
```

`ExpectedIntakeRate` is a configuration constant representing a typical good patch. A rich place scores near 1, a poor one near 0, and the value spans its range in normal operation instead of pinning to the ceiling.

That value is stored **on the place**, not on the resource kind, which is what lets a creature prefer one patch over another — the capability the roadmap calls "learned resource quality affecting future choices" and which no per-kind scalar can provide.

### Remembered places compete with visible ones, at a discount

Per the P2 learning phase, "utility scores may use remembered targets with lower confidence than current perception". A remembered place is scored with the same `ForagingEconomics.PatchScore`, using `LastKnownAmount` in place of observed amount, multiplied by `Confidence`.

Since confidence decays with time and with failed searches, a stale memory scores progressively worse than a fresh sighting without needing a separate rule. Acting on a stale memory and finding nothing is a visible, explainable mistake — a P2 exit requirement that the current code cannot produce.

### Threat memory is finally read

Threat entries contribute an avoidance term to every candidate destination:

```text
avoidance = Σ over remembered threats of
            FearResponse × Confidence × falloff(distance from candidate to threat)
```

subtracted from that destination's score. Timid lineages therefore develop no-go regions and their territory visibly differs from bold lineages' — divergence readable at population scale.

### Memory dies with the creature

Offspring inherit `MemoryCapacity`, `MemoryRetention`, `LearningRate`, and `Exploration` — nothing else. No remembered place and no learned value crosses generations. This is a hard rule from the P2 learning phase, and it exists to keep the genetic-selection evidence uncontaminated by cultural transmission.

## Components

| File | Change |
|---|---|
| `Core/SimulationTypes.cs` | `PlaceMemory` struct; `MemoryState` reduced to slot range plus counters |
| `Core/CreatureStore.cs` | Dense `PlaceMemory[]` sidecar with slot-range accessors |
| `Behavior/MemorySystem.cs` | Observe, evict, decay, learn outcome, query nearest by kind |
| `Behavior/ForagingEconomics.cs` | Score a remembered place; threat avoidance term |
| `Core/SimulationWorld.cs` | Write perceptions into memory; use corrected outcome formula |
| `Core/SimulationConfig.cs` | `MinimumMemorySlots`, `AdditionalMemorySlots`, `ExpectedIntakeRate` |

Gated behind the existing `CognitionEnabled` flag. No new flag.

## Testing

1. Capacity scales with `MemoryCapacity`: a high-capacity genome retains more places than a low-capacity one.
2. Eviction is deterministic: identical observation sequences produce identical ring contents.
3. Eviction removes the lowest-value entry, not the oldest.
4. Learned outcome spans its range: a rich place and a poor place produce materially different `OutcomeValue`, unlike today where both reach 1.0.
5. A creature prefers the higher-`OutcomeValue` of two remembered places at equal distance.
6. Confidence decays with elapsed time and drops sharply after a failed search.
7. A stale memory produces a wasted journey — a visible, explainable mistake.
8. Remembered threats reduce the score of destinations near them, scaled by `FearResponse`.
9. Offspring inherit cognition genes and zero remembered places.
10. Allocation guard: 100,000 observe-and-evict cycles allocate zero managed bytes.
11. With `CognitionEnabled = false`, behaviour is unchanged.

## Exit gate

- All eleven fixtures pass.
- The P2 exit gate becomes measurable for the first time: cognition improves reproductive output in a patterned environment and is neutral or harmful in a simple or rapidly changing one. It is not currently measurable, because the saturated learning signal carries no information.
- Memory cost per creature is fixed by configured capacity and independent of observation count.
