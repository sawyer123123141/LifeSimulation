## 5. Lessons log

Append with a date. Keep each entry to what a future session must not repeat.

**2026-08-17 — A mechanism story is not evidence.** Four conclusions were wrong
in one session because a plausible causal story was written before anything was
measured: place memory blamed as the cause of extinctions (the subsystem never
ran); "the calibration constraint set is unsatisfiable" (it was satisfiable);
"the Persistence fix resolved B-4" (Persistence is Legacy-only, so it did not);
and a site-count sweep read as proving site count when it also varied geometry.
Each was caught, but only after work had been built on it. Measure first.

**2026-08-17 — Transcribe measured configurations; never reconstruct one that
looks equivalent.** A probe measured four *clustered* plant sites; the
implementation used four *spread* sites on the reasoning that the count was what
mattered. Extinctions went from 0/30 to 16/30. Copy the exact numbers that were
measured, or measure the exact numbers you intend to ship.

**2026-08-17 — Confounded sweeps.** When a sweep generates its arms
procedurally, check what *else* changes between them. The site-count sweep moved
sites along a grid, so count and spatial spread moved together and the result
was credited entirely to count.

**2026-08-17 — Positional constructor arguments silently drop genes.**
`GenomeInheritance.CreateChild` passed 23 of `Genome`'s 24 positional
parameters, so `persistence` took its default for every creature ever born, and
nothing failed. When adding a gene, add it to the inheritance call, the
constructor, the hash, and a test that asserts *every* gene transmits with
pairwise-distinct values.

**2026-08-17 — A static caller-search is not a liveness check.** Grepping for
callers finds "nothing calls this". It does not find code that runs every tick
against permanently empty data (`RecordFailedPlaceSearch`), nor code that
computes a real value nothing consumes (`Commitment`). A real liveness probe
must record that a mechanism's output **entered creature state or a decision
score** — and must be excluded from `ComputeStateHash` so it does not change the
simulation it measures.

**2026-08-17 — Engine bounds are not ecological parameters.** The calibration
scenario ran two plant sites against a population cap of 48, so carrying
capacity sat below the array bound: population grew to the cap and collapsed
every time. Sweeping lifespan and cap both read as "unsatisfiable" because
neither could reach the binding variable. When every arm of a sweep fails,
suspect the variable is not in the sweep.

**2026-08-17 — A failing regression guard is usually right.** The six-site
change shipped with a guard that immediately failed. The guard was correct and
the implementation was wrong. Do not reach for the tolerance.

**2026-08-18 — When a trait will not move, check that it *can* move before
tuning pressure.** Three documents in a row treated flat plant defense as a
calibration problem and proposed raising grazing pressure. Reading the
consumption path took ten minutes and showed `ConsumeAt` removes biomass with no
defense term: defense protected zero tissue, so no pressure setting could have
produced a gradient. Before sweeping a parameter to make selection appear, verify
the trait has a path to fitness at all. `GeneLivenessAnalysis` answers this for
animal genes; for plant genes, read the consumption path.

**2026-08-18 — A prediction that fails is still a measurement; report both.**
The prediction here was that defense would be selected *down*, being a cost that
buys nothing. It was not — deltas were pure drift. The cost was unrealized too,
because patches sit at capacity and the penalty is charged against growth *rate*.
"Cost-free and benefit-free" is a different diagnosis with different fixes than
"costly and unrewarded", and only measurement separated them.

**2026-08-18 — Check the sign of a feedback, not just its magnitude.** Defense
lowers nutrition per unit eaten, so consumers compensate by eating more: realized
grazing rose 2.2x from defense 0.0 to 0.9. The mechanism was not weak, it was
*inverted* relative to the ecology being modelled. A metric reported without its
sign checked would have read as "pressure exists, so the null is real".

**2026-08-18 — A caller-search misses readers inside aggregate expressions.**
The 2026-08-17 audit reported `Persistence` as having exactly three readers and
concluded it was behaviorally inert under P4. It missed `0.05f * genome.Persistence`
inside the `bodyMass` weighted sum — a real production path found immediately by
perturbation. Grep finds *names*; it does not tell you whether the value flows
anywhere. Use `LivenessTests`.

**2026-08-18 — A trait that only moves while the ecosystem collapses has not
demonstrated a gradient.** Sweeping plant regeneration down finally moved defense
(+0.1157 at founder 0.6, regeneration 3) — in an arm where 30/30 seeds lost their
animal population and plants reached generation 0. That number is not a positive
control and must not be cited as one. When a long-flat trait suddenly responds,
check the survival columns in the same row before celebrating.

