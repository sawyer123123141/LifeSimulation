# Multi-Threat Perception for IntentUtilityV1 (C-3) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
C-3: `PerceptionSystem.FindNearestOtherCreature` returns a single
`CreatureObservation`. Not a defect — P1 specifies "nearest viable prey,
threats... " in the singular, so the code matches its own spec — but it
structurally blocks any future group behavior (mate choice among
candidates, multi-threat assessment, herding) that needs a top-K query.
Nothing downstream of C-3 is built yet either, so this task picks the one
concrete consumer that already exists in this codebase this session:
`IntentUtilityV1`'s predation scoring (`DecisionSystem.ScorePredation`),
currently reacting to only the single nearest other creature.

No phase of `docs/superpowers/plans/2026-08-12-p0-p7-program-plan.md`
specifies how multiple simultaneous threats should combine into one
score — P1's utility terms (expected energy reward, injury risk, escape
probability, opportunity cost) describe evaluating a single perceived
target, not aggregating several. In the absence of a specified formula,
this design uses **best-of-K per action**: score each visible creature
independently with today's exact per-target formula, then keep only the
single best flee candidate and single best hunt candidate. This reduces to
today's exact math when only one creature is visible, avoiding an invented,
unvalidated aggregate-danger formula the docs never asked for.

## Fix

### 1. `PerceptionSystem` - new top-K creature query

Add a `CreatureCandidateBuffer` struct mirroring the existing
`ResourceCandidateBuffer` (`PerceptionSystem.cs:42-111`) exactly — same
`Capacity = 4`, same fixed-field top-K insertion-sort shape (`Consider`,
`GetAt`, `Count`) — except holding `CreatureObservation` instead of
`ResourceObservation`, ordered by `Distance` (tie-broken by `CreatureId`,
mirroring `ResourceCandidateBuffer.IsBefore`'s tie-break by `ResourceId`).

Add `PerceptionSystem.FindOtherCreatures(CreatureStore creatures,
UniformGrid creatureGrid, SimVector2 origin, float visionRange,
CreatureId excludedCreatureId, ref CreatureCandidateBuffer candidates)`,
reusing the exact same grid-scan loop `FindNearestOtherCreature`
(`PerceptionSystem.cs:152-213`) already uses, except calling
`candidates.Consider(...)` for every in-range creature instead of tracking
only the single best. `FindNearestOtherCreature` itself is NOT modified —
`Legacy` continues to call it exactly as today.

### 2. `DecisionSystem` - scoring against up to 4 candidates

Add a `PredationCandidateBuffer` struct (new, in `DecisionSystem.cs`)
holding up to 4 `(CreatureObservation Observation, Phenotype Phenotype)`
pairs with `Count`/`GetAt`/`Add` — this exists because `DecisionSystem` is
a pure scoring module with no `CreatureStore` access (it already receives
a single pre-resolved `Phenotype otherPhenotype` from its caller today, at
`SimulationWorld.cs`'s `DecideIntentUtilityV1` call site); the caller
resolves each candidate's `Phenotype` via `Creatures.GetPhenotypeAt(...)`
before building this buffer, extending the exact pattern already used for
the single-candidate case (`other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default`).

Add `ScorePredationMulti(CreatureNeeds needs, Genome genome, Phenotype self,
PredationCandidateBuffer others, ref DecisionCandidateBuffer candidates,
bool economicsEnabled, out float fleeScore, out float huntScore)`. For each
candidate `i` in `others` (0 to `others.Count - 1`), compute the exact
per-target formula `ScorePredation` (`DecisionSystem.cs:428-460`) already
uses — `distanceAvailability`, `PredationSystem.Threat(candidatePhenotype,
self, distance, economicsEnabled)` for that candidate's flee contribution,
`PredationSystem.HuntCapability(self, candidatePhenotype, distance,
economicsEnabled)` for that candidate's hunt contribution — and track the
single highest-scoring flee candidate and single highest-scoring hunt
candidate across the loop (each independently, since fleeing one creature
and hunting another are not mutually exclusive intents to compare, only to
select-best-of-each). After the loop, add at most one `Flee` and one
`SeekPrey` `DecisionCandidate` (same `>= 0.10` threshold as today), each
carrying its own best candidate's `CreatureId` as `targetCreatureId`.

`DecideIntentUtilityV1` (both overloads) gains two new trailing optional
parameters: `PredationCandidateBuffer otherCandidates = default` and `bool
multiThreatPerceptionEnabled = false`. Inside the `predationEnabled` block,
branch on the new flag: `false` (default) calls today's exact `ScorePredation`
with the existing single `otherPhenotype`/`threat`/`threatIntensity`
parameters, completely unchanged; `true` calls `ScorePredationMulti` with
`otherCandidates` instead.

### 3. `SimulationConfig` - new flag

`MultiThreatPerceptionEnabled` (bool, default `false`), added the same way
as `PredationEconomicsEnabled`/`DecisionStaggerEnabled` before it.

### 4. `SimulationWorld.cs` - call site

Inside `TickDecisions`'s `IntentUtilityV1` branch (`SimulationWorld.cs:657-670`),
branch on `Config.MultiThreatPerceptionEnabled`:
- `false` (default): keep today's exact `FindNearestOtherCreature` +
  single `other`/`threatIntensity` computation, completely unchanged.
- `true`: call `PerceptionSystem.FindOtherCreatures` into a
  `CreatureCandidateBuffer`, then build a `PredationCandidateBuffer` by
  resolving each candidate's `Phenotype` via `Creatures.GetPhenotypeAt`,
  and pass that (plus `multiThreatPerceptionEnabled: true`) into
  `DecideIntentUtilityV1` instead of the single `other`/`threatIntensity`
  arguments.

## Hash safety

When `MultiThreatPerceptionEnabled` is `false`, `SimulationWorld.cs`'s call
site is unchanged, `DecideIntentUtilityV1` takes the `false`-branch
(today's exact `ScorePredation` call, unchanged), and `ScorePredationMulti`
is never invoked — behavior and hash are byte-identical to today. This is
proven by a hash-regression test, following this session's established
methodology (throwaway worktree at the pre-change commit,
`ComputeStateHash()` captured and pinned).

## Testing

1. `PerceptionSystem` unit tests: `FindOtherCreatures` returns up to 4
   creatures within vision range, ordered nearest-first, excludes the
   observer's own id, matches `FindNearestOtherCreature`'s result as its
   first (`GetAt(0)`) entry for an identical scenario.
2. `DecisionSystem` unit test: with `multiThreatPerceptionEnabled: true`
   and 2+ visible creatures of differing favorability, the winning
   `SeekPrey`/`Flee` candidate corresponds to the best-of-K target, not
   necessarily the nearest one (proves best-of-K actually selects across
   candidates, not just the first).
3. Hash-regression test with the flag `false` (default), mirroring the
   established pattern from this session's B-5/B-8 tasks.
