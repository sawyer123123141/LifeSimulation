using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Resources;

namespace LifeSimulation.Simulation.Behavior
{
    public enum CreatureAction : byte
    {
        Wander,
        SeekFood,
        Eat,
        SeekWater,
        Drink,
        Rest,
        SeekMate,
        Reproduce,
        SeekPrey,
        Attack,
        Flee,
        SeekCarcass,
        FeedCarcass,
        SeekThermalComfort,
    }

    public enum CreatureIntent : byte
    {
        Wander = 0,
        SeekFood = 1,
        SeekWater = 2,
        SeekPrey = 3,
        Flee = 4,
        SeekCarcass = 5,
        SeekThermalComfort = 6,
        SeekMate = 7,
        Rest = 8,
    }

    public readonly struct DecisionCandidate
    {
        public DecisionCandidate(CreatureIntent intent, int targetResourceIndex, CreatureId targetCreatureId, float score)
        {
            Intent = intent;
            TargetResourceIndex = targetResourceIndex;
            TargetCreatureId = targetCreatureId;
            Score = score;
        }

        public CreatureIntent Intent { get; }
        public int TargetResourceIndex { get; }
        public CreatureId TargetCreatureId { get; }
        public float Score { get; }
    }

    public struct DecisionCandidateBuffer
    {
        public const int Capacity = 16;

        private DecisionCandidate _candidate0;
        private DecisionCandidate _candidate1;
        private DecisionCandidate _candidate2;
        private DecisionCandidate _candidate3;
        private DecisionCandidate _candidate4;
        private DecisionCandidate _candidate5;
        private DecisionCandidate _candidate6;
        private DecisionCandidate _candidate7;
        private DecisionCandidate _candidate8;
        private DecisionCandidate _candidate9;
        private DecisionCandidate _candidate10;
        private DecisionCandidate _candidate11;
        private DecisionCandidate _candidate12;
        private DecisionCandidate _candidate13;
        private DecisionCandidate _candidate14;
        private DecisionCandidate _candidate15;
        private int _count;

        public int Count => _count;

        public bool TryAdd(DecisionCandidate candidate)
        {
            if (_count >= Capacity)
            {
                return false;
            }

            SetAt(_count++, candidate);
            return true;
        }

        public bool TryGetBest(out DecisionCandidate best)
        {
            if (_count == 0)
            {
                best = default;
                return false;
            }

            best = GetAt(0);
            for (int index = 1; index < _count; index++)
            {
                DecisionCandidate candidate = GetAt(index);
                if (IsBetter(candidate, best))
                {
                    best = candidate;
                }
            }

            return true;
        }

        private static bool IsBetter(DecisionCandidate candidate, DecisionCandidate currentBest)
        {
            if (candidate.Score != currentBest.Score)
            {
                return candidate.Score > currentBest.Score;
            }

            if (candidate.Intent != currentBest.Intent)
            {
                return candidate.Intent < currentBest.Intent;
            }

            if (candidate.TargetResourceIndex != currentBest.TargetResourceIndex)
            {
                return candidate.TargetResourceIndex < currentBest.TargetResourceIndex;
            }

            return candidate.TargetCreatureId.Value < currentBest.TargetCreatureId.Value;
        }

        private DecisionCandidate GetAt(int index)
        {
            switch (index)
            {
                case 0: return _candidate0;
                case 1: return _candidate1;
                case 2: return _candidate2;
                case 3: return _candidate3;
                case 4: return _candidate4;
                case 5: return _candidate5;
                case 6: return _candidate6;
                case 7: return _candidate7;
                case 8: return _candidate8;
                case 9: return _candidate9;
                case 10: return _candidate10;
                case 11: return _candidate11;
                case 12: return _candidate12;
                case 13: return _candidate13;
                case 14: return _candidate14;
                default: return _candidate15;
            }
        }

        private void SetAt(int index, DecisionCandidate candidate)
        {
            switch (index)
            {
                case 0: _candidate0 = candidate; break;
                case 1: _candidate1 = candidate; break;
                case 2: _candidate2 = candidate; break;
                case 3: _candidate3 = candidate; break;
                case 4: _candidate4 = candidate; break;
                case 5: _candidate5 = candidate; break;
                case 6: _candidate6 = candidate; break;
                case 7: _candidate7 = candidate; break;
                case 8: _candidate8 = candidate; break;
                case 9: _candidate9 = candidate; break;
                case 10: _candidate10 = candidate; break;
                case 11: _candidate11 = candidate; break;
                case 12: _candidate12 = candidate; break;
                case 13: _candidate13 = candidate; break;
                case 14: _candidate14 = candidate; break;
                default: _candidate15 = candidate; break;
            }
        }
    }