**2026-08-18 — A one-dimensional pressure lever moves two things here.**
`RegenerationPerSecond` sets both the food supply for the whole animal population
and how depletable an individual patch is. Sweeping it cannot find a window where
grazing bites locally without starving the population globally — the arms trade
one failure for the other, the same shape as the lifespan and population-cap
sweeps that were declared "unsatisfiable" in 2026-08-17. When every arm fails in
alternating directions, the lever is coupled and the fix is a scenario redesign,
not another sweep.

**2026-08-18 — Selection cannot act without standing variance, and a uniform
founder value provides none.** Every plant-defense sweep from 2026-08-17 onward
seeded *all* patches with the same defense, varying it only between arms. Response
to selection is proportional to within-population variance, so the flat results
measured drift and nothing else. Seeding three of six sites defended produced
deltas of -0.05, ten to thirty times any earlier arm. Before concluding a trait
"does not respond", check that the founders actually differ in it.

**2026-08-18 — `dotnet test` passing did not mean Unity compiles.**
`tools/HeadlessTests/GlobalUsings.cs` carried `global using System;`, which Unity
has no equivalent of. A file using `System.Math` without `using System;` passed all
385 headless tests and then failed in the editor with seven `CS0103`s. The file is
now empty and commented to stay that way — emptying it produced zero errors, so
nothing depended on it — and `<ImplicitUsings>disable</ImplicitUsings>` is set.
Verified by deleting a `using` and confirming the headless build now fails (14
errors) where it previously succeeded. **If a green headless run is ever
contradicted by the editor again, check for global usings first.**

**2026-08-18 — A liveness test cannot tell you a trait is worth carrying.**
Perturbation detects influence, not reward, so a **pure-cost** gene passes it.
Plant `TemperatureTolerance` read live only because it charges `-.10f` growth,
while temperature entered `PlantGrowthSystem` as a raw limit no gene could improve
against. Prediction that it would read *dead* was wrong for that reason. When a
trait refuses to evolve, ask what its **benefit path** is — a reader is not a
benefit. `MoistureTolerance` next to it is the reference for what a real trade-off
looks like.

**2026-08-18 — Environment constants silently disable whole gene channels.**
`EnvironmentField` returns `Fertility = 1` and `Temperature = 1` on every
production path, so the growth limit always collapsed to `moistureAdaptation` and
two of three environment channels did nothing. Before adding a trait that adapts
to an environment field, check the field actually varies — otherwise the trait ships
as a tax. This is why terrain work needs the field *and* the adaptation term
together; landing only the field makes such genes more costly, not more meaningful.

**2026-08-18 — Compare an effect against its own sampling error, never against
another arm whose spread is small for structural reasons.** The -0.05 defense
decline was called "measurable directional selection" because it was 10-30x the
deltas in the uniform-founder arms. But uniform founders have no standing
variance, so no lineage can fix and their SD is structurally tiny — the
comparison measured the presence of variance, not of selection. Against its own
SE the decline is t = -1.44, 95% CI [-0.122, +0.032]. **Retracted the same day.**
This is the recorded "n=5 looked real, vanished at n=30" failure in a subtler
form: the error was not sample size but a baseline chosen for the wrong reason.
Compute SD, SE and a bootstrap CI before calling anything a response.

**2026-08-18 — A six-patch population is drift-dominated; expect to need far more
than 30 seeds.** Standing variance triples the outcome SD (0.208 vs 0.069), which
is lineage fixation, not selection. At that SD an effect near -0.055 needs roughly
230 seeds for 80% power. Raising the **patch count** shrinks the spread far more
efficiently than raising the seed count, because drift scales inversely with
population size. Check the achievable power before designing another plant-trait
experiment at six sites.

**2026-08-18 — A control that moves the trait downward would still be a valid
positive control.** The principle holds and is worth keeping: the blocker asks for
proof the setup can detect selection at all, and a demonstrated *decline* answers
that as well as a rise. Do not keep hunting for an upward response to prove a
pipeline works. (The 2026-08-18 attempt to supply one failed on significance, not
on direction — see the entry above.)

**2026-08-18 — A clamped score term silently deletes a whole decision dimension.**
`ComputeNeedGain` ends in `Math.Min(1f, ..)` and returned exactly 1.0000 for 88 of
88 patch-and-hunger combinations, ~10x over the clamp even at 5% energy. Patch
quality was therefore invisible to `IntentUtilityV1` foraging — grazers chose on
hunger and distance alone. When a scoring term is bounded, measure its realized
distribution before assuming it discriminates.

**2026-08-18 — Identical results across an arm flip means the flag is dead, not
that the effect is small.** A sweep adding `learnedResourceQualityEnabled` to the
deterrence arm returned numbers matching the previous run to four decimals in
every cell. That is not a weak effect — floating-point ecology does not reproduce
by coincidence. Bit-identical output is a liveness signal; read it that way
immediately instead of interpreting the "difference".

