using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Biology
{
    /// <summary>
    /// Founders for <see cref="FounderProfile.PredationVariation"/>: a <b>neutral</b> genome with the
    /// combat family varied, and nothing else varied.
    ///
    /// <para><b>Rebuilt 2026-08-26.</b> This factory previously started from
    /// <see cref="Genome.Neutral"/> and set only the six combat traits, which left <i>every other
    /// family at the constructor's default of 0</i> - not at the neutral 0.5 the name implies.
    /// <see cref="Genome.Neutral"/> itself passes six of twenty-four traits, so the zeros came from
    /// two positional constructors in a row and were invisible at both call sites.</para>
    ///
    /// <para><b>What those zeros did.</b> <c>lifespanTendency = 0</c> gives maximum age
    /// <c>90 + 180 * 0</c>, the floor, against an adult age of 20 s. <c>fertilityInvestment = 0</c>
    /// gives <c>16 - 8 * 0</c>, the longest reproduction interval. <c>temperatureTolerance = 0</c>
    /// gives a comfort band of <c>2 + 8 * 0</c>, the narrowest possible, so founders took continuous
    /// stress damage into a health value that never recovers and is one of the three mate-seeking
    /// gate conditions. Measured: <b>zero births across 90 runs in three different worlds</b>, while
    /// <see cref="PhysiologyFounderFactory"/> produced 492 births per run in the same worlds. Every
    /// predator-prey verdict on record was measured on that cohort, so none of them measured
    /// predation.</para>
    ///
    /// <para><b>Why neutral rather than varied.</b> The profile's job is to make the combat family
    /// the <i>only</i> axis that differs between founders, so an outcome is attributable to it -
    /// which is exactly what <c>PredationFounderProfileSeedsUnlabeledPredationVariation</c> asserts.
    /// Varying the other families would turn this into physiology variation plus combat variation and
    /// confound the two. The bug was never that the other traits failed to vary; it was that they
    /// were zero instead of neutral.</para>
    ///
    /// <para>See <c>docs/experiments/p6-predation-never-failed-its-founders-cannot-breed-2026-08-26.md</c>
    /// and its follow-up.</para>
    /// </summary>
    public static class PredationFounderFactory
    {
        /// <summary>The neutral value every non-combat trait takes here.</summary>
        private const float Neutral = 0.5f;

        public static Genome Create(int worldSeed, long founderOrdinal)
        {
            Genome baseline = Genome.Neutral;
            return new Genome(
                baseline.BodySize,
                baseline.MovementSpeed,
                baseline.MetabolicPace,
                baseline.VisionRange,
                baseline.WaterEfficiency,
                baseline.FoodEfficiency,
                attack: Trait(worldSeed, founderOrdinal, 0),
                defense: Trait(worldSeed, founderOrdinal, 1),
                maneuverability: Trait(worldSeed, founderOrdinal, 2),
                fear: Trait(worldSeed, founderOrdinal, 3),
                aggression: Trait(worldSeed, founderOrdinal, 4),
                dietSpecialization: Trait(worldSeed, founderOrdinal, 5),
                memoryCapacity: Neutral,
                memoryRetention: Neutral,
                learningRate: Neutral,
                exploration: Neutral,
                temperatureTolerance: Neutral,
                fertilityInvestment: Neutral,
                lifespanTendency: Neutral);
        }

        private static float Trait(int worldSeed, long founderOrdinal, int traitIndex)
        {
            return DeterministicRandom.Float01(
                worldSeed,
                RandomDomain.FounderGenome,
                founderOrdinal,
                traitIndex,
                1,
                12 + traitIndex);
        }
    }
}
