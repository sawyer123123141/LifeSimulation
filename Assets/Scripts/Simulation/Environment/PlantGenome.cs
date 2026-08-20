using System;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Environment
{
    public readonly struct PlantGenome
    {
        /// <param name="nutrientUptake">
        /// Answers the fertility channel, which no other gene can reach. Defaults to .5f so the
        /// many existing eight-argument constructions keep compiling at a neutral value - which
        /// is the same shape that silently dropped <c>persistence</c> from the animal genome, so
        /// <see cref="CloneMutated"/>, <see cref="ToTraits"/>, <see cref="FromTraits"/> and the
        /// state hash all pass it explicitly and a transmission test fails if one stops.
        /// </param>
        public PlantGenome(float growth, float seedInvestment, float waterEfficiency, float nutrition, float defense, float dispersal, float moistureTolerance, float temperatureTolerance, float nutrientUptake = .5f)
        {
            Growth = Clamp01(growth);
            SeedInvestment = Clamp01(seedInvestment);
            WaterEfficiency = Clamp01(waterEfficiency);
            Nutrition = Clamp01(nutrition);
            Defense = Clamp01(defense);
            Dispersal = Clamp01(dispersal);
            MoistureTolerance = Clamp01(moistureTolerance);
            TemperatureTolerance = Clamp01(temperatureTolerance);
            NutrientUptake = Clamp01(nutrientUptake);
        }

        public static PlantGenome Neutral => new PlantGenome(.5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f);
        public float Growth { get; }
        public float SeedInvestment { get; }
        public float WaterEfficiency { get; }
        public float Nutrition { get; }
        public float Defense { get; }
        public float Dispersal { get; }
        public float MoistureTolerance { get; }
        public float TemperatureTolerance { get; }

        /// <summary>
        /// Adaptation to soil fertility. Fertility binds the growth limit at 82-90% of
        /// plant-reachable positions and was the one channel with no genome modulation, which is
        /// why neither tolerance gene could be selected on - see
        /// docs/experiments/p4-fertility-binds-the-growth-limit-2026-08-19.md.
        /// </summary>
        public float NutrientUptake { get; }

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
                Mutate(parent.TemperatureTolerance, worldSeed, ordinal, 7, mutationStandardDeviation),
                Mutate(parent.NutrientUptake, worldSeed, ordinal, 8, mutationStandardDeviation));
        }

        private static float Mutate(float value, int worldSeed, long ordinal, int trait, float standardDeviation)
        {
            return value + (DeterministicRandom.Gaussian(worldSeed, RandomDomain.PlantMutation, ordinal, trait, 0, 0) * standardDeviation);
        }

        /// <summary>Number of heritable plant traits. Keep in step with the constructor, <see cref="ToTraits"/> and <see cref="CloneMutated"/>.</summary>
        public const int TraitCount = 9;

        private static readonly string[] TraitNames =
        {
            nameof(Growth), nameof(SeedInvestment), nameof(WaterEfficiency), nameof(Nutrition),
            nameof(Defense), nameof(Dispersal), nameof(MoistureTolerance), nameof(TemperatureTolerance),
            nameof(NutrientUptake),
        };

        /// <summary>Trait name for an index. Ordering matches the mutation trait indices in <see cref="CloneMutated"/>.</summary>
        public static string TraitName(int index)
        {
            if ((uint)index >= (uint)TraitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return TraitNames[index];
        }

        /// <summary>
        /// All traits in constructor order. Single enumeration of the plant genome, so a trait
        /// missing here fails the round-trip test rather than silently taking a default.
        /// </summary>
        public float[] ToTraits()
        {
            return new[]
            {
                Growth, SeedInvestment, WaterEfficiency, Nutrition,
                Defense, Dispersal, MoistureTolerance, TemperatureTolerance, NutrientUptake,
            };
        }

        /// <summary>Rebuild a plant genome from <see cref="ToTraits"/> output.</summary>
        public static PlantGenome FromTraits(float[] traits)
        {
            if (traits == null)
            {
                throw new ArgumentNullException(nameof(traits));
            }

            if (traits.Length != TraitCount)
            {
                throw new ArgumentException($"Expected {TraitCount} traits, got {traits.Length}.", nameof(traits));
            }

            return new PlantGenome(
                traits[0], traits[1], traits[2], traits[3],
                traits[4], traits[5], traits[6], traits[7], traits[8]);
        }

        public float GetTrait(int index)
        {
            if ((uint)index >= (uint)TraitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return ToTraits()[index];
        }

        /// <summary>Copy with one trait replaced. Used by the plant gene liveness perturbation harness.</summary>
        public PlantGenome WithTrait(int index, float value)
        {
            if ((uint)index >= (uint)TraitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            float[] traits = ToTraits();
            traits[index] = value;
            return FromTraits(traits);
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

        /// <param name="fertilityAdaptationEnabled">
        /// Gates the <c>NutrientUptake</c> growth charge as well as its benefit in
        /// <see cref="PlantGrowthSystem"/>. Both halves move together so that flag-off is
        /// byte-identical to the world before the gene existed: an unconditional cost here would
        /// change every plant run the moment the gene was added, which is exactly what the
        /// flag-off-is-identical rule exists to prevent.
        /// </param>
        public static PlantPhenotype FromGenome(PlantGenome genome, bool fertilityAdaptationEnabled = false)
        {
            float growth = .55f + (.90f * genome.Growth) - (.18f * genome.Nutrition) - (.15f * genome.Defense) - (.08f * genome.WaterEfficiency) - (.10f * genome.MoistureTolerance) - (.10f * genome.TemperatureTolerance);
            if (fertilityAdaptationEnabled)
            {
                growth -= .10f * genome.NutrientUptake;
            }

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
