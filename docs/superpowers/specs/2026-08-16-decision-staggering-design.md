# Decision Staggering (B-8) - Design

## Problem

`docs/superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md`,
B-8: `IsDue(tick, DecisionsHz)` (`SimulationWorld.cs:471-475`) fires for the
entire population on the same tick — `SimulationWorld.cs:266` gates the
single call to `TickDecisions(nextTick)` on this, and `TickDecisions`
(`SimulationWorld.cs:623`) then loops every creature unconditionally.
Consequence: one O(population) cost spike every `interval` ticks
(`interval = BaseFrequencyHz / DecisionsHz`), and every creature reconsiders
in lockstep — visible as synchronized population-wide behavior changes.
Fix per the doc: phase-stagger by creature index.

## Fix

Add `SimulationConfig.DecisionStaggerEnabled` (bool, default `false`),
following this session's established flag-gating convention
(`PredationEconomicsEnabled`, etc.).

**When `false` (default):** `Step()` keeps today's exact gate —
`if (IsDue(nextTick, Config.Schedule.DecisionsHz)) { TickDecisions(nextTick); }`
— and `TickDecisions` processes every creature with no skip. Byte-identical
to current behavior; the new flag's `&&`-guarded inner check
(below) never triggers, so no separate legacy code path is needed inside
`TickDecisions` itself.

**When `true`:** `Step()` calls `TickDecisions(nextTick)` unconditionally
every tick (replacing the `IsDue` gate for this call only — all other
`IsDue`-gated systems are untouched). Inside `TickDecisions`'s per-creature
loop, the very first line becomes:

```csharp
int interval = Config.Schedule.BaseFrequencyHz / Config.Schedule.DecisionsHz;
for (int index = 0; index < Creatures.Count; index++)
{
    if (Config.DecisionStaggerEnabled && (tick + index) % interval != 0)
    {
        continue;
    }

    // ...existing per-creature decision body, unchanged...
}
```

This skips all perception/memory/decision work for a creature on ticks that
aren't its phase, so each creature still decides once every `interval`
ticks — same frequency as today — just on a different offset per creature,
spreading the cost across ticks instead of spiking on one.

## Hash safety

`ComputeStateHash` already includes `decision.DecisionTick`
(`SimulationWorld.cs:385`), so staggering genuinely changes which tick each
creature's decision was made on, and therefore changes the hash whenever
`DecisionStaggerEnabled` is `true`. This is why the flag exists and defaults
`false`: any existing recorded/frozen scenario never sets this flag, so its
hash is provably unaffected (the inner skip condition is `false &&
anything`, which never skips).

## Known limitation (accepted, not fixed here)

Phase is `index`-based, matching the defect doc's literal fix text
("phase-stagger by creature index"). `index` is not a stable creature
identity across a run — a death mid-run swap-removes and can reindex a
later creature, which can shift that creature's phase by one cycle at the
moment of the reindex. This is a cosmetic timing jitter only (affects which
tick a creature's next decision falls on, not the decision's correctness or
frequency over time) and is accepted as out of scope. An ID-based phase
(`Creatures.GetIdAt(index).Value % interval`) would avoid it entirely but
is not what the doc asked for and is unnecessary complexity for a cost/
synchronization smoothing fix — YAGNI.

## Testing

Extend the relevant `SimulationWorld`/`CoreSimulationTests`-style tests:
1. With `DecisionStaggerEnabled: true` and a population > `interval`, run
   enough ticks to observe that not every creature's `DecisionTick` updates
   on the same tick (staggering is actually happening).
2. With `DecisionStaggerEnabled: false` (default), a hash-regression check
   confirming output is unchanged from a pre-existing scenario's recorded
   hash (mirrors `CoreSimulationTests.cs`'s existing pattern from the B-5
   task).
