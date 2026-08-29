# Session Handoff — 2026-08-24

> **This file was 1,559 lines.** The archival sections now live in `docs/handoff/`, lifted
> whole by `tools/split_doc.py`: **nothing was summarised or rewritten**. What stays here is
> what a session needs immediately - unresolved findings, decisions not to reopen, the next
> task, and the working-tree rules. The index below says what is in each file.
**Head at handoff: `b0830b8`** plus the commit carrying this handoff, pushed to `origin/main`.
Working tree clean apart from the untracked Unity `.meta` files, `Assets/_Recovery/` and
`ProjectSettings/PackageManagerSettings.asset` that are **never to be staged**.
**611 headless tests green**, Unity compile clean.

### Phase I — the reproduction gate, the cap, and the first Play-mode numbers (2026-08-24, late)

**29 commits, `4387439` through `b0830b8`.** Five new flags, all default `false`; two switched on for
the `Y` playtest only. **Nothing recorded moved.**

| commit | what |
|---|---|
| `4387439` | **temperature tolerance is a saturating gene**; the terrain-join hypothesis refuted by the call path |
| `3aa3f1a` | `CreatureAppearance` - the pure genome-to-appearance half that survives real models |
| `775baa3` | dose-response replicates at 80 seeds; **corrects two earlier claims** |
| `0700e27` | **`terrainDrivenTemperatureEnabled`** - creature temperature from the world, not a sine |
| `c84de2a` | measured across three conditions; **on for `Y`** |
| `6c87e06` | external terrain brief + a review that mostly disagrees with it |
| `1a2f2cc` | **`MetabolicPace` is a pure cost**, and a liveness harness cannot see that |
| `434e504` | benefit attempt 1 - faster ingestion. Shared channel, diluted, **declined** |
| `e2c2581` | **`UrgencyExponent` is a monotone benefit** - 9 of 9 conditions negative |
| `c165d0c` | **correction: the proposed repair was backwards.** `ComputeNeedGain`'s clamp is right |
| `9a17c28` | **nothing starves** - 15 of 5,619 deaths - and the population is pinned to the mating gate |
| `ad052ae` | **the mating gate is the dominant selective channel** |
| `9926b36` | **`healthRecoveryEnabled`** - health was a one-way ratchet gating reproduction |
| `88aebb6` | benefit attempt 2 - faster healing. Private but never collectable. **Three failures explained** |
| `175c7b5` | health recovery **on for `Y`**; `MetabolicPace` documented, **not renamed** |
| `159bb1a` | the gate dose-response: **a squeezed margin, not a switch** |
| `3b4c7ea` | **reading the whole curve deletes the "five traits" claim** |
| `403b04f` | sequencing for the 8-animal asset pack |
| `7bbae9d` | **the population cap is the stabiliser, not the ceiling** |
| `d663ac7` | **`gradedFertilityEnabled`** - a carrying capacity at last; the oldest debt closes |
| `77a8f6e` | **qualified: the brake generalises, its strength does not.** 3 wrecks the plant ecology, 1 is right |
| `928944a` | **fix: `PlantSweep` was overwriting a committed corpus on every run** |
| `a381c28` | the game writes `Logs/performance.txt` itself |
| `37ca115` | per-section timing |
| `c02ce34` | **Play mode profiled**: 1,090 renderers, 566,272 triangles, 354 fps |
| `ac34f53` | **the profiler was discarding its own history**, which is why it missed the spikes |
| `ea085c5` | **the stutter is the heatmap** - 192.95 ms a rebuild, several times a second |
| `b0830b8` | `tools/split_doc.py`; the two session-opening docs cut from 3,311 lines to 833 |

### Phase I verified numbers - do not re-derive

**Selection and the gate**
- `urgency_exponent` across five gate values 0.45 / 0.55 / 0.60 / 0.65 / 0.70:
  **t = -0.44 / -1.02 / -2.01 / -7.13 / -14.55**. Margin above the seek gate: **0.167 / 0.089 /
  0.064 / 0.041 / 0.006**. Each 0.05 of gate multiplies drift by ~2.7x. **The default sits on the
  steepest part.**
- **Of the two gate literals, 0.80 binds and 0.70 is dead** - lowering only `CanReproduce` changes
  nothing, found by a wiring bug that produced byte-identical output.
- Death mix at cap 100: **age 96.9%, health 2.9%, starvation 8, dehydration 7 of 5,619.** Mean energy
  **0.8058** against a 0.80 seek gate - a homeostat, not an equilibrium.

**Temperature**
- Saturation: tolerance is `2 + 8*gene` against a field bounded at 8, so **gene 0.75 covers the
  world**. Measured plateau **0.7790 at 40 seeds**, 0.7475 with the join off.
- Terrain temperature: endpoint sd across worlds **0.0744 -> 0.1454**, variance ratio 3.8 on 39/39 df.
  Selection **halves** (+0.2879 to +0.1251 at moderate); `lifespan_tendency` **overtakes it**.
- Across three resource levels the thermal endpoint is **0.767 / 0.763 / 0.783** - the destination is
  a property of the field, not the ecology.

**The cap and the brake**
- Same ecology, 2.0x regeneration: **23 of 24 surviving at cap 250, 3 of 20 at cap 500.** Starvation
  **0.1% of deaths at cap 100, 35-64% at cap 500.**
- Graded fertility at cap 500: survival **3 of 20 -> 19 of 20**, starvation **-> exactly 0.0%**,
  population 75-110 with sd 50-75. At cap 100: **63.1 with sd 33.6** against 98.2 pinned.
- **Brake strength does not transfer.** Plant ecology at cap 250: no brake 11/60 extinct and 7 frozen;
  **strength 1.0 gives 5/40 extinct, 0 frozen, population 70.9**; strength 3.0 gives 21/60 and
  population 10.0.

**Play mode, measured for the first time**
- Planet view: **1,090 renderers, 566,272 triangles, median 2.83 ms (354 fps), 597 draw calls.**
  **The terrain optimisation queue is unnecessary.**
- Heatmap stutter: **192.95 ms per rebuild, 1,534 ms in a five-second window**, worst frame
  **1,476.83 ms**. After amortising: worst call **7.49 ms**, worst frame **11.95 ms**, **0 frames
  over 33 ms**.
- **The recorded "908 renderers" and "232k triangles" were never measurements** - both came from
  walking the quadtree, and they never reconciled. Superseded.

### Phase I rejected hypotheses - do not re-run these

1. **The terrain join explains temperature tolerance.** Refuted by the call path before any run:
   thermoregulation reads `TemperatureField`, the join builds `EnvironmentField`. Both arms would
   have been the same experiment.
2. **`ComputeNeedGain` saturation removes `UrgencyExponent`'s trade-off, so unsaturate it.**
   **Backwards.** The term is `min(1, patch / shortfall)`; removing the clamp makes food *more*
   attractive to a full creature. The clamp is correct and `urgency` is itself the
   diminishing-returns term.
3. **The health ratchet explains the thermal selection.** Contributes 19% (t 26.03 -> 23.89), does not
   explain.
4. **Scarcity causes boom-and-collapse.** Cannot: `Scaled` moves amount, capacity and regeneration
   together, so the dynamics are scale-invariant by construction. 0.40x to 1.00x all collapse
   identically.
5. **A private benefit rescues `MetabolicPace`.** Necessary, nowhere near sufficient - healing pays
   only while injured and mean health is 0.9939. **The costs are continuous and no available benefit
   is.**
6. **The gate effect is a cliff.** It is a smooth accelerating curve; the level the gate was expected
   to cross **moves with the gate**.
7. **"Five traits stop being selected" at a slack gate.** Two points made a slope out of noise; five
   points deleted it.
