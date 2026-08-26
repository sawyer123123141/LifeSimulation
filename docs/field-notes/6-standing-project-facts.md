## 6. Standing project facts

- **P5 ancestry-aware cluster history is analysis-only.** Each segment is scoped to its
  explicit clustering threshold and immutable snapshot provenance; confirmation requires the one
  ancestry source bound to that segment to be complete through the observation. This does not
  alter simulation state or hashes. A host-triggered session and an eight-row Unity evidence panel
  are built; durable chunk storage, a graphical evolutionary tree, and continuity filtering remain
  unbuilt. `GeneticClusterHistory.cs` is ~1,310 lines and should receive a separately approved,
  behavior-preserving decomposition before substantial new classification logic is added.
- Two decision policies exist: `Legacy` and `IntentUtilityV1`. **Every P4
  scenario, `CreatePrototype4Defaults`, and both playtest hotkeys use
  `IntentUtilityV1`.** Anything gated on `Legacy` is inert for P4. This asymmetry
  is the source of most half-wired mechanisms — check both paths.
- Arena is hard-coded `(-25, 25)` on both axes in `SimulationWorld`.
- New behavior goes behind a `SimulationConfig` bool defaulting `false`, added as
  the constructor's last optional parameter with its property immediately after
  the previous flag's. Flag-off must be byte-identical, proven by a hash
  regression on the `PredationVariation`/`Legacy` scenario. Scenario-data changes
  are not behavior changes and need no flag.
- `PlantMortalityEnabled` defaults `false`. Any ecosystem experiment must enable
  it explicitly or plants freeze at generation 2.
- `CreateFullEcosystemDefaults` turns every P4 mechanism on at once. It is a
  **liveness and integration** configuration, not an experimental one: every flag
  moves together, so any difference it produces is unattributable. Use it to pin
  liveness against the widest surface; run experiment arms against
  `CreatePrototype4Defaults` varying one flag.
- `PlantQualityPreferenceEnabled` defaults `false`. With it off, `ComputeNeedGain`
  saturates at its `Math.Min(1f, ..)` clamp for every active patch, so patch
  quality is invisible to `IntentUtilityV1` foraging and grazing is uniform.
- **Seed plant founders with *varying* defense when testing selection.** A uniform
  founder value gives zero standing variance and every result is drift. See
  `docs/experiments/p4-defense-selection-demonstrated-2026-08-18.md`.
- **Growth-rate traits are the wrong place to look for plant selection.** `growth` is gated by
  `(1 - Biomass/Capacity)`, measured mean **0.1711**. Everything through
  `PlantPhenotype.GrowthRateMultiplier` measures null or weak even at the 168-site operating point;
  all six dedicated rate traits were re-audited there. `Growth` is the exception only because it
  also controls lifespan and moves downward when mortality has headroom. `SeedInvestmentFraction`
  and `DispersalRange` skip the gate and are the strong positive controls. Before wiring a new plant
  trait, check which side of that gate it lands on.
- **`ElevationFieldEnabled` defaults `false`** and is **inert under the standard P4 plant
  config**. It needs `maximumPopulation: 1000` *and* `PlantFertilityAdaptationEnabled` together
  before it changes anything — grazing has to pull patches off capacity, and fertility has to
  stop binding the `Min`. Requires `ProceduralEnvironmentFieldsEnabled`.
- **`PlantFertilityAdaptationEnabled` defaults `false`** and gates `NutrientUptake`'s growth
  charge as well as its benefit, so flag-off is byte-identical to the world before the gene
  existed. The constructor's optional positional parameters are the shape that once silently
  dropped `persistence`, so
  `EveryPlantTraitTransmitsThroughCloneMutated` pins it.
- **All three non-growth-rate plant routes are operating-point dependent.** At the 24-site,
  ~91%-occupied calibration, establishment is selected (`SeedlingResilience`, t +4.03, 76/120 up),
  while lifespan and seed production lack free-site headroom. At the 168-site, ~27-33%-occupied
  count-plus-geometry condition, lifespan comes alive (`Growth` moves down: t -2.65, 46/120 up),
  `SeedProductionRate` becomes selected (t +4.32, 79/120 up versus 66/120 drift), and incumbent
  `SeedlingResilience` reverses (t -2.56, 44/120 up). Do not call occupancy the sole mediator: the
  168-site scenario also changes geometry. Keep 24 sites as calibration and 168 as an explicit
  experimental operating point until the human resolves the design question.
- **The liveness suite is the slow half and it grows with every flag and gene.** `dotnet test` is
  dominated by simulation-per-gene/flag fixtures, and a single test —
  `LivenessTests.RiskAversionIsLiveOnlyWhenThreatsExist` — is **16 s** on its own, more than
  several preceding tests combined on the reference machine. `FlagLivenessAnalysis` runs a
  simulation pair per config flag, so the cost rises every time a flag is added. In a throttled
  container this reads as a hung runner. Build once, run the non-liveness shard, PlantLiveness,
  liveness excluding RiskAversion, then RiskAversion alone with a generous timeout.
- **`PlantEstablishmentContestEnabled` stays OUT of `CreatePrototype4Defaults`** (decided
  2026-08-20). That factory sets exactly one plant flag, `plantCohortsEnabled`; every other plant
  mechanism is opted into explicitly at each experiment's call site. Defaulting the contest on
  would make it the only plant mechanism ever enabled by the factory and would invalidate every
  P4 baseline on record.
- **`PlantSeedProductionRateEnabled` defaults `false` and is kept as the project's live NEGATIVE
  control.** It reaches behaviour and demonstrably is not selected, which `Genome.NeutralMarker`
  cannot supply because that one is unwired. `PlantGenome.TraitCount` is now **11**. Its dispersal
  charge is a `float` config value, not a bool, so `FlagLivenessAnalysis` does not cover it —
  perturb it by hand if you need a verdict on the charge.
- **Site abundance is the mediator for `SeedProductionRate`** (2026-08-20): at 24 sites,
  occupancy is 0.904-0.908 and the enabled arm is null (+0.01953, t +3.22, 68/120 up versus
  70/120 disabled drift). At 168 sites, occupancy falls to 0.322 and it becomes selected
  (+0.02022, t +4.32, 79/120 versus 66/120 drift), with no extinctions. The gene is thus a
  conditional positive, not a universal negative; see the site-abundance writeup.
- **The P4 plant-selection blocker is answered: the route is ESTABLISHMENT.**
  `PlantEstablishmentContestEnabled` (default `false`) lets a seedling below
  `VulnerabilityFraction` resist takeover with `PlantGenome.SeedlingResilience`, the tenth plant
  trait. It rises at t +4.03, 76/120 seeds up, 0/120 extinct, paying a real `DispersalRange`
  charge of 2. Requires `PlantSiteCompetitionEnabled`, which is what creates the contest. This
  is a 24-site conclusion and reverses at the 168-site operating point. See
  `docs/experiments/p4-establishment-contest-2026-08-20.md`.
- **Site competition is infanticide, not competition between established patches.** Only patches
  below `VulnerabilityFraction = .25f` can be taken over, and newborns start at 1.5-9% of
  capacity, so newborns are the only class it ever reaches. Without the contest flag it destroys
  **34% of every patch ever born** inside a median two seconds, and that binary is **51.9% of the
  variance** in per-patch lifetime offspring. Site occupancy is 91% of 24 sites, so reproduction
  is site-limited: realised lifespan explains only **2.4%** of offspring variance among survivors.
  `docs/experiments/p4-where-plant-fitness-is-decided-2026-08-20.md`.
- **The plant-selection positive control is `Dispersal`.** It moves +0.098 to +0.125 at
  t 14-17 with 105-115 of 120 seeds up, across four arms, at 0/120 extinctions —
  `docs/experiments/p4-plant-trait-selection-nonreplication-2026-08-19.md`. Read any plant-trait
  null against it. `SeedInvestment` is a weaker second (t 4.8-6.8). Both are the traits with no
  growth-rate cost term in `PlantPhenotype`.
- **`maximumPopulation: 48` is part of the P4 plant calibration**, not a default. The factory
  default is 1000, which collapses the calibration scenario entirely. Copy the config from
  `ResourceExperimentTests.ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds`.
- `PlantDefenseDeterrenceEnabled` defaults `false`. With it off, plant defense
  protects no biomass and cannot be selected on at all — see
  `docs/experiments/p4-defense-no-gradient-2026-08-18.md`. Any coevolution run
  must enable it, and must report `SimulationStatistics.RealizedGrazingPressure`
  so a null can be read against the pressure that actually existed.
- `SimulationStatistics.RealizedGrazingPressure` and the `PlantBiomassSeconds` /
  `PlantPatchSeconds` integrals behind it are read-only accumulators, deliberately
  absent from `ComputeStateHash`. Same rule applies to anything in `Diagnostics/`.
- **Soft home-range affinity is CLOSED as a measured negative (2026-08-22). Do not tune it, do not
  reopen it.** `HomeRangeAffinityEnabled` stays default `false`; the implementation, its tests and
  key `R` stay in the tree because they are correct and flag-off is byte-identical. Measured across
  two experiments, five conditions, 240 fixed-seed runs. Shipped observation scenarios: route
  metric saturated at 1.0000 flag-off, delta +0.0000, +0.0001 at a 10x bonus, and the 10x arm cost
  2.7% food intake for no births. `ObservationRouteRing`, built so a route *can* exist and
  delivering **90.6% decision opportunity at 0.88 familiarity**: route repeatability **fell**
  -0.0345 (t -2.87, 8/30 up) while same-site re-entry **rose** +0.0594 (t +4.93, 26/30 up).
  Geometry was tested as the rescue hypothesis and was not the blocker. Spec and plan carry
  SUPERSEDED banners. Evidence: `docs/experiments/p4a-home-range-affinity-2026-08-22.md`,
  `docs/experiments/p4a-route-ring-home-range-2026-08-22.md`.
