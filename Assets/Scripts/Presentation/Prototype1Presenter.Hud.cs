using System;
using System.Collections.Generic;
using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Behavior;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Resources;
using UnityEngine;
using LifeSimulation.Simulation.World;

namespace LifeSimulation.Presentation
{
    public sealed partial class Prototype1Presenter : MonoBehaviour
    {

        private void OnGUI()
        {
            EnsureInitialized();

            // Drawn before the hidden-HUD early return: tuning terrain is exactly when the rest of
            // the HUD is in the way, so the panel has to survive H.
            _terrainTuningPanel.Draw(_terrainPreview, RebuildTerrainViews);

            if (_hudHidden)
            {
                // One line rather than nothing, so it is obvious the HUD is suppressed and how to
                // get it back.
                string what = _sphericalArena
                    ? "Planet view - paused"
                    : "Terrain viewer - " + (_terrainPreview == null ? string.Empty : _terrainPreview.Describe());
                GUI.Label(new Rect(12f, 12f, 760f, 22f),
                    $"{what}   |   J tuning, [ ] height, K flat views, O back to the arena");
                return;
            }

            GUI.Box(new Rect(12f, 12f, 440f, 284f), "LifeSimulation — Prototype 1");
            GUI.Label(new Rect(24f, 40f, 300f, 22f), $"Population: {_world.CreatureCount}    Tick: {_world.CurrentTick}");
            GUI.Label(new Rect(24f, 62f, 400f, 22f), $"Scenario: {_scenarioId}    Speed: {_speedMultiplier:0}x    {(_isPaused ? "Paused" : "Running")}");
            GUI.Label(new Rect(300f, 40f, 300f, 22f),
                $"O: {(_sphericalArena ? "on the planet" : "flat patch")}");
            if (_world != null && _world.Config.ElevationFieldEnabled)
            {
                // Below the Creature Inspector. These used to sit at 84/106/128, drawn on top of
                // the Generation, Food and Mean-genes lines - three pairs of labels over each other
                // in every configuration with the elevation field on, which is all of them.
                GUI.Label(new Rect(24f, 632f, 430f, 22f),
                    $"Height {_terrainHeightScale:0.0}  ([ lower, ] raise or PgDn/PgUp)");
                GUI.Label(new Rect(24f, 654f, 430f, 22f),
                    $"Smoothing {_terrainSmoothingRadius:0.0}  (, less, . more or -/=)");
                GUI.Label(new Rect(24f, 676f, 430f, 22f),
                    $"K view: {(_terrainPreview == null ? "off" : _terrainPreview.Describe())}   |   J tuning panel");

            }
            DrawSelectedCreatureInspector();
            DrawSelectedCreatureHistory();
            var stats = _world.Statistics;
            DrawPopulationCondition(stats);
            DrawP5HistoryPanel();
            GUI.Label(new Rect(24f, 84f, 400f, 22f), $"Generation: {stats.HighestGeneration}    Births: {stats.BirthCount}    Deaths: {stats.DeathCount}");
            GUI.Label(new Rect(24f, 106f, 400f, 22f), $"Food: {stats.AvailableFood:0.0}    Water: {stats.AvailableWater:0.0}");
            GUI.Label(new Rect(24f, 216f, 400f, 22f), $"Predation: {stats.AttackHitCount} hits  {stats.PredationDeathCount} kills  {stats.CumulativeCarcassConsumed:0.0} meat");
            GUI.Label(new Rect(24f, 238f, 420f, 22f),
                $"Died of: age {stats.AgeDeathCount} · hunger {stats.StarvationDeathCount} · thirst {stats.DehydrationDeathCount} · wounds {stats.HealthDeathCount} · hunted {stats.PredationDeathCount}");
            GUI.Label(new Rect(24f, 260f, 420f, 22f), $"Running away: {stats.FleeingFraction:P1} of all decisions");
            if (_world.Config.CognitionEnabled)
            {
                GUI.Label(new Rect(24f, 194f, 420f, 22f), $"Mean P2 genes: memory {stats.MeanMemoryCapacityGene:0.00} | retention {stats.MeanMemoryRetentionGene:0.00} | learning {stats.MeanLearningRateGene:0.00}");
            }
            if (_world.Config.PhysiologyEnabled)
            {
                GUI.Label(new Rect(24f, 172f, 420f, 22f), $"Mean P3 genes: temperature {stats.MeanTemperatureToleranceGene:0.00} | fertility {stats.MeanFertilityInvestmentGene:0.00} | lifespan {stats.MeanLifespanTendencyGene:0.00}");
            }
            GUI.Label(new Rect(24f, 128f, 420f, 22f), $"Mean genes: size {stats.MeanBodySizeGene:0.00} · speed {stats.MeanMovementSpeedGene:0.00} · metabolism {stats.MeanMetabolicPaceGene:0.00}");
            GUI.Label(new Rect(24f, 150f, 420f, 22f), $"Mean genes: vision {stats.MeanVisionRangeGene:0.00} · water {stats.MeanWaterEfficiencyGene:0.00} · food {stats.MeanFoodEfficiencyGene:0.00}");
            GUI.Label(new Rect(24f, 698f, 1240f, 22f), "Space pause · 1/2/4/8 speed · B/D/F resources · P predators · C cognition · T temperature · G foraging memory · H overlay");
            GUI.Label(new Rect(24f, 764f, 1240f, 22f), "Scenarios: E starter habitat · 5/6/7/9 watch · R home range · V shifting patches · M mating demo · Y terrain playtest · N every flag on");
            GUI.Label(new Rect(24f, 720f, 1240f, 22f), "O planet view (pauses, hides UI) · K flat terrain views · J tuning panel · Camera: hold right mouse to fly · WASD move · Q/E down/up · shift boost · alt slow · wheel speed · arrows move anytime · Home frames the arena");
            GUI.Label(new Rect(24f, 742f, 1240f, 22f), "Colors: green wander · gold food · blue water · purple mate · cyan flee · red hunt · brown carcass · grey rest · orange warmth");
        }