8. **The 197 ms frame was a one-off.** It was not - the profiler was overwriting its own history.
   **The user's direct report was right and the instrument was wrong.**

### Phase H — camera, planet level of detail, and the first measured selection (2026-08-24)

| commit | what |
|---|---|
| `3af2583` | **free-fly developer camera** replaces the orbit rig; speed scales with height |
| `1a59da8` | **the planet has level of detail** - quadtree per face, 19 m facets down to 0.54 m |
| `9a21544` | chunk-sizing idea measured and reverted; **`PatchLift` closed** |
| `84ca29e` | `O` is a mode: hides UI, pauses, cleans up. `K` drops its redundant second planet |
| `3a40b0e` | **climbing costs energy**, behind a flag, off and unmeasured |
| `d76b021` | `tools/CreatureSweep`; slope cost moves nothing at the plant corpus's conditions |
| `1ffccb7` | the `J` panel reaches the planet, not just the arena under it |
| `8fcc96e` | re-mesh the planet in place once tuning settles - the drag lag |
| `5a089f1` | **`GeneticClusterHistory` split**, 1324 lines into four partials |
| `56c44e5` | focused slope run: suggestive on survival, gene table an artefact |
| `1977e3a` | **ice measured and closed** - 3.32% of surface, 98.31% polar |
| `4a4bd37` | slope cost puts creatures on flatter ground; **on for the `Y` playtest** |
| `b542c5b` | **selection is happening** - measured against founders for the first time |
| `04ef603` | plan for making adaptation visible, deliberately unbuilt |
| *this commit* | **body size shrinks under scarcity**, and this handoff |

Phase G and earlier are below.

### Phase G — the join measured, a local climate band, and rivers rejected (2026-08-23)

| commit | what |
|---|---|
| `88a236b` | **the join moves no plant conclusion** - 480 runs - because it flattens the arena |
| `d7caa52` | that negative result, and why a 50 m window cannot see continental climate |
| `af43b76` | **terrain sets the regional climate, a local band supplies the variation** |
| `d0d2199` | rivers, attempt one - carved as a slot |
| `65e78ac` | rivers, attempt two - inscribed with a valley blend |
| `c58b444` | **both reverted.** Painted rivers cannot drain, erode or animate |

Earlier phases and their nineteen commits (`9442bd0` through `6b87771`) are in Phase F below.

**Read `docs/terrain-brainstorm-2-2026-08-23.md` and `docs/reference-implementations.md` before
touching terrain**, and `docs/terrain-caves-and-rivers.md` before adding to it. The first explains
why the original design was structurally wrong rather than under-tuned.

Two documentation commits sit between that and the previous handoff state (`c197061`): `d40f7ea`
rewrote this file, and the docs commit that follows `7343653` records the fingerprint work below.

Read this first, then `docs/CLAUDE_HANDOFF_2026-08-22.md` for architecture, scientific context,
testing rules and user preferences. `docs/ROADMAP.md` is the backlog. `docs/superpowers/plans/` is
an archive, not a backlog.

---

## Sections held in separate files

Lifted whole, nothing rewritten. Read the one you need.

- [1. What was completed this session](handoff/1-what-was-completed-this-session.md) — 87 lines. Twenty commits, f0a691d through c197061. Three phases
- [2. Verified numeric results — do not re-derive these](handoff/2-verified-numeric-results-do-not-re-derive-these.md) — 348 lines. mean familiarity, with an unsaturated off-arm route metric of 0.7955
- [5b. Next task (the older queue)](handoff/5b-next-task-the-older-queue.md) — 176 lines. 12,000 ticks, flat versus terrain-driven crossed with the establishment contest. Full writeup in
- [6. Test commands](handoff/6-test-commands.md) — 27 lines. dotnet test tools/HeadlessTests            # 603 green
- [6b. Test commands (older)](handoff/6b-test-commands-older.md) — 61 lines. From tools/HeadlessTests
- [7. Play-mode keys](handoff/7-play-mode-keys.md) — 11 lines. Scenarios: V shifting patches (best — the map changes as you watch) · 5 stable · 6 scarcity
- [9. Deferred work — what to pick up after terrain](handoff/9-deferred-work-what-to-pick-up-after-terrain.md) — 81 lines. Written at the point of switching to P6 terrain groundwork. The tree was clean at this point
- [10. Terrain (P6 groundwork) — where it actually stands](handoff/10-terrain-p6-groundwork-where-it-actually-stands.md) — 222 lines. touching terrain. The first explains why the original design was structurally wrong rather than

## 3. Unresolved findings

### Added 2026-08-24

- ~~**Why temperature tolerance.**~~ **Answered, and the hypothesis in this slot was wrong** -
  `p6-why-temperature-tolerance-2026-08-24.md`. The terrain join cannot reach the gene: creature
  thermoregulation reads `TemperatureField.Sample`, a fixed sine `20 + 8*sin(0.18x + 0.11y)` with no
  terrain input, while the join builds the `EnvironmentField` that feeds plants. Tolerance in degrees
  is `2 + 8*gene` against a field that deviates by at most 8, so **gene 0.75 covers the whole world**
  and costs about 1% of upkeep. Measured: the mean plateaus at **0.7790 at 40 seeds** (0.7475 with
  the join off), realised `|T - 20|` maxes at **8.000**, covering gene **0.750**. Running the join arm
  anyway removed 0.044 of 0.285 and none of the shape.
- ~~**The temperature field is a placeholder.**~~ **Fixed behind a flag** -
  `terrainDrivenTemperatureEnabled`, default false, flag-off byte-identical.
  `p6-terrain-temperature-2026-08-24.md`. `ClimateField` is where a creature's degrees come from; a
  `default` instance *is* the sine, which is what makes flag-off free of a branch at every call site.
  **What it changes:** the between-world standard deviation of the endpoint goes 0.0744 to 0.1454,
  variance ratio 3.8 on 39 and 39 df. The sine gave every arena the same climate; terrain gives one a
  temperate continent and another a cold one, and one world ended **below its founders** - the first
  time the gene's maintenance cost has been visible.
- **`terrainDrivenTemperatureEnabled` is on for the `Y` playtest, default still false.** Three
  conditions at 80 seeds each: **105 extinct of 240 against 94, z = 1.02** - no detected survival
  cost, but consistently worse under scarcity (lean 33 against 25, z = 1.32) and **underpowered to
  resolve it**. Absence of evidence of harm, not evidence of safety. Selection on
  temperature_tolerance **roughly halves at every level** (+0.288 to +0.125 at moderate) while
  lifespan_tendency strengthens (+0.275 to +0.296) and **becomes the strongest trait in the model**.
  Every recorded thermal result was measured with the flag off.
- **The lean control is noisy at t = -1.91** in the terrain-temperature run. Columns near |t| = 2 in
  that condition mean little.
- ~~`metabolic_pace` is a candidate worth another look.~~ **Looked at, and the answer was in the
  source** - `p6-metabolic-pace-is-a-pure-cost-2026-08-24.md`. `MetabolicPace` raises the water drain
  (`NeedsSystem.cs:49`) and the energy drain (`NeedsSystem.cs:45`) by 2.14x across its range and
  **has no third reader at all** - nothing converts it into food, yield, or speed, and
  `DigestionRate` does not make digestion faster. Downward in **five of six** already-committed
  conditions, monotonic in scarcity, strongest at lean/sine with **t = -2.99 against a control at
  +0.07**. No new runs were needed.
- **A pure-cost gene passes every liveness test by construction, and nothing here tests for a
  benefit.** `GeneLivenessAnalysis` asks whether a gene reaches behaviour; a cost reaches behaviour.
  This is the shape `PlantGeneLivenessAnalysis` already names for plant `TemperatureTolerance`.
  **`metabolicIngestionEnabled` now exists to answer that** - default false, flag-off byte-identical,
  ingestion scaled by the same `0.7 + 0.8*pace` factor the drains use.
