using System.Collections.Generic;
using LifeSimulation.Presentation;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class HudLayoutTests
    {
        private static readonly HudDetail[] AllDetails = { HudDetail.Full, HudDetail.Compact, HudDetail.Hidden };

        /// <summary>
        /// The bug this file exists for. The HUD drew Predation and the colour legend both at y=216,
        /// and three more pairs collided whenever the elevation field was on - which is every
        /// ecosystem configuration. It was found by reading, not by a test, and only after months.
        /// </summary>
        [Test]
        public void NoTwoPanelsOverlapInAnyModeOrConfiguration()
        {
            foreach (HudDetail detail in AllDetails)
            {
                foreach (bool elevation in new[] { false, true })
                {
                    foreach (bool selected in new[] { false, true })
                    {
                        List<HudSection> sections = HudLayout.VisibleSections(detail, elevation, selected);
                        for (int first = 0; first < sections.Count; first++)
                        {
                            for (int second = first + 1; second < sections.Count; second++)
                            {
                                HudRect a = HudLayout.RectOf(sections[first]);
                                HudRect b = HudLayout.RectOf(sections[second]);
                                Assert.That(a.Overlaps(b), Is.False,
                                    $"{sections[first]} overlaps {sections[second]} at {detail}, elevation {elevation}, selected {selected}");
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void TabCyclesFullToCompactToHiddenAndBack()
        {
            Assert.That(HudLayout.Next(HudDetail.Full), Is.EqualTo(HudDetail.Compact));
            Assert.That(HudLayout.Next(HudDetail.Compact), Is.EqualTo(HudDetail.Hidden));
            Assert.That(HudLayout.Next(HudDetail.Hidden), Is.EqualTo(HudDetail.Full));
        }

        [Test]
        public void HiddenDrawsNothing()
        {
            Assert.That(HudLayout.VisibleSections(HudDetail.Hidden, true, true), Is.Empty);
        }

        /// <summary>
        /// The user's actual complaint, as a number: at full detail the HUD needs more than a
        /// 1,280 x 720 window, so on one it is the whole screen.
        /// </summary>
        [Test]
        public void FullDetailDoesNotFitA720pWindowAndCompactComfortablyDoes()
        {
            HudRect full = HudLayout.Bounds(HudDetail.Full, elevationFieldEnabled: true, creatureSelected: true);
            Assert.That(full.Bottom, Is.GreaterThan(720f));
            Assert.That(full.Right, Is.GreaterThan(1200f));

            HudRect compact = HudLayout.Bounds(HudDetail.Compact, elevationFieldEnabled: true, creatureSelected: true);
            Assert.That(compact.Right, Is.LessThan(500f));
            Assert.That(compact.Bottom, Is.LessThan(640f));
        }

        [Test]
        public void CompactKeepsTheStatusBoxAndOnlyShowsTheInspectorWhenSomethingIsSelected()
        {
            List<HudSection> nothingSelected = HudLayout.VisibleSections(HudDetail.Compact, true, creatureSelected: false);
            Assert.That(nothingSelected, Is.EqualTo(new[] { HudSection.Status }));

            List<HudSection> selected = HudLayout.VisibleSections(HudDetail.Compact, true, creatureSelected: true);
            Assert.That(selected, Is.EqualTo(new[] { HudSection.Status, HudSection.Inspector }));
        }

        [Test]
        public void TheTerrainControlsAppearOnlyWithTheElevationField()
        {
            // Plain bool checks: Unity's NUnit resolves Does.Not.Contain to the string overload and
            // will not compile the collection form, while dotnet test accepts it. The Unity batch
            // compile is the arbiter for anything under Assets.
            Assert.That(HudLayout.VisibleSections(HudDetail.Full, elevationFieldEnabled: false, creatureSelected: false)
                .Contains(HudSection.TerrainControls), Is.False);
            Assert.That(HudLayout.VisibleSections(HudDetail.Full, elevationFieldEnabled: true, creatureSelected: false)
                .Contains(HudSection.TerrainControls), Is.True);
        }

        [Test]
        public void CompactRemovesMostOfWhatFullDraws()
        {
            int full = HudLayout.VisibleSections(HudDetail.Full, true, true).Count;
            int compact = HudLayout.VisibleSections(HudDetail.Compact, true, true).Count;

            Assert.That(compact, Is.LessThan(full / 2));
        }

        [Test]
        public void TouchingEdgesAreNotAnOverlap()
        {
            var left = new HudRect(0f, 0f, 10f, 10f);
            var right = new HudRect(10f, 0f, 10f, 10f);

            Assert.That(left.Overlaps(right), Is.False);
            Assert.That(left.Overlaps(new HudRect(9f, 0f, 10f, 10f)), Is.True);
        }
    }
}
