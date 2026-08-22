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

5. **`Prototype4Scenarios.ObservationShiftingPatches`** — new scenario data (three clusters, each
   with a permanent central water site, two active food sites 7 units out, and four dormant food
   sites as dispersal targets) plus a 120-run measurement. With `plantMortalityEnabled: true` the
   existing plant dispersal/mortality machinery produces ~29 patch deaths and ~33 establishments
   per run: route permanence falls (pair repeat -0.0935, t -6.47) and distinct routes per creature
   rise 27% (+0.628, t +4.48) at no survival cost. No new mechanism was needed or added.
   Bound to Play key **`V`** at world seed 45: the scenario's honest extinction rate is 6/30 and
   seed 42 is one of the six failures, so the demo seed is a documented demonstration choice and no
   published statistic rests on it.

Experiment writeups and per-seed CSVs are in `docs/experiments/`:
`p4a-home-range-affinity-2026-08-22.md`, `p4a-home-range-bonus-sensitivity-2026-08-22.csv`,
`p4a-route-ring-home-range-2026-08-22.md`, `p4a-shifting-patches-2026-08-22.md`. The clustered-patch
design spec is `docs/superpowers/specs/2026-08-22-clustered-changing-resource-patches-design.md`.

## Next task

**There is an open human design decision blocking the cleanest next step.** See
`docs/experiments/p4a-founder-mortality-2026-08-22.md`.

`ReproductionSystem.CanReproduce` requires energy AND hydration AND health each at or above 70% of
capacity. Adults meet that joint gate 95.0% of the time when food and water are co-located and only
33.5% when food sits 7 units from water. That makes **every spatially separated world systematically
sub-fertile** — which is why `ObservationShiftingPatches` and `ObservationRouteRing` go extinct in
6/30 and 11/30 seeds while `ObservationStable` never does. Nothing starves and nothing dehydrates;
all four founders die of age at tick ~2500 in every arm. Extinction is failure to replace.

The three options are keep-as-realism (status quo, costs nothing), decouple the thresholds, or gate
on a rolling average. **Do not change `CanReproduce` without an explicit decision:** it is core
biology on every scenario's hot path and changing it invalidates every population baseline, survival
calibration and plant-selection result on record. Ask the user.

Work that does not depend on that decision, in order:

1. **Selected-creature action/history feedback** — only where it helps a person distinguish
   foraging, drinking, mating, fleeing, resting, births, deaths, depletion and recovery. Fully
   independent of the reproduction question.
2. **Reassess safety-gated rendezvous.** Its first ecological experiment was null; do not build pack
   architecture to force it.
3. P5: durable chunk storage and a graphical evolutionary tree, and a behaviour-preserving
   decomposition of the ~1,310-line `GeneticClusterHistory.cs` before adding classification logic.

Do **not** build the "optional juvenile local-area bias" as a fix for separated-resource extinction
— juveniles are not the failing class and mortality is not the failure mode. Do not reopen soft
home-range affinity; it is closed as a measured negative. Do not run another placement or
productivity calibration on `ObservationShiftingPatches`; six variants are already recorded and the
joint gate explains why all of them failed.

## Working-tree rules

Intentionally untracked: Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. Do not stage or delete them. Add named files only;
never `git add -A`. Delete `Assets/Tests/EditMode/ZZZ*.cs` probes before committing.

## Verification workflow

From `tools/HeadlessTests`: `dotnet build`, then the non-liveness shard, `PlantLivenessTests`,
liveness excluding `RiskAversionIsLiveOnlyWhenThreatsExist`, then that test alone. Presentation
changes additionally need a Unity 6000.2.14f1 batch compile; the headless project does not compile
`Assets/Scripts/Presentation`.
