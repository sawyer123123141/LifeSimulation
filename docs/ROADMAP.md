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
- ~~safety-gated rendezvous~~ **REASSESSED 2026-08-22 and closed as "works, buys nothing".** The gate is live and correctly signed — it cuts fleeing (t −5.07 per creature-tick) and cuts predation deaths (t −4.64, 70/120) — but the benefit does not propagate: extinction does not move under a paired McNemar test (χ² 1.49), and the raw birth gain is exposure, not fertility (birth *rate* t +1.24, and t +1.01 among seeds where both arms survived). **Starvation, not predation, limits this population.** Flag stays default false. Do not build pack architecture, group cohesion or family structure to force an effect; the mechanism already works and the ecology declines to reward it. Reopen only in a predation-limited habitat. Distinct from home-range affinity, which was closed for having the *wrong sign*. See `experiments/p4a-rendezvous-headroom-2026-08-22.md`. Two-parent reproduction remains unbuilt.
- **The population cap is load-bearing ecology, not a safety limit** (measured 2026-08-22). Raising it does not free the population to grow, it kills it: extinct 0/8 at cap 72, 5/8 at 84, 8/8 at 96 and above, where runs boom to ~293 births then collapse on starvation. Any conclusion drawn from an arm pinned at its cap should be re-checked — the 2026-08-21 rendezvous experiment ended at exactly 48 in all 240 runs.
- optional juvenile local-area bias; offspring remain fully simulated individuals and do not become follower GameObjects (**premise weakened 2026-08-22** — this was the presumed fix for extinction under separated food/water, but that failure is a reproduction-gate problem, not juvenile mortality: nothing starves or dehydrates, and adults meet the joint 70%/70% reproduction gate only 33.5% of the time when commuting versus 95.0% co-located. See `experiments/p4a-founder-mortality-2026-08-22.md`. If this item is built, build it for a reason that is not this one.)
- clear visible action/need feedback, selected-creature history, births/deaths, resource depletion/recovery, and lineages (**largely done 2026-08-22**: the creature inspector names the specific reason a creature cannot breed and the population panel shows how many adults are ready to breed right now — the joint reproduction gate was the binding constraint on every separated-resource world and was completely invisible while watching. A second panel now shows the selected creature's **history**: its recent action episodes with the need change across each one, so a foraging trip that ended with less energy than it started reads as the failed trip it was, plus what it has spent most of its watched life doing. `CreatureActionHistory` is an outside observer like `LivenessRecorder` — the world never reads it, so it is absent from every hash and cannot change a tick; a test pins that an observed and an unobserved world have identical V2 fingerprints. **Resource depletion/recovery feedback closed 2026-08-30, and the finding is that there is almost
nothing to watch.** The feedback itself works and is now shared, tested and rendered: a site's marker
scales from 0.08 to 0.5 in height and from 20% to full brightness with `Amount / Capacity`, in
`ResourceMarkerAppearance` — pure, six headless tests, called by both the presenter and the arena
capture, which had never drawn resource markers at all. **But in `Y` at tick 12,000 the median food
site is at fill 1.00 and 21 of 22 sit above 75%.** The population is cap-limited rather than
food-limited — Phase I recorded starvation as 0.1% of deaths at cap 100 — so patches sit at capacity
and there is no recovery to see. Watching depletion needs a food-limited condition, not more UI. See
`experiments/p6-generated-plant-placement-2026-08-30.md` for the run these numbers come from.
**Lineage display: the inspector shows `Gen N` and both parent ids.** An ancestry or tree view is P5
work (`lineage IDs`, `evolutionary tree`), not this item.)
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