- **The benefit halves the bleed and does not make the gene a trade-off** -
  `p6-metabolic-ingestion-2026-08-24.md`. Predicted a sign flip across the resource ladder; **there
  is none.** At lean the drift goes -0.0252 (t = -2.99) to **-0.0129 (t = -1.55)**, below
  significance, and at moderate the gene is flat at t = -0.21. The gene went from being sold to being
  held, not to being bought. **Default stays false and it is NOT on for `Y`** - unlike the slope cost
  and the terrain temperature, it did not do what it was built to do.
- **The scarce row of that run is unreadable and must not be quoted**: `neutral_marker` at
  **t = +3.31** on 4 surviving worlds of 80. The -0.1267 at t = -3.12 beside it is composition, not
  selection.
- **Untested hypothesis worth keeping:** ingestion is a *shared* channel, so faster eating may deplete
  contested sites sooner and partly cancel its own benefit - a commons effect that would explain both
  the missing sign flip and the extinction direction (94 to 107 of 240, z = 1.20, not significant).
  **The test is site depletion and mean energy between the two configurations**, which the drift table
  does not carry. A *private* benefit - faster recovery, shorter handling, shorter reproduction
  cooldown - would not be diluted by competitors.
- **Renaming was never a fix and was wrongly offered as one.** A trait axis with no upside at any
  value is a missing mechanic, not a bad allele. The real repair is a **private** benefit.
- **Health never regenerated, and that is a second real defect** - `p6-health-recovery-2026-08-24.md`.
  Five subtractions in `NeedsSystem`, no addition anywhere, so health was a one-way ratchet - and it
  is one of the three conditions on the mate-seeking gate, which makes a fifth of health lost equal to
  **permanent sterility**, not injury. `healthRecoveryEnabled` closes it: 0.5% of capacity per second,
  only while over half full on energy and hydration, applied after the damage so a hot band still nets
  a loss. Default false.
- **The ratchet was a contributing cause, not the explanation.** Predicted healing would visibly
  weaken the thermal selection; measured **+0.2879 (t 26.03) to +0.2323 (t 23.89)** - a 19% reduction.
  Real, right direction, **not** the driver. The arithmetic account in
  `p6-why-temperature-tolerance-2026-08-24.md` remains primary.
- **Do not compare the other columns across those two arms.** The recovery arm has **zero extinctions
  against one** and a control at t = -0.0039, so it is simply cleaner; every other |t| rises and none
  of it is attributable. The report's "vs control" ratio column divides by ~0 there and prints numbers
  like 30,988x - **meaningless in that arm, read the t values.**
- **DONE, and it failed as predicted** - `p6-metabolic-pace-has-no-benefit-that-fits-2026-08-24.md`.
  `metabolicHealingEnabled` scales recovery by pace: private, undilutable, feeds the gate. Predicted
  little or no effect because mean health is **0.9939** and almost nobody is ever injured. Measured
  **-0.0050 at t = -1.01** against -0.0020 at t = -0.36 without the scaling. Being private was
  necessary and nowhere near sufficient.
- **The reason all three attempts fail is the real result.** The drains are paid **continuously**;
  ingestion pays only while eating *and is shared*, healing pays only while injured *and injury is
  rare*. **A continuously-charged gene cannot be balanced by an occasionally-collectable benefit**, and
  every continuously-paying channel left - movement speed, perception, food yield, water efficiency -
  **is already another gene's job**, so doubling up would make them non-identifiable.
- **DECIDED: do NOT rename, and this reverses the recommendation above it.** Counted first: 18 code
  files (trivially scriptable) but **38 doc files, 82 mentions, and `metabolic_pace` as a column in
  10 committed CSV corpora.** Renaming breaks the link between every recorded result and the code it
  describes - new runs stop matching old ones, and rewriting old CSVs means editing experimental
  records. **The whole benefit was "a name stops misleading a reader", which an authoritative doc
  comment at the definition delivers at zero cost to the corpus.** Those comments are now on
  `Genome.MetabolicPace` and `Phenotype.DigestionRate` and carry the full story including why the name
  survives. Deleting the gene is likewise rejected - it disturbs every recorded genome layout.
- **Both flags stay committed and default false** as the record of what was tried, so re-running
  either costs one command rather than one rediscovery.
- **`healthRecoveryEnabled` is ON for the `Y` playtest, config default still false.** Measured across
  the same three conditions the other flags got: baseline-arm extinctions **94 of 240 to 82** -
  1 to 0 at moderate, **25 to 14** at lean, 68 to 68 at scarce. z = 1.14, not significant, but **never
  worse at any level**, unlike the terrain temperature which was consistently worse under scarcity.
  Controls quiet in both new arms (t = -0.08, -0.12).
- **Health recovery is the strongest candidate to become a DEFAULT at the next deliberate
  re-baseline.** Unlike the slope cost and terrain temperature, which *add* realism, it **removes an
  artefact**: a quantity that only ever decrements gating a quantity that decides fitness was nobody's
  design choice. Not done now because it re-baselines everything.
- **`metabolicHealingEnabled` is on the `KnownInertFlags` list in `LivenessTests`** - it is inert
  without `healthRecoveryEnabled`, the same shape as slope cost needing elevation. **If it ever
  reports live there, something is healing creatures unasked.**
- **`UrgencyExponent` is the same family, opposite sign** - `p6-urgency-exponent-is-monotone-2026-08-24.md`.
  Read in two places, both `shortfall ^ (0.5 + 2.5*gene)` on a shortfall in `[0,1]`, so **lower is
  monotonically better** and there is no trade-off. **Nine conditions out of nine negative**,
  -0.035 to -0.055, |t| from 3.2 to **19.4**, the most reproducible selection signal in the model.
- **The cause is NOT established, and the first explanation committed for it was wrong.** That doc
  originally blamed the `ComputeNeedGain` saturation and proposed unsaturating it. **The proposed
  repair is backwards**: the term is `min(1, patchAmount * perUnitGain / missing)`, "what fraction of
  my shortfall can this patch fill", and removing the clamp would make food *more* attractive to a
  full creature. The clamp is correct and the term was never the diminishing-returns term - `urgency`
  itself is. Correction is in the doc rather than edited away.
- **Current best hypothesis: the reproduction gates.** `CanReproduce` needs energy, hydration **and
  health all >= 70%** (`ReproductionSystem.cs:215`) and `CanSeekMate` needs all three **>= 80%**
  (`:224`). A creature below 80% cannot even look for a mate, so topping up is a *precondition* for
  breeding rather than a competitor with it - under which eagerness is correct and **the gene may not
  be broken at all**.
- **DONE, and the answer is emphatic** - `p6-the-mating-gate-is-the-selection-2026-08-24.md`. At
  `--gate=0.45`, `urgency_exponent` goes from **-0.0353 (t = -14.55) to -0.0006 (t = -0.44)**. Not
  reduced - **gone**, to a fifth of the control's own movement. **The gene is healthy and there is
  nothing to fix in it.**
- **The gate is the dominant selective channel in the model.** Slackening it removes selection on
  **five traits at once** - urgency, movement speed, body size, travel sensitivity, metabolic pace -
  every one of them a trait whose route to fitness ran through clearing the threshold. Two get
  *stronger*: lifespan_tendency +0.275 to **+0.314 (t 27.8)** and fertility_investment +0.059 to
  **+0.088 (t 7.36)**, which is what pays once the threshold is cheap.
- **Of the two literals, 0.80 is the one that binds.** Lowering only `CanReproduce` (0.70) changes
  **nothing at all** - a creature that cannot seek a mate below 0.80 is already past 0.70 when it
  finds one. Found by a wiring bug that produced byte-identical output and was nearly reported as a
  null.
