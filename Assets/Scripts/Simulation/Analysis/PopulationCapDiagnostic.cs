#nullable enable annotations

using System;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;

namespace LifeSimulation.Simulation.Analysis
{
    /// <summary>
    /// Whether the population cap, rather than ecology, is what stopped creatures breeding.
    ///
    /// <para>The decomposition, all of it observable after <c>Step</c> on a reproduction tick:</para>
    /// <list type="bullet">
    /// <item>population at or above the cap <b>and</b> two or more creatures still pass
    /// <see cref="ReproductionSystem.CanReproduce"/> — the cap is a sufficient explanation for their
    /// not breeding, so the sample is cap-blocked.</item>
    /// <item>population below the cap with ready creatures — they failed to find a mate in range.
    /// That is ecology, not the cap.</item>
    /// </list>
    ///
    /// <para>This exists so that a reproductive-skew result can be labelled interpretable or not.
    /// <c>ReproductionSystem</c>'s <c>CreatureId</c>-ordered scheduler is a real source of artificial
    /// skew when the cap binds, and it is deliberately left exactly as it is: the decision to change
    /// it should be made on this evidence rather than ahead of it.</para>
    /// </summary>
    public sealed class PopulationCapDiagnostic
    {
        private readonly float _needFraction;
        private readonly float _unsafeCapBlockedFractionThreshold;

        /// <summary>
        /// <paramref name="unsafeCapBlockedFractionThreshold"/> has no default on purpose. A silent
        /// default would become a fact nobody chose.
        /// </summary>
        public PopulationCapDiagnostic(float needFraction, float unsafeCapBlockedFractionThreshold)
        {
            if (needFraction < 0f || needFraction > 1f) throw new ArgumentOutOfRangeException(nameof(needFraction));
            if (unsafeCapBlockedFractionThreshold < 0f || unsafeCapBlockedFractionThreshold > 1f) throw new ArgumentOutOfRangeException(nameof(unsafeCapBlockedFractionThreshold));
            _needFraction = needFraction;
            _unsafeCapBlockedFractionThreshold = unsafeCapBlockedFractionThreshold;
        }

        /// <summary>Reproduction ticks sampled. The denominator for both fractions below.</summary>
        public long ReproductionTickSamples { get; private set; }

        /// <summary>Samples where the population stood at or above the cap.</summary>
        public long SaturatedSamples { get; private set; }

        /// <summary>Saturated samples where at least two creatures were still ready to breed.</summary>
        public long CapBlockedSamples { get; private set; }

        /// <summary>Samples below the cap where at least two creatures were ready and did not pair.</summary>
        public long MateLimitedSamples { get; private set; }

        /// <summary>Ready creature-samples counted while the population was at the cap.</summary>
        public long ReadyButUnbredAtCap { get; private set; }

        /// <summary>Ready creature-samples counted while the population was below the cap.</summary>
        public long ReadyButUnbredBelowCap { get; private set; }

        public float CapSaturationFraction => ReproductionTickSamples == 0 ? 0f : (float)((double)SaturatedSamples / ReproductionTickSamples);

        public float CapBlockedFraction => ReproductionTickSamples == 0 ? 0f : (float)((double)CapBlockedSamples / ReproductionTickSamples);

        /// <summary>
        /// The run safety flag. When set, any statement about reproductive skew is confounded by the
        /// cap and by the ordering of the reproduction scheduler underneath it, and must say so.
        /// </summary>
        public bool ReproductiveSkewInterpretationIsUnsafe =>
            ReproductionTickSamples > 0 && CapBlockedFraction >= _unsafeCapBlockedFractionThreshold;

        /// <summary>Samples the world, but only on a reproduction tick.</summary>
        public bool SampleWorld(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!ReproductionBottleneck.IsReproductionTick(world.Config.Schedule, world.CurrentTick)) return false;

            Sample(world.CreatureCount, world.Config.MaximumPopulation, CountReadyToReproduce(world.Creatures, _needFraction));
            return true;
        }

        public void Sample(int population, int maximumPopulation, int readyToReproduce)
        {
            if (population < 0) throw new ArgumentOutOfRangeException(nameof(population));
            if (maximumPopulation <= 0) throw new ArgumentOutOfRangeException(nameof(maximumPopulation));
            if (readyToReproduce < 0) throw new ArgumentOutOfRangeException(nameof(readyToReproduce));

            ReproductionTickSamples++;

            bool saturated = population >= maximumPopulation;
            // Two, because one ready creature alone has nobody to breed with and the cap explains
            // nothing about it.
            bool hasAPairsWorthOfReadyCreatures = readyToReproduce >= 2;

            if (saturated)
            {
                SaturatedSamples++;
                ReadyButUnbredAtCap += readyToReproduce;
                if (hasAPairsWorthOfReadyCreatures) CapBlockedSamples++;
                return;
            }

            ReadyButUnbredBelowCap += readyToReproduce;
            if (hasAPairsWorthOfReadyCreatures) MateLimitedSamples++;
        }

        /// <summary>Counts with the production predicate rather than a copy of the gate.</summary>
        public static int CountReadyToReproduce(CreatureStore creatures, float needFraction)
        {
            if (creatures == null) throw new ArgumentNullException(nameof(creatures));
            int ready = 0;
            for (int index = 0; index < creatures.Count; index++)
            {
                if (ReproductionSystem.CanReproduce(
                        creatures.GetNeedsAt(index),
                        creatures.GetPhenotypeAt(index),
                        creatures.GetReproductionRefAt(index),
                        needFraction))
                {
                    ready++;
                }
            }

            return ready;
        }
    }
}
