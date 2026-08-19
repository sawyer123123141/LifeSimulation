using System;
using System.Text;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Environment;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Diagnostics
{
    public readonly struct PlantGeneLivenessResult
    {
        public PlantGeneLivenessResult(int traitIndex, string traitName, bool reachesBehavior, long divergedAtTick)
        {
            TraitIndex = traitIndex;
            TraitName = traitName;
            ReachesBehavior = reachesBehavior;
            DivergedAtTick = divergedAtTick;
        }

        public int TraitIndex { get; }
        public string TraitName { get; }

        /// <summary>False means no configuration of this plant gene changed anything the simulation does.</summary>
        public bool ReachesBehavior { get; }

        public long DivergedAtTick { get; }
    }

    /// <summary>
    /// Whether each <see cref="PlantGenome"/> trait actually reaches behavior, by perturbation.
    /// The plant-side counterpart of <see cref="GeneLivenessAnalysis"/>.
    ///
    /// <para>This exists because the animal-side harness does not cover plant genes, and that gap hid
    /// a real defect: plant <c>TemperatureTolerance</c> has no production reader that rewards it — it
    /// only pays a <c>-.10f</c> growth penalty in <c>PlantPhenotype</c> — because
    /// <c>EnvironmentField.Sample</c> returns <c>Temperature = 1</c> on every production path. Same
    /// for <c>Fertility</c>, which is pinned at 1 and therefore never constrains growth. A pure-cost
    /// gene is the <c>Defense</c> shape all over again, and a caller-search does not reveal it
    /// because the readers exist — they are just fed constants.</para>
    ///
    /// <para>Method matches the animal harness: perturb one trait across all founder patches, then
    /// compare <see cref="SimulationWorld.ComputeBehaviorHash"/> tick by tick. That hash covers plant
    /// biomass and generation but no genome field, so it moves only if the gene influenced something.</para>
    ///
    /// <para>Same scoping caveat: an "inert" verdict is relative to the scenario and environment
    /// tested. A gene that adapts to spatial variation reads inert in an environment that has none —
    /// which is exactly the finding here, and is a statement about the environment, not the gene.</para>
    /// </summary>
    public static class PlantGeneLivenessAnalysis
    {
        private static readonly float[] PerturbationValues = { 0f, 1f };

        public static PlantGeneLivenessResult[] Analyze(
            Func<SimulationConfig> configFactory,
            SimulationScenario scenario,
            int ticks)
        {
            if (configFactory == null) throw new ArgumentNullException(nameof(configFactory));
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (ticks <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));

            var results = new PlantGeneLivenessResult[PlantGenome.TraitCount];
            for (int traitIndex = 0; traitIndex < PlantGenome.TraitCount; traitIndex++)
            {
                long divergedAtTick = -1;
                foreach (float perturbation in PerturbationValues)
                {
                    long tick = FindDivergenceTick(configFactory, scenario, ticks, traitIndex, perturbation);
                    if (tick >= 0)
                    {
                        divergedAtTick = tick;
                        break;
                    }
                }

                results[traitIndex] = new PlantGeneLivenessResult(
                    traitIndex,
                    PlantGenome.TraitName(traitIndex),
                    divergedAtTick >= 0,
                    divergedAtTick);
            }

            return results;
        }

        private static long FindDivergenceTick(
            Func<SimulationConfig> configFactory,
            SimulationScenario scenario,
            int ticks,
            int traitIndex,
            float perturbation)
        {
            SimulationConfig baselineConfig = configFactory();
            SimulationConfig perturbedConfig = configFactory();

            var baseline = new SimulationWorld(baselineConfig);
            scenario.ApplyTo(baseline);

            var perturbed = new SimulationWorld(perturbedConfig);
            scenario.ApplyTo(perturbed);
            perturbed.OverwritePlantTraitForAllPatches(traitIndex, perturbation);

            for (int step = 0; step < ticks; step++)
            {
                baseline.Step(baselineConfig.FixedDeltaTime);
                perturbed.Step(perturbedConfig.FixedDeltaTime);
                baseline.Events.Clear();
                perturbed.Events.Clear();

                if (baseline.ComputeBehaviorHash() != perturbed.ComputeBehaviorHash())
                {
                    return baseline.CurrentTick;
                }
            }

            return -1;
        }

        public static string Report(PlantGeneLivenessResult[] results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            var builder = new StringBuilder();
            builder.AppendLine("idx | plant trait          | reaches behavior | first diverged at tick");
            foreach (PlantGeneLivenessResult result in results)
            {
                builder.AppendLine(
                    $"{result.TraitIndex,3} | {result.TraitName,-20} | {(result.ReachesBehavior ? "yes" : "NO  "),-16} | {(result.DivergedAtTick < 0 ? "never" : result.DivergedAtTick.ToString())}");
            }

            return builder.ToString();
        }
    }
}