**2026-08-18 — Per-patch drawdown and population survival cannot be separated by
site count.** Grazers-per-patch and regen-per-patch scale together, so their
ratio is `total_need / total_regen` regardless of how many sites exist. Drawing
the *average* patch below capacity therefore requires the population to need more
biomass than the system regrows — starvation by definition. Selection on a defense
trait needs **heterogeneity** (some patches grazed hard, others spared), not more
average pressure. Any future "raise grazing pressure" proposal should be checked
against this before it costs another sweep.

**2026-08-18 — Perturbation is blind to code that runs on empty data.** Flipping a
gene or flag answers "does this matter"; it cannot see a branch that executes every
tick and never does anything, because there is nothing to flip. That is the §4
Class B shape and it is what produced the retracted place-memory root cause. A
recorder with an explicit INERT verdict (reached > 0, effective == 0) is the only
thing that catches it — and it needs one probe on a known-live path, or a silently
broken recorder looks identical to a correct one reporting all-inert.

**2026-08-18 — Never run `perl -0pi` over these docs.** Without `-CSD` it reads
UTF-8 as latin1 and rewrites it, turning every em-dash and `§` into mojibake. It
corrupted 62 lines of this file and the damage was committed and pushed before
anyone looked at the result. Use the Edit tool for prose; reserve `sed`/`perl` for
ASCII-only source, and check the file after a bulk rewrite rather than trusting
the exit code.

**2026-08-18 — Perturbation must cover config flags, not just genes.** Four of
sixteen flags are inert under `IntentUtilityV1` and every one had passed a
caller-search audit. `FlagLivenessAnalysis` flips each flag and compares behavior
hashes; it enumerates flags by reflection so a newly added flag is covered without
anyone remembering to add it. Run it before trusting any flag.

**2026-08-18 — A name collision is a defect when it makes a wrong belief
plausible.** `Genome.Commitment` sat beside `ForagingEconomics.CommitmentBonus`
and `SimulationConfig.CommitmentStrength`, all unrelated — the bonus takes
`Persistence`. An audit read the shared word and concluded the gene fed the
bonus. Renaming it `NeutralMarker` cost nothing and removes the trap. When two
unrelated concepts share a name in this codebase, treat that as worth fixing
rather than worth a comment.

**2026-08-18 — Liveness verdicts are scoped to the scenario, and a narrow
scenario manufactures false deaths.** `RiskAversion` reads dead under P4 defaults
purely because the herbivore calibration never produces a threat for its three
guarded call sites. Had that been recorded as "dead code" it would have been
deleted. Always pin liveness against `CreateFullEcosystemDefaults`, and state the
scenario alongside any negative verdict.

**2026-08-19 — The measured config includes the engine bounds, and "looks equivalent" is
how you lose them.** A probe rebuilt the calibration config by hand and passed
`baseline.MaximumPopulation` (**1000**) where the committed guard
`ConsumerDefenseCalibrationModerateSurvivesPlantMortalityAcrossSeeds` pins
`maximumPopulation: 48`. The population ran to ~310, stripped every patch, and gave 30/30
extinct on a scenario whose own comment records 0/30 — which was briefly reported as
behavior drift before the cap was found. Transcribing 48 restored 0/30 exactly. This is the
2026-08-17 "transcribe, never reconstruct" lesson recurring in the one place easiest to miss:
the cap is not an ecological parameter, so it does not look like part of the configuration.
**Copy the config out of the committed guard, do not rebuild it from the factory defaults.**

**2026-08-19 — Answer the variance objection by measuring it, not by conceding it.** A null
on a trait is only as good as the standing variance it was measured at, because response to
selection is proportional to variance. Rather than caveat the tolerance null, a second pair of
arms widened *only* the two tolerance founders to Uniform(.2, .8) — doubling the outcome SD —
and the null held. One extra arm pair converts "suggestive, conditional on founder spread"
into a result. Widening *every* trait instead destroys the operating point: the first attempt
did that and lost the whole ecosystem.

**2026-08-19 — Report the sign test next to the t.** `Defense` under the moisture-gradient arm
gave t -2.05 with 50 of 120 seeds down — no directional consistency at all, a t driven by a
few large magnitudes. A t alone would have been read as a near-significant decline, which is
the shape of the claim retracted on 2026-08-18. The sign test costs one `awk` line over the
per-run CSV.

**2026-08-19 — Measure every trait in the same runs; the free ones are the controls.**
Recording all eight plant genes instead of the two under investigation cost nothing extra and
produced both the refutation of that session's own hypothesis and the strongest positive
control the project has (`Dispersal`, t 14-17 across four arms). A sweep scoped to the trait
you are curious about cannot tell you the effect belongs to that trait.

