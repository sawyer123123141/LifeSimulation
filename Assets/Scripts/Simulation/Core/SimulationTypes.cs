using System;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Core
{
    public readonly struct CreatureId : IEquatable<CreatureId>
    {
        public CreatureId(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public bool Equals(CreatureId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CreatureId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public readonly struct SimVector2
    {
        public SimVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public static float Distance(SimVector2 left, SimVector2 right)
        {
            float x = right.X - left.X;
            float y = right.Y - left.Y;
            return (float)Math.Sqrt((x * x) + (y * y));
        }
    }

    public readonly struct CreatureLineage
    {
        public CreatureLineage(CreatureId lineageId, CreatureId firstParent, CreatureId secondParent, int generation)
        {
            LineageId = lineageId;
            FirstParent = firstParent;
            SecondParent = secondParent;
            Generation = generation;
        }

        public CreatureId LineageId { get; }
        public CreatureId FirstParent { get; }
        public CreatureId SecondParent { get; }
        public int Generation { get; }
    }

    public readonly struct SimulationStatistics
    {
        public SimulationStatistics(
            long tick,
            int population,
            int highestGeneration,
            float meanBodySizeGene,
            float meanMovementSpeedGene,
            float meanMetabolicPaceGene,
            float meanVisionRangeGene,
            float meanWaterEfficiencyGene,
            float meanFoodEfficiencyGene,
            float meanEnergyFraction,
            float meanHydrationFraction,
            float availableFood,
            float availableWater,
            float cumulativeFoodConsumed,
            float cumulativeWaterConsumed,
            int birthCount,
            int deathCount,
            int attackHitCount = 0,
            int predationDeathCount = 0,
            float cumulativeCarcassConsumed = 0f,
            float meanTemperatureToleranceGene = 0f,
            float meanFertilityInvestmentGene = 0f,
            float meanLifespanTendencyGene = 0f,
            float meanUrgencyExponentGene = 0f,
            float meanTravelSensitivityGene = 0f,
            float meanRiskAversionGene = 0f,
            float meanNeutralMarkerGene = 0f,
            int starvationDeathCount = 0,
            int dehydrationDeathCount = 0,
            int ageDeathCount = 0,
            int healthDeathCount = 0,
            float meanAttackGene = 0f,
            float meanDefenseGene = 0f,
            float meanAggressionGene = 0f,
            float meanDietSpecializationGene = 0f,
            int viableHunterCount = 0,
            float meanMemoryCapacityGene = 0f,
            float meanMemoryRetentionGene = 0f,
            float meanLearningRateGene = 0f,
            float meanExplorationGene = 0f,
            float totalPlantBiomass = 0f,
            float cumulativePlantGrowth = 0f,
            float cumulativePlantBiomassConsumed = 0f,
            int dormantPlantPatchCount = 0,
            float plantBiomassResidual = 0f,
            int plantBirthCount = 0,
            int activePlantPatchCount = 0,
            int highestPlantGeneration = 0,
            float meanPlantGrowthGene = 0f,
            float meanPlantNutritionGene = 0f,
            float meanPlantDefenseGene = 0f,
            float cumulativePlantBiomassLostToMortality = 0f,
            float plantBiomassSeconds = 0f,
            float plantPatchSeconds = 0f)
        {
            Tick = tick;
            Population = population;
            HighestGeneration = highestGeneration;
            MeanBodySizeGene = meanBodySizeGene;
            MeanMovementSpeedGene = meanMovementSpeedGene;
            MeanMetabolicPaceGene = meanMetabolicPaceGene;
            MeanVisionRangeGene = meanVisionRangeGene;
            MeanWaterEfficiencyGene = meanWaterEfficiencyGene;
            MeanFoodEfficiencyGene = meanFoodEfficiencyGene;
            MeanEnergyFraction = meanEnergyFraction;
            MeanHydrationFraction = meanHydrationFraction;
            AvailableFood = availableFood;
            AvailableWater = availableWater;
            CumulativeFoodConsumed = cumulativeFoodConsumed;
            CumulativeWaterConsumed = cumulativeWaterConsumed;
            BirthCount = birthCount;
            DeathCount = deathCount;
            AttackHitCount = attackHitCount;
            PredationDeathCount = predationDeathCount;
            CumulativeCarcassConsumed = cumulativeCarcassConsumed;
            MeanTemperatureToleranceGene = meanTemperatureToleranceGene;
            MeanFertilityInvestmentGene = meanFertilityInvestmentGene;
            MeanLifespanTendencyGene = meanLifespanTendencyGene;
            MeanUrgencyExponentGene = meanUrgencyExponentGene;
            MeanTravelSensitivityGene = meanTravelSensitivityGene;
            MeanRiskAversionGene = meanRiskAversionGene;
            MeanNeutralMarkerGene = meanNeutralMarkerGene;
            StarvationDeathCount = starvationDeathCount;
            DehydrationDeathCount = dehydrationDeathCount;
            AgeDeathCount = ageDeathCount;
            HealthDeathCount = healthDeathCount;
            MeanAttackGene = meanAttackGene;
            MeanDefenseGene = meanDefenseGene;
            MeanAggressionGene = meanAggressionGene;
            MeanDietSpecializationGene = meanDietSpecializationGene;
            ViableHunterCount = viableHunterCount;
            MeanMemoryCapacityGene = meanMemoryCapacityGene;
            MeanMemoryRetentionGene = meanMemoryRetentionGene;
            MeanLearningRateGene = meanLearningRateGene;
            MeanExplorationGene = meanExplorationGene;
            TotalPlantBiomass = totalPlantBiomass;
            CumulativePlantGrowth = cumulativePlantGrowth;
            CumulativePlantBiomassConsumed = cumulativePlantBiomassConsumed;
            DormantPlantPatchCount = dormantPlantPatchCount;
            PlantBiomassResidual = plantBiomassResidual;
            PlantBirthCount = plantBirthCount;
            ActivePlantPatchCount = activePlantPatchCount;
            HighestPlantGeneration = highestPlantGeneration;
            MeanPlantGrowthGene = meanPlantGrowthGene;
            MeanPlantNutritionGene = meanPlantNutritionGene;
            MeanPlantDefenseGene = meanPlantDefenseGene;
            CumulativePlantBiomassLostToMortality = cumulativePlantBiomassLostToMortality;
            PlantBiomassSeconds = plantBiomassSeconds;
            PlantPatchSeconds = plantPatchSeconds;
        }

        public long Tick { get; }
        public int Population { get; }
        public int HighestGeneration { get; }
        public float MeanBodySizeGene { get; }
        public float MeanMovementSpeedGene { get; }
        public float MeanMetabolicPaceGene { get; }
        public float MeanVisionRangeGene { get; }
        public float MeanWaterEfficiencyGene { get; }
        public float MeanFoodEfficiencyGene { get; }
        public float MeanEnergyFraction { get; }
        public float MeanHydrationFraction { get; }
        public float AvailableFood { get; }
        public float AvailableWater { get; }
        public float CumulativeFoodConsumed { get; }
        public float CumulativeWaterConsumed { get; }
        public int BirthCount { get; }
        public int DeathCount { get; }
        public int AttackHitCount { get; }
        public int PredationDeathCount { get; }
        public float CumulativeCarcassConsumed { get; }
        public float MeanTemperatureToleranceGene { get; }
        public float MeanFertilityInvestmentGene { get; }
        public float MeanLifespanTendencyGene { get; }
        public float MeanUrgencyExponentGene { get; }
        public float MeanTravelSensitivityGene { get; }
        public float MeanRiskAversionGene { get; }
        public float MeanNeutralMarkerGene { get; }
        public int StarvationDeathCount { get; }
        public int DehydrationDeathCount { get; }
        public int AgeDeathCount { get; }
        public int HealthDeathCount { get; }
        public float MeanAttackGene { get; }
        public float MeanDefenseGene { get; }
        public float MeanAggressionGene { get; }
        public float MeanDietSpecializationGene { get; }
        public int ViableHunterCount { get; }
        public int NonHunterCount => Population - ViableHunterCount;
        public float MeanMemoryCapacityGene { get; }
        public float MeanMemoryRetentionGene { get; }
        public float MeanLearningRateGene { get; }
        public float MeanExplorationGene { get; }
        public float TotalPlantBiomass { get; }
        public float CumulativePlantGrowth { get; }
        public float CumulativePlantBiomassConsumed { get; }
        public int DormantPlantPatchCount { get; }
        public float PlantBiomassResidual { get; }
        public int PlantBirthCount { get; }
        public int ActivePlantPatchCount { get; }
        public int HighestPlantGeneration { get; }
        public float MeanPlantGrowthGene { get; }
        public float MeanPlantNutritionGene { get; }
        public float MeanPlantDefenseGene { get; }
        public float CumulativePlantBiomassLostToMortality { get; }

        /// <summary>
        /// Time integral of standing plant biomass (biomass units x seconds), accumulated on the
        /// resource tick. Exists to normalize <see cref="CumulativePlantBiomassConsumed"/> into a
        /// rate; on its own it is not interpretable.
        /// </summary>
        public float PlantBiomassSeconds { get; }

        /// <summary>Time integral of the live plant patch count (patches x seconds).</summary>
        public float PlantPatchSeconds { get; }

        /// <summary>
        /// Realized grazing pressure: the fraction of standing plant biomass removed by consumers
        /// per second, averaged over the whole run.
        ///
        /// This is the metric a coevolution null must be read against. A null recorded at low
        /// realized pressure cannot distinguish "plants and consumers do not coevolve" from
        /// "nothing was grazing hard enough to select on defense" — the trap
        /// docs/experiments/p4-coevolution-null-2026-08-17.md fell into. Always report it
        /// alongside any plant-trait result.
        /// </summary>
        public float RealizedGrazingPressure => PlantBiomassSeconds <= 0f
            ? 0f
            : CumulativePlantBiomassConsumed / PlantBiomassSeconds;

        /// <summary>Biomass removed by consumers per live patch per second, averaged over the run.</summary>
        public float GrazingPerPatchSecond => PlantPatchSeconds <= 0f
            ? 0f
            : CumulativePlantBiomassConsumed / PlantPatchSeconds;
    }

    public struct ReproductionState
    {
        public float CooldownRemaining;
    }

    public struct CombatState
    {
        public float WoundSeverity;
        public float AttackRecoveryRemaining;
    }

    public struct MemoryState
    {
        public SimVector2 FoodPosition;
        public SimVector2 WaterPosition;
        public SimVector2 ThreatPosition;
        public float FoodConfidence;
        public float WaterConfidence;
        public float ThreatConfidence;
        public float FoodAge;
        public float WaterAge;
        public float ThreatAge;
        public SimVector2 ActiveRememberedTarget;
        public bool HasActiveRememberedTarget;
        public float FoodOutcomeValue;
        public float WaterOutcomeValue;
        public int FoodExperienceCount;
        public int WaterExperienceCount;
    }

    public struct ForagingState
    {
        public float SecondsInCurrentAction;
        public float RecentIntakeRate;
    }

    public struct PlaceMemory
    {
        public SimVector2 Position;
        public ResourceKind Kind;
        public float LastKnownAmount;
        public float OutcomeValue;
        public int VisitCount;
        public float Confidence;
        public long LastSeenTick;
    }

    public enum RandomDomain : ulong
    {
        Wander = 1,
        Crossover = 2,
        Mutation = 3,
        BirthPlacement = 4,
        ExperimentSampling = 5,
        Exploration = 6,
        FounderGenome = 7,
        AttackResolution = 8,
        PlantMutation = 9,
        PlantDispersal = 10,
        PlantEstablishment = 11,
        TerrainField = 12,
        PlantEstablishmentContest = 13
    }

    public enum DeathCause : byte
    {
        None = 0,
        Debug = 1,
        Starvation = 2,
        Dehydration = 3,
        Age = 4,
        Health = 5,
        Predation = 6
    }

    public enum SimulationEventKind : byte
    {
        Birth = 0,
        Death = 1
    }

    public readonly struct SimulationEvent
    {
        public SimulationEvent(
            long tick,
            SimulationEventKind kind,
            CreatureId subject,
            CreatureId firstRelated,
            CreatureId secondRelated,
            DeathCause deathCause)
        {
            Tick = tick;
            Kind = kind;
            Subject = subject;
            FirstRelated = firstRelated;
            SecondRelated = secondRelated;
            DeathCause = deathCause;
        }

        public long Tick { get; }
        public SimulationEventKind Kind { get; }
        public CreatureId Subject { get; }
        public CreatureId FirstRelated { get; }
        public CreatureId SecondRelated { get; }
        public DeathCause DeathCause { get; }
    }
}