    public readonly struct CreatureDecision
    {
        public CreatureDecision(
            CreatureAction action,
            int targetResourceIndex,
            float score,
            long decisionTick = -1,
            CreatureId targetCreatureId = default)
        {
            Action = action;
            TargetResourceIndex = targetResourceIndex;
            Score = score;
            DecisionTick = decisionTick;
            TargetCreatureId = targetCreatureId;
        }

        public CreatureAction Action { get; }
        public int TargetResourceIndex { get; }
        public float Score { get; }
        public long DecisionTick { get; }
        public CreatureId TargetCreatureId { get; }
    }

    public readonly struct DecisionDiagnostics
    {
        public DecisionDiagnostics(float foodScore, float waterScore, bool foodVisible, bool waterVisible)
            : this(foodScore, waterScore, foodVisible, waterVisible, 0f, 0f, 0f, 0f, CreatureAction.Wander)
        {
        }

        private DecisionDiagnostics(
            float foodScore,
            float waterScore,
            bool foodVisible,
            bool waterVisible,
            float fleeScore,
            float huntScore,
            float carcassScore,
            float thermalScore,
            CreatureAction winningAction)
        {
            FoodScore = foodScore;
            WaterScore = waterScore;
            FoodVisible = foodVisible;
            WaterVisible = waterVisible;
            FleeScore = fleeScore;
            HuntScore = huntScore;
            CarcassScore = carcassScore;
            ThermalScore = thermalScore;
            WinningAction = winningAction;
        }

        public float FoodScore { get; }
        public float WaterScore { get; }
        public bool FoodVisible { get; }
        public bool WaterVisible { get; }
        public float FleeScore { get; }
        public float HuntScore { get; }
        public float CarcassScore { get; }
        public float ThermalScore { get; }
        public CreatureAction WinningAction { get; }

        public DecisionDiagnostics WithPredationScores(float fleeScore, float huntScore)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                fleeScore, huntScore, CarcassScore, ThermalScore, WinningAction);
        }

        public DecisionDiagnostics WithCarcassScore(float carcassScore)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                FleeScore, HuntScore, carcassScore, ThermalScore, WinningAction);
        }

        public DecisionDiagnostics WithThermalScore(float thermalScore)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                FleeScore, HuntScore, CarcassScore, thermalScore, WinningAction);
        }

        public DecisionDiagnostics WithWinningAction(CreatureAction winningAction)
        {
            return new DecisionDiagnostics(
                FoodScore, WaterScore, FoodVisible, WaterVisible,
                FleeScore, HuntScore, CarcassScore, ThermalScore, winningAction);
        }
    }

    public struct PredationCandidateBuffer
    {
        public const int Capacity = 4;

        private CreatureObservation _observation0;
        private Phenotype _phenotype0;
        private CreatureLineage _lineage0;
        private CreatureObservation _observation1;
        private Phenotype _phenotype1;
        private CreatureLineage _lineage1;
        private CreatureObservation _observation2;
        private Phenotype _phenotype2;
        private CreatureLineage _lineage2;
        private CreatureObservation _observation3;
        private Phenotype _phenotype3;
        private CreatureLineage _lineage3;
        private int _count;

        public int Count => _count;

        public CreatureObservation GetObservationAt(int index)
        {
            switch (index)
            {
                case 0: return _observation0;
                case 1: return _observation1;
                case 2: return _observation2;
                default: return _observation3;
            }
        }

        public Phenotype GetPhenotypeAt(int index)
        {
            switch (index)
            {
                case 0: return _phenotype0;
                case 1: return _phenotype1;
                case 2: return _phenotype2;
                default: return _phenotype3;
            }
        }

        public CreatureLineage GetLineageAt(int index)
        {
            switch (index)
            {
                case 0: return _lineage0;
                case 1: return _lineage1;
                case 2: return _lineage2;
                default: return _lineage3;
            }
        }

        public void Add(CreatureObservation observation, Phenotype phenotype, CreatureLineage lineage)
        {
            if (_count >= Capacity)
            {
                return;
            }

            switch (_count)
            {
                case 0: _observation0 = observation; _phenotype0 = phenotype; _lineage0 = lineage; break;
                case 1: _observation1 = observation; _phenotype1 = phenotype; _lineage1 = lineage; break;
                case 2: _observation2 = observation; _phenotype2 = phenotype; _lineage2 = lineage; break;
                default: _observation3 = observation; _phenotype3 = phenotype; _lineage3 = lineage; break;
            }

            _count++;
        }
    }

    public static class DecisionSystem
    {
        private const float MinimumUrgencyToSeekResource = 0.05f;

        public static CreatureDecision DecideIntentUtilityV1(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            SimVector2 origin,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            ResourceObservation carcass,
            MemoryState memory,
            bool cognitionEnabled,
            CreatureObservation threat,
            float threatIntensity,
            Phenotype otherPhenotype,
            bool predationEnabled,
            bool physiologyEnabled,
            long tick,
            out DecisionDiagnostics diagnostics,
            bool economicsEnabled = false,
            float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance,
            PredationCandidateBuffer otherCandidates = default,
            bool multiThreatPerceptionEnabled = false,
            bool restBehaviorEnabled = false,
            CreatureId selfId = default,
            CreatureLineage selfLineage = default,
            CreatureLineage otherLineage = default,
            bool kinRecognitionEnabled = false)
        {
            return DecideIntentUtilityV1(
                needs, genome, phenotype, resources, origin, foodCandidates, waterCandidates, carcass, memory,
                cognitionEnabled, threat, threatIntensity, otherPhenotype, predationEnabled, physiologyEnabled,
                default, default, default, default, default, false, tick, out diagnostics, economicsEnabled,
                threatFalloffDistance, otherCandidates, multiThreatPerceptionEnabled, restBehaviorEnabled,
                selfId, selfLineage, otherLineage, kinRecognitionEnabled);
        }

        public static CreatureDecision DecideIntentUtilityV1(
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceStore resources,
            SimVector2 origin,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            ResourceObservation carcass,
            MemoryState memory,
            bool cognitionEnabled,
            CreatureObservation threat,
            float threatIntensity,
            Phenotype otherPhenotype,
            bool predationEnabled,
            bool physiologyEnabled,
            ReproductionState reproduction,
            CreatureObservation mate,
            CreatureNeeds mateNeeds,
            Phenotype matePhenotype,
            ReproductionState mateReproduction,
            bool reproductionEnabled,
            long tick,
            out DecisionDiagnostics diagnostics,
            bool economicsEnabled = false,
            float threatFalloffDistance = SimulationConfig.DefaultThreatFalloffDistance,
            PredationCandidateBuffer otherCandidates = default,
            bool multiThreatPerceptionEnabled = false,
            bool restBehaviorEnabled = false,
            CreatureId selfId = default,
            CreatureLineage selfLineage = default,
            CreatureLineage otherLineage = default,
            bool kinRecognitionEnabled = false)
        {
            var candidates = new DecisionCandidateBuffer();
            float bestFoodScore = -1f;
            float bestWaterScore = -1f;

            ScoreResourceCandidates(CreatureIntent.SeekFood, needs, genome, phenotype, resources, foodCandidates, threat, threatIntensity, ref candidates, ref bestFoodScore);
            ScoreResourceCandidates(CreatureIntent.SeekWater, needs, genome, phenotype, resources, waterCandidates, threat, threatIntensity, ref candidates, ref bestWaterScore);
            if (cognitionEnabled)
            {
                ScoreRememberedResource(CreatureIntent.SeekFood, needs, genome, phenotype, origin, memory.FoodPosition, memory.FoodConfidence, memory.FoodAge, memory.FoodOutcomeValue, memory.FoodExperienceCount, memory.ThreatPosition, memory.ThreatConfidence, threatFalloffDistance, ref candidates, ref bestFoodScore);
                ScoreRememberedResource(CreatureIntent.SeekWater, needs, genome, phenotype, origin, memory.WaterPosition, memory.WaterConfidence, memory.WaterAge, memory.WaterOutcomeValue, memory.WaterExperienceCount, memory.ThreatPosition, memory.ThreatConfidence, threatFalloffDistance, ref candidates, ref bestWaterScore);
            }
            float carcassScore = 0f;
            float fleeScore = 0f;
            float huntScore = 0f;
            if (predationEnabled)
            {
                ScoreCarcass(needs, phenotype, resources, carcass, ref candidates, out carcassScore);
                if (multiThreatPerceptionEnabled)
                {
                    ScorePredationMulti(needs, genome, phenotype, otherCandidates, ref candidates, economicsEnabled, selfId, selfLineage, kinRecognitionEnabled, out fleeScore, out huntScore);
                }
                else
                {
                    ScorePredation(needs, genome, phenotype, otherPhenotype, threat, threatIntensity, ref candidates, economicsEnabled, selfId, selfLineage, otherLineage, kinRecognitionEnabled, out fleeScore, out huntScore);
                }
            }
            float thermalScore = 0f;
            if (physiologyEnabled)
            {
                thermalScore = ThermoregulationSystem.ScoreThermalComfort(phenotype, origin, tick);
                if (thermalScore >= 0.15f)
                {
                    candidates.TryAdd(new DecisionCandidate(CreatureIntent.SeekThermalComfort, -1, default, thermalScore));
                }
            }
            if (restBehaviorEnabled)
            {
                float restScore = Urgency(needs.Rest, NeedsSystem.RestCapacity);
                if (restScore >= 0.15f)
                {
                    candidates.TryAdd(new DecisionCandidate(CreatureIntent.Rest, -1, default, restScore));
                }
            }
            if (reproductionEnabled)
            {
                ScoreMate(needs, phenotype, reproduction, mate, mateNeeds, matePhenotype, mateReproduction, ref candidates);
            }
            diagnostics = new DecisionDiagnostics(bestFoodScore, bestWaterScore, foodCandidates.Count > 0, waterCandidates.Count > 0)
                .WithPredationScores(fleeScore, huntScore)
                .WithCarcassScore(carcassScore)
                .WithThermalScore(thermalScore);
            if (!candidates.TryGetBest(out DecisionCandidate best) || best.Score < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            return ToDecision(best);
        }

        private static CreatureDecision ToDecision(DecisionCandidate candidate)
        {
            CreatureAction action;
            switch (candidate.Intent)
            {
                case CreatureIntent.SeekWater: action = CreatureAction.SeekWater; break;
                case CreatureIntent.SeekPrey: action = CreatureAction.SeekPrey; break;
                case CreatureIntent.Flee: action = CreatureAction.Flee; break;
                case CreatureIntent.SeekCarcass: action = CreatureAction.SeekCarcass; break;
                case CreatureIntent.SeekThermalComfort: action = CreatureAction.SeekThermalComfort; break;
                case CreatureIntent.SeekMate: action = CreatureAction.SeekMate; break;
                case CreatureIntent.Rest: action = CreatureAction.Rest; break;
                case CreatureIntent.Wander: action = CreatureAction.Wander; break;
                default: action = CreatureAction.SeekFood; break;
            }

            return new CreatureDecision(action, candidate.TargetResourceIndex, candidate.Score, targetCreatureId: candidate.TargetCreatureId);
        }

        private static void ScoreMate(
            CreatureNeeds needs,
            Phenotype phenotype,
            ReproductionState reproduction,
            CreatureObservation mate,
            CreatureNeeds mateNeeds,
            Phenotype matePhenotype,
            ReproductionState mateReproduction,
            ref DecisionCandidateBuffer candidates)
        {
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

        private static bool IsKin(CreatureId selfId, CreatureLineage selfLineage, CreatureId otherId, CreatureLineage otherLineage)
        {
            if (otherId.Equals(selfLineage.FirstParent) || otherId.Equals(selfLineage.SecondParent))
            {
                return true;
            }

            if (selfId.Equals(otherLineage.FirstParent) || selfId.Equals(otherLineage.SecondParent))
            {
                return true;
            }

            if (selfLineage.FirstParent.Value != 0
                && (selfLineage.FirstParent.Equals(otherLineage.FirstParent) || selfLineage.FirstParent.Equals(otherLineage.SecondParent)))
            {
                return true;
            }

            if (selfLineage.SecondParent.Value != 0
                && (selfLineage.SecondParent.Equals(otherLineage.FirstParent) || selfLineage.SecondParent.Equals(otherLineage.SecondParent)))
            {
                return true;
            }

            return false;
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
            ref DecisionCandidateBuffer candidates,
            ref float bestScore)
        {
            for (int index = 0; index < observations.Count; index++)
            {
                ResourceObservation observation = observations.GetAt(index);
                ResourceState resource = resources.GetAt(observation.ResourceIndex);
                float score = ResourceUtility(intent, needs, genome, phenotype, resource, observation.Distance, threat, threatIntensity);
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

        private static float ComputeNeedGain(bool seekingWater, CreatureNeeds needs, Phenotype phenotype, ResourceState resource)
        {
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float missing = Math.Max(0.0001f, capacity - current);
            float perUnitGain = seekingWater ? 20f : 20f * phenotype.FoodYield * resource.NutritionMultiplier;
            return Math.Min(1f, (resource.Amount * perUnitGain) / missing);
        }

        private static float ResourceUtility(
            CreatureIntent intent,
            CreatureNeeds needs,
            Genome genome,
            Phenotype phenotype,
            ResourceState resource,
            float distance,
            CreatureObservation threat,
            float threatIntensity)
        {
            bool seekingWater = intent == CreatureIntent.SeekWater;
            float capacity = seekingWater ? phenotype.HydrationCapacity : phenotype.EnergyCapacity;
            float current = seekingWater ? needs.Hydration : needs.Energy;
            float urgency = (float)Math.Pow(Urgency(current, capacity), 0.5f + (2.5f * genome.UrgencyExponent));
            float needGain = ComputeNeedGain(seekingWater, needs, phenotype, resource);
            float travelBurden = (0.5f + (1.5f * genome.TravelSensitivity)) * EstimateTravelBurden(distance, phenotype);
            float dangerPenalty = threat.IsValid ? Math.Max(0f, threatIntensity) * genome.RiskAversion * (distance / Math.Max(0.01f, phenotype.MaximumSpeed)) : 0f;
            return Math.Max(0f, (urgency * needGain) - travelBurden - dangerPenalty);
        }

        private static float EstimateTravelBurden(float distance, Phenotype phenotype)
        {
            float safeDistance = Math.Max(0f, distance);
            float travelTime = safeDistance / Math.Max(0.01f, phenotype.MaximumSpeed);
            float energyCost = (travelTime * phenotype.BasalEnergyCostMultiplier) + (safeDistance * phenotype.BodyMass * 0.5f);
            float hydrationCost = travelTime * phenotype.BodyMass * phenotype.DigestionRate * phenotype.WaterLossMultiplier * 0.75f;
            return 0.5f * ((energyCost / Math.Max(0.01f, phenotype.EnergyCapacity)) + (hydrationCost / Math.Max(0.01f, phenotype.HydrationCapacity)));
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation food,
            ResourceObservation water)
        {
            return Decide(needs, phenotype, food, water, out _);
        }

        public static CreatureDecision PreferRememberedResource(
            CreatureNeeds needs,
            Phenotype phenotype,
            MemoryState memory,
            CreatureDecision currentDecision,
            out SimVector2 rememberedTarget)
        {
            rememberedTarget = default;
            if (currentDecision.Action != CreatureAction.Wander)
            {
                return currentDecision;
            }

            float foodValue = KnownOutcomeOrCuriosity(memory.FoodOutcomeValue, memory.FoodExperienceCount, phenotype.Exploration);
            float waterValue = KnownOutcomeOrCuriosity(memory.WaterOutcomeValue, memory.WaterExperienceCount, phenotype.Exploration);
            float foodScore = Urgency(needs.Energy, phenotype.EnergyCapacity) * memory.FoodConfidence * foodValue;
            float waterScore = Urgency(needs.Hydration, phenotype.HydrationCapacity) * memory.WaterConfidence * waterValue;
            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return currentDecision;
            }

            if (waterScore > foodScore)
            {
                rememberedTarget = memory.WaterPosition;
                return new CreatureDecision(CreatureAction.SeekWater, -1, waterScore);
            }

            rememberedTarget = memory.FoodPosition;
            return new CreatureDecision(CreatureAction.SeekFood, -1, foodScore);
        }

        public static CreatureDecision DecideFromLearnedOutcomes(
            CreatureNeeds needs,
            Phenotype phenotype,
            MemoryState memory,
            ResourceObservation food,
            ResourceObservation water,
            ResourceStore resources,
            out DecisionDiagnostics diagnostics,
            bool learnedResourceQualityEnabled = false)
        {
            float foodValue = KnownOutcomeOrCuriosity(memory.FoodOutcomeValue, memory.FoodExperienceCount, phenotype.Exploration);
            float waterValue = KnownOutcomeOrCuriosity(memory.WaterOutcomeValue, memory.WaterExperienceCount, phenotype.Exploration);
            float foodNeedGain = learnedResourceQualityEnabled && food.IsValid
                ? ComputeNeedGain(false, needs, phenotype, resources.GetAt(food.ResourceIndex))
                : 1f;
            float waterNeedGain = learnedResourceQualityEnabled && water.IsValid
                ? ComputeNeedGain(true, needs, phenotype, resources.GetAt(water.ResourceIndex))
                : 1f;
            float foodScore = food.IsValid ? Urgency(needs.Energy, phenotype.EnergyCapacity) * Availability(food.Distance) * foodValue * foodNeedGain : -1f;
            float waterScore = water.IsValid ? Urgency(needs.Hydration, phenotype.HydrationCapacity) * Availability(water.Distance) * waterValue * waterNeedGain : -1f;
            diagnostics = new DecisionDiagnostics(foodScore, waterScore, food.IsValid, water.IsValid);
            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            return waterScore > foodScore
                ? new CreatureDecision(CreatureAction.SeekWater, water.ResourceIndex, waterScore)
                : new CreatureDecision(CreatureAction.SeekFood, food.ResourceIndex, foodScore);
        }

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceCandidateBuffer foodCandidates,
            ResourceCandidateBuffer waterCandidates,
            CreatureAction currentAction,
            float secondsInCurrentAction,
            float handlingSeconds,
            float referenceGain,
            float commitmentStrength,
            float commitmentHalfLifeSeconds,
            out DecisionDiagnostics diagnostics)
        {
            float foodScore = BestPatchScore(needs.Energy, phenotype.EnergyCapacity, phenotype, foodCandidates, handlingSeconds, referenceGain, out int foodResourceIndex);
            float waterScore = BestPatchScore(needs.Hydration, phenotype.HydrationCapacity, phenotype, waterCandidates, handlingSeconds, referenceGain, out int waterResourceIndex);

            if (foodResourceIndex >= 0 && (currentAction == CreatureAction.SeekFood || currentAction == CreatureAction.Eat))
            {
                foodScore += ForagingEconomics.CommitmentBonus(secondsInCurrentAction, phenotype.Persistence, commitmentStrength, commitmentHalfLifeSeconds);
            }
            else if (waterResourceIndex >= 0 && (currentAction == CreatureAction.SeekWater || currentAction == CreatureAction.Drink))
            {
                waterScore += ForagingEconomics.CommitmentBonus(secondsInCurrentAction, phenotype.Persistence, commitmentStrength, commitmentHalfLifeSeconds);
            }

            diagnostics = new DecisionDiagnostics(foodScore, waterScore, foodCandidates.Count > 0, waterCandidates.Count > 0);

            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            if (waterScore > foodScore && waterScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekWater, waterResourceIndex, waterScore);
            }

            if (foodScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekFood, foodResourceIndex, foodScore);
            }

            return new CreatureDecision(CreatureAction.Wander, -1, 0f);
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

        public static CreatureDecision Decide(
            CreatureNeeds needs,
            Phenotype phenotype,
            ResourceObservation food,
            ResourceObservation water,
            out DecisionDiagnostics diagnostics)
        {
            float foodScore = food.IsValid
                ? Urgency(needs.Energy, phenotype.EnergyCapacity) * Availability(food.Distance)
                : -1f;
            float waterScore = water.IsValid
                ? Urgency(needs.Hydration, phenotype.HydrationCapacity) * Availability(water.Distance)
                : -1f;
            diagnostics = new DecisionDiagnostics(foodScore, waterScore, food.IsValid, water.IsValid);

            if (Math.Max(foodScore, waterScore) < MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.Wander, -1, 0f);
            }

            if (waterScore > foodScore && waterScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekWater, water.ResourceIndex, waterScore);
            }

            if (foodScore >= MinimumUrgencyToSeekResource)
            {
                return new CreatureDecision(CreatureAction.SeekFood, food.ResourceIndex, foodScore);
            }

            return new CreatureDecision(CreatureAction.Wander, -1, 0f);
        }

        private static float Urgency(float current, float capacity)
        {
            if (capacity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            return Math.Max(0f, Math.Min(1f, 1f - (current / capacity)));
        }

        private static float Availability(float distance)
        {
            return 1f / (1f + Math.Max(0f, distance));
        }

        private static float KnownOutcomeOrCuriosity(float learnedValue, int experienceCount, float exploration)
        {
            return experienceCount > 0 && learnedValue >= 0.05f
                ? learnedValue
                : 0.5f + (0.5f * exploration);
        }

        public static float ComputeNeedGainForTest(bool seekingWater, CreatureNeeds needs, Phenotype phenotype, ResourceState resource)
        {
            return ComputeNeedGain(seekingWater, needs, phenotype, resource);
        }
    }
}
