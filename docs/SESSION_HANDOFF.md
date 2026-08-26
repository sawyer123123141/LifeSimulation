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

**Do not restart rivers.** Still blocked behind a persistent grid.

## 8. Working-tree rules

Intentionally untracked: Unity `.meta` files, `Assets/_Recovery/`, and
`ProjectSettings/PackageManagerSettings.asset`. **Never stage or delete them. Never `git add -A`** —
add named files only. Delete `Assets/Tests/EditMode/ZZZ*.cs` probes before committing; none exist at
handoff.

---
