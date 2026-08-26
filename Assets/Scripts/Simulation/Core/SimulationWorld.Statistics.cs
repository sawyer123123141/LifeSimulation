using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.Spatial;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Core
{
    public sealed partial class SimulationWorld
    {

        /// <summary>
        /// Builds a statistics sample describing the world <i>right now</i>, at
        /// <see cref="CurrentTick"/>, rather than returning the cached sample from the last
        /// cadence tick.
        ///
        /// <para><see cref="Statistics"/> is only refreshed on statistics-cadence ticks, so a run
        /// that stops on any other tick reports state up to a full cadence interval old, and the
        /// constructor-time value predates any scenario the caller applied. Experiments and panels
        /// that need the end-of-run truth must call this; per-tick simulation code must not, as it
        /// walks every creature.</para>
        ///
        /// <para><b>Valid only at a settled step boundary</b> - between completed <see cref="Step"/>
        /// calls, with no queued deaths outstanding. Calling it after <see cref="RequestDeath"/> and
        /// before the next <see cref="Step"/> throws, rather than quietly reporting the pending
        /// state this method exists to exclude.</para>
        /// </summary>
        public SimulationStatistics CaptureStatistics()
        {
            // Valid only at a settled step boundary. RequestDeath is public, so a caller can queue
            // a death and then sample before the next Step commits it; the sample would count a
            // creature already condemned and omit it from the death count - the exact defect this
            // method exists to remove, reintroduced through the front door. Pending deaths are
            // always empty between completed steps, so this can only fire on a genuine misuse.
            if (_pendingDeathCount > 0)
            {
                throw new InvalidOperationException(
                    "CaptureStatistics is only valid at a settled step boundary; "
                    + _pendingDeathCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + " death(s) are queued and uncommitted. Call Step first.");
            }

            return BuildStatistics(CurrentTick);
        }

        private SimulationStatistics BuildStatistics(long tick)
        {
            float bodySizeTotal = 0f;
            float movementSpeedTotal = 0f;
            float metabolicPaceTotal = 0f;
            float visionRangeTotal = 0f;
            float waterEfficiencyTotal = 0f;
            float foodEfficiencyTotal = 0f;
            float temperatureToleranceTotal = 0f;
            float fertilityInvestmentTotal = 0f;
            float lifespanTendencyTotal = 0f;
            float urgencyExponentTotal = 0f;
            float travelSensitivityTotal = 0f;
            float riskAversionTotal = 0f;
            float neutralMarkerTotal = 0f;
            float attackTotal = 0f;
            float defenseTotal = 0f;
            float aggressionTotal = 0f;
            float dietSpecializationTotal = 0f;
            int viableHunterCount = 0;
            float memoryCapacityTotal = 0f;
            float memoryRetentionTotal = 0f;
            float learningRateTotal = 0f;
            float explorationTotal = 0f;
            float maneuverabilityTotal = 0f;
            float fearTotal = 0f;
            float energyFractionTotal = 0f;
            float hydrationFractionTotal = 0f;
            int highestGeneration = 0;
            for (int index = 0; index < Creatures.Count; index++)
            {
                Genome genome = Creatures.GetGenomeAt(index);
                Phenotype phenotype = Creatures.GetPhenotypeAt(index);
                CreatureNeeds needs = Creatures.GetNeedsAt(index);
                bodySizeTotal += genome.BodySize;
                movementSpeedTotal += genome.MovementSpeed;
                metabolicPaceTotal += genome.MetabolicPace;
                visionRangeTotal += genome.VisionRange;
                waterEfficiencyTotal += genome.WaterEfficiency;
                foodEfficiencyTotal += genome.FoodEfficiency;
                temperatureToleranceTotal += genome.TemperatureTolerance;
                fertilityInvestmentTotal += genome.FertilityInvestment;
                lifespanTendencyTotal += genome.LifespanTendency;
                urgencyExponentTotal += genome.UrgencyExponent;
                travelSensitivityTotal += genome.TravelSensitivity;
                riskAversionTotal += genome.RiskAversion;
                neutralMarkerTotal += genome.NeutralMarker;
                attackTotal += genome.Attack;
                defenseTotal += genome.Defense;
                aggressionTotal += genome.Aggression;
                dietSpecializationTotal += genome.DietSpecialization;
                if (PredationSystem.HasViableHuntingStrategy(phenotype)) viableHunterCount++;
                memoryCapacityTotal += genome.MemoryCapacity;
                memoryRetentionTotal += genome.MemoryRetention;
                learningRateTotal += genome.LearningRate;
                explorationTotal += genome.Exploration;
                maneuverabilityTotal += genome.Maneuverability;
                fearTotal += genome.Fear;
                energyFractionTotal += needs.Energy / phenotype.EnergyCapacity;
                hydrationFractionTotal += needs.Hydration / phenotype.HydrationCapacity;
                highestGeneration = Math.Max(highestGeneration, Creatures.GetLineageAt(index).Generation);
            }

            float food = 0f;
            float water = 0f;
            float plantBiomass = 0f;
            int dormantPlantPatches = 0;
            int highestPlantGeneration = 0;
            float plantGrowthTotal = 0f;
            float plantNutritionTotal = 0f;
            float plantDefenseTotal = 0f;
            for (int index = 0; index < Resources.Count; index++)
            {
                ResourceState resource = Resources.GetAt(index);
                if (resource.Kind == ResourceKind.Food) food += resource.Amount;
                else if (resource.Kind == ResourceKind.Water) water += resource.Amount;
            }

            for (int index = 0; index < Plants.Count; index++)
            {
                PlantPatchState patch = Plants.GetAt(index);
                plantBiomass += patch.Biomass;
                if (patch.IsDormant) dormantPlantPatches++;
                highestPlantGeneration = Math.Max(highestPlantGeneration, patch.Lineage.Generation);
                plantGrowthTotal += patch.Genome.Growth;
                plantNutritionTotal += patch.Genome.Nutrition;
                plantDefenseTotal += patch.Genome.Defense;
            }

            float reciprocalPopulation = Creatures.Count == 0 ? 0f : 1f / Creatures.Count;
            return new SimulationStatistics(
                tick,
                Creatures.Count,
                highestGeneration,
                bodySizeTotal * reciprocalPopulation,
                movementSpeedTotal * reciprocalPopulation,
                metabolicPaceTotal * reciprocalPopulation,
                visionRangeTotal * reciprocalPopulation,
                waterEfficiencyTotal * reciprocalPopulation,
                foodEfficiencyTotal * reciprocalPopulation,
                energyFractionTotal * reciprocalPopulation,
                hydrationFractionTotal * reciprocalPopulation,
                food,
                water,
                _cumulativeFoodConsumed,
                _cumulativeWaterConsumed,
                _birthCount,
                _deathCount,
                _attackHitCount,
                _predationDeathCount,
                _cumulativeCarcassConsumed,
                temperatureToleranceTotal * reciprocalPopulation,
                fertilityInvestmentTotal * reciprocalPopulation,
                lifespanTendencyTotal * reciprocalPopulation,
                urgencyExponentTotal * reciprocalPopulation,
                travelSensitivityTotal * reciprocalPopulation,
                riskAversionTotal * reciprocalPopulation,
                neutralMarkerTotal * reciprocalPopulation,
                _starvationDeathCount,
                _dehydrationDeathCount,
                _ageDeathCount,
                _healthDeathCount,
                attackTotal * reciprocalPopulation,
                defenseTotal * reciprocalPopulation,
                aggressionTotal * reciprocalPopulation,
                dietSpecializationTotal * reciprocalPopulation,
                viableHunterCount,
                memoryCapacityTotal * reciprocalPopulation,
                memoryRetentionTotal * reciprocalPopulation,
                learningRateTotal * reciprocalPopulation,
                explorationTotal * reciprocalPopulation,
                plantBiomass,
                _cumulativePlantGrowth,
                _cumulativePlantBiomassConsumed,
                dormantPlantPatches,
                plantBiomass - (_initialPlantBiomass + _cumulativePlantGrowth - _cumulativePlantBiomassConsumed - _cumulativePlantBiomassLostToMortality),
                _plantBirthCount,
                Plants.Count,
                highestPlantGeneration,
                Plants.Count == 0 ? 0f : plantGrowthTotal / Plants.Count,
                Plants.Count == 0 ? 0f : plantNutritionTotal / Plants.Count,
                Plants.Count == 0 ? 0f : plantDefenseTotal / Plants.Count,
                _cumulativePlantBiomassLostToMortality,
                _plantBiomassSeconds,
                _plantPatchSeconds,
                meanManeuverabilityGene: maneuverabilityTotal * reciprocalPopulation,
                meanFearGene: fearTotal * reciprocalPopulation,
                cumulativeCombatDamage: _cumulativeCombatDamage,
                meanDefenseAtDeath: _defenseAtDeathCount == 0 ? 0f : _defenseAtDeathTotal / _defenseAtDeathCount,
                meanDefenseAtPredationDeath: _defenseAtPredationDeathCount == 0 ? 0f : _defenseAtPredationDeathTotal / _defenseAtPredationDeathCount);
        }
    }
}
