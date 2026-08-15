# System Integration Design

**Status:** design approved.

**Scope:** how world generation and creature behaviour form one simulation rather than two. Fixes the seam where terrain produces fertility values nobody reads and creatures eat point resources nobody generates. Also reconciles the scale numbers, which currently disagree by a factor of several hundred.

**This document exists because the other specs describe pieces and none describes the whole.**

## The loop

```text
plates and drift
      ↓
elevation, temperature, moisture          [T1, sampled anywhere, stores nothing]
      ↓
fertility field
      ↓
plants establish, grow, and set seed      [P4, the bridge]
      ↓
ResourceState entries                     [the existing food API, unchanged]
      ↓
creatures perceive, score, travel, eat    [foraging economics, unchanged]
      ↓
patches deplete, creatures leave          [give-up rule]
      ↓
plants regrow at a rate set by local fertility
      ↓
                 back to the top
```

Everything above the plants line is world generation. Everything below is behaviour. **Plants are the only join**, and they were always going to be — that is what the P4 phase is for.

## The bridge: plants emit into the existing resource store

The connection is deliberately narrow. Plants do **not** introduce a new thing for creatures to perceive. They populate `ResourceStore` with the same `ResourceState` entries that scenarios author by hand today.

| `ResourceState` field | Today | With plants |
|---|---|---|
| `Position` | authored by scenario | where a plant established |
| `Capacity` | authored constant | set by local fertility |
| `RegenerationPerSecond` | authored constant | set by local fertility and moisture |
| `NutritionMultiplier` | authored constant | plant genome |
| `Amount` | consumed and regrown | unchanged |

**Consequence: the entire behaviour layer needs no changes.** `PerceptionSystem` still finds resources. `ForagingEconomics.PatchScore` already takes remaining amount, distance, and nutrition. Place memory already remembers positions. Nothing in the four behaviour specs is invalidated.

This also matches what the P4 plan already requires — "Preserve the P0 food-resource API through a temporary compatibility facade while consumer systems migrate."

The facade is not temporary here. It is the permanent interface between the two halves, and keeping it narrow is what stops world generation and behaviour becoming coupled.

### What plants add that authored resources cannot

- **Spatial correlation with climate.** Food appears where fertility is high, so the wet side of a mountain range feeds animals and the dry side does not. Migration acquires a reason.
- **Recovery that varies by place.** A stripped patch in a fertile valley regrows fast; the same patch in scrubland does not. The give-up rule then produces genuinely different movement in different biomes.
- **Seasonal supply.** Fertility already varies with the temperature and moisture fields, which vary with season and climate drift. Food supply becomes cyclical without anyone scripting it.
- **Coevolution.** Plant defences and nutrition become heritable, which is the actual P4 scientific question.

## Scale reconciliation

The numbers currently disagree. This section fixes them.

### One simulation unit is one metre

Every existing constant is consistent with this reading and with a small-to-medium animal:

| Quantity | Current value | As metres |
|---|---|---|
| Vision range | 4 to 16 | 4–16 m — plausible for a ground animal |
| Maximum speed | 1 to 4 | 1–4 m/s — walk to run |
| Body mass | 0.6 to 2.4 | kilograms, roughly cat-to-dog |
| Interaction radius | ~1.1 | arm's reach |
| Arena | 50 × 50 | a large garden |

So the unit is settled by the numbers that already exist. **1 unit = 1 metre.** Nothing needs rescaling.

### The planet is small, and it still dwarfs the arena

| Planet radius | Surface area | Arenas that fit |
|---|---|---|
| 500 m | ~3.1 km² | ~1,300 |
| 2 km | ~50 km² | ~20,100 |

At roughly 30% land, that is about 400 or 6,000 arenas of habitable ground.

### Terrain forces simulation level of detail — it is not optional

The recorded benchmark handles about 1,000 creatures. At today's densities:

| Planet radius | Creatures at current density | Fraction simulable at full fidelity |
|---|---|---|
| 500 m | ~40,000 | ~2.5% |
| 2 km | ~600,000 | ~0.2% |

**This is the most important consequence in this document.** The moment terrain exists, the world holds hundreds of times more life than the simulation can run. Level of detail stops being a scaling optimisation and becomes a correctness requirement — without it, a planet is simply an empty arena with scenery.

Practically: T5 (regions) and the P6 fidelity tiers must land **with** terrain, not after it. The T-numbering hides this, because T6 is meshes and T5 is regions, which reads as though visuals come first. They do not.

**Recommended target: 500 m radius.** Small enough that the far side is a short walk, large enough for real climate bands, and it keeps the fully-simulated fraction within one order of magnitude of what the kernel already does.

## What this means for sequencing

The behaviour layer and the world layer are independent until plants exist. That is a feature — both can be built and tested in isolation — but it means the project is not one simulation until P4 lands.

| Stage | State of the project |
|---|---|
| Behaviour plans 1–7 done | Animals behave well in a hand-authored arena. No terrain involvement. |
| Terrain T0–T2 done | A planet exists and can be sampled. No creature reads it. |
| **P4 plants done** | **The two halves become one system.** Food appears because of climate; animals move because of food. |
| T5 regions and P6 fidelity | The planet can actually be populated rather than displayed. |
| T6, T7 | You can see it. |

**Plants are the keystone, and they are the least specified piece in the project.** Every other document assumes something on the other side of that bridge.

## Deliberately unchanged

- No new perception path. Plants are resources.
- No new scoring. `ForagingEconomics` already handles amount, distance, and nutrition.
- No coupling from behaviour back into world generation. Creatures read fields; they never write them. Grazing changes `ResourceState`, and plants read fields — the loop closes through plants, not through the field layer.
- No change to any of the four behaviour specs. If plants require one, the bridge has been drawn in the wrong place.

## Exit gate

This design is satisfied when:

- A plant spec exists defining establishment, growth, and seeding as functions of the fertility field.
- Plants populate `ResourceState` and no consumer system was modified to accommodate them.
- A creature in a fertile region measurably out-survives an identical creature in a barren one, in the same world, with no scenario authoring involved.
- Food supply varies with season without any scripted event.
- Planet radius, unit scale, and the fidelity budget are recorded together, and the simulable fraction is stated honestly rather than implied.
