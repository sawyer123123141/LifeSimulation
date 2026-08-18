# P4 Calibration Blocker — Memory Root Cause Retracted, Overshoot Measured — 2026-08-17

> **SUPERSEDED IN PART 2026-08-17.** The conclusion that the calibration's
> constraint set is unsatisfiable is withdrawn. It is satisfiable at 4 active
> plant sites; the binding variable was site count, not lifespan or population
> cap. See `p4-calibration-unblocked-carrying-capacity-2026-08-17.md`. The
> place-memory retraction and the overshoot measurement below both stand.

Supersedes the "Root cause: place memory has no invalidation for vanished
resources" section of `p4-plant-mortality-calibration-blocked-2026-08-17.md`.
That section is **wrong** and its recommended order of work ("fix place-memory
invalidation first") would have produced no behavior change at all.

## The retraction

The prior finding held that `MemorySystem.RecordFailedPlaceSearch` — the
"I travelled to a remembered spot and the food was gone" invalidation — was
gated on `DecisionPolicyVersion.Legacy` while every P4 scenario runs
`IntentUtilityV1`, leaving place memories uncorrectable.

The gating is real. It is also irrelevant, because **the entire place-memory
subsystem has never executed in production.**

`MemorySystem.ObservePlace` is the only writer of `PlaceMemory` slots. Its only
callers, repo-wide, are in `Assets/Tests/EditMode/PlaceMemoryObservationTests.cs`.
`MemorySystem.TickPlaceMemoryDecay` likewise has no production caller. Every
slot therefore holds `VisitCount == 0, Confidence == 0` for the life of every
run, which makes:

- `SimulationWorld.TryScoreBestRememberedPlace` always return false — its loop
  `continue`s on every slot,
- `MemorySystem.RecordFailedPlaceSearch` a guaranteed no-op — no slot can match.

Porting `RecordFailedPlaceSearch` to `IntentUtilityV1` would have ported a
no-op. This is the sixth halfway-wired mechanism found in this codebase and the
largest: a seven-task feature landed with its write path never connected.

Measured corroboration under the calibration config: creature-ticks spent
pursuing a memory-sourced target (`Action == SeekFood/SeekWater` with
`TargetResourceIndex < 0`) are **0.0–0.6%** of all creature-ticks, 10 to 660 out
of ~130,000 per seed. Creatures barely act on memory at all, so no memory defect
can account for a doubling of extinctions.

## What the cognition A/B actually measures

The documented contrast reproduces on the recovered config (seeds 42-71,
`ConsumerDefenseCalibrationModerate`, 12,000 ticks, 12 founders,
`maximumPopulation: 48`, site competition and plant mortality both enabled):

| cognition | animal extinctions | min plant generations | mean peak population | mean energy |
| --- | ---: | ---: | ---: | ---: |
| enabled | 30/30 | 10 | **48.0** | 67.1 |
| disabled | 16/30 | 9 | **31.3** | 70.8 |

(The original recorded 29/30 and 14/30; the one-seed difference in each arm is
that the original called `ExperimentRunner.Run` where this replication steps the
world directly. The contrast is the same.)

The mechanism is the peak-population column, not navigation. With cognition on,
every one of thirty seeds saturates the population ceiling. Mean per-capita
energy is *lower* with cognition, not higher, because five times the population
shares the same eight patches. Cognition does not mislead creatures into
starving; it makes them efficient enough to breed to the ceiling, and the
ceiling exceeds what the scenario can feed.

## Overshoot sweep

Cognition left enabled, population cap swept, same 30 seeds:

| maximumPopulation | animal extinctions | mean peak population | mean energy | min plant generations |
| ---: | ---: | ---: | ---: | ---: |
| 16 | 18/30 | 16.0 | 84.2 | 10 |
| 24 | 26/30 | 24.0 | 80.2 | 10 |
| 32 | 30/30 | 32.0 | 74.5 | 10 |
| 48 | 30/30 | 48.0 | 67.1 | 10 |

Population saturates every ceiling exactly. Extinctions rise monotonically with
the ceiling; per-capita energy falls monotonically. This is carrying-capacity
overshoot — boom then bust — which is ecologically correct behavior for
efficient foragers in a closed system with a hard population cap, not a defect.

Two qualifications, both against a pure-overshoot reading:

1. Cognition carries a cost beyond headcount. Cognition-off at cap 48 reaches
   mean peak 31.3 with 16/30 extinctions; cognition-on at cap 32 reaches peak
   32.0 with 30/30. At comparable population the cognitive arm still does
   strictly worse. Overshoot is the dominant lever, not the only one.
2. Lowering the cap does not rescue the constraint. At cap 16 — one third of the
   original, mean energy 84 — 18/30 seeds still go extinct.

`minPlantGen` holds at 10 across every cap. Plant turnover is robust and
entirely insensitive to the animal population ceiling, so plant mortality is
working as designed and is not implicated.

## Consequence for P4

The calibration's constraint set is unsatisfiable, and now known to be
unsatisfiable along two independent axes rather than one:

- across an 8x sweep of `BaseLifespanSeconds` (recorded in the prior document),
  plant turnover and animal survival move together monotonically;
- across a 3x sweep of `maximumPopulation`, extinctions never reach zero.

"At least 8 plant generations AND zero animal extinctions across 30 seeds" was
invented for the plant-mortality spec. It is stricter than P4's actual exit gate,
which asks for repeatable reciprocal plant/consumer trait response — that needs
both populations to persist long enough for selection to act, not literal
zero-extinction in every seed of a boom-bust system.

The open decision is therefore whether to restate the calibration constraint
(for example, persistence in a majority of seeds, or absence of total plant
collapse) rather than continue tuning the simulation until it stops behaving
like an ecosystem. That decision is not taken here.

## Standing corrections to earlier records

- The "Root cause" section of
  `p4-plant-mortality-calibration-blocked-2026-08-17.md` is retracted.
- Its closing order of work ("fix place-memory invalidation first, then re-run
  this calibration, then re-run the coevolution experiment") is void; the first
  step is a no-op.
- `BaseLifespanSeconds = 90f` remains an uncalibrated placeholder and
  `PlantMortalityEnabled` still defaults false. Nothing in this investigation
  changed any production code.

## Separately: place memory is dead code

Independent of the calibration question, the place-memory subsystem is written,
tested at 351/351, and never invoked. It should either be wired up
(`ObservePlace` on resource observation, `TickPlaceMemoryDecay` on the decay
tick, and both consumers ported to `IntentUtilityV1`) or deleted. Leaving it in
place is what produced this false root cause in the first place: the code reads
as though it runs.
