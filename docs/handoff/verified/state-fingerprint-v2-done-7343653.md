### State fingerprint V2 (DONE — `7343653`)

Three hashes, three jobs, and they must stay separate:

| | job | includes configuration? |
|---|---|---|
| `ComputeStateHash` (V1) | frozen historical identifier; tests pin its literals | only `WorldSeed` |
| `ComputeStateFingerprint` (V2) | "will these two worlds evolve identically from here?" | **yes, all of it** |
| `ComputeBehaviorHash` | did this gene/flag reach behavior? | **no, and never** |

V2 adds, over V1: the config hash, `_birthOrdinal`, `_plantSeedOrdinal`, the three store id
counters, `PlantSiteRegistry` contents and order, plant `Age` and `ReproductionCooldownRemaining`,
and home-range state **unconditionally** rather than behind its flag. Guarded to a settled step
boundary, like `CaptureStatistics`. Excluded on purpose: reporting accumulators, liveness counters,
derived caches.

`BehaviorHash` also gained plant `Age` and `ReproductionCooldownRemaining` — decided by measurement,
not argument. Prediction stated first (the inert set would not move, since all four inert flags are
inert for a *reachability* reason); measured with the lines in and out: **identical inert set,
identical plant gene verdicts, 33 / 19 / 1 either way**. No `BehaviorHash` value is pinned as a
literal anywhere, so extending it invalidated no baseline.

Config hash covers **44 of 46** public `SimulationConfig` properties; `FixedDeltaTime` and
`MaximumMemorySlots` are derived from inputs already hashed. Two drift guards: every `bool`
constructor parameter must move the hash, and the property count is pinned.

**Green: 489 / 19 / 33 / 1**, up from 480 / 19 / 33 / 1. The three liveness counts being unchanged
*was* the acceptance criterion.

---
