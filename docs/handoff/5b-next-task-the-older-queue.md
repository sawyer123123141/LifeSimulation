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

---

## Moved out of SESSION_HANDOFF section 5 on 2026-08-29

The 2026-08-24 next-task list, verbatim. It was still sitting at the bottom of section 5 and had
grown to 134 lines of material that is either closed or superseded, which every new session was
paying to read past. **Nothing here was deleted and several entries are still live** - notably
item 6, the emergent-behaviour question, whose current state is the 2026-08-26 and 2026-08-29
blocks in section 5 and the `emergent-behaviour-*` documents.

### Everything in the previous list is closed (2026-08-24, late)

The four items that stood here - the dose-response replication, why temperature tolerance,
`CreatureAppearance`, and rivers - are done or deliberately blocked. What that work opened is below,
ordered by what is actually worth doing next.

**1.** ~~The gate dose-response.~~ **DONE** - `p6-gate-dose-response-2026-08-24.md`. Five values,
80 seeds each, 80/80 surviving at every one. **Predicted a cliff; it is a smooth accelerating curve.**
|t| runs 0.44 / 1.02 / 2.01 / **7.13** / **14.55** across gates 0.45 / 0.55 / 0.60 / 0.65 / 0.70, and
each 0.05 multiplies the drift by roughly 2.7x. **The default gate sits on the steepest part.**
- **The mechanism is a margin, not a threshold.** The population always sits *slightly* above the seek
  gate, and the gate decides how slightly: margin 0.167 / 0.089 / 0.064 / 0.041 / **0.006**. At the
  default it lives six thousandths above the line that decides whether it can breed. **Raising the
  gate raises the population's energy** - it is a feedback loop, which is why the cliff prediction
  failed: the level I expected the gate to cross moves with the gate.
- **DONE, and it caught an overclaim.** "Five traits stop being selected" was true at 0.45 and false
  across the curve. Only `urgency_exponent` has a clean graded response (-0.44 to -14.55);
  `travel_sensitivity` supports it but **never passes |t| = 2.2**; `movement_speed` is back to
  **t = 3.16 at a gate of 0.55** so it is not a gate response at all; `body_size` and `metabolic_pace`
  are noise across the curve and their default-gate values (-2.01, +0.86) were marginal anyway.
  **A two-point comparison manufactured a five-trait claim that five points deleted.** Banner added to
  the earlier doc.

**2.** ~~Profile Play mode.~~ **DONE** - `p6-play-mode-profiled-2026-08-24.md`. The game writes
`Logs/performance.txt` itself every five seconds; no Profiler window, and the reading is an artefact
that can be diffed.
- **1,090 renderers and 566,272 triangles at a median of 2.83 ms - 354 fps.** Faster than the arena
  view at 49 renderers. **The terrain optimisation queue is unnecessary**: GPU displacement, CDLOD,
  shared index buffers and the 53 MB of de-indexed vertex data are all real and none of them matter
  at this scale. Only 597 draw calls for 1,090 renderers - Unity batches about half unaided.
- **The 197 ms worst frame was first-entry cost, not a stutter.** Steady state worst is **19.08 ms**
  over 1,664 frames. The heatmap - the prime suspect, 16,384 samples every two seconds - measures
  **0.00 ms** and is cleared. It was instrumented rather than accused, which is what showed it.
- **The 908-renderer / 232k-triangle figures were never measurements** and are superseded by the
  numbers above. My own review flagged that they did not reconcile; neither was a capture.
- **Still unknown:** one machine, one session, in the **editor**, with population at 9-17 against a
  cap of 100. **Creature rendering at full population is untested** and creatures are one renderer
  each.

**3. Real models — a pack of 8 animated animals is available.** Sequencing decided and written into
`docs/creature-appearance.md`: **profile with capsules first** (no baseline exists and there are 908
chunk renderers already), then **swap ONE model** to prove the pipeline, then map the rest to an axis
that is real *today* - predator/herbivore via `Aggression`/`Attack`/`DietSpecialization`, or body-size
class - and only assign **model per species cluster** once P5 clustering is trusted. **Do not assign
eight models arbitrarily**: it teaches a viewer that model means decoration, which has to be un-taught
when it should mean species. Animations map to `CreatureAction` for free, which is the biggest
legibility win available.

**3b. `CreatureAppearance` step 3.** Apply the mapping at the one call site in
`Prototype1Presenter.Views.cs`, behind the unbound `U` key, per creature and never by population
mean. **Waits for real models** - that half is what a model swap would redo. The pure half is built
and tested.

**4. A carrying-capacity-limited habitat** - section 9's item C5, the oldest debt here. **Now
diagnosed rather than merely described** - `p6-the-cap-is-the-stabiliser-2026-08-24.md`.
- **Scarcity is not the cause and cannot be.** `Scaled` multiplies amount, capacity and regeneration
  together, so the dynamics are **scale-invariant by construction**; measured, 0.40x to 1.00x all
  collapse at cap 250 (21-23 of 24 extinct) and the level makes no difference.
- **`WithRegeneration(id, factor)` is new** and moves the ratio `Scaled` cannot. Faster regrowth
  converts collapse into survival at cap 250 (3 -> 23 of 24 at 2.0x), **then pins at the new cap**
  (sd 0.65 at 3.0x - the same zero-variance ceiling, moved up).
- **The finding: raise the ceiling out of the way and it collapses again.** 2.0x regeneration
  survives **23 of 24 at cap 250 and 3 of 20 at cap 500**, same ecology. Starvation is **35-64% of
  deaths at cap 500 against 0.1% at cap 100**. **The cap is not bounding a self-regulating ecology;
  it is supplying the regulation.** The model has no density-dependent brake of its own.
