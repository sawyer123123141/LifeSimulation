# Soft Home-Range Affinity Design

**Status:** proposed for review  
**Date:** 2026-08-21

## Purpose

Make successful creatures develop recognisable, repeatable local routes without trapping them in a fixed territory or reviving the project's deliberately inert place-memory subsystem.

## Boundary decision

`MemorySystem.ObservePlace` and `PlaceMemory` stay unwired. Their production inertness is an explicit liveness contract and historical baseline assumption.

This feature instead adds a small dedicated `HomeRangeState` to creature state. It records one attraction centre and a bounded familiarity value; it is not resource memory, does not store resource kinds or site claims, and does not grant exclusive territory.

## State and learning

After a creature successfully consumes food, drinks water, or completes reproduction, its home-range centre moves toward its current position by an explicit fixed learning fraction and familiarity rises by an explicit fixed increment, clamped to `[0, 1]`. It decays slowly each needs tick. Newborns start with no familiarity and inherit no territory.

No random draw is introduced. Updates use existing deterministic tick order and preserve arithmetic order. State is included in `ComputeStateHash` only when the feature flag is enabled; flag-off keeps the previous hash/output byte-identical.

## Decision effect

Only after existing candidate scores are calculated, a candidate target receives a small affinity bonus based on its distance from the home centre and current familiarity. The bonus is capped below the existing resource/need score range and is applied only when all of these are true:

- `homeRangeAffinityEnabled` is true;
- the creature has familiarity above zero; and
- the intent is ordinary foraging or drinking.

No bonus applies to fleeing, active threat avoidance, mating intent, no-resource fallback movement, or a target that fails existing availability/visibility checks. Existing danger, scarcity, urgency, mating, and exploration mechanisms therefore remain able to win; home range breaks only otherwise-close choices.

## Configuration

Add `HomeRangeAffinityEnabled` plus explicit constants to `SimulationConfig`. Default factories keep it false. A focused P4a scenario may enable it later, but this change adds no default-scenario behavior change.

## Verification

- Flag-off paired worlds retain state-hash equality over fixed seeds/ticks.
- A successful food/drink/reproduction event updates only the successful creature's home range deterministically; birth starts blank.
- Equal viable food targets select the one nearer the familiar centre when enabled.
- Greater need, danger/fleeing, unavailable targets, and mating do not receive the affinity bonus.
- Replaying the same seed/config/ticks produces identical centre/familiarity/decisions.
- Existing `PlaceMemoryProbesRunButNeverTakeEffect` stays green.

## Non-goals

- territory ownership, packs, follower objects, inherited territories, resource claiming, pathfinding/navigation, and direct UI rendering;
- changing the genetic model or declaring affinity an evolved trait in this slice; and
- clustered-resource generation or juvenile local-area bias.

## Decision requested

Approve the dedicated, flag-gated, non-genetic affinity state and its narrow foraging/drinking tie-break role. It is deliberately conservative: recognisable routes first, ecological claims only after measurement.