- **Mechanism confirmed from the other side:** mean energy tracks the gate, 0.8058 to **0.7165**, and
  starvation goes 8 deaths (0.1%) to 163 (2.6%) - creatures are no longer held at 80% by the need to
  court.
- **Caveat on the slack arm:** its control moves to **t = 2.55** against 0.17 at the default, so its
  noise floor is much higher and columns near |t| = 2 there mean little. The headline is unaffected -
  `urgency_exponent` at |t| = 0.44 sits *below* its own control.
- **`ReproductionNeedFraction` is a config value now**, default `0.7f` = the original literal, so
  everything recorded reproduces. **The gate remains a recorded design decision.** This measures how
  much rests on it; it is not an argument for changing it. **Not done: a dose-response across gate
  values** - one alternative value is not a curve.
- **Uniform grazing is real and separate.** `ComputeNeedGain` pinning at 1 does mean patches are not
  differentiated by size, which is a genuine plant-defense problem and why
  `plantQualityPreferenceEnabled` exists. It is not the explanation for this gene.
- **Four genes start monomorphic:** `UrgencyExponent`, `TravelSensitivity`, `RiskAversion` and
  `NeutralMarker` all show founder exactly `0.5000` in every run, because the founder profile does not
  vary them. **Their response is mutation-limited, not selection-limited** - which is how t = -19.4
  produces a shift of only 0.04, and why the control is so quiet in the healthy rows.
- **The amplitude test is still not run**, and it matters less now: the flag above changes the
  field's structure while deliberately holding the 12-28 degree span, so the ceiling argument is
  untouched by it either way.
- **The old amplitude note.** Moving the sine's amplitude should move the plateau to
  `(amplitude - 2) / 8`. It needs the amplitude in `SimulationConfig`, hashed, with a
  `ConfigurationHashVersion` bump and the value threaded through `ThermoregulationSystem` and the
  decision path - production surgery to confirm something the formula already fixes. Worth doing when
  temperature becomes a real climate variable.
- **Slope cost on survival is suggestive only.** Extinction 46 against 38 of 60 at cap 200, but the
  paired test is 13 discordant against 5, **McNemar 2.72 against 3.84 for p = .05**. Direction
  consistent, significance not reached.
- ~~**Ten of thirteen traits show no detectable selection.**~~ **Seven, at 80 seeds.**
  `fertility_investment`, `movement_speed` and `body_size` join the list, consistent across three
  resource levels. `metabolic_pace` and `vision_range` cross at lean only and stay undecided. The
  original phrasing was "not here, not inert" and that is exactly what it turned out to mean - **the
  statement was about the sample size.**
- **`lifespan_tendency` collapses under scarcity** (+0.275 / +0.227 / +0.054) while
  **`fertility_investment` strengthens** (+0.059 / +0.060 / +0.097). Consistent across three levels,
  neither established. Fewer better-provisioned offspring when resources thin is the textbook
  direction, and it is the only trait whose effect grows as the world gets worse.
- **The level-of-detail seam is visible** where two depths meet. Removing it needs neighbour-aware
  morphing; the cheap fix was tried and measured worse.
- **908 chunks means 908 renderers** at ground level. Never profiled in Play mode. Merging finished
  chunks into fewer renderers is the batching fix if it ever matters.
- **Nothing in Play mode has been seen by me.** The camera, the planet view, the `J` panel fix and the
  tuning-drag performance are all verified by compile and by offline capture only.

### The three low-occupancy plant conclusions are UNVERIFIABLE

`p4-site-abundance-seed-production-rate-2026-08-20.md`,
`p4-low-occupancy-plant-route-audit-2026-08-20.md`,
`p4-low-occupancy-growth-trait-reaudit-2026-08-20.md` — banners stay.

Their scenario was never committed and cannot be recovered (no ZZZ probe was ever committed, so
none was ever deleted; the CSV and writeups give count, config, seeds, ticks and occupancy but never
coordinates). The calibrated replication reproduces occupancy **0.311** but grazes at
**0.00261 vs 0.00699 — a ratio of 0.373** — because its free-site pool sits outside the ±25 creature
arena and is never grazed. Placing 162 targets at non-saturating spacing *inside* the arena is
geometrically impossible.

Measured at that condition, for the record: `SeedProductionRate` **+0.00424, t +0.72, 64/120** (does
not replicate); `SeedlingResilience` contest-on−off **−0.00248, t −0.34, 53/120** (reversal not
demonstrated, but the +0.0362 advantage seen at 24 sites is **abolished**); the six growth-rate nulls
**hold**. `PlantEstablishmentContestEnabled` costs **19/120 extinctions** at low occupancy against
4/120 base.

**Do not attempt a fourth reconstruction.** If free-site abundance matters, re-derive it as a NEW
experiment with a committed scenario, in a geometry that fits inside the grazed arena.

### Lifespan-headroom claim: not adjudicated, control was confounded

`mortality-off` gives lifespan no channel but also removes site turnover and rewrites the regime:
the same comparison moved `Dispersal` **+0.0834 (t +21.40, 118/120)**, `NutrientUptake` **−0.0466
(t −7.62)**, `WaterEfficiency` **−0.0445 (t −8.61)**. Needs a lifespan-specific control that does
not exist.

### Terrain — open

- **The planet has adaptive level of detail** as of 2026-08-24 (`PlanetChunkedSurface`). See the
  section below. What is left of T6 is streaming cost, not detail.
- A small **stepped comb** remains on some steep ridges.
- ~~Ice cover looks high at 0.074 of surface~~ - **closed 2026-08-24, measured.** `TerrainProbe
  --ice`, seed 42 at the globe's band limit: **3.32% of the sphere, 20.9% of land, and 98.31% of it
  beyond 60 degrees.** Earth carries about 3% of its surface as permanent ice. Non-polar ice is 1.7%
  of the total and the highest of it sits at elevation 0.657 - mountain tops, which is where it
  belongs. The recorded 0.074 does not match 0.0332 and the quantity behind it is not stated; what is
  measured now is area share of the sphere at the resolution the planet is drawn at. **Nothing is
  wrong with the ice, and no coefficient was touched.**
- **Terrain is cosmetic.** Creatures are drawn on relief; a hill costs them nothing.

### Terrain — hypotheses tried and REFUTED, do not retry

Each of these was a confident diagnosis of the striped combs, and each changed nothing visible:

1. **Elevation clamping producing mesas** — fixed (0.81% pinned at 1.0 → 0.00%), render identical.
2. **Mesh-edge tapering** — the combs were not at the patch boundary.
3. **Vertex jitter** — made it visibly worse (shredded slivers at 0.75 cell); reverted, then deleted.
4. **Boundary landform width** — widened 3x, no change.
5. **Ambient light** — set and probe regenerated, no change.
6. **Self-shadowing** — shadows disabled, no change.
7. **"It is just level of detail"** — refuted by the planet-marked render: the two views were showing
   **different parts of the planet**.

The test that actually worked: **render the same mesh unlit.** The stripes vanished completely,
which separated shading from geometry in one image. It should have been first, not seventh.

### The spherical view — partly unverified, and known imperfect

- **`PatchLift` is 0.02 units and is probably far too small.** The backdrop globe is subdivision 5 -
  about **19-unit triangle edges at true scale**, roughly seven triangles under the whole 50-unit
  arena - and it samples elevation at ~13 cycles/radian against the patch's **965**. The two surfaces
  can disagree by more than 0.02, in which case the coarse globe pokes through the fine patch. Not
  seen either way; fix is a larger lift or a slightly smaller backdrop radius.
- The user reports it as "**a bit buggy**" without specifics. Not diagnosed.
- The pan clamp was a **box in x/z** and fought at planet distance. **Fixed by deletion** - the free
  camera has no focus to clamp.
