using LifeSimulation.Simulation.Analysis;
using LifeSimulation.Simulation.Biology;
using LifeSimulation.Simulation.Core;
using NUnit.Framework;

namespace LifeSimulation.Tests.EditMode
{
    public sealed class GeneticClusterRelationTests
    {
        [Test]
        public void DirectSurvivorsProvideCompleteStrongSupportAtDepthZero()
        {
            var creatures = new CreatureStore(2);
            creatures.Add(Genome.Neutral);
            creatures.Add(Genome.Neutral);
            GeneticClusterObservation previous = Observe(10, creatures);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            GeneticClusterObservation current = Observe(20, creatures);

            GeneticClusterRelation relation = GeneticClusterRelation.Create(previous, 0, current, 0, ancestry, StrictPolicy(maximumAncestorGenerations: 1));

            Assert.That(relation.PreviousClusterMemberCount, Is.EqualTo(2));
            Assert.That(relation.CurrentClusterMemberCount, Is.EqualTo(2));
            Assert.That(relation.DirectSupportCount, Is.EqualTo(2));
            Assert.That(relation.DescendantOnlySupportCount, Is.EqualTo(0));
            Assert.That(relation.SupportedCurrentMemberCount, Is.EqualTo(2));
            Assert.That(relation.CurrentSupportFraction, Is.EqualTo(1f));
            Assert.That(relation.SupportingPreviousMemberCount, Is.EqualTo(2));
            Assert.That(relation.PreviousSupportFraction, Is.EqualTo(1f));
            Assert.That(relation.MinimumSupportingAncestorDepth, Is.EqualTo(0));
            Assert.That(relation.MaximumSupportingAncestorDepth, Is.EqualTo(0));
            Assert.That(relation.AncestryCoverageIsComplete, Is.True);
            Assert.That(relation.IsStrong, Is.True);
        }

        [Test]
        public void RecordedChildrenProvideDescendantOnlySupport()
        {
            var creatures = new CreatureStore(4);
            CreatureId firstParent = creatures.Add(Genome.Neutral);
            CreatureId secondParent = creatures.Add(Genome.Neutral);
            GeneticClusterObservation previous = Observe(10, creatures);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            CreatureId firstChild = creatures.Add(Genome.Neutral);
            CreatureId secondChild = creatures.Add(Genome.Neutral);
            ancestry.Record(Birth(15, firstChild, firstParent));
            ancestry.Record(new SimulationEvent(15, SimulationEventKind.Birth, secondChild, default, secondParent, DeathCause.None));
            creatures.Remove(firstParent);
            creatures.Remove(secondParent);
            GeneticClusterObservation current = Observe(20, creatures);

            GeneticClusterRelation relation = GeneticClusterRelation.Create(previous, 0, current, 0, ancestry, StrictPolicy(maximumAncestorGenerations: 1));

            Assert.That(relation.DirectSupportCount, Is.EqualTo(0));
            Assert.That(relation.DescendantOnlySupportCount, Is.EqualTo(2));
            Assert.That(relation.SupportedCurrentMemberCount, Is.EqualTo(2));
            Assert.That(relation.CurrentSupportFraction, Is.EqualTo(1f));
            Assert.That(relation.SupportingPreviousMemberCount, Is.EqualTo(2));
            Assert.That(relation.PreviousSupportFraction, Is.EqualTo(1f));
            Assert.That(relation.MinimumSupportingAncestorDepth, Is.EqualTo(1));
            Assert.That(relation.MaximumSupportingAncestorDepth, Is.EqualTo(1));
            Assert.That(relation.AncestryCoverageIsComplete, Is.True);
            Assert.That(relation.IsStrong, Is.True);
        }

        [Test]
        public void MaximumAncestorDepthBlocksGrandchildSupport()
        {
            var creatures = new CreatureStore(3);
            CreatureId founder = creatures.Add(Genome.Neutral);
            GeneticClusterObservation previous = Observe(10, creatures);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            CreatureId child = creatures.Add(Genome.Neutral);
            CreatureId grandchild = creatures.Add(Genome.Neutral);
            ancestry.Record(Birth(15, child, founder));
            ancestry.Record(Birth(20, grandchild, child));
            creatures.Remove(founder);
            creatures.Remove(child);
            GeneticClusterObservation current = Observe(25, creatures);

            GeneticClusterRelation relation = GeneticClusterRelation.Create(previous, 0, current, 0, ancestry, StrictPolicy(maximumAncestorGenerations: 1));

            Assert.That(relation.SupportedCurrentMemberCount, Is.EqualTo(0));
            Assert.That(relation.CurrentSupportFraction, Is.EqualTo(0f));
            Assert.That(relation.SupportingPreviousMemberCount, Is.EqualTo(0));
            Assert.That(relation.PreviousSupportFraction, Is.EqualTo(0f));
            Assert.That(relation.MinimumSupportingAncestorDepth, Is.EqualTo(-1));
            Assert.That(relation.MaximumSupportingAncestorDepth, Is.EqualTo(-1));
            Assert.That(relation.AncestryCoverageIsComplete, Is.True);
            Assert.That(relation.IsStrong, Is.False);
        }

