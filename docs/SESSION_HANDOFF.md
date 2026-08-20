# Session Handoff — 2026-08-18

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
Both at 3899685, 385/385 green, tree clean.

=== WHERE THINGS STAND ===

The P4 blocker from p4-lifespan-derived-2026-08-17.md is RESOLVED. Plant defense
declines only when grazers are present (cap-0 control flat, t -0.05; grazed arms
CI excludes zero). That is the positive control the blocker demanded, so a
coevolution null is now interpretable.

P4's EXIT GATE is not met and will not be met by tuning. Defense has no route to
rise: it costs growth, protects no tissue (ConsumeAt ignores it), and provokes
compensatory feeding, so grazing selects it away. Giving defense a positive
fitness route is a DESIGN decision, not a calibration.

Read in order, and note several docs carry supersede or retraction banners —
read those before trusting any conclusion:
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

Play mode: H cycles ground overlay temperature -> biome -> off. N (all-flags
playtest) enables procedural fields so there is terrain to see.

=== OPEN WORK, ROUGHLY BY VALUE ===

1. Plant tolerance genes now respond to selection, DOWNWARD. Measured: flat
   environment gives no response (t 0.29 / 0.64); procedural fields give
   -0.086 on both MoistureTolerance and TemperatureTolerance (t -2.45, -2.57,
   CIs exclude zero, 8 up / 22 down and 10 up / 20 down). Four comparisons puts
   the Bonferroni bar near 2.6, so both sit just under — suggestive, not
   established, and I did not write it up. Worth confirming and understanding:
   the likely reading is that tolerance is insurance for bad ground, and the
   population concentrates on good ground where it is pure cost.

2. Elevation field, waiting on a spec from ChatGPT. See docs/sphere-sandbox-prompt.md.
   Slots in as a fourth channel on EnvironmentSample. The FIELD can land and be
   measured long before the world is round; sphere geometry is a separate and much
   larger job (every position is a 2D SimVector2, arena hardcoded (-25,25),
   perception on a uniform 2D grid).

3. Four config flags are inert because their readers sit on the Legacy path, which
   no configuration reaches: foragingEconomicsEnabled, kinRecognitionEnabled,
   learnedResourceQualityEnabled, multiThreatPerceptionEnabled. Wire or delete —
   nobody has decided.

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

Append any new lesson to AGENT_FIELD_NOTES.md §5 before the session ends.
```