- **Four bugs shipped in a row on this feature**, all from wiring behaviour into paths not read
  carefully: the terrain path most scenarios never take (`96990b8`), a `Camera.main` lookup that never
  resolves (`165cb8f`), no yaw at all (`56ba489`), and a focus that zooming never re-clamped
  (`b336b7d`). **Presentation has no headless check that means anything** - a human in Play mode is
  the only instrument.

### `GeneticClusterHistory` is split (2026-08-24)

1324 lines became 313 + 555 + 439 + 65, as partials:

| file | what |
|---|---|
| `GeneticClusterHistory.cs` | fields, `Record`, `ProcessTransition`, segment and track state |
| `.Events.cs` | emitting, holding back pending evidence, writing unresolved |
| `.Graph.cs` | **every method static, reads no field** - relations, strong components, ancestry |
| `.Pending.cs` | the two nested evidence classes |

**The previous note here was wrong and cost two sessions of not doing this.** It said the members do
not sit at class indent and the bulk is nested types, so a mechanical pass finds nothing. The nested
types are the last 55 lines and every member sits at class indent. `.Graph.cs` was the real seam:
nothing in it can touch a history's state.

563 tests green, Unity compile clean, no behaviour change.

### Unverified by me

The breeding-readiness inspector UI (`15c7a5a`) compiles and passes tests but was **never seen in
Play mode**. Layout at 324px with all optional trait rows showing is untested. The same applies to
the selected-creature history panel (`32900de`): its *model* is covered by ten headless tests, but
the panel itself has never been seen rendered. It was placed at (464, 300) in free space beside the
population-condition box specifically to avoid stacking more onto the untested inspector.

### Not measured

Per-tick resource request counts were never instrumented. The do-not-optimise decision uses
population as an upper bound — sound for that decision, but it is a bound, not an attribution.

---

## 4. Decisions that must NOT be reopened

### Added 2026-08-26

- **Selection work in the pressured cell runs BOTH health arms — flag on and flag off — every time.**
  Decided by the user on 2026-08-26 after the alternatives were laid out. `healthRecoveryEnabled`
  stays **off by default and is not being flipped**: no re-baseline is taken, and every recorded
  number stays comparable. **The reason both arms are run:** health has no regeneration path with the
  flag off (five subtractions in `NeedsSystem`, one addition behind the flag at
  `SimulationWorld.Ticking.cs:160`), and health is one of the three mate-seeking gate conditions, so a
  damaged creature is **permanently sterile**. At brake 1.5 / regen 2.0 / cap 500 that is **20.9% of
  the living**, which makes every drift figure conditional on a breeding subpopulation health already
  selected. Measured cost of not doing this: `fertility_investment` reads **9.48 with the ratchet and
  5.77 without** (-39%), and `body_size` crosses |t| = 2 **only** with it — it would have been
  reported as a finding. Evidence: `p6-the-gate-is-a-survival-mechanism-2026-08-26.md`.
  **One extra run per experiment. Do not drop the second arm to save it.**

### Added 2026-08-24

- **Rivers stay reverted.** Blocked behind a persistent grid; the postmortem is in
  `docs/terrain-caves-and-rivers.md`. Painted rivers cannot drain, erode or animate.
- **`Segments = 16`, `MaximumDepth = 6` on the chunk tree.** 32 with a cap of 5 was tried: same finest
  triangle, 908 chunks became 764, and **triangles went 232k to 782k** because raising the band limit
  makes the coarse chunks four times denser and those are most of the sphere. The numbers are in the
  comment on `Segments`.
- **`PatchLift` is fine at 0.02.** The patch and the deepest chunk produce *identical* elevation - the
  octave cap binds before either band limit does. `PlanetChunkSeamTests` fails if the octave cap is
  ever raised, which is when it would stop being true.
- **The ice is fine.** 3.32% of surface, 20.86% of land, **98.31% beyond 60 degrees**. No coefficient
  was touched and none should be.
- **`K` does not show a planet.** Its fourth cycle entry was the old single-mesh globe at draw radius
  60; `O` shows the real planet at true radius with level of detail. Do not add it back.
- **Do not use `--scenario=stable` or `=scarcity` in `CreatureSweep`.** Different scenario family,
  different calibration, 30 of 30 extinct in both arms.
- **Do not measure selection with a paired arm-against-arm design.** It cancels exactly.

1. **Soft home-range affinity is closed as a measured negative.** Flag stays default `false`; code,
   tests and key `R` stay; spec and plan carry SUPERSEDED banners. Do not tune
   `DefaultHomeRangeBonusMaximum`, the falloff distance or the learning fraction — the **sign** of
   the effect is wrong, not its size.
2. **The joint 70%/70% reproduction gate stays** (user decision). Reduced fertility while commuting
   is accepted as real ecology. Do not change `ReproductionSystem.CanReproduce`. No re-baseline is
   needed and every result on record stands. Separated scenarios must be calibrated to be viable
   *under* the gate.
3. **Resource allocation is not to be optimised** at current scales. Revisit only if populations in
   the thousands and site counts in the hundreds coincide.
4. **`ObservationShiftingPatches` needs no further placement or productivity calibration** — six
   variants are recorded and the joint gate explains why all failed.
5. **Do not build the P4a juvenile local-area bias as a fix for separated-resource extinction.**
   Juveniles are not the failing class and mortality is not the failure mode.
6. **Place memory stays inert.** Never wire `MemorySystem.ObservePlace`.
7. **Do not use the competition-off arm as a drift control.** It disables no trait.
8. **Elevation is SIGNED DISPLACEMENT from sea level, never a bounded 0..1 field.** A bounded range
   forces a clamp, a clamp forces a knee, and an interior sea level forces a branch at the waterline
   - three slope discontinuities, and a terrace is a slope discontinuity. Do not reintroduce
   `SoftSaturate`, `Clamp01` on composed elevation, or a `SeaLevel` constant inside the field.
9. **Plate properties must be blended between the two nearest plates.** A Voronoi lookup is
   piecewise constant; taking only the nearest plate puts a measured 0.825 step in the field.
10. **`TerrainMeshBuilder` is the single mesh/material/lighting path.** The capture and the runtime
    must not build scenes separately again - they drifted, and the PNGs became evidence about a mesh
    nobody was looking at.
11. **Measure before changing a terrain coefficient.** Reasoning about the field produced six wrong
    diagnoses; the instruments produced every answer.
12. **A band amplitude must sit under the slope ceiling, not on it.** `SlopeLimited` is a safety
    ceiling. Two bands both clipped to it sum to relief no ground has - measured median land grade
    0.243 against 0.085 for the planet bands alone. If a chosen amplitude is above the ceiling, the
    number in the source is fiction.
13. **No ambient settings in `Simulation`.** `PlanetTerrain.Sample` and friends **require** a
    `TerrainSettings` argument. While generation lived in Presentation a mutable static was the right
    trade for a slider panel; in Simulation it would be behaviour-changing state outside
    `SimulationConfig`, invisible to the configuration hash, so two worlds with equal hashes could
    diverge. The viewer's mutable instance lives in `Presentation.TerrainView` and can only affect
    what is drawn. **Making terrain tunable per world means putting the values in the config and
    hashing them** - a deliberate later step, not a convenience.
14. **The boundary landform must be crossfaded between BOTH candidate neighbours.** Kind and
    intensity belong to a pair of plates, so a rank swap is a discontinuity even where the seam blend
    has saturated. This is the same rule as decision 9, one layer down: **ranking is a lookup.**