**2026-08-19 — A `Min` over channels hands all selection to the channel no gene can answer,
and adaptation makes it worse.** `PlantGrowthSystem` takes `limit = Min(moistureAdaptation,
Fertility, temperatureLimit)`. Fertility has no genome modulation, and it binds **82-90%** of
plant-reachable positions — *rising* as tolerance rises, because each adaptation term lifts its
own channel out of contention for the minimum. Raising both tolerances .35 to .65 buys +2.23%
growth limit against -7.76% on `GrowthRateMultiplier`, net **-5.7%** on a rate that is
multiplied by `(1 - Biomass/Capacity)` and so is barely paid at capacity. Before adding another
environment channel or another adaptation gene, check which channel actually binds — see
`docs/experiments/p4-fertility-binds-the-growth-limit-2026-08-19.md`.

**2026-08-19 — Plant growth-RATE traits are nearly unselectable, and no adaptation term fixes
that.** `growth` is multiplied by `(1 - Biomass/Capacity)`, and patches live near capacity:
measured mean gate **0.1711**, with 39.8% of patch-ticks within 1% of capacity. A trait that
changes growth rate by X% changes realised growth by roughly `0.17 * X%`, and by ~nothing for
two fifths of the time. That single fact covers every plant-trait null on record — Nutrition,
Defense, WaterEfficiency, both tolerances and `NutrientUptake` all route through
`GrowthRateMultiplier`, while `Dispersal` (t 14-17) and `SeedInvestment` (t 4.8-6.8) act on
colonisation and are ungated. **Three sessions have now tried to make a growth-rate trait
selectable by improving its benefit channel; the benefit channel was never the constraint.**
Do not run a fourth. Selection on plants has to act on establishment, mortality or seed
production. See `docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md`.

**2026-08-19 — An environment channel needs the binding channel lifted AND the patches below
capacity before it can matter; neither alone is enough.** `ElevationFieldEnabled` drops
temperature by 0.18 on average at plant positions and is nonetheless **bit-identical** under the
standard P4 plant config. It only goes live when the population cap is 1000 (grazing pulls
patches off capacity, so the growth limit has something to act on) **and**
`PlantFertilityAdaptationEnabled` is on (fertility stops binding the `Min`, so temperature can).
Two guesses at a single cause were both wrong before the two-factor test separated them. When a
new environment channel reads inert, check both conditions before concluding the wiring is
broken — and note that this makes the fertility adaptation term a **precondition for any other
channel mattering**, which is a better reason to keep it than the one it was built on. See
`docs/experiments/p4-elevation-field-2026-08-19.md`.

**2026-08-19 — "Widest configuration" is not "widest scenario", and a flag sweep inherits the
scenario's blind spots.** `FlagLivenessAnalysis` pins against `CreateFullEcosystemDefaults` on
`ConsumerDefenseCalibrationModerate`. That turns every flag on, and still cannot exercise
anything gated on `predationEnabled`, because the calibration is a herbivore world with no
threats. Two flags were recorded as unwired on that basis when both are fully wired into
`DecideIntentUtilityV1`. **Before writing down *why* a flag is inert, find its use sites and
check whether the scenario can reach them** — the conclusion "inert here" is cheap and correct;
the explanation is what gets code deleted.

**2026-08-19 — Print the survival columns on every arm, including the ones you expect to
work.** Switching to `PredationVariation` founders moved a flag out of the inert set and was
briefly reported as a confirmed prediction. The population was extinct before 3,000 ticks with
**zero births**: the flag was changing how the collapse unfolded, and three other flags went
"inert" purely because nothing lived to mate, parent or rest. The survival check that caught it
took one probe. Arms that appear to confirm a hypothesis need the check more than arms that
refute one.

**2026-08-19 — Regress the final value on the founder value, never the delta.** Testing whether
a trait converges to an equilibrium by regressing `delta` on `founderMean` is invalid: the
founder mean sits on both sides with opposite signs and manufactures a negative slope out of
pure noise. Regress **final on founder** instead — slope 1 is drift, slope 0 is full
convergence — and give it leverage, because six independent founder draws average out to an
SD of 0.073 and leave the slope undetermined. A per-seed centre for between-seed spread plus
per-site jitter for within-run standing variance answers both needs at once. The flag-off arm
came out at slope 0.9995 +/- 0.0412, which is what a correct drift baseline looks like.

**2026-08-19 — The P4 "flat environment control" is not flat.** `SimulationWorld.cs:62-64`
builds `EnvironmentField.CreateMoistureGradient()` whenever `plantCohortsEnabled` is on, which
`CreatePrototype4Defaults` sets. Moisture already varies with the procedural flag off; only
fertility and temperature are pinned at 1. Flipping `proceduralEnvironmentFieldsEnabled`
therefore changes three channels at once and is not a one-variable contrast. Do not describe
that arm as a flat or no-variation baseline.