- **`Prototype4Scenarios.ObservationRouteRing` is the repository's only route-capable geometry.**
  Eight sites on a radius-8 ring alternating Food and Water: adjacent opposite-kind separation
  6.12, same-kind 11.31, founders at the centre, total capacity/regeneration matched to
  `ObservationStable`. Every site has two equidistant opposite-kind neighbours, so travel burden
  alone cannot break the choice. **It is a harsh survival condition, not a calibration: 11/30 and
  9/30 seeds go extinct** even at matched productivity, because splitting the same output across
  eight sites is harder to live on than two co-located pairs. Use it as the harness for
  clustered/changing-patch work; never as a survival baseline.
- **`Prototype4Scenarios.ObservationShiftingPatches` is the changing-map scenario, on Play key `V`
  at world seed 45.** Three clusters 23-28 apart, each with a permanent water site at its centre,
  two active food sites 7 units out, and four dormant food sites as dispersal targets. With
  `plantMortalityEnabled: true` it sustains ~29 patch deaths and ~33 establishments per 6,000-tick
  run at an equilibrium of 11.96 active food sites. **Its honest extinction rate is 6/30 and the key
  runs seed 45 because seed 42 is one of the six failures** - that is a demonstration choice, and
  every published statistic comes from the full 30-seed sweeps, never from seed 45.
- **Separated food and water does not kill creatures, it STERILISES them (explained 2026-08-22).**
  `ReproductionSystem.CanReproduce` requires energy AND hydration AND health each at or above 70% of
  that creature's own capacity. Adults satisfy that joint gate **95.0% of adult-ticks in
  `ObservationStable`** (co-located) and only **33.5% in `ObservationShiftingPatches`** and 56.8% in
  `ObservationRouteRing`. Two effects: the marginals collapse (energy above 0.7 falls from 95.1% to
  46.3%) and a genuine simultaneity penalty of 8.6-12.8 points sits on top, because a creature tops
  one need up while the other drains. **Nothing starves and nothing dehydrates**: all four founders
  die of age at tick ~2500 in every arm, and the minimum hydration fraction reached averages 0.445
  even in the worst arm. Extinction is failure to replace, not mortality.
  `docs/experiments/p4a-founder-mortality-2026-08-22.md`.
- **The P4a "optional juvenile local-area bias" item is NOT the fix for separated-resource
  extinction.** Juveniles are not the failing class and mortality is not the failure mode. A bias
  keeping young creatures near their birth area cannot raise the joint reproduction window and might
  lower it by keeping them beside whichever single resource they were born next to. Every refuted
  calibration lever is explained by the joint gate: founders on food are worse because that lowers
  the binding hydration marginal; mature founders do nothing because age was never the constraint;
  eight founders are worse because more grazers depress both marginals; 1.5x productivity does
  almost nothing because refilling faster does not make two needs peak together.
- **DECIDED 2026-08-22: the joint 70%/70% reproduction gate STAYS. Do not change
  `ReproductionSystem.CanReproduce`.** The user chose to treat reduced fertility while commuting
  between separated resources as real ecology. No re-baseline is needed and every result on record
  stands. The standing consequence: **a spatially separated scenario must be calibrated to be
  viable under the gate** - more productivity, more founders, or accepted extinction - rather than
  the gate being relaxed to suit the scenario. `ObservationStable`'s 0/30 extinct is the outlier,
  not the norm. Do not re-open this as a bug; it is a decided design property.
- **Plant dispersal does not need plant mortality, so "mortality off" is NOT a frozen map.** With
  mortality off, plants colonise every dormant site within ~20 ticks and stay there: 12
  activations, 0 deactivations, 17.37 of 18 sites active. Any experiment wanting a genuinely static
  resource map must declare no dormant sites at all, not merely leave mortality off. This
  mis-specified a control on 2026-08-22 and inflated a scenario's real productivity from a declared
  1200 food capacity to roughly 3600.
- **`SimulationWorld.CaptureStatistics()` is the correct way to read end-of-run statistics.** The
  `Statistics` property is refreshed only on statistics-cadence ticks (every 20 ticks at the P1
  schedule) and its constructor-time value predates any applied scenario. `ExperimentRunner` uses
  `CaptureStatistics`. Per-tick simulation code must not call it - it walks every creature.
- **Statistics are sampled after the tick's deaths are committed (since 2026-08-22).** Any figure
  taken from a pre-`9763374` run understates deaths at run boundaries and can report a surviving
  population on an extinction tick. Trajectories are unaffected: the audit found a seed whose death
  count changed while its state hash stayed identical.
- **Post-fix revalidation status of the plant corpus (2026-08-22).** Confirmed on fixed code with
  varying founders over 120 seeds: the `Dispersal` positive control (t +15.63, 110/120),
  `SeedInvestment` (t +7.10, 91/120), the establishment-contest manipulation (+0.0362, t +3.22,
  72/120 versus recorded t +4.03, 76/120), the 24-site `SeedProductionRate` null (43/120 up), and
  the whole plant lifetime decomposition. **Still un-auditable:** every 168-site low-occupancy
  conclusion and the six growth-rate nulls, because the committed replication geometry produces
  occupancy 0.84 rather than 0.32. Survival was clean throughout: 0/120 extinct, 0/120 frozen in all
  six combinations. `docs/experiments/p4-postfix-revalidation-2026-08-22.md`.
- **`AbundantSiteReplicationModerate` is calibrated to span 114 / spacing 9.5, measuring occupancy
  0.311 (recorded 0.322-0.332) at 0/10 extinct.** It is a replication *condition*, never a re-run:
  the original's target coordinates are unrecoverable. **Known ecological difference: the lattice
  spans +/-57 while the creature arena is hard-coded to +/-25, so outer patches are never grazed.**
  Any trait result from this scenario must report `RealizedGrazingPressure` and state that
  difference - it reproduces the recorded occupancy, not necessarily the recorded ecology.
- **The three low-occupancy plant conclusions are UNVERIFIABLE, not merely unverified (2026-08-22).**
  Measured at the calibrated replication condition (occupancy 0.28-0.35, 120 seeds, varying founders,
  disabled-channel controls): `SeedProductionRate` is null (+0.00424, t +0.72, 64/120 against a
  recorded +0.02022, t +4.32, 79/120); the `SeedlingResilience` reversal is not demonstrated
  (-0.00248, t -0.34, 53/120) though the +0.0362 advantage measured at 24 sites is abolished; the
  lifespan-headroom claim is not adjudicated because its only available control is confounded. The
  six growth-rate nulls hold. **None of this refutes the recorded results**, because the replication
  reproduces the recorded occupancy and not the recorded ecology - grazing pressure is **0.00261 against
  0.00699 at 24 sites, a ratio of 0.373**, because the free-site pool sits outside the grazed arena.
  The matched 24-site arm reproduced its recorded occupancy (0.9139 against 0.904-0.908) at 0/120
  extinct, so the harness is faithful and the deficit is geometric. Banners stay.
- **`PlantEstablishmentContestEnabled` costs 19/120 extinctions at low occupancy** against 4/120 in
  the base arm, where at 24 sites it cost nothing. Any future use of the contest at low site
  occupancy must report survival.
