# Performance Strategy

## Main rule

Architecture first, micro-optimization second.

A terrible O(N²) system in C++ can still lose to a sane C# spatial-grid implementation.

## Important metric

The main simulation metric is **creature-seconds simulated per real second**, not just rendered FPS.

Also track:
- creatures simulated
- simulation ticks/sec
- perception queries/sec
- decision evaluations/sec
- memory used/creature
- milliseconds spent in each system
- visible/rendered creature count

## Tick separation

Rendering and biology do not need the same frequency.

Starting values to benchmark:

```text
Rendering           60+ FPS
Movement            10-30 Hz
Perception           2-10 Hz
Decision making      2-5 Hz
Needs                1-5 Hz
Population stats     ~1 Hz
```

Interpolate visuals when needed.

## Data layout

Prefer:
- structs
- arrays / contiguous storage
- stable integer IDs
- indexes instead of deep object references
- no per-tick allocations in hot loops
- pooling for visual GameObjects
- cached queries

Avoid thousands of independent MonoBehaviours each running expensive `Update()` logic.

## Spatial partitioning

Prototype: uniform grid / spatial hash.

Each creature queries nearby cells instead of every entity in the simulation.

## Burst

Use Burst once the system is correct and profiling identifies a hot numerical loop.

Likely candidates:
- metabolism updates
- movement integration
- distance/perception filtering
- utility-score calculations
- mutation math
- environmental updates

## Jobs

Use Unity Jobs where large groups can be processed independently.

Likely candidates:
- metabolism
- perception candidate filtering
- decision-score calculation
- phenotype calculation

Parallelism is not free. Avoid creating job dependency spaghetti that spends more time synchronizing than simulating.

## Simulation LOD

Long term:

```text
Near       full individual AI + visuals
Medium     reduced perception/decision frequency
Far        simplified individuals
Very far   aggregate population simulation
```

The simulation should eventually be capable of containing more creatures than it renders.

## Native C++

Do not add C++ merely because C++ has a higher performance ceiling.

Consider native code only when:
1. profiling shows a major isolated bottleneck,
2. algorithm/layout improvements were attempted,
3. Burst/Jobs are insufficient,
4. interop cost is acceptable,
5. the complexity is justified.

## Benchmark discipline

For meaningful benchmark results, record:
- commit SHA
- hardware
- build type
- creature count
- simulation speed setting
- average simulation step ms
- p95 simulation step ms
- FPS / render frame time
- memory usage
- notes

Performance regressions should become measurable rather than based on whether the game "feels kinda slower now."