**2026-08-20 — Share of variance is not share of available selection.** 51.9% of the variance in
per-patch plant offspring is the single binary of surviving newborn takeover, and that number was
read as if the whole 51.9% were up for grabs. It is not: most of it stays luck no matter what
gene is wired in, because a doomed seedling faces repeated attempts and the invader just retries
elsewhere. The cost of the new trait was priced against the imagined payoff and came out three
times too high, so the trait *declined* in its first sweep. **Decompose variance to find where to
look, then measure the response slope before pricing anything against it.**

**2026-08-20 — Sweep the COST, not just the benefit.** The first arm of a new plant trait read as
another null (t -2.10, 48/120 up). Three prior sessions had responded to that shape by improving
a benefit channel. Sweeping the *charge* instead — 6, 2, 0 — showed the benefit channel was fine
the whole time and the trait rises at t +6.24 uncharged, +4.03 at a charge it can afford. A
single-arm null on a trait with a cost term tells you nothing about which half is wrong.

**2026-08-20 — Pooling two death classes invented a route that does not exist.** Realised plant
lifespan looked like it explained 53% of offspring variance. It explains **2.4%**: the pooled
number came from mixing two-second infants killed by takeover with hundred-second adults. Before
attributing variance to a mechanism, split the sample by *how* the outcome happened — the
`endReason` column cost nothing and moved the conclusion from "mortality is the dominant route"
to "mortality has no headroom at all".

**2026-08-20 — A live genetic channel can convert at nearly zero.** `LifespanSeconds` gives
`Growth` a genuine 2x span and `r(Growth, lifespan) = -0.51` among patches that die of age. It
buys `r(Growth, offspring) = -0.07`, because reproduction here is site-limited, not time-limited:
91% site occupancy, a hard-coded 20-second cooldown, ~96 seconds of life, and a mean of 1.52
offspring achieved out of roughly four possible. **A strong correlation with an intermediate is
not evidence the intermediate reaches fitness.** Measure the last link.

---

**2026-08-20 — A genetic seeding-rate channel did not clear the directional bar.**
`SeedProductionRate` shortens the post-birth cooldown and bypasses the growth gate, but a
120-seed, 12,000-tick sweep found charge 0 at delta +0.01953, t +3.22, only 68/120 up;
charge 2 at delta -0.00355, t -0.62, 57/120 up; and charge 6 at delta -0.01172,
t -1.77, 53/120 up. Dispersal remained live in every arm (t +13.22 to +15.56) and
there were 0/120 extinctions, so this is a measured weak route, not a collapsed scenario.
Do not call the zero-charge arm selection or ship a nonzero price without a new, separately
predicted calibration.

**2026-08-20 — WHY that route is null, recorded so nobody retries it: the cooldown was never the
constraint.** With founders pinned so the cooldown is a constant per arm, halving it from 30s to
15s moves plant births only **203.7 to 221.8 (+8.9%)** and does not raise plant generations at
all. The lifetime budget of a 95.8-second patch is **6.7s growing to maturity, 30.4s on cooldown,
and 58.7s already mature, off cooldown, and failing to find a free site** at 91% occupancy.
Cooldown time freed by a gene is added to a pool of time that is already being wasted. **Two of
the three non-growth routes fail for the same reason, and it is not the growth gate: lifespan and
cooldown both buy TIME, and time is not the scarce resource here — free sites are.** Only
establishment acts on the scarce thing.
`docs/experiments/p4-seed-production-rate-is-not-the-constraint-2026-08-20.md`

**2026-08-20 — A task was set on an inference the same day's own data already refuted.** The
handoff asserted that `ReproductionCooldownSeconds` "is why a 96-second lifetime yields 1.52
offspring out of roughly four possible". The decomposition CSV collected hours earlier contained
`meanEligible = 58.7` against `meanBirths = 1.52`, which says a patch is *already* idle and
eligible for most of its life. Nobody did that subtraction before spending a session on it.
**When a handoff states a causal claim, check whether the last measurement already answers it —
the arithmetic here was one `awk` line over a CSV that was already committed.**

**2026-08-20 — A `t` that beats its own inert control while being LESS directionally consistent
than it is not a result.** The seeding-rate arm at charge 0 read t +3.22 with 68/120 seeds up; the
flag-disabled arm, where the gene does nothing whatsoever, read t +1.51 with **70/120** up. The
disabled arm is the null distribution, so it is the comparison that matters, and the "better" t
came with worse directional agreement. Run the flag-disabled arm as a fourth arm on every trait
sweep — it costs one arm and it calibrates what drift looks like for that trait in that harness.

