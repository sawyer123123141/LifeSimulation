# Reference implementations worth reading

**Started 2026-08-23**, after the terrain work established the lesson the hard way: fifteen rounds of
first-principles tuning produced six wrong diagnoses, and twenty minutes reading a working
implementation produced the architectural answer. **Read the reference before writing the system, not
after.**

Primary source so far: **Sebastian Lague** — videos plus source on GitHub. Already used, and it found
the defect that instrumentation had missed.

---

## Directly on the roadmap

### Hydraulic erosion — **highest value, and it is already specified as T2**

`docs/superpowers/specs/2026-08-14-world-generation-design.md` lists T2 as "derived networks: rivers,
lakes, coastlines, shorelines" from flow accumulation, with erosion as a later pass. This is the
single biggest remaining "this was *caused*, not generated" win for terrain: water-carved valleys
read as history rather than as noise, and rivers, lakes and shorelines all fall out of the same
accumulation pass.

It also feeds the ecology rather than only the visuals — rivers are water resources, and valley
floors are where fertility concentrates.

**Constraint to respect:** an erosion pass is iterative and stateful. If it ever moves into
`Assets/Scripts/Simulation` it must be deterministic and order-independent, or it must be precomputed
into a field the simulation only reads. Prototype it in Presentation first, like `PlanetTerrain`.

### Planet generation — **already used**

`SebLague/Procedural-Planets`, read from source. Gave us signed elevation, layer masking
(`useFirstLayerAsMask`), and per-layer `max(0, v - minValue)` instead of a global squash. See
`docs/terrain-brainstorm-2-2026-08-23.md`.

Still unused from it: its colour generator drives a gradient from measured elevation min/max per
planet, which is a cleaner idea than our fixed `HighGround` constant.

### Ecosystem simulation — **read for presentation, not for mechanism**

Worth being precise about what transfers. This project's ecology is already far more developed: real
genomes, mutation, selection, deterministic RNG domains, 499 tests, and an experimental method with
manipulation checks and pre-registered predictions. The mechanism does **not** need replacing.

What is likely useful: how it is **shown** — legible creature state, cause visible at a glance, the
population/statistics presentation. That is exactly the open P4a item (selected-creature history,
resource depletion feedback, lineage display).

**Do not** import its behaviour model. Ours is measured; swapping mechanisms would invalidate the
recorded corpus.

### Pathfinding — **blocked until terrain affects movement**

Currently movement is direct seeking on a flat plane, and elevation is cosmetic. Pathfinding only
becomes meaningful once terrain obstructs — which is the "elevation affects movement" change, itself
a simulation change needing a flag, tests and a re-measure. Relevant later, not now.

Note it is also a prerequisite for caves being interesting, since caves are the case where the route
is not a straight line.

---

## Further out

### Atmosphere

T6/T7 visual work. Nothing depends on it and it changes no simulation result. Good "make it look
alive" work once the surface is settled.

### Solar system

Mostly out of scope, with one real hook: **day/night and seasons**. `EnvironmentField` already has a
latitude-driven temperature term, and an axial tilt with an orbit would turn that into a seasonal
cycle — which is a genuine ecological driver, not decoration, and would give plants a reason to have
dormancy.

Would need care: anything time-varying that the simulation reads must be a pure function of tick, not
of wall-clock, to preserve determinism.

---

## How to use this list

1. **Read the source, not only the video.** The video explains intent; the source shows the
   composition order, the masks and the clamps, which is where our defects were.
2. **Prototype in Presentation first.** `PlanetTerrain`, `PlateStructure` and `IcoSphere` all live
   there and cost nothing to iterate on, because no hash depends on them.
3. **Promotion into `Assets/Scripts/Simulation` is a separate, deliberate step**: new flag defaulting
   false, prove flag-off byte-identical, then re-measure every result scoped to the old behaviour.
4. **Anything imported must survive the determinism rules**: pure functions of position/tick/seed, no
   wall-clock, no threading, no call-order dependence.
