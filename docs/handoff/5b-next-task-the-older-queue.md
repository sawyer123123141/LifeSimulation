## 5b. Next task (the older queue)

**Step three of the join is DONE and its answer is negative (`88a236b`).** 480 runs, 120 seeds,
12,000 ticks, flat versus terrain-driven crossed with the establishment contest. Full writeup in
`docs/experiments/p4-terrain-join-2026-08-23.md`; the instrument is `tools/PlantSweep`, committed.

**No plant conclusion moves.** Paired terrain-minus-flat per seed, contest-on: `WaterEfficiency`
-0.0233 (t -2.72) is the only cell past |t| = 2 in twenty-two comparisons and is not claimed;
everything else sits under |t| = 1.9. Extinctions are uncorrelated between fields (2 both, 12
terrain-only, 6 flat-only, 100 neither).

**The reason is the real finding: the join makes the arena MORE uniform, not less.** Across 1,681
positions in the +/-25 arena, terrain moisture has sd **0.050 / 0.037 / 0.005** at seeds 42 / 71 /
161 against the flat field's **0.240 / 0.240 / 0.283**; temperature sd **0.014** against 0.189 at
seed 161. The arena is 50 units wide = **0.1 radian** on a 500-unit planet, and terrain climate
varies on continental scales. Terrain also runs systematically warmer: mean temperature 0.75-0.79
against 0.39-0.43.

**The flag is left OFF everywhere, including the terrain playtest.** Turning it on would flatten
that scenario's climate heatmap and buy nothing. This is a deliberate deviation from the queued
instruction, which assumed the join would add spatial structure.

**`plantTemperatureAdaptationEnabled` was already out of `KnownInertFlags`** - removed when the
procedural fields landed, with the reason recorded in the list's comment. The queued step three
predicted that failure; it had already happened.

### The decision was taken: a local band, not a wider unit (`docs/experiments/p4-terrain-local-band-2026-08-23.md`)

Of the two options, **option 2** is implemented: `SampleTerrain` treats terrain climate as the
regional **mean** and adds a zero-centred local band to moisture and temperature at the procedural
field's own noise scale. Option 1 - widening metres-per-unit - was **rejected**, because it changes
what every recorded distance means (decision 15).

- **Spread restored:** moisture sd **0.207 / 0.192 / 0.178** at seeds 42 / 71 / 161, against the
  procedural field's 0.240 / 0.240 / 0.283 and the band-less terrain's 0.050 / 0.037 / **0.005**.
  Temperature 0.154 / 0.134 / 0.108 against 0.182 / 0.201 / 0.189 - lower because the window sits at
  a warm latitude and the upward half of the band clips.
- **Strengths are `LocalMoistureStrength = .40`, `LocalTemperatureStrength = .32`**, chosen to match
  the procedural spread, not to maximise it. Raising them does not add variety, it deletes the
  regional signal.
- **Elevation is untouched** and still equals the generator's own sample exactly.
- **No new config flag**, because the band sits inside the already-gated terrain path. Flag-off is
  still byte-identical: **503 / 19 / 33 / 1 green**, Unity compile clean.
- **Re-measured, 480 more runs: still no plant conclusion moves.** Paired terrain-minus-flat,
  contest-on: `WaterEfficiency` -0.0221 (t -2.33) and `NutrientUptake` +0.0277 (t +2.06) are the
  only cells past |t| = 2 in twenty-two comparisons; neither is claimed. **This null now means
  something** - the field carries comparable structure and selection is indifferent to its source.
- **`NutrientUptake` is the one to watch** if anything is followed up: its selection weakens under
  terrain (contest-on flat -0.0536, t -5.39; terrain -0.0259, t -2.37), consistent with fertility
  being the channel whose shape moved most.
- **`p6-terrain-playtest` now sets `terrainDrivenEnvironmentEnabled: true`.** Only safe because the
  band exists - without it that scenario's temperature heatmap would be one flat colour. **No
  experiment configuration is touched**; `CreateFullEcosystemDefaults` still has the flag off, so
  every recorded baseline stands.

### Rivers: built twice, rejected, and REVERTED (`d0d2199` and `65e78ac`, reverted 2026-08-23)

**Head carries no river code.** Both attempts are reverted; the terrain is exactly what it was before
them. The postmortem is in `docs/terrain-caves-and-rivers.md` and is the authoritative account.

**Why it was rejected, in one line: painted rivers do not drain, cannot erode, and cannot animate.**
The generator is a pure function of one direction, so it reads nothing about a point's neighbours -
and drainage, erosion and a flowing surface are all relationships *between* neighbours. No amount of
profile shaping fixes that.

**What the two attempts cost, and what they bought.** Attempt one carved a fixed 5 m slot and read as
a stripe. Attempt two fixed every named artefact - monotone profile, compactly supported valley blend
after Peytavie et al., cut-only modifier, no flooding land to sea, gradient plus momentum instead of
eight compass points, a tributary pass, tapered heads - and the verdict was still that it does not
look like a river. **That is the finding worth keeping:** the fixes were correct individually and the
approach was wrong.

Numbers, since they cost real compute: seed 42 produced 58 courses and 2,005 segments in 1.1 s; a
snapped 50-unit arena window held 34.5% valley and 5.8% open water with a deepest cut of 4.1 m. The
480-run plant sweep over that field moved nothing conclusive - `MoistureTolerance` +0.0402 (t +2.57)
and `WaterEfficiency` -0.0189 (t -2.10) were the only cells past |t| = 2 in twenty-two comparisons.

**Next on this thread, and it is a prerequisite chain, not a menu:**

1. **Region/chunk system.** A finite grid at fixed resolution, cached per region.
2. **Depression filling and D8 flow accumulation** on it - real drainage, branching, discharge.
3. **Erosion** - valleys form around rivers because water dug them.
4. **A water surface mesh** with flow vectors, which is the only way rivers animate.