- **BUILT AND CONFIRMED - the debt is closed.** `p6-graded-fertility-closes-the-cap-debt-2026-08-24.md`.
  `gradedFertilityEnabled` scales the reproduction **cooldown** by condition, measured on the binding
  need and against the gate rather than zero: `1 + 3*(1 - headroom)`, so full condition is unslowed
  and sitting on the gate waits 4x. **Deterministic** - a breeding probability would need an RNG in
  the tick; the cooldown gives the same feedback with none.
- **Survival 3 of 20 -> 19 of 20 at cap 500. Starvation 55-64% of deaths -> exactly 0.0%** - no
  creature starved in any run. Population settles at **75-110 with sd 50-75 under a cap of 500**.
  **That is a carrying capacity.**
- **It needed no scenario redesign.** Standard layout, standard cap 100: population **63.1, sd 33.6**
  against 98.2 pinned with no variance. The habitat could always limit itself; nothing told it to slow
  down. `WithRegeneration` was needed to *diagnose* and is not needed to *fix*.
- **The price, honestly:** 28 of 30 surviving against 30 of 30 at cap 100. A self-regulating
  population sits lower and a lower population is nearer zero. Mean energy 0.8058 -> 0.7847.
- **Not proof the step gate was the only cause** - adding a brake removes the collapse, which is
  strong, but two mechanisms tonight looked sufficient and were partial.
- **Default false and NOT on for `Y`.** It changes population dynamics at the root, which is a larger
  claim on a scenario than a slope cost or a temperature field. **Deliberate decision, not a
  playtest fold-in.**
- **QUALIFIED - the strength does not transfer between ecologies.**
  `p6-graded-fertility-is-scenario-specific-2026-08-24.md`. Strength 3 gives a carrying capacity in
  the resource-backed calibration scenario and **collapses the plant-backed full ecosystem to
  population 10 with 21 of 60 extinct.** **Strength 1.0 is the equivalent result there** - extinction
  5 of 40 against 11 of 60 with **no** brake, zero frozen worlds against 7, population 70.9 under a
  cap of 250. **A factor of three in strength separates the best and worst conditions tested**, which
  is why `GradedFertilityStrength` is now a hashed configuration value rather than a `const`.
- **Do not carry the default strength into a new scenario.** Choose one and measure it. That is the
  finding.
- **My own confound, recorded:** the first plant comparison changed cap *and* brake together and
  looked like the brake was harmless. Separating them showed raising the cap alone *raises*
  population (40.8 to 80.7) and the brake at 3 is what collapses it.
- **Untested hypothesis for why they differ:** plants are patchy and variable, so condition likely
  sits lower and more variably in the plant world, and the brake keys off exactly that. **Nothing
  currently reports the distribution of the binding-need condition** - that is the measurement.
- **Consequence: the plant corpus's scope qualification now has a working configuration to be
  re-tested in** - cap 250 at brake 1.0, population unpinned and healthier than unbraked. The 60-seed
  contest and join comparisons are null in both arms, **but those used the confounded arm and are not
  a re-validation.** ~~**Re-running the plant corpus at brake 1.0 is the large next piece of work.**~~ **DONE
  (2026-08-26)** - `p6-plant-corpus-revalidated-unpinned-2026-08-26.md`. 60 seeds, four cells, cap 250
  at brake 1.0: population **63-67** under a cap of 250, **0 / 60 frozen in every cell**, extinction
  10-15%. **The contest null and the join null both hold unpinned** - every |t| <= 1.64 on the contest
  and <= 1.99 on the join, across 44 columns - while drift from founders in the same runs reads
  **+8 to +11** on Dispersal, so the instrument is demonstrably not blind. **The qualification is
  lifted for these two comparisons only**; the nine other corpora were measured pinned and are not
  re-run. **One thing to test rather than claim:** `MoistureTolerance` is the only trait the join
  moves (+0.045 / +0.048, t +1.87 / +1.99, and selection against it is -3.6/-3.9 flat versus
  -0.64/-1.42 under terrain). **The two arms share seeds, so that is one observation, not two** -
  it needs a fresh seed block, not a re-reading.
- `--regen=X` and `--scale=X` exist on `CreatureSweep`, and `--deaths` now reports population
  **spread** (min/median/max/sd) - a carrying capacity makes a distribution, a cap makes a constant,
  and eleven committed corpora could not tell them apart.

**5. Health recovery as a default.** Flagged in `p6-health-recovery-2026-08-24.md`: it *removes an
artefact* rather than adding realism, which makes it different in kind from the slope cost and the
terrain temperature. It is the first flag to flip whenever a re-baseline is taken deliberately.
**Do not flip it casually** - it re-baselines everything.

**6. Emergent behaviour - can evolution invent behaviours nobody programmed?** Asked 2026-08-24.
**Read `docs/emergent-behaviour-constraints-2026-08-24.md` before designing anything**, because three
measurements from that session bear on the answer and were not available when the question was framed:
**96.9% of deaths are old age with predation at zero and 15 starvations in 5,619**; **one threshold
carries nearly all selection** (the 0.80 mating gate, t = -14.55 to -0.44 across five values); and
**`ComputeNeedGain` saturates**, so the foraging decision carries no information a richer controller
could exploit. The rival proposal a design must beat is **enrich the world before enriching the
brain** - stated there as a hypothesis to argue against, not a conclusion. That doc also records a
**contradiction**: place memory is on the do-not-touch list, and caching, territory and shelter all
require it.

**Do not restart rivers.** Still blocked behind a persistent grid.
