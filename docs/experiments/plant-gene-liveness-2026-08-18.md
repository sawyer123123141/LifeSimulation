# Plant Gene Liveness, and the Limit of Perturbation — 2026-08-18

Extends the perturbation harnesses to `PlantGenome`. Motivated by a gap: the animal-side
`GeneLivenessAnalysis` and `FlagLivenessAnalysis` do not cover plant genes at all, and plant
`TemperatureTolerance` looked like a pure-cost trait on a code read.

## Result: all eight plant genes reach behavior

Seed 42, `ConsumerDefenseCalibrationModerate`, both `CreatePrototype4Defaults` and
`CreateFullEcosystemDefaults`:

| idx | trait | reaches behavior | diverged at tick |
| ---: | --- | --- | ---: |
| 0 | Growth | yes | 40 |
| 1 | SeedInvestment | yes | 20 |
| 2 | WaterEfficiency | yes | 40 |
| 3 | Nutrition | yes | 40 |
| 4 | Defense | yes | 40 |
| 5 | Dispersal | yes | 20 |
| 6 | MoistureTolerance | yes | 40 |
| 7 | **TemperatureTolerance** | **yes** | 40 |

**The prediction that `TemperatureTolerance` would read dead was wrong**, and the reason is the
important part.

## A pure-cost gene passes a liveness test

`PlantPhenotype.FromGenome` charges `-.10f * genome.TemperatureTolerance` against growth. Perturbing
the gene therefore changes growth, changes biomass, and moves the behavior hash. It "reaches
behavior" — while being unable to benefit its carrier under any environment.

**So reaching behavior is not the same as having a fitness trade-off.** This is a genuine limit of
the perturbation method and should be stated wherever the harnesses are described: they detect
*influence*, not *reward*. A trait can be live and still be unselectable-for.

## The specific gap, characterized

`PlantGrowthSystem.Step`:

```csharp
float moistureAdaptation = sample.Moisture <= 0f
    ? 0f
    : Math.Min(1f, sample.Moisture + ((1f - sample.Moisture) * (.7f * Genome.WaterEfficiency + .3f * Genome.MoistureTolerance)));
float limit = Math.Max(0f, Math.Min(moistureAdaptation, Math.Min(sample.Fertility, sample.Temperature)));
```

- **Moisture** has an adaptation term, so `MoistureTolerance` and `WaterEfficiency` buy real growth
  where moisture is scarce. This is what a trait with a trade-off looks like.
- **Temperature and fertility** enter as *raw limits with no genome modulation*. Nothing a plant
  carries can improve its position against them.

Additionally, `EnvironmentField.Sample` returns `Fertility = 1` and `Temperature = 1` on **every
production path** — both `CreateMoistureGradient()` and the default constructor. So `limit` always
collapses to `moistureAdaptation`, and both channels are unused.

### Consequence for terrain work

**Richer environment fields alone will not make `TemperatureTolerance` meaningful.** Even with real
spatial temperature variation, the gene has no channel through which to help. Two changes are needed
together:

1. `EnvironmentField` returning genuinely varying temperature and fertility.
2. A `temperatureAdaptation` term in `PlantGrowthSystem` mirroring `moistureAdaptation`, so tolerance
   can be paid for.

Doing only the first would add spatial variation that plants cannot adapt to, and would make the gene
*more* costly rather than meaningful.

## Pinned by test

`PlantLivenessTests` covers the plant genome round-trip and `WithTrait` isolation, asserts all eight
genes reach behavior, and characterizes the gap directly:

- `MoistureToleranceHelpsThePlantWhenMoistureIsScarce` — the working reference pattern.
- `TemperatureToleranceCannotHelpThePlantEvenWhenTemperatureIsLimiting` — asserts higher tolerance
  produces *less* growth even at `temperature = 0.2`. **This test is expected to fail when the
  adaptation term lands**, and its comment says so; it should then be replaced by the moisture-shaped
  assertion.
- `FertilityIsPinnedAtOneOnEveryProductionPath` — records that fertility is an unused channel rather
  than letting terrain work assume it already varies.
