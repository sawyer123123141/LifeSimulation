using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Behavior
{
    public static partial class DecisionSystem
    {

        private static void ScoreMate(
            CreatureNeeds needs,
            Phenotype phenotype,
            ReproductionState reproduction,
            CreatureObservation mate,
            CreatureNeeds mateNeeds,
            Phenotype matePhenotype,
            ReproductionState mateReproduction,
            CreatureObservation threat,
            float threatIntensity,
            float threatFalloffDistance,
            bool safetyGatedMateRendezvousEnabled,
            ref DecisionCandidateBuffer candidates)
        {
            if (safetyGatedMateRendezvousEnabled
                && threat.IsValid
                && threatIntensity > 0f
                && threat.Distance <= threatFalloffDistance)
            {
                return;
            }

            if (!mate.IsValid
                || !ReproductionSystem.CanSeekMate(needs, phenotype, reproduction)
                || !ReproductionSystem.CanSeekMate(mateNeeds, matePhenotype, mateReproduction))
            {
                return;
            }

            float safety = Math.Min(
                Math.Min(needs.Energy / Math.Max(0.01f, phenotype.EnergyCapacity), needs.Hydration / Math.Max(0.01f, phenotype.HydrationCapacity)),
                needs.Health / Math.Max(0.01f, phenotype.HealthCapacity));
            float score = 0.25f * safety / (1f + mate.Distance);
            candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekMate, -1, mate.CreatureId, score));
        }

        private static void ScoreCarcass(CreatureNeeds needs, Phenotype phenotype, ResourceStore resources, ResourceObservation carcass, ref DecisionCandidateBuffer candidates, out float carcassScore)
        {
            carcassScore = 0f;
            if (!carcass.IsValid)
            {
                return;
            }

            ResourceState resource = resources.GetAt(carcass.ResourceIndex);
            float hunger = Urgency(needs.Energy, phenotype.EnergyCapacity);
            float score = hunger * phenotype.MeatYieldMultiplier * Math.Min(1f, resource.Amount / Math.Max(0.01f, phenotype.IngestionRate)) / (1f + carcass.Distance);
            carcassScore = score;
            if (score >= 0.10f)
            {
                candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekCarcass, carcass.ResourceIndex, default, score));
            }
        }

        private static void ScorePredation(
            CreatureNeeds needs,
            Genome genome,
            Phenotype self,
            Phenotype other,
            CreatureObservation observation,
            float threatIntensity,
            ref DecisionCandidateBuffer candidates,
            bool economicsEnabled,
            CreatureId selfId,
            CreatureLineage selfLineage,
            CreatureLineage otherLineage,
            bool kinRecognitionEnabled,
            out float fleeScore,
            out float huntScore)
        {
            fleeScore = 0f;
            huntScore = 0f;
            if (!observation.IsValid)
            {
                return;
            }

            if (kinRecognitionEnabled && IsKin(selfId, selfLineage, observation.CreatureId, otherLineage))
            {
                return;
            }

            float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + observation.Distance);
            float hunger = Urgency(needs.Energy, self.EnergyCapacity);
            fleeScore = Math.Max(0f, threatIntensity * genome.RiskAversion * distanceAvailability);
            huntScore = PredationSystem.HuntCapability(self, other, observation.Distance, economicsEnabled) * hunger * distanceAvailability;
            if (fleeScore >= 0.10f)
            {
                candidates.TryAdd(new DecisionCandidate(CreatureIntent.Flee, -1, observation.CreatureId, fleeScore));
            }

            if (huntScore >= 0.10f)
            {
                candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekPrey, -1, observation.CreatureId, huntScore));
            }
        }

        private static void ScorePredationMulti(
            CreatureNeeds needs,
            Genome genome,
            Phenotype self,
            PredationCandidateBuffer others,
            ref DecisionCandidateBuffer candidates,
            bool economicsEnabled,
            CreatureId selfId,
            CreatureLineage selfLineage,
            bool kinRecognitionEnabled,
            out float fleeScore,
            out float huntScore)
        {
            fleeScore = 0f;
            huntScore = 0f;
            if (others.Count == 0)
            {
                return;
            }

            float hunger = Urgency(needs.Energy, self.EnergyCapacity);
            CreatureId bestFleeTarget = default;
            CreatureId bestHuntTarget = default;
            for (int i = 0; i < others.Count; i++)
            {
                CreatureObservation observation = others.GetObservationAt(i);
                if (kinRecognitionEnabled && IsKin(selfId, selfLineage, observation.CreatureId, others.GetLineageAt(i)))
                {
                    continue;
                }

                Phenotype otherPhenotype = others.GetPhenotypeAt(i);
                float distanceAvailability = economicsEnabled ? 1f : 1f / (1f + observation.Distance);
                float candidateThreatIntensity = PredationSystem.Threat(otherPhenotype, self, observation.Distance, economicsEnabled);
                float candidateFleeScore = Math.Max(0f, candidateThreatIntensity * genome.RiskAversion * distanceAvailability);
                float candidateHuntScore = PredationSystem.HuntCapability(self, otherPhenotype, observation.Distance, economicsEnabled) * hunger * distanceAvailability;
                if (candidateFleeScore > fleeScore)
                {
                    fleeScore = candidateFleeScore;
                    bestFleeTarget = observation.CreatureId;
                }

                if (candidateHuntScore > huntScore)
                {
                    huntScore = candidateHuntScore;
                    bestHuntTarget = observation.CreatureId;
                }
            }

            if (fleeScore >= 0.10f)
            {
                candidates.TryAdd(new DecisionCandidate(CreatureIntent.Flee, -1, bestFleeTarget, fleeScore));
            }

            if (huntScore >= 0.10f)
            {
                candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekPrey, -1, bestHuntTarget, huntScore));
            }
        }

        private static void ScoreResourceCandidates(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            ResourceCandidateBuffer observations,
            CreatureObservation threat,
            float threatIntensity,
            bool plantQualityPreferenceEnabled,
            HomeRangeState homeRange,
            bool homeRangeAffinityEnabled,
            ref DecisionCandidateBuffer candidates,
            ref float bestScore)
        {
            for (int index = 0; index < observations.Count; index++)
            {
                ResourceObservation observation = observations.GetAt(index);
                ResourceState resource = resources.GetAt(observation.ResourceIndex);
                float score = ResourceUtility(intent, needs, genome, phenotype, resource, observation.Distance, threat, threatIntensity, plantQualityPreferenceEnabled);
                if (homeRangeAffinityEnabled
                    && score >= MinimumUrgencyToSeekResource
                    && resource.IsActive
                    && resource.Amount > 0f
                    && ((intent == CreatureIntent.SeekFood && resource.Kind == ResourceKind.Food)
                        || (intent == CreatureIntent.SeekWater && resource.Kind == ResourceKind.Water)))
                {
                    score += HomeRangeSystem.GetCandidateBonus(homeRange, resource.Position);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                }

                candidates.TryAdd(new DecisionCandidate(intent, observation.ResourceIndex, default, score));
            }
        }

        private static void ScoreRememberedResource(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            SimVector2 origin,
            SimVector2 location,
            float confidence,
            float age,
            float learnedValue,
            int experienceCount,
            SimVector2 threatPosition,
            float threatConfidence,
            float threatFalloffDistance,
            ref DecisionCandidateBuffer candidates,
            ref float bestScore)
        {
            if (confidence <= 0f)
            {
                return;
            }

            bool seekingWater = intent == CreatureIntent.SeekWater;
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
            float distance = SimVector2.Distance(origin, location);
            float expectedValue = KnownOutcomeOrCuriosity(learnedValue, experienceCount, phenotype.Exploration);
            float staleness = 1f / (1f + Math.Max(0f, age));
            float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
            float avoidance = 0f;
            if (threatConfidence > 0f)
            {
                Span<PlaceMemory> threatPlaces = stackalloc PlaceMemory[1];
                threatPlaces[0] = new PlaceMemory { Position = threatPosition, Confidence = threatConfidence };
                avoidance = ForagingEconomics.ThreatAvoidance(location, threatPlaces, phenotype, threatFalloffDistance);
            }
            float score = Math.Max(0f, (urgency * confidence * staleness * expectedValue) - travelBurden - avoidance);
            if (score > bestScore)
            {
                bestScore = score;
            }

            candidates.TryAdd(new DecisionCandidate(intent, -1, default, score));
        }

        private static float ResourceUtility(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceState resource,
            float distance,
            CreatureObservation threat,
            float threatIntensity,
            bool plantQualityPreferenceEnabled)
        {
            bool seekingWater = intent == CreatureIntent.SeekWater;
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
            float needGain = ComputeNeedGain(seekingWater, needs, phenotype, resource);

            // ComputeNeedGain is clamped by Math.Min(1f, ..) and in practice saturates: every
            // active food patch returns exactly 1.0, roughly 10x over the clamp, at every hunger
            // level down to 5% energy. So the term carries no information about which patch is
            // better, and foraging reduces to urgency minus travel and danger — grazing is uniform.
            //
            // Plant defense lowers a patch's nutrition density, so uniform grazing is exactly the
            // condition under which defense cannot pay: it is never differentially avoided. This
            // term restores that preference, weighting a patch by nutrition density even when both
            // candidates would fully satisfy the need. Water is unaffected (no nutrition density).
            float qualityPreference = 1f;
            if (plantQualityPreferenceEnabled && !seekingWater)
            {
                qualityPreference = Math.Max(0f, resource.NutritionMultiplier);
            }

            float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
            float dangerPenalty = threat.IsValid ? Math.Max(0f, threatIntensity) * genome.RiskAversion * (distance / Math.Max(0.01f, phenotype.MaximumSpeed)) : 0f;
            if (!plantQualityPreferenceEnabled)
            {
                // Kept as a separate return so the flag-off path is the original expression
                // verbatim, rather than relying on multiplication by 1f being bit-exact.
                return Math.Max(0f, (urgency * needGain) - travelBurden - dangerPenalty);
            }

            return Math.Max(0f, (urgency * needGain * qualityPreference) - travelBurden - dangerPenalty);
        }

        private static float BestPatchScore(
            float current,
            float capacity,
            Phenotype phenotype,
            ResourceCandidateBuffer candidates,
            float handlingSeconds,
            float referenceGain,
            out int bestResourceIndex)
        {
            float urgency = Urgency(current, capacity);
            float bestScore = -1f;
            bestResourceIndex = -1;
            for (int index = 0; index < candidates.Count; index++)
            {
                ResourceObservation candidate = candidates.GetAt(index);
                float score = ForagingEconomics.PatchScore(urgency, candidate.RemainingAmount, candidate.Distance, phenotype, 1f, handlingSeconds, referenceGain);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestResourceIndex = candidate.ResourceIndex;
                }
            }

            return bestScore;
        }
    }
}
