### File sizes (`6b87771`, and the presenter split before it)

| file | before | after |
|---|---:|---:|
| `Prototype1Presenter` | 1886 | **1033** + Hud 194, Terrain 536, Views 180 |
| `SimulationWorld` | 2058 | **844** + Ticking 643, Hashing 427, Statistics 193 |
| `DecisionSystem` | 1021 | **592** + Scoring 324, Legacy 130 |

Done **mechanically**: a scanner lifts whole members by brace depth, ignoring braces inside strings
and comments; nothing rewritten, no member changing class. The simulation splits are verified by 503
green tests including every pinned hash literal. **No file was read into context to do it** - the
script is in the session scratchpad and is worth promoting to `tools/` if it is wanted again.

---