- **Non-finite values are rejected at two boundaries (2026-08-22).** `SimulationConfig.Validate()`
  checks all ten float tuning values are finite, and `ResourceDefinition`'s constructor checks
  position, radius, amount, capacity, regeneration and nutrition multiplier. **Clamping is not a NaN
  filter** - `Math.Max(0f, Math.Min(1f, NaN))` is NaN - so a non-finite value that gets past a
  boundary propagates through every later operation and surfaces as an unreproducible run rather
  than as an error. Convention preserved: config constructs freely and `Validate()` is the gate
  (called by `SimulationWorld`'s constructor); scenario data has no deferred gate so it rejects at
  construction.
- **Every experiment CSV must carry a manifest (2026-08-22).** Use
  `ExperimentManifest.Describe(codeRevision, scenario, config, firstSeed, seedCount, ticks)` and
  `ExperimentCsv.Compose(manifest, header, rows)`; the composer **refuses** to write without one.
  The manifest records the schema version, a caller-supplied code revision, the scenario id, its
  **layout fingerprint**, resource count, seeds, ticks and all 26 behaviour flags plus the key
  numerics. A test uses reflection to assert every public bool on `SimulationConfig` appears, so a
  new flag that is not added to the manifest fails the build - a manifest that silently omits a
  flag is worse than none, because it looks complete.
  **`SimulationScenario.ComputeLayoutFingerprint()` is the part that matters most**: an identifier
  is not provenance, two scenarios can share a name and differ in geometry, and that is exactly how
  the 168-site condition was lost. `ExperimentManifest` is deliberately environment-free (no clock,
  no git call, no file system) because it lives in Simulation; the caller supplies the revision.
- **P5 clustering allocation is now linear in population, measured both before and after
  (2026-08-22).** `GeneticClusters.From` flattens every genome into one shared trait buffer once,
  then compares offsets into it via `GeneticDistance.Between(traits, offsetA, offsetB, count)`.
  Before: a constant **240 bytes per pair** (two `ToTraits` arrays), giving 187 KB at 40 creatures,
  4.8 MB at 200 and **120 MB / 126 ms at 1,000**. After: 4.3 KB, 21 KB and **104 KB / 50 ms** - a
  1,151x reduction and 2.5x faster at 1,000. `Genome.WriteTraits(destination, offset)` is the single
  source of trait order; `ToTraits()` is now the allocating wrapper around it. A test pins that
  allocation grows under 8x when pairs grow 16x, so a regression to per-pair allocation fails.
- **Resource allocation is NOT a hot spot, and its cost shape is the opposite of the obvious one
  (measured 2026-08-22).** `ResourceAllocationSystem.Resolve` costs
  **O(requests x distinct resources)**, not O(requests^2): the expensive branch runs once per
  distinct resource, so **crowding is the cheap case**. 1,000 requests on one resource resolve in
  16.9 us; the same 1,000 across 24 resources cost 165 us. End to end, a 12,000-tick run at the
  largest population a committed scenario reaches (523 creatures) takes **2.72 s total, 0.227
  ms/tick including every other system**. **Do not optimise this** - it would mean touching a
  deterministic path that decides who eats when resources run short, for an unmeasurable gain.
  Revisit only if populations in the thousands and site counts in the hundreds coincide; the fix is
  then to bucket requests by resource index in one pass.
  `docs/experiments/p1-resource-allocation-benchmark-2026-08-22.md`.
- Phase order is fixed: P4 (ecosystem) → P5 (species/history) → P6 (terrain
  generation). Terrain is last, deliberately.
- Subagents: dispatch on `model: sonnet` explicitly. Ask before using opus.
- `python3` is unavailable in this environment. Large heredocs sometimes fail;
  use the Write tool.
- **Three hashes, three jobs — never merge them (2026-08-22, implemented in `7343653`).**
  `ComputeStateHash` is **V1, frozen**: a historical identifier that many tests pin as literals. It
  is incomplete and that is now fine; never "complete" it and never recompute a recorded V1 value.
  `ComputeStateFingerprint` is **V2**: every future-determining field *plus* configuration, versioned
  so a field-set change is a new number rather than a silent redefinition, and valid **only at a
  settled step boundary** (it throws if deaths are queued). `ComputeBehaviorHash` answers a different
  question — did this gene or flag reach behavior — and **must never include configuration**:
  `FlagLivenessAnalysis` flips a flag and compares that hash, so folding config in would make every
  flag read live by definition and destroy the harness. If you are ever tempted to unify them, that
  interaction is the reason not to.
- **A fingerprint whose field set depends on a flag cannot do its job.** V1 hashes home-range state
  only when `HomeRangeAffinityEnabled` is on. V2 hashes it unconditionally. Two worlds differing in
  a flag would otherwise be compared on different fields, which is exactly the comparison a
  fingerprint exists to make valid.
- **Decide "should this hash cover more?" by measurement, not by argument.** Adding plant `Age` and
  `ReproductionCooldownRemaining` to `BehaviorHash` was the open question; the prediction (stated
  first) was that the inert set would not move, since all four inert flags are inert for a
  *reachability* reason, not a sensitivity one. Measured with the lines in and out: identical inert
  set, identical plant gene verdicts, 33 / 19 / 1 either way. Kept because it is strictly more
  sensitive at no cost. `BehaviorHash` is never pinned as a literal anywhere — only ever compared
  against itself — so extending it invalidates no baseline. Check that property before extending any
  hash.
- **Absence from every hash is why a real defect stayed invisible.** Plant `Age` was in no hash, so
  the `ReplaceAt` takeover-age bug passed every hash regression the project had. When a fix reveals
  that a field was unhashed, hashing it is part of the fix, not a follow-up.
- **A detector must not be keyed on the thing it is testing.** The takeover control keys on the
  lineage-parent change, not on the age reset — keying on age would key the detector on the very fix
  under test. Same lesson as the 2026-08-22 lifetime detector, now pinned in a committed test.
- **Assert the manipulation, or the test name is an overclaim.** A 2,000-tick equality test named
  "...AcrossBirthsDispersalDeathAndTakeover" passes just as happily in a run where no patch ever
  died. It now carries a positive control per named path. A green test that proves nothing happened
  is worse than no test, because it reads as evidence.
- **Guard drift with a pinned count, not with a comment.** `ComputeConfigurationHash` is enforced by
  two tests: every `bool` constructor parameter must move the hash, and `SimulationConfig`'s public
  property count is pinned (44 hashed of 46; `FixedDeltaTime` and `MaximumMemorySlots` are derived
  from inputs already covered). Adding a field without hashing it fails a test instead of quietly
  producing a fingerprint that no longer means what a baseline assumed.
- **Put per-entity observation OUTSIDE `SimulationWorld` (2026-08-22, `CreatureActionHistory`).**
  Anything that watches creatures — histories, timelines, per-entity diagnostics — should sample the
  world from outside rather than live inside it, following the `LivenessRecorder` precedent. Held
  inside, it is future-determining state by the letter of the fingerprint design and has to be
  re-argued every time a hash changes; held outside, the question never arises, it is absent from
  every hash by construction, and it is still fully deterministic and testable headlessly. Do not
  gate such a thing behind a `SimulationConfig` flag: a diagnostics flag must be behavior-inert to
  be correct, and `FlagLivenessAnalysis` then reports it inert and fails the known-inert assertion.
- **The V2 fingerprint's first real job is proving an observer is an observer.** Fingerprint a
  watched world and an unwatched world after N ticks and assert equality. That is a much stronger
  claim than "I did not intend to mutate anything", and it is one line. Pair it with an assertion
  that the observer actually recorded something, or the equality proves nothing.
- **Sample observers per simulated step, not per frame.** Frame-rate and the speed multiplier would
  otherwise change what the player sees, which makes an on-screen history non-reproducible and
  useless as evidence.
- **Show the need delta across an episode, not just the action.** "SeekFood 12.4s, food -6%" reads
  as a failed trip; "SeekFood" alone reads as normal behavior. The instantaneous inspector cannot
  show this, which is why the reproduction gate stayed invisible for so long.
- **A population pinned at its cap cannot measure survival (2026-08-22).** The 2026-08-21 rendezvous
  experiment reported "0/120 extinct, both arms 48/48" — and **all 240 runs ended at exactly 48**,
  zero variance. That is a ceiling, not a null. Before believing any survival result, check the
  spread of the outcome variable; if it has none, the experiment answered a different question than
  it claimed. The same check applies to any metric sitting against a clamp.
- **The population cap is load-bearing ecology in this project, not a guard rail.** Raising it does
  not free growth, it causes overshoot and collapse: extinct 0/8 at cap 72, 5/8 at 84, 8/8 at 96+
  (≈293 births then starvation). Pick an operating point where the outcome is partial — cap 84 —
  when the outcome under test *is* survival.
- **Normalise decision-tick counts by creature-ticks before calling them a manipulation check.** An
  arm whose population survives longer accumulates more of every count. Raw rendezvous births rose
  t +2.04; per creature-tick the same difference is t +1.24, and among seeds where both arms survived
  it is t +1.01. The raw number measured exposure, not fertility.
- **Use McNemar, not a proportion comparison, for paired binary outcomes.** Extinction 75/120 versus
  66/120 looks like a benefit; the discordant pairs are 26 versus 17, χ² 1.49, not significant. The
  aggregate hid the pairing that the design deliberately created.
- **"Wrong sign" and "right sign, no payoff" are different verdicts — record them differently.**
  Home-range affinity was closed because its effect ran backwards. Safety-gated rendezvous was closed
  because its effect is real, correctly signed and well powered (predation deaths t −4.64) but
  reaches no outcome that matters, since starvation rather than predation limits the population.
  Filing both as "measured negative" would lose the fact that one mechanism works and is waiting for
  a habitat that rewards it.
- **A mechanism that does not pay off may be limited by the scenario, not broken.** Before tuning a
  mechanism or building architecture around it, ask what actually limits the population. Saving 2.3
  creatures per run from predators changes nothing when the binding constraint is food.
- **Cap-pinning audit result: the pattern was systemic, the damage was not (2026-08-22).** Eleven
  CSVs and 4,080 runs have a zero-variance population or extinction column. Exactly **one**
  conclusion was a ceiling artefact — the rendezvous survival null. In the other ten, zero extinction
  was a *control against differential survival*, and that control is valid exactly as stated. **A
  zero-variance column invalidates a conclusion only if that column was the outcome under test.**
  Do not let a scary-looking scan turn into a blanket retraction; classify first.
- **"0/120 extinct" is not evidence of a healthy world.** Those populations are cap-stabilised, and
  removing the cap causes overshoot and collapse in both harnesses tested — predation config
  0/8 extinct at cap 48 but 8/8 at 96; herbivore plant config 0/6 at cap 48 **and still pinned at
  95.8 at cap 96**, then 5/6 at 200 and 6/6 at 600. Same cliff, different position. Check the second
  configuration before claiming a finding generalises.
- **The plant corpus is scoped to constant cap-saturated grazing.** Every plant trait result was
  measured with the herbivore population pinned at 48. That is a fine controlled variable, but
  whether the gradients survive a freely fluctuating herbivore population is untested — and raising
  the cap does not test it, because that yields a boom-and-collapse rather than a free-running
  population. It needs a habitat limited by carrying capacity instead of by a cap; none exists yet.
- **Terrain that reads as terrain needs separated scales, not more octaves (2026-08-23).** One band
  of fBm doing everything gives a uniform gravel field: every peak the same size, no continents, no
  plains. The structure that works is a hierarchy combined **multiplicatively** - a very-low-frequency
  continent mask deciding where land exists, ridged mountain belts modulated by that mask *and* by a
  second low-frequency belt mask so ranges cover only part of a continent, then small-amplitude
  detail. The belt mask being zero over most of a continent is what makes plains exist.
- **Never sample noise finer than the render resolution.** Octaves below the sample spacing do not
  add detail, they add aliasing. A globe drawn with 192 columns resolves frequencies to 192/4pi ~ 15;
  it was being handed a field whose finest octave carried ~4,000 features around the equator, and it
  rendered as television static. Derive octave count from the caller's resolution
  (`PlanetTerrain.OctavesUnder`), so the same generator gives a globe fewer octaves than a close-up.
- **When a field looks wrong, evaluate the formula on typical inputs before rewriting it.** Twice the
  structure read correctly while the numbers did not: land came out at 0.51 against a 0.38 waterline
  because the belt mask multiplying the relief term is zero over most of a continent; and patch
  relief reused the arena's 14 units for a 400-unit patch. Both rendered as a coloured plane, and
  neither was visible from reading the code.
- **A smoothing filter with a fixed tap count is an aliasing filter.** Averaging three samples spread
  over a radius undersamples harder as the radius grows - more smoothing produced more roughness.
  Sample density must scale with radius; blurring on the grid instead is correct at any radius and
  costs the same.
- **Prototype generation in Presentation before promoting it to Simulation.** `PlanetTerrain` lives
  in Presentation, so iterating on it moves no hash and needs no re-measure. Promotion is a
  deliberate step: flag defaulting false, prove flag-off byte-identical, then re-measure every result
  scoped to the old field. Do not shortcut it by pointing the renderer at a field the simulation does
  not use - visuals disagreeing with the simulation is the failure this project is built to avoid.
- **The 50-unit arena is too small for planet-scale terrain, and possibly for the ecology too.**
  Continents are ~500 units; the arena fits inside a fraction of one. The same smallness is the
  likeliest reason the population cap is load-bearing (2026-08-22 cap audit). Growing the arena is a
  terrain decision *and* the most plausible route to a carrying-capacity-limited habitat.
- **Read the reference implementation BEFORE writing the system (2026-08-23).** Fifteen rounds of
  first-principles tuning on terrain produced six wrong diagnoses; twenty minutes reading
  SebLague/Procedural-Planets from source produced the architectural answer. The user asked three
  times for research before it was done. Read the SOURCE, not only the explanation: the video gives
  intent, the source gives composition order, masks and clamps - which is where every defect was.
  `docs/reference-implementations.md` lists what is worth reading and what each maps to.
- **A bounded field with an interior threshold is a terrace generator.** Elevation as 0..1 with sea
  level at 0.38 inside it forces a clamp, the clamp forces a knee, and the threshold forces a branch
  at the waterline. Three slope discontinuities, and a terrace *is* a slope discontinuity. Signed
  displacement from the threshold has none of them, and the coast becomes the zero crossing rather
  than a tuning problem.
- **Any piecewise-constant lookup is a cliff in whatever it feeds.** A spherical Voronoi plate lookup
  stepped elevation by **0.825 between samples one unit apart against a median of 0.00093 - a ratio
  of 885**. Blending the two nearest cells took it to 0.0417. If a field reads a cell property,
  interpolate across the seam or accept a wall there.
- **Measure CONTINUITY, not just distribution.** Deciles, land fraction, biome counts and saturation
  were all identical whether the field was smooth or cliffed. The instrument that found the 885x step
  was adjacent-sample gradient along a line. Distribution statistics cannot see a discontinuity.
- **A render and a field statistic are blind to different things.** A statistic cannot see colour
  quantised per triangle, unlit faces, or z-fighting; a render cannot see a step discontinuity in a
  field. Build both, and expect each to catch what the other missed.
- **When a symptom is invariant under every change, stop changing things and run a splitting test.**
  Six terrain diagnoses were wrong and four changed nothing at all. Rendering the same mesh **unlit**
  separated shading from geometry in one image and should have been the first move, not the seventh.
- **Check units on any "maximum" constant.** `MaximumSlope = 0.55` in elevation-per-radian was a
  **3% grade** - I had capped terrain at "gently sloping field" and crushed every band above ~10
  cycles/radian to centimetres. Elevation 1.0 is 30 m and a radian is 500 m; the conversion has to be
  written down or the constant is meaningless.
- **A view-relative scale silently breaks physical quantities.** Height scale as a fraction of view
  width rendered the same ground **eight times flatter the closer you looked**. Thirty metres does
  not shrink because the camera moved: make physical scales constant and put artistic exaggeration
  in a separately named factor.
- **Terrain needs bands at the scale of the thing living on it.** Every band was planet-scale - the
  hill band is a 77 m wavelength, so **less than one hill spanned the 50 m arena** and terrain was
  flat everywhere a creature could walk. Add local bands, gate them on whether the view can resolve
  them, and slope-limit their amplitude rather than picking it.
- **Never sample noise finer than the render resolution, and cap amplitude by SLOPE not frequency.**
  Octaves below the sample spacing add aliasing, not detail - a globe drawn with 192 columns was fed
  a band carrying ~4,000 features around the equator and rendered as television static. Separately,
  doubling mesh resolution DOUBLED the stripe count without removing it, proving the binding limit
  was representable slope.
- **A capture that cannot reproduce the runtime is a second implementation, not an instrument.** The
  offline PNG tool and the live preview drifted - different resolution, different triangulation,
  water in one and not the other - so the diagnostics described a mesh nobody was looking at, and
  missed a reported bug entirely. One shared build path, and anything that differs must differ there
  visibly.
- **"Are these two views the same world?" is worth a marker, not an argument.** Tinting the vertices
  inside the flat views window on the globe refuted a confident explanation of mine in one image: the
  two views were showing different parts of the planet, which looks exactly like a level-of-detail
  difference and is not one.
- **Prototype generation in Presentation, promote deliberately.** `PlanetTerrain`, `PlateStructure`,
  `IcoSphere` and `TerrainMeshBuilder` all live in Presentation, so fifteen rounds of iteration moved
  no hash and needed no re-measure. Promotion into Simulation is a flag defaulting false, proof that
  flag-off is byte-identical, then re-measuring every result scoped to the old behaviour.
- **Transparency is not an alpha value.** A Unity Standard material stays opaque however low its
  alpha until it is switched to the transparent blend path explicitly. And transparent water was the
  wrong call anyway: it revealed the finite sea bed patch as a lighter rectangle mid-ocean, an
  artefact worse than the one it solved.

- **A slope ceiling is not an amplitude (2026-08-23).** `SlopeLimited` clips a band to the steepest
  slope the mesh can represent. Two bands whose chosen amplitudes were both **above** that ceiling
  were therefore both clipped to it and summed - measured median land grade **0.243** in the 200-unit
  view against **0.085** for the planet-scale bands alone. When a limiter is doing the choosing, the
  number written in the source is fiction. Check whether any amplitude is above its own limit before
  believing it.
- **"Bumpy at one zoom, fine at another" is a band-gating question, not a noise-quality one.** Patch
  resolution is fixed at 193 samples, so the resolvable frequency is set entirely by how wide the
  window is: **120.6** cycles/radian at 400 units, **241.2** at 200 units. A band at 150 is therefore
  absent from one view and present at full strength in the next. Before tuning noise, work out which
  bands each view can actually carry.
- **A hard resolution gate is a pop.** `if (maximumFrequency >= BandFrequency)` gives a band full
  amplitude the instant the camera crosses the threshold, so zooming changes the *character* of the
  ground rather than its detail - the world appears to be rebuilt rather than approached. Fade the
  band in across half an octave instead.
- **Build the instrument that does not need the editor.** Both terrain instruments are Unity menu
  items, and both fail while the editor holds the project lock - which is exactly when someone is
  looking at terrain and wants a number. `PlanetTerrain`, `PlateStructure` and `TerrainSettings` are
  pure C# by design, so `tools/TerrainProbe` compiles them directly and answers in two seconds with
  nothing closed. Keeping generation free of engine types is what made that possible; it is worth
  protecting.
- **An instrument must sample at the resolution of the thing it describes.** The flat-view statistics
  passed the *globe's* maximum frequency to every window, which silenced the two creature-scale bands
  completely. It was therefore reporting on a field that no flat view renders, and could not have
  seen the relief that turned out to be the complaint. Same failure as the capture-versus-runtime
  drift, one level down: the instrument had quietly become a description of something else.
- **Put the tunables behind sliders once a thing is judged by eye.** Terrain is judged against a
  one-metre creature at three zoom levels. That is not a judgement anything makes from source, and
  edit-recompile-look is why the previous round took fifteen passes. Mutable global settings are the
  right trade here - there is one terrain - as long as an explicit parameter survives for probes and
  tests, and a Reset restores the shipped values exactly.
- **A biome that exists globally and appears in no view is absent (2026-08-23).** Global counts said
  the planet had ice, tundra, desert, marsh and scrub. The flat views had been parked on one computed
  coastline at latitude -15 degrees for the whole of the terrain work, where the mix is 61% ocean and
  35% grassland and **contains no ice or tundra at all**. "Just green and some sand and water" was an
  accurate report about the view and said nothing about the generator. Walk the viewpoint before
  concluding anything about variety - at +75 degrees the same window is 79% ice.
- **Name the categories, do not count them.** The window statistics reported "biomes 5", which says
  a window is varied and cannot say whether any of the five is the one somebody reported never
  seeing. Counting distinct kinds is the cheapest possible summary and it hid a reachability problem
  for days.
- **A global statistic and a local view answer different questions, and the local one is what a
  person sees.** Ice at 0.116 of the surface is true and was no help; ice at 0.00 of the window in
  front of the user is the fact that mattered.
- **Ranking is a lookup, and a lookup is a cliff (2026-08-23).** Blending the shelf between the two
  nearest plates fixed the seam, and a wall survived it: boundary **kind and intensity belong to a
  pair of plates**, so they change discontinuously the moment a different plate becomes
  second-nearest - along a line through the cell interior, far from any seam, where the seam blend
  has already saturated to 1.000 and smooths nothing. Measured at latitude 48.7: elevation stepped
  **0.277 to 0.528 across 1.04 metres, a grade of 7.24 - an 82 degree wall** - with identical shelf
  and identical seam distance on both sides, reading Divergent on one and ContinentalCollision on the
  other. Carrying both candidate neighbours and crossfading on how close they are to swapping fixed
  it: max grade 7.24 to 2.80, medians and biome mix unchanged to within a point.
- **A median cannot see a wall.** Land grade median at that latitude was 0.069 - the smoothest window
  measured anywhere - while it contained an 82 degree cliff. Report a maximum alongside every median,
  and when the maximum is bad, print what the lookup was doing on each side of it rather than
  guessing. The first guess here was wrong (a suspected off-by-one in the seam smoothstep) and the
  measurement took two minutes.
- **Fix the discontinuity, not the symptom.** The crossfade only acts where the ranking is close to
  changing hands, so away from those lines the field is bit-for-bit what it was. That is why the
  biome mix survived a change to the plate machinery - worth aiming for deliberately, because a
  global retune would have invalidated every window measurement taken this session.
- **A round world is a display transform, not a spatial model (2026-08-23).** Creature positions stay
  two floats on a flat 50-unit square with Euclidean distances; `ArenaProjection` maps them onto a
  sphere *after* the tick. No hash moves, no flag, nothing re-measured. The trick that keeps the
  camera working is putting the planet's centre at `(0, -R, 0)`, so the arena's centre lands on the
  origin with its normal pointing up - there the mapping is the identity, and a rig written for flat
  ground with up = +Y needs a zoom ceiling and a far clip and nothing else. **Ask what has to be true
  for the existing code to keep working, and then arrange for it.**
- **Check whether two views are already the same shape before "fixing" the scale.** I predicted that
  drawing the planet at true radius would shrink the mountains. It does not: the preview's relief is
  a *fraction* of radius, so 0.06 at radius 60 is 3.6 units per elevation unit, and at radius 500 it
  is 30 - exactly what the arena uses. The globe and the ground were always the same shape at
  different sizes. A ratio that looks like a discrepancy may be the same number twice.
- **Curve the existing mesh, do not write a curved mesh builder.** The patch is remapped from the
  flat builder's output. A spherical copy would be a second implementation of the same geometry, and
  the last two times this project had those they drifted until the diagnostics described a mesh
  nobody was looking at.
- **Cache derived quantities in the coordinates they are asked about.** Ground heights are taken from
  the flat vertices before curving, because "how high is the ground at this arena position" is a
  question in simulation coordinates; reading it off curved vertices would fold the planet's radius
  into every creature's height.
- **Derive a shared limit, never pick it twice (2026-08-23).** When the simulation began reading
  terrain it had to sample at the frequency the arena mesh resolves - computed the same way
  `BuildPatch` computes it, not chosen to look similar. A blunter or sharper field means a creature
  climbing a bump nobody drew, and the join would be a lie in the small while looking correct in
  every screenshot.
- **Hold the output ranges when you replace the source of a field.** Moisture .15 to 1, temperature
  .20 to 1, fertility .20 to 1 were kept exactly when terrain took over from independent noise, so
  the coming re-measure reports the **shape** of the field changing rather than its scale. Rescaling
  at the same time would move every plant result for a reason that has nothing to do with terrain,
  and nothing could be attributed afterwards.
- **A mutable static is fine in Presentation and forbidden in Simulation.** The tuning panel's
  settings object was the right trade while generation was presentation-only. Moving generation into
  Simulation, the same static becomes behaviour outside `SimulationConfig` - invisible to the
  configuration hash, so two worlds with equal hashes could diverge and every fingerprint guarantee
  quietly stops holding. Promotion means **removing the ambient default**, not carrying it along.
- **Presentation has no instrument that means anything.** Four bugs shipped in a row on the spherical
  view, every one compiling cleanly and passing 503 tests: behaviour wired into a terrain path most
  scenarios never take, a `Camera.main` lookup that never resolves when the presenter builds its own
  camera, a rig with no yaw at all, and a focus that panning clamped and zooming did not. **Say
  plainly that a presentation feature is unverified**, name the two or three things most likely wrong,
  and let the human look - do not report it as working.
- **Use the reference the file already uses.** `Camera.main` only finds a camera tagged MainCamera;
  the rest of the presenter used `_simulationCamera`, the one it builds itself. The new method's null
  check swallowed the mismatch and the feature silently did nothing. When adding to an existing file,
  copy how its neighbours reach the same object.
- **One clamp, used by everything that moves the value.** Panning clamped the camera focus; zooming
  toward the cursor did not, so a shallow ray meeting the ground far away flung the focus out of the
  world in a single notch. Two writers, one invariant, one of them enforcing it.
- **A big file can be split without reading it (2026-08-23).** A scanner that tracks brace depth -
  ignoring braces inside strings and comments - lifts whole members with their doc comments into
  `partial class` files. Nothing is rewritten and no member changes class, so behaviour cannot change.
  `Prototype1Presenter` 1886 to 1033, `SimulationWorld` 2058 to 844, `DecisionSystem` 1021 to 592, in
  one pass each, compiling first time, **with none of the files entering context**. Simulation splits
  are additionally proved by 503 green tests including every pinned hash literal.
- **Oversized files cost usage on every unrelated search.** The presenter was 95 KB and every grep for
  one hook paid for all of it; two of this session's bugs came from wiring behaviour into paths inside
  it that had not been read. Split mechanically **before** working in a file that big, not after.
- **A mechanical split has a limit, and it should be reported rather than forced.**
  `GeneticClusterHistory` (1324 lines) has almost nothing at class indent - the bulk is nested types -
  so the pass finds nothing to move. That is a decomposition needing comprehension, which is a
  different and larger job. Say so instead of half-doing it.
- **A field can be correct, live, and still deliver nothing (2026-08-23).** Terrain-driven
  environment passed every equality test against the generator and changed the state hash in every
  seed, and moved no plant conclusion - because a 50-unit arena is 0.1 radian on a 500-unit planet,
  and continental climate is nearly constant across it. The terrain field is *more uniform* than the
  hand-written one it replaced (moisture sd 0.005 against 0.283 at seed 161). **Measure the field's
  variance over the window before concluding anything from a null result**, or "no difference" and
  "no signal" are indistinguishable.
- **Run the control arm yourself when the original instrument is gone.** The recorded plant corpus
  came from an uncommitted probe, so any difference against it is unattributable between the change
  and the harness. Running flat and terrain arms in one sweep makes the comparison internal - and the
  flat arm doubles as a fidelity check against the recorded numbers (`SeedProductionRate` reproduced
  43/120 up exactly). **Commit the instrument**, which is why `tools/PlantSweep` exists.
- **Check the scenario's constants, not just its flags.** The sweep first ran at
  `SimulationConfig`'s default `maximumPopulation` of 1,000 rather than the recorded 48, and the cap
  never bound: populations ran to 134 with extinctions in half the seeds. Every flag was right and
  the ecology was wrong.
- **A null result is only worth having once the signal exists (2026-08-23).** "Terrain changes no
  plant conclusion" meant nothing while the terrain field was flatter than the field it replaced.
  After a local band restored comparable spread (moisture sd .207 against .240), the same null became
  a finding: the corpus is robust to where its environment comes from. **Re-run the null after
  fixing the thing that made it uninformative** - do not bank the first one.
- **Regional signal plus local band, not one field doing both.** Terrain climate sets the arena's
  mean and a zero-centred band supplies within-arena variation. The band's strength is chosen to
  MATCH the previous field's spread, not to maximise it: past that point the regional value stops
  mattering and the join is decorative again.
- **A feature that is not visible has not landed (2026-08-23).** Rivers were carved correctly - the
  probe measured a 1 m channel across 2.4% of the window, and four tests passed - and the render
  showed nothing. Terrain relief runs to tens of metres, so a metre of cut is invisible at every zoom
  anyone uses. **Render it before believing it**; the fix was a colour, not a depth.
- **Walk a coarse field, carve a fine one.** A downhill walk at full detail stops in the first
  metre-wide hollow it meets. Rivers follow the shape of a continent, so the walk reads a deliberately
  blunt field (`WalkFrequency = 24`) and the channel is applied to the sharp one. The cost is that a
  river can sit slightly off the finest valley floor; the alternative is erosion, which is a different
  and much larger feature.
- **Store a path as segments, not as the points you sampled.** Distance-to-nearest-point rises between
  samples, so a channel measured that way scallops at the sampling frequency of its own path -
  measured at 0.999 / 0.848 / 0.999 along one straight reach. Point-to-segment is four extra lines and
  removes the artefact entirely.
- **Subtracting a feature is not the same as inscribing one (2026-08-23).** A river cut as a fixed
  slot keeps the hillside it crosses and reads as a groove scratched across a slope. Inscribing means
  the surrounding ground is **blended toward** the feature over a compact support, so the land slopes
  into it. The same distinction applies to any feature meant to belong to terrain rather than sit on
  it - roads, craters, lake beds.
- **A modifier that can raise ground will raise it somewhere you did not look.** Blending terrain
  toward a profile built from a coarser field filled every dip the coarse field could not see, and the
  river ended up on a raised bank. `min(profile, terrain)` is the whole fix. Ask of any elevation
  modifier: which sign is it allowed to have?
- **Discrete direction choices show up as right angles.** Steepest descent over eight compass points
  makes staircases wherever the ground is nearly flat across one step. Take the gradient from all the
  samples and carry momentum between steps - the standard particle-erosion trick - and the same walk
  produces curves.
- **Structure that must exist has to be generated deliberately.** Rivers spaced far enough apart not
  to corrugate the land never meet, so the network had zero confluences and read as parallel
  scratches. Tributaries are a second pass with their own spacing, kept only if they join. Hoping an
  emergent property emerges is not a plan.
- **Ask what a feature must DO before choosing how to build it (2026-08-23).** Rivers were written
  twice and reverted. The second attempt fixed every visual artefact by name - monotone profile,
  valley blend, cut-only modifier, momentum instead of compass directions, tributary pass, tapered
  heads - and still did not read as a river, because the acceptance test was never "is the geometry
  right". It was drain, erode, animate: three properties a pure function of one direction cannot have
  at any quality of tuning. **Three questions asked at the start would have saved both attempts.**
- **"Get it visible and see if it reads" is not always the cheap first step.** That was the recorded
  reasoning for doing painted rivers before real hydrology, and it cost two implementations to
  discover something derivable from the data structure. Prototype first when the *unknown* is
  perceptual; reason first when the unknown is structural.
- **When the user asks whether to wait for a prerequisite, answer about the GOAL, not the patch
  (2026-08-23).** Asked "if we should wait and revisit after something else is built, do that
  instead", the answer given was "this fix needs no prerequisite" - true of the valley blend, false of
  what was actually wanted, which was rivers that drain, erode and animate. Those need the chunk
  system, and the doc said so before either attempt started. **A question about sequencing is a
  question about the objective.**
- **A new control scheme has to be checked against the keys already bound (2026-08-24).** The free
  camera wanted WASD; `D` resets to the drought scenario, `E` to the starter habitat, `F` to food
  scarcity. Gating movement behind a held right mouse button - Unity's own scene-view idiom - kept
  both, and cost nothing. Grep the input handler before choosing keys, not after someone reports that
  flying left restarted the simulation.
- **Put the arithmetic where a test can reach it.** Every camera bug this project shipped was found
  by a human in Play mode, because the rules lived inside `LateUpdate` next to `Input`. The free
  camera's speed, height bounds and pitch limit are pure functions in `FreeCameraMotion` with no
  Unity types, so the headless project compiles them. **A MonoBehaviour is not a place to keep
  rules.**
- **Measure an artefact before fixing it (2026-08-24).** A sawtooth seam in the chunked planet looked
  exactly like an oversized skirt. Shrinking the skirt fourfold changed the render by zero bytes -
  the seam was a flat-shaded coastline quantised to the triangle grid, which is the art style. Two
  renders and a measurement cost less than the fix would have.
- **A cost model has to be calibrated against the thing it models.** `ApproximateLeafCount` predicted
  150 chunks; the renderer drew 908, because a chunk exists when its *parent* splits, not when it
  does. The test passed the whole time and asserted nothing true. **An estimator nobody has checked
  against a measurement is a comment with an assert in it.**
- **Check whether an open worry is still true before acting on it (2026-08-24).** `PatchLift` had been
  flagged for two sessions as "almost certainly too small". It was - against a backdrop that no longer
  exists. Once the backdrop had level of detail, the two surfaces measured identical and the item
  closed without a line of code. **A stale worry costs more than a stale fact, because it asks for
  work.**
- **An optimisation is a hypothesis until it is measured.** Doubling the chunk grid and dropping a
  tree level looked like a strict win - same finest triangle, fewer chunks, one fewer seam. It
  tripled the triangle count, because raising the band limit by a level makes the coarse chunks
  denser too and those are most of the sphere. Reverted, and the number is in the comment so nobody
  tries it twice.
- **A liveness test against the fingerprint proves nothing (2026-08-24).** `ComputeStateFingerprint`
  folds in `ComputeConfigurationHash`, so two worlds differing only by a flag have different
  fingerprints whether or not the flag does anything. The test passes, the flag could be a no-op, and
  the matching inertness test cannot be satisfied at all. **`ComputeBehaviorHash` is the config-free
  one** and is what "did these evolve the same way" means.
- **The project's own guards are the fastest reviewer.** Adding one flag failed three tests
  immediately: the manifest did not mention it, the configuration hash did not cover it, and the
  pinned property count disagreed. None of that needed a person to notice.
- **Carry a control column in every selection sweep (2026-08-24).** `CreatureSweep` reports
  `NeutralMarker`, a gene that responds to nothing by construction. In the four-seed smoke test it
  came out at t = 2.44 - the largest mover in the table - which said "this is drift" before anyone
  could read a story into the real columns. At 120 seeds it sat mid-pack, which is what made the null
  believable rather than merely quiet.
- **Check that the arms diverged at all before interpreting a null.** Half the slope-cost pairs were
  byte-identical, because half the arenas are flat. That could have been dilution hiding an effect;
  restricting to the pairs that diverged doubled the means and left every t identical, which settles
  it. Two lines of analysis, no rerun.
- **A note saying "this cannot be done mechanically" deserves the same suspicion as any other stale
  worry (2026-08-24).** `GeneticClusterHistory` sat at 1324 lines across two sessions because the
  handoff said its members were not at class indent and the bulk was nested types. Neither was true -
  the nested types were the last 55 lines. Thirty seconds of looking beat two sessions of trusting
  the note. Same shape as the `PatchLift` closure.
- **When every column moves together, including the control, it is composition and not effect
  (2026-08-24).** The focused slope run moved all fourteen gene means by about the same amount at
  |t| around 1.4 to 2.0, two of them past 2. `NeutralMarker` moved with them - and it cannot respond
  to anything - so the cause was differential extinction changing who was left to average, not
  selection. **Without the control column that table reads as a finding.**
- **Removing a ceiling can replace one problem with a worse one.** Raising the population cap from 48
  to 200 gave survival room to move, and also let populations overshoot and crash: 33 of 60 pairs
  went extinct in both arms, against 2 or 3 in 120 at the old cap. The metric that was saturated
  became the metric that is now mostly zero. Change a limit by a step, not by a factor of four.
- **Do not report a comparison conditioned on a post-treatment variable as if it were evidence.**
  Population among the pairs where both arms survived came out at t = -2.27, which looks like the
  clearest number in the run. Survival is what the treatment affects, so conditioning on it selects
  the sample. It is in the write-up marked as not to be used.
- **"Looks high" is a question about distribution, not about a total (2026-08-24).** Ice cover was on
  the open list as too heavy. The total was never the deciding quantity: 3% of a surface is Earth-like
  and 7% is plausible, while ice on every tropical mountain reads wrong at any total. Measuring
  *where* it was - 98.31% beyond 60 degrees - closed the item without touching a coefficient. Third
  stale worry closed by measurement in one session, after `PatchLift` and the claim that
  `GeneticClusterHistory` could not be split.
- **Measure the control's own noise floor before calling a result significant (2026-08-24).** The
  cap-100 slope run put `occupied_slope` at sign z = 2.41 - but the `NeutralMarker` control, which
  cannot respond to anything, came in at z = -1.64 on the same test. The floor is not zero. A result
  is only as impressive as its distance from what the control does, and reporting 2.41 without 1.64
  beside it would have been the more flattering half of the truth.
- **A behavioural readout beats a genetic one for showing a mechanism works.** Where an animal is
  standing responds within a lifetime; a gene needs selection, generations and a strong enough
  pressure. The slope cost moved `occupied_slope` and no gene at all, which is the coherent result -
  and it was measurable with no new simulation state, from positions that already existed.
- **A paired arm-against-arm design cannot see selection (2026-08-24).** Three creature sweeps
  reported flat gene tables, and every one of them was blind to the question by construction: a trait
  under strong selection in both arms cancels exactly. Asked the other way - drift from founders
  against a neutral control - two traits moved by a quarter of their range at t = 11 and t = 7.9.
  **Check what a design is capable of detecting before reading its null as an absence.**
- **Print the baseline, not just the change.** Drift toward the centre of a bounded range is what
  symmetric mutation does on its own, so a shift is only selection if you know where the gene
  started. Every gene here began at 0.50, which is what rules the artefact out - and it is the exact
  failure that got `p4-defense-selection-demonstrated` retracted on the day it was written.
- **A dead run has no gene means, and including it corrupts every column (2026-08-24).** Extinct
  worlds report every gene as zero, so their "drift" is minus the founder value on all thirteen
  columns at once. With them in, the lean scarcity arm showed everything down ~0.21 with the control
  moving identically and every ratio at 1.0. **Exclude them - and then say out loud that doing so
  conditions on survival**, which the treatment affects, so magnitudes stop being comparable between
  conditions even though directions remain sound.
- **Vary one thing by deriving it, not by swapping in a neighbour.** A "scarcity" arm built by
  substituting a scenario from another family killed 30 of 30 runs in both arms - those layouts are
  calibrated against different founder counts and flags. `SimulationScenario.Scaled(id, factor)`
  multiplies the amounts of the *calibrated* layout instead, so scarcity differs from abundance in
  exactly one respect and a difference is attributable.
- **Check the mechanism in the source before predicting or denying an outcome.** "Do creatures shrink
  when food is scarce" was answerable by reading three lines: mass is `0.6 * 4^BodySize`, it is
  charged against energy per distance and water per second, and nothing pays for being large. The
  prediction followed from the code, and the measurement then confirmed it with a dose-response.
- **Trace the call path before testing a causal story (2026-08-24).** The queued explanation for the
  strongest selection in the model was "the terrain join introduced the temperature field", and one
  grep killed it: creature thermoregulation reads `TemperatureField.Sample`, a fixed sine, while the
  join builds the `EnvironmentField` that feeds plants. Both arms of that experiment would have been
  the same experiment. **A hypothesis about a mechanism is checkable against the wiring for free, and
  the run only becomes worth doing once the wiring permits the answer.**
- **An endpoint hides the shape; checkpoints show it.** Every creature measurement until now read the
  gene means once, at the end. Sampled twelve times instead, temperature tolerance moved 0.28 in the
  first 8,000 ticks and **0.004 in the last 4,000** - a plateau, which is a completely different
  claim from a drift and points straight at a saturating mechanism. Endpoint drift and trajectory are
  different instruments; a flat tail is invisible to the first.
- **Derive the equilibrium, then check the number.** Tolerance is `2 + 8*gene` against a field
  bounded at 8 degrees, so the benefit runs out at gene 0.75 exactly and the residual cost is ~1% of
  upkeep. Predicted 0.750; measured plateau 0.7475 with the join off and 0.7790 at 40 seeds with it
  on. **An arithmetic prediction that lands is worth more than a larger sample of an unexplained
  effect** - and the slight overshoot is itself informative, because an asymmetric landscape (health
  loss below, 1% upkeep above) rests its mean above the point where the benefit ended.
- **Measure the environment the animals actually experienced, not the one on paper.** Sampling
  `|T - 20|` under every living creature is what fixed the ceiling at 8.000 and turned "0.75 in
  principle" into "0.75 in this world". It needed no new simulation state - positions and the field
  were both already there. Same trick as `occupied_slope`.
- **The strongest signal in a model can be an artefact of a placeholder.** Temperature tolerance
  dominates selection because it is adapting to a decorative sine with no seasons, altitude, latitude
  or terrain. The gene is fine. **When a result is much larger than everything around it, ask what it
  is responding to before asking why it is strong.**
- **"No detectable selection" is a claim about n, and it expires (2026-08-24).** Forty seeds put ten
  of thirteen traits in the undetected bucket. Eighty seeds pulled three of them out, each consistent
  across three resource levels against a control under |t| = 1.13. **Before recording a null, record
  the sample size beside it** - and prefer "not here" to "inert", which is the phrasing that made
  this correction cheap rather than embarrassing.
- **A sweep run for one question often answers another for free.** The dose-response sweeps were run
  for body size and happened to contain the decisive test of the thermal ceiling: three resource
  levels, survival from 79 worlds down to 12, and the temperature-tolerance *endpoint* moving 0.02
  while the drift moved 0.07. **Read the whole table of a run you already paid for** before designing
  the next one.
- **A placeholder can be replaced without re-baselining anything (2026-08-24).** Creature temperature
  moved from a fixed sine to the world's own climate field, and every recorded thermal result still
  reproduces, because a `default ClimateField` **is** the sine. Making the neutral value of a new type
  the old behaviour is cheaper than a branch at every call site and impossible to get half-right - one
  test comparing `default` against `TemperatureField` over the arena pins it.
- **Write the prediction down even when it is wrong, especially then.** The prediction was that a
  terrain-driven temperature would be nearly uniform over a 50-unit arena and so produce smaller
  deviations. **Deviations barely moved** - mean 3.92 against 4.28, the full 8-degree span reached in
  both. What changed was between worlds, not within one: the endpoint's standard deviation across 40
  worlds went 0.0744 to 0.1454. Having the wrong prediction on the page is what made the right
  distinction obvious.
- **Look at the spread across runs, not only the mean of them.** Both arms above have a perfectly
  reasonable mean and they are completely different environments. A uniform field applies the same
  pressure to every world; a real one gives one arena a temperate continent and another a cold one,
  and only the variance shows it.
- **Liveness is not the same as having a benefit (2026-08-24).** `GeneLivenessAnalysis` asks whether
  perturbing a gene changes the behaviour hash. A gene that only ever costs something changes the
  behaviour hash, so it passes - and `MetabolicPace` has been passing while raising two drains 2.14x
  across its range and returning nothing. **A caller search finds the readers; only reading them
  tells you which side of the ledger they are on.**
- **A column that keeps crossing in one condition is a question about the mechanism, not about
  seeds.** `metabolic_pace` crossed |t| = 2 at lean and only at lean, twice. The reflex is more runs;
  the answer took one reader search and needed **no new simulation at all** - six corpora already on
  disk had the dose-response in them.
- **Build the flag to answer the question, then let it say no (2026-08-24).** `MetabolicPace` was a
  pure cost, so it got the obvious benefit behind a flag and a written prediction of a sign flip
  across the resource ladder. **There was no sign flip** - the bleed halved and stopped there. The
  flag, the tests and the corpus stay committed anyway, because the next person now starts from a
  measurement rather than from the same argument.
- **A benefit routed through a shared resource is diluted by everyone else having it.** Faster
  ingestion sounds like a private gain and is not: contested sites are divided between requesters, so
  every competitor getting faster partly cancels it. **When adding a trait benefit, ask whether the
  channel is private or common before predicting its sign.**
- **The control is what makes a row discardable rather than tempting.** The scarce condition produced
  `metabolic_pace` at t = -3.12, which looks like the strongest result in the run until you see
  `neutral_marker` at **t = +3.31** on 4 surviving worlds of 80. Without the control that row would
  have been written up.
- **A huge t with a tiny shift means mutation-limited, not weak (2026-08-24).** `UrgencyExponent`
  drifts -0.04 at t = -19.4, which reads as contradictory until you notice **founder is exactly
  0.5000 in every run**: four genes are monomorphic in the founder profile, so selection has nothing
  standing to act on and waits for mutation to supply it. **Read the founder column before calling a
  gradient weak** - the gradient and the response are different quantities.
- **The same defect looks different from the gene side and the system side.** "Grazing is uniform
  because `ComputeNeedGain` saturates" was already a comment in the source and a known plant-side
  problem. From the gene side it is why `UrgencyExponent` has no trade-off and is under the most
  reproducible selection in the model. **When a gene turns out to be monotone, the missing punishment
  is usually somewhere else and already known about.**
- **Nine corpora on disk answered a question that would have cost nine new sweeps.** Two genes were
  fully characterised tonight without running anything new. **Before designing a run, grep the
  corpora already committed for the column you care about.**
- **Ask which channel is open before building the instrument (2026-08-24).** Two hypotheses both
  predicted the `UrgencyExponent` sign - fertility via the reproduction gates, or survival via
  starvation. Counting causes of death cost **one run** and retired the survival one outright:
  starvation and dehydration were 15 of 5,619 deaths against 96.9% old age. The configuration value,
  hash bump and guard updates were then spent on the one surviving explanation instead of on both.
- **Byte-identical output is a bug report until proven otherwise.** A gate change produced two files
  identical to the baseline, which looked like a clean null. It was a wiring miss - the threshold
  reached `CanReproduce` but not `CanSeekMate`. **It was also a finding**: lowering only the lower
  gate does nothing, because the higher one binds first. Chase identity before writing it up.
- **A parameter you cannot vary is a hypothesis you cannot test.** The 0.7/0.8 literals had been
  stable for the life of the project and turned out to be the dominant selective channel in it -
  slackening them removed selection on five traits at once. **When a constant is load-bearing, make
  it a knob before arguing about it.**
- **Watch the control in every arm separately.** The slack-gate arm's control moved to t = 2.55 while
  the default arm's sat at 0.17. Same code, same seeds, different noise floor. A t of 2 does not mean
  the same thing in two arms of the same experiment.
- **A quantity that is only ever subtracted from is worth checking for (2026-08-24).** `Health` had
  five subtractions and no addition anywhere in the simulation, which is invisible to every test
  because nothing asserts a value can go up. It mattered because health gates reproduction, so the
  ratchet meant permanent sterility rather than injury. **Grep for the writes to a field, not the
  reads, and look at their signs.**
- **Two mechanisms found in the source tonight, two overstatements.** The `ComputeNeedGain` saturation
  was not the cause of the urgency drift, and the health ratchet explains only 19% of the thermal
  selection. **Finding a real mechanism in the code is not the same as finding the dominant one** -
  say "contributes" until the measurement says otherwise.
- **t-statistics are not comparable across arms with different survival.** The health-recovery arm has
  zero extinctions against one, and every column's |t| rose. None of that is attributable to healing;
  a cleaner arm has less composition noise. **Compare the column the run was designed to test, and say
  out loud that the rest moved for a different reason.**
- **A ratio against a near-zero control is a display artefact.** With the control at -0.0000 the "vs
  control" column printed 30,988x. Ratios need a denominator with a floor, or the column needs to be
  read as t.
- **A continuous cost needs a continuous benefit (2026-08-24).** `MetabolicPace` charges energy and
  water *every second* and both benefits tried pay only sometimes - ingestion while eating (and it is
  shared, so competitors cancel it), healing while injured (and mean health is 0.9939, so nearly
  never). Neither moved the gene. **Before designing a trade-off, check that the two sides are
  collected on the same schedule**, not just that they point in opposite directions.
- **Predicting a null and getting it is worth as much as predicting an effect.** The healing benefit
  was predicted to do nothing, for a stated reason - the channel is idle - and it did nothing. That
  turned a third failed attempt into the explanation for all three.
- **A flag that is inert for a benign reason belongs on the known-inert list with the reason.**
  `metabolicHealingEnabled` cannot act without `healthRecoveryEnabled`, the same shape as slope cost
  needing elevation. Pinning it there means the guard now fails if it ever becomes live *without*
  healing, which would mean something is healing creatures unasked.
- **Count the blast radius before a rename, and include the corpora (2026-08-24).** Renaming
  `MetabolicPace` looked cheap - 18 code files, one script. The real cost was **10 committed CSV
  corpora carrying `metabolic_pace` as a column header** plus 82 mentions across 38 docs: the rename
  would have severed every recorded result from the code it describes. **An authoritative doc comment
  at the definition captures the entire benefit of a rename at none of its cost**, whenever the
  problem is "this name misleads" rather than "this name is used wrongly".
- **"Adds realism" and "removes an artefact" deserve different defaults.** The slope cost and the
  terrain temperature add something a designer chose. Health recovery removes something nobody chose -
  a quantity that only decrements happened to gate a quantity that decides fitness. That distinction
  is the argument for which flags should eventually flip to default on, and it is worth recording per
  flag rather than rediscovering.
- **A threshold's effect can be a margin rather than a switch (2026-08-24).** The mate-seeking gate
  was predicted to bind or not bind, with a cliff where it crossed the population's natural energy
  level. There is no such level: **raising the gate raises the population's energy**, because
  creatures work harder to clear a higher bar. The margin above the gate ran 0.167 down to **0.006**
  across five values and selection intensity tracked it, accelerating. **Before predicting a
  threshold effect, ask whether the thing being crossed is itself free to move.**
- **Five points beat one, and the shape was the finding.** One alternative gate value said "the gate
  is the driver". Five said "and the default sits on the steepest part of the curve, where a small
  change to the parameter is a large change to how hard the model selects" - which is the part that
  matters for tuning and was invisible from a single comparison.
- **Two points make a slope out of anything (2026-08-24).** Comparing gate 0.70 against 0.45 said
  "five traits stop being selected". Five gate values said one trait does, one might, and three were
  noise whose default-gate values were marginal to begin with. **The extension cost a grep against
  corpora already on disk and deleted a claim that had been written up and committed.** When a result
  comes from exactly two conditions, the cheapest possible next thing is a third.
- **An instrument that varies everything together varies nothing (2026-08-24).**
  `SimulationScenario.Scaled` multiplies amount, capacity and regeneration by one factor, so the
  ratios between them - which are what set an ecology's dynamics - never move. Five resource levels
  from 0.40x to 1.00x all collapsed identically, and could not have done otherwise. **Before sweeping
  a knob, check that it changes a ratio and not just a unit.**
- **When a bound turns out to be load-bearing, say so.** The population cap was treated as a ceiling
  on a self-regulating ecology for the life of the project. 2.0x regeneration survives 23 of 24 runs
  at cap 250 and 3 of 20 at cap 500 - same ecology, same forage. **The cap was supplying the
  regulation, not bounding it.** Same shape as the reproduction gate turning out to be the dominant
  selective channel: the constants nobody questions are the ones doing the work.
- **Report the spread, not the mean, whenever a ceiling might be involved.** Eleven committed corpora
  and 4,080 runs have a population column with zero variance, and no summary statistic in them says
  so. **A carrying capacity produces a distribution; a cap produces a constant** - printing sd would
  have caught it years of runs earlier.
- **A step function where a curve belongs produces boom and bust (2026-08-24).** Reproduction was
  gated at 70% and 80% of three needs, so nothing told a population that resources were *tightening* -
  only that they were gone. Replacing the step with a graded cooldown took starvation from 55-64% of
  deaths to **exactly zero** and survival from 3 of 20 to 19 of 20. **When an ecology overshoots and
  crashes, look for the threshold that should have been a slope.**
- **Scale the cooldown, not the probability.** A graded breeding *chance* needs a random source inside
  a deterministic tick, which would have cost a seeded stream and a hash change. Scaling how long a
  creature waits gives the identical negative feedback with no randomness at all. **Check whether a
  rate can carry what a probability was wanted for.**
- **Measure headroom against the threshold that matters, not against zero.** A creature that cannot
  breed below 0.70 is not "half fed" at 0.50 - it is simply out. Normalising `(condition - gate) /
  (1 - gate)` puts the whole curve in the range where it can act; normalising against zero would leave
  the brake fully applied nearly everywhere.
- **A liveness test can only see a flag whose machinery has had time to run.** The graded-fertility
  liveness check failed at 600 ticks because `AdultAgeSeconds` is 20 seconds - 1,200 ticks - so
  nothing had bred once, let alone twice. **Before calling a flag inert, check the run is longer than
  the slowest thing it touches.**
- **Do not change two things in one arm (2026-08-24).** The first plant comparison moved the cap and
  added the fertility brake together, and read as though the brake were harmless. A third arm - cap
  raised, no brake - showed raising the cap alone *raises* the population and the brake is what
  collapses it. **One arm, one change, every time**, and the temptation is strongest when both changes
  feel like "the new configuration".
- **A constant that works is not a constant that generalises.** Brake strength 3 turns boom-and-bust
  into a carrying capacity in one scenario family and destroys the population in another, where 1 is
  right. **A factor of three between the best and worst condition tested is the definition of a
  tuning parameter**, and shipping it as a `const` would have exported one ecology's calibration to
  every other.
- **Publishing a result does not end the work on it.** The "at every cap tried" claim survived about
  an hour. The qualification came from doing the *next* thing - applying it to a second scenario -
  rather than from re-reading the first. **The fastest way to find the boundary of a result is to use
  it somewhere else.**
- **A tool that writes to a fixed filename will overwrite the evidence eventually (2026-08-24).**
  `tools/PlantSweep` hardcoded its output as `p4-terrain-local-band-2026-08-23.csv`, a **committed
  480-row corpus**, so every run of it silently replaced a recorded experimental result. On this date
  one did - 480 rows became 160 from a 40-seed tuning sweep - and it was caught only by reading
  `git status` while writing a summary. **Encode the configuration in the output filename, and never
  let a tool reproduce the name of a committed corpus.** A corpus is the evidence for a written
  conclusion; rewriting one invalidates the conclusion without touching the document that states it.
- **`git status` is part of finishing, not part of tidying.** The clobber survived several commits
  because staging was done by explicit path every time - which is the rule that prevents accidents,
  and also the rule that hid this one. **Read the full status, including the files you did not
  stage.**
- **Make the program report on itself rather than asking a human to read a GUI (2026-08-24).** The
  first attempt at profiling asked the user to open Unity's Profiler and relay three figures. They
  reasonably asked why it could not just be a text file. It could: `Logs/performance.txt`, written
  every five seconds, took one small class and produced an artefact that can be diffed, committed and
  re-checked. **A measurement that lives only in somebody's eyes is not a measurement.**
- **Instrument the suspect instead of accusing it.** A 197 ms worst frame looked like a recurring
  stutter and the heatmap - 16,384 terrain samples every two seconds - looked obviously guilty. Timed,
  it reports **0.00 ms**, and the second reading's worst frame is 19 ms: the spike was first-entry
  cost. **Percentiles say a hitch happened; only named sections say what did it.**
- **Check the view is showing the thing being measured.** The first reading had 49 renderers because
  the instruction was to press `Y`, and the chunked planet is behind `O`. It was nearly written up as
  "the planet is cheap" with the planet off screen. **Confirm the subject is on screen before
  believing the number.**
- **Numbers derived from reading code are not measurements, and should be labelled.** "908 chunks
  means 908 renderers" and "232k triangles" both came from walking the quadtree. Measured: 1,090 and
  566,272. The two recorded figures never reconciled with each other, which was the clue.

## Standing facts added 2026-08-24 (late)

- **Five flags exist that did not before**, all default false: terrainDrivenTemperatureEnabled,
  healthRecoveryEnabled, metabolicIngestionEnabled, metabolicHealingEnabled, gradedFertilityEnabled.
  Two configuration values joined them: ReproductionNeedFraction (0.7) and GradedFertilityStrength
  (3.0). ConfigurationHashVersion is 9; the pinned property count is 53.
- **The Y terrain playtest runs slopeMovementCost, terrainDrivenTemperature and healthRecovery.**
  Graded fertility is deliberately NOT on it - it changes population dynamics at the root.
- **metabolicHealingEnabled is on KnownInertFlags** in LivenessTests, because it cannot act without
  healthRecoveryEnabled. If it ever reports live there, something is healing creatures unasked.
- **The game writes Logs/performance.txt every five seconds while playing**, appending. Read it
  rather than asking anyone to open the Profiler.
- **tools/split_doc.py splits long markdown mechanically.** Use it before any document read at
  session start passes a few hundred lines.
- **tools/PlantSweep no longer writes to a committed corpus name.** It used to overwrite
  p4-terrain-local-band-2026-08-23.csv on every run.
- **CreatureSweep arms:** --thermal, --deaths, --relief, --focused, plus --join=off,
  --terrain-temperature, --metabolic-ingestion, --metabolic-healing, --health-recovery,
  --graded-fertility, --gate=X, --brake=X, --scale=X, --regen=X.

