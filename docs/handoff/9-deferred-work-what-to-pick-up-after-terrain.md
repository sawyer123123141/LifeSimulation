## 9. Deferred work — what to pick up after terrain

Written at the point of switching to P6 terrain groundwork. **The tree was clean at this point:**
nothing uncommitted, no `ZZZ*` probes, no `TODO`/`FIXME`/`NotImplementedException` anywhere in
`Assets/Scripts`, nothing unpushed, 499 / 19 / 33 / 1 green, Unity compile clean. Nothing below is
half-built — these are *unstarted* items, not loose ends.

### A. Small, finishable in a session each

1. **Resource depletion/recovery feedback** (P4a visible-feedback item). A player cannot currently
   see a patch being grazed down and regrowing. Presentation-side.
2. **Lineage display** (same roadmap line). Parents are shown as bare ids in the inspector;
   generation and descent are not visible.
3. **Two-parent reproduction** — named on the rendezvous roadmap line and never built. Note the
   rendezvous *gate* is closed as "works, buys nothing"; two-parent reproduction is a separate,
   still-open idea and should not be justified by the gate.

### B. Needs a human in Play mode — I cannot verify these headlessly

4. **Breeding-readiness inspector** (`15c7a5a`) and **selected-creature history panel** (`32900de`).
   Both compile and are covered by tests at the model level; **neither has ever been seen rendered.**
   The inspector is at full height (324px) with all optional trait rows showing, which is why the
   history went in its own panel at (464, 300) rather than lengthening it. Someone should press `V`,
   click a creature, and confirm both panels look right and do not overlap.

### C. Measurement debts — real, and each has a stated reason it is still open

5. **A carrying-capacity-limited habitat.** This is the one that matters most. Every plant trait
   result on record was measured with the herbivore population **pinned at 48**. Raising the cap does
   **not** free it — it produces boom-and-collapse (`p4-cap-pinning-audit-2026-08-22.md`). Until a
   habitat limited by carrying capacity rather than by a cap exists, the whole plant corpus carries a
   scope qualification. This is scenario design, not an audit.
6. **Lifespan headroom: not adjudicated.** The available control (`mortality-off`) is confounded —
   it also moved `Dispersal` +0.0834 (t +21.40), `NutrientUptake` −0.0466 and `WaterEfficiency`
   −0.0445. Needs a lifespan-specific control that does not exist.
7. **Per-tick resource request counts were never instrumented.** The do-not-optimise decision uses
   population as an upper bound — sound for declining to optimise, but it is a bound, not an
   attribution. Only needed if someone wants allocation reported as a *share* of tick time.

### D. Closed — do not reopen, and do not spend a session rediscovering why

8. **Soft home-range affinity** — closed as a measured negative. The **sign** is wrong, not the size.
9. **Safety-gated rendezvous** — closed as "works, buys nothing". Right sign, well powered
   (predation deaths t −4.64), but starvation limits the population. Reopen only in a
   predation-limited habitat.
10. **The three low-occupancy plant conclusions** — permanently unverifiable, banners stay. **Do not
    attempt a fourth reconstruction.**
11. **Place memory stays inert.** Never wire `MemorySystem.ObservePlace`.

### D2. Caves and rivers — asked for, and blocked on architecture

**Read `docs/terrain-caves-and-rivers.md`.** Both were requested; neither is a coefficient.

- **`PlanetTerrain.Sample` is a pure function of one direction.** Rivers and erosion depend on the
  ground *uphill*, which is a computation over the surface, not at a point. They need a pass over a
  finite region, which is the same chunk machinery adaptive level of detail needs.
- **A heightfield stores one surface per direction**, so it cannot express an overhang at any setting.
  Caves need a volumetric density field and marching cubes, with density seeded as
  `surfaceElevation - height` so the zero isosurface reproduces exactly the terrain that exists now.

Order: the join, then painted rivers (additive, cheap, no new machinery), then chunked regions, then
flow accumulation and erosion on that machinery, then caves. **Keep generation a pure function of
position and settings** - the probe, the statistics instrument and headless determinism all depend on
it, and it is why terrain could be iterated fifteen times without re-measuring anything.

### E. Large

12. **P5 — species and history.** Genetic distance, lineage ids, cluster history and the P5 panel
    already exist. Species clustering proper, the evolutionary tree, extinct branches and the
    historical timeline do not. This is the big remaining phase.

### Terrain-specific note

When terrain makes temperature genuinely vary, **`LivenessTests` will fail on
`plantTemperatureAdaptationEnabled`**. That flag is pinned inert *only* because `EnvironmentField`
returns Temperature = 1.0 everywhere and the adaptation expression collapses to the raw value at 1.0.
The failure is the **designed signal** — the same thing happened when the procedural fields landed.
Move the flag out of `LivenessTests.KnownInertFlags`; do not "fix" the test.

---
