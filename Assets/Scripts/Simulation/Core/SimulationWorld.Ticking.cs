using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Resources;
using LifeSimulation.Simulation.World;
using LifeSimulation.Simulation.Spatial;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Diagnostics;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Core
{
    public sealed partial class SimulationWorld
    {

        public void Step(float fixedDeltaTime)
        {
            if (Math.Abs(fixedDeltaTime - Config.FixedDeltaTime) > 0.000001f)
            {
                throw new ArgumentException("Simulation steps must use the configured fixed delta.", nameof(fixedDeltaTime));
            }

            long nextTick = CurrentTick + 1;
            if (IsDue(nextTick, Config.Schedule.ResourcesHz))
            {
                float resourceDeltaTime = 1f / Config.Schedule.ResourcesHz;
                if (Config.PlantCohortsEnabled)
                {
                    Resources.RegenerateNonFood(resourceDeltaTime);
                    _cumulativePlantGrowth += PlantGrowthSystem.Step(Plants, Environment, resourceDeltaTime, Config.PlantTemperatureAdaptationEnabled, Config.PlantFertilityAdaptationEnabled);
                    _plantBirthCount += PlantReproductionSystem.Step(Plants, Resources, PlantSites, Config.WorldSeed, nextTick, resourceDeltaTime, ref _plantSeedOrdinal, Config.PlantSiteCompetitionEnabled, Config.PlantEstablishmentContestEnabled, Config.PlantInvaderEstablishmentContestEnabled, Config.PlantSeedProductionRateDispersalCharge, Config.PlantSeedProductionRateEnabled);
                    if (Config.PlantMortalityEnabled)
                    {
                        _cumulativePlantBiomassLostToMortality += PlantMortalitySystem.Step(Plants, Resources, resourceDeltaTime);
                    }

                    PlantGrowthSystem.ProjectFoodResources(Plants, Resources);

                    // Standing-biomass and patch-count time integrals. Read-only accumulators:
                    // nothing in the simulation consumes them, and they are deliberately absent
                    // from ComputeStateHash, so realized grazing pressure can be reported without
                    // perturbing the run it measures.
                    float standingBiomass = 0f;
                    for (int index = 0; index < Plants.Count; index++)
                    {
                        standingBiomass += Plants.GetAt(index).Biomass;
                    }

                    _plantBiomassSeconds += standingBiomass * resourceDeltaTime;
                    _plantPatchSeconds += Plants.Count * resourceDeltaTime;
                }
                else
                {
                    Resources.Regenerate(resourceDeltaTime);
                }
            }

            if (IsDue(nextTick, Config.Schedule.PerceptionHz))
            {
                RebuildResourceGrid();
                RebuildCombatGrid();
            }

            if (Config.ForagingEconomicsEnabled)
            {
                AdvanceForagingActionTime(Config.FixedDeltaTime);
            }

            if (Config.DecisionStaggerEnabled || IsDue(nextTick, Config.Schedule.DecisionsHz))
            {
                TickDecisions(nextTick);
            }

            TickMovement(nextTick);
            TickCombat(nextTick);
            if (IsDue(nextTick, Config.Schedule.NeedsHz))
            {
                TickNeeds();
            }

            ResolveResourceInteractions();

            if (Config.ForagingEconomicsEnabled)
            {
                UpdateForagingIntakeRate(Config.FixedDeltaTime);
            }

            if (IsDue(nextTick, Config.Schedule.ReproductionHz))
            {
                TickReproduction();
            }

            for (int index = 0; index < _pendingDeathCount; index++)
            {
                CreatureId deceased = _pendingDeaths[index];
                if (_pendingDeathCauses[index] == DeathCause.Predation
                    && Creatures.TryGetIndex(deceased, out int deceasedIndex))
                {
                    Phenotype phenotype = Creatures.GetPhenotypeAt(deceasedIndex);
                    Resources.Add(
                        ResourceKind.Carcass,
                        _pendingDeathPositions[index],
                        interactionRadius: 1.1f,
                        initialAmount: 10f * phenotype.BodyMass,
                        capacity: 10f * phenotype.BodyMass,
                        regenerationPerSecond: 0f);
                }

                if (Creatures.Remove(deceased))
                {
                    Events.TryWrite(new SimulationEvent(
                        nextTick,
                        SimulationEventKind.Death,
                        deceased,
                        default,
                        default,
                        _pendingDeathCauses[index]));
                    _deathCount++;
                    CountDeathCause(_pendingDeathCauses[index]);
                }
            }

            _pendingDeathCount = 0;

            // Statistics are sampled AFTER the tick's deaths are committed. Sampling first
            // reports a population that still contains creatures this tick removed and a death
            // count that excludes them, which silently understates mortality and can report a
            // surviving population on the tick a world went extinct. Moving the commit earlier
            // changes only what the sample observes: nothing between here and the end of Step
            // reads the creature set, so the simulation trajectory and event semantics are
            // unchanged.
            if (IsDue(nextTick, Config.Schedule.StatisticsHz))
            {
                Statistics = BuildStatistics(nextTick);
            }

            CurrentTick = nextTick;
        }

        private void TickNeeds()
        {
            float deltaTime = 1f / Config.Schedule.NeedsHz;
            for (int index = 0; index < Creatures.Count; index++)
            {
                ref CreatureNeeds needs = ref Creatures.GetNeedsRefAt(index);
                ref MovementState movement = ref Creatures.GetMovementRefAt(index);
                bool isResting = Config.RestBehaviorEnabled && Creatures.GetDecisionAt(index).Action == CreatureAction.Rest;
                NeedsSystem.Tick(ref needs, Creatures.GetPhenotypeAt(index), deltaTime, movement.DistanceSinceLastNeeds, Config.RestBehaviorEnabled, isResting);
                if (Config.PhysiologyEnabled)
                {
                    NeedsSystem.ApplyTemperatureStress(ref needs, Creatures.GetPhenotypeAt(index), Climate.Celsius(movement.Position, CurrentTick + 1), deltaTime);
                }
                if (Config.HealthRecoveryEnabled)
                {
                    // After the damage, deliberately: a creature standing in a hot band nets a loss
                    // and only makes the ground back once it leaves.
                    float metabolicScale = Config.MetabolicHealingEnabled
                        ? 0.7f + (0.8f * Creatures.GetGenomeAt(index).MetabolicPace)
                        : 1f;
                    NeedsSystem.RecoverHealth(ref needs, Creatures.GetPhenotypeAt(index), deltaTime, metabolicScale);
                }
                movement.DistanceSinceLastNeeds = 0f;
                if (needs.Health <= 0f)
                {
                    DeathCause cause = needs.Hydration <= 0f
                        ? DeathCause.Dehydration
                        : needs.Energy <= 0f ? DeathCause.Starvation : DeathCause.Health;
                    RequestDeath(Creatures.GetIdAt(index), cause);
                }
                else if (Config.PhysiologyEnabled && needs.Age >= Creatures.GetPhenotypeAt(index).MaximumAgeSeconds)
                {
                    RequestDeath(Creatures.GetIdAt(index), DeathCause.Age);
                }

                if (Config.CognitionEnabled)
                {
                    MemorySystem.TickDecay(
                        ref Creatures.GetMemoryRefAt(index),
                        deltaTime,
                        Creatures.GetPhenotypeAt(index).MemoryConfidenceDecayPerSecond);
                }

                if (Config.HomeRangeAffinityEnabled)
                {
                    HomeRangeSystem.TickDecay(ref Creatures.GetHomeRangeRefAt(index), deltaTime);
                }
            }
        }

        private void TickMovement(long nextTick)
        {
            for (int index = 0; index < Creatures.Count; index++)
            {
                CreatureId id = Creatures.GetIdAt(index);
                ref MovementState movement = ref Creatures.GetMovementRefAt(index);
                SimVector2 target = GetMovementTarget(index, id, nextTick, movement.Position);
                MovementSystem.MoveToward(
                    ref movement,
                    target,
                    GetEffectivePhenotype(index).MaximumSpeed,
                    Config.FixedDeltaTime,
                    Arena);

                ChargeForClimbing(ref movement);
            }
        }

        /// <summary>
        /// Bill a creature for the height it just gained.
        ///
        /// <para>Energy drain is already proportional to <c>DistanceSinceLastNeeds</c>, so the whole
        /// of "a hill costs something" is charging a climb as extra distance. Descending is free:
        /// going downhill is cheaper than level ground for a real animal only within a narrow range
        /// of gradients, and paying creatures to walk downhill is a strange incentive to introduce by
        /// accident.</para>
        ///
        /// <para>Off, this method reads nothing and writes nothing, which is what keeps the flag
        /// byte-identical rather than merely close.</para>
        /// </summary>
        private void ChargeForClimbing(ref MovementState movement)
        {
            if (!Config.SlopeMovementCostEnabled) return;

            float climb = Environment.Sample(movement.Position).Elevation
                - Environment.Sample(movement.PreviousPosition).Elevation;
            if (climb <= 0f) return;

            movement.DistanceSinceLastNeeds +=
                climb * PlanetTerrain.MetresPerElevationUnit * SimulationConfig.SlopeClimbCost;
        }

        private void TickDecisions(long tick)
        {
            int interval = Config.Schedule.BaseFrequencyHz / Config.Schedule.DecisionsHz;
            for (int index = 0; index < Creatures.Count; index++)
            {
                if (Config.DecisionStaggerEnabled && (tick + index) % interval != 0)
                {
                    continue;
                }

                MovementState movement = Creatures.GetMovementAt(index);
                Phenotype phenotype = GetEffectivePhenotype(index);
                CreatureLineage selfLineage = Creatures.GetLineageAt(index);
                CreatureDecision previousDecision = Creatures.GetDecisionAt(index);
                ResourceObservation food = PerceptionSystem.FindNearestAvailableResource(
                    Resources,
                    ResourceGrid,
                    movement.Position,
                    phenotype.VisionRange,
                    ResourceKind.Food);
                ResourceObservation water = PerceptionSystem.FindNearestAvailableResource(
                    Resources,
                    ResourceGrid,
                    movement.Position,
                    phenotype.VisionRange,
                    ResourceKind.Water);
                ResourceObservation carcass = PerceptionSystem.FindNearestAvailableResource(
                    Resources,
                    ResourceGrid,
                    movement.Position,
                    phenotype.VisionRange,
                    ResourceKind.Carcass);
                var foodCandidates = new ResourceCandidateBuffer();
                var waterCandidates = new ResourceCandidateBuffer();
                CreatureObservation other = default;
                float threatIntensity = 0f;
                if (Config.ForagingEconomicsEnabled && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy && !Config.CognitionEnabled)
                {
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Food, ref foodCandidates);
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Water, ref waterCandidates);
                }
                if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                {
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Food, ref foodCandidates);
                    PerceptionSystem.FindAvailableResources(Resources, ResourceGrid, movement.Position, phenotype.VisionRange, ResourceKind.Water, ref waterCandidates);
                    if (Config.FounderProfile == FounderProfile.PredationVariation
                        || Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                    {
                        other = PerceptionSystem.FindNearestOtherCreature(Creatures, CombatGrid, movement.Position, phenotype.VisionRange, Creatures.GetIdAt(index));
                        if (other.IsValid)
                        {
                            threatIntensity = PredationSystem.Threat(Creatures.GetPhenotypeAt(other.CreatureIndex), phenotype, other.Distance, Config.PredationEconomicsEnabled);
                        }
                    }
                }
                var otherCandidates = new PredationCandidateBuffer();
                if (Config.MultiThreatPerceptionEnabled)
                {
                    var creatureCandidates = new CreatureCandidateBuffer();
                    PerceptionSystem.FindOtherCreatures(Creatures, CombatGrid, movement.Position, phenotype.VisionRange, Creatures.GetIdAt(index), ref creatureCandidates);
                    for (int candidateIndex = 0; candidateIndex < creatureCandidates.Count; candidateIndex++)
                    {
                        CreatureObservation candidateObservation = creatureCandidates.GetAt(candidateIndex);
                        otherCandidates.Add(candidateObservation, Creatures.GetPhenotypeAt(candidateObservation.CreatureIndex), Creatures.GetLineageAt(candidateObservation.CreatureIndex));
                    }
                }
                if (Config.CognitionEnabled)
                {
                    ref MemoryState memory = ref Creatures.GetMemoryRefAt(index);
                    if (memory.HasActiveRememberedTarget
                        && SimVector2.Distance(movement.Position, memory.ActiveRememberedTarget) <= 1f
                        && ((previousDecision.Action == CreatureAction.SeekFood && !food.IsValid)
                            || (previousDecision.Action == CreatureAction.SeekWater && !water.IsValid)))
                    {
                        MemorySystem.RecordFailedSearch(
                            ref memory,
                            previousDecision.Action == CreatureAction.SeekFood ? ResourceKind.Food : ResourceKind.Water);
                    }

                    if (food.IsValid) MemorySystem.RememberResource(ref memory, ResourceKind.Food, Resources.GetAt(food.ResourceIndex).Position);
                    if (water.IsValid) MemorySystem.RememberResource(ref memory, ResourceKind.Water, Resources.GetAt(water.ResourceIndex).Position);
                }
                if (Config.CognitionEnabled
                    && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy
                    && previousDecision.TargetResourceIndex < 0
                    && (previousDecision.Action == CreatureAction.SeekFood || previousDecision.Action == CreatureAction.SeekWater))
                {
                    // TargetResourceIndex < 0 with a Seek action, under the Legacy policy, only ever
                    // comes from the remembered-place override below - a real visible target always
                    // carries its resource index. Arriving there and still seeing nothing of that kind
                    // means the remembered place was wrong; RecordFailedPlaceSearch itself is a no-op
                    // until the creature is actually within SamePlaceRadius of a matching slot.
                    ResourceKind previousKind = previousDecision.Action == CreatureAction.SeekFood ? ResourceKind.Food : ResourceKind.Water;
                    bool nothingOfThatKindVisible = previousKind == ResourceKind.Food ? !food.IsValid : !water.IsValid;
                    if (nothingOfThatKindVisible)
                    {
                        int usableSlotCount = GetUsableMemorySlotCount(Creatures.GetGenomeAt(index));
                        MemoryState beforeFailedSearch = Creatures.GetMemoryRefAt(index);
                        MemorySystem.RecordFailedPlaceSearch(Creatures, index, usableSlotCount, movement.Position, previousKind, Config.SamePlaceRadius);

                        // Effective only if the call actually altered a slot's confidence. It runs
                        // whenever the branch is reached, but cannot match a slot while place memory
                        // is never populated, so the expected verdict is INERT rather than UNREACHED.
                        MemoryState afterFailedSearch = Creatures.GetMemoryRefAt(index);
                        Liveness?.RecordOutcome(
                            LivenessProbe.FailedPlaceSearch,
                            afterFailedSearch.FoodConfidence != beforeFailedSearch.FoodConfidence
                                || afterFailedSearch.WaterConfidence != beforeFailedSearch.WaterConfidence);
                    }
                }
                DecisionDiagnostics diagnostics;
                CreatureDecision decision;
                if (Config.DecisionPolicyVersion == DecisionPolicyVersion.IntentUtilityV1)
                {
                    decision = DecisionSystem.DecideIntentUtilityV1(
                        Creatures.GetNeedsAt(index),
                        Creatures.GetGenomeAt(index),
                        phenotype,
                        Resources,
                        movement.Position,
                        foodCandidates,
                        waterCandidates,
                        carcass,
                        Creatures.GetMemoryRefAt(index),
                        Config.CognitionEnabled,
                        other,
                        threatIntensity,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        Config.FounderProfile == FounderProfile.PredationVariation,
                        Config.PhysiologyEnabled,
                        Creatures.GetReproductionRefAt(index),
                        other,
                        other.IsValid ? Creatures.GetNeedsAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetPhenotypeAt(other.CreatureIndex) : default,
                        other.IsValid ? Creatures.GetReproductionRefAt(other.CreatureIndex) : default,
                        true,
                        tick,
                        out diagnostics,
                        Config.PredationEconomicsEnabled,
                        Config.ThreatFalloffDistance,
                        otherCandidates,
                        Config.MultiThreatPerceptionEnabled,
                        Config.RestBehaviorEnabled,
                        Creatures.GetIdAt(index),
                        selfLineage,
                        other.IsValid ? Creatures.GetLineageAt(other.CreatureIndex) : default,
                        Config.KinRecognitionEnabled,
                        Config.PlantQualityPreferenceEnabled,
                        Config.SafetyGatedMateRendezvousEnabled,
                        Config.HomeRangeAffinityEnabled ? Creatures.GetHomeRangeRefAt(index) : default,
                        Config.HomeRangeAffinityEnabled,
                        Climate,
                        Config.ReproductionNeedFraction);
                    if (Config.CognitionEnabled)
                    {
                        ref MemoryState memory = ref Creatures.GetMemoryRefAt(index);
                        memory.HasActiveRememberedTarget = false;
                        if (decision.TargetResourceIndex < 0 && decision.Action == CreatureAction.SeekFood)
                        {
                            memory.ActiveRememberedTarget = memory.FoodPosition;
                            memory.HasActiveRememberedTarget = true;
                        }
                        else if (decision.TargetResourceIndex < 0 && decision.Action == CreatureAction.SeekWater)
                        {
                            memory.ActiveRememberedTarget = memory.WaterPosition;
                            memory.HasActiveRememberedTarget = true;
                        }
                        if (decision.Action == CreatureAction.Flee && other.IsValid)
                        {
                            MemorySystem.RememberThreat(ref memory, Creatures.GetMovementAt(other.CreatureIndex).Position);
                        }
                    }
                }
                else if (Config.CognitionEnabled)
                {
                    decision = DecisionSystem.DecideFromLearnedOutcomes(Creatures.GetNeedsAt(index), phenotype, Creatures.GetMemoryRefAt(index), food, water, Resources, out diagnostics, Config.LearnedResourceQualityEnabled);
                }
                else if (Config.ForagingEconomicsEnabled)
                {
                    decision = DecisionSystem.Decide(
                        Creatures.GetNeedsAt(index),
                        phenotype,
                        foodCandidates,
                        waterCandidates,
                        previousDecision.Action,
                        Creatures.GetForagingRefAt(index).SecondsInCurrentAction,
                        Config.HandlingSeconds,
                        Config.ReferenceGain,
                        Config.CommitmentStrength,
                        Config.CommitmentHalfLifeSeconds,
                        out diagnostics);
                }
                else
                {
                    decision = DecisionSystem.Decide(Creatures.GetNeedsAt(index), phenotype, food, water, out diagnostics);
                }
                if (Config.ForagingEconomicsEnabled
                    && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy
                    && !Config.CognitionEnabled
                    && previousDecision.Action == CreatureAction.Eat)
                {
                    float currentPatchIntakeRate = index < _foragingEnergyGained.Length
                        ? _foragingEnergyGained[index] / Config.FixedDeltaTime
                        : 0f;
                    float recentIntakeRate = Creatures.GetForagingRefAt(index).RecentIntakeRate;
                    if (ForagingEconomics.ShouldAbandon(currentPatchIntakeRate, recentIntakeRate, phenotype.Persistence, Config.GiveUpSensitivity))
                    {
                        decision = new CreatureDecision(CreatureAction.SeekFood, -1, decision.Score);
                    }
                }
                if (Config.CognitionEnabled && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy)
                {
                    // Remembered places only compete once nothing visible was suitable: a visible
                    // patch already won the primary decision above, and confidence (<=1) discounting
                    // an identical remembered PatchScore means a real, currently-seen option always
                    // beats an equally good memory of one - it never needs to be scored to lose.
                    ref MemoryState memory = ref Creatures.GetMemoryRefAt(index);
                    memory.HasActiveRememberedTarget = false;
                    if (decision.Action == CreatureAction.Wander)
                    {
                        Genome genome = Creatures.GetGenomeAt(index);
                        int usableSlotCount = GetUsableMemorySlotCount(genome);
                        bool foundRememberedPlace = TryScoreBestRememberedPlace(
                            index,
                            usableSlotCount,
                            phenotype,
                            Creatures.GetNeedsAt(index),
                            movement.Position,
                            memory.ThreatPosition,
                            memory.ThreatConfidence,
                            out SimVector2 bestPosition,
                            out ResourceKind bestKind,
                            out float bestScore);

                        // Reached whenever a creature wanders; effective only if a populated slot
                        // actually produced a score. Expected INERT while ObservePlace has no
                        // production caller, which is the whole point of tracking it.
                        Liveness?.RecordOutcome(LivenessProbe.PlaceMemoryScoring, foundRememberedPlace);
                        if (foundRememberedPlace && bestScore >= RememberedPlaceMinimumScore)
                        {
                            decision = new CreatureDecision(
                                bestKind == ResourceKind.Water ? CreatureAction.SeekWater : CreatureAction.SeekFood,
                                -1,
                                bestScore);
                            memory.ActiveRememberedTarget = bestPosition;
                            memory.HasActiveRememberedTarget = true;
                        }
                    }
                }
                if (Config.FounderProfile == FounderProfile.PredationVariation && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy)
                {
                    if (!other.IsValid)
                    {
                        other = PerceptionSystem.FindNearestOtherCreature(
                            Creatures,
                            CombatGrid,
                            movement.Position,
                            phenotype.VisionRange,
                            Creatures.GetIdAt(index));
                    }
                    if (other.IsValid)
                    {
                        decision = PredationSystem.Decide(
                            Creatures.GetNeedsAt(index),
                            phenotype,
                            Creatures.GetPhenotypeAt(other.CreatureIndex),
                            other,
                            decision,
                            ref diagnostics,
                            Config.PredationEconomicsEnabled);
                        if (Config.CognitionEnabled && decision.Action == CreatureAction.Flee)
                        {
                            MemorySystem.RememberThreat(ref Creatures.GetMemoryRefAt(index), Creatures.GetMovementAt(other.CreatureIndex).Position);
                        }
                    }

                    decision = PredationSystem.PreferCarcassWhenUseful(
                        Creatures.GetNeedsAt(index),
                        phenotype,
                        carcass,
                        decision,
                        ref diagnostics);
                }
                if (Config.PhysiologyEnabled && Config.DecisionPolicyVersion == DecisionPolicyVersion.Legacy)
                {
                    decision = ThermoregulationSystem.PreferThermalComfort(phenotype, movement.Position, tick, decision, ref diagnostics, Climate);
                }
                CreatureDecision selectedIntent = decision;
                if (Config.CognitionEnabled
                    && decision.TargetResourceIndex < 0
                    && (decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.SeekWater))
                {
                    ResourceObservation visibleResource = decision.Action == CreatureAction.SeekFood ? food : water;
                    if (visibleResource.IsValid)
                    {
                        ResourceState resource = Resources.GetAt(visibleResource.ResourceIndex);
                        MemoryState memory = Creatures.GetMemoryRefAt(index);
                        SimVector2 rememberedTarget = decision.Action == CreatureAction.SeekFood ? memory.FoodPosition : memory.WaterPosition;
                        if (SimVector2.Distance(rememberedTarget, resource.Position) <= resource.InteractionRadius)
                        {
                            decision = new CreatureDecision(decision.Action, visibleResource.ResourceIndex, decision.Score);
                        }
                    }
                }
                if ((decision.Action == CreatureAction.SeekFood || decision.Action == CreatureAction.SeekWater)
                    && (uint)decision.TargetResourceIndex < (uint)Resources.Count)
                {
                    ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                    if (SimVector2.Distance(movement.Position, resource.Position) <= resource.InteractionRadius)
                    {
                        decision = new CreatureDecision(
                            resource.Kind == ResourceKind.Food ? CreatureAction.Eat : CreatureAction.Drink,
                            decision.TargetResourceIndex,
                            decision.Score);
                    }
                }

                if (decision.Action == CreatureAction.SeekCarcass
                    && (uint)decision.TargetResourceIndex < (uint)Resources.Count)
                {
                    ResourceState resource = Resources.GetAt(decision.TargetResourceIndex);
                    if (resource.Kind == ResourceKind.Carcass
                        && SimVector2.Distance(movement.Position, resource.Position) <= resource.InteractionRadius)
                    {
                        decision = new CreatureDecision(CreatureAction.FeedCarcass, decision.TargetResourceIndex, decision.Score);
                    }
                }

                if (decision.Action == CreatureAction.SeekPrey
                    && Creatures.TryGetIndex(decision.TargetCreatureId, out int preyIndex)
                    && SimVector2.Distance(movement.Position, Creatures.GetMovementAt(preyIndex).Position) <= 1.1f)
                {
                    decision = new CreatureDecision(
                        CreatureAction.Attack,
                        -1,
                        decision.Score,
                        targetCreatureId: decision.TargetCreatureId);
                }

                Creatures.SetDecisionAt(index, new CreatureDecision(
                    decision.Action,
                    decision.TargetResourceIndex,
                    decision.Score,
                    tick,
                    decision.TargetCreatureId));
                if (Config.ForagingEconomicsEnabled && previousDecision.Action != decision.Action)
                {
                    Creatures.GetForagingRefAt(index).SecondsInCurrentAction = 0f;
                }
                Creatures.SetDecisionDiagnosticsAt(index, diagnostics.WithWinningAction(decision.Action));
                if (DecisionTrace != null)
                {
                    DecisionInvalidationReason invalidationReason = DetermineDecisionInvalidation(previousDecision, selectedIntent, decision);
                    DecisionTrace.Record(new DecisionTraceEntry(tick, Creatures.GetIdAt(index), previousDecision, decision, diagnostics, invalidationReason));
                }
            }
        }

        private void TickReproduction()
        {
            long reproductionTick = CurrentTick + 1;
            _birthCount += _reproduction.Step(
                Config.WorldSeed,
                1f / Config.Schedule.ReproductionHz,
                ref _birthOrdinal,
                reproductionTick,
                Config.MaximumPopulation,
                Events);
            if (!Config.HomeRangeAffinityEnabled)
            {
                return;
            }

            for (int index = 0; index < Creatures.Count; index++)
            {
                CreatureDecision decision = Creatures.GetDecisionAt(index);
                if (decision.Action == CreatureAction.Reproduce && decision.DecisionTick == reproductionTick)
                {
                    HomeRangeSystem.RecordSuccess(
                        ref Creatures.GetHomeRangeRefAt(index),
                        Creatures.GetMovementAt(index).Position);
                }
            }
        }

        private void TickCombat(long tick)
        {
            EnsureCombatDamageCapacity(Creatures.Count);
            Array.Clear(_combatDamage, 0, Creatures.Count);
            for (int index = 0; index < Creatures.Count; index++)
            {
                ref CombatState combat = ref Creatures.GetCombatRefAt(index);
                combat.AttackRecoveryRemaining = Math.Max(0f, combat.AttackRecoveryRemaining - Config.FixedDeltaTime);
                CreatureDecision decision = Creatures.GetDecisionAt(index);
                if (decision.Action != CreatureAction.Attack
                    || combat.AttackRecoveryRemaining > 0f
                    || !Creatures.TryGetIndex(decision.TargetCreatureId, out int targetIndex)
                    || targetIndex == index)
                {
                    continue;
                }

                MovementState attackerMovement = Creatures.GetMovementAt(index);
                MovementState defenderMovement = Creatures.GetMovementAt(targetIndex);
                float engagementDistance = SimVector2.Distance(attackerMovement.Position, defenderMovement.Position);
                if (engagementDistance > 1.1f)
                {
                    continue;
                }

                Phenotype attacker = GetEffectivePhenotype(index);
                Phenotype defender = GetEffectivePhenotype(targetIndex);
                float hitChance = 0.20f + (0.70f * PredationSystem.Threat(attacker, defender, engagementDistance, Config.PredationEconomicsEnabled));
                float roll = DeterministicRandom.Float01(
                    Config.WorldSeed,
                    RandomDomain.AttackResolution,
                    tick,
                    Creatures.GetIdAt(index).Value,
                    decision.TargetCreatureId.Value,
                    0);
                combat.AttackRecoveryRemaining = 0.75f;
                if (roll > hitChance)
                {
                    continue;
                }

                float damage = 4f + (12f * attacker.AttackPower);
                _combatDamage[targetIndex] += damage;
                _attackHitCount++;
            }

            for (int index = 0; index < Creatures.Count; index++)
            {
                float damage = _combatDamage[index];
                if (damage <= 0f)
                {
                    continue;
                }

                Phenotype defender = GetEffectivePhenotype(index);
                ref CreatureNeeds targetNeeds = ref Creatures.GetNeedsRefAt(index);
                ref CombatState targetCombat = ref Creatures.GetCombatRefAt(index);
                targetNeeds.Health -= damage;
                _cumulativeCombatDamage += damage;
                targetCombat.WoundSeverity += damage / defender.HealthCapacity;
                if (targetNeeds.Health <= 0f) RequestDeath(Creatures.GetIdAt(index), DeathCause.Predation);
            }
        }
    }
}
