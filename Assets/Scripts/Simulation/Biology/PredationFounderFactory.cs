using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Biology
{
    public static class PredationFounderFactory
    {
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
                // Reproductive traits, added 2026-08-26. Without these the constructor's defaults of
                // 0 applied: every predator founder got maximum age 90s - the floor of
                // `90 + 180 * lifespanTendency` - and `16 - 8 * fertilityInvestment`, the longest
                // reproduction interval, against an adult age of 20s. Measured consequence: ZERO
                // births across 90 runs and three different worlds, while `PhysiologyVariation` gave
                // 492 births per run in the same worlds. The profile was not failing to survive
                // predation; it was failing to reproduce at all, so no predator-prey verdict taken
                // before this date measured predation.
                // `PhysiologyFounderFactory` has always set both explicitly. See
                // `docs/experiments/p6-predation-never-failed-its-founders-cannot-breed-2026-08-26.md`.
                fertilityInvestment: Trait(worldSeed, founderOrdinal, 6),
                lifespanTendency: Trait(worldSeed, founderOrdinal, 7));
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
