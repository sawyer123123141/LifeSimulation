# "Nothing kills a creature" is two configuration values, and there is a setting where starvation and survival coexist

> **FOLLOWED UP 2026-08-26** — `p6-the-pressured-cell-is-a-plateau-2026-08-26.md`. The brake-1.5 cell
> was found by moving **one** axis, so it was checked for being a knife-edge: **it is not.** All nine
> cells of brake 1.4–1.6 x regen 1.75–2.25 survive 29–30 of 30 with starvation between 4.2% and 27.2%.
> **The cap is the one cliff, and it is a floor:** cap 250 takes starvation to 0.0%, while cap 500 and
> cap 1000 are the same ecology (16.2% both, population self-limiting near 300). **Above 500 the cap
> stops mattering.**
>
> The sterility channel named below is a **confound for every selection claim taken in these cells**,
> not just a finding: health does not regenerate unless `healthRecoveryEnabled` is on, so drift is
> conditioned on a breeding subpopulation health has already selected, and the selected fraction
> **scales with the pressure** (27.1% down to 8.9% across the grid). With `--health-recovery` on, the
> same cell keeps its pressure (13.2% starvation, 30/30) and sterility falls to 15.2% and becomes
> recoverable rather than permanent.

**2026-08-26.** `tools/CreatureSweep --deaths 30 <cap> [--regen=2.0] [--brake=X]`, 12,000 ticks,
consumer-defense calibration layout. Ten cells, all from one build in one session. Console artefact:
`p6-death-mix-cap-by-brake-2026-08-26.txt`.

Answers the confound named against the naive version of this test: **a death mix taken at high cap
with the brake on cannot separate "the world is rich" from "the brake suppressed starvation", because
suppressing overshoot-and-starve is exactly what the brake is for.** Both arms are therefore measured,
and the low-cap baseline is re-measured rather than quoted.

## Instrument check first

Cap 100, brake off reproduces the recorded baseline **exactly**: 5,619 deaths, 96.9% age, **8
starvations**, 30 of 30 surviving, population 98.2 pinned with sd 8.35. Nothing has drifted since
`p6-nothing-starves-2026-08-24.md`, so the cells below are comparable to the record.

## The scenario correction that had to come first

The recorded "35–64% starvation at cap 500" was measured at **2.0x regeneration**, not on the standard
layout. Run at cap 500 on the standard layout instead, the world does not starve — **it dies**: 29 of
30 extinct, 78 deaths in the single survivor. **Raising the cap alone does not buy starvation
pressure; it buys collapse.** Every cell below that is meant to speak to the recorded figure therefore
carries `--regen=2.0`.

## The dial: 2.0x regeneration, cap 500, brake swept

| brake | surviving | **starvation** | age | mean energy | below health gate | population mean / sd |
|---:|---:|---:|---:|---:|---:|---:|
| **off** | 6 / 30 | **49.6%** | 47.4% | 0.257 | 63.6% | 222.3 / 244.2 |
| 0.5 | 18 / 30 | **49.2%** | 50.0% | 0.112 | 71.9% | 139.6 / 171.7 |
| 1.0 | 28 / 30 | **33.6%** | 65.4% | 0.353 | 43.3% | 262.4 / 167.9 |
| **1.5** | **30 / 30** | **16.2%** | 82.7% | 0.583 | 20.9% | 299.4 / 146.8 |
| 3.0 | 29 / 30 | **0.0%** | 98.9% | 0.790 | 5.3% | 100.0 / 69.8 |

**Starvation runs from 49.6% of deaths to 0.0% under one ecology, moved by one configuration value.**

**The confound was real and is now separated.** Brake-on at cap 500 gives 0.0% starvation — and the
same scenario with the brake off starves half its deaths. **Zero starvation is the brake's doing, not
the world's richness.** A single brake-on cell would have licensed the opposite reading.

**And the answer is not binary.** At **strength 1.5, every world survives and 16.2% of deaths are
starvation**, with mean energy 0.583 and a population of 299 with sd 147 under a cap of 500. **A
survivable world with real mortality pressure exists in the current model**, without predation and
without a new mechanism. That cell is the interesting one and it was not visible from either endpoint.

## What this settles and what it does not

- **"Almost nothing kills a creature" is a property of the default configuration, decisively.** It is
  cap 100 plus no brake. Two values move starvation to a third or a half of all deaths.
- **It does not show the default is wrong.** It shows the recorded corpora sit at one end of a dial
  nobody had swept, and that anything reasoning from "no mortality pressure exists" is reasoning about
  a setting.
- **Health is the hidden channel.** "Below the health gate" — permanently unable to seek a mate, since
  health never regenerates — runs **71.9% to 5.3%** across the same sweep. In the starving cells most
  of the *living* population is sterile. Mortality and fertility pressure move together here, and no
  recorded experiment separates them.

## Two things measured on the way that should not be re-derived

- **Standard layout, brake 3.0: cap 250 and cap 500 are the same run** — identical death totals,
  identical population distribution (mean 77.5, max 183, sd 53.92). The brake holds the population
  below 183, so the cap stops binding above it. **A cap only matters while it binds**, and above that
  point "cap 500" names nothing.
- **Every figure is conditioned on surviving runs**, and the survivor count varies from 6 to 30 across
  the table. The brake-off cells describe the few worlds that lived through a collapse, which is why
  brake 0.5 shows *lower* mean energy than brake 0: different denominators, not a non-monotone
  ecology. **The starvation shares are comparable; the energy means across arms are not.**
