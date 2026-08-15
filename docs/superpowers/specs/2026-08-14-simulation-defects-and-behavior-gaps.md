# Simulation Defects and Behavior Gaps

**Status:** audit findings, recorded for planning. No fixes applied.

**Method:** read of `Assets/Scripts/Simulation/` against `docs/superpowers/specs/2026-08-12-product-architecture.md` and `docs/superpowers/plans/2026-08-12-p0-p7-program-plan.md`. Every claim below cites the code that produced it. Line numbers are from the commit that introduced this document.

## How to read this

The organizing question is **does fixing it change simulation results?**

`SimulationWorld.ComputeStateHash()` covers genomes, needs, movement, decisions, reproduction, combat, memory, and resources. It does **not** cover death causes, decision diagnostics, or statistics. Anything the hash covers is frozen-evidence-bearing: changing it shifts recorded P0–P3 results and requires a versioned migration under the program's own rules ("Later stages may not silently change earlier benchmark/scientific fixtures").

- **Group A — hash-safe.** Fixable now. No behavior change, no evidence impact.
- **Group B — hash-breaking.** Real defects, but each shifts recorded results. Needs a versioned scenario migration and re-run evidence.
- **Group C — unbuilt specification.** Not defects. Written scope that was never implemented.

## Group A — hash-safe, fixable immediately

### A-1. Starvation and dehydration deaths are never recorded

**2026-08-15 update: fixed.** This was written against `main`'s snapshot of `Assets/Scripts/Simulation/`, before it was known that `codex/decision-policy-foundation` had continued past the same fork point independently. That branch now emits `DeathCause.Starvation` and `DeathCause.Dehydration` and counts them separately (`SimulationWorld.cs`). Left below for the historical record.

`SimulationTypes.cs` declares `DeathCause.Starvation = 2` and `DeathCause.Dehydration = 3`. Neither is ever emitted. `SimulationWorld.cs:394` passes `DeathCause.Health` for every metabolic death, and `:398` passes `Age`. A repository-wide search finds no other emission site.

`NeedsSystem.Tick` already distinguishes the causes internally — energy exhaustion drains health at `4f * deltaTime` (`NeedsSystem.cs:56-59`), dehydration at `5f * deltaTime` (`:61-64`) — then discards which one applied.

**Consequence:** the question "is my population dying of thirst or hunger?" is unanswerable from telemetry, despite the enum existing to answer it. Every ecosystem-collapse diagnosis is blocked on this.

**Why it is hash-safe:** death causes are not hashed. Only `Predation` has downstream behavior (carcass creation, `SimulationWorld.cs:227-238`), which is untouched. Statistics counts totals, not causes.

**Fix:** record which need reached zero and pass the matching cause. Then surface a death-cause histogram in the presenter.

### A-2. Decision diagnostics cannot explain most decisions

`DecisionDiagnostics` carries four values: food score, water score, and two visibility booleans (`DecisionSystem.cs:45-58`). Decisions can also resolve to `Flee`, `SeekPrey`, `Attack`, `SeekCarcass`, `FeedCarcass`, `SeekThermalComfort`, or `Reproduce`, none of which contribute a score to diagnostics.

**Consequence:** the inspector can explain a foraging choice and nothing else. This conflicts with the delivery cycle's phase 5, which requires every prototype to "expose why decisions... produced their outputs", and with P1's exit requirement that behavior remain explainable.

**Why it is hash-safe:** diagnostics are stored via `SetDecisionDiagnosticsAt` and never hashed or read by simulation logic.

**Fix:** replace the fixed four fields with a per-action score array plus the winning action, written by whichever system produced the decision.

## Group B — real defects that shift recorded results

Each requires a versioned migration and re-run evidence. Ordered by evidence impact, cheapest first.

### B-1. The learning signal is saturated and therefore inert

`SimulationWorld.cs:742` computes the food learning outcome as:

```csharp
Math.Min(1f, nutrition * phenotype.FoodYield * 20f)
```

A typical per-tick allocation is on the order of `IngestionRate × FixedDeltaTime` ≈ 0.05. With `FoodYield` in the 0.75–1.4 range, the product before clamping is already near or above 1.0, so the clamp binds on essentially every feeding event. Water is worse: `:755` uses `Math.Min(1f, allocatedAmount * 20f)` with no yield term at all.

**Consequence:** `FoodOutcomeValue` and `WaterOutcomeValue` converge to 1.0 for every creature regardless of experience. `KnownOutcomeOrCuriosity` (`DecisionSystem.cs:180-185`) then returns the same value for everyone, so learned outcomes cannot discriminate between creatures, resources, or strategies.

