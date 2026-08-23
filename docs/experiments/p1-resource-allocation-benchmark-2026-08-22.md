# Resource allocation: benchmarked, and deliberately not optimised

**Date:** 2026-08-22
**P1 item 4:** "benchmark crowded resource allocation before optimizing it"
**Outcome:** measured; **no optimisation made**, and the reason is recorded.

## What the code actually does

`ResourceAllocationSystem.Resolve` walks the request list and, for the first request against each
resource, scans the whole list twice — once to sum demand for that resource, once to write scaled
allocations — plus a backward scan to detect whether that resource was already handled.

The shape is often described as O(R²) in requests. It is not. The expensive branch runs **once per
distinct resource**, so the cost is **O(requests × distinct resources)**.

## Measured cost

200 repeats per configuration, warmed up first.

| requests | distinct resources | per resolve |
|---|---|---|
| 40 | 1 | 0.82 µs |
| 40 | 24 | 7.42 µs |
| 100 | 1 | 1.78 µs |
| 100 | 24 | 17.61 µs |
| 400 | 1 | 6.83 µs |
| 400 | 24 | 66.33 µs |
| 1000 | 1 | **16.90 µs** |
| 1000 | 24 | **165.12 µs** |

**The framing in the review is backwards.** Crowding — many creatures contending for *one* resource
— is the **cheap** case: 1,000 requests on a single resource resolve in 16.9 µs, because the
backward scan short-circuits every request after the first and the expensive branch runs exactly
once. What costs is the **number of distinct resources**, which multiplies the full-list scans. At
1,000 requests, going from 1 resource to 24 costs 10x.

## Whether it is ever hot in a real run

A quadratic that never sees more than a few dozen inputs is not a hot spot, whatever its shape. Full
12,000-tick runs, end to end, including every other system:

| scenario | peak population | total | per tick |
|---|---|---|---|
| ObservationStable (cap 40) | 38 | 0.14 s | **0.012 ms** |
| calibration (cap 48) | 48 | 1.09 s | **0.090 ms** |
| calibration (cap 1000) | 523 | 2.72 s | **0.227 ms** |

Requests per tick are bounded above by population, so the worst realistic case measured here —
523 creatures across the calibration's 24 sites — has an allocation cost bounded by roughly 0.17 ms,
against a **whole-tick** cost of 0.227 ms that already contains it. A 12,000-tick run of the largest
population reachable in a committed scenario takes **2.72 seconds in total**.

## Decision: do not optimise

There is no performance problem to fix. Optimising this would mean touching a deterministic
allocation path — one whose ordering semantics decide who eats when resources run short — to save a
fraction of a millisecond in runs that already complete in seconds. The risk is entirely on the
correctness side and the benefit is unmeasurable at every population the project can currently
reach.

**What would change this decision:** populations in the thousands *and* site counts in the hundreds
at the same time. The 168-site replication has 168 sites, but the population cap that goes with it
is 48, so it is nowhere near. If both grow, the fix is straightforward and should be done then, not
now: bucket requests by resource index in a single pass, then resolve each bucket, which turns
`O(requests × distinct resources)` into `O(requests)`.

## What was measured and what was not

Measured: synthetic resolve cost across request counts and resource counts; end-to-end tick cost at
three population caps.

**Not measured: the actual number of requests per tick.** The bound above uses population as a
ceiling, which is sound for the decision — the true figure is lower, so the real cost is smaller
than stated — but it means this document does not report allocation as a *share* of tick time. If
anyone later wants that attribution rather than a bound, it needs instrumenting the request count
directly.
