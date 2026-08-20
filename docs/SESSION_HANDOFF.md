# Session Handoff — 2026-08-19

Paste the block below to start a fresh session. Everything it needs is in the repo; the prompt's
job is to point at the right files and stop the next session rediscovering what this one paid for.

**Model:** Opus for measurement and judgment work — this session nearly shipped two false results,
both caught by noticing an inconsistency rather than by following a procedure. Sonnet is fine for
mechanical implementation against a settled spec.

---

```
Continue LifeSimulation. Read docs/AGENT_FIELD_NOTES.md first — file map, live/dead
mechanism ledger, and the accumulated lessons. Do not re-read the repository; the map
exists so you don't have to.

Scratch clone for git/CLI:
  C:\Users\sawye\AppData\Local\Temp\claude\C--Users-sawye-OneDrive-Claude-Code-Roblox-Game\d728dc93-e79a-4320-809d-9f6495c4f1df\scratchpad\ls-check
Unity checkout (Play mode only):
  C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim
Same remote, separate trees — pull the other after every push.
Tests: cd tools/HeadlessTests && dotnet test
Both at a081d28, 393/393 green, tree clean.

=== WHERE THINGS STAND (updated 2026-08-19) ===

THE ONE THING TO KNOW: plant traits acting on GROWTH RATE are close to
unselectable, and no adaptation term fixes that. growth is multiplied by
(1 - Biomass/Capacity), whose measured mean is 0.1711, with 39.8% of patch-ticks
within 1% of capacity. Every trait routed through GrowthRateMultiplier - Nutrition,
Defense, WaterEfficiency, both tolerances, NutrientUptake - measures null or weak.
The two that ARE strongly selected, Dispersal (t 14-17) and SeedInvestment (t 4.8-6.8),
are the only ones acting on colonisation instead.
  docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md

Three sessions have now tried to make a growth-rate trait selectable by improving its
benefit channel. The benefit channel was never the constraint. DO NOT RUN A FOURTH.
Selection on plants has to act on establishment, mortality or seed production - a
DESIGN decision nobody has taken.

Dispersal is the positive control to read any plant-trait null against: +0.098 to
+0.125, t 14-17, 105-115 of 120 seeds up, four arms, 0/120 extinct, ~15 plant
generations. Unlike the 2026-08-18 defense result it is not fragile and does not come
from collapsing runs.

The P4 EXIT GATE is still not met, and the reason is now structural rather than a
calibration problem. Defense has no route to rise, and neither does any other
growth-rate trait.

Read in order, and note several docs carry supersede or retraction banners —
read those before trusting any conclusion:
  docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md (the big one)
  docs/experiments/p4-fertility-binds-the-growth-limit-2026-08-19.md (why the Min matters)
  docs/experiments/p4-plant-trait-selection-nonreplication-2026-08-19.md (tolerance did not replicate)
  docs/experiments/p4-inert-flags-readjudicated-2026-08-19.md (2 of 4 flags are wired, not dead)
  docs/experiments/p4-elevation-field-2026-08-19.md (elevation, and the 2 conditions it needs)
  docs/experiments/p4-grazing-dose-response-2026-08-18.md   (the positive control)
  docs/experiments/p4-dose-curve-2026-08-18.md              (operating point, 1 refuted hypothesis)
  docs/experiments/p4-scale-and-closing-position-2026-08-18.md (closing position, 2 more refuted)
  docs/experiments/p4-defense-selection-demonstrated-2026-08-18.md (RETRACTED — read the banner)
  docs/experiments/plant-gene-liveness-2026-08-18.md        (perturbation detects influence, not reward)

=== WHAT EXISTS NOW THAT DID NOT BEFORE ===

Three perturbation harnesses. Use these instead of grepping for callers; a
caller-search has produced a false clean bill of health three times here.
  Diagnostics/GeneLivenessAnalysis.cs       animal genes
  Diagnostics/FlagLivenessAnalysis.cs       config flags, reflection-driven
  Diagnostics/PlantGeneLivenessAnalysis.cs  plant genes
  Diagnostics/LivenessRecorder.cs           code paths that run on empty data
LivenessTests + PlantLivenessTests enforce the §4 ledger, so it cannot rot silently.

Measurement: SimulationStatistics.RealizedGrazingPressure, plus PlantBiomassSeconds
and PlantPatchSeconds behind it. Report it alongside any plant-trait result.

Flags added this session, all defaulting false:
  plantDefenseDeterrenceEnabled       defense reduces biomass stripped per bite
  plantQualityPreferenceEnabled       patch choice weights nutrition density
  plantTemperatureAdaptationEnabled   TemperatureTolerance can be paid for
  proceduralEnvironmentFieldsEnabled  real moisture/fertility/temperature

Environment: EnvironmentNoise (3D value noise on DeterministicRandom, sphere-sampled)
and EnvironmentField.CreateProcedural. Replaces a linear moisture ramp and two
constants pinned at 1.

Added 2026-08-19:
  PlantGenome.NutrientUptake        ninth plant trait; TraitCount is now 9, and the ninth
                                    constructor parameter has a DEFAULT - the shape that once
                                    silently dropped persistence - so
                                    EveryPlantTraitTransmitsThroughCloneMutated pins it
  plantFertilityAdaptationEnabled   fertility gets an adaptation term; gates the gene's COST
                                    as well as its benefit, so flag-off is byte-identical
  EnvironmentNoise.RidgedFbm        folded, octave-weighted noise for terrain chains
  EnvironmentSample.Elevation       + a .45 lapse rate onto temperature
  elevationFieldEnabled             both default false

Careful with the last two: elevation is INERT under the standard P4 plant config. It needs
cap=1000 AND plantFertilityAdaptationEnabled together before it changes anything.

Play mode: H cycles ground overlay temperature -> biome -> off. N (all-flags
playtest) enables procedural fields so there is terrain to see.

=== OPEN WORK, ROUGHLY BY VALUE ===

1. CLOSED 2026-08-19. The tolerance result did NOT replicate — see
   docs/experiments/p4-plant-trait-selection-nonreplication-2026-08-19.md. At n=120
   both tolerances sit at zero under procedural fields (t 0.08 and 0.29, CIs
   [-0.013,+0.015] and [-0.012,+0.016]), with sign counts at chance in all arms, and
   the null holds at a doubled founder variance too. -0.086 is far outside every CI.
   Scoped to a transcribed config; the original config was never recorded, so this is
   a non-replication under a documented setup, not proof the original was wrong.

   Two things came out of it that matter more than the tolerance question:
   - Dispersal is a STRONG positive control: +0.098 to +0.125, t 14-17, 105-115 of
     120 seeds up, in all four arms, 0/120 extinct, 15 plant generations. Read any
     future plant-trait null against it. SeedInvestment is a weaker second (t 4.8-6.8).
     Both are the traits with no growth-rate cost term in PlantPhenotype.
   - The "flat environment control" is not flat. With plantCohortsEnabled on,
     SimulationWorld builds CreateMoistureGradient(); moisture already varies and only
     fertility and temperature are pinned at 1. Flipping proceduralEnvironmentFieldsEnabled
     moves three channels at once.

   Open thread, deliberately not claimed: Defense under procedural+wideTolerance is
   -0.0225, t -3.04, 71/119 down — one cell of 32, under a Bonferroni bar near 3.2, and
   the same claim shape that was retracted on 2026-08-18.

1b. READ BEFORE ITEM 2: docs/experiments/p4-fertility-binds-the-growth-limit-2026-08-19.md.
   Fertility binds the growth limit 82-90% of the time at plant-reachable positions, and that
   share RISES as tolerance rises, because each adaptation term lifts its own channel out of
   contention for the Min. Elevation couples to temperature and moisture — channels that
   already vary and already lose the minimum to fertility — so it will NOT make the tolerance
   genes selectable. Build elevation for terrain and P6 groundwork, not for that.
   The fertility adaptation term is now BUILT (NutrientUptake, PlantFertilityAdaptationEnabled,
   default false, flag-off byte-identical, 389/389). It works as designed and barely matters,
   for a reason that supersedes this whole line of work:

1c. THE STRUCTURAL FINDING, read before proposing any plant-trait experiment:
   docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md.
   growth is multiplied by (1 - Biomass/Capacity); the measured mean of that gate is 0.1711,
   with 39.8% of patch-ticks within 1% of capacity. Every trait routed through
   GrowthRateMultiplier - Nutrition, Defense, WaterEfficiency, both tolerances, NutrientUptake -
   is null or weak. The two that ARE strongly selected, Dispersal and SeedInvestment, are the
   only ones acting on colonisation instead of growth rate.
   Three sessions have now tried to make a growth-rate trait selectable by improving its
   benefit channel. The benefit channel was never the constraint. DO NOT RUN A FOURTH.
   Selection on plants has to act on establishment, mortality or seed production - a design
   decision nobody has taken yet.

2. DONE 2026-08-19 — docs/experiments/p4-elevation-field-2026-08-19.md.
   EnvironmentNoise.RidgedFbm, EnvironmentSample.Elevation, a .45 lapse rate onto temperature,
   behind ElevationFieldEnabled (default false, flag-off byte-identical, 4 tests, 393/393).
   Mean elevation 0.3948 at plant positions; temperature 0.6593 -> 0.4816. Calibration
   untouched: 0/30 extinct either way.
   IMPORTANT SCOPING: elevation is INERT under the standard P4 plant config. It goes live only
   when cap=1000 (grazing pulls patches off capacity) AND plantFertilityAdaptationEnabled is on
   (fertility stops binding the Min). Neither alone is enough. Enabling it at cap 48 does
   nothing at all, and the survival table will look reassuringly unchanged while nothing happens.
   NOT DONE, deliberately: the rain shadow (needs a wind-direction convention - a design choice),
   and an elevation mode on the H overlay (Unity presentation code, cannot be verified headlessly).

   Original notes, still accurate for the sphere half: — the other three fields were
   designed in-repo and elevation is the same job. Ridged multifractal
   (accumulate 1 - |noise|) for mountain chains rather than plain fBm, a lapse rate
   coupling it to temperature, optionally a rain shadow coupling it to moisture.
   Slots in as a fourth channel on EnvironmentSample, behind a flag like everything
   else. docs/sphere-sandbox-prompt.md exists if an outside visual opinion is ever
   wanted, but it is optional and nothing waits on it.

   The FIELD can land and be measured long before the world is round. Sphere
   GEOMETRY is a separate and much larger job: every position is a 2D SimVector2,
   the arena is hardcoded (-25,25), and perception runs on a uniform 2D grid, so a
   round world is a spatial-model refactor threaded through movement, distance,
   dispersal, site placement and spatial hashing while preserving determinism and
   every hash baseline. P6/P7, gated behind P4 and P5.

3. RE-ADJUDICATED 2026-08-19 — the framing was wrong for half of them. See
   docs/experiments/p4-inert-flags-readjudicated-2026-08-19.md.
   - foragingEconomicsEnabled and learnedResourceQualityEnabled ARE Legacy-only and are the
     genuine wire-or-delete candidates. (foragingEconomics also runs two per-tick foraging
     updates on the live path whose output nothing there consumes — inert, but not unwired.)
   - multiThreatPerceptionEnabled and kinRecognitionEnabled are ALREADY WIRED into
     DecideIntentUtilityV1: the first selects ScorePredationMulti vs ScorePredation, the
     second is read inside both. They read inert only because every use site sits inside
     if (predationEnabled) and the pinned sweep runs a herbivore scenario with no threats.
     DO NOT DELETE THEM.
   Blocker: there is no survivable predator-prey scenario. FounderProfile.PredationVariation
   is extinct before 3,000 ticks with zero births on the plant calibration, so a liveness
   verdict measured there is worthless in both directions. Building that scenario is the real
   task behind this item, and it is closer to P5 species work than to P4.

4. Place memory is still unwired, deliberately, and now enforced-inert by test.
   Wire-or-delete deferred to P5.

=== METHOD RULES THAT COST THE MOST TO LEARN ===

- Seed founders with VARYING values. A uniform founder value gives zero standing
  variance and every result is drift. This cost four sweeps.
- Compare an effect against its OWN sampling error (SD, SE, bootstrap CI), never
  against another arm whose spread is small for structural reasons. That error
  produced a retraction the same day.
- Read world.Statistics AFTER stepping. It is stale before the first Step, so a
  tick-0 baseline silently makes delta equal the final value. That produced a
  spectacular fake result (+0.29, t 9.45) caught only by noticing the "delta"
  equalled a known endpoint.
- A green dotnet test does not prove Unity compiles. Check for global usings first
  if the editor disagrees.
- Never run perl -0pi over the docs; it mangles UTF-8. Use the Edit tool.
- TRANSCRIBE the measured config, including the engine bounds. maximumPopulation is 48 in
  the plant calibration and 1000 from the factory; rebuilding the config by hand and taking
  the factory default collapses the scenario 30/30 and looks exactly like a behavior
  regression. Copy it from the committed guard
  (ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds).
- PRINT THE SURVIVAL COLUMNS ON EVERY ARM, including arms that appear to confirm what you
  expected. A flag "going live" under PredationVariation founders turned out to be measured
  on a population that was already extinct with zero births.
- Report the SIGN TEST next to the t. A t of -2.05 with 50/120 seeds down is magnitude-driven
  noise, and that is the exact shape of the claim retracted on 2026-08-18.
- Regress FINAL on FOUNDER, never delta on founder — the latter puts the founder mean on both
  sides with opposite signs and manufactures a slope out of noise. Slope 1 is drift, 0 is
  convergence, and the flag-off control should land on 1 (it read 0.9995 +/- 0.0412).
- Bit-identical output across an arm flip means DEAD, not "small effect". Check the state hash
  before interpreting any difference, especially when aggregates look reassuringly unchanged.

Append any new lesson to AGENT_FIELD_NOTES.md §5 before the session ends.
```
