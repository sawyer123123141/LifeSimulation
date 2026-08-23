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

## Current state — evidence-integrity audit (2026-08-22)

The P4a/P5 queue is **paused** for an evidence-integrity audit prompted by a deep code review. Three
review findings were independently confirmed on `main` and handled in order, tests first. The
review's blanket claim that all evidence is unreliable was **not** accepted, and the paired audit
shows why it would have been wrong.

Done and pushed:

1. `4cc9a47` — `PlantPatchStore.ReplaceAt` now resets plant age. Takeovers previously installed
   seedlings carrying the incumbent's age, killed on the dead patch's clock. Two failing tests
   first.
2. `9763374` — statistics are sampled after the tick's deaths are committed, and
   `SimulationWorld.CaptureStatistics()` gives an explicit end-of-run snapshot that
   `ExperimentRunner` now uses. Five failing tests first.
3. `docs/superpowers/specs/2026-08-22-state-fingerprint-design.md` — design only, deliberately not
   implemented. Three separate hashes (frozen V1, versioned complete V2 including config,
   config-free BehaviorHash), plus an omissions audit that found `_birthOrdinal` and
   `_plantSeedOrdinal` — two RNG stream counters the review did not name.
4. `docs/experiments/evidence-impact-audit-2026-08-22.md` — paired old-versus-fixed sweep, 85 runs,
   pre-fix worktree versus fixed main.

**Audit outcome.** With `PlantSiteCompetitionEnabled` off, **trajectories and route metrics are
identical** (state hashes match in every arm) while the statistics fix corrected at least one final
death count without changing any trajectory. Home-range, route-ring and shifting-patch conclusions
are cleared outright, no banner needed.
Only the competition path moved (30/30 hashes), where `SeedlingResilience` drifts **down** (t -1.99)
and plant generations fall by one (t -2.63). Three establishment/competition experiments carry
**requires re-measurement** banners — not retractions, because those drift magnitudes do not
overturn a t +4.03 selection result measured under standing variance, and not clearance either,
because they move the mechanism those conclusions rest on in the unfavourable direction. Original
files are preserved unedited.

## Next task

**The evidence audit and the whole P1 queue are complete.**

P1 outcomes:

1. **Finite/range validation** (`c751a13`) - `SimulationConfig.Validate()` requires all ten float
   tuning values finite; `ResourceDefinition` rejects non-finite geometry and amounts at
   construction. Clamping is not a NaN filter, which is why the boundary is the only cheap place.
2. **Experiment manifest/provenance** (`b8a61a7`) - `ExperimentManifest` + `ExperimentCsv`, which
   **refuses to compose a CSV without provenance**, plus
   `SimulationScenario.ComputeLayoutFingerprint()`. **Use these for every new experiment CSV.**
3. **Genetic distance** (`d3fac12`) - measured at a constant 240 bytes per pair (120 MB and 126 ms
   per observation at 1,000 creatures), fixed to flatten traits once per snapshot: 1,151x less
   allocation, 2.5x faster.
4. **Resource allocation** - benchmarked and **deliberately not optimised**. Cost is
   O(requests x distinct resources), so crowding is the *cheap* case, and a 12,000-tick run at 523
   creatures takes 2.72 s end to end. Optimising would risk a deterministic path for an
   unmeasurable gain. See `docs/experiments/p1-resource-allocation-benchmark-2026-08-22.md`.

Remaining, in order:

1. **State fingerprint V2** - designed, unimplemented:
   `docs/superpowers/specs/2026-08-22-state-fingerprint-design.md`. Three hashes with three jobs;
   the acceptance criterion that matters is that `FlagLivenessAnalysis` must still report exactly
   the known inert set afterwards.
2. **Resume the ROADMAP.** Next unfinished P4a items are the selected-creature action/history
   feedback and reassessing safety-gated rendezvous (its first ecological experiment was null - do
   not build pack architecture to force it).
3. Treat dense-index scheduling, stale grids, defense projection and Legacy predation as measured or
   design questions, not automatic fixes. The stale-grid question is already scoped by the
   fingerprint design's settled-tick validity rule.

Do **not** attempt a fourth reconstruction of the 168-site condition. If free-site abundance matters
scientifically, re-derive it as a new experiment with a committed scenario, in a geometry that fits
inside the +/-25 grazed arena.

**Unverified by me:** the breeding-readiness inspector UI (`15c7a5a`) compiles and passes tests but
was never seen in Play mode; its layout at 324px with all optional trait rows showing is untested.

## Working-tree rules

Intentionally untracked: Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. Do not stage or delete them. Add named files only;
never `git add -A`. Delete `Assets/Tests/EditMode/ZZZ*.cs` probes before committing.

## Verification workflow

From `tools/HeadlessTests`: `dotnet build`, then the non-liveness shard, `PlantLivenessTests`,
liveness excluding `RiskAversionIsLiveOnlyWhenThreatsExist`, then that test alone. Presentation
changes additionally need a Unity 6000.2.14f1 batch compile; the headless project does not compile
`Assets/Scripts/Presentation`.
