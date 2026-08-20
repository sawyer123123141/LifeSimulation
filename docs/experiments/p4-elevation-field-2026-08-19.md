# Elevation Field, and the Two Conditions It Needs to Matter — 2026-08-19

Handoff item 2. Adds a ridged-multifractal elevation channel and a lapse rate coupling it to
temperature, behind `SimulationConfig.ElevationFieldEnabled` defaulting false.

Landed with expectations already set by
`p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md`: this is terrain and P6
groundwork, and it was **not** expected to make any plant gene selectable. It does not.

## What was built

- **`EnvironmentNoise.RidgedFbm`** — each octave folded as `1 - |2n - 1|` and squared, so peaks
  become creases rather than domes, with each octave weighted by the previous so detail
  accumulates on ground that is already high. That is what produces connected chains instead of
  scattered hills.
- **`EnvironmentSample.Elevation`** — 0..1, zero unless the flag is set.
- **Lapse rate** — `temperature = max(.02, temperature - .45 * elevation)`, floored above zero
  because temperature 0 stops growth outright and a dead crest is less interesting than a cold
  one.

**Elevation deliberately has no growth channel of its own.** It acts only through temperature,
which already limits growth. A fourth channel that plants had to adapt to would have shipped as
another tax on genes that demonstrably cannot pay one.

Four tests: flag-off leaves elevation at zero *and* moisture and fertility bit-identical;
elevation spans a real range and stays in bounds; high ground is colder than the same ground
unraised; and ridged noise is measurably more right-skewed and higher-variance than plain fBm.
393/393 green.

## The field is a real gradient

Measured at plant-reachable positions over seeds 42-71:

| quantity | value |
|---|---:|
| mean elevation | 0.3948 |
| mean temperature, elevation off | 0.6593 |
| mean temperature, elevation on | 0.4816 |
| mean temperature drop | 0.1777 |
| max temperature drop | 0.4101 |

And it does not disturb the plant calibration: 0/30 extinct, 0/30 plantless, mean population
47.9, 14.3 plant generations — **identical with the flag on and off**.

## Two conditions, and neither alone is enough

That identical survival table is the signature of a dead flag in this repo, not of a small
effect, so it was checked against the state hash rather than trusted. Seed 42, 2,000 ticks:

| population cap | fertility adaptation | elevation |
|---:|---|---|
| 48 | off | **inert** (bit-identical) |
| 48 | on | **inert** |
| 1000 | off | **inert** |
| **1000** | **on** | **live** (hash diverges) |

Live under `CreateFullEcosystemDefaults` too, confirmed directly on seeds 42-44 rather than
inferred from the passing liveness test.

Both conditions are necessary and neither is sufficient, and each corresponds to one of today's
findings:

1. **The population cap**, through grazing pressure. At cap 48 the patches sit near capacity, and
   growth is multiplied by `(1 - Biomass/Capacity)`, so a change to the growth *limit* has almost
   nothing to act on. At cap 1000 heavy grazing pulls patches down and the limit starts to matter.
2. **The fertility adaptation term.** Fertility binds the `Min` at 82-90% of plant-reachable
   positions, so lowering temperature changes nothing while fertility is still the smallest
   channel. `NutrientUptake` lifts fertility out of contention, and only then can a colder crest
   reach the growth limit.

So the fertility term added earlier today — which barely moved its own gene — turns out to be a
**precondition for any other environment channel mattering at all**. That is a better
justification for it than the one it was built on.

## Honest scoping

**Elevation is inert under the standard P4 plant configuration** (cap 48, procedural fields,
temperature adaptation), with or without fertility adaptation. Anyone enabling it there and
expecting an effect will not get one, and the survival aggregates will look reassuringly
unchanged while nothing at all is happening.

Two hypotheses were wrong on the way here and are recorded because the wrong turns are the
useful part: first that the lapse rate was absorbed by the temperature adaptation term (it is
not — that arm is inert either way), then that the fertility term alone would unlock it (it does
not — cap 48 stays inert with fertility adaptation on). Only the two-factor test separated them.

## Not done

- **Rain shadow.** The design sketch pairs the lapse rate with a moisture coupling. It needs a
  wind-direction convention, which is a design choice rather than a mechanical one, so it is left
  undone rather than invented.
- **Play-mode overlay.** `H` cycles temperature -> biome -> off; an elevation mode belongs there.
  Not added, because `Prototype1Presenter` is Unity presentation code that cannot be verified
  headlessly, and the standing rule here is that a green `dotnet test` does not prove Unity
  compiles.