This voids the P2 exit gate as currently measured. That gate requires cognition to "improve reproductive output in at least one patterned environment" and to lose value in another — but the learning term carries no information, so any measured difference comes from other genes.

**Fix:** normalize the outcome against expected intake rather than clamping raw nutrition, so the value spans its range in normal operation.

### B-2. Learned value is per resource kind, not per location

`MemoryState` holds exactly one `FoodOutcomeValue` and one `WaterOutcomeValue` (`SimulationTypes.cs:167-170`). Both are scalars over the whole resource *category*.

**Consequence:** a creature cannot prefer a rich patch over a poor one. It can only learn "food is good in general", which is not a decision-relevant fact. This is the mechanism the roadmap calls "learned resource quality affecting future choices", and it does not exist.

**Fix:** move learned value into per-place memory entries; see C-1.

### B-3. Resource quality is absent from decision scoring

**2026-08-15 update: partly fixed.** Same fork-point caveat as A-1. On `codex/decision-policy-foundation`, the non-cognition decision path (`DecisionSystem.ResourceUtility`) now weighs `resource.Amount` against the creature's missing need. The cognition-mode path (`DecideFromLearnedOutcomes`) still doesn't — it's still pure distance-based `Availability`, exactly as described below. So this defect still applies whenever `CognitionEnabled` is on.

`DecisionSystem.Availability` is `1f / (1f + distance)` (`DecisionSystem.cs:175-178`). `ResourceState.Amount` never enters any score.

**Consequence:** creatures always choose the nearest resource, never the richest. A depleted patch two metres away outscores a full patch ten metres away, indefinitely. This is the direct mechanical cause of creatures starving beside exhausted resources.

**Fix:** include remaining amount and regeneration rate in the score, and express travel cost in energy rather than raw distance so speed and metabolism genes change foraging range.

### B-4. Decisions have no commitment, so they oscillate

Decisions are recomputed wholesale at `DecisionsHz` (2 Hz by default) in `TickDecisions`, with no bonus for continuing the current action and no give-up rule.

**Consequence:** two near-tied options alternate indefinitely, and a creature never decides a patch is exhausted and leaves. Combined with B-3, this produces the observed oscillate-until-death behavior.

**Fix:** add a decaying commitment bonus for the current action plus a marginal-value-theorem departure rule — leave when intake rate falls below the habitat average.

### B-5. Hunting is gated by a threshold, which is a role label

`PredationSystem.cs:9-10` defines `MinimumHuntingDiet = 0.58f` and `MinimumHuntingAggression = 0.35f`, applied at `:70` as a hard zero.

Because `MeatYieldMultiplier = 0.5 + DietSpecialization` (`GenomePhenotype.cs:226`), the computed `diet` term equals `DietSpecialization` exactly. The gate therefore reads: creatures with diet specialization below 0.58 cannot hunt, ever, at any advantage.

**Consequence:** this is a categorical predator flag expressed as a float comparison. It conflicts with a permanent architectural principle — "Ecological roles and species labels must be derived, never authoritative biology flags" — and with P1's explicit prohibition on predator/prey booleans.

**Fix:** replace the gate with a continuous expected-value comparison: expected meat energy times success probability, minus expected injury cost and pursuit energy. Specialization then shifts the balance instead of forbidding the behavior.

### B-6. Threat memory is written and never read

`MemorySystem.RememberThreat` populates `ThreatPosition`, `ThreatConfidence`, and `ThreatAge`, and `TickDecay` decays them. No decision path reads any of the three; the only other consumer is `ComputeStateHash`.

**Consequence:** danger memory does not exist. P2's data phase specifies memory "for resource, threat, and encounter observations" and its learning phase requires scores to use remembered information.

**Fix:** apply an avoidance term from remembered threats, scaled by the `Fear` gene, so timid lineages develop avoided territory.

### B-7. `TemperatureField` ignores the world seed

`TemperatureField.Sample` uses only hardcoded constants. Every world in every experiment shares one identical climate.

**Consequence:** climate cannot vary across seeds, so "shifting selection pressures" cannot be tested.

**Deliberately not fixed in place.** `docs/superpowers/specs/2026-08-14-world-generation-design.md` resolves this on a new code path and leaves `TemperatureField` untouched, because editing it directly would shift frozen P3 physiology evidence.

### B-8. Decisions are unstaggered

`IsDue(tick, DecisionsHz)` fires for the entire population on the same tick.

