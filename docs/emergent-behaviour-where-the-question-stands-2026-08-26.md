# Emergent behaviour: where the question actually stands after 2026-08-26

**Synthesis of one day's measurements**, written because the answer is now spread across nine
documents and a reader starting from `emergent-behaviour-constraints-2026-08-24.md` would take its §1
as the state of the art. It is not. This document supersedes nothing and re-measures nothing; it says
what is established, what was withdrawn, and what is still open.

The question: give organisms primitives and senses, do **not** program hunt / flee / shelter / cache,
and see whether selection discovers strategies that were not encoded — **measurably**.

## What the 2026-08-24 position was

**"Enrich the world before enriching the brain."** Four supports: nothing kills a creature (96.9% old
age, predation zero); one threshold carries nearly all selection; the foraging decision carries no
exploitable information; and world enrichment is the cheaper step.

## What survived a day of measurement

**One of the four.** The rest are withdrawn, qualified, or inverted, and the source document carries a
banner saying so.

| support | status |
|---|---|
| "Almost nothing kills a creature" | **Withdrawn — it is a configuration.** Starvation runs **49.6% to 0.0%** of deaths on two values. `p6-starvation-is-a-dial-2026-08-26.md` |
| "The foraging decision carries no information" | **Measured false.** The quality channel moves population by a third and occupancy by a fifth, 0 of 240 hashes matching. `p6-patch-quality-is-not-a-free-parameter-2026-08-26.md` |
| "Enriching the world is cheaper" | **Was argued from a failure that was a bug.** The predator profile set six of twenty-four genes. `p6-predation-never-failed-its-founders-cannot-breed-2026-08-26.md` |
| "One threshold carries the selection" | **Stands, and grew.** The mate gate is *also* the density brake. `p6-the-gate-is-a-survival-mechanism-2026-08-26.md` |

## What is now established that was not

1. **A survivable world with real mortality pressure exists** — brake 1.5, regen 2.0, cap 500: 30 of
   30 worlds, 16.2% of deaths starvation, and **nine surrounding cells behave the same**, so it is a
   plateau.
2. **A survivable predator-prey world exists** — the same cell at gate 0.45: 24–26 of 30, 146 births
   per run, predation firing. **The first this project has had.**
3. **Predation selects.** `defense` **+4.97 to +10.97 across six cells**, `attack` negative in all
   six, control flat. Both health arms agree.
4. **The two flags that could never be adjudicated are live** — kin recognition off costs 29% of the
   population.

**None of that required a new controller, a new mechanism, or a new gene.** It required configuration
and one genuine bug fix. **That is the strongest thing this day says**, and it cuts against the
world-first thesis while conceding its underlying observation: the *default* configuration really is
a world where almost nothing selects.

## What this does not establish

- **No behaviour was invented.** Everything measured is selection on genes that already exist, doing
  what they were written to do. **The original question is untouched** — nothing here shows a strategy
  emerging that nobody encoded, and nothing here is evidence that one would.
- **All of it lives outside the recorded corpora.** Gate 0.45, cap 500, regen 2.0 is a configuration
  no committed result uses. **Nothing here is comparable to the eleven corpora**, and the nine plant
  corpora other than contest and join are still measured with the population pinned.
- **Three of the named target behaviours remain impossible.** Caching, territory and shelter need
  place memory, which is deliberately inert and pinned inert. **The roadmap and the goal still
  contradict each other**, exactly as §2 of the source document said — and that is now the *oldest*
  unaddressed item in this thread.
- **Two of six combat genes cannot be measured at all.** No statistic exposes `maneuverability` or
  `fear`.

## What I got wrong today, since the method is the point

- Claimed a flag was off in the sweeps when both sweeps pass it **true** — read a declaration instead
  of the call sites, the exact error being criticised.
- Claimed mortality pressure opened a fertility channel from a **two-point** comparison; the third
  point was non-monotone and it was withdrawn before publication.
- Claimed the predator founders' lifespan floor caused zero births; **fixing it changed nothing** and
  the gate was the cause.
- Proposed that predation selects through sterilisation; measured **1.8%** below the health gate
  against a predicted majority. **Refuted, and the mortality route stands by elimination rather than
  by direct measurement** — which is weaker and is labelled as such.

**Four wrong turns, all caught by running one more arm.** The pattern is the same every time: a
mechanism that explained the data was adopted before an arm that could have contradicted it was run.

## The honest recommendation

**Neither "enrich the world" nor "enrich the brain" is the next step.** The next step is that the
world now *has* pressure and *does* select, and **nothing has yet asked whether a richer controller
does better in it** — which is a paired-arm question the existing harness can answer, in the cell that
now exists. That comparison has never been run, in either direction, and until it is, the ordering
argument stays what it was on 2026-08-24: a hypothesis with a story attached.
