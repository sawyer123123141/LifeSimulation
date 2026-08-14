# P4 Plant Defense Control — 2026-08-13

## Purpose

Verify that plant defense changes consumer food return in the plant-cohort simulation. This is a control experiment, not an evolutionary-selection claim.

## Configuration

- Prototype 4 defaults; four founders; 4,000 ticks.
- Seeds: 42, 43, and 44.
- Two otherwise identical plant-backed food patches and two water patches.
- Undefended condition: plant defense gene `0.00`.
- Defended condition: plant defense gene `0.85`.
- All other plant genes were fixed; the scenario deliberately has no empty plant sites, so plant reproduction cannot confound the consumer result.

## Results

| Condition | Final populations (42, 43, 44) | Births | Deaths | Mean final food-efficiency |
| --- | --- | ---: | ---: | ---: |
| Undefended | 0, 1, 0 | 0 | 11 | 0.3513 in the surviving seed only |
| Defended | 0, 0, 0 | 0 | 12 | n/a |

Plant biomass remained positive in every run. Defense therefore changed the effective reward from feeding strongly enough to worsen consumer survival, including the one seed that retained a survivor without defense.

## Interpretation and next gate

The result validates the defense-to-digestion connection, but no run produced an animal birth. It cannot show heritable consumer adaptation because there was no next generation to select.

The next experiment must use a moderate defended condition and a demographic setup that reliably yields multiple births in both paired conditions. It should report per-generation food-efficiency distributions and survival, while holding all non-defense plant traits and resource layout fixed.
