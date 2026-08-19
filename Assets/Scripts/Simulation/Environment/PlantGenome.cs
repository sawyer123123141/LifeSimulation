using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    public readonly struct PlantGenome
    {
        public PlantGenome(float growth, float seedInvestment, float waterEfficiency, float nutrition, float defense, float dispersal, float moistureTolerance, float temperatureTolerance)
        {
            Growth = Clamp01(growth);
            SeedInvestment = Clamp01(seedInvestment);
            WaterEfficiency = Clamp01(waterEfficiency);
            Nutrition = Clamp01(nutrition);
            Defense = Clamp01(defense);
            Dispersal = Clamp01(dispersal);
            MoistureTolerance = Clamp01(moistureTolerance);
            TemperatureTolerance = Clamp01(temperatureTolerance);
        }

        public static PlantGenome Neutral => new PlantGenome(.5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f);
        public float Growth { get; }
        public float SeedInvestment { get; }
        public float WaterEfficiency { get; }
        public float Nutrition { get; }
        public float Defense { get; }
        public float Dispersal { get; }
        public float MoistureTolerance { get; }
        public float TemperatureTolerance { get; }

        public static PlantGenome CloneMutated(PlantGenome parent, int worldSeed, long ordinal, float mutationStandardDeviation)
        {
            return new PlantGenome(
                Mutate(parent.Growth, worldSeed, ordinal, 0, mutationStandardDeviation),
                Mutate(parent.SeedInvestment, worldSeed, ordinal, 1, mutationStandardDeviation),
                Mutate(parent.WaterEfficiency, worldSeed, ordinal, 2, mutationStandardDeviation),
                Mutate(parent.Nutrition, worldSeed, ordinal, 3, mutationStandardDeviation),
                Mutate(parent.Defense, worldSeed, ordinal, 4, mutationStandardDeviation),
                Mutate(parent.Dispersal, worldSeed, ordinal, 5, mutationStandardDeviation),
                Mutate(parent.MoistureTolerance, worldSeed, ordinal, 6, mutationStandardDeviation),
                Mutate(parent.TemperatureTolerance, worldSeed, ordinal, 7, mutationStandardDeviation));
        }

        private static float Mutate(float value, int worldSeed, long ordinal, int trait, float standardDeviation)
        {
            return value + (DeterministicRandom.Gaussian(worldSeed, RandomDomain.PlantMutation, ordinal, trait, 0, 0) * standardDeviation);
        }

        private static float Clamp01(float value) { return Math.Max(0f, Math.Min(1f, value)); }
    }

    public readonly struct PlantPhenotype
    {
        /// <summary>
        /// Reference lifespan in seconds before the Growth-gene tradeoff is applied.
        /// Derived empirically on 2026-08-17 against the six-site
        /// ConsumerDefenseCalibrationModerate scenario, seeds 42-71, 12,000 ticks, with
        /// mortality and site competition enabled. Halving downward from 90:
        ///   90    -> 0/30 extinct, 12 plant generations
        ///   45    -> 1/30 extinct, 0 plant generations (plants collapse in some seeds)
        ///   22.5  -> 30/30 extinct
        ///   11.25 -> 30/30 extinct
        /// 90 is the smallest value satisfying both calibration constraints (>=8 plant
        /// generations, zero animal extinctions). Shorter lifespans kill the plant layer
        /// outright rather than merely accelerating turnover.
        /// </summary>
        public const float BaseLifespanSeconds = 90f;

        public PlantPhenotype(float growthRateMultiplier, float nutritionMultiplier, float defense, float dispersalRange, float seedInvestmentFraction, float lifespanSeconds)
        {
            GrowthRateMultiplier = growthRateMultiplier;
            NutritionMultiplier = nutritionMultiplier;
            Defense = defense;
            DispersalRange = dispersalRange;
            SeedInvestmentFraction = seedInvestmentFraction;
            LifespanSeconds = lifespanSeconds;
        }

        public float GrowthRateMultiplier { get; }
        public float NutritionMultiplier { get; }
        public float Defense { get; }
        public float DispersalRange { get; }
        public float SeedInvestmentFraction { get; }
        public float LifespanSeconds { get; }

        public static PlantPhenotype FromGenome(PlantGenome genome)
        {
            float growth = .55f + (.90f * genome.Growth) - (.18f * genome.Nutrition) - (.15f * genome.Defense) - (.08f * genome.WaterEfficiency) - (.10f * genome.MoistureTolerance) - (.10f * genome.TemperatureTolerance);
            return new PlantPhenotype(
                Math.Max(.1f, growth),
                .55f + (.90f * genome.Nutrition) - (.25f * genome.Defense),
                genome.Defense,
                4f + (20f * genome.Dispersal),
                .02f + (.10f * genome.SeedInvestment),
                BaseLifespanSeconds * (1.5f - (.75f * genome.Growth)));
        }
    }
}