**Consequence:** one cost spike every tenth tick, and every creature reconsiders in lockstep — visible as synchronized population-wide behavior changes.

**Fix:** phase-stagger by creature index.

## Group C — written specification never implemented

Not defects. Scope that exists in the plans and not in the code.

### C-1. Memory is three fixed slots, not a capacity-N sidecar

P2's data phase specifies "a fixed-capacity aligned memory sidecar for resource, threat, and encounter observations" with "confidence, observation age, decay, and deterministic replacement metadata", and the architecture document adds that "Fixed per-creature capacity avoids per-creature collections" and that "Increasing capacity is a heritable benefit with a proportional memory and metabolism cost".

`MemoryState` implements one food slot, one water slot, one threat slot, and no replacement policy. The `MemoryCapacity` gene affects only `CognitionRestCostMultiplier` (`GenomePhenotype.cs:228`) — creatures pay a metabolic cost for capacity they never receive.

This is the largest gap between written and built scope in the project.

### C-2. Mating is proximity, not behavior

**2026-08-15 update: partly stale.** Same fork-point caveat as A-1. On `codex/decision-policy-foundation`, `CreatureAction.SeekMate` is now a real scored candidate (`DecisionSystem`, gated by `ReproductionSystem.CanSeekMate`) — a creature can now choose to seek and approach a mate. But `ReproductionSystem.Step`, the actual birth mechanism, still independently pairs the nearest two ready creatures within `MateDistance` every tick, with no reference to who was sought or chosen. So "a creature never approaches a mate" is no longer true; "reproduction is spatial coincidence with no choice between candidates" still is — seeking and pairing are two disconnected systems today.

`CreatureAction.SeekMate` is declared and never produced. `ReproductionSystem.Step` scans the spatial grid, pairs any two ready creatures within `MateDistance = 2f`, and then writes `CreatureAction.Reproduce` onto both retroactively (`ReproductionSystem.cs:170-171`).

No creature ever seeks a mate, approaches one, or chooses between candidates. Reproduction is a spatial coincidence. P3 lists mate signaling as optional, "add only if two-parent choice remains behaviorally impoverished after the prior slices" — that condition is met.

### C-3. Perception returns one creature

`PerceptionSystem.FindNearestOtherCreature` returns a single `CreatureObservation`.

This structurally blocks mate choice among candidates, multi-threat assessment, herding, and any group behavior. P1 specifies "nearest viable prey, threats, and carcasses" in the singular, so the code matches its spec — but every social behavior later in the roadmap requires a top-K query.

### C-4. `CreatureAction.Rest` is never selected

Declared in the action enum and produced by no scoring path.

### C-5. No juvenile behavior

`AdultAgeSeconds = 20f` gates reproduction only. Offspring spawn at the parents' midpoint as fully capable agents. There is no parental following, kin recognition, or reduced juvenile capability.

## Evidence-quality observations

Recorded because they bear on whether gates have actually been passed, and both are already acknowledged in the project's own documents.

**The P0 throughput gate is not demonstrated.** `docs/benchmarks/prototype1-baseline-2026-08-13.md` reports 0.245 ms per step at 1,000 founders, then states the population declined to 21–58 survivors during measurement. The recorded figure therefore measures a nearly empty world, not sustained 1,000-creature throughput.

**The P0 selection result is weak.** `docs/experiments/p0-calibration-2026-08-13.md` reports a standardized paired effect of 1.10 with a clean bootstrap interval, but only 60% of seed pairs shifted in the same direction. Founder drift is identified there as the remaining noise source.

**Pattern.** P1's utility terms (expected energy reward, injury risk, escape probability, opportunity cost) are specified in the program plan and absent from `DecisionSystem`. P2's memory sidecar is specified and implemented as three slots. Both prototypes are described as delivered. The gate discipline in the plans is sound; it is not being enforced against the plans themselves.

## Suggested order

1. **A-1**, then **A-2** — hash-safe, unblock all diagnosis, no evidence cost.
2. **B-1** — the P2 evidence is currently meaningless without it.
3. **B-3** and **B-4** together — they share a cause and fixing one alone helps little.
4. **C-1** — unblocks B-2 and B-6.
5. **C-2** — the roadmap's stated next priority, and B-3/B-4 make approach behavior work first.
6. **B-5**, **B-6**, **B-8**, **C-3**, **C-4**, **C-5** as their dependent slices arrive.

Items in groups B and C each need a versioned scenario migration and re-run evidence before their prototype can be described as passing.
