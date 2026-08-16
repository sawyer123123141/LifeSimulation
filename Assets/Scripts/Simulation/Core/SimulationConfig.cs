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

        /// <summary>Place-memory slot count every creature has regardless of genome.</summary>
        public const int DefaultMinimumMemorySlots = 3;

        /// <summary>Extra place-memory slots available to a creature with the maximum <c>MemoryCapacity</c> gene.</summary>
        public const int DefaultAdditionalMemorySlots = 5;

        /// <summary>Maximum distance between two observations of the same kind for them to be treated as the same remembered place.</summary>
        public const float DefaultSamePlaceRadius = 2f;

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
            float giveUpSensitivity = DefaultGiveUpSensitivity,
            int minimumMemorySlots = DefaultMinimumMemorySlots,
            int additionalMemorySlots = DefaultAdditionalMemorySlots,
            float samePlaceRadius = DefaultSamePlaceRadius)
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
            MinimumMemorySlots = minimumMemorySlots;
            AdditionalMemorySlots = additionalMemorySlots;
            SamePlaceRadius = samePlaceRadius;
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
        public int MinimumMemorySlots { get; }
        public int AdditionalMemorySlots { get; }
        public float SamePlaceRadius { get; }
        public SimulationSchedule Schedule { get; }
        public float FixedDeltaTime => 1f / Schedule.BaseFrequencyHz;

        /// <summary>Widest place-memory row any creature can have; the value used to size dense per-creature storage.</summary>
        public int MaximumMemorySlots => MinimumMemorySlots + AdditionalMemorySlots;

        /// <summary>
        /// Usable place-memory slot count for a creature with the given <c>MemoryCapacity</c> gene (expected in [0, 1]).
        /// Always between <see cref="MinimumMemorySlots"/> and <see cref="MaximumMemorySlots"/> inclusive.
        /// </summary>
        public static int ComputeMemorySlotCount(int minimumMemorySlots, int additionalMemorySlots, float memoryCapacityGene)
        {
            float clampedGene = Math.Min(1f, Math.Max(0f, memoryCapacityGene));
            int bonusSlots = (int)Math.Round(clampedGene * additionalMemorySlots, MidpointRounding.AwayFromZero);
            return minimumMemorySlots + bonusSlots;
        }

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

            if (MinimumMemorySlots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(MinimumMemorySlots), "Minimum memory slots cannot be negative.");
            }

            if (AdditionalMemorySlots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(AdditionalMemorySlots), "Additional memory slots cannot be negative.");
            }

            if (SamePlaceRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(SamePlaceRadius), "Same-place radius must be positive.");
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
