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
- explicit resource-intent execution: remembered targets must resolve to a real visible resource before eating or drinking
- soft home-range affinity: creatures prefer familiar successful areas but can leave under need, danger, scarcity, mating opportunity, or exploration pressure
- clustered, changing plant/resource patches so travel creates recognizable routes rather than unstructured wandering
- safety-gated rendezvous and two-parent reproduction, creating short-lived family/local groups without hardcoded species packs
- optional juvenile local-area bias; offspring remain fully simulated individuals and do not become follower GameObjects
- clear visible action/need feedback, selected-creature history, births/deaths, resource depletion/recovery, and lineages
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
