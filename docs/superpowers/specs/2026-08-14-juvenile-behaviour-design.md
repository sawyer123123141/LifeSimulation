# Juvenile Behaviour Design

**Status:** design approved, staged. Fourth of four behaviour-layer specs.

**Scope:** give offspring a distinct early life stage. Resolves defect **C-5**.

**Depends on:** `2026-08-14-foraging-economics-design.md` and `2026-08-14-mating-behaviour-design.md`. Nothing depends on this spec, so it may be deferred without blocking anything.

## Honest scope note

This is the least pre-specified work in the behaviour layer. P3 trait slice 1 mentions "maturation delay" and nothing about juvenile behaviour; parental following and reduced juvenile capability appear in no earlier plan. It is on the project's stated priority list ("improve juvenile/local-area behavior"), so it is not invented from nothing, but no prior spec anticipates the mechanism.

It is therefore **staged deliberately**, smallest first, so the cheap part can ship and prove useful before the larger part is built.

Today `AdultAgeSeconds = 20f` gates reproduction only. Offspring spawn at the parents' midpoint as fully capable adults that happen to be unable to breed.

## Stage 1 — Reduced capability

Almost no new machinery. A single maturity fraction derived from existing age:

```text
maturity = Clamp01(Age / AdultAgeSeconds)
```

Three existing phenotype values are scaled by it while a creature is immature:

| Value | Effect | Reason |
|---|---|---|
| `VisionRange` | scaled from `JuvenileVisionFraction` up to 1 | undeveloped senses |
| `FearResponse` | scaled from above 1 down to 1 | juveniles are warier |
| `MaximumSpeed` | scaled from `JuvenileSpeedFraction` up to 1 | undeveloped locomotion |

Nothing else changes. No new state, no new gene, no new action — maturity is computed from `Age`, which already exists and is already hashed.

**What this buys:** juvenile mortality becomes real and age-structured rather than uniform. That alone changes selection: traits that help the young survive gain value, and lineages that invest in fewer, better-provisioned offspring gain a mechanism to win. It also makes `FertilityInvestment`, an existing P3 gene, meaningful in a way it currently is not.

**What it does not buy:** nothing visibly different on screen. Juveniles just move slower and see less.

## Stage 2 — Parental following

Add when stage 1 is in and proven.

An immature creature scores a `FollowParent` action toward its nearest surviving parent:

```text
followScore = (1 − maturity)
            × ParentalAttachment
            × Clamp01(1 − distanceToParent / LeashRadius)
```

It competes with other actions rather than overriding them: a starving juvenile still forages, a threatened one still flees. Following wins when nothing is urgent, which is when real juveniles stay close.

Parent identity comes from the existing `Lineage` record. No new relationship tracking is required. If neither parent survives, the term is zero and the juvenile behaves as a small adult.

`ParentalAttachment` is a new gene, following the project's trait rule:

- **Benefit:** juveniles stay in territory a parent already found survivable, inheriting a good location without inheriting knowledge of it.
- **Cost:** a maintenance term in `Phenotype.FromGenome`, plus real crowding — following concentrates a family on the same patch, so they compete with each other for it.
- **Falsifiable experiment:** attachment should rise where resources are patchy and danger is high, and fall where resources are uniform and safe. If it drifts identically under both, it is not paying for itself.

**What this buys:** family groups moving together — the first genuinely visible social structure in the simulation, readable at a glance without an inspector.

**Deliberately excluded:** parental feeding, kin recognition beyond direct parents, sibling bonds, and group defence. Each is a separate design with its own justification, and none is needed for family groups to appear.

## Components

**Stage 1**

| File | Change |
|---|---|
| `Biology/GenomePhenotype.cs` | `Phenotype.AtMaturity(float maturity)` returning scaled vision, fear, and speed |
| `Core/SimulationWorld.cs` | Use the maturity-scaled phenotype in perception, movement, and decisions |
| `Core/SimulationConfig.cs` | `JuvenileVisionFraction`, `JuvenileSpeedFraction`, `JuvenileFearMultiplier`, `JuvenileBehaviourEnabled` |

**Stage 2**

| File | Change |
|---|---|
| `Behavior/JuvenileSystem.cs` | **New.** Follow scoring and parent lookup |
| `Core/SimulationTypes.cs` | `CreatureAction.FollowParent` |
| `Biology/GenomePhenotype.cs` | `ParentalAttachment` gene, maintenance cost, passthrough |
| `Core/SimulationConfig.cs` | `LeashRadius` |

Both stages gated behind `JuvenileBehaviourEnabled`, default `false`.

## Testing

**Stage 1**

1. A newborn's effective vision range is `JuvenileVisionFraction` of its adult value.
2. Effective values reach full adult values exactly at `AdultAgeSeconds`.
3. Scaling is monotonic in age — no discontinuity at maturity.
4. Juvenile mortality is measurably higher than adult mortality in a baseline scenario.
5. With `JuvenileBehaviourEnabled = false`, behaviour is unchanged.

**Stage 2**

6. A juvenile with a surviving parent nearby and no urgent need produces `FollowParent`.
7. A starving juvenile forages instead of following.
8. A threatened juvenile flees instead of following.
9. A juvenile with no surviving parent never produces `FollowParent`.
10. Follow score falls to zero at `LeashRadius` and at full maturity.
11. `ParentalAttachment` carries a maintenance cost: two genomes differing only in it have different `BasalEnergyCostMultiplier`.

## Exit gate

**Stage 1:** fixtures 1–5 pass, and juvenile mortality is demonstrably age-structured rather than uniform.

**Stage 2:** fixtures 6–11 pass, family groups are visibly identifiable in a running scenario, and a paired-seed experiment shows `ParentalAttachment` shifting in opposite directions under patchy-dangerous and uniform-safe treatments, or the trait is reconsidered.