15. **The simulation stays 2D; a round world is a display transform.** Positions are `SimVector2` on
    a flat 50-unit square with Euclidean distances. `ArenaProjection` maps them onto the sphere after
    the tick. Do not "fix" this by making the spatial model spherical without deciding to pay for it:
    distance stops being Euclidean, the perception grid has no drop-in equivalent, and **every
    recorded distance changes meaning**.
16. **Splits are mechanical or they do not happen.** Members are lifted whole by brace depth, nothing
    rewritten, no member changing class - so a split cannot alter behaviour and needs no reading. A
    split that requires understanding the file is a different, larger job.
17. **Terrain tunables live in `TerrainSettings`, not in `const` fields.** They are judged by eye
    against a one-metre creature at three zoom levels; that judgement cannot be made from source, and
    an edit-and-reload loop is why the previous round took fifteen passes. `PlanetTerrain.Active` is
    deliberately mutable global state - there is one terrain - and the explicit parameter on
    `Sample` exists so probes and tests can sweep without touching it.
12. **Safety-gated rendezvous is closed as "works, buys nothing."** Flag stays default `false`. Its
   effect is real and correctly signed; the ecology is starvation-limited, so it does not propagate.
   No pack architecture, no tuning. Reopen only in a predation-limited habitat.
9. **The three hashes stay three hashes.** V1 stays frozen and incomplete; V2 carries configuration;
   `BehaviorHash` never carries configuration. Merging any two of them breaks either a recorded
   baseline or `FlagLivenessAnalysis`, which would then report every flag as live.

---

## 5. Next task

### 2026-08-29 — fleeing is selected AGAINST, and a correction, read this first

`emergent-behaviour-fleeing-is-selected-against-2026-08-29.md`. **First simulation-code change in
this run of sessions.** 619 tests green (8 new); flag default false, flag-off byte-identical.

1. **CORRECTION: the block below names the wrong gene.** It calls `FearResponse` the Flee knob. Under
   `IntentUtilityV1` - which every cell ran - the flee score is `threatIntensity *
   genome.RiskAversion` (`Scoring.cs:96`). `FearResponse` is read only by `PredationSystem.Decide`
   (**Legacy-only**) and the place-memory penalty (**inert**). `fear`'s 1-of-18 meant *nothing reads
   it*, not *fleeing is unselected*.
2. **The conclusion inverts, and is sharper.** `risk_aversion` crosses |t| = 2 in **20 of 22** cells,
   **negative, -5.92 to +0.16**. **The gradient on defensive behaviour is strong and points the wrong
   way** - caution bred out at t = -3 to -6 while armour is bred in at +11.
3. **Built `evasiveFleeingEnabled`** - the one place a defender's *decision* reaches combat. Applied
   at resolution, deliberately **not** inside `PredationSystem.Threat`, which also feeds the decision
   path and would make a fleeing creature perceive less threat and stop fleeing.
4. **It works and it does not achieve its purpose.** At strength 0.5 and again at **4.0 - where
   fleeing cuts hit chance to about 12% - `risk_aversion` is still -6.44 / -5.09**, no better than
   the -3.44 baseline. **Not a tuning problem.**
5. **Why: one gene, two jobs, opposite signs.** `RiskAversion` scales the flee score (`:96`) **and**
   penalises food near threats (`:287`). The cell loses **44.8% of deaths to starvation against 8.4%
   to predation**, so the foraging cost outruns the combat benefit about five to one and the gene is
   selected out through *caution* however good *fleeing* gets.
6. **The actionable route is a representation change, not tuning.** Splitting flee propensity from
   foraging caution would let "flees when attacked, still forages boldly" exist - **today there is no
   value of `risk_aversion` that expresses it**, so evolution cannot find it however long it runs.
   That is a concrete instance of the whole emergent-behaviour question.
7. **A calibration bug the unit test caught:** `Phenotype.Maneuverability` is `1 + 2 * gene`, range
   **1.0 to 3.0**, not 0 to 1. The first formula assumed 0-1 and over-rewarded evasion by ~20 points.
8. **Not instrumented: flee frequency.** Everything in 4-5 is inferred from gene drift and the death
   mix. **That instrument is the obvious next build** and would test the five-to-one story directly.

9. **The flee-frequency instrument is BUILT, and it corrects item 6.** `FleeDecisionCount`,
   `DecisionCount`, `FleeingFraction` - counting only, **not hashed**, 623 green with four new
   including a herbivore negative control that must read exactly zero.
   **Fleeing is 38.2% of every decision taken.** Evasion at 4.0 cuts predation deaths **8.4% to
   2.3%** and starvation rises **44.8% to 48.9%**; population moves only 234 to 259 and
   `risk_aversion` selection gets *stronger* (-3.44 to -6.44).
10. **So the cost of caution is foraging TIME, and its benefit is capped by the predation share.**
    Making flight safer does nothing about time - and it lowers the ceiling on the benefit.
    **Route 1 (splitting the gene) is insufficient on its own**, contrary to item 6: the flee half
    carries the 38% time cost by itself. **Route 2, the mortality mix, is the necessary one.**
    Evolved defensive behaviour cannot pay while its cost is meals and predation is 8% of death.

**Next:** raise predation's share of mortality in the predation cell and re-measure `risk_aversion` -
that is the one prediction on the table, and the instruments to test it now all exist.

### 2026-08-26, final — the emergent-behaviour gradient, read this first

`emergent-behaviour-the-gradient-is-on-armour-2026-08-26.md`. Banner added to the constraints
document; nothing edited out of it.

1. **The world now selects hard and entirely on armour.** Eighteen powered predation cells:
   `defense` crosses |t| = 2 in **18 of 18** (+2.5 to +11.0) - and `Defense` is read **only** inside
   the attack success formula, changing **no decision at all**. `fear`, the gene that scales the Flee
   decision, crosses in **1 of 18. The `neutral_marker` control crosses in 2.** By this project's own
   standard the flee knob is indistinguishable from a gene nothing reads.
2. **The hunt knobs are selected against**, not merely absent - `attack` negative in 15 of 18,
   `aggression` in 4 of 18. **Evolved hunting cannot appear in this cell.**
3. **So: five named behaviours, zero positive gradients.** §1's last surviving support was attacked
   and **confirmed by a different route**. It is no longer "nothing kills you" - things kill you now.
   It is that **the cheapest answer to being killed is armour, and armour is free of behaviour.**
   **The binding constraint is the combat model's shape** - resistance is a scalar in a denominator,
   so nothing a creature *does* can compete with having more of it. That is where a design must bite,
   not the decision system.
4. **World-versus-brain is a crossover, now measured** - the matched 2x2 the rebuttal demanded, brake
   1.5, proximity pairing, gate 0.45, both health arms. Poor world/poor brain **0 / 0** extinct of 60;
   **brain alone 59 / 57**; **world alone 38 / 37**; **both 2 / 2**. **Enriching either side alone is
   worse than enriching neither.** Neither "world first" nor "brain first" - the question was
   malformed.
5. **Caveat that nearly cost me the reading:** the 0-of-60 corner is **not** a healthy world. It
   survives by pinning against the cap - **70.4% starvation, population 403, median 468 under a cap of
   500**, the exact pathology the carrying-capacity work removed. The rich/rich corner holds 234 at
   44.8% with the cap never binding. **A 2x2 read on extinctions alone would call the poorest world
   the best.**
6. **Untouched:** §2 (place memory blocks cache/territory/shelter), §3 (ablation, null controller,
   cross-seed) and §4 (determinism, flag-gating) of the constraints document all stand.

**Next:** this is now a design conversation, not a measurement. The evidence to argue from is in
place and the obvious first question is whether the combat success formula is the thing to change.

### 2026-08-26, last — the controller comparison redone cleanly, read this first

**Corrects this morning's controller document.** `p6-the-clean-controller-comparison-2026-08-26.md`.
A banner is on `p6-the-controller-comparison-2026-08-26.md`; nothing there was deleted.

