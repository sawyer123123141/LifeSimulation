using System;
using System.Text;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Diagnostics
{
    public readonly struct GeneLivenessResult
    {
        public GeneLivenessResult(int traitIndex, string traitName, bool reachesBehavior, long divergedAtTick)
        {
            TraitIndex = traitIndex;
            TraitName = traitName;
            ReachesBehavior = reachesBehavior;
            DivergedAtTick = divergedAtTick;
        }

        public int TraitIndex { get; }
        public string TraitName { get; }

        /// <summary>False means no configuration of this gene changed anything the simulation does.</summary>
        public bool ReachesBehavior { get; }

        /// <summary>Tick at which the behavior hash first diverged, or -1 if it never did.</summary>
        public long DivergedAtTick { get; }
    }

    /// <summary>
    /// Decides whether each gene actually reaches behavior, by perturbation rather than by reading
    /// call sites.
    ///
    /// Method: run a scenario twice from the same seed. In the second run, overwrite one trait
    /// across all founders before the first step. Compare <see cref="SimulationWorld.ComputeBehaviorHash"/>
    /// — which excludes every genome and phenotype field — after each step. If the two runs stay
    /// hash-identical for the whole run, that gene influenced nothing.
    ///
    /// This is the authority on gene liveness, in preference to both a caller-search and the
    /// runtime <see cref="LivenessRecorder"/>. A caller-search cannot see a value that is computed
    /// and then consumed by nobody, and the recorder cannot instrument a consumption site that does
    /// not exist. Perturbation needs neither: it asks the simulation directly.
    ///
    /// Caveat worth stating: a false verdict is only as strong as the scenario. A gene that matters
    /// solely under predation will read as dead in a herbivore-only scenario. Run this against the
    /// widest scenario available — that is what FULL ecosystem mode is for — and read a "does not
    /// reach behavior" result as scoped to the scenario tested.
    /// </summary>
    public static class GeneLivenessAnalysis
    {
        /// <summary>
        /// Perturbation values tried per trait. Two are used because a single value can coincide
        /// with the founder value for some profiles, which would silently test nothing.
        /// </summary>
        private static readonly float[] PerturbationValues = { 0f, 1f };

        public static GeneLivenessResult[] Analyze(
            Func<SimulationConfig> configFactory,
            SimulationScenario scenario,
            int ticks)
        {
            if (configFactory == null) throw new ArgumentNullException(nameof(configFactory));
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (ticks <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));

            var results = new GeneLivenessResult[Genome.TraitCount];
            for (int traitIndex = 0; traitIndex < Genome.TraitCount; traitIndex++)
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

                results[traitIndex] = new GeneLivenessResult(
                    traitIndex,
                    Genome.TraitName(traitIndex),
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
            perturbed.OverwriteTraitForAllCreatures(traitIndex, perturbation);

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

        public static string Report(GeneLivenessResult[] results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            var builder = new StringBuilder();
            builder.AppendLine("idx | trait                | reaches behavior | first diverged at tick");
            foreach (GeneLivenessResult result in results)
            {
                builder.AppendLine(
                    $"{result.TraitIndex,3} | {result.TraitName,-20} | {(result.ReachesBehavior ? "yes" : "NO  "),-16} | {(result.DivergedAtTick < 0 ? "never" : result.DivergedAtTick.ToString()),-10}");
            }

            return builder.ToString();
        }
    }
}
