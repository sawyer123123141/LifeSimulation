# P4 Consumer-Defense Calibration — 2026-08-13

## Purpose

Find a plant-defense condition that permits enough animal births and survivor generations to test whether food-efficiency evolves under plant defense.

## Controlled setup

- Prototype 4 defaults, Intent Utility V1, 12 founders, 48-population cap, 12,000 ticks.
- Seeds: 42–46.
- Food and water are co-located at each of two patches; all founders start on one patch.
- Both conditions have identical resource positions, quantities, renewal, founder genomes, and plant traits.
- Control plant defense: `0.00`; treatment plant defense: `0.30`.

The co-located layout intentionally removes travel between food and water as a confound. It is an experiment-only control and does not change the playable scenario.

## Result

| Condition | Births by seed (42–46) | Final populations | Outcome |
| --- | --- | --- | --- |
| Control | 0, 3, 0, 3, 0 | 0, 0, 0, 0, 0 | Extinction in every seed |
| Moderate defense | 1, 4, 1, 4, 0 | 0, 0, 0, 0, 0 | Extinction in every seed |

The moderate-defense condition does create births, but neither condition sustains an animal lineage. Final food-efficiency is consequently unavailable because every final population is empty.

## Decision

Do not interpret or tune this as plant-defense selection. The next prerequisite is a reliable baseline animal survival/reproduction loop under abundant, co-located food and water. Once that baseline has repeatable surviving generations, re-run this exact paired calibration before adding plant competition or other ecological pressure.
