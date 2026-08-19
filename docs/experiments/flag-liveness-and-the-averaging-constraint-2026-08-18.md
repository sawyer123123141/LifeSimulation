# Four Inert Config Flags, and Why Grazing Pressure Cannot Be Tuned — 2026-08-18

> Extends `p4-defense-no-gradient-2026-08-18.md` and
> `gene-liveness-perturbation-2026-08-18.md`. Corrects the 2026-08-17 audit's
> Class D clearance of the config flags, and corrects this author's own claim that
> FULL ecosystem mode gives every mechanism its best chance of mattering.

## 1. The averaging constraint — why the pressure sweeps were doomed

The previous document ended by proposing a scenario redesign that decoupled per-patch
depletability from total food supply, by raising site count while lowering per-patch capacity.
**That proposal was arithmetically impossible and is withdrawn.**

At steady state a patch is drawn below capacity when

```
grazers_on_patch x ingestion_rate  >  regen_per_patch
```

With grazers distributed over sites, `grazers_on_patch = population / sites` and
`regen_per_patch = total_regen / sites`. Site count cancels. The condition reduces to

```
population x ingestion_rate  >  total_regen
```

which is exactly the condition for the population to consume more biomass than the system
regrows — starvation. **Drawing the *average* patch below capacity and keeping the population fed
are the same knob with opposite signs.** No site-count arrangement, and no regeneration value,
separates them. That is why every arm of the regeneration sweep traded extinction against
no-gradient, and why the lifespan and population-cap sweeps before it did the same.

The escape is not more average pressure but **heterogeneity**: some patches grazed hard while
others are spared. Defense earns nothing from uniform grazing no matter how intense — it pays only
when carrying it causes a patch to be *differentially* avoided. Any future proposal of the form
"raise grazing pressure" should be checked against this constraint before it costs another sweep.

## 2. The avoidance channel exists — and is dead

`DecisionSystem.ComputeNeedGain` (`DecisionSystem.cs:742`) scores a patch by
`resource.NutritionMultiplier`, and defense lowers a patch's projected nutrition through
`PlantPhenotype` (`.55 + .90*Nutrition - .25*Defense`). So grazers should already deprioritize
defended patches — precisely the heterogeneity §1 calls for.

It is gated on `learnedResourceQualityEnabled`, which was **off in every sweep run so far**.

Re-running the four defense arms with that flag added returned results **bit-identical to the
previous run in every cell, to four decimal places**. Floating-point ecology over 12,000 ticks and
30 seeds does not reproduce by coincidence. The flag does nothing.

Cause: `Config.LearnedResourceQualityEnabled` has exactly one production reader,
`SimulationWorld.cs:973`, inside `DecideFromLearnedOutcomes` — the Legacy+Cognition path.
`DecideIntentUtilityV1` never receives it. A comment at `Prototype1Presenter.cs:332` already said
so; it had never reached the ledger.

## 3. Flag liveness by perturbation

Generalized from the above. `Diagnostics/FlagLivenessAnalysis.cs` flips each `SimulationConfig`
boolean and compares `ComputeBehaviorHash` tick by tick, the same method
`GeneLivenessAnalysis` uses for genes. Flags are enumerated by **reflection over the constructor's
`bool` parameters**, so a newly added flag is covered without anyone remembering to add it.

Seed 42, `ConsumerDefenseCalibrationModerate`, 3,000 ticks:

| flag | P4 defaults | FULL ecosystem |
| --- | --- | --- |
| `cognitionEnabled` | live (t=10) | live (t=5) |
| `physiologyEnabled` | live (t=20) | live (t=60) |
| `plantCohortsEnabled` | live (t=1) | live (t=1) |
| **`foragingEconomicsEnabled`** | **inert** | **inert** |
| `predationEconomicsEnabled` | live (t=610) | live (t=726) |
| `decisionStaggerEnabled` | live (t=5) | live (t=5) |
| **`multiThreatPerceptionEnabled`** | **inert** | **inert** |
| `restBehaviorEnabled` | live (t=1750) | live (t=1749) |
| `juvenileCapabilityEnabled` | live (t=1) | live (t=1) |
| `parentalFollowingEnabled` | live (t=401) | live (t=421) |
| **`kinRecognitionEnabled`** | **inert** | **inert** |
| **`learnedResourceQualityEnabled`** | **inert** | **inert** |
| `mateSelectionEnabled` | live (t=400) | live (t=400) |
| `plantSiteCompetitionEnabled` | live (t=20) | live (t=20) |
| `plantMortalityEnabled` | live (t=1960) | live (t=1960) |
| `plantDefenseDeterrenceEnabled` | live (t=170) | live (t=221) |

**Four of sixteen flags are inert**, and inert in FULL ecosystem mode too, where all four are on.
This is not the scenario-scoping caveat that applies to `RiskAversion` — these do nothing under
`IntentUtilityV1` in any configuration.

### Correction to the 2026-08-17 audit

Its Class D cleared all sixteen flags because each "has at least one production reader". True of
all four above, and insufficient: the reader sits on a path `IntentUtilityV1` never takes. This is
the same error shape as the `Persistence` clearance, and it is now the third instance of a
caller-search producing a false clean bill of health in this project.

### Correction to this author's own claim

`gene-liveness-perturbation-2026-08-18.md` describes FULL ecosystem mode as giving "every mechanism
its best chance of mattering". For these four flags that is false — no configuration gives them a
chance short of switching to `Legacy`. FULL mode is the widest surface *available*, which is not the
same as every mechanism getting its chance.

## 4. Pinned by test

`LivenessTests.InertFlagsAreExactlyTheKnownSetUnderTheWidestConfiguration` asserts the inert set
exactly. A flag becoming live is a real behavior change that invalidates every baseline measured
before it, so it should fail loudly rather than be discovered later.
`EveryConfigFlagIsCoveredByTheLivenessSweep` guards the reflection itself, so a convention change
cannot silently reduce the sweep to nothing.

## 5. Where the positive control now stands

Unresolved, but the search space is much smaller than it looked.

Ruled out by measurement: raising grazing pressure (§1, structurally impossible), deterrence alone
(previous document), and enabling the existing avoidance channel (§2, the flag is dead).

The remaining route is to make defense produce differential grazing on the `IntentUtilityV1` path,
which means **porting patch-quality scoring into `DecideIntentUtilityV1`** rather than enabling a
Legacy flag. That is a genuine behavior change to the decision system, needs its own flag and
calibration, and should be specified deliberately — not attempted as a sweep.
