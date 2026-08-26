# Play mode profiled at last: 1,090 renderers at 353 fps, and the terrain optimisation queue is unnecessary

**2026-08-24.** `Logs/performance.txt`, written by the running game. Two readings from the `Y`
terrain playtest, the second with the planet view up.

This is the first time anything rendered by this project has been observed running. Every performance
suggestion on record — the external design brief's and my own review of it — was unranked until now.

## The readings

| | arena view | **planet view** |
|---|---|---|
| frames sampled | 986 | 1,664 |
| **median** | 3.02 ms (**332 fps**) | **2.83 ms (354 fps)** |
| p90 | 4.09 ms | 3.29 ms |
| p99 | 13.24 ms | 8.10 ms |
| **worst frame** | **197.52 ms** | **19.08 ms** |
| renderers | 49 | **1,090** |
| triangles | 90,064 | **566,272** |
| draw calls | 143 | 597 |
| heatmap, worst call | — | **0.00 ms** |

## The headline

**1,090 renderers and 566,272 triangles run at a median of 2.83 ms.** That is *faster* than the arena
view with 49 renderers, and it is twenty-two times the renderer count at no cost.

**The terrain optimisation queue is unnecessary.** GPU heightmap displacement, CDLOD, shared index
buffers, and the 53 MB of de-indexed vertex data I calculated in
`docs/terrain-brief-review-2026-08-24.md` — all of it is real, and none of it matters at this scale.
The external brief's own top item said terrain probably was not the bottleneck. **It was right, and
that is the most useful thing in it.**

Note also **1,090 renderers producing only 597 draw calls**: Unity is batching roughly half of them
unaided, which is a large part of why the count is harmless.

## The stutter was a one-off, and the suspect is cleared

The first reading had a **197.52 ms worst frame**, which looked like a recurring hitch worth chasing.
The leading suspect was the temperature heatmap — 128x128 is 16,384 terrain samples on the main
thread every two seconds — so it was instrumented rather than accused.

**The heatmap's worst single call is 0.00 ms**, and the worst frame in the second reading is
**19.08 ms**. One dropped frame at 60 fps, in steady state, over 1,664 frames.

So the 197 ms was **first-entry cost** — scene construction, chunk building and shader warm-up on the
frames right after the view opened — not a recurring stutter. **Instrumenting it was what showed
that**; the frame-time percentiles alone said only that something had happened once.

## What this closes and what it corrects

**Closes:** "Profile Play mode" has been the highest-value open item since the terrain review, and
section 9's note that *"nothing in Play mode has been seen"* has stood for the life of the terrain
work. Both are now answered.

**Corrects the 908 figure.** The handoff records "908 chunks means 908 renderers" from reading the
quadtree rather than from measuring. The measured number with the planet view up is **1,090
renderers and 566,272 triangles**, which also settles the inconsistency my own review flagged between
the recorded 232k triangles and 908 chunks — **neither figure was a measurement**, and both should be
replaced by these.

**Corrects my instruction, not the tool.** The first reading was taken with the arena view, because I
told the user to press `Y` and the chunked planet is behind `O`. 49 renderers against 1,090 is the
difference, and the run was nearly reported as "the planet is cheap" when the planet was not on
screen.

## What is still not known

- **One machine, one session.** No claim about lower-end hardware.
- **Population was 9 and 17 creatures**, far below the cap of 100. Creature rendering at full
  population is untested, and creatures are one renderer each.
- **This is the editor.** Draw calls are only available there, and editor overhead usually makes
  things *slower* rather than faster, so the figures are conservative — but they are not a build.

## Consequence

**Do not spend time on terrain rendering performance.** If a frame-rate problem ever appears, take a
fresh reading first: the file is written automatically every five seconds and costs nothing to
consult. The instrument is committed, so this is now a re-checkable measurement rather than a
one-off.
