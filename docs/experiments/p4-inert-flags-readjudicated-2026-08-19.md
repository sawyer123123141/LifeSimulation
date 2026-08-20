# The Four Inert Flags, Re-adjudicated — 2026-08-19

Handoff item 3 asks whether to wire or delete four config flags that
`FlagLivenessAnalysis` reports inert. **For two of the four the question is wrong: they are
already wired on the live path.** They read inert because the scenario the sweep pins
against has no threats, not because nothing reads them.

Nothing was deleted or rewired. This is a diagnosis.

## What the ledger said, and what the code does

| flag | ledger's stated reason | verdict |
|---|---|---|
| `foragingEconomicsEnabled` | "consumers (`CommitmentBonus`, `ShouldAbandon`) are Legacy-only" | **correct, with a nuance** |
| `learnedResourceQualityEnabled` | "single reader is inside `DecideFromLearnedOutcomes`, the Legacy+Cognition path" | **correct** |
| `multiThreatPerceptionEnabled` | "`IntentUtilityV1` carries its own inline threat handling" | **wrong** |
| `kinRecognitionEnabled` | "no reader on the `IntentUtilityV1` path" | **wrong** |

### The two that were misdiagnosed

`SimulationWorld` passes **both** flags into `DecisionSystem.DecideIntentUtilityV1` — the
policy every P4 scenario uses — at the `multiThreatPerceptionEnabled` and
`kinRecognitionEnabled` arguments. Inside, they are read:

```csharp
if (predationEnabled)
{
    ScoreCarcass(...);
    if (multiThreatPerceptionEnabled)
        ScorePredationMulti(..., selfId, selfLineage, kinRecognitionEnabled, out fleeScore, out huntScore);
    else
        ScorePredation(..., selfId, selfLineage, otherLineage, kinRecognitionEnabled, out fleeScore, out huntScore);
}
```

`multiThreatPerceptionEnabled` selects between two real scoring functions, and
`kinRecognitionEnabled` is read inside both, gating `IsKin`. There is no inline
reimplementation and there is no missing reader.

Every one of those sites sits inside `if (predationEnabled)`, and the sweep pins against
`ConsumerDefenseCalibrationModerate` — a **herbivore** calibration that produces no threats.
Both branches therefore score nothing and return identical results. `CreateFullEcosystemDefaults`
widens the **config** but not the **scenario**, so it does not rescue this.

This is precisely the scoped-verdict trap the ledger already records for `RiskAversion`
("a narrow scenario manufactures false deaths"), and the same wrong-reason-right-conclusion
shape as the retracted `Persistence` entry. The *conclusion* "inert under the pinned sweep"
was right and is still pinned by test. The *reason* was wrong, and a wrong reason is what
gets a mechanism deleted.

### The nuance on `foragingEconomicsEnabled`

Its behavioral consumers really are Legacy-only. But the flag also gates two per-tick updates
that run on the live path regardless of policy:

```csharp
if (Config.ForagingEconomicsEnabled) AdvanceForagingActionTime(Config.FixedDeltaTime);
...
if (Config.ForagingEconomicsEnabled) UpdateForagingIntakeRate(Config.FixedDeltaTime);
```

These maintain per-creature foraging state every tick under `IntentUtilityV1`, and nothing on
that path consumes it — `SecondsInCurrentAction` is read by `DecisionSystem.Decide`, which is
Legacy. So this is the section 4 **"executes but nothing consumes the result"** class, the same
shape as place memory, rather than "unwired". Flipping the flag is still bit-identical, which
also tells us foraging state is absent from `ComputeStateHash`.

## The attempt to adjudicate, and why it failed

If the two misdiagnosed flags are merely unexercised, giving the sweep some threats should
revive them. Switching only the founder profile to `PredationVariation`, changing nothing
else:

| sweep | inert set |
|---|---|
| herbivore calibration (the pinned sweep) | foragingEconomics, kinRecognition, learnedResourceQuality, multiThreatPerception |
| same config, `PredationVariation` founders | foragingEconomics, kinRecognition, learnedResourceQuality, **mateSelection, parentalFollowing, restBehavior** |

`multiThreatPerceptionEnabled` does drop out of the inert set — and it means nothing, because
of the survival columns:

| ticks | founder profile | seeds alive | mean births | predation deaths |
|---:|---|---:|---:|---:|
| 3,000 | PhysiologyVariation | 5/5 | 57.0 | 0.0 |
| 3,000 | **PredationVariation** | **0/5** | **0.0** | 0.6 |
| 12,000 | PhysiologyVariation | 0/5 | 384.8 | 0.0 |
| 12,000 | PredationVariation | 0/5 | 0.0 | 0.6 |

`PredationVariation` founders are extinct well before 3,000 ticks with **zero births**. So
`multiThreatPerception` "going live" means only that it changes how the collapse unfolds, and
`mateSelection`, `parentalFollowing` and `restBehavior` "going inert" means only that nothing
survives to mate, parent or rest. **No verdict measured on that arm is worth anything**, in
either direction. This is the "a trait that only moves while the ecosystem collapses has not
demonstrated a gradient" lesson, applied to flags.

## What actually blocks item 3

**There is no survivable predator-prey scenario in the repo.** `Prototype4Scenarios` contains
no predation scenario; predation only arises from `FounderProfile.PredationVariation`, which
does not survive contact with the plant calibration. Until such a scenario exists,
`multiThreatPerceptionEnabled` and `kinRecognitionEnabled` **cannot be adjudicated at all** —
and deleting them on the strength of an inert verdict measured in a world with no predators
would be deleting working code.

Recommended, and not done here because it is a design decision:

1. **Do not delete any of the four.** Two are unexercised rather than unwired.
2. `foragingEconomicsEnabled` and `learnedResourceQualityEnabled` are the genuine wire-or-delete
   candidates. Both are reachable only from the `Legacy` policy, which no configuration uses.
3. The prerequisite for the other two is a **survivable predator-prey scenario** — a P5 species
   concern more than a P4 one. That is the task hiding behind item 3.

## Incidental finding

`CreateFullEcosystemDefaults` inherits `maximumPopulation` from the P4 factory, which is
**1000**, and at 12,000 ticks it goes extinct in 5/5 seeds after ~385 births — the same
boom-and-collapse the cap produces elsewhere today
(`p4-plant-trait-selection-nonreplication-2026-08-19.md`, method notes). The liveness sweep
runs at 3,000 ticks, comfortably inside the survivable window, so the pinned tests are
unaffected. But full-ecosystem mode should not be used for a 12,000-tick run expecting a
surviving population.

## Method note

The `PredationVariation` result was reported as a confirmed prediction for about two minutes
before the survival probe — written specifically to catch this — showed the population was
already dead. Sixth hypothesis refuted by measurement in this session. The survival columns
are worth printing on every arm, not just the ones that look suspicious.