1. **The mate gate is replaceable by a scenario parameter, and bettered by it.**
   `gradedFertilityEnabled` is already a density brake and the arm that died 50 of 60 this morning
   **already had it, at strength 1.5** - the wrong number, not a missing mechanism. Intent with
   **proximity pairing and no mate gate** at **brake 4.0 is 0 of 60 extinct, population 238, energy
   0.657**, against the mate gate's 3 of 60, population 299, energy 0.583.
2. **So the comparison this morning called impossible is possible.** Matched pairing, matched brake,
   only the controller differing. **At every matched brake the rich controller carries five to
   fifteen times the population at comparable per-creature energy** - at brake 4.0, **238 against 24**
   with energy 0.657 against 0.671, and 0 of 60 extinct against 4 of 60.
3. **"On survival it is a tie" is WITHDRAWN.** That tie compared each controller on a different
   pairing rule at a brake tuned for neither. **"Its advantage is a brake" survives but is reframed:
   the brake is a precondition for the controller paying, not what it was buying.** Best-against-best
   is intent 238 to legacy 144 - 1.65x - because legacy's own optimum sits at a much weaker brake.
4. **First direct evidence on "enrich the world before enriching the brain", and it cuts both ways.**
   The one thing the rich brain was demonstrably buying was purchasable from the world, better and
   cheaper - so building brain-for-a-brake would have been wrong. But **given** that brake the rich
   brain is worth an order of magnitude of carrying capacity, which no world parameter tested
   supplies. **Enriching the brain without the world was actively harmful; the world alone leaves 10x
   on the table.**
5. **A guess made and withdrawn inside the same session:** from brake 3.0 and 6.0 alone I proposed
   the gate reached a middle no brake could. Filling in 4.0, 4.5 and 5.0 deleted it.
6. **Deliberately not claimed: drift at matched brake.** Populations of 238 and 24 do not produce
   comparable selection tables, and this session already established that small populations select
   weakly. **Brake 4.0 is a number for this cell only** - graded-fertility strength is known to be
   scenario-specific.

**Next:** the emergent-behaviour design question (item 6 below) now has evidence to be argued from
rather than around. Targeting-versus-success-rate on low-defense prey was assessed and **deliberately
skipped as having nothing downstream**.

### 2026-08-26, latest — the predation cell is robust, read this first

**Closed: the robustness check the previous block called the first thing to do.**
`p6-the-predation-cell-is-robust-2026-08-26.md`. Four axes, one step either side of the baseline,
60 runs per cell, both health arms — twenty cells.

1. **The whole predator-prey result travels.** `defense` drift is **positive in all twenty cells**,
   +2.48 to +10.97, across cap 250-1000, regen 1.5-3.0, brake 1.0-3.0 and gate 0.40-0.55.
   `attack` is negative in nineteen of twenty. **It is not an artefact of the cell it was found in;
   work can be built on this cell.**
2. **The cell is a plateau.** Nothing one step from the baseline collapses it. Only the gate can, and
   smoothly — 6 / 10 / 24 / 52 extinct of 60 across gate 0.40 / 0.45 / 0.55 / 0.65. Same accelerating
   curve the no-predation dose-response found, not a cliff.
3. **The cap is inert, proven by hash equality.** Cap 500 and cap 1000 give **byte-identical hashes
   on all 60 runs**; the population tops out at 486 and never touches either ceiling. Cap 250 does
   bind and still changes nothing (10 of 60 either way). **The brake and predation regulate this
   cell; the cap does not.** `p6-the-cap-is-the-stabiliser-2026-08-24.md` no longer describes this
   substrate — closing the carrying-capacity debt is what removed it.
4. **Gate 0.65 is excluded from every claim** — 5 and 6 surviving runs of 30. Its `defense` +1.2 is
   not evidence selection stops and its `attack` -25 is not evidence it intensifies. Recorded so
   nobody re-derives it and believes it.
5. **The baseline is not the best point.** Gate 0.40 gives 6 of 60 extinct and regen 3.0 gives 2,
   against the baseline's 10. Free statistical power for a future experiment, but both change the
   ecology and must be stated rather than silently adopted.
6. **Unexplained, and no mechanism offered:** `defense` t varies 2.5 to 11 and tracks neither the
   surviving-run count nor predation exposure (non-monotone: 6.7 attacks per run gives +10.97, 18.9
   gives +7.68, 3.1 gives +2.48). The two lowest values are the two smallest populations. That is
   all that can be said.
7. **A trap in the sweep's own output, recorded:** when t <= -10 with a negative mean, the mean and t
   columns run together (`-0.2729-18.4080`) and splitting on whitespace reads the *next* column as t.
   It produced a "+87.68" that was really -18.41. Parse that table with a number regex.

**Followed up the same day, in the same document.**

8. **What sets the size of `defense` selection is now a measured negative, not an open gap.** The
   **selection differential is near-constant** - the predated carry `defense` 0.18-0.34 and the
   living 0.61-0.73 in all eighteen cells, a differential of 0.335 to 0.512 against a t range of
   2.5 to 11, rho **+0.08 / +0.33**. **Predation kills the same kind of creature everywhere**, so the
   variation is not variation in how selectively it kills. That retires the obvious explanation.
9. **Population and turnover are withdrawn as arm-conditional.** Rho of population against t is
   **+0.83 with health recovery on and +0.30 with it off**; births give +0.80 and +0.15. An
   inverted-U in the health-off arm does not exist in the health-on arm. **One arm alone would have
   produced a confident finding and the second deleted it.** Only "populations under about a hundred
   select weakly" replicates.
10. **Correction to item 3, made the same day.** The cap hash-equality holds on the *relief-selected*
    seed set the sweep uses. On the contiguous seed set `--deaths` uses, cap 1000 reaches population
    **505** against cap 500's **500**, so at least one run there is clipped. "The ceiling is never
    touched" was true of the sweep's seeds, not of every seed. **The conclusion is unaffected** - cap
    250 binds hard and changes nothing - but "never" was too strong and is qualified in the document.

**Next, in order:** targeting versus success-rate on low-defense prey; then the mate-gate
separability question the controller comparison opened.

### 2026-08-26, later — the controller comparison, read this first

**One commit on top of `9631800`.** Nothing below was deleted. One bullet in the previous block's
"what is worth doing next" is struck through; everything else there stands.

**Closed: the controller comparison.** `p6-the-controller-comparison-2026-08-26.md`. Both cells, both
health arms, 60 runs per arm per cell.

1. **The comparison cannot be made single-variable, and this is a property of the code, not of the
   experiment design.** `mateSelectionEnabled` reproduces through `FindSeekMateTarget`, which
   requires the decision to *be* `CreatureAction.SeekMate`; `DecideFromLearnedOutcomes` emits only
   `SeekFood`, `SeekWater` and `Wander`. **A Legacy world with mate selection on has zero births by
   construction.** The first arm run was 8 of 8 extinct in 1.5 seconds and measured nothing but that.
   `--mate-selection=off` was added to give both controllers a pairing rule they can reach.
2. **Herbivore cell (cap 500, regen 2.0, brake 1.5, gate 0.70): on survival it is a tie** - intent
   3 of 60 extinct against Legacy 5, and 1 against 1 with health recovery on. Intent carries **twice
   the population** (299 against 144) and loses **nobody** to thirst against Legacy's 2.4%.
3. **The rich controller's advantage there is a brake, not foraging skill.** Give it Legacy's
   proximity pairing and it **starves itself out in 50 of 60 runs** - 623 births per run, 69%
   starvation, mean energy 0.03. `SeekMate` is what limits its birth rate.
