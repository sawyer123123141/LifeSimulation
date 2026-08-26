## 1. What was completed this session

Twenty commits, `f0a691d` through `c197061`. Three phases.

### Phase A — P4a feature work

| commit | what |
|---|---|
| `f0a691d` | key `R` home-range playtest; P5 panel hides routine continuity rows |
| `a817ccb` | home-range measured null for route formation |
| `173f3a3` | `ObservationRouteRing` scenario; home-range **closed as a measured negative** |
| `c528ced` | `ObservationShiftingPatches` scenario; map-turnover measurement |
| `6d64df0` | key `V` shifting-patches playtest at world seed 45 |
| `0b0387c` | founder mortality diagnosis: separation **sterilises**, does not kill |
| `15c7a5a` | inspector shows *why* a creature cannot breed; "ready to breed" count |

### Phase B — evidence-integrity audit (triggered by an external code review)

| commit | what |
|---|---|
| `4cc9a47` | **fix:** `PlantPatchStore.ReplaceAt` did not reset plant age |
| `9763374` | **fix:** statistics sampled before deaths committed; `CaptureStatistics()` added |
| `c97efe9` | paired old-vs-fixed blast-radius audit |
| `0fbb7f8` | `CaptureStatistics` guarded to a settled step boundary; corrected an overclaim |
| `1eb801c` | affected-evidence ledger widened to 9 docs; 168-site replication committed |
| `3f3b77c` | plant lifetime accounting **survives** the fix |
| `c19c1a5` | plant corpus revalidated on fixed code |
| `8f55b6e` | low-occupancy replication calibrated (occupancy is a cliff) |
| `06d80a8` | low-occupancy conclusions are **unverifiable** |
| `bbd7a76` | grazing deficit quantified |

### Phase C — P1 queue (all four items complete)

| commit | what |
|---|---|
| `c751a13` | finite/range validation at config and scenario boundaries |
| `b8a61a7` | mandatory experiment manifest + scenario layout fingerprint |
| `d3fac12` | P5 clustering allocation made linear in population |
| `c197061` | resource allocation benchmarked and **deliberately not optimised** |

### Phase D — state fingerprint V2 (the item the last handoff queued)

| commit | what |
|---|---|
| `7343653` | `ComputeStateFingerprint` (V2) + `SimulationConfig.ComputeConfigurationHash`; `BehaviorHash` extended with plant age/cooldown after measuring it changes no verdict |
| `32900de` | selected-creature action history — an outside observer, testable headlessly |
| `e6ce068` | safety-gated rendezvous reassessed at an operating point with survival headroom |

---

### Phase E — terrain (2026-08-23)

| commit | what |
|---|---|
| `94b2686` | terrain statistics instrument; baseline recorded |
| `e40eb7d` | planet was too cold and too wet to have biomes |
| `9ab9a2a` | icosphere planet, blended biomes, flat views centred on land |
| `e189ccf` | striped combs were lighting and z-fighting, not terrain |
| `02579d5` | **second brainstorm: the bounded 0..1 range was the defect** |
| `2cbedcb` | **signed elevation and plate blending, from the reference implementation** |
| `8da9b72` | live preview and offline capture unified onto one build path |
| `e6da13d` | planet-marked render: are the two views the same world? |
| `eead1b1` | creature-scale terrain; the arena now stands on the planet |
| `29fb83e` | the sea was a flat primitive plane |
| `6136493`, `c326d72` | terrain handoff rewritten; next-session decision recorded |

---

### Phase F — terrain tuning, the join, and a round world (2026-08-23)

| commit | what |
|---|---|
| `9442bd0` | tunables become `TerrainSettings`; creature-scale bands retuned; `J` panel |
| `3832b23` | `tools/TerrainProbe`; each window sampled at **its own** resolvable frequency |
| `9c1c6f2` | the retune recorded with its sweep |
| `c9b73d8` | every panel control describes itself |
| `d5d04b0`, `a111e6b` | flat views can be aimed anywhere; live biome readout |
| `ce71fcb`, `1e8e2df` | **the 82 degree wall: second-nearest plate changing hands** |
| `38fea7c` | caves and rivers: what they need, before the join fixed the shape |
| `8c82c77` | generation moves into `Simulation`, with no ambient settings |
| `6c35905` | **the join** - terrain drives the environment, behind a flag |
| `2e1f2af`, `ed7879b` | `O`: the arena drawn on the planet it is a window on |
| `96990b8`, `165cb8f`, `56ba489`, `354b9e9`, `b336b7d` | four camera/toggle bugs, all mine |
| `6b87771` | `SimulationWorld` and `DecisionSystem` split into partials |

---
