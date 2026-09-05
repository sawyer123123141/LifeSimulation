# P4a splits: five behaviours are judgeable before tick 8,000, resource recovery is not

**Date:** 2026-09-03, **amended 2026-09-05**, **mechanism confirmed 2026-09-05 (second amendment)**
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

That leaves the gate in an odd position: the surface it is judged on does not persist.

## The ruling

**The gate splits, because its six items do not share a fate.**

> **Foraging, drinking, mating, fleeing and resting are judgeable inside the pre-crash window**, at or
> before tick 8,000 of a `Y` run. They are visible long before the population turns, and holding a
> watchability gate hostage to an ecology decision would stall it on something it does not depend on.
>
> **Resource recovery stays UNVERIFIED**, and it cannot be verified in this configuration at either
> end. It is pending a configuration in which **hunger is chronic without being fatal** — which is the
> deferred brake experiment, not a window.
>
> **Confirmed 2026-09-05:** that experiment is not a matter of picking a brake value. Hunger onset
> and collapse onset are within one trajectory sample of each other at **every** strength measured,
> 0.75 through 5.0, across four scenario families. See the section added below.
>
> **P4a therefore cannot close until that experiment is settled.** Five of six passing is not the gate.

The amendment on 2026-09-05 is this split. The first version of this note recorded the window as the
whole ruling and put the resource-recovery problem beside it as a tension; it belongs *inside* the
ruling, because it changes what the window can deliver. Section "The tension" below is superseded by
this section and kept for its reasoning.

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

## Why resource recovery is excluded, in full

*(Written first as a tension beside the ruling; promoted into the ruling on 2026-09-05, because it
decides an item rather than qualifying one.)*

The gate item that motivated the whole cap-and-brake change was **resource recovery**:
`p6-y-is-food-limited-2026-08-30.md` records that at cap 96 *"nothing was ever hungry"*, patches sat at
88% full, and there was nothing to watch. Cap 500 with the brake was adopted to produce hunger.

But hunger in this configuration arrives **with** the collapse, not before it: starvation is 0.0% of
deaths through tick 8,000 and 22.9% by 12,000, 65.6% by 16,000. So the pre-crash window is the window
in which the world is stable **and** the window in which nothing is hungry. The two are the same
window, and that is not a coincidence — the brake produced hunger by letting the population outgrow
its food, which is also how it produced the collapse.

That closes the configuration from both ends:

- **At cap 96**, nothing is ever hungry, patches sit at 88% full, and there is nothing to watch.
- **At cap 500 with the 0.75 brake**, hunger exists but only as the leading edge of an extinction.
  Observing depletion and recovery there means observing a world on its way out, and a resource that
  never recovers because the population that was eating it is gone is not a demonstration of recovery.

**Resource recovery is therefore not verifiable in either measured configuration**, and no window
drawn on the existing curve fixes that. What it needs is a configuration in which **hunger is chronic
without being fatal** — a population that stays under pressure and does not crash. Whether such a
configuration exists in this model is an open question, and finding out means moving an ecology value,
which is the deferred brake experiment with predeclared signs
(`docs/brake-0.75-provisional-status-2026-09-03.md`). It is not decided here and no value is proposed.

**The consequence for the gate: P4a cannot close until that experiment settles.** Five of six items
can be verified now, and the sixth is the one the whole cap-and-brake change was made to deliver. A
gate reported as met on five of six would record as satisfied the exact requirement that is still
outstanding.

## The mechanism claim was checked, and it holds across the whole brake axis

*(Added 2026-09-05, second amendment. Evidence: `docs/experiments/p6-regime-b-triage-2026-09-05.md`.)*

The ruling above rests on one mechanism sentence: hunger arrives **with** the collapse because the
brake produces hunger by letting the population outgrow its food, which is also how it produces the
collapse. That was measured in one configuration, `Y` at brake 0.75, so it was open whether some other
brake strength separates the two.

**It was checked at eight strengths, at full seed count, and they do not separate.** The Regime B
triage ran cap 500 at brake 1.4, 1.5, 1.6, 3.0, 4.0 and 5.0, and cap 250 at brake 1.0, all at 24,000
ticks with the death mix sampled at nine points, alongside the recorded curves at 0.75 and 1.0. It was
run twice: a six-seed screen and then a **24-seed** re-run, the seed count at which the persistence
criterion can return PERSISTENT. **No cell read differently at 24 seeds.** In **every** cell:

- starvation is at or below 5% of deaths in every interval up to and including the one in which the
  population peaks;
- it is **47-76% of deaths in the very next interval**;
- worlds begin disappearing in that same interval or the one after it.

At 24 seeds the gap between hunger and world loss is **0 samples in four cells, 1 in six and 2 in one**
- never more than 5,333 ticks out of 24,000 - and **hunger arrives at the top of the boom in every
cell**, coinciding exactly with the population peak in seven of eleven and differing by one sample in
either direction in the rest, which is what a 2,666-tick sampling grid looks like rather than a
mechanism. The predeclared falsifier (a cell with materially non-zero starvation in every interval
including the last third, while keeping its worlds) was met by none of them at either seed count. Two
cells predicted before the screen to survive it did not.

**What this settles.** The deferred brake experiment is **not a matter of picking a value**. Hunger
and collapse in this model are not two regimes a strength selects between; they are the same event
seen a few thousand ticks apart, and the strength moves at most where the event falls. Within the one
clean single-variable axis available - brake 1.4, 1.5, 1.6 under otherwise identical conditions - it
does not move the event by even one trajectory sample.

**What it does not settle**, and the note says so rather than overclaiming:

- **It is not a proof that no brake value can work.** Eight strengths at 24 seeds each, spanning the
  ends and the middle, is a strong prior and not a theorem. What has moved is the burden: a proposal
  that some untested strength gives chronic non-fatal hunger now has to say why, given that the ends
  and the middle behave identically.
- **It does not say what would work.** "There is no negative feedback except death" is a reading of
  the death mix and the trajectory, not a code audit. A mechanism that regulates before starvation -
  rather than a strength that postpones it - is the shape of the open question, and identifying one is
  separate, explicitly biological work with its own spec.

**No brake value is proposed here or anywhere in the audit, and no configuration value was moved to
obtain any of this.** The consequence for the gate is unchanged: **P4a still cannot close**, and it
now cannot close on a brake value either.

## What this note does not do

- It does not change the P4a verdict in the frozen spec, which stands at **UNVERIFIED**.
- It does not change `Y`, its cap, or its brake.
- It does not propose an ecology value of any kind.