        private void DrawPopulationCondition(SimulationStatistics stats)
        {
            GUI.Box(new Rect(464f, 12f, 280f, 264f), "Population condition");
            GUI.Label(new Rect(476f, 40f, 250f, 22f), $"Energy: {stats.MeanEnergyFraction:P0}");
            GUI.Label(new Rect(476f, 62f, 250f, 22f), $"Hydration: {stats.MeanHydrationFraction:P0}");
            GUI.Label(new Rect(476f, 84f, 250f, 22f), $"Food eaten: {stats.CumulativeFoodConsumed:0.0}");
            GUI.Label(new Rect(476f, 106f, 250f, 22f), $"Water used: {stats.CumulativeWaterConsumed:0.0}");
            CountFertileAdults(out int fertileAdults, out int adultCount);
            GUI.Label(new Rect(476f, 128f, 250f, 22f), $"Ready to breed: {fertileAdults} of {adultCount} adults");
            GUI.Label(new Rect(476f, 150f, 250f, 22f), $"Deaths: food {stats.StarvationDeathCount}  water {stats.DehydrationDeathCount}");
            GUI.Label(new Rect(476f, 172f, 250f, 22f), _hasRecentEvent ? FormatRecentEvent() : "Latest event: waiting");
            if (_world.Config.FounderProfile == FounderProfile.PredationVariation)
            {
                GUI.Label(new Rect(476f, 194f, 250f, 22f), $"P1 cohorts: hunters {stats.ViableHunterCount}  others {stats.NonHunterCount}");
            }
            GUI.Label(new Rect(476f, 216f, 250f, 22f), "Watch: 5 stable · 6 scarce · 7 migration · 9 mating · R home range · V shifting");
            GUI.Label(new Rect(476f, 238f, 250f, 22f), _scenarioHint);
        }

