# Gene Liveness by Perturbation — Two Corrections to the Audit — 2026-08-18

> Corrects `halfway-wired-mechanism-audit-2026-08-17.md` on `Persistence`, and adds `RiskAversion`
> as a scenario-scoped false negative. The audit's verdict on `Commitment` is confirmed.

The 2026-08-17 audit worked by extracting every public member and counting production references.
That method is stated in `AGENT_FIELD_NOTES.md` §5 to be insufficient, and this document shows two
concrete ways it failed.

## Method

Run a scenario twice from the same seed. In the second run, overwrite one trait across every founder
before the first step, then compare a **behavior hash** after every tick.

`SimulationWorld.ComputeBehaviorHash` covers needs, movement, decisions, combat, reproduction,
memory, population and death counters, resource amounts and plant biomass — and **no genome or
phenotype field**. `ComputeStateHash` includes the genome, so perturbing any gene always moves it;
that is why it cannot be used here. If the behavior hash never diverges, the gene influenced
nothing.

This asks the simulation directly rather than asking the source what it appears to do. It cannot be
fooled by a value that is computed and consumed by nobody, nor by a branch that runs every tick
against permanently empty data.

Implementation: `Assets/Scripts/Simulation/Diagnostics/GeneLivenessAnalysis.cs`. Pinned by
`Assets/Tests/EditMode/LivenessTests.cs`. Two perturbation values (0 and 1) are tried per trait, so
a perturbation that happens to coincide with the founder value cannot silently test nothing.

## Results, 3,000 ticks, seed 42, `ConsumerDefenseCalibrationModerate`

| configuration | genes that reach no behavior |
| --- | --- |
| `CreatePrototype4Defaults` | `RiskAversion`, `Commitment` |
| `CreateFullEcosystemDefaults` | `Commitment` |
| `CreateFullEcosystemDefaults`, `PredationVariation` founders | `Commitment` |

## Correction 1 — `Persistence` is live under P4, contrary to the audit

The audit states: *"`phenotype.Persistence` is read in exactly three places: `CommitmentBonus`
(Legacy path), `ShouldAbandon` (Legacy and cognition-disabled only), and the state hash. Under P4's
actual configuration … `Persistence` still has no behavioral effect."*

Perturbation says otherwise: `Persistence` diverges the behavior hash at tick 10 under plain P4
defaults. The missed reader is `GenomePhenotype.cs:351`:

```
+ (0.05f * genome.Persistence);
```

`Persistence` is one term in the weighted sum producing `bodyMass`, which sets energy capacity,
speed and metabolic cost. It reaches behavior through allometry, not through foraging economics.

The narrow claim the audit was making — that *foraging commitment* is Legacy-only — still holds.
The general claim, that the gene has no behavioral effect under P4, is withdrawn. Anything that
relied on `Persistence` being an inert channel must be re-read; in particular it is **not** a valid
placebo.

## Correction 2 — `RiskAversion` is a scenario-scoped false negative

`RiskAversion` reads dead under `CreatePrototype4Defaults` and live under FULL ecosystem mode. It is
not dead code: it has three genuine call sites in `DecisionSystem` (`fleeScore`,
`candidateFleeScore`, `dangerPenalty`), all gated on a valid threat. The P4 herbivore calibration,
with `PhysiologyVariation` founders and no predation economics, never produces one.

This is the load-bearing caveat of the whole method: **a "does not reach behavior" verdict is scoped
to the scenario it was measured in.** A narrow scenario manufactures false deaths. This is precisely
why `CreateFullEcosystemDefaults` exists — to give every mechanism its best chance of mattering, so
that a gene reading dead there is dead everywhere narrower.

`LivenessTests.RiskAversionIsLiveOnlyWhenThreatsExist` pins both halves of this, so the narrow
result cannot later be cited as evidence that `RiskAversion` is dead code.

## Confirmation — `Commitment` is structurally dead

`Commitment` reaches no behavior under any of the three configurations, including FULL ecosystem
mode with predation. This matches the audit exactly, and it is the one verdict a caller-search got
right for the right reason: there is no consumption site at all.

It is **retained deliberately**, against the audit's recommendation to delete it. The 2026-08-17
coevolution run used its inertness as a placebo, and it came back at effect +0.020 with a tight
interval around zero — evidence that the bootstrap pipeline does not manufacture false positives.
That argument is only worth having if the channel stays inert, which
`LivenessTests.CommitmentReachesNoBehaviorUnderTheWidestConfiguration` now enforces. Note that
`Persistence` cannot substitute for it (Correction 1).

## Why a runtime recorder exists as well

`Diagnostics/LivenessRecorder.cs` tracks *code paths* rather than genes, separating three states:
never executed, executed but never produced a non-identity output, and demonstrably effective. It
answers questions perturbation cannot — which branch inside a live gene's path actually fired — and
it is explicitly forbidden from reaching either hash, pinned by
`LivenessTests.LivenessRecorderCountersDoNotReachTheStateHash`.

The recorder cannot replace perturbation, for the reason `Commitment` illustrates: you cannot
instrument a consumption site that does not exist. Perturbation is the authority.

## Not re-litigated here

Place memory (`ObservePlace`, `TickPlaceMemoryDecay`, `TryScoreBestRememberedPlace`,
`RecordFailedPlaceSearch`) remains unwired, and is deliberately **kept** rather than deleted. With
liveness now enforced by test rather than by documentation, dead code can no longer masquerade as
live, which was the only argument for removing it. The wire-or-delete decision moves to P5, where
spatial and history work may want it.
