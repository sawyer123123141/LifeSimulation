using LifeSimulation.Presentation;
using LifeSimulation.Simulation.Resources;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    /// <summary>
    /// P4a asks that resource recovery be visible without reading logs. These pin the rule that
    /// makes it visible, which lived in the presenter where nothing could see it.
    /// </summary>
    public sealed class ResourceMarkerAppearanceTests
    {
        [Test]
        public void AFullSiteIsTallAndAtFullColour()
        {
            ResourceMarkerAppearance marker = ResourceMarkerAppearance.For(ResourceKind.Food, 24f, 24f);

            Assert.That(marker.FillFraction, Is.EqualTo(1f));
            Assert.That(marker.Height, Is.EqualTo(ResourceMarkerAppearance.FullHeight));
            ResourceMarkerAppearance.BaseColor(ResourceKind.Food, out float red, out float green, out float blue);
            Assert.That(marker.Red, Is.EqualTo(red).Within(.0001f));
            Assert.That(marker.Green, Is.EqualTo(green).Within(.0001f));
            Assert.That(marker.Blue, Is.EqualTo(blue).Within(.0001f));
        }

        [Test]
        public void AnEmptiedSiteStillMarksItsPlace()
        {
            ResourceMarkerAppearance marker = ResourceMarkerAppearance.For(ResourceKind.Food, 0f, 24f);

            Assert.That(marker.Height, Is.EqualTo(ResourceMarkerAppearance.EmptyHeight));
            Assert.That(marker.Height, Is.GreaterThan(0f), "an empty site that vanished would read as no site at all");
            Assert.That(marker.FillFraction, Is.EqualTo(0f));
        }

        /// <summary>
        /// The whole point of the item: depletion and recovery have to be READABLE, which means the
        /// marker has to change by an amount an eye can see rather than merely change.
        /// </summary>
        [Test]
        public void HeightAndColourRiseWithFill()
        {
            ResourceMarkerAppearance empty = ResourceMarkerAppearance.For(ResourceKind.Food, 0f, 24f);
            ResourceMarkerAppearance half = ResourceMarkerAppearance.For(ResourceKind.Food, 12f, 24f);
            ResourceMarkerAppearance full = ResourceMarkerAppearance.For(ResourceKind.Food, 24f, 24f);

            Assert.That(half.Height, Is.GreaterThan(empty.Height));
            Assert.That(full.Height, Is.GreaterThan(half.Height));
            Assert.That(half.Red, Is.GreaterThan(empty.Red));
            Assert.That(full.Red, Is.GreaterThan(half.Red));

            // A full marker is more than six times the height of an empty one, and more than four
            // times the brightness. Numbers, so "you can see it" is a claim with a size.
            Assert.That(full.Height / empty.Height, Is.GreaterThan(6f));
            Assert.That(full.Red / empty.Red, Is.GreaterThan(4f));
        }

        [Test]
        public void EachKindKeepsItsOwnColour()
        {
            ResourceMarkerAppearance food = ResourceMarkerAppearance.For(ResourceKind.Food, 10f, 10f);
            ResourceMarkerAppearance water = ResourceMarkerAppearance.For(ResourceKind.Water, 10f, 10f);

            Assert.That(food.Red, Is.GreaterThan(food.Blue), "food reads amber");
            Assert.That(water.Blue, Is.GreaterThan(water.Red), "water reads blue");
        }

        [Test]
        public void OverfilledAndNegativeAmountsAreClamped()
        {
            Assert.That(ResourceMarkerAppearance.For(ResourceKind.Food, 100f, 10f).FillFraction, Is.EqualTo(1f));
            Assert.That(ResourceMarkerAppearance.For(ResourceKind.Food, -5f, 10f).FillFraction, Is.EqualTo(0f));
        }

        [Test]
        public void AZeroCapacitySiteReadsAsEmptyRatherThanDividingByZero()
        {
            ResourceMarkerAppearance marker = ResourceMarkerAppearance.For(ResourceKind.Food, 5f, 0f);

            Assert.That(marker.FillFraction, Is.EqualTo(0f));
            Assert.That(marker.Height, Is.EqualTo(ResourceMarkerAppearance.EmptyHeight));
        }
    }
}
