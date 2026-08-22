# Session Handoff — 2026-08-21

Paste the block below to start a fresh session. Everything it needs is in the repo; the prompt's
job is to point at the right files and stop the next session rediscovering what this one paid for.

**Model:** Opus for measurement and judgment work. The 2026-08-19 session refuted **seven** of its
own hypotheses by measurement, several of which had already been written up as confident prose;
the ones that survived were the ones stated with numbers before they were believed. Sonnet is fine
for mechanical implementation against a settled spec.

---

```
Continue LifeSimulation. Read docs/AGENT_FIELD_NOTES.md first — file map, live/dead
mechanism ledger, and the accumulated lessons. Do not re-read the repository; the map
exists so you don't have to. Then read this file's OPEN WORK and METHOD RULES sections.

Scratch clone for git/CLI:
  C:\Users\sawye\AppData\Local\Temp\claude\C--Users-sawye-OneDrive-Claude-Code-Roblox-Game\d728dc93-e79a-4320-809d-9f6495c4f1df\scratchpad\ls-check
Unity checkout (Play mode only, and the user compiles it there):
  C:\Users\sawye\OneDrive\Documents\ChatGPT\life sim
Same remote, separate trees — pull the other after every push.
Tests: cd tools/HeadlessTests && dotnet test
Both trees on main, 398/398 green, trees clean. Run git log --oneline -1 for the head.
Unity editor compile confirmed by the user on 2026-08-20, covering the tenth and eleventh
plant genes and both new flags. A green dotnet test does not prove Unity
compiles — if the editor disagrees, check for global usings first.

=== THE ONE THING TO KNOW ===

Plant traits acting on GROWTH RATE are close to unselectable, and no adaptation term
fixes it. growth is multiplied by (1 - Biomass/Capacity); that gate's measured mean is
0.1711, with 39.8% of patch-ticks within 1% of capacity. So a trait changing growth rate
by X% changes realised growth by ~0.17X, and by ~nothing two fifths of the time.

Every trait routed through PlantPhenotype.GrowthRateMultiplier measures null or weak:
Nutrition, Defense, WaterEfficiency, MoistureTolerance, TemperatureTolerance, NutrientUptake.
The two that ARE strongly selected act on COLONISATION instead and skip the gate entirely:
Dispersal (t 14-17, 105-115 of 120 seeds up) and SeedInvestment (t 4.8-6.8).

THREE SESSIONS have now tried to make a growth-rate trait selectable by improving its
benefit channel — defense deterrence, temperature adaptation, fertility adaptation. The
benefit channel was never the constraint. DO NOT RUN A FOURTH.
  docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md

Dispersal is the POSITIVE CONTROL. Read every plant-trait null against it. Unlike the
retracted 2026-08-18 defense result it is not fragile and does not come from collapsing
runs: four arms, 0/120 extinct, ~15 plant generations.

=== THE P4 BLOCKER IS ANSWERED: the route is ESTABLISHMENT ===

Closed 2026-08-20. PlantEstablishmentContestEnabled (default false) lets a seedling below
VulnerabilityFraction resist takeover with PlantGenome.SeedlingResilience, the tenth plant
trait. It rises at t +4.03, 76/120 seeds up, 0/120 extinct, paying a DispersalRange charge
of 2. That is a plant trait selected without touching growth rate, between SeedInvestment
and Dispersal in strength, with a real cost.
  docs/experiments/p4-establishment-contest-2026-08-20.md
  docs/experiments/p4-where-plant-fitness-is-decided-2026-08-20.md

Why establishment and not the other two:
  - Site competition is INFANTICIDE. Only patches below VulnerabilityFraction = .25 can be
    taken over, and newborns start at 1.5-9% of capacity, so newborns are the only class it
    reaches. Uncontested it destroys 34% of every patch ever born inside a median two
    seconds, and that binary is 51.9% of the variance in per-patch lifetime offspring.
  - MORTALITY has no headroom. LifespanSeconds gives Growth a genuine 2x span and
    r(Growth, lifespan) = -0.51 among patches that die of age — and it converts to
    R2 = 0.024 on offspring. Site occupancy is 91% of 24 sites, so reproduction is
    site-limited, not time-limited.
  - SEED PRODUCTION now HAS a genetic channel (SeedProductionRate) and it is a measured
    null. Halving the cooldown moves births under 10%, for the same reason mortality
    has no headroom: both buy TIME, and free SITES are what is scarce.

=== RECOMMENDED NEXT ===

One item left on this track, plus a pending user decision.

`SeedProductionRate` is conditional, not closed: at the 24-site calibration it is null
(charge 0: t +3.22, 68/120 up versus 70/120 disabled drift), but at 168 sites it is selected
(t +4.32, 79/120 up versus 66/120 drift). Site abundance moves mean occupancy from ~0.91 to
~0.32 with no extinctions. See `docs/experiments/p4-site-abundance-seed-production-rate-2026-08-20.md`.
t +1.51 with 70/120 up, so it is less directionally consistent than pure drift.
The reason is the one thing to carry forward: **reproduction here is SITE-limited, not
time-limited.** Halving the cooldown moves plant births only 203.7 -> 221.8 and raises
generations not at all, because a 95.8-second patch already spends 58.7 seconds mature,
off cooldown, and failing to find a free site at 91% occupancy. Lifespan fails for the
same reason. Both buy TIME; time is not scarce, free SITES are.
  docs/experiments/p4-seed-production-rate-is-not-the-constraint-2026-08-20.md

Before proposing any new plant trait, ask whether it changes a patch's access to SITES.
If it only changes how fast the patch grows, how long it lives, or how often it seeds,
that question is already answered and the answer is no.

A. THE INVADER SIDE. The contest is one-sided: only the incumbent's genome enters. An
   invader-side term is the obvious question and was deliberately left out to keep the
   first wiring one-variable.

DECIDED 2026-08-20, do not reopen: PlantEstablishmentContestEnabled stays OUT of
CreatePrototype4Defaults. That factory sets exactly one plant flag, plantCohortsEnabled;
site competition, mortality and every other plant mechanism are opted into explicitly at
the call site of each experiment. Defaulting the contest on would make it the only plant
mechanism ever enabled by the factory, and would invalidate every P4 baseline on record.
It stays on only in CreateFullEcosystemDefaults, which exists for liveness, not experiments.

P4's exit criterion is met: plant selection has a demonstrated route off the growth-rate
gate. The remaining P4 items below are optional polish, not blockers.

Two things to be careful of, both learned the hard way:
  - A prediction stated as a mechanism story is not evidence. State it as a number.
  - Any new plant gene needs the eleventh-parameter treatment (see
    PlantGenome.SeedProductionRate): constructor, CloneMutated, ToTraits/FromTraits,
    TraitNames, TraitCount, ComputeStateHash, and a transmission test. The animal genome
    once dropped a gene silently this exact way.

=== P5 HISTORY BOUNDARY ===

P5 ancestry-aware cluster history is now analysis-only: one segment is scoped to an explicit
cluster threshold and immutable snapshot provenance, and confirmation requires the single bound
ancestry source to be complete through the observation. It does not alter simulation state or
hashes. Presentation/UI and durable chunk storage for this analysis are still unbuilt.

=== ALSO OPEN, LOWER VALUE ===

A. Survivable predator-prey scenario. Prerequisite for adjudicating two config flags, and
   closer to P5 species work than P4. FounderProfile.PredationVariation is extinct before
   3,000 ticks with ZERO births on the plant calibration, so no liveness verdict measured
   there means anything in either direction.
   docs/experiments/p4-inert-flags-readjudicated-2026-08-19.md

B. Rain shadow for the elevation field. Needs a wind-direction convention, which is a design
   choice rather than a mechanical one, so it was left undone rather than invented.

C. Elevation mode on the Play-mode H overlay. Small. Prototype1Presenter is Unity presentation
   code that cannot be verified headlessly, but the user compiles the editor and can check.

D. foragingEconomicsEnabled and learnedResourceQualityEnabled are the genuine wire-or-delete
   candidates — both reachable only from the Legacy policy, which no configuration uses.
   multiThreatPerceptionEnabled and kinRecognitionEnabled are ALREADY WIRED into
   DecideIntentUtilityV1; DO NOT DELETE THEM, they are merely unexercised. See A.

E. Place memory still unwired, deliberately, enforced-inert by test. Deferred to P5.

F. P5 presentation/UI and durable chunk storage remain unbuilt. They are separate follow-on work
   from the completed, host-triggered analysis history.

=== READING ORDER ===

Several docs carry supersede or retraction banners. Read the banner before trusting any
conclusion in an older document.

  p4-where-plant-fitness-is-decided-2026-08-20.md             where the variance lives
  p4-establishment-contest-2026-08-20.md                      the blocker, answered
  p4-seed-production-rate-is-not-the-constraint-2026-08-20.md why the 3rd route is closed
  p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md  the big one, read first
  p4-fertility-binds-the-growth-limit-2026-08-19.md            why the Min matters
  p4-plant-trait-selection-nonreplication-2026-08-19.md        tolerance did not replicate
  p4-inert-flags-readjudicated-2026-08-19.md                   2 of 4 flags are wired, not dead
  p4-elevation-field-2026-08-19.md                             the 2 conditions it needs
  p4-grazing-dose-response-2026-08-18.md                       older positive control
  p4-dose-curve-2026-08-18.md                                  operating point
  p4-scale-and-closing-position-2026-08-18.md                  closing position
  p4-defense-selection-demonstrated-2026-08-18.md              RETRACTED — read the banner
  plant-gene-liveness-2026-08-18.md                            influence is not reward

=== TOOLING THAT EXISTS — USE IT INSTEAD OF GREPPING ===

A caller-search has produced a false clean bill of health FOUR times in this repo. It finds
names; it cannot tell you whether a value flows anywhere, and it cannot see code that runs
every tick against permanently empty data.
  Diagnostics/GeneLivenessAnalysis.cs       animal genes
  Diagnostics/FlagLivenessAnalysis.cs       config flags, reflection-driven, covers new flags
  Diagnostics/PlantGeneLivenessAnalysis.cs  plant genes
  Diagnostics/LivenessRecorder.cs           code paths that run on empty data
LivenessTests + PlantLivenessTests enforce the ledger, so it cannot rot silently.

Measurement: SimulationStatistics.RealizedGrazingPressure, plus PlantBiomassSeconds and
PlantPatchSeconds behind it. Report it alongside any plant-trait result.

Throwaway probes go in Assets/Tests/EditMode/ZZZ*.cs, run with
  dotnet test --filter "FullyQualifiedName~ZZZName"
and are DELETED before committing. Verify the tree is clean afterwards.

=== STATE OF THE PLANT SIDE ===

PlantGenome.TraitCount is 10. Growth-rate costs in PlantPhenotype.FromGenome:
  Growth +.90 benefit, Nutrition -.18, Defense -.15, WaterEfficiency -.08,
  MoistureTolerance -.10, TemperatureTolerance -.10, NutrientUptake -.10 (flag-gated)
Ungated by (1 - B/K): SeedInvestmentFraction, DispersalRange. Those are the ones that move.

Growth limit is Min(moistureAdaptation, fertilityLimit, temperatureLimit). Fertility bound
it 82-90% of the time before NutrientUptake existed, and each adaptation term lifts ITS OWN
channel out of contention, so adding one shifts the binding constraint to the next channel
rather than removing it.

Flags, all defaulting false:
  plantSiteCompetitionEnabled       plantMortalityEnabled
  plantDefenseDeterrenceEnabled     plantQualityPreferenceEnabled
  plantTemperatureAdaptationEnabled proceduralEnvironmentFieldsEnabled
  plantFertilityAdaptationEnabled   elevationFieldEnabled
  plantEstablishmentContestEnabled
maximumPopulation is 48 in the plant calibration and 1000 from the factory. This matters
enormously — see METHOD RULES.

Elevation is INERT under the standard P4 plant config. It needs cap=1000 AND
plantFertilityAdaptationEnabled TOGETHER before it changes anything. Enabling it at cap 48
does nothing at all and the survival table looks reassuringly unchanged while nothing happens.

Play mode: H cycles ground overlay temperature -> biome -> off. N (all-flags playtest)
enables procedural fields so there is terrain to see.

=== METHOD RULES THAT COST THE MOST TO LEARN ===

- TRANSCRIBE the measured config, including the engine bounds. maximumPopulation is 48 in the
  plant calibration and 1000 from the factory; rebuilding a config by hand and taking the
  factory default collapses the scenario 30/30 and looks EXACTLY like a behavior regression.
  It was reported as one before the cause was found. Copy the config from the committed guard
  ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds.
- Seed founders with VARYING values. A uniform founder value gives zero standing variance and
  every result is drift. This cost four sweeps.
- Response to selection scales with standing variance, so a null at one founder spread does not
  carry to another. Answer that objection by MEASURING it — add an arm at a wider spread —
  rather than caveating it.
- SWEEP THE COST, NOT JUST THE BENEFIT. A new trait with a cost term that reads null tells you
  nothing about WHICH HALF is wrong. The establishment contest read t -2.10 at its first charge
  and +6.24 at zero charge — same benefit wiring, same seeds. Three sessions had responded to
  that exact shape by improving a benefit channel that was never the problem.
- SHARE OF VARIANCE IS NOT SHARE OF AVAILABLE SELECTION. 51.9% of plant offspring variance is
  newborn takeover, and most of that stays luck whatever gene is wired in. Pricing a cost
  against the whole share put the first charge three times too high.
- SPLIT THE SAMPLE BY HOW THE OUTCOME HAPPENED before attributing variance to a mechanism.
  Pooled, lifespan looked like 53% of plant offspring variance; split by cause of death it is
  2.4%, and the difference was two-second infants mixed in with hundred-second adults.
- Compare an effect against its OWN sampling error (SD, SE, bootstrap CI), never against
  another arm whose spread is small for structural reasons. That error produced a retraction
  the same day it was made.
- PRINT THE SURVIVAL COLUMNS ON EVERY ARM, including arms that appear to confirm what you
  expected — those need it more. A flag "going live" under PredationVariation founders was
  measured on a population already extinct with zero births.
- Report the SIGN TEST next to the t. A t of -2.05 with 50/120 seeds down is magnitude-driven
  noise, and that is the exact shape of the claim retracted on 2026-08-18.
- Regress FINAL on FOUNDER, never delta on founder — the latter puts the founder mean on both
  sides with opposite signs and manufactures a slope out of noise. Slope 1 is drift, 0 is
  convergence, and a correct flag-off control lands on 1 (it read 0.9995 +/- 0.0412).
- BIT-IDENTICAL OUTPUT ACROSS AN ARM FLIP MEANS DEAD, not "small effect". Check the state hash
  before interpreting any difference, especially when aggregates look reassuringly unchanged.
- Read world.Statistics AFTER stepping. It is stale before the first Step, so a tick-0 baseline
  silently makes delta equal the final value. That produced a spectacular fake result
  (+0.29, t 9.45) caught only by noticing the "delta" equalled a known endpoint.
- "Widest configuration" is not "widest scenario". CreateFullEcosystemDefaults turns every flag
  on but still runs a HERBIVORE scenario, so nothing gated on predationEnabled can fire. Before
  writing down WHY a flag is inert, find its use sites and check the scenario can reach them.
- A failing regression guard is usually right — but check whether the METRIC is the problem
  before the implementation or the threshold. One guard failed because "% above own mean" is
  blind to skew; the field was correct and the measure was replaced, not the tolerance.
- A green dotnet test does not prove Unity compiles — Unity has no global usings. Check for
  those first if the editor disagrees.
- Never run perl -0pi over the docs; it mangles UTF-8. Use the Edit tool.

Append any new lesson to AGENT_FIELD_NOTES.md section 5 before the session ends, and update
this file's OPEN WORK section so the next session starts where this one stopped.
```
