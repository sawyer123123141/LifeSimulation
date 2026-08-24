# Caves and rivers — what they need, before anything blocks them

**Status: not started, deliberately.** Written now because both have an architectural requirement
that the current design does not meet, and the terrain/ecology join is about to fix the shape of that
design. A note written after the join would be a note about a corner already painted into.

**Neither may disturb what exists.** Elevation as signed displacement, plate blending, the two-
neighbour crossfade, the band amplitudes and the single mesh path are all settled and measured. Both
features below are **additive**: a new term or a new pass, gated behind its own flag, with the field
unchanged when the flag is off.

---

## The constraint both share

`PlanetTerrain.Sample` is a **pure function of one direction**. Given a point it returns that point's
elevation, and it reads nothing about any neighbour. That is what makes it cheap, seamless, resolution
independent and trivially parallel, and it is why the whole generator can be sampled at any zoom.

**A river cannot be written that way, and neither can erosion.** Where water flows depends on where it
came from, which depends on the ground uphill of it. That is a computation *over the surface*, not at
a point. Same for hydraulic erosion — a droplet carries sediment from one cell to the next.

**A cave cannot be written that way either, for a different reason.** A heightfield stores one surface
per direction, by definition. A cave needs two: a roof and a floor. No amount of tuning a height
function produces an overhang, because the type does not have room for one.

So: **rivers need a pass, caves need a different representation.** Neither is a coefficient.

---

## Rivers

Two options, and they are not alternatives — the first is a stepping stone.

### R1. Painted rivers - **DONE, 2026-08-23** (`RiverNetwork`)

Pick river sources on high ground, walk downhill sampling the existing field, and record the path.
Carve a channel into elevation near the path, and raise moisture beside it. The walk happens once per
world, so it costs nothing per sample; sampling becomes "elevation, minus the channel if this point is
near a recorded path".

- **Honest about what it is:** the rivers follow the terrain but do not change it elsewhere, so no
  valleys form around them. They will look painted on, because they are.
- Wants a spatial index on the paths, or every sample tests every river.
- Fits the current architecture with no changes at all. Probably one session.

**As built.** 2,048 candidate sources on a spiral over the sphere, the highest kept subject to a
minimum separation of 0.12 radians so rivers scatter over the continents instead of crowding one
range. Steepest descent in 0.0025-radian steps (1.25 m), up to 400 steps, **against a deliberately
coarse field** (`WalkFrequency = 24`) - a walk that sees every micro bump stops in the first hollow.
A path that stalls inland is **discarded**, not drained: an inland lake needs a water surface, which
is a different feature.

At seed 42: **16 of 48 rivers reach the sea, 716 segments, 255 ms** to build. Segments, not points -
measuring to points alone left the channel floor scalloped at proximity 0.999 / 0.848 / 0.999 along
one straight reach, because between two path points the nearest-point distance rises.

Channel: **0.055 elevation units deep (about 1.7 m), 5 m wide at the mouth** narrowing upstream,
smoothstepped so the banks meet the surrounding ground with matching slope. It fades out as the
ground approaches sea level, or a river mouth becomes a notch in the coastline seen from orbit.
Moisture is lifted 0.30 at the channel.

**Carving alone was invisible and the render proved it.** A metre-deep cut in ground whose relief
runs to tens of metres showed one faint line in a 200-unit view and nothing at all in the arena. The
water surface is the cue: `PlanetSample` now carries `Channel`, and `PlanetBiome.Shade` blends toward
river blue. With that the river reads clearly in the 200-unit view and clips the arena's corner.

- **Sampling without a `RiverNetwork` is bit-for-bit the old sample**, so every non-river caller is
  unaffected - the backdrop globe is deliberately not given one, since its triangles are 19 units
  across and would miss every channel.
- **Four tests** (`RiverNetworkTests`): same seed walks the same rivers, rivers reach the sea, ground
  away from a channel is untouched, and a channel cuts down and never up.
- The simulation sees rivers too when the terrain join is on - **it moved no plant result**, which is
  expected: channels touch about 2.4% of an arena window.

### R2. Flow accumulation (the real thing)

Build a grid or mesh over the region, compute flow direction per cell downhill, accumulate upstream
area, and call cells above a threshold rivers. This is the standard approach and it is also **exactly
the machinery hydraulic erosion needs**, which is why it is worth doing properly rather than twice.

- Needs a **finite region** with a resolution — the opposite of the current sample-anywhere design.
  The generator stays as it is; this becomes a *layer over* it, computed per region and cached.
- Depressions must be filled or flow gets stuck in pits, which is its own well-known step.
- Rivers must reach the sea, which means the region has to include coast or the paths dead-end.

**Recommendation: R1 first**, because it gets rivers visible and tells us whether they read at all at
this scale, and because R2's real prerequisite is the region/chunk system that adaptive level of
detail also needs (T6). Do not build R2 before chunking exists; it would be built twice.

---

## Caves

A cave is a **volumetric** question: for each point in space, is it rock or air? That is a 3D density
field meshed with marching cubes, not a heightfield.

The workable shape:

- Density starts as `surfaceElevation - height` — negative above ground, positive below — so the
  isosurface at zero **is exactly the terrain that already exists**. That is the property that keeps
  this additive: with no cave term, the volumetric mesh reproduces the current surface.
- Cave systems subtract from density: worm-like tunnels (a walk with a radius), or 3D ridged noise
  thresholded into connected passages. Only where they surface do they change the visible ground.
- Meshing is per **chunk**, which is the same region system R2 and level of detail want.

**Cost is the honest problem.** Marching cubes over a volume is orders of magnitude more work than
sampling a heightfield, and the arena is currently a 193x193 grid. Caves are a chunked-world feature,
not a feature of the present renderer.

**And they need a reason to exist in the simulation**, or they are scenery: shelter from predators, a
temperature refuge, a place a creature can be hidden in. That is an ecology decision, not a terrain
one, and it should be made before the meshing work rather than after.

---

## Order, and what has to exist first

1. **The terrain/ecology join** — in progress. Terrain has to mean something before adding more of it.
2. **R1 painted rivers** — additive, cheap, tells us whether rivers read at this scale.
3. **Chunked regions with level of detail (T6)** — the shared prerequisite. Zooming the planet still
   adds no detail; subdivision is fixed at 5.
4. **R2 flow accumulation**, then hydraulic erosion on the same machinery.
5. **Caves**, once chunks exist and once there is an ecological reason for one.

**The thing to protect:** keep generation a pure function of position and settings. Every item above
is a *layer over* that function or a *pass beside* it. The moment generation starts depending on
mutable neighbour state, the probe, the statistics instrument and headless determinism all stop
working, and the reason terrain could be iterated fifteen times without re-measuring anything goes
away.
