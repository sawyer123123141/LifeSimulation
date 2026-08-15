# Documentation Index

Start here. This page says what every document is and the order to read them.

## The three kinds of document

| Kind | Answers | Where |
|---|---|---|
| **Spec** | What are we building, and why? | `superpowers/specs/` |
| **Plan** | How do we build it, step by step? | `superpowers/plans/` |
| **Record** | What did we measure, and what is broken? | `benchmarks/`, `experiments/`, the defect list |

A spec becomes a plan becomes code. Records are evidence.

## The two numbering systems

They are unrelated. This trips people up, so:

**P0 through P7** are the project's roadmap phases. They describe *what the simulation can do*.

| Phase | Question it answers |
|---|---|
| P0 | Can environmental pressure shift inherited traits? |
| P1 | Can predator and prey roles emerge without labelling them? |
| P2 | Does memory pay for what it costs? |
| P3 | Do body trade-offs create distinct survival strategies? |
| P4 | Can plants and animals evolve in response to each other? |
| P5 | Can we summarise species and history without faking it? |
| P6 | Can the world hold more life than it can simulate at once? |
| P7 | Can a player move from one animal to the whole planet? |

**T0 through T7** are world-generation pieces. They describe *how a world gets built*.

| Piece | What it does |
|---|---|
| T0 | Tectonic plates and their boundaries |
| T1 | Elevation, temperature, moisture, fertility, caves |
| T2 | Rivers, lakes, coastlines |
| T3 | Layered world so animals can go underground |
| T4 | Biome classification |
| T5 | Splitting the planet into regions |
| T6 | Terrain meshes, streaming, level of detail |
| T7 | Zooming from one animal out to the planet |

**A, B, C** on the defect list are severity, not sequence:

- **A** — safe to fix. Cannot change recorded results.
- **B** — real bug, but fixing it changes recorded experiment results. Needs a migration.
- **C** — was specified in an earlier phase and never actually built.

## Read in this order

**To understand the project:**

1. `../README.md` — what this is
2. `ARCHITECTURE.md` — how simulation and rendering stay separate
3. `ROADMAP.md` — the phases, briefly
4. `superpowers/specs/2026-08-12-product-architecture.md` — the phases in full, plus the permanent rules

**To understand the current state:**

5. `superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md` — every known defect, with the code that proves it
6. `benchmarks/` and `experiments/` — what has actually been measured

**Before writing code:**

7. `../AGENTS.md` — rules for AI agents. Determinism, allocation, scope.
8. `PERFORMANCE.md` — what to measure and when to optimise

## Every document

### Yours, written before August 2026

| File | What |
|---|---|
| `ARCHITECTURE.md` | Layer separation, creature representation, utility brain |
| `PERFORMANCE.md` | Performance strategy and required metrics |
| `PROTOTYPE_1.md` | Prototype 1 requirements |
| `ROADMAP.md` | P0–P7 summary |
| `superpowers/specs/2026-08-12-product-architecture.md` | Full P0–P7 architecture and permanent principles |
| `superpowers/specs/2026-08-12-prototype-1-design.md` | Detailed P0 design |
| `superpowers/plans/2026-08-12-p0-evolution-proof-implementation.md` | P0 build plan |
| `superpowers/plans/2026-08-12-p0-p7-program-plan.md` | Delivery sequencing for every phase |

### Added 2026-08-14

**Specs — what to build**

| File | What |
|---|---|
| `superpowers/specs/2026-08-14-system-integration-design.md` | **Read this first.** How world generation and creature behaviour form one simulation, plus the scale numbers |
| `superpowers/specs/2026-08-14-simulation-defects-and-behavior-gaps.md` | The bug list. Everything wrong with the code today, grouped by how dangerous the fix is |
| `superpowers/specs/2026-08-14-world-generation-design.md` | T0–T7. Plates, fields, rivers, caves, world recipes |
| `superpowers/specs/2026-08-14-foraging-economics-design.md` | How an animal values food and decides to leave a patch |
| `superpowers/specs/2026-08-14-place-memory-design.md` | How an animal remembers specific places |
| `superpowers/specs/2026-08-14-mating-behaviour-design.md` | How animals find, choose, and court mates |
| `superpowers/specs/2026-08-14-juvenile-behaviour-design.md` | Young animals: weaker senses, then following a parent |

The last four are one system, built in that order. Foraging comes first because commitment is what makes any of the rest visible — behaviour that lasts half a second looks like jitter no matter what genes drive it.

**Plans — how to build it**

Build them in this order. Each assumes the ones above it are done.

| # | File | Builds | Safe for a small model? |
|---|---|---|---|
| 1 | `superpowers/plans/2026-08-14-death-causes.md` | Recording what an animal died of | Yes. Cannot change results |
| 2 | `superpowers/plans/2026-08-14-decision-diagnostics.md` | Recording why an animal chose what it chose | Yes. Cannot change results |
| 3 | `superpowers/plans/2026-08-14-foraging-economics-scoring.md` | The patch-scoring functions | Yes. New files only |
| 4 | `superpowers/plans/2026-08-14-foraging-economics-integration.md` | Wiring scoring into the simulation | No. Changes behaviour and hashes |
| 5 | `superpowers/plans/2026-08-14-place-memory.md` | Remembering specific places, and learning their quality | No. Changes cognition-mode results |
| 6 | `superpowers/plans/2026-08-14-mating-behaviour.md` | Seeking, choosing, and courting mates | No. Changes reproduction |
| 7 | `superpowers/plans/2026-08-14-juvenile-behaviour.md` | Weaker young, then following a parent | No. Two stages; stage 1 ships alone |

| — | `superpowers/plans/2026-08-14-local-model-capability-test.md` | Not a plan — a test for judging whether a local model can be trusted with the others | — |

Plans 1–3 cannot damage recorded evidence. Plans 4–7 all change simulation behaviour, so each one needs its recorded results re-run afterwards, and each is gated behind a config flag so the old behaviour stays available.

### Elsewhere in the repository

| File | What |
|---|---|
| `../AGENTS.md` | Rules for AI agents editing this code |
| `../CODEX_TASK.md` | The original bootstrap task. Historical |

## Naming rules

- Files are named for what they do, not for a code. `death-causes`, not `a1`.
- Letter codes appear inside documents only, never in filenames.
- `T` is world generation. `P` is roadmap phase. `A`/`B`/`C` is defect severity. Nothing else uses single letters.