**2026-08-22 — A flag-gated behavior is not Play-testable merely because it compiles and has
integration tests.** Soft home-range affinity passed its deterministic, flag-off, danger, and
liveness checks, but every shipped Play-mode scenario still leaves the new flag false — including
the `N` all-flags presenter constructor, which predates the new final optional parameter. The next
step is not more tuning from prose: add an explicit ordinary-key scenario (recommended `R`) that
differs from Stable only by enabling the flag, then measure and watch `5` versus `R`. Whenever a
new behavior flag lands, audit both factory defaults and actual presenter/scenario constructors;
an omitted optional argument silently produces a feature nobody can exercise.

**2026-08-22 — Analysis correctness and watchability are separate acceptance gates.** The P5
panel correctly reports confirmed continuity, but routine continuity can fill all eight visible
rows and hide the split/merge/extinction evidence a person actually cares about. Keep continuity
in the analysis record; presentation should hide routine continuity by default and report a compact
count such as “N routine continuities hidden.” Never weaken the analytical stream to repair UI
signal-to-noise.

**2026-08-22 - A tie-break bonus built on distance-from-recent-position cannot beat a travel
burden that already charges distance.** Soft home-range affinity was measured off/on across 30
paired seeds in stable, scarcity and migration. The learning half works (familiarity 0.80-0.89,
hashes diverge), but same-site fidelity moved by **+0.0000** in migration where 20.8% of
creature-ticks held two visible food patches, and by **+0.0001** at ten times the bonus. The
centre is dragged toward wherever the creature just fed, so distance-to-centre is nearly collinear
with distance-to-candidate, and `ResourceUtility` already penalises that. At 10x the only real
effect is clinging: stable loses 7.6% of site re-entries and 2.7% of food intake for no births.
**Before adding a term to a utility function, ask what information it carries that the existing
terms do not.** `docs/experiments/p4a-home-range-affinity-2026-08-22.md`

**2026-08-22 - Two of the three observation scenarios cannot contain a route, by geometry.**
Stable and scarcity co-locate food and water at each cluster and place the two clusters 28.8 apart
against a maximum `Phenotype.VisionRange` of 16. Measured patch fidelity is **1.0000 with the flag
off**: a creature picks one cluster at birth and never visits the other. No affinity mechanism can
improve on 1.0000. Scarcity additionally goes **30/30 extinct in both arms**, so it judges nothing.
Check that a scenario can physically express the behavior before measuring a mechanism in it.

**2026-08-22 - Building the scenario a mechanism needs is a legitimate rescue attempt, and it can
still kill the mechanism.** Soft home-range affinity read null in scenarios where the route metric
was saturated, so `ObservationRouteRing` was built specifically to give it opportunity: 90.6% of
creature-ticks with a genuine equidistant choice, familiarity 0.88, an off-arm route metric at
0.7955 with headroom in both directions. The flag then moved route repeatability **down** (t -2.87)
and same-site re-entry **up** (t +4.93). **When you rescue a null with a better test bed, commit in
advance to accepting a negative from it** - otherwise the rescue is just a search for a scenario
that flatters the code. And note the general lesson about the mechanism: a bonus proportional to
proximity-to-recent-success can only ever reward staying put; nothing in that shape rewards
completing a circuit between complementary resources.

**2026-08-22 - A changing map, not a smarter creature, is what varies routes.** With every
optional behavior flag off, turning on plant mortality in a clustered layout with dormant dispersal
sites left the *amount* of route behaviour statistically unchanged (445 versus 441 cross-kind legs)
while cutting route permanence (pair repeat -0.0935, t -6.47, 3/30 seeds up) and raising distinct
routes per creature by 27% (+0.628, t +4.48, 22/30 up) at no survival cost. The closed home-range
mechanism was trying to do from inside the creature what the environment does for free. **Check
whether an existing system can be placed in a scenario before designing a new one** - both halves
of this backlog item turned out to be already implemented and merely never switched on together.

**2026-08-22 - Extinction in a fixed-seed sweep is not a distribution when the demo runs one seed.**
`ObservationShiftingPatches` looked acceptable at 6/30 extinct until seed 42 was checked
individually: it dies in all four arms. Play mode runs seed 42, so the aggregate was irrelevant to
the actual watchability decision. **Before binding a Play key to a scenario, check the Play seed by
itself.**

**2026-08-22 - "They keep dying" was a reproduction bug, not a mortality bug, and the death causes
proved it in one run.** Founder extinction under separated food and water was assumed for two
sessions to be a survival problem, and four calibration levers were spent on it. Tallying the actual
`DeathCause` values showed **zero dehydration deaths and 0.07 starvation deaths per run** - all four
founders died of age at tick ~2500 in every arm including the healthy one. The real cause was the
joint 70%-energy-AND-70%-hydration reproduction gate, satisfied 95% of the time with co-located
resources and 33.5% when food sits 7 units from water. **When a population fails, tally death causes
and check the reproduction eligibility window before tuning the environment.** Both numbers are
cheap; the four refuted calibration arms were not.

