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

        /// <summary>
        /// Intake rate (resource units of energy-equivalent per second) a fully-fed creature at an
        /// ideal place would achieve. Learned feeding outcomes are measured against this ceiling
        /// instead of being clamped to 1.0 on nearly every feeding event.
        /// </summary>
        public const float DefaultExpectedIntakeRate = 2.5f;

        /// <summary>Distance at which a remembered threat's avoidance penalty falls to exactly zero.</summary>
        public const float DefaultThreatFalloffDistance = 10f;

        /// <summary>
        /// Fraction of a grazing bite that a maximally defended patch (Defense = 1) withholds when
        /// <c>PlantDefenseDeterrenceEnabled</c> is set. Placeholder pending the sweep in
        /// docs/experiments/; not yet derived.
        /// </summary>
        public const float DefaultPlantDefenseDeterrenceStrength = 0.75f;

        /// <summary>Dispersal-range charge at maximum SeedProductionRate when its route is enabled.</summary>
        public const float DefaultPlantSeedProductionRateDispersalCharge = 2f;

        /// <summary>Fraction of the distance toward a successful position retained by the home-range centre.</summary>
        public const float DefaultHomeRangeLearningFraction = 0.25f;

        /// <summary>Familiarity added by each successful food, water, or reproduction event.</summary>
        public const float DefaultHomeRangeFamiliarityGain = 0.25f;

        /// <summary>Familiarity lost per second while the home-range feature is enabled.</summary>
        public const float DefaultHomeRangeFamiliarityDecayPerSecond = 0.01f;

        /// <summary>Largest affinity score adjustment available to an ordinary resource candidate.</summary>
        public const float DefaultHomeRangeBonusMaximum = 0.1f;

        /// <summary>Distance beyond which home-range familiarity contributes no candidate bonus.</summary>
        public const float DefaultHomeRangeBonusFalloffDistance = 10f;

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
            bool predationEconomicsEnabled = false,
            float handlingSeconds = DefaultHandlingSeconds,
            float referenceGain = DefaultReferenceGain,
            float commitmentStrength = DefaultCommitmentStrength,
            float commitmentHalfLifeSeconds = DefaultCommitmentHalfLifeSeconds,
            float giveUpSensitivity = DefaultGiveUpSensitivity,
            int minimumMemorySlots = DefaultMinimumMemorySlots,
            int additionalMemorySlots = DefaultAdditionalMemorySlots,
            float samePlaceRadius = DefaultSamePlaceRadius,
            float expectedIntakeRate = DefaultExpectedIntakeRate,
            float threatFalloffDistance = DefaultThreatFalloffDistance,
            bool decisionStaggerEnabled = false,
            bool multiThreatPerceptionEnabled = false,
            bool restBehaviorEnabled = false,
            bool juvenileCapabilityEnabled = false,
            bool parentalFollowingEnabled = false,
            bool kinRecognitionEnabled = false,
            bool learnedResourceQualityEnabled = false,
            bool mateSelectionEnabled = false,
            bool plantSiteCompetitionEnabled = false,
            bool plantMortalityEnabled = false,
            bool plantDefenseDeterrenceEnabled = false,
            float plantDefenseDeterrenceStrength = DefaultPlantDefenseDeterrenceStrength,
            bool plantQualityPreferenceEnabled = false,
            bool plantTemperatureAdaptationEnabled = false,
            bool proceduralEnvironmentFieldsEnabled = false,
            bool plantFertilityAdaptationEnabled = false,
            bool elevationFieldEnabled = false,
            bool plantEstablishmentContestEnabled = false,
            bool plantInvaderEstablishmentContestEnabled = false,
            float plantSeedProductionRateDispersalCharge = DefaultPlantSeedProductionRateDispersalCharge,
            bool plantSeedProductionRateEnabled = false,
            bool safetyGatedMateRendezvousEnabled = false,
            bool homeRangeAffinityEnabled = false,
            bool terrainDrivenEnvironmentEnabled = false,
            bool slopeMovementCostEnabled = false,
            bool terrainDrivenTemperatureEnabled = false)
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
            PredationEconomicsEnabled = predationEconomicsEnabled;
            HandlingSeconds = handlingSeconds;
            ReferenceGain = referenceGain;
            CommitmentStrength = commitmentStrength;
            CommitmentHalfLifeSeconds = commitmentHalfLifeSeconds;
            GiveUpSensitivity = giveUpSensitivity;
            MinimumMemorySlots = minimumMemorySlots;
            AdditionalMemorySlots = additionalMemorySlots;
            SamePlaceRadius = samePlaceRadius;
            ExpectedIntakeRate = expectedIntakeRate;
            ThreatFalloffDistance = threatFalloffDistance;
            DecisionStaggerEnabled = decisionStaggerEnabled;
            MultiThreatPerceptionEnabled = multiThreatPerceptionEnabled;
            RestBehaviorEnabled = restBehaviorEnabled;
            JuvenileCapabilityEnabled = juvenileCapabilityEnabled;
            ParentalFollowingEnabled = parentalFollowingEnabled;
            KinRecognitionEnabled = kinRecognitionEnabled;
            LearnedResourceQualityEnabled = learnedResourceQualityEnabled;
            MateSelectionEnabled = mateSelectionEnabled;
            PlantSiteCompetitionEnabled = plantSiteCompetitionEnabled;
            PlantMortalityEnabled = plantMortalityEnabled;
            PlantDefenseDeterrenceEnabled = plantDefenseDeterrenceEnabled;
            PlantDefenseDeterrenceStrength = plantDefenseDeterrenceStrength;
            PlantQualityPreferenceEnabled = plantQualityPreferenceEnabled;
            PlantTemperatureAdaptationEnabled = plantTemperatureAdaptationEnabled;
            ProceduralEnvironmentFieldsEnabled = proceduralEnvironmentFieldsEnabled;
            PlantFertilityAdaptationEnabled = plantFertilityAdaptationEnabled;
            ElevationFieldEnabled = elevationFieldEnabled;
            TerrainDrivenEnvironmentEnabled = terrainDrivenEnvironmentEnabled;
            SlopeMovementCostEnabled = slopeMovementCostEnabled;
            TerrainDrivenTemperatureEnabled = terrainDrivenTemperatureEnabled;
            PlantEstablishmentContestEnabled = plantEstablishmentContestEnabled;
            PlantInvaderEstablishmentContestEnabled = plantInvaderEstablishmentContestEnabled;
            PlantSeedProductionRateDispersalCharge = plantSeedProductionRateDispersalCharge;
            PlantSeedProductionRateEnabled = plantSeedProductionRateEnabled;
            SafetyGatedMateRendezvousEnabled = safetyGatedMateRendezvousEnabled;
            HomeRangeAffinityEnabled = homeRangeAffinityEnabled;
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
        public bool PredationEconomicsEnabled { get; }
        public float HandlingSeconds { get; }
        public float ReferenceGain { get; }
        public float CommitmentStrength { get; }
        public float CommitmentHalfLifeSeconds { get; }
        public float GiveUpSensitivity { get; }
        public int MinimumMemorySlots { get; }
        public int AdditionalMemorySlots { get; }
        public float SamePlaceRadius { get; }
        public float ExpectedIntakeRate { get; }
        public float ThreatFalloffDistance { get; }
        public bool DecisionStaggerEnabled { get; }
        public bool MultiThreatPerceptionEnabled { get; }
        public bool RestBehaviorEnabled { get; }
        public bool JuvenileCapabilityEnabled { get; }
        public bool ParentalFollowingEnabled { get; }
        public bool KinRecognitionEnabled { get; }
        public bool LearnedResourceQualityEnabled { get; }
        public bool MateSelectionEnabled { get; }
        public bool PlantSiteCompetitionEnabled { get; }
        public bool PlantMortalityEnabled { get; }
        public bool SafetyGatedMateRendezvousEnabled { get; }
        public bool HomeRangeAffinityEnabled { get; }

        /// <summary>
        /// When set, plant Defense reduces the biomass a grazer can actually remove per bite,
        /// not merely the nutrition extracted from it. Off by default: with it off, Defense
        /// protects no tissue at all, so it carries no individual-level benefit and cannot be
        /// selected on (docs/experiments/p4-defense-no-gradient-2026-08-18.md).
        /// </summary>
        public bool PlantDefenseDeterrenceEnabled { get; }

        /// <summary>Fraction of a bite withheld at Defense = 1. Only read when deterrence is enabled.</summary>
        public float PlantDefenseDeterrenceStrength { get; }

        /// <summary>
        /// When set, foraging patch choice under <c>IntentUtilityV1</c> weights a patch by its
        /// nutrition density, so a richer patch is preferred even when both patches would fully
        /// satisfy the need.
        ///
        /// <para>Off by default. With it off, <c>ComputeNeedGain</c> returns exactly 1.0 for every
        /// active patch — measured at 88 of 88 patch-hunger combinations, roughly 10x over its
        /// <c>Math.Min(1f, ..)</c> clamp — so patch quality is invisible to foraging and grazing is
        /// effectively uniform. Plant defense lowers nutrition density, so without this flag defense
        /// cannot cause a defended patch to be avoided, and uniform grazing gives it nothing to be
        /// selected on. See docs/experiments/needgain-clamp-2026-08-18.md.</para>
        /// </summary>
        public bool PlantQualityPreferenceEnabled { get; }

        /// <summary>
        /// When set, plant <c>TemperatureTolerance</c> improves a patch's position against a cold or
        /// hot site, mirroring how <c>MoistureTolerance</c> already works in
        /// <c>PlantGrowthSystem</c>.
        ///
        /// <para>Off by default, and <b>inert even when on</b> until the environment varies in
        /// temperature: <c>EnvironmentField</c> returns <c>Temperature = 1</c> on every production
        /// path, and the adaptation expression collapses to the raw value at 1. It is listed in
        /// <c>LivenessTests.KnownInertFlags</c> for exactly that reason, and should be moved out of
        /// that list when terrain fields land.</para>
        ///
        /// <para>Without this, <c>TemperatureTolerance</c> is a pure cost - it charges growth in
        /// <c>PlantPhenotype</c> and has no channel to earn it back under any environment.</para>
        /// </summary>
        public bool PlantTemperatureAdaptationEnabled { get; }

        /// <summary>
        /// When set, <c>EnvironmentField</c> supplies procedural moisture, fertility and temperature
        /// sampled on a sphere, replacing the hardcoded linear moisture ramp and the constant
        /// fertility and temperature of 1.
        /// </summary>
        public bool ProceduralEnvironmentFieldsEnabled { get; }

        /// <summary>
        /// When set, plant <c>NutrientUptake</c> improves a patch's position against poor soil,
        /// mirroring how <c>MoistureTolerance</c> and <c>TemperatureTolerance</c> already work,
        /// and the gene is charged <c>-.10f</c> growth for it.
        ///
        /// <para>Fertility was the only growth channel with no genome modulation, and it bound the
        /// <c>Min</c> at 82-90% of plant-reachable positions - so both existing adaptation terms
        /// were mostly buying nothing, which is why neither tolerance gene could be selected on.
        /// See docs/experiments/p4-fertility-binds-the-growth-limit-2026-08-19.md.</para>
        ///
        /// <para>The flag gates the cost as well as the benefit, so flag-off is byte-identical to
        /// the world before <c>NutrientUptake</c> existed.</para>
        /// </summary>
        public bool PlantFertilityAdaptationEnabled { get; }

        /// <summary>
        /// When set, <c>EnvironmentField</c> generates a ridged-multifractal elevation channel and
        /// applies a lapse rate, so high ground is colder than the valley beside it.
        ///
        /// <para>Requires <see cref="ProceduralEnvironmentFieldsEnabled"/>; the constant and
        /// moisture-gradient fields have no terrain to raise.</para>
        ///
        /// <para>Elevation deliberately has <b>no</b> growth channel of its own. It acts through
        /// temperature, which already limits growth, because a fourth channel plants had to adapt to
        /// would ship as another tax on genes that demonstrably cannot pay it - see
        /// docs/experiments/p4-growth-rate-traits-are-nearly-unselectable-2026-08-19.md. This is
        /// terrain and P6 groundwork, and it is not expected to make any plant gene selectable.</para>
        /// </summary>
        public bool ElevationFieldEnabled { get; }

        /// <summary>
        /// Whether moisture, temperature and elevation come from the terrain generator rather than
        /// from <see cref="EnvironmentField"/>'s own noise.
        ///
        /// <para><b>This is the join.</b> Until it is on, the ground a creature is drawn standing on
        /// and the ground the simulation reads are two unrelated fields: the arena mesh comes from
        /// <c>PlanetTerrain</c> and the ecology from independent fBm, so a hill costs a creature
        /// nothing and the visible world is decoration.</para>
        ///
        /// <para><b>Every plant result on record was measured with this off</b> and is scoped to the
        /// old field. Turning it on for a scenario invalidates nothing by itself, but any result
        /// carried over from a scenario that has it on is a new measurement, not a comparison.</para>
        /// </summary>
        public bool TerrainDrivenEnvironmentEnabled { get; }

        /// <summary>
        /// Whether climbing costs a creature anything.
        ///
        /// <para><b>This is the other half of the join.</b> With
        /// <see cref="TerrainDrivenEnvironmentEnabled"/> on, terrain decides where food grows - but
        /// the ground itself is still flat as far as a creature is concerned, and walking up a
        /// mountain costs exactly what walking across a plain costs. Relief that nothing has to climb
        /// is scenery with an ecology painted on it.</para>
        ///
        /// <para>The mechanism is deliberately the smallest one that exists: energy drain is already
        /// proportional to distance travelled, so a climb is charged <b>as extra distance</b>. No new
        /// creature state, no second energy path, and nothing to keep in step with the existing one.
        /// See <see cref="SlopeClimbCost"/> for the exchange rate.</para>
        ///
        /// <para><b>Requires elevation.</b> Without <see cref="TerrainDrivenEnvironmentEnabled"/> or
        /// <see cref="ElevationFieldEnabled"/> the field reports no elevation, every climb is zero,
        /// and this flag does nothing - which is inert, not broken.</para>
        /// </summary>
        public bool SlopeMovementCostEnabled { get; }

        /// <summary>
        /// When set, a creature's temperature comes from the world's <c>EnvironmentField</c> instead
        /// of <c>TemperatureField</c>, the fixed sine <c>20 + 8*sin(0.18x + 0.11y)</c>.
        ///
        /// <para><b>The third half of the join, and the one that was missed.</b>
        /// <see cref="TerrainDrivenEnvironmentEnabled"/> gave plants a real climate and
        /// <see cref="SlopeMovementCostEnabled"/> gave the ground a cost, but creature
        /// thermoregulation kept reading a decoration with no latitude, no altitude, no seasons and
        /// no terrain. That decoration is the strongest selection pressure in the model - the
        /// population moves a quarter of the trait range at t = 24 against a control at t = 0.07.
        /// See <c>docs/experiments/p6-why-temperature-tolerance-2026-08-24.md</c>.</para>
        ///
        /// <para><b>The degree span is deliberately unchanged</b> at 12 to 28. Tolerance is
        /// <c>2 + 8*gene</c>, so an 8-degree half-span is what puts the saturation ceiling at gene
        /// 0.75; holding it fixed means this flag changes the field's <i>spatial structure</i> and
        /// nothing else, and any change in the measured equilibrium is attributable to that alone.
        /// See <c>ClimateField</c>.</para>
        ///
        /// <para><b>Requires the join.</b> Without <see cref="TerrainDrivenEnvironmentEnabled"/> or
        /// <see cref="ProceduralEnvironmentFieldsEnabled"/> the field has no climate to offer and
        /// this reads whatever the plain field returns - inert, not broken.</para>
        /// </summary>
        public bool TerrainDrivenTemperatureEnabled { get; }

        /// <summary>
        /// Metres of level walking that one metre of climb costs, on top of the climb's own distance.
        ///
        /// <para>Four is the human figure to the nearest whole number - climbing is roughly five
        /// times as expensive as walking the same distance on the flat, and one of those five is the
        /// distance itself. It is a coefficient, not a measurement of this world, and it is here so
        /// it can be changed in one place when there is something to fit it to.</para>
        /// </summary>
        public const float SlopeClimbCost = 4f;

        /// <summary>
        /// Lets a vulnerable seedling resist takeover with its own <c>SeedlingResilience</c>,
        /// turning the single largest non-heritable term in plant fitness into a selectable one -
        /// docs/experiments/p4-where-plant-fitness-is-decided-2026-08-20.md. Requires
        /// <see cref="PlantSiteCompetitionEnabled"/>, which is what creates the contest at all.
        /// </summary>
        public bool PlantEstablishmentContestEnabled { get; }
        public bool PlantInvaderEstablishmentContestEnabled { get; }

        /// <summary>Dispersal-range cost paid at SeedProductionRate = 1 when its route is enabled.</summary>
        public float PlantSeedProductionRateDispersalCharge { get; }

        /// <summary>
        /// Lets <c>SeedProductionRate</c> shorten a mature patch's successful-seeding cooldown.
        /// The flag gates its dispersal charge at the same time, preserving byte-identical output
        /// for configurations that predate this gene.
        /// </summary>
        public bool PlantSeedProductionRateEnabled { get; }
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

        /// <summary>Field-set version for <see cref="ComputeConfigurationHash"/>. Bump on any change to the fields it covers.</summary>
        public const int ConfigurationHashVersion = 3;

        /// <summary>
        /// FNV-1a hash of every configuration value that can affect future simulation behavior:
        /// schedule, population and founder settings, every tuning constant, and every feature
        /// flag, in declaration order. Used by <c>SimulationWorld.ComputeStateFingerprint</c> so
        /// that two worlds with identical entity state but different configuration are correctly
        /// reported as divergent.
        /// </summary>
        public ulong ComputeConfigurationHash()
        {
            ulong hash = 14695981039346656037UL;
            hash = Hash(hash, unchecked((ulong)ConfigurationHashVersion));

            hash = Hash(hash, unchecked((ulong)Schedule.BaseFrequencyHz));
            hash = Hash(hash, unchecked((ulong)Schedule.MovementHz));
            hash = Hash(hash, unchecked((ulong)Schedule.PerceptionHz));
            hash = Hash(hash, unchecked((ulong)Schedule.NeedsHz));
            hash = Hash(hash, unchecked((ulong)Schedule.DecisionsHz));
            hash = Hash(hash, unchecked((ulong)Schedule.ResourcesHz));
            hash = Hash(hash, unchecked((ulong)Schedule.ReproductionHz));
            hash = Hash(hash, unchecked((ulong)Schedule.StatisticsHz));

            hash = Hash(hash, unchecked((ulong)WorldSeed));
            hash = Hash(hash, unchecked((ulong)InitialPopulation));
            hash = Hash(hash, unchecked((ulong)MaximumPopulation));
            hash = Hash(hash, unchecked((ulong)(int)FounderProfile));
            hash = Hash(hash, unchecked((ulong)(int)DecisionPolicyVersion));
            hash = HashFloat(hash, HandlingSeconds);
            hash = HashFloat(hash, ReferenceGain);
            hash = HashFloat(hash, CommitmentStrength);
            hash = HashFloat(hash, CommitmentHalfLifeSeconds);
            hash = HashFloat(hash, GiveUpSensitivity);
            hash = Hash(hash, unchecked((ulong)MinimumMemorySlots));
            hash = Hash(hash, unchecked((ulong)AdditionalMemorySlots));
            hash = HashFloat(hash, SamePlaceRadius);
            hash = HashFloat(hash, ExpectedIntakeRate);
            hash = HashFloat(hash, ThreatFalloffDistance);
            hash = HashFloat(hash, PlantDefenseDeterrenceStrength);
            hash = HashFloat(hash, PlantSeedProductionRateDispersalCharge);

            hash = Hash(hash, CognitionEnabled ? 1UL : 0UL);
            hash = Hash(hash, PhysiologyEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantCohortsEnabled ? 1UL : 0UL);
            hash = Hash(hash, ForagingEconomicsEnabled ? 1UL : 0UL);
            hash = Hash(hash, PredationEconomicsEnabled ? 1UL : 0UL);
            hash = Hash(hash, DecisionStaggerEnabled ? 1UL : 0UL);
            hash = Hash(hash, MultiThreatPerceptionEnabled ? 1UL : 0UL);
            hash = Hash(hash, RestBehaviorEnabled ? 1UL : 0UL);
            hash = Hash(hash, JuvenileCapabilityEnabled ? 1UL : 0UL);
            hash = Hash(hash, ParentalFollowingEnabled ? 1UL : 0UL);
            hash = Hash(hash, KinRecognitionEnabled ? 1UL : 0UL);
            hash = Hash(hash, LearnedResourceQualityEnabled ? 1UL : 0UL);
            hash = Hash(hash, MateSelectionEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantSiteCompetitionEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantMortalityEnabled ? 1UL : 0UL);
            hash = Hash(hash, SafetyGatedMateRendezvousEnabled ? 1UL : 0UL);
            hash = Hash(hash, HomeRangeAffinityEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantDefenseDeterrenceEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantQualityPreferenceEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantTemperatureAdaptationEnabled ? 1UL : 0UL);
            hash = Hash(hash, ProceduralEnvironmentFieldsEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantFertilityAdaptationEnabled ? 1UL : 0UL);
            hash = Hash(hash, ElevationFieldEnabled ? 1UL : 0UL);
            hash = Hash(hash, TerrainDrivenEnvironmentEnabled ? 1UL : 0UL);
            hash = Hash(hash, SlopeMovementCostEnabled ? 1UL : 0UL);
            hash = Hash(hash, TerrainDrivenTemperatureEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantEstablishmentContestEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantInvaderEstablishmentContestEnabled ? 1UL : 0UL);
            hash = Hash(hash, PlantSeedProductionRateEnabled ? 1UL : 0UL);

            return hash;
        }

        private static ulong Hash(ulong hash, ulong value)
        {
            return (hash ^ value) * 1099511628211UL;
        }

        private static ulong HashFloat(ulong hash, float value)
        {
            return Hash(hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(value)));
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

        /// <summary>
        /// Every P4 mechanism switched on at once, including plant mortality and defense deterrence.
        ///
        /// <para>This is a <b>liveness and integration</b> configuration, not an experimental one.
        /// Its purpose is to give every mechanism its best chance of mattering, so that
        /// <c>GeneLivenessAnalysis</c> and <c>LivenessRecorder</c> report against the widest
        /// available surface: a gene that reads as dead here is dead under every narrower
        /// configuration too.</para>
        ///
        /// <para><b>Do not run experiment arms against this.</b> Every flag moves together, so any
        /// difference it produces is unattributable — the confounded-sweep mistake recorded in
        /// docs/AGENT_FIELD_NOTES.md §5. Experiments vary one flag against
        /// <see cref="CreatePrototype4Defaults"/>.</para>
        /// </summary>
        public static SimulationConfig CreateFullEcosystemDefaults(int worldSeed, int initialPopulation)
        {
            SimulationConfig defaults = CreatePrototype4Defaults(worldSeed, initialPopulation);
            return new SimulationConfig(
                worldSeed,
                initialPopulation,
                defaults.Schedule,
                defaults.MaximumPopulation,
                FounderProfile.PhysiologyVariation,
                cognitionEnabled: true,
                physiologyEnabled: true,
                decisionPolicyVersion: DecisionPolicyVersion.IntentUtilityV1,
                plantCohortsEnabled: true,
                foragingEconomicsEnabled: true,
                predationEconomicsEnabled: true,
                decisionStaggerEnabled: true,
                multiThreatPerceptionEnabled: true,
                restBehaviorEnabled: true,
                juvenileCapabilityEnabled: true,
                parentalFollowingEnabled: true,
                kinRecognitionEnabled: true,
                learnedResourceQualityEnabled: true,
                mateSelectionEnabled: true,
                plantSiteCompetitionEnabled: true,
                plantMortalityEnabled: true,
                plantDefenseDeterrenceEnabled: true,
                plantQualityPreferenceEnabled: true,
                plantTemperatureAdaptationEnabled: true,
                proceduralEnvironmentFieldsEnabled: true,
                plantFertilityAdaptationEnabled: true,
                elevationFieldEnabled: true,
                plantEstablishmentContestEnabled: true,
                plantInvaderEstablishmentContestEnabled: true,
                plantSeedProductionRateEnabled: true);
        }

        /// <summary>
        /// Rejects a non-finite tuning value at construction. NaN and infinity are never caught
        /// later: they propagate silently through every arithmetic operation, survive the
        /// <c>Math.Max(0f, Math.Min(1f, value))</c> clamping the genome relies on, and then poison
        /// state hashes in a way that reads as nondeterminism rather than as bad input. The
        /// boundary is the only place this is cheap to check.
        /// </summary>
        private static void RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
            }
        }

        public void Validate()
        {
            RequireFinite(HandlingSeconds, nameof(HandlingSeconds));
            RequireFinite(ReferenceGain, nameof(ReferenceGain));
            RequireFinite(CommitmentStrength, nameof(CommitmentStrength));
            RequireFinite(CommitmentHalfLifeSeconds, nameof(CommitmentHalfLifeSeconds));
            RequireFinite(GiveUpSensitivity, nameof(GiveUpSensitivity));
            RequireFinite(SamePlaceRadius, nameof(SamePlaceRadius));
            RequireFinite(ExpectedIntakeRate, nameof(ExpectedIntakeRate));
            RequireFinite(ThreatFalloffDistance, nameof(ThreatFalloffDistance));
            RequireFinite(PlantDefenseDeterrenceStrength, nameof(PlantDefenseDeterrenceStrength));
            RequireFinite(PlantSeedProductionRateDispersalCharge, nameof(PlantSeedProductionRateDispersalCharge));

            if (InitialPopulation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(InitialPopulation), "Founder population cannot be negative.");
            }

            if (MaximumPopulation < InitialPopulation)
            {
                throw new ArgumentOutOfRangeException(nameof(MaximumPopulation), "Population limit cannot be lower than the founder population.");
            }

            if (PlantSeedProductionRateDispersalCharge < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(PlantSeedProductionRateDispersalCharge), "Seed-production dispersal charge cannot be negative.");
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

            if (ExpectedIntakeRate <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(ExpectedIntakeRate), "Expected intake rate must be positive.");
            }

            if (ThreatFalloffDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(ThreatFalloffDistance), "Threat falloff distance must be positive.");
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
