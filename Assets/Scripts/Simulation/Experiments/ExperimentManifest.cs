using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Experiments
{
    /// <summary>
    /// Provenance for an experiment result: everything needed to re-run it, recorded beside the
    /// numbers rather than in prose that may or may not be written.
    ///
    /// <para>This exists because of a concrete loss. On 2026-08-22 three plant conclusions became
    /// permanently unverifiable: the scenario they ran on lived in a throwaway probe, was never
    /// committed, and could not be recovered from git, the writeups or the per-seed CSV. The
    /// writeups recorded the site count, the config, the seeds and the resulting occupancy -
    /// everything except the coordinates. A layout fingerprint beside those results would at least
    /// have told a later session whether a reconstruction matched.</para>
    ///
    /// <para>Deliberately environment-free: no clock, no machine identity, no file system, no
    /// git invocation. Simulation code may not read any of those. The code revision is supplied by
    /// the caller, which is allowed to know it.</para>
    /// </summary>
    public static class ExperimentManifest
    {
        /// <summary>Bump when the field set changes, never redefine fields silently.</summary>
        public const int SchemaVersion = 1;

        public static string Describe(
            string codeRevision,
            SimulationScenario scenario,
            SimulationConfig config,
            int firstSeed,
            int seedCount,
            int ticks)
        {
            if (string.IsNullOrWhiteSpace(codeRevision))
            {
                throw new ArgumentException(
                    "A code revision is required; a result that cannot be traced to code cannot be re-run.",
                    nameof(codeRevision));
            }

            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (config == null) throw new ArgumentNullException(nameof(config));

            var builder = new StringBuilder();
            Line(builder, "schema", SchemaVersion.ToString(CultureInfo.InvariantCulture));
            Line(builder, "code_revision", codeRevision.Trim());
            Line(builder, "scenario_id", scenario.Id);
            Line(builder, "scenario_layout_fingerprint", scenario.ComputeLayoutFingerprint().ToString(CultureInfo.InvariantCulture));
            Line(builder, "scenario_resource_count", scenario.ResourceCount.ToString(CultureInfo.InvariantCulture));
            Line(builder, "first_seed", firstSeed.ToString(CultureInfo.InvariantCulture));
            Line(builder, "seed_count", seedCount.ToString(CultureInfo.InvariantCulture));
            Line(builder, "ticks", ticks.ToString(CultureInfo.InvariantCulture));

            Line(builder, "WorldSeed", config.WorldSeed.ToString(CultureInfo.InvariantCulture));
            Line(builder, "InitialPopulation", config.InitialPopulation.ToString(CultureInfo.InvariantCulture));
            Line(builder, "MaximumPopulation", config.MaximumPopulation.ToString(CultureInfo.InvariantCulture));
            Line(builder, "FounderProfile", config.FounderProfile.ToString());
            Line(builder, "DecisionPolicyVersion", config.DecisionPolicyVersion.ToString());
            Line(builder, "BaseFrequencyHz", config.Schedule.BaseFrequencyHz.ToString(CultureInfo.InvariantCulture));

            Line(builder, "CognitionEnabled", config.CognitionEnabled);
            Line(builder, "PhysiologyEnabled", config.PhysiologyEnabled);
            Line(builder, "PlantCohortsEnabled", config.PlantCohortsEnabled);
            Line(builder, "PredationEconomicsEnabled", config.PredationEconomicsEnabled);
            Line(builder, "ForagingEconomicsEnabled", config.ForagingEconomicsEnabled);
            Line(builder, "DecisionStaggerEnabled", config.DecisionStaggerEnabled);
            Line(builder, "MultiThreatPerceptionEnabled", config.MultiThreatPerceptionEnabled);
            Line(builder, "RestBehaviorEnabled", config.RestBehaviorEnabled);
            Line(builder, "JuvenileCapabilityEnabled", config.JuvenileCapabilityEnabled);
            Line(builder, "ParentalFollowingEnabled", config.ParentalFollowingEnabled);
            Line(builder, "KinRecognitionEnabled", config.KinRecognitionEnabled);
            Line(builder, "LearnedResourceQualityEnabled", config.LearnedResourceQualityEnabled);
            Line(builder, "MateSelectionEnabled", config.MateSelectionEnabled);
            Line(builder, "PlantSiteCompetitionEnabled", config.PlantSiteCompetitionEnabled);
            Line(builder, "PlantMortalityEnabled", config.PlantMortalityEnabled);
            Line(builder, "PlantDefenseDeterrenceEnabled", config.PlantDefenseDeterrenceEnabled);
            Line(builder, "PlantQualityPreferenceEnabled", config.PlantQualityPreferenceEnabled);
            Line(builder, "PlantTemperatureAdaptationEnabled", config.PlantTemperatureAdaptationEnabled);
            Line(builder, "PlantFertilityAdaptationEnabled", config.PlantFertilityAdaptationEnabled);
            Line(builder, "ProceduralEnvironmentFieldsEnabled", config.ProceduralEnvironmentFieldsEnabled);
            Line(builder, "ElevationFieldEnabled", config.ElevationFieldEnabled);
            Line(builder, "TerrainDrivenEnvironmentEnabled", config.TerrainDrivenEnvironmentEnabled);
            Line(builder, "SlopeMovementCostEnabled", config.SlopeMovementCostEnabled);
            Line(builder, "PlantEstablishmentContestEnabled", config.PlantEstablishmentContestEnabled);
            Line(builder, "PlantInvaderEstablishmentContestEnabled", config.PlantInvaderEstablishmentContestEnabled);
            Line(builder, "PlantSeedProductionRateEnabled", config.PlantSeedProductionRateEnabled);
            Line(builder, "SafetyGatedMateRendezvousEnabled", config.SafetyGatedMateRendezvousEnabled);
            Line(builder, "HomeRangeAffinityEnabled", config.HomeRangeAffinityEnabled);

            Line(builder, "PlantSeedProductionRateDispersalCharge", config.PlantSeedProductionRateDispersalCharge.ToString("R", CultureInfo.InvariantCulture));
            Line(builder, "PlantDefenseDeterrenceStrength", config.PlantDefenseDeterrenceStrength.ToString("R", CultureInfo.InvariantCulture));
            Line(builder, "ThreatFalloffDistance", config.ThreatFalloffDistance.ToString("R", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static void Line(StringBuilder builder, string key, bool value)
        {
            Line(builder, key, value ? "true" : "false");
        }

        private static void Line(StringBuilder builder, string key, string value)
        {
            builder.Append(key).Append('=').Append(value).Append('\n');
        }
    }

    /// <summary>
    /// Composes an experiment CSV that carries its own provenance. It refuses to compose without a
    /// manifest, which is the whole point: results written without provenance are the failure this
    /// type exists to prevent, and a convention nobody enforces is not a control.
    /// </summary>
    public static class ExperimentCsv
    {
        public static string Compose(string manifest, string header, IReadOnlyList<string> rows)
        {
            if (string.IsNullOrWhiteSpace(manifest))
            {
                throw new ArgumentException(
                    "Experiment output requires a manifest; see ExperimentManifest.Describe.",
                    nameof(manifest));
            }

            if (string.IsNullOrWhiteSpace(header)) throw new ArgumentException("A CSV header is required.", nameof(header));
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            var builder = new StringBuilder();
            foreach (string line in manifest.Split('\n'))
            {
                if (line.Length == 0)
                {
                    continue;
                }

                builder.Append("# ").Append(line).Append('\n');
            }

            builder.Append(header).Append('\n');
            for (int index = 0; index < rows.Count; index++)
            {
                builder.Append(rows[index]).Append('\n');
            }

            return builder.ToString();
        }
    }
}