**2026-08-22 - A behaviour bug hid for weeks because the thing it changed was not in the hash.**
`PlantPatchStore.ReplaceAt` never reset plant age, so every takeover installed a seedling that died
on the incumbent's clock. No hash regression caught it: plant `Age` is absent from
`ComputeStateHash`, and the pinned regression runs a scenario with no plant competition. **A passing
hash regression proves only that the hashed fields did not move in the pinned scenario.** Before
trusting one, check that the field you changed is actually hashed and that some pinned scenario
actually exercises the path.

**2026-08-22 - Measure the blast radius before retracting anything.** The correctness fixes above
could in principle have invalidated a season of plant conclusions. A paired old-versus-fixed run in a
detached worktree at the pre-fix commit settled it in two probe runs: every competition-off
**trajectory** was identical - state hashes matched, and home-range, route ring and shifting patches
were cleared outright - while the statistics fix corrected at least one final death count without
changing any trajectory. Only the competition path moved. Retraction by assumption would have thrown away good evidence; clearing by
assumption would have kept bad evidence. **Run the paired sweep.**

**2026-08-22 - An experiment whose scenario was never committed cannot be re-audited.** The 168-site
low-occupancy geometry lived in a deleted throwaway probe, so when a correctness fix landed there was
no way to assess its impact on those conclusions. Promote any geometry a conclusion depends on into
committed scenario data.

**2026-08-22 - A "control" that does not disable the trait is not a control.** A revalidation sweep
labelled its competition-off arm `drift`. Turning site competition off disables no plant trait:
dispersal, seed investment and the rest keep acting, so `Dispersal` read *larger* there (t +26.7)
than with competition on (t +15.6). Read naively that says the project's strongest positive control
is not selected. It says nothing of the kind. **A drift control must disable the trait's channel** -
as the seed-production sweeps did with a charge of zero - not merely change the ecology.

**2026-08-22 - Compare the manipulation the conclusion is about, not the absolute delta.** Absolute
`SeedlingResilience` fell in every arm of the post-fix sweep, which looks like a refutation of the
establishment result. The establishment claim is about what *enabling the contest* does, and the
paired contest-on-minus-contest-off comparison replicates it closely: +0.0362, t +3.22, 72/120
against the recorded t +4.03, 76/120. Match the comparison to the claim before concluding anything.

**2026-08-22 - More sites is not the same lever as lower occupancy.** The 168-site replication
matched the original's site count exactly (6 active plus 162 inactive) and produced occupancy
**0.84, not 0.32**, while the 24-site arm in the same sweep reproduced its recorded occupancy to
within 0.006. Site count was never the mechanism; target *spacing* is. A dense regular lattice
saturates. Any future attempt should spread targets far apart rather than add more of them.

**2026-08-22 - Plant site occupancy is a cliff in target spacing, not a gradient.** Holding the six
active sites, the config and the 162-target count fixed and varying only the lattice span: spacing 4
gives occupancy 0.833, spacing 8 gives 0.528, spacing 9.5 gives 0.311, spacing 11 gives 0.085 with
3/10 seeds extinct, and spacing 13.3 collapses the ecosystem (9/10 extinct, 3 plant generations).
The window reproducing the recorded 0.32 operating point with clean survival is roughly spacing
9.3-9.7 - about four percent of the swept range. **`DispersalRange` is `4 + 20 * Dispersal` and
Dispersal evolves upward, so a mature patch throws seeds 14-24 units; any lattice tighter than that
saturates.** Do not guess a spacing; sweep it and read occupancy before drawing any conclusion.

**2026-08-22 - "Disabling the flag" is not always a clean control either.** The low-occupancy
adjudication used `plantMortalityEnabled: false` as the matched control for the lifespan-headroom
claim, on the reasoning that lifespan has no channel without mortality. True, and useless: the same
comparison moved `Dispersal` +0.0834 (t +21.40, 118/120), `NutrientUptake` -0.0466 (t -7.62) and
`WaterEfficiency` -0.0445 (t -8.61). Turning mortality off removes site turnover and rewrites the
whole selective regime. **Check what else your control disables before reading its numbers** - a
control is only clean if the manipulation is the *only* thing that changes.