4. **Predator-prey cell (`--predation --gate=0.45`): the ordering inverts.** Legacy is worst at
   **38 of 60 extinct** against intent's 10, and the configuration that was catastrophic in cell 1 is
   **the best here, 2 of 60**. Predation supplies the mortality that starvation had to supply before.
5. **Legacy dies of its own predation** - 230 attacks per run, 33% of deaths, against 1.0% for
   intent. `PredationSystem.Decide` overrides the Legacy decision; intent scores hunting against
   every other intent.
6. **`urgency_exponent` selection exists only under one controller.** t = **-11.53 / -10.11** under
   intent, **-1.34 / +1.23** under Legacy - the neutral control's own range. Predicted from the
   source before the table was read: the gene's only behavioural readers are two lines in
   `DecisionSystem.Scoring.cs`, the intent path. **Under Legacy nothing reads it.**
7. **A trap in the drift table, recorded:** all six combat genes drift +0.040 to +0.048 at t = 13 to
   27 **in both controllers**. Founders are exactly `0.0000` under `PhysiologyVariation` - that is
   mutation off a floor, not selection. Combat questions need the `--predation` founder profile.
8. **Replication, unplanned:** the committed
   `...30seeds-predation-brake1.5-gate0.45-2026-08-26.csv` reproduced **byte-for-byte** at `9631800`
   against the copy written at `a065905`. Its `-healthrecovery` companion was rewritten from
   `d99b854`: every shared column and every hash identical, plus the `maneuverability` and `fear`
   columns that did not exist then.

**Tooling added:** `--policy=legacy|intent` and `--mate-selection=off` on `CreatureSweep`, both
suffixed into the corpus filename. No simulation code was touched; 611 green.

**What is worth doing next, after this**

- **Is the mate gate separable from the controller?** Proximity pairing plus an explicit birth-rate
  brake would test whether intent's cell-1 advantage survives without `SeekMate`. Nothing offers that
  today.
- **Attack rate is monotone in mean energy across all six predation arms**, and `hunt` is multiplied
  by hunger against a hard `>= 0.10` floor in both controllers. Direction is code-grounded; the 40x
  magnitude is not. A resource-level sweep with the controller fixed would measure it.
- **Everything below still stands**, minus the struck bullet.

### 2026-08-26 — read this first; the list below it is the previous session's

**Thirteen commits, `fd6eca8` to `0914369`.** Nothing below this block was deleted; several entries in
it are now qualified by banners on the documents they cite.

**Closed today**

1. **Plant corpus re-run unpinned** (the item the session opened on) —
   `p6-plant-corpus-revalidated-unpinned-2026-08-26.md`. Contest and join nulls hold with the
   population free; qualification lifted **for those two comparisons only**.
2. **The patch-quality channel is not free** — `p6-patch-quality-is-not-a-free-parameter-2026-08-26.md`.
   Off vs on: population **-31.9 (t -6.42)**, occupancy **+0.201 (t +10.69)**, 0 of 240 hashes matching.
3. **Starvation is a dial** — `p6-starvation-is-a-dial-2026-08-26.md` and
   `p6-the-pressured-cell-is-a-plateau-2026-08-26.md`. 49.6% to 0.0% of deaths on two configuration
   values; **brake 1.5 / regen 2.0 / cap 500 survives 30 of 30 with 16.2% starvation**, and nine
   surrounding cells behave the same, so it is a plateau not a knife-edge.
4. **The mating gate is also the density brake** — `p6-the-gate-is-a-survival-mechanism-2026-08-26.md`.
   Survival **4 / 11 / 24 / 38 of 40** across gate 0.45-0.70 when the ecology limits the population,
   against **40 of 40** at cap 100. The recorded dose-response could not see this; it is banner-qualified.
5. **The predation founder profile was broken and is fixed** —
   `p6-a-survivable-predator-prey-scenario-exists-2026-08-26.md`. It set six of twenty-four traits and
   left the rest at the constructor's `0f`. **Fixing it was necessary and not sufficient**: the real
   blocker was the gate. **`--predation --gate=0.45` in the pressured cell is the first survivable
   predator-prey scenario this project has had** — 24-26 of 30, 146 births per run.
6. **Both stuck flags adjudicated** — `p6-two-flags-adjudicated-at-last-2026-08-26.md`.
   `kinRecognitionEnabled` off costs **29% of the population**; `multiThreatPerceptionEnabled` off
   diverges **60 of 60** hashes. **`KnownInertFlags` deliberately unchanged** - see the field-notes
   entry for why.
7. **Predation selects** — `p6-predation-selects-on-defense-2026-08-26.md`. **defense +0.267,
   t +10.97**; attack **t -3.84**; both health arms agree. The drift table had no combat genes at all
   until today, so the question had never been askable.

**Standing decision (section 4):** selection work in the pressured cell runs **both health arms**.

8. **Both instrument gaps closed, and the result got worse rather than better** —
   `p6-the-combat-forces-are-too-small-2026-08-26.md`. `maneuverability`, `fear` and
   `CumulativeCombatDamage` are now in `SimulationStatistics` (appended at the end of the positional
   constructor, behaviour-inert, 611 green). The two newly visible genes are **not** strongly selected
   (t +2.00 and +0.73), so **the predation result is about `defense` alone.** And the measured combat
   forces are small - **0.53 predation deaths per run** against ~65 deaths from all causes, **96.6
   health of combat damage per run** across a population near 130. **Both mechanisms I proposed are
   withdrawn and no third is offered: defense selection is robust and unexplained.**

9. **And the open question is answered** — `p6-death-is-concentrated-on-the-low-defense-tail-2026-08-26.md`.
   `MeanDefenseAtDeath` and `MeanDefenseAtPredationDeath` added. Mean `defense` of the **predated** is
   **0.2479** against **0.7190** for the living and 0.489 for the founders. **Predation kills rarely
   and kills almost only the weakest-defended**, which is how 0.53 deaths per run produce t = 11.
   Mechanism: **selective mortality on the low-defense tail** - measured, not inferred. The "all dead"
   column is explained by timing and no claim is made from it.

**What is worth doing next**

- ~~The controller comparison has still never been run~~ **RUN, 2026-08-26 (later session)** - see
  the block at the top of this section and `p6-the-controller-comparison-2026-08-26.md`.
- **Nine plant corpora are still measured pinned.** Only contest and join were re-validated.
- ~~The slope arms in every predation run were never examined~~ **CHECKED, null** - 168 paired columns
  across eight cells, **3 at |t| >= 2 against a chance expectation of 8.4**. The slope cost does not
  interact with combat; the predation drift tables are safe to read as single-arm measurements.
  Appendix in `p6-defense-selection-is-robust-and-my-mechanism-was-wrong-2026-08-26.md`.
- **Not separated:** whether attackers target low-defense creatures or merely succeed against them
  more often. `PredationSystem` makes success-rate concentration sufficient, so targeting need not be
  invoked - but they are not distinguished.

- **The whole predation result lives at gate 0.45, cap 500, regen 2.0 — one cell.** Robustness across
  gate, cap and regeneration is the first thing to check before anything is built on it.
- **"Losing a fight sterilises rather than kills" is a hypothesis.** Predation is 1-2% of deaths yet
  defense is under t = 11 selection, and the proposed route is health damage feeding the mate gate.
  Nothing reports the attack-damage path directly.
- **`maneuverability` and `fear` are invisible to every instrument** - no `SimulationStatistics` mean
  exists for them, so two of the six combat genes cannot be measured at all.
- **The plant corpus's other nine corpora are still measured pinned.** Only contest and join were
  re-validated.
- **Do not restart rivers.** Unchanged.


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

## 8. Working-tree rules

Intentionally untracked: Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. **Never stage or delete them. Never `git add -A`** —
add named files only. Delete `Assets/Tests/EditMode/ZZZ*.cs` probes before committing; none exist at
handoff.

---