        [Test]
        public void MissingParentRecordMakesUnsupportedRelationIncomplete()
        {
            var creatures = new CreatureStore(3);
            CreatureId founder = creatures.Add(Genome.Neutral);
            GeneticClusterObservation previous = Observe(10, creatures);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            CreatureId missingParent = creatures.Add(Genome.Neutral);
            CreatureId child = creatures.Add(Genome.Neutral);
            ancestry.Record(Birth(20, child, missingParent));
            creatures.Remove(founder);
            creatures.Remove(missingParent);
            GeneticClusterObservation current = Observe(25, creatures);

            GeneticClusterRelation relation = GeneticClusterRelation.Create(previous, 0, current, 0, ancestry, StrictPolicy(maximumAncestorGenerations: 2));

            Assert.That(relation.SupportedCurrentMemberCount, Is.EqualTo(0));
            Assert.That(relation.AncestryCoverageIsComplete, Is.False);
            Assert.That(relation.IsStrong, Is.False);
        }

        [Test]
        public void ProlificAncestorFailsPreviousClusterSupportThreshold()
        {
            var creatures = new CreatureStore(5);
            CreatureId prolificParent = creatures.Add(Genome.Neutral);
            CreatureId unrepresentedParent = creatures.Add(Genome.Neutral);
            GeneticClusterObservation previous = Observe(10, creatures);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            CreatureId firstChild = creatures.Add(Genome.Neutral);
            CreatureId secondChild = creatures.Add(Genome.Neutral);
            CreatureId thirdChild = creatures.Add(Genome.Neutral);
            ancestry.Record(Birth(20, firstChild, prolificParent));
            ancestry.Record(Birth(20, secondChild, prolificParent));
            ancestry.Record(Birth(20, thirdChild, prolificParent));
            creatures.Remove(prolificParent);
            creatures.Remove(unrepresentedParent);
            GeneticClusterObservation current = Observe(25, creatures);
            var policy = new ClusterHistoryPolicy(3, 1f, 2, 1f, 1, 1, 1);

            GeneticClusterRelation relation = GeneticClusterRelation.Create(previous, 0, current, 0, ancestry, policy);

            Assert.That(relation.SupportedCurrentMemberCount, Is.EqualTo(3));
            Assert.That(relation.CurrentSupportFraction, Is.EqualTo(1f));
            Assert.That(relation.SupportingPreviousMemberCount, Is.EqualTo(1));
            Assert.That(relation.PreviousSupportFraction, Is.EqualTo(.5f));
            Assert.That(relation.AncestryCoverageIsComplete, Is.True);
            Assert.That(relation.IsStrong, Is.False);
        }

        [Test]
        public void AncestryCycleMakesRelationIncomplete()
        {
            var creatures = new CreatureStore(3);
            CreatureId previousMember = creatures.Add(Genome.Neutral);
            GeneticClusterObservation previous = Observe(10, creatures);
            var ancestry = new AncestryHistory();
            ancestry.RecordFounders(0, creatures);
            CreatureId cycleParent = creatures.Add(Genome.Neutral);
            CreatureId currentMember = creatures.Add(Genome.Neutral);
            ancestry.Record(Birth(15, currentMember, cycleParent));
            ancestry.Record(Birth(15, cycleParent, currentMember));
            creatures.Remove(previousMember);
            creatures.Remove(cycleParent);
            GeneticClusterObservation current = Observe(20, creatures);

            GeneticClusterRelation relation = GeneticClusterRelation.Create(previous, 0, current, 0, ancestry, StrictPolicy(maximumAncestorGenerations: 3));

            Assert.That(relation.SupportedCurrentMemberCount, Is.EqualTo(0));
            Assert.That(relation.AncestryCoverageIsComplete, Is.False);
            Assert.That(relation.IsStrong, Is.False);
        }

        [Test]
        public void CrossBranchAncestorCycleMakesRelationIncomplete()
        {
            var creatures = new CreatureStore(3);
            CreatureId firstPreviousMember = creatures.Add(Genome.Neutral);
            CreatureId secondPreviousMember = creatures.Add(Genome.Neutral);
            GeneticClusterObservation previous = Observe(10, creatures);
            CreatureId currentMember = creatures.Add(Genome.Neutral);
            var ancestry = new AncestryHistory();
            ancestry.Record(new SimulationEvent(0, SimulationEventKind.Birth, firstPreviousMember, secondPreviousMember, default, DeathCause.None));
            ancestry.Record(new SimulationEvent(0, SimulationEventKind.Birth, secondPreviousMember, firstPreviousMember, default, DeathCause.None));
            ancestry.Record(new SimulationEvent(15, SimulationEventKind.Birth, currentMember, firstPreviousMember, secondPreviousMember, DeathCause.None));
            creatures.Remove(firstPreviousMember);
            creatures.Remove(secondPreviousMember);
            GeneticClusterObservation current = Observe(20, creatures);
            var policy = new ClusterHistoryPolicy(1, 1f, 2, 1f, 3, 1, 1);

            GeneticClusterRelation relation = GeneticClusterRelation.Create(previous, 0, current, 0, ancestry, policy);

            Assert.That(relation.SupportedCurrentMemberCount, Is.EqualTo(1));
            Assert.That(relation.SupportingPreviousMemberCount, Is.EqualTo(2));
            Assert.That(relation.AncestryCoverageIsComplete, Is.False);
            Assert.That(relation.IsStrong, Is.False);
        }

        private static GeneticClusterObservation Observe(long tick, CreatureStore creatures)
        {
            return GeneticClusterObservation.Create(PopulationGenomeSnapshot.Capture(tick, creatures), threshold: 0f);
        }

        private static SimulationEvent Birth(long tick, CreatureId child, CreatureId firstParent)
        {
            return new SimulationEvent(tick, SimulationEventKind.Birth, child, firstParent, default, DeathCause.None);
        }

        private static ClusterHistoryPolicy StrictPolicy(int maximumAncestorGenerations)
        {
            return new ClusterHistoryPolicy(2, 1f, 2, 1f, maximumAncestorGenerations, 1, 1);
        }
    }
}