        /// <summary>
        /// What the selected creature has been doing, rather than what it is doing right now. The
        /// inspector answers the second question already; this answers the first, which is the one
        /// that makes a commute, a failed foraging trip or a long thirsty wander legible while
        /// watching.
        /// </summary>
        private void DrawSelectedCreatureHistory()
        {
            const float panelX = 464f;
            const float panelY = 300f;
            const float lineHeight = 22f;
            const int maximumEpisodes = 6;

            GUI.Box(new Rect(panelX, panelY, 280f, 268f), "Selected creature history");

            if (!_hasSelectedCreature || !_selectedCreatureHistory.IsTracking)
            {
                GUI.Label(new Rect(panelX + 12f, panelY + 28f, 260f, lineHeight), "Click a creature to follow it.");
                return;
            }

            float y = panelY + 28f;
            if (!_selectedCreatureHistory.IsAlive)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), "This creature has died.");
                y += lineHeight;
            }
            else if (_selectedCreatureHistory.TryGetOpenEpisode(out CreatureActionEpisode current))
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), $"Now: {DescribeEpisode(current)}");
                y += lineHeight;
            }

            GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), $"Watched {DescribeSeconds(_selectedCreatureHistory.ObservedTicks)}, mostly {DescribeBusiestAction()}");
            y += lineHeight + 4f;

            if (_selectedCreatureHistory.EpisodeCount == 0)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), "No completed activity yet.");
                return;
            }

            int shown = Math.Min(maximumEpisodes, _selectedCreatureHistory.EpisodeCount);
            for (int index = 0; index < shown; index++)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), DescribeEpisode(_selectedCreatureHistory.GetEpisodeAt(index)));
                y += lineHeight;
            }

            int hidden = _selectedCreatureHistory.EpisodeCount - shown;
            if (hidden > 0)
            {
                GUI.Label(new Rect(panelX + 12f, y, 260f, lineHeight), $"{hidden} older activit{(hidden == 1 ? "y" : "ies")} hidden");
            }
        }

        private void DrawSelectedCreatureInspector()
        {
            const float inspectorTop = 300f;
            GUI.Box(new Rect(12f, inspectorTop, 440f, 324f), "Creature Inspector");
            if (!_hasSelectedCreature || !_world.TryGetCreatureIndex(_selectedCreature, out int index))
            {
                GUI.Label(new Rect(24f, inspectorTop + 26f, 350f, 22f), "Click a creature to inspect it.");
                return;
            }

            var needs = _world.GetCreatureNeedsAt(index);
            var phenotype = _world.Creatures.GetPhenotypeAt(index);
            var genome = _world.Creatures.GetGenomeAt(index);
            var lineage = _world.Creatures.GetLineageAt(index);
            var decision = _world.GetCreatureDecisionAt(index);
            var diagnostics = _world.GetCreatureDecisionDiagnosticsAt(index);
            MemoryState memory = _world.GetCreatureMemoryAt(index);
            GUI.Label(new Rect(24f, inspectorTop + 26f, 360f, 22f), $"Selected #{_selectedCreature.Value} | Gen {lineage.Generation} | {decision.Action}");
            GUI.Label(new Rect(24f, inspectorTop + 48f, 360f, 22f), $"Energy {needs.Energy:0}/{phenotype.EnergyCapacity:0} | Water {needs.Hydration:0}/{phenotype.HydrationCapacity:0}");
            GUI.Label(new Rect(24f, inspectorTop + 70f, 360f, 22f), $"Health {needs.Health:0}/{phenotype.HealthCapacity:0} | Age {needs.Age:0.0}s");
            GUI.Label(new Rect(24f, inspectorTop + 92f, 360f, 22f), $"Genes size {genome.BodySize:0.00} | speed {genome.MovementSpeed:0.00} | metabolism {genome.MetabolicPace:0.00}");
            GUI.Label(new Rect(24f, inspectorTop + 114f, 420f, 22f), $"Why: food {diagnostics.FoodScore:0.00} ({(diagnostics.FoodVisible ? "seen" : "unseen")}) | water {diagnostics.WaterScore:0.00} ({(diagnostics.WaterVisible ? "seen" : "unseen")})");
            GUI.Label(new Rect(24f, inspectorTop + 136f, 420f, 22f), $"Also: flee {diagnostics.FleeScore:0.00} | hunt {diagnostics.HuntScore:0.00} | carcass {diagnostics.CarcassScore:0.00} | warmth {diagnostics.ThermalScore:0.00}");
            GUI.Label(new Rect(24f, inspectorTop + 158f, 360f, 22f), $"Parents: {lineage.FirstParent.Value}, {lineage.SecondParent.Value}");
            GUI.Label(new Rect(24f, inspectorTop + 180f, 420f, 22f), $"Breeding: {DescribeBreedingReadiness(needs, phenotype, _world.Creatures.GetReproductionRefAt(index))}");
            float optionalDetailY = inspectorTop + 202f;
            if (_world.Config.FounderProfile == FounderProfile.PredationVariation)
            {
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"P1 traits: attack {genome.Attack:0.00} | defense {genome.Defense:0.00} | aggression {genome.Aggression:0.00} | diet {genome.DietSpecialization:0.00}");
                optionalDetailY += 22f;
            }
            if (_world.Config.CognitionEnabled)
            {
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"P2 traits: memory {genome.MemoryCapacity:0.00} | retention {genome.MemoryRetention:0.00} | learning {genome.LearningRate:0.00} | explore {genome.Exploration:0.00}");
                optionalDetailY += 22f;
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"Learned: food {memory.FoodOutcomeValue:0.00} ({memory.FoodExperienceCount}) | water {memory.WaterOutcomeValue:0.00} ({memory.WaterExperienceCount})");
                optionalDetailY += 22f;
            }
            if (_world.Config.PhysiologyEnabled)
            {
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"Temperature tolerance: {genome.TemperatureTolerance:0.00} | local field {TemperatureField.Sample(_world.GetCreatureMovementAt(index).Position, _world.CurrentTick):0.0} C");
                optionalDetailY += 22f;
                GUI.Label(new Rect(24f, optionalDetailY, 420f, 22f), $"Life history: fertility {genome.FertilityInvestment:0.00} | lifespan {genome.LifespanTendency:0.00} | max age {phenotype.MaximumAgeSeconds:0}s");
            }
        }
    }
}
