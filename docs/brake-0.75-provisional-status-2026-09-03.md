# The 0.75 brake's PROVISIONAL status now has evidence against one of its criteria

**Date:** 2026-09-03
**Status:** a status note. **This is not an edit to the frozen spec** — it records evidence bearing on
an entry that already exists there, and the entry itself is the user's to move.
**Evidence:** `docs/experiments/p6-the-shipped-world-does-not-persist-2026-09-03.md`.

## What section 6 already says

`docs/superpowers/specs/2026-08-30-what-finished-means-design.md`, section 6, marks the density brake
**PROVISIONAL**. It was shipped on death-mix grounds — starvation from 0.0% to 5.4% of deaths — and
that section demotes death-mix reasoning, so the change was left standing but no longer
self-justifying. The spec names what would settle it:

> matched brake-on / brake-off experiments decide it, on variance in reproductive fitness,
> trait-to-fitness relationships, **population stability**, starvation and survival as explanatory
> variables never as targets, and effective-population-size diagnostics where relevant.

Four of those five criteria are untouched by anything measured since.

## What has changed

**The population-stability criterion now has evidence.** `Y` at cap 500 with the brake at strength
0.75 is extinct in **24 of 24 worlds at 36,000 ticks**, while the same layout at cap 96 is level from
12,000 to 36,000 in two independent arms. The 12,000-tick numbers that the original decision rested on
reproduce exactly; the run simply continues past them.

Two things that follow, and one that does not.

**It follows that the shipped setting has not met the population-stability criterion.** Not that it
failed a test it was given — no such test was ever run — but that the criterion now has a measurement
against it where before it had none.

**It follows that the evidence the brake strength was chosen from cannot bear on this criterion at
all.** The five survival counts in `p6-y-is-food-limited-2026-08-30.md` — 16 / 17 / 20 / 20 / 16 of 24
across no brake / 0.5 / 0.75 / 1.0 / 1.5 — are counts at tick 12,000, which the collapse curve now
places at the peak of a boom. A count taken before any world has had the chance to die cannot
distinguish stability from pre-collapse, whichever value it favours. The **starvation** dial from the
same table (43.1 / 15.9 / 5.4 / 0.7 / 0.3) is a within-run composition, reproduces, and is not in
question.

**It does not follow that some other brake value is correct.** Nothing here measured one, and nothing
here should be read as pointing at one.

## What deciding it actually requires

**A separate, declared experiment with predeclared signs — not a value picked off the back of a
collapse finding.** The reasons are specific rather than procedural:

1. **Changing the brake is a biological change.** It invalidates every baseline measured before it.
   That is why the measurement-validity milestone forbade it inside a measurement task and why this
   audit did not touch it.
2. **The obvious inference is the one this project has been burned by.** A cell collapses at 0.75 and
   at 1.0, so a stronger brake suggests itself — and `p6-graded-fertility-is-scenario-specific-2026-08-24.md`
   already records strength 3 stabilising one ecology and collapsing another to a tenth of its
   population with a third of worlds dying. Brake strength does not transfer between scenarios. A
   value chosen by extrapolating from two collapses would be chosen by exactly the reasoning that
   document refutes.
3. **The spec asks for four criteria this note says nothing about.** Reproductive-fitness variance,
   trait-to-fitness relationships, starvation and survival as explanatory variables, and Ne
   diagnostics. Deciding the brake on stability alone would answer one fifth of the question the spec
   asked and would read as though it had answered all of it.
4. **A survival count at a fixed horizon is now known to be the wrong statistic** for this family of
   questions. Whatever experiment decides the brake must judge on a trajectory against a predeclared
   persistence criterion — the one in
   `docs/experiments/cell-family-persistence-ledger.md` — or it will reproduce the error it is meant
   to correct.

**No value is proposed here, and none was searched for.** The cap-96 comparator arms in the evidence
document are an attribution control: they exist to show the collapse belongs to the configuration
rather than to the layout. That they are also the pre-2026-08-30 setting does not make them a
recommendation.

## Amendment, 2026-09-05: the brake axis has now been measured, and point 2 above was right

`docs/experiments/p6-regime-b-triage-2026-09-05.md` ran cap 500 at brake **1.4, 1.5, 1.6, 3.0, 4.0 and
5.0** and cap 250 at brake 1.0, all at 24,000 ticks - first as a six-seed screen and then at **24
seeds**, the count at which the persistence criterion can return PERSISTENT. **All of them collapse at
both seed counts**, and so does every other Regime B cell in the ledger. The stronger-brake inference
that point 2 named as the tempting error is now measured and is wrong on its own terms: brake 4.0, the
value `p6-the-clean-controller-comparison-2026-08-26.md` searched the axis for and recorded at **0 of
60 extinct** at 12,000 ticks, keeps **1 world of 24 at 24,000**. The best any strength manages is
brake 5.0 at **11 of 24**, nine worlds short of the threshold and failing the trend clause as well.

Three consequences for this note.

- **The population-stability criterion has evidence against the whole axis, not just against 0.75.**
  Nothing measured in this project persists in Regime B at any brake strength.
- **Point 2 stands and is sharpened.** It warned against extrapolating a value from two collapses.
  The extrapolation would have landed on a stronger brake, and stronger brakes collapse too.
- **Point 4 is now the operative one.** Whatever experiment decides the brake must judge on a
  trajectory against the predeclared criterion; a survival count at a fixed horizon has now been shown
  to mislead at 12,000 ticks for eight distinct strengths, at 24 seeds each.

**Still no value is proposed, and none was searched for.** The triage moved no configuration value:
every strength it ran is one a recorded document had already run at 12,000 ticks.

## Status

- Section 6's PROVISIONAL entry **stands as written**. It is not upgraded, downgraded or reworded by
  this note.
- Moving it, or the section 4 gate verdicts, is the user's decision on evidence.
- The shipped scenario is unchanged.
