using System;

namespace LifeSimulation.Simulation.Biology
{
    public readonly struct Genome
    {
        public Genome(
            float bodySize,
            float movementSpeed,
            float metabolicPace,
            float visionRange,
            float waterEfficiency,
            float foodEfficiency,
            float attack = 0f,
            float defense = 0f,
            float maneuverability = 0f,
            float fear = 0f,
            float aggression = 0f,
            float dietSpecialization = 0f,
            float memoryCapacity = 0f,
            float memoryRetention = 0f,
            float learningRate = 0f,
            float exploration = 0f,
            float temperatureTolerance = 0f,
            float fertilityInvestment = 0f,
            float lifespanTendency = 0f,
            float urgencyExponent = 0.5f,
            float travelSensitivity = 0.5f,
            float riskAversion = 0.5f,
            float neutralMarker = 0.5f,
            float persistence = 0.5f)
        {
            BodySize = Clamp01(bodySize);
            MovementSpeed = Clamp01(movementSpeed);
            MetabolicPace = Clamp01(metabolicPace);
            VisionRange = Clamp01(visionRange);
            WaterEfficiency = Clamp01(waterEfficiency);
            FoodEfficiency = Clamp01(foodEfficiency);
            Attack = Clamp01(attack);
            Defense = Clamp01(defense);
            Maneuverability = Clamp01(maneuverability);
            Fear = Clamp01(fear);
            Aggression = Clamp01(aggression);
            DietSpecialization = Clamp01(dietSpecialization);
            MemoryCapacity = Clamp01(memoryCapacity);
            MemoryRetention = Clamp01(memoryRetention);
            LearningRate = Clamp01(learningRate);
            Exploration = Clamp01(exploration);
            TemperatureTolerance = Clamp01(temperatureTolerance);
            FertilityInvestment = Clamp01(fertilityInvestment);
            LifespanTendency = Clamp01(lifespanTendency);
            UrgencyExponent = Clamp01(urgencyExponent);
            TravelSensitivity = Clamp01(travelSensitivity);
            RiskAversion = Clamp01(riskAversion);
            NeutralMarker = Clamp01(neutralMarker);
            Persistence = Clamp01(persistence);
        }

        public static Genome Neutral => new Genome(0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);

        public float BodySize { get; }
        public float MovementSpeed { get; }
        public float MetabolicPace { get; }
        public float VisionRange { get; }
        public float WaterEfficiency { get; }
        public float FoodEfficiency { get; }
        public float Attack { get; }
        public float Defense { get; }
        public float Maneuverability { get; }
        public float Fear { get; }
        public float Aggression { get; }
        public float DietSpecialization { get; }
        public float MemoryCapacity { get; }
        public float MemoryRetention { get; }
        public float LearningRate { get; }
        public float Exploration { get; }
        public float TemperatureTolerance { get; }
        public float FertilityInvestment { get; }
        public float LifespanTendency { get; }
        public float UrgencyExponent { get; }
        public float TravelSensitivity { get; }
        public float RiskAversion { get; }
        /// <summary>
        /// Deliberately inert drift-control locus. Inherited, mutated, hashed and reported as
        /// <c>ExperimentMetric.NeutralMarker</c>, and read by <b>no behavior system at all</b>.
        ///
        /// <para>This is not an oversight — do not wire it. Selection cannot act on it, so its
        /// measured change in any paired experiment is pure drift by construction, which makes it
        /// the negative control that shows the bootstrap pipeline does not manufacture false
        /// positives (it returned effect +0.020 with a tight interval around zero on
        /// 2026-08-17). Wiring it destroys that control and invalidates every experiment that
        /// leaned on it.</para>
        ///
        /// <para>Formerly named <c>Commitment</c>, which collided with
        /// <c>ForagingEconomics.CommitmentBonus</c> and <c>SimulationConfig.CommitmentStrength</c>
        /// — unrelated foraging machinery that takes <see cref="Persistence"/>. That collision is
        /// what led an audit to assume this gene fed the bonus.</para>
        ///
        /// <para>Pinned inert by <c>LivenessTests.NeutralMarkerReachesNoBehaviorUnderTheWidestConfiguration</c>.</para>
        /// </summary>
        public float NeutralMarker { get; }
        public float Persistence { get; }

        public Genome WithBodySize(float value)
        {
            return new Genome(
                value,
                MovementSpeed,
                MetabolicPace,
                VisionRange,
                WaterEfficiency,
                FoodEfficiency,
                Attack,
                Defense,
                Maneuverability,
                Fear,
                Aggression,
                DietSpecialization,
                MemoryCapacity,
                MemoryRetention,
                LearningRate,
                Exploration,
                TemperatureTolerance,
                FertilityInvestment,
                LifespanTendency,
                UrgencyExponent,
                TravelSensitivity,
                RiskAversion,
                NeutralMarker,
                Persistence);
        }

        /// <summary>Number of heritable traits. Keep in step with the constructor and <see cref="ToTraits"/>.</summary>
        public const int TraitCount = 24;

        private static readonly string[] TraitNames =
        {
            nameof(BodySize), nameof(MovementSpeed), nameof(MetabolicPace), nameof(VisionRange),
            nameof(WaterEfficiency), nameof(FoodEfficiency), nameof(Attack), nameof(Defense),
            nameof(Maneuverability), nameof(Fear), nameof(Aggression), nameof(DietSpecialization),
            nameof(MemoryCapacity), nameof(MemoryRetention), nameof(LearningRate), nameof(Exploration),
            nameof(TemperatureTolerance), nameof(FertilityInvestment), nameof(LifespanTendency),
            nameof(UrgencyExponent), nameof(TravelSensitivity), nameof(RiskAversion),
            nameof(NeutralMarker), nameof(Persistence),
        };

        /// <summary>Trait name for an index, matching <see cref="ToTraits"/> ordering.</summary>
        public static string TraitName(int index)
        {
            if ((uint)index >= (uint)TraitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return TraitNames[index];
        }

        /// <summary>
        /// All traits in constructor order. The single enumeration of the genome: a trait missing
        /// here fails the round-trip test rather than silently taking a default, which is how
        /// <c>Persistence</c> was dropped for every creature ever born.
        /// </summary>
        public float[] ToTraits()
        {
            var traits = new float[TraitCount];
            WriteTraits(traits, 0);
            return traits;
        }

        /// <summary>
        /// Writes this genome's traits into a caller-owned buffer at <paramref name="offset"/>.
        /// <see cref="ToTraits"/> is the allocating convenience wrapper; analysis loops that compare
        /// many genomes use this so their allocation scales with the population rather than with the
        /// number of pairs compared.
        /// </summary>
        public void WriteTraits(float[] destination, int offset)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (offset < 0 || offset + TraitCount > destination.Length) throw new ArgumentOutOfRangeException(nameof(offset));

            destination[offset + 0] = BodySize;
            destination[offset + 1] = MovementSpeed;
            destination[offset + 2] = MetabolicPace;
            destination[offset + 3] = VisionRange;
            destination[offset + 4] = WaterEfficiency;
            destination[offset + 5] = FoodEfficiency;
            destination[offset + 6] = Attack;
            destination[offset + 7] = Defense;
            destination[offset + 8] = Maneuverability;
            destination[offset + 9] = Fear;
            destination[offset + 10] = Aggression;
            destination[offset + 11] = DietSpecialization;
            destination[offset + 12] = MemoryCapacity;
            destination[offset + 13] = MemoryRetention;
            destination[offset + 14] = LearningRate;
            destination[offset + 15] = Exploration;
            destination[offset + 16] = TemperatureTolerance;
            destination[offset + 17] = FertilityInvestment;
            destination[offset + 18] = LifespanTendency;
            destination[offset + 19] = UrgencyExponent;
            destination[offset + 20] = TravelSensitivity;
            destination[offset + 21] = RiskAversion;
            destination[offset + 22] = NeutralMarker;
            destination[offset + 23] = Persistence;
        }

        /// <summary>Rebuild a genome from <see cref="ToTraits"/> output.</summary>
        public static Genome FromTraits(float[] traits)
        {
            if (traits == null)
            {
                throw new ArgumentNullException(nameof(traits));
            }

            if (traits.Length != TraitCount)
            {
                throw new ArgumentException($"Expected {TraitCount} traits, got {traits.Length}.", nameof(traits));
            }

            return new Genome(
                traits[0], traits[1], traits[2], traits[3],
                traits[4], traits[5], traits[6], traits[7],
                traits[8], traits[9], traits[10], traits[11],
                traits[12], traits[13], traits[14], traits[15],
                traits[16], traits[17], traits[18], traits[19],
                traits[20], traits[21], traits[22], traits[23]);
        }

        public float GetTrait(int index)
        {
            if ((uint)index >= (uint)TraitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return ToTraits()[index];
        }

        /// <summary>Copy with one trait replaced. Used by the gene liveness perturbation harness.</summary>
        public Genome WithTrait(int index, float value)
        {
            if ((uint)index >= (uint)TraitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            float[] traits = ToTraits();
            traits[index] = value;
            return FromTraits(traits);
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }

    public readonly struct Phenotype
    {
        private Phenotype(
            float bodyMass,
            float energyCapacity,
            float hydrationCapacity,
            float healthCapacity,
            float maximumSpeed,
            float visionRange,
            float foodYield,
            float ingestionRate,
            float digestionRate,
            float waterLossMultiplier,
            float basalEnergyCostMultiplier,
            float attackPower,
            float defense,
            float maneuverability,
            float fearResponse,
            float aggression,
            float plantFoodYieldMultiplier,
            float meatYieldMultiplier,
            float memoryConfidenceDecayPerSecond,
            float cognitionRestCostMultiplier,
            float temperatureTolerance,
            float learningRate,
            float exploration,
            float reproductionCooldownSeconds,
            float reproductionEnergyCostFraction,
            float maximumAgeSeconds,
            float persistence)
        {
            BodyMass = bodyMass;
            EnergyCapacity = energyCapacity;
            HydrationCapacity = hydrationCapacity;
            HealthCapacity = healthCapacity;
            MaximumSpeed = maximumSpeed;
            VisionRange = visionRange;
            FoodYield = foodYield;
            IngestionRate = ingestionRate;
            DigestionRate = digestionRate;
            WaterLossMultiplier = waterLossMultiplier;
            BasalEnergyCostMultiplier = basalEnergyCostMultiplier;
            AttackPower = attackPower;
            Defense = defense;
            Maneuverability = maneuverability;
            FearResponse = fearResponse;
            Aggression = aggression;
            PlantFoodYieldMultiplier = plantFoodYieldMultiplier;
            MeatYieldMultiplier = meatYieldMultiplier;
            MemoryConfidenceDecayPerSecond = memoryConfidenceDecayPerSecond;
            CognitionRestCostMultiplier = cognitionRestCostMultiplier;
            TemperatureTolerance = temperatureTolerance;
            LearningRate = learningRate;
            Exploration = exploration;
            ReproductionCooldownSeconds = reproductionCooldownSeconds;
            ReproductionEnergyCostFraction = reproductionEnergyCostFraction;
            MaximumAgeSeconds = maximumAgeSeconds;
            Persistence = persistence;
        }

        public float BodyMass { get; }
        public float EnergyCapacity { get; }
        public float HydrationCapacity { get; }
        public float HealthCapacity { get; }
        public float MaximumSpeed { get; }
        public float VisionRange { get; }
        public float FoodYield { get; }
        public float IngestionRate { get; }
        public float DigestionRate { get; }
        public float WaterLossMultiplier { get; }
        public float BasalEnergyCostMultiplier { get; }
        public float AttackPower { get; }
        public float Defense { get; }
        public float Maneuverability { get; }
        public float FearResponse { get; }
        public float Aggression { get; }
        public float PlantFoodYieldMultiplier { get; }
        public float MeatYieldMultiplier { get; }
        public float MemoryConfidenceDecayPerSecond { get; }
        public float CognitionRestCostMultiplier { get; }
        public float TemperatureTolerance { get; }
        public float LearningRate { get; }
        public float Exploration { get; }
        public float ReproductionCooldownSeconds { get; }
        public float ReproductionEnergyCostFraction { get; }
        public float MaximumAgeSeconds { get; }
        public float Persistence { get; }

        public Phenotype WithJuvenileScaling(float multiplier)
        {
            return new Phenotype(
                BodyMass,
                EnergyCapacity,
                HydrationCapacity,
                HealthCapacity,
                MaximumSpeed * multiplier,
                VisionRange * multiplier,
                FoodYield,
                IngestionRate,
                DigestionRate,
                WaterLossMultiplier,
                BasalEnergyCostMultiplier,
                AttackPower * multiplier,
                Defense * multiplier,
                Maneuverability * multiplier,
                FearResponse,
                Aggression,
                PlantFoodYieldMultiplier,
                MeatYieldMultiplier,
                MemoryConfidenceDecayPerSecond,
                CognitionRestCostMultiplier,
                TemperatureTolerance,
                LearningRate,
                Exploration,
                ReproductionCooldownSeconds,
                ReproductionEnergyCostFraction,
                MaximumAgeSeconds,
                Persistence);
        }

        /// <summary>
        /// <paramref name="metabolicIngestionEnabled"/> gives <see cref="Genome.MetabolicPace"/> the
        /// benefit it never had.
        ///
        /// <para>Without it the gene is a <b>pure cost</b>: it raises the water drain
        /// (<c>NeedsSystem.cs:49</c>) and the energy drain (<c>NeedsSystem.cs:45</c>) by 2.14x across
        /// its range and has no third reader anywhere - nothing converts a faster metabolism into
        /// food, yield or speed, so <c>DigestionRate</c> does not in fact make digestion faster. The
        /// population is selling it: downward in five of six measured conditions. See
        /// <c>docs/experiments/p6-metabolic-pace-is-a-pure-cost-2026-08-24.md</c>.</para>
        ///
        /// <para>With it, ingestion is scaled by <b>the same</b> <c>0.7 + 0.8*pace</c> factor the two
        /// drains already use, so a creature with twice the metabolism burns twice as fast and eats
        /// twice as fast. That is deliberately not a free win: <b>the cost is paid every second and
        /// the benefit only while standing at food that still has some left</b>, which should make
        /// the gene's optimum depend on how much there is to eat rather than sit at zero.</para>
        /// </summary>
        public static Phenotype FromGenome(Genome genome, bool metabolicIngestionEnabled = false)
        {
            float bodyMass = 0.6f * (float)Math.Pow(4d, genome.BodySize);
            float maintenance = 1f
                + (0.08f * genome.MovementSpeed)
                + (0.05f * genome.VisionRange)
                + (0.07f * genome.WaterEfficiency)
                + (0.04f * genome.FoodEfficiency)
                + (0.10f * genome.Attack)
                + (0.10f * genome.Defense)
                + (0.08f * genome.Maneuverability)
                + (0.03f * genome.Fear)
                + (0.04f * genome.Aggression)
                + (0.04f * genome.DietSpecialization)
                + (0.08f * genome.MemoryCapacity)
                + (0.05f * genome.MemoryRetention)
                + (0.04f * genome.LearningRate)
                + (0.02f * genome.Exploration)
                + (0.06f * genome.TemperatureTolerance)
                + (0.08f * genome.FertilityInvestment)
                + (0.07f * genome.LifespanTendency)
                + (0.05f * genome.Persistence);

            return new Phenotype(
                bodyMass,
                bodyMass * 100f,
                (float)Math.Pow(bodyMass, 0.8d) * 50f,
                (float)Math.Pow(bodyMass, 0.67d) * 100f,
                1f + (3f * genome.MovementSpeed),
                4f + (12f * genome.VisionRange),
                0.75f + (0.65f * genome.FoodEfficiency),
                metabolicIngestionEnabled
                    ? (1.25f - (0.3f * genome.FoodEfficiency)) * (0.7f + (0.8f * genome.MetabolicPace))
                    : 1.25f - (0.3f * genome.FoodEfficiency),
                0.7f + (0.8f * genome.MetabolicPace),
                1f - (0.55f * genome.WaterEfficiency),
                (float)Math.Pow(bodyMass, 0.75d) * (0.7f + (0.8f * genome.MetabolicPace)) * maintenance,
                genome.Attack * (0.75f + (0.5f * bodyMass)),
                genome.Defense * (0.75f + (0.5f * bodyMass)),
                1f + (2f * genome.Maneuverability),
                genome.Fear,
                genome.Aggression,
                1f - (0.3f * genome.DietSpecialization),
                0.5f + genome.DietSpecialization,
                0.12f - (0.10f * genome.MemoryRetention),
                1f + (0.6f * genome.MemoryCapacity) + (0.25f * genome.LearningRate),
                2f + (8f * genome.TemperatureTolerance),
                genome.LearningRate,
                genome.Exploration,
                16f - (8f * genome.FertilityInvestment),
                0.15f + (0.20f * genome.FertilityInvestment),
                90f + (180f * genome.LifespanTendency),
                genome.Persistence);
        }
    }
}
