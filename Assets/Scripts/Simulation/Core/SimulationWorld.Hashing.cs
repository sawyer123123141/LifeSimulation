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

        public ulong ComputeBehaviorHash()
        {
            ulong hash = 14695981039346656037UL;
            hash = Hash(hash, unchecked((ulong)CurrentTick));
            hash = Hash(hash, unchecked((ulong)CreatureCount));
            hash = Hash(hash, unchecked((ulong)_spawnOrdinal));
            hash = Hash(hash, unchecked((ulong)_birthCount));
            hash = Hash(hash, unchecked((ulong)_deathCount));
            hash = Hash(hash, unchecked((ulong)_starvationDeathCount));
            hash = Hash(hash, unchecked((ulong)_dehydrationDeathCount));
            hash = Hash(hash, unchecked((ulong)_ageDeathCount));
            hash = Hash(hash, unchecked((ulong)_healthDeathCount));
            hash = Hash(hash, unchecked((ulong)_predationDeathCount));
            hash = Hash(hash, unchecked((ulong)_attackHitCount));

            for (int index = 0; index < CreatureCount; index++)
            {
                CreatureNeeds needs = Creatures.GetNeedsAt(index);
                hash = HashFloat(hash, needs.Energy);
                hash = HashFloat(hash, needs.Hydration);
                hash = HashFloat(hash, needs.Rest);
                hash = HashFloat(hash, needs.Health);
                hash = HashFloat(hash, needs.Age);

                MovementState movement = Creatures.GetMovementAt(index);
                hash = HashFloat(hash, movement.Position.X);
                hash = HashFloat(hash, movement.Position.Y);
                hash = HashFloat(hash, movement.DistanceSinceLastNeeds);

                CreatureDecision decision = Creatures.GetDecisionAt(index);
                hash = Hash(hash, unchecked((ulong)decision.Action));
                hash = Hash(hash, unchecked((ulong)(long)decision.TargetResourceIndex));
                hash = HashFloat(hash, decision.Score);

                ReproductionState reproduction = Creatures.GetReproductionRefAt(index);
                hash = HashFloat(hash, reproduction.CooldownRemaining);

                CombatState combat = Creatures.GetCombatRefAt(index);
                hash = HashFloat(hash, combat.WoundSeverity);

                MemoryState memory = Creatures.GetMemoryRefAt(index);
                hash = HashFloat(hash, memory.FoodConfidence);
                hash = HashFloat(hash, memory.WaterConfidence);
                hash = HashFloat(hash, memory.ThreatConfidence);
                hash = HashFloat(hash, memory.FoodOutcomeValue);
                hash = HashFloat(hash, memory.WaterOutcomeValue);
            }

            for (int index = 0; index < Resources.Count; index++)
            {
                ResourceState resource = Resources.GetAt(index);
                hash = HashFloat(hash, resource.Amount);
                hash = Hash(hash, resource.IsActive ? 1UL : 0UL);
            }

            for (int index = 0; index < Plants.Count; index++)
            {
                PlantPatchState patch = Plants.GetAt(index);
                hash = HashFloat(hash, patch.Biomass);
                hash = Hash(hash, unchecked((ulong)patch.Lineage.Generation));

                // Age and cooldown are behavior state, not genome: they decide when a patch dies
                // and when it may next disperse. Their absence here is why the ReplaceAt takeover
                // defect fixed on 2026-08-22 was invisible to every hash regression. Added after
                // measuring that it changes no liveness verdict - the inert flag set and every
                // plant gene verdict are identical with and without these two lines - so it is
                // strictly more sensitive at no cost to the pinned set. No BehaviorHash value is
                // pinned as a literal anywhere; the hash is only ever compared against itself.
                hash = HashFloat(hash, patch.Age);
                hash = HashFloat(hash, patch.ReproductionCooldownRemaining);
            }

            return hash;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = 14695981039346656037UL;
            hash = Hash(hash, unchecked((ulong)Config.WorldSeed));
            hash = Hash(hash, unchecked((ulong)CurrentTick));
            hash = Hash(hash, unchecked((ulong)CreatureCount));
            hash = Hash(hash, unchecked((ulong)_spawnOrdinal));

            for (int index = 0; index < CreatureCount; index++)
            {
                hash = Hash(hash, unchecked((ulong)GetCreatureIdAt(index).Value));
                Genome genome = Creatures.GetGenomeAt(index);
                hash = HashFloat(hash, genome.BodySize);
                hash = HashFloat(hash, genome.MovementSpeed);
                hash = HashFloat(hash, genome.MetabolicPace);
                hash = HashFloat(hash, genome.VisionRange);
                hash = HashFloat(hash, genome.WaterEfficiency);
                hash = HashFloat(hash, genome.FoodEfficiency);
                hash = HashFloat(hash, genome.Attack);
                hash = HashFloat(hash, genome.Defense);
                hash = HashFloat(hash, genome.Maneuverability);
                hash = HashFloat(hash, genome.Fear);
                hash = HashFloat(hash, genome.Aggression);
                hash = HashFloat(hash, genome.DietSpecialization);
                hash = HashFloat(hash, genome.MemoryCapacity);
                hash = HashFloat(hash, genome.MemoryRetention);
                hash = HashFloat(hash, genome.LearningRate);
                hash = HashFloat(hash, genome.Exploration);
                hash = HashFloat(hash, genome.TemperatureTolerance);
                hash = HashFloat(hash, genome.FertilityInvestment);
                hash = HashFloat(hash, genome.LifespanTendency);
                hash = HashFloat(hash, genome.UrgencyExponent);
                hash = HashFloat(hash, genome.TravelSensitivity);
                hash = HashFloat(hash, genome.RiskAversion);
                hash = HashFloat(hash, genome.NeutralMarker);
                hash = HashFloat(hash, genome.Persistence);

                CreatureNeeds needs = Creatures.GetNeedsAt(index);
                hash = HashFloat(hash, needs.Energy);
                hash = HashFloat(hash, needs.Hydration);
                hash = HashFloat(hash, needs.Rest);
                hash = HashFloat(hash, needs.Health);
                hash = HashFloat(hash, needs.Age);

                MovementState movement = Creatures.GetMovementAt(index);
                hash = HashFloat(hash, movement.PreviousPosition.X);
                hash = HashFloat(hash, movement.PreviousPosition.Y);
                hash = HashFloat(hash, movement.Position.X);
                hash = HashFloat(hash, movement.Position.Y);
                hash = HashFloat(hash, movement.DistanceSinceLastNeeds);

                CreatureDecision decision = Creatures.GetDecisionAt(index);
                hash = Hash(hash, unchecked((ulong)decision.Action));
                hash = Hash(hash, unchecked((ulong)(long)decision.TargetResourceIndex));
                hash = Hash(hash, unchecked((ulong)decision.TargetCreatureId.Value));
                hash = HashFloat(hash, decision.Score);
                hash = Hash(hash, unchecked((ulong)decision.DecisionTick));

                ReproductionState reproduction = Creatures.GetReproductionRefAt(index);
                hash = HashFloat(hash, reproduction.CooldownRemaining);

                CombatState combat = Creatures.GetCombatRefAt(index);
                hash = HashFloat(hash, combat.WoundSeverity);
                hash = HashFloat(hash, combat.AttackRecoveryRemaining);

                MemoryState memory = Creatures.GetMemoryRefAt(index);
                hash = HashFloat(hash, memory.FoodPosition.X);
                hash = HashFloat(hash, memory.FoodPosition.Y);
                hash = HashFloat(hash, memory.WaterPosition.X);
                hash = HashFloat(hash, memory.WaterPosition.Y);
                hash = HashFloat(hash, memory.ThreatPosition.X);
                hash = HashFloat(hash, memory.ThreatPosition.Y);
                hash = HashFloat(hash, memory.FoodConfidence);
                hash = HashFloat(hash, memory.WaterConfidence);
                hash = HashFloat(hash, memory.ThreatConfidence);
                hash = HashFloat(hash, memory.FoodAge);
                hash = HashFloat(hash, memory.WaterAge);
                hash = HashFloat(hash, memory.ThreatAge);
                hash = HashFloat(hash, memory.ActiveRememberedTarget.X);
                hash = HashFloat(hash, memory.ActiveRememberedTarget.Y);
                hash = Hash(hash, memory.HasActiveRememberedTarget ? 1UL : 0UL);
                hash = HashFloat(hash, memory.FoodOutcomeValue);
                hash = HashFloat(hash, memory.WaterOutcomeValue);
                hash = Hash(hash, unchecked((ulong)memory.FoodExperienceCount));
                hash = Hash(hash, unchecked((ulong)memory.WaterExperienceCount));

                if (Config.HomeRangeAffinityEnabled)
                {
                    HomeRangeState homeRange = Creatures.GetHomeRangeRefAt(index);
                    hash = HashFloat(hash, homeRange.Centre.X);
                    hash = HashFloat(hash, homeRange.Centre.Y);
                    hash = HashFloat(hash, homeRange.Familiarity);
                }
            }

            hash = Hash(hash, unchecked((ulong)Resources.Count));
            for (int index = 0; index < Resources.Count; index++)
            {
                ResourceState resource = Resources.GetAt(index);
                hash = Hash(hash, unchecked((ulong)resource.Id.Value));
                hash = Hash(hash, unchecked((ulong)resource.Kind));
                hash = HashFloat(hash, resource.Position.X);
                hash = HashFloat(hash, resource.Position.Y);
                hash = HashFloat(hash, resource.InteractionRadius);
                hash = HashFloat(hash, resource.Amount);
                hash = HashFloat(hash, resource.Capacity);
                hash = HashFloat(hash, resource.RegenerationPerSecond);
                hash = Hash(hash, resource.IsActive ? 1UL : 0UL);
                hash = HashFloat(hash, resource.NutritionMultiplier);
            }

            hash = Hash(hash, unchecked((ulong)Plants.Count));
            for (int index = 0; index < Plants.Count; index++)
            {
                PlantPatchState patch = Plants.GetAt(index);
                hash = Hash(hash, unchecked((ulong)patch.Id.Value));
                hash = Hash(hash, unchecked((ulong)patch.FoodResourceId.Value));
                hash = HashFloat(hash, patch.Biomass);
                hash = HashFloat(hash, patch.Capacity);
                hash = HashFloat(hash, patch.GrowthRate);
                hash = HashFloat(hash, patch.Nutrition);
                hash = HashFloat(hash, patch.Defense);
                hash = HashFloat(hash, patch.Genome.Growth);
                hash = HashFloat(hash, patch.Genome.SeedInvestment);
                hash = HashFloat(hash, patch.Genome.WaterEfficiency);
                hash = HashFloat(hash, patch.Genome.Nutrition);
                hash = HashFloat(hash, patch.Genome.Defense);
                hash = HashFloat(hash, patch.Genome.Dispersal);
                hash = HashFloat(hash, patch.Genome.MoistureTolerance);
                hash = HashFloat(hash, patch.Genome.TemperatureTolerance);
                hash = HashFloat(hash, patch.Genome.NutrientUptake);
                hash = HashFloat(hash, patch.Genome.SeedlingResilience);
                hash = HashFloat(hash, patch.Genome.SeedProductionRate);
                hash = Hash(hash, unchecked((ulong)patch.Lineage.LineageId.Value));
                hash = Hash(hash, unchecked((ulong)patch.Lineage.ParentId.Value));
                hash = Hash(hash, unchecked((ulong)patch.Lineage.Generation));
            }

            return hash;
        }

        /// <summary>
        /// Answers "will these two worlds evolve identically from here?" — every piece of state and
        /// configuration that determines future simulation behavior, hashed together. Unlike
        /// <see cref="ComputeStateHash"/> (frozen at V1, kept only for historical continuity with
        /// recorded baselines and never recomputed to add coverage), this hash covers configuration,
        /// the RNG-stream ordinals that decide birth and dispersal outcomes, entity id counters,
        /// plant site registry contents, and plant age/cooldown — see
        /// docs/superpowers/specs/2026-08-22-state-fingerprint-design.md for the audit and the
        /// reasoning behind what is deliberately excluded (reporting accumulators, liveness
        /// counters, and derived caches).
        ///
        /// <para><b>Valid only at a settled step boundary</b> - between completed <see cref="Step"/>
        /// calls, with no queued deaths outstanding. Calling it after <see cref="RequestDeath"/> and
        /// before the next <see cref="Step"/> throws, rather than quietly reporting the pending
        /// state this method exists to exclude.</para>
        /// </summary>
        public ulong ComputeStateFingerprint()
        {
            // Valid only at a settled step boundary. RequestDeath is public, so a caller can queue
            // a death and then fingerprint before the next Step commits it; the sample would then
            // depend on which entities are about to be removed rather than on settled state.
            // Pending deaths are always empty between completed steps, so this can only fire on a
            // genuine misuse.
            if (_pendingDeathCount > 0)
            {
                throw new InvalidOperationException(
                    "ComputeStateFingerprint is only valid at a settled step boundary; "
                    + _pendingDeathCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + " death(s) are queued and uncommitted. Call Step first.");
            }

            ulong hash = 14695981039346656037UL;
            hash = Hash(hash, unchecked((ulong)StateFingerprintVersion));
            hash = Hash(hash, Config.ComputeConfigurationHash());

            hash = Hash(hash, unchecked((ulong)CurrentTick));
            hash = Hash(hash, unchecked((ulong)CreatureCount));
            hash = Hash(hash, unchecked((ulong)_spawnOrdinal));
            hash = Hash(hash, unchecked((ulong)_birthOrdinal));
            hash = Hash(hash, unchecked((ulong)_plantSeedOrdinal));

            hash = Hash(hash, unchecked((ulong)Creatures.NextIdPeek));
            hash = Hash(hash, unchecked((ulong)Resources.NextIdPeek));
            hash = Hash(hash, unchecked((ulong)Plants.NextIdPeek));

            hash = Hash(hash, unchecked((ulong)PlantSites.Count));
            for (int slot = 0; slot < PlantSites.Count; slot++)
            {
                hash = Hash(hash, unchecked((ulong)(long)PlantSites.GetResourceIndexAt(slot)));
            }

            for (int index = 0; index < CreatureCount; index++)
            {
                hash = Hash(hash, unchecked((ulong)GetCreatureIdAt(index).Value));
                Genome genome = Creatures.GetGenomeAt(index);
                hash = HashFloat(hash, genome.BodySize);
                hash = HashFloat(hash, genome.MovementSpeed);
                hash = HashFloat(hash, genome.MetabolicPace);
                hash = HashFloat(hash, genome.VisionRange);
                hash = HashFloat(hash, genome.WaterEfficiency);
                hash = HashFloat(hash, genome.FoodEfficiency);
                hash = HashFloat(hash, genome.Attack);
                hash = HashFloat(hash, genome.Defense);
                hash = HashFloat(hash, genome.Maneuverability);
                hash = HashFloat(hash, genome.Fear);
                hash = HashFloat(hash, genome.Aggression);
                hash = HashFloat(hash, genome.DietSpecialization);
                hash = HashFloat(hash, genome.MemoryCapacity);
                hash = HashFloat(hash, genome.MemoryRetention);
                hash = HashFloat(hash, genome.LearningRate);
                hash = HashFloat(hash, genome.Exploration);
                hash = HashFloat(hash, genome.TemperatureTolerance);
                hash = HashFloat(hash, genome.FertilityInvestment);
                hash = HashFloat(hash, genome.LifespanTendency);
                hash = HashFloat(hash, genome.UrgencyExponent);
                hash = HashFloat(hash, genome.TravelSensitivity);
                hash = HashFloat(hash, genome.RiskAversion);
                hash = HashFloat(hash, genome.NeutralMarker);
                hash = HashFloat(hash, genome.Persistence);

                CreatureNeeds needs = Creatures.GetNeedsAt(index);
                hash = HashFloat(hash, needs.Energy);
                hash = HashFloat(hash, needs.Hydration);
                hash = HashFloat(hash, needs.Rest);
                hash = HashFloat(hash, needs.Health);
                hash = HashFloat(hash, needs.Age);

                MovementState movement = Creatures.GetMovementAt(index);
                hash = HashFloat(hash, movement.PreviousPosition.X);
                hash = HashFloat(hash, movement.PreviousPosition.Y);
                hash = HashFloat(hash, movement.Position.X);
                hash = HashFloat(hash, movement.Position.Y);
                hash = HashFloat(hash, movement.DistanceSinceLastNeeds);

                CreatureDecision decision = Creatures.GetDecisionAt(index);
                hash = Hash(hash, unchecked((ulong)decision.Action));
                hash = Hash(hash, unchecked((ulong)(long)decision.TargetResourceIndex));
                hash = Hash(hash, unchecked((ulong)decision.TargetCreatureId.Value));
                hash = HashFloat(hash, decision.Score);
                hash = Hash(hash, unchecked((ulong)decision.DecisionTick));

                ReproductionState reproduction = Creatures.GetReproductionRefAt(index);
                hash = HashFloat(hash, reproduction.CooldownRemaining);

                CombatState combat = Creatures.GetCombatRefAt(index);
                hash = HashFloat(hash, combat.WoundSeverity);
                hash = HashFloat(hash, combat.AttackRecoveryRemaining);

                MemoryState memory = Creatures.GetMemoryRefAt(index);
                hash = HashFloat(hash, memory.FoodPosition.X);
                hash = HashFloat(hash, memory.FoodPosition.Y);
                hash = HashFloat(hash, memory.WaterPosition.X);
                hash = HashFloat(hash, memory.WaterPosition.Y);
                hash = HashFloat(hash, memory.ThreatPosition.X);
                hash = HashFloat(hash, memory.ThreatPosition.Y);
                hash = HashFloat(hash, memory.FoodConfidence);
                hash = HashFloat(hash, memory.WaterConfidence);
                hash = HashFloat(hash, memory.ThreatConfidence);
                hash = HashFloat(hash, memory.FoodAge);
                hash = HashFloat(hash, memory.WaterAge);
                hash = HashFloat(hash, memory.ThreatAge);
                hash = HashFloat(hash, memory.ActiveRememberedTarget.X);
                hash = HashFloat(hash, memory.ActiveRememberedTarget.Y);
                hash = Hash(hash, memory.HasActiveRememberedTarget ? 1UL : 0UL);
                hash = HashFloat(hash, memory.FoodOutcomeValue);
                hash = HashFloat(hash, memory.WaterOutcomeValue);
                hash = Hash(hash, unchecked((ulong)memory.FoodExperienceCount));
                hash = Hash(hash, unchecked((ulong)memory.WaterExperienceCount));

                // Unconditional, unlike ComputeStateHash's HomeRangeAffinityEnabled gate: V2 must
                // not depend on a flag to decide what it covers.
                HomeRangeState homeRange = Creatures.GetHomeRangeRefAt(index);
                hash = HashFloat(hash, homeRange.Centre.X);
                hash = HashFloat(hash, homeRange.Centre.Y);
                hash = HashFloat(hash, homeRange.Familiarity);
            }

            hash = Hash(hash, unchecked((ulong)Resources.Count));
            for (int index = 0; index < Resources.Count; index++)
            {
                ResourceState resource = Resources.GetAt(index);
                hash = Hash(hash, unchecked((ulong)resource.Id.Value));
                hash = Hash(hash, unchecked((ulong)resource.Kind));
                hash = HashFloat(hash, resource.Position.X);
                hash = HashFloat(hash, resource.Position.Y);
                hash = HashFloat(hash, resource.InteractionRadius);
                hash = HashFloat(hash, resource.Amount);
                hash = HashFloat(hash, resource.Capacity);
                hash = HashFloat(hash, resource.RegenerationPerSecond);
                hash = Hash(hash, resource.IsActive ? 1UL : 0UL);
                hash = HashFloat(hash, resource.NutritionMultiplier);
            }

            hash = Hash(hash, unchecked((ulong)Plants.Count));
            for (int index = 0; index < Plants.Count; index++)
            {
                PlantPatchState patch = Plants.GetAt(index);
                hash = Hash(hash, unchecked((ulong)patch.Id.Value));
                hash = Hash(hash, unchecked((ulong)patch.FoodResourceId.Value));
                hash = HashFloat(hash, patch.Biomass);
                hash = HashFloat(hash, patch.Capacity);
                hash = HashFloat(hash, patch.GrowthRate);
                hash = HashFloat(hash, patch.Nutrition);
                hash = HashFloat(hash, patch.Defense);
                hash = HashFloat(hash, patch.Genome.Growth);
                hash = HashFloat(hash, patch.Genome.SeedInvestment);
                hash = HashFloat(hash, patch.Genome.WaterEfficiency);
                hash = HashFloat(hash, patch.Genome.Nutrition);
                hash = HashFloat(hash, patch.Genome.Defense);
                hash = HashFloat(hash, patch.Genome.Dispersal);
                hash = HashFloat(hash, patch.Genome.MoistureTolerance);
                hash = HashFloat(hash, patch.Genome.TemperatureTolerance);
                hash = HashFloat(hash, patch.Genome.NutrientUptake);
                hash = HashFloat(hash, patch.Genome.SeedlingResilience);
                hash = HashFloat(hash, patch.Genome.SeedProductionRate);
                hash = Hash(hash, unchecked((ulong)patch.Lineage.LineageId.Value));
                hash = Hash(hash, unchecked((ulong)patch.Lineage.ParentId.Value));
                hash = Hash(hash, unchecked((ulong)patch.Lineage.Generation));
                hash = HashFloat(hash, patch.Age);
                hash = HashFloat(hash, patch.ReproductionCooldownRemaining);
            }

            return hash;
        }

        private static ulong Hash(ulong hash, ulong value)
        {
            return (hash ^ value) * 1099511628211UL;
        }

        private static ulong HashFloat(ulong hash, float value)
        {
            return Hash(hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(value)));
        }
    }
}