**Do not attempt rivers again before step 1 exists.**

### The planet has level of detail (`PlanetChunkedSurface`, 2026-08-24)

**Twenty icosahedron faces, each the root of a quadtree, split where the camera is.** The single
subdivision-5 icosphere is gone from the runtime - it survives only in the offline capture, as the
"before" half of a comparison. Depth caps at 6, which is a 0.54 m triangle: finer than a creature.

**The detail is real.** Each chunk band-limits its elevation to its own grid
(`PlanetChunkLod.DetailLevelFor` = depth + 4 icosphere subdivisions), so splitting adds octaves
rather than re-drawing a smooth surface with more triangles. Sampling past the grid is what turned
the globe into static originally, so the limit is derived per chunk, never chosen.

**Measured, from the capture at 20 m altitude: 908 chunks, depths 1 to 6** - roughly 230k triangles
and, more to the point, **908 renderers**. That draw-call count is the open cost question; nothing
has profiled it in Play mode yet. Halving it is one line (`MaximumDepth` 5).

**Seams.** Neighbouring depths disagree along their shared edge because they band-limit differently.
`PlanetChunkSeamTests` measures it rather than leaving it to the eye: **worst case ~0.04 of a chunk
edge** at every level - 0.285 m at depth 6, 3.8 m at depth 1. Skirts are sized at 0.05 of the edge,
which clears it everywhere. Removing the disagreement entirely needs geomorphing, which is a separate
piece of work and is not queued.

**Chunks fully underneath the arena are dropped** (`HiddenByArena`), because the arena is drawn
separately at a finer resolution. The ring straddling its border is still drawn, so there is no hole
beside the patch edge.

**`PatchLift` is no longer suspect, and this is measured.** The patch samples about 12,000 around the
equator and the deepest chunk about 5,300 - and they produce **identical elevation**, because the
octave cap is reached before either band limit binds. The worry was real against the old
subdivision-5 backdrop, which sampled 166. `PlanetChunkSeamTests` guards it: if the octave cap is
ever raised, the patch gains detail the backdrop lacks and the test fails.

**A seam is visible where two depths meet**, reported from Play mode. Removing it needs neighbour-aware
morphing, which is a real piece of work and is not queued. **One cheap idea was tried and measured
worse** - see `Segments`.

### The developer camera is now free-fly (`FreeFlyCameraController`, 2026-08-24)

**The orbit rig is deleted.** `GroundPlaneCameraController` is gone; the camera is a position and a
direction, and the whole class of bug that came from clamping a focus point to a box in x and z is
gone with it. There is no focus, no orbit, and no handover between two framing rules - the three
things the previous camera's four shipped bugs came out of.

**Controls.** Hold the **right mouse button** to fly: mouse looks, **WASD** moves along the view
axes, **Q/E** down and up along the local vertical, **shift** boosts five times, **alt** slows to a
fifth, the **wheel** sets the speed dial. With the button up the camera is inert, which is what makes
this possible at all - `D`, `E` and `F` are scenario hotkeys, and a camera that swallowed them would
have cost more than it gained. **Arrow keys** move without the button, the **wheel alone** dollies,
and **Home** frames the arena. The HUD legend lists all of it.

**Speed is proportional to height above the surface**, clamped below by a floor of 1.8 units/second
and above by the extent of what is on screen. That single rule is why one camera works beside a
1-unit creature and around a 500-unit planet: the height above the ground *is* the scale the viewer
is working at.

**Two rules, never blended.** When `SetExtent` is given a surface radius, height is measured from the
sphere and up points away from its centre - so flying to the far side stays upright. With radius zero
the world is a plane and height is `y`. The arena and every preview use the plane; the curved arena
uses the sphere. There is deliberately **no interpolation between them**, because a handover between
two framing rules is exactly what produced `b336b7d`.

**It finally has an instrument.** `FreeCameraMotion` holds the arithmetic - `SpeedAt`,
`ClampAltitude`, `ClampPitch`, `AdjustDial` - with no Unity types in it, and is compiled into
`tools/HeadlessTests` by an explicit `<Compile Include>` alongside nine tests in
`FreeCameraMotionTests`. It is the only Presentation file in that project and the comment there says
why. `LifeSimulation.EditModeTests` also references `LifeSimulation.Unity` now.

**Not verified visually.** Camera behaviour only exists in Play mode and there is no way to drive
synthetic input headlessly, so the compile is clean and the arithmetic is tested but nobody has flown
it yet. **First thing to do next session: press Play, fly, and report.** Things most likely to be
wrong are feel rather than logic - look sensitivity (4.5 degrees per unit of mouse movement), the
dolly notch, and whether the speed floor is slow enough beside a creature.

### Also open, smaller

- ~~`PatchLift` needs raising~~ - **closed 2026-08-24, measured.** The backdrop and the patch agree
  exactly now that the backdrop has level of detail. See the planet section above.
- **Rivers are not on this list.** They were built, rejected and reverted; see the rivers section
  above. Nothing about them is to be attempted before the region/chunk system exists.
- ~~Ice is heavy at high latitude~~ - **that is what high latitude is for.** Closed with the item
  above: the ice is 98% polar, which is the shape a planet is supposed to have.

**Use `ComputeStateFingerprint()` for "do these two worlds evolve identically" questions.** Never
`ComputeStateHash` — V1 is a frozen historical identifier and is deliberately incomplete. Never
recompute or overwrite a recorded V1 value.

**Use `ExperimentManifest` + `ExperimentCsv` for every new experiment CSV.** `ExperimentCsv.Compose`
refuses without provenance; that is deliberate.

---
