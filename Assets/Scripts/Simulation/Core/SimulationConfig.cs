using System;

namespace LifeSimulation.Simulation.Core
{
    public enum FounderProfile : byte
    {
        Prototype1 = 0,
        PredationVariation = 1,
        CognitionVariation = 2,
        PhysiologyVariation = 3,
    }

    public enum DecisionPolicyVersion : byte
    {
        Legacy = 0,
        IntentUtilityV1 = 1,
    }

    public readonly struct SimulationSchedule
    {
        public SimulationSchedule(
            int baseFrequencyHz,
            int movementHz,
            int perceptionHz,
            int needsHz,
            int decisionsHz,
            int resourcesHz,
            int reproductionHz,
            int statisticsHz)
        {
            BaseFrequencyHz = baseFrequencyHz;
            MovementHz = movementHz;
            PerceptionHz = perceptionHz;
            NeedsHz = needsHz;
            DecisionsHz = decisionsHz;
            ResourcesHz = resourcesHz;
            ReproductionHz = reproductionHz;
            StatisticsHz = statisticsHz;
        }

        public int BaseFrequencyHz { get; }
        public int MovementHz { get; }
        public int PerceptionHz { get; }
        public int NeedsHz { get; }
        public int DecisionsHz { get; }
        public int ResourcesHz { get; }
        public int ReproductionHz { get; }
        public int StatisticsHz { get; }
    }

    public sealed class SimulationConfig
    {
        /// <summary>Seconds of handling time assumed when estimating a patch's expected energy yield.</summary>
        public const float DefaultHandlingSeconds = 2f;

        /// <summary>Reference net energy gain used to normalize patch scores into [0, 1].</summary>
        public const float DefaultReferenceGain = 5f;

        /// <summary>Strength of the decaying bonus applied to the action already underway.</summary>
        public const float DefaultCommitmentStrength = 0.3f;

        /// <summary>Seconds for the commitment bonus to halve.</summary>
        public const float DefaultCommitmentHalfLifeSeconds = 5f;

        /// <summary>Sensitivity multiplier used when deciding whether to abandon a draining patch.</summary>
        public const float DefaultGiveUpSensitivity = 0.5f;

        public SimulationConfig(
            int worldSeed,
            int initialPopulation,
            SimulationSchedule schedule,
            int maximumPopulation = 1000,
            FounderProfile founderProfile = FounderProfile.Prototype1,
            bool cognitionEnabled = false,
            bool physiologyEnabled = false,
            DecisionPolicyVersion decisionPolicyVersion = DecisionPolicyVersion.Legacy,
            bool plantCohortsEnabled = false,
            bool foragingEconomicsEnabled = false,
            float handlingSeconds = DefaultHandlingSeconds,
            float referenceGain = DefaultReferenceGain,
            float commitmentStrength = DefaultCommitmentStrength,
            float commitmentHalfLifeSeconds = DefaultCommitmentHalfLifeSeconds,
            float giveUpSensitivity = DefaultGiveUpSensitivity)
        {
            WorldSeed = worldSeed;
            InitialPopulation = initialPopulation;
            Schedule = schedule;
            MaximumPopulation = maximumPopulation;
            FounderProfile = founderProfile;
            CognitionEnabled = cognitionEnabled;
            PhysiologyEnabled = physiologyEnabled;
            DecisionPolicyVersion = decisionPolicyVersion;
            PlantCohortsEnabled = plantCohortsEnabled;
            ForagingEconomicsEnabled = foragingEconomicsEnabled;
            HandlingSeconds = handlingSeconds;
            ReferenceGain = referenceGain;
            CommitmentStrength = commitmentStrength;
            CommitmentHalfLifeSeconds = commitmentHalfLifeSeconds;
            GiveUpSensitivity = giveUpSensitivity;
        }

        public int WorldSeed { get; }
        public int InitialPopulation { get; }
        public int MaximumPopulation { get; }
        public FounderProfile FounderProfile { get; }
        public bool CognitionEnabled { get; }
        public bool PhysiologyEnabled { get; }
        public DecisionPolicyVersion DecisionPolicyVersion { get; }
        public bool PlantCohortsEnabled { get; }
        public bool ForagingEconomicsEnabled { get; }
        public float HandlingSeconds { get; }
        public float ReferenceGain { get; }
        public float CommitmentStrength { get; }
        public float CommitmentHalfLifeSeconds { get; }
        public float GiveUpSensitivity { get; }
        public SimulationSchedule Schedule { get; }
        public float FixedDeltaTime => 1f / Schedule.BaseFrequencyHz;