**2026-08-22 - Some conclusions are not recoverable, and saying so is the result.** The three
low-occupancy plant conclusions cannot be audited. Their scenario was never committed; the
occupancy condition was reproduced by calibration, but only by placing free sites outside the +/-25
creature arena, where nothing grazes them (`RealizedGrazingPressure` 0.0026). Free sites no herbivore
can reach are not the resource those claims are about. Putting 162 targets at non-saturating spacing
*inside* the arena is geometrically impossible. **If the original achieved it in-arena, its layout
was something a lattice cannot express, and it is gone.** The honest next move is to re-derive the
underlying question as a new experiment with a committed scenario, not to keep auditing an
unrecoverable one.

**2026-08-22 - Measure the thing before optimising it, and be willing to decline.** Two P1
performance items, same discipline, opposite outcomes. Genetic distance was a real hot spot with a
number attached - a constant **240 bytes per pair**, 120 MB and 126 ms per observation at 1,000
creatures - and got fixed (1,151x less allocation). Resource allocation was benchmarked and
**declined**: its cost is O(requests x distinct resources) so crowding is the *cheap* case (1,000
requests on one resource = 16.9 us), and a full 12,000-tick run at the largest reachable population
takes 2.72 s end to end. Optimising it would have risked a deterministic path that decides who eats
when resources run short, for an unmeasurable gain. **A review calling something a hot spot is a
hypothesis; the benchmark is the result, including when it says do nothing.**

**2026-08-22 - Pin a scaling property, not a magic number.** The regression guard for the clustering
fix asserts that allocation grows under 8x when pairs grow 16x, rather than asserting a byte count.
A byte threshold rots the moment anything else in the path changes; the scaling assertion fails
exactly when someone reintroduces per-pair allocation, which is the thing worth catching.

**2026-08-22 - A single source of truth for trait order, or inheritance and the hash will drift.**
`Genome.WriteTraits(destination, offset)` now writes the fields and `ToTraits()` is a wrapper around
it. Trait order feeds `FromTraits`, inheritance and analysis; two hand-maintained copies of that
order is a silent-corruption bug waiting to happen. If a trait is added, there is one place to
change.

- **A profiler that overwrites its own output cannot see an intermittent problem (2026-08-24).** The
  first performance writer called `File.WriteAllText`, so only the last five-second window survived.
  The user reported a lag spike every second or two; the file showed a worst frame of 19 ms and it was
  written up as a one-off. **The user was right and the instrument was wrong.** It appends now. The
  failure is worth remembering in general: **an intermittent fault is the only kind worth profiling
  for, and discarding history makes it exactly the kind you cannot see.**
- **Percentiles cannot show a rare spike, by arithmetic.** At 60 fps a stutter every two seconds is
  under 1% of frames, so it hides *below* the 99th percentile while being the most obvious thing on
  screen. **Count frames over a threshold as well.** p99 said 8.10 ms in a window whose worst frame
  was 1,476.83 ms.
- **A diagnostic overlay must not be driven by simulated time.** The heatmap accumulator advanced
  inside the step loop by `FixedDeltaTime`, so at speed 8 it rebuilt eight times as often - and each
  rebuild was 192.95 ms. **The refresh rate of a debug view has no business scaling with the speed
  multiplier.**
- **Amortise a periodic rebuild before optimising it.** 16,384 samples in one frame is a freeze; the
  same work at four rows a frame is 1.5 ms and invisible. **No algorithm changed** - worst call
  192.95 ms to 7.49 ms, worst frame 1,476.83 ms to 11.95 ms. Capture the bounds once per pass so a
  moving camera cannot tear the image mid-pass.
- **Check the view is showing the subject before believing the number.** The first Play-mode reading
  had 49 renderers because the instruction was to press `Y` and the chunked planet is behind `O`. It
  was nearly written up as "the planet is cheap" with the planet off screen.
- **Figures derived from reading code are not measurements and should be labelled.** "908 renderers"
  and "232k triangles" both came from walking the quadtree, never reconciled with each other, and were
  wrong: measured, 1,090 and 566,272. **The fact that two recorded numbers disagreed was the clue.**
- **When a user contradicts your measurement, they are the ground truth.** They are looking at the
  thing. Three separate instrument faults were found by taking "it lags every second or two"
  seriously after a file said otherwise.
- **Split long documents with a script, never by hand.** `AGENT_FIELD_NOTES.md` and
  `SESSION_HANDOFF.md` reached 1,752 and 1,559 lines - read at the start of every session.
  Summarising them by hand would have meant reading them by hand, which is the cost being removed.
  `tools/split_doc.py` lifts sections whole on their headings: **nothing summarised, nothing
  rewritten, so a split cannot change what a document says.** Give the index each section's line count
  and first real line so it says what is in a file rather than only naming it.
- **Backticks in a shell string are executed.** A `python -c` one-liner containing markdown backticks
  had its content eaten by bash and silently corrupted the docstring of the very tool being written.
  **Put multi-line edit scripts in a file.** Three times this session `git status` or a direct
  re-read caught something that would otherwise have shipped broken.
