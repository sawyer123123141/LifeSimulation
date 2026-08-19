using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LifeSimulation.Simulation.Core;
using LifeSimulation.Simulation.Experiments;

namespace LifeSimulation.Simulation.Diagnostics
{
    public readonly struct FlagLivenessResult
    {
        public FlagLivenessResult(string flagName, bool baselineValue, bool changesBehavior, long divergedAtTick)
        {
            FlagName = flagName;
            BaselineValue = baselineValue;
            ChangesBehavior = changesBehavior;
            DivergedAtTick = divergedAtTick;
        }

        public string FlagName { get; }
        public bool BaselineValue { get; }

        /// <summary>False means flipping this flag changed nothing the simulation does.</summary>
        public bool ChangesBehavior { get; }

        public long DivergedAtTick { get; }
    }

    /// <summary>
    /// Whether each <see cref="SimulationConfig"/> boolean flag actually changes behavior, decided
    /// by flipping it and comparing <see cref="SimulationWorld.ComputeBehaviorHash"/> tick by tick.
    ///
    /// <para>Why this exists, concretely: the 2026-08-17 audit cleared every config flag on the
    /// grounds that each had "at least one production reader". That is true of
    /// <c>LearnedResourceQualityEnabled</c>, whose single reader sits inside
    /// <c>DecideFromLearnedOutcomes</c> — the Legacy path. Under <c>IntentUtilityV1</c>, which every
    /// P4 scenario uses, flipping it produces bit-identical runs. A reader existing is not the same
    /// as the reader running.</para>
    ///
    /// <para>Flags are enumerated by reflection over the constructor's <c>bool</c> parameters, so a
    /// newly added flag is covered automatically rather than needing to be remembered.</para>
    ///
    /// <para>Same scoping caveat as <see cref="GeneLivenessAnalysis"/>: an "inert" verdict is
    /// relative to the scenario and baseline configuration tested. A flag governing predation reads
    /// inert in a herbivore scenario. Pin against the widest configuration available.</para>
    /// </summary>
    public static class FlagLivenessAnalysis
    {
        public static FlagLivenessResult[] Analyze(
            Func<SimulationConfig> baselineFactory,
            SimulationScenario scenario,
            int ticks)
        {
            if (baselineFactory == null) throw new ArgumentNullException(nameof(baselineFactory));
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (ticks <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));

            ConstructorInfo constructor = typeof(SimulationConfig)
                .GetConstructors()
                .OrderByDescending(candidate => candidate.GetParameters().Length)
                .First();

            ParameterInfo[] parameters = constructor.GetParameters();
            var results = new List<FlagLivenessResult>();

            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].ParameterType != typeof(bool))
                {
                    continue;
                }

                SimulationConfig baseline = baselineFactory();
                object[] arguments = BuildArguments(parameters, baseline);
                bool baselineValue = (bool)arguments[index];
                arguments[index] = !baselineValue;

                var flipped = (SimulationConfig)constructor.Invoke(arguments);
                long divergedAtTick = FindDivergenceTick(baseline, flipped, scenario, ticks);

                results.Add(new FlagLivenessResult(
                    parameters[index].Name,
                    baselineValue,
                    divergedAtTick >= 0,
                    divergedAtTick));
            }

            return results.ToArray();
        }

        /// <summary>
        /// Rebuild the constructor argument list from a config's own properties, so the flipped
        /// config differs from the baseline in exactly one position. Relies on the project-wide
        /// convention that a constructor parameter's property is its PascalCase form.
        /// </summary>
        private static object[] BuildArguments(ParameterInfo[] parameters, SimulationConfig source)
        {
            var arguments = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                string parameterName = parameters[index].Name;
                string propertyName = char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);
                PropertyInfo property = typeof(SimulationConfig).GetProperty(propertyName);
                if (property == null)
                {
                    throw new InvalidOperationException(
                        $"SimulationConfig parameter '{parameterName}' has no matching property '{propertyName}'. "
                        + "FlagLivenessAnalysis relies on that convention to rebuild a config.");
                }

                arguments[index] = property.GetValue(source);
            }

            return arguments;
        }

        private static long FindDivergenceTick(
            SimulationConfig baselineConfig,
            SimulationConfig flippedConfig,
            SimulationScenario scenario,
            int ticks)
        {
            var baseline = new SimulationWorld(baselineConfig);
            scenario.ApplyTo(baseline);

            var flipped = new SimulationWorld(flippedConfig);
            scenario.ApplyTo(flipped);

            for (int step = 0; step < ticks; step++)
            {
                baseline.Step(baselineConfig.FixedDeltaTime);
                flipped.Step(flippedConfig.FixedDeltaTime);
                baseline.Events.Clear();
                flipped.Events.Clear();

                if (baseline.ComputeBehaviorHash() != flipped.ComputeBehaviorHash())
                {
                    return baseline.CurrentTick;
                }
            }

            return -1;
        }

        public static string Report(FlagLivenessResult[] results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            var builder = new StringBuilder();
            builder.AppendLine("flag                             | baseline | changes behavior | diverged at tick");
            foreach (FlagLivenessResult result in results)
            {
                builder.AppendLine(
                    $"{result.FlagName,-32} | {result.BaselineValue,-8} | {(result.ChangesBehavior ? "yes" : "NO  "),-16} | {(result.DivergedAtTick < 0 ? "never" : result.DivergedAtTick.ToString())}");
            }

            return builder.ToString();
        }
    }
}
