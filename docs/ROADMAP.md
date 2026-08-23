# Roadmap

Detailed cross-prototype architecture and delivery sequencing:

- [`superpowers/specs/2026-08-12-product-architecture.md`](superpowers/specs/2026-08-12-product-architecture.md)
- [`superpowers/plans/2026-08-12-p0-p7-program-plan.md`](superpowers/plans/2026-08-12-p0-p7-program-plan.md)

## P0 - Core evolution proof

Goal: prove heritable traits can create measurable natural selection.

- simulation clock
- creature state
- genome
- needs
- food/water
- movement
- perception
- utility decisions
- reproduction
- mutation
- death
- inspector
- population graphs
- benchmark harness

## P1 - Predator/prey

- predation
- threat perception
- hunting cost
- escape behavior
- edible biomass
- carnivore/herbivore dietary differences
- population cycles

Predator/prey roles should emerge from biology rather than hardcoded labels such as `IsApexPredator = true`.

## P2 - Better cognition

- memory
- remembered resource locations
- danger memory
- imperfect information
- exploration
- learning during a lifetime
- biological cost for improved cognition

## P3 - Richer biology

- fertility
- lifespan tendencies
- digestion
- temperature tolerance
- sexual selection
- phenotype mapping from genes
- optional disease/immunity only if it creates useful evolutionary pressure

## P4 - Evolving ecosystem

- plant genetics
- plant competition
- nutrition
- defenses/toxins
- seed dispersal
- coevolution
- changing climate variables

### P4a - Watchable regional ecology

This is the bridge between a scientifically valid survival simulation and a simulation that is interesting to observe. It follows the current survival/reproduction reliability gate and precedes broad biome, terrain, and art work.

- reliable multi-generation baseline under declared resource conditions
- explicit resource-intent execution: remembered targets must resolve to a real visible resource before eating or drinking (**already implemented** in `SimulationWorld.ResolveResourceInteractions`)
- ~~soft home-range affinity~~ **CLOSED 2026-08-22 as a measured negative.** Implemented, flag-gated (default false), Play-testable on key `R`, and measured across two experiments and 240 fixed-seed runs: it does not create routes, and on purpose-built route-capable geometry it made routes *less* repeatable (t -2.87) while increasing same-site clinging (t +4.93). Geometry was tested as the rescue hypothesis and was not the blocker. Do not tune or reopen. See `experiments/p4a-home-range-affinity-2026-08-22.md` and `experiments/p4a-route-ring-home-range-2026-08-22.md`. Route behavior, if wanted, needs a mechanism that scores a *pair* of complementary resources or anticipates a need before it is urgent.
- clustered, changing plant/resource patches so travel creates recognizable routes rather than unstructured wandering (**done 2026-08-22, Play key `V`**). Routes already form from geometry alone (537 cross-kind legs at 0.7955 pair repeat with every behavior flag off), and patch turnover already exists via plant dispersal/mortality — `ObservationShiftingPatches` sustains ~29 deaths and ~33 establishments per run, which cuts route permanence and raises distinct routes per creature 27% at no survival cost. Key `V` runs it at world seed 45; the scenario's honest extinction rate is 6/30 and seed 42 is one of the failures. See `experiments/p4a-shifting-patches-2026-08-22.md`.
- safety-gated rendezvous and two-parent reproduction, creating short-lived family/local groups without hardcoded species packs
- optional juvenile local-area bias; offspring remain fully simulated individuals and do not become follower GameObjects (**premise weakened 2026-08-22** — this was the presumed fix for extinction under separated food/water, but that failure is a reproduction-gate problem, not juvenile mortality: nothing starves or dehydrates, and adults meet the joint 70%/70% reproduction gate only 33.5% of the time when commuting versus 95.0% co-located. See `experiments/p4a-founder-mortality-2026-08-22.md`. If this item is built, build it for a reason that is not this one.)
- clear visible action/need feedback, selected-creature history, births/deaths, resource depletion/recovery, and lineages (**largely done 2026-08-22**: the creature inspector names the specific reason a creature cannot breed and the population panel shows how many adults are ready to breed right now — the joint reproduction gate was the binding constraint on every separated-resource world and was completely invisible while watching. A second panel now shows the selected creature's **history**: its recent action episodes with the need change across each one, so a foraging trip that ended with less energy than it started reads as the failed trip it was, plus what it has spent most of its watched life doing. `CreatureActionHistory` is an outside observer like `LivenessRecorder` — the world never reads it, so it is absent from every hash and cannot change a tick; a test pins that an observed and an unobserved world have identical V2 fingerprints. Remaining under this item: resource depletion/recovery feedback and lineage display.)
- deterministic scenario controls for observing stable, scarcity, migration, and mating conditions

Acceptance: across fixed seeds, the same world yields repeatable routes, resource visits, births, and local population changes; a player can visually distinguish foraging, drinking, mating, fleeing, resting, and resource recovery without reading logs. These systems remain compact simulation data, not Unity navigation, flocking, or per-creature MonoBehaviour truth.

## P5 - Species and history

- genetic-distance tracking
- lineage IDs
- species clustering
- evolutionary tree
- extinct branches
- historical timeline
- major ecology events

## P6 - World scale

- multiple biomes
- deterministic procedural terrain, biome, and climate-field generation
- world partitioning
- simulation LOD
- far-away statistical populations

## P7 - Planet presentation

- small spherical / planet-like world
- zoom from organism scale to world scale
- climate regions
- planetary ecosystem overview
- historical replay / timeline views

## Art milestone

Custom fictional body plans should be introduced after the evolution loop proves interesting with temporary assets.

Possible starting body plans:
- grazer-type
- hunter-type
- small generalist

These are starting morphologies, not permanent species roles. Descendants should be able to drift into different ecological niches.
