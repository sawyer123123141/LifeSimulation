# Session Handoff — 2026-08-22 (updated)

The complete architecture, scientific context, testing rules and preferences brief is still:

`docs/CLAUDE_HANDOFF_2026-08-22.md`

Read that after this file. `docs/ROADMAP.md` is the backlog. `docs/superpowers/plans/` is an
archive, not a backlog.

## Current state

Head is on `main`. Completed and pushed this session:

1. **Key `R`** — matched Play-mode home-range scenario: `ObservationStable` with the same seed and
   config as `5` except `HomeRangeAffinityEnabled=true`. `5`, `6`, `7`, `9`, `N`, the factory
   defaults and the inert place-memory system are unchanged.
2. **P5 panel continuity filter** — `P5HistoryPanelSession` exposes `NotableEventCount`,
   `GetNotableEventAt` and `HiddenRoutineContinuityCount`. Routine confirmed continuity is kept in
   the analytical history but hidden from the eight visible rows, which now carry
   "(N routine continuities hidden)". `Unresolved` continuity is not routine and stays visible.
3. **Soft home-range affinity is CLOSED as a measured negative.** Two experiments, five conditions,
   240 fixed-seed runs. It does not create routes; on purpose-built route-capable geometry it made
   routes *less* repeatable (t -2.87, 8/30 up) while increasing same-site clinging (t +4.93, 26/30
   up). The flag stays default `false`, the implementation and tests stay in the tree, and the
   design spec and plan carry SUPERSEDED banners. **Do not tune the constants or reopen it.**
4. **`Prototype4Scenarios.ObservationRouteRing`** — new scenario data: eight sites on a radius-8
   ring alternating Food and Water (adjacent opposite-kind separation 6.12, same-kind 11.31,
   founders at the centre, capacity and regeneration matched to `ObservationStable`). It is the
   only geometry in the repository in which a food/water route can physically exist, and it
   delivers 90.6% decision opportunity. It is **not** a survival calibration: 11/30 and 9/30 seeds
   go extinct.

Experiment writeups and per-seed CSVs are in `docs/experiments/`:
`p4a-home-range-affinity-2026-08-22.md`, `p4a-home-range-bonus-sensitivity-2026-08-22.csv`,
`p4a-route-ring-home-range-2026-08-22.md`.

## Next task

`docs/ROADMAP.md` P4a, next unfinished item: **clustered, changing plant/resource patches so travel
creates recognizable routes rather than unstructured wandering.** `ObservationRouteRing` is the
harness to build on. Write a new spec/plan in `docs/superpowers/` before implementing, following
the existing format.

Two live constraints carried forward from this session's measurements:

- Splitting a fixed productivity across more sites materially raises extinction risk. Any clustered
  patch design must report survival columns per arm, and must not be judged on the ring's
  calibration.
- Do not attempt to recover route behavior with another proximity-to-recent-success term. If route
  behavior is wanted, it needs a mechanism that scores a *pair* of complementary resources, or a
  need-anticipation term that begins travel before the need is urgent. That is a design decision
  worth raising with the user before building.

## Working-tree rules

Intentionally untracked: Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. Do not stage or delete them. Add named files only;
never `git add -A`. Delete `Assets/Tests/EditMode/ZZZ*.cs` probes before committing.

## Verification workflow

From `tools/HeadlessTests`: `dotnet build`, then the non-liveness shard, `PlantLivenessTests`,
liveness excluding `RiskAversionIsLiveOnlyWhenThreatsExist`, then that test alone. Presentation
changes additionally need a Unity 6000.2.14f1 batch compile; the headless project does not compile
`Assets/Scripts/Presentation`.
