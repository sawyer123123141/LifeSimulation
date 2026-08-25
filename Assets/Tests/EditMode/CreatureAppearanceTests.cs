using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Biology;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// The genome-to-appearance mapping, tested where it is still pure arithmetic.
    ///
    /// <para>The applied half - materials, renderers, one primitive per creature - is deliberately
    /// not built yet and would be redone when real models land. This half would not.</para>
    /// </summary>
    public sealed class CreatureAppearanceTests
    {
        private const float Tolerance = 1e-5f;

        /// <summary>The first six genes are required by the constructor and matter to nothing here.</summary>
        private static Genome Gene(float thermal = 0.5f, float bodySize = 0.5f, float neutralMarker = 0.5f)
        {
            return new Genome(bodySize, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
                temperatureTolerance: thermal, neutralMarker: neutralMarker);
        }

        private static Genome WithThermal(float thermal)
        {
            return Gene(thermal);
        }

        [Test]
        public void FoundersLandOnTheTemperateColourRatherThanAnEndOfTheRamp()
        {
            // Founders are drawn around 0.50, so the midpoint is the colour a run starts at. If it
            // sat at either extreme, the first frame would already look like an adapted population.
            CreatureAppearance appearance = CreatureAppearanceRules.FromGenome(WithThermal(0.5f));

            Assert.That(appearance.Red, Is.EqualTo(0.92f).Within(Tolerance));
            Assert.That(appearance.Green, Is.EqualTo(0.90f).Within(Tolerance));
            Assert.That(appearance.Blue, Is.EqualTo(0.78f).Within(Tolerance));
        }

        [Test]
        public void ColdAndHeatAdaptedCreaturesReachTheEndsOfTheRamp()
        {
            CreatureAppearance cold = CreatureAppearanceRules.FromGenome(WithThermal(0f));
            CreatureAppearance hot = CreatureAppearanceRules.FromGenome(WithThermal(1f));

            Assert.That(cold.Blue, Is.EqualTo(0.95f).Within(Tolerance));
            Assert.That(cold.Red, Is.EqualTo(0.24f).Within(Tolerance));
            Assert.That(hot.Red, Is.EqualTo(0.95f).Within(Tolerance));
            Assert.That(hot.Blue, Is.EqualTo(0.14f).Within(Tolerance));
        }

        [Test]
        public void HueMovesMonotonicallyWithThermalToleranceAcrossTheWholeRange()
        {
            // A non-monotonic ramp would put two different adaptations at the same colour, which is
            // the one thing a channel carrying a trait must not do.
            float previousRed = -1f;
            float previousBlue = 2f;

            for (int step = 0; step <= 100; step++)
            {
                CreatureAppearance appearance = CreatureAppearanceRules.FromGenome(WithThermal(step / 100f));

                Assert.That(appearance.Red, Is.GreaterThanOrEqualTo(previousRed - Tolerance));
                Assert.That(appearance.Blue, Is.LessThanOrEqualTo(previousBlue + Tolerance));
                previousRed = appearance.Red;
                previousBlue = appearance.Blue;
            }
        }

        [Test]
        public void EveryChannelStaysInsideTheDisplayableRange()
        {
            for (int step = 0; step <= 100; step++)
            {
                CreatureAppearance appearance = CreatureAppearanceRules.FromGenome(WithThermal(step / 100f));

                Assert.That(appearance.Red, Is.InRange(0f, 1f));
                Assert.That(appearance.Green, Is.InRange(0f, 1f));
                Assert.That(appearance.Blue, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void ScaleReproducesTheBodySizeRampTheViewAlreadyApplies()
        {
            // Prototype1Presenter.Views.cs lerps 0.7 to 1.35 on BodySize. The mapping has to agree
            // with it exactly, or adopting this function silently resizes every creature.
            Assert.That(
                CreatureAppearanceRules.FromGenome(Gene(bodySize: 0f)).ScaleMultiplier,
                Is.EqualTo(0.7f).Within(Tolerance));
            Assert.That(
                CreatureAppearanceRules.FromGenome(Gene(bodySize: 0.5f)).ScaleMultiplier,
                Is.EqualTo(1.025f).Within(Tolerance));
            Assert.That(
                CreatureAppearanceRules.FromGenome(Gene(bodySize: 1f)).ScaleMultiplier,
                Is.EqualTo(1.35f).Within(Tolerance));
        }

        [Test]
        public void TheNeutralMarkerIsInvisible()
        {
            // The drift control must reach no channel. If it did, a drifting population and a
            // selected one would look the same, which is the confusion the control exists to prevent.
            CreatureAppearance low = CreatureAppearanceRules.FromGenome(
                Gene(0.3f, 0.6f, neutralMarker: 0f));
            CreatureAppearance high = CreatureAppearanceRules.FromGenome(
                Gene(0.3f, 0.6f, neutralMarker: 1f));

            Assert.That(high.Red, Is.EqualTo(low.Red).Within(Tolerance));
            Assert.That(high.Green, Is.EqualTo(low.Green).Within(Tolerance));
            Assert.That(high.Blue, Is.EqualTo(low.Blue).Within(Tolerance));
            Assert.That(high.ScaleMultiplier, Is.EqualTo(low.ScaleMultiplier).Within(Tolerance));
        }

        [Test]
        public void TheMappingIsPure()
        {
            Genome genome = Gene(0.77f, 0.42f);

            CreatureAppearance first = CreatureAppearanceRules.FromGenome(genome);
            CreatureAppearance second = CreatureAppearanceRules.FromGenome(genome);

            Assert.That(second.Red, Is.EqualTo(first.Red));
            Assert.That(second.Green, Is.EqualTo(first.Green));
            Assert.That(second.Blue, Is.EqualTo(first.Blue));
            Assert.That(second.ScaleMultiplier, Is.EqualTo(first.ScaleMultiplier));
        }

        [Test]
        public void TheSaturationPlateauIsStillDistinguishableFromTheFounderColour()
        {
            // Thermal tolerance plateaus near 0.78 rather than running to 1.0, so the colours that
            // matter are 0.50 against 0.78 - not 0 against 1. If those two were close, the channel
            // would carry a real adaptation invisibly.
            CreatureAppearance founder = CreatureAppearanceRules.FromGenome(WithThermal(0.50f));
            CreatureAppearance plateau = CreatureAppearanceRules.FromGenome(WithThermal(0.78f));

            float separation =
                System.Math.Abs(plateau.Red - founder.Red)
                + System.Math.Abs(plateau.Green - founder.Green)
                + System.Math.Abs(plateau.Blue - founder.Blue);

            Assert.That(separation, Is.GreaterThan(0.5f));
        }
    }
}