        public static SimulationConfig CreatePrototype1Defaults(int worldSeed, int initialPopulation)
        {
            return new SimulationConfig(
                worldSeed,
                initialPopulation,
                new SimulationSchedule(20, 20, 4, 2, 2, 1, 1, 1),
                maximumPopulation: 1000);
        }

        public static SimulationConfig CreatePrototype2Defaults(int worldSeed, int initialPopulation)
        {
            SimulationConfig defaults = CreatePrototype1Defaults(worldSeed, initialPopulation);
            return new SimulationConfig(
                worldSeed,
                initialPopulation,
                defaults.Schedule,
                defaults.MaximumPopulation,
                FounderProfile.CognitionVariation,
                cognitionEnabled: true);
        }

        public static SimulationConfig CreatePrototype3Defaults(int worldSeed, int initialPopulation)
        {
            SimulationConfig defaults = CreatePrototype2Defaults(worldSeed, initialPopulation);
            return new SimulationConfig(worldSeed, initialPopulation, defaults.Schedule, defaults.MaximumPopulation, FounderProfile.PhysiologyVariation, cognitionEnabled: true, physiologyEnabled: true);
        }

        public static SimulationConfig CreatePrototype4Defaults(int worldSeed, int initialPopulation)
        {
            SimulationConfig defaults = CreatePrototype3Defaults(worldSeed, initialPopulation);
            return new SimulationConfig(worldSeed, initialPopulation, defaults.Schedule, defaults.MaximumPopulation, FounderProfile.PhysiologyVariation, cognitionEnabled: true, physiologyEnabled: true, decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1, plantCohortsEnabled: true);
        }

        public void Validate()
        {
            if (InitialPopulation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(InitialPopulation), "Founder population cannot be negative.");
            }

            if (MaximumPopulation < InitialPopulation)
            {
                throw new ArgumentOutOfRangeException(nameof(MaximumPopulation), "Population limit cannot be lower than the founder population.");
            }

            if (!Enum.IsDefined(typeof(FounderProfile), FounderProfile))
            {
                throw new ArgumentOutOfRangeException(nameof(FounderProfile));
            }

            if (!Enum.IsDefined(typeof(DecisionPolicyVersion), DecisionPolicyVersion))
            {
                throw new ArgumentOutOfRangeException(nameof(DecisionPolicyVersion));
            }

            if (Schedule.BaseFrequencyHz <= 0)
            {
                throw new ArgumentException("Base simulation frequency must be positive.", nameof(Schedule));
            }

            ValidateScheduledFrequency(Schedule.MovementHz, nameof(Schedule.MovementHz));
            ValidateScheduledFrequency(Schedule.PerceptionHz, nameof(Schedule.PerceptionHz));
            ValidateScheduledFrequency(Schedule.NeedsHz, nameof(Schedule.NeedsHz));
            ValidateScheduledFrequency(Schedule.DecisionsHz, nameof(Schedule.DecisionsHz));
            ValidateScheduledFrequency(Schedule.ResourcesHz, nameof(Schedule.ResourcesHz));
            ValidateScheduledFrequency(Schedule.ReproductionHz, nameof(Schedule.ReproductionHz));
            ValidateScheduledFrequency(Schedule.StatisticsHz, nameof(Schedule.StatisticsHz));

            if (HandlingSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(HandlingSeconds), "Handling seconds must be positive.");
            }

            if (ReferenceGain <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(ReferenceGain), "Reference gain must be positive.");
            }

            if (CommitmentStrength <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(CommitmentStrength), "Commitment strength must be positive.");
            }

            if (CommitmentHalfLifeSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(CommitmentHalfLifeSeconds), "Commitment half-life seconds must be positive.");
            }

            if (GiveUpSensitivity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(GiveUpSensitivity), "Give-up sensitivity must be positive.");
            }
        }

        private void ValidateScheduledFrequency(int frequencyHz, string name)
        {
            if (frequencyHz <= 0 || frequencyHz > Schedule.BaseFrequencyHz || Schedule.BaseFrequencyHz % frequencyHz != 0)
            {
                throw new ArgumentException(
                    $"{name} must be a positive integer divisor of the base frequency.",
                    nameof(Schedule));
            }
        }
    }
}
