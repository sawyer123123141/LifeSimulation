# P4a is to be judged inside the pre-crash window, and the boundary is tick 8,000

**Date:** 2026-09-03
**Status:** a scoping note recording the user's ruling. **No code changed, no scenario changed, and
the gate verdict in the frozen spec is untouched** — section 4 of
`docs/superpowers/specs/2026-08-30-what-finished-means-design.md` remains the user's to update.
**Evidence:** `docs/experiments/p6-the-shipped-world-does-not-persist-2026-09-03.md`.

## The situation

P4a — *"distinguish foraging, drinking, mating, fleeing, resting, and resource recovery"* — is
verified by watching, and what is watched is `Y`. `Y` is therefore the gate's acceptance surface, and
that is not a choice anyone made recently: it is the scenario the presenter loads.

`Y`'s shipped configuration is now measured as **extinct in 24 of 24 worlds at 36,000 ticks**, and the
same layout at cap 96 is level over the same span, so the collapse belongs to the cap-and-brake change
rather than to the layout or the model.

That leaves the gate in an odd position: the surface it is judged on does not persist. **The ruling is
that P4a is judged on the interval before the crash rather than deferred until the configuration
question is settled.** The behaviours the gate asks about are visible long before the population
turns, and holding a watchability gate hostage to an ecology decision would stall it on something it
does not depend on.

## The boundary, and where the number comes from

> **Tick 8,000.** A P4a observation is admissible if it is taken at or before tick 8,000 of a `Y` run.

Derived from the collapse curve, 24 seeds, all-world figures per 4,000-tick interval:

| sample | tick | worlds alive | population (alive) | starvation, share of deaths **in that interval** |
|---|---:|---:|---:|---:|
| 1/9 | 4,000 | 24 / 24 | 13.7 | **0.0%** |
| 2/9 | **8,000** | 21 / 24 | 55.7 | **0.0%** |
| 3/9 | 12,000 | 20 / 24 | 154.1 | **22.9%** |
| 4/9 | 16,000 | 12 / 24 | 130.8 | **65.6%** |

**8,000 is the last sample at which the world shows no starvation at all.** Starvation is the mechanism
of the collapse, so the interval in which its share is zero is the interval in which the collapse has
not started. By the next sample it is nearly a quarter of deaths and nine worlds are lost in the one
after that.

Two honest qualifications on the number:

- **The onset is bounded, not located.** The trajectory samples every 4,000 ticks, so all that is
  established is that the collapse begins somewhere in **(8,000, 12,000]**. 8,000 is the last
  measured-clean point, which is why it is the boundary rather than 10,000 or 11,999. Locating the
  onset precisely would need a run sampled more finely, and nothing here requires that.
- **The three worlds lost by tick 8,000 are not the collapse.** The cap-96 comparator arms also sit at
  21-22 of 24 from tick 8,000 onward and stay there to 36,000. Those are early establishment failures,
  a different phenomenon, and they are present in configurations that never collapse.

**12,000 is not the boundary, even though it is the population peak.** The peak is the largest the
population gets, not the last moment it is healthy: starvation is already 22.9% of deaths in the
interval that ends there. A window drawn at the peak would include the onset of the thing it is
supposed to exclude.

## The tension this creates, stated rather than resolved

The gate item that motivated the whole cap-and-brake change was **resource recovery**:
`p6-y-is-food-limited-2026-08-30.md` records that at cap 96 *"nothing was ever hungry"*, patches sat at
88% full, and there was nothing to watch. Cap 500 with the brake was adopted to produce hunger.

But hunger in this configuration arrives **with** the collapse, not before it: starvation is 0.0% of
deaths through tick 8,000 and 22.9% by 12,000. So the window this note defines is the window in which
the world is stable, and it may also be the window in which **resource depletion and recovery are not
yet visible** — which is the one P4a item that cap 96 could not satisfy either.

Five of the six behaviours — foraging, drinking, mating, fleeing, resting — are unaffected and can be
verified inside the window immediately. **Resource recovery may not be verifiable in either
configuration**, and that is a finding about the gate rather than about the window. It is recorded here
so that a later session does not discover it as a surprise, and it is not resolved here: resolving it
means changing an ecology value, which is a separate declared experiment.

## What this note does not do

- It does not change the P4a verdict in the frozen spec, which stands at **UNVERIFIED**.
- It does not change `Y`, its cap, or its brake.
- It does not propose an ecology value of any kind.
