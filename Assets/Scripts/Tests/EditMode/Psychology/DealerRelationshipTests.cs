using System;
using NUnit.Framework;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Tests.EditMode.Psychology
{
    public sealed class DealerRelationshipTests
    {
        [TestCase(DealerRelationshipChangeCause.AllInConfirmed, DealerRelationshipAxis.Greed, 10)]
        [TestCase(DealerRelationshipChangeCause.It01MagnifyingGlassUsed, DealerRelationshipAxis.Distrust, 10)]
        [TestCase(DealerRelationshipChangeCause.It03SecondChanceActivated, DealerRelationshipAxis.Dependence, 20)]
        [TestCase(DealerRelationshipChangeCause.Sc05DealerDealRejected, DealerRelationshipAxis.Distrust, 10)]
        public void SingleAxisDefaultsMatchTheTechnicalSheet(
            DealerRelationshipChangeCause cause,
            DealerRelationshipAxis expectedAxis,
            int expectedDelta)
        {
            DealerRelationshipChange change = DealerRelationshipChange.DefaultFor(cause);

            Assert.That(change.Cause, Is.EqualTo(cause));
            Assert.That(change.PrimaryAxis, Is.EqualTo(expectedAxis));
            Assert.That(change.PrimaryDelta, Is.EqualTo(expectedDelta));
            Assert.That(change.SecondaryAxis, Is.Null);
            Assert.That(change.SecondaryDelta, Is.Null);
        }

        [Test]
        public void AcceptedDealerDealDefinesDependenceBeforeGreed()
        {
            DealerRelationshipChange change = DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.Sc05DealerDealAccepted);

            Assert.That(change.PrimaryAxis, Is.EqualTo(DealerRelationshipAxis.Dependence));
            Assert.That(change.PrimaryDelta, Is.EqualTo(25));
            Assert.That(change.SecondaryAxis, Is.EqualTo(DealerRelationshipAxis.Greed));
            Assert.That(change.SecondaryDelta, Is.EqualTo(15));
        }

        [Test]
        public void EveryApprovedCauseChangesOnlyItsDeclaredAxes()
        {
            PsychologyState state = new PsychologyState();

            state.ApplyDealerRelationship(DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.AllInConfirmed));
            state.ApplyDealerRelationship(DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.It01MagnifyingGlassUsed));
            state.ApplyDealerRelationship(DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.It03SecondChanceActivated));
            DealerRelationshipChangeResult accepted = state.ApplyDealerRelationship(
                DealerRelationshipChange.DefaultFor(
                    DealerRelationshipChangeCause.Sc05DealerDealAccepted));
            state.ApplyDealerRelationship(DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.Sc05DealerDealRejected));

            Assert.That(state.DealerRelationship.Respect, Is.Zero);
            Assert.That(state.DealerRelationship.Greed, Is.EqualTo(25));
            Assert.That(state.DealerRelationship.Dependence, Is.EqualTo(45));
            Assert.That(state.DealerRelationship.Distrust, Is.EqualTo(20));
            Assert.That(accepted.ChangeCount, Is.EqualTo(2));
            Assert.That(accepted.GetChange(0).Axis, Is.EqualTo(DealerRelationshipAxis.Dependence));
            Assert.That(accepted.GetChange(1).Axis, Is.EqualTo(DealerRelationshipAxis.Greed));
            Assert.That(accepted.GetChange(0).Cause, Is.EqualTo(DealerRelationshipChangeCause.Sc05DealerDealAccepted));
            Assert.That(accepted.GetChange(1).Cause, Is.EqualTo(DealerRelationshipChangeCause.Sc05DealerDealAccepted));
        }

        [Test]
        public void RelationshipAxesClampIndependentlyAtOneHundred()
        {
            PsychologyState state = new PsychologyState();

            DealerRelationshipChangeResult result = state.ApplyDealerRelationship(
                new DealerRelationshipChange(
                    DealerRelationshipChangeCause.Sc05DealerDealAccepted,
                    int.MaxValue,
                    int.MaxValue));

            Assert.That(state.DealerRelationship.Respect, Is.Zero);
            Assert.That(state.DealerRelationship.Dependence, Is.EqualTo(100));
            Assert.That(state.DealerRelationship.Greed, Is.EqualTo(100));
            Assert.That(state.DealerRelationship.Distrust, Is.Zero);
            Assert.That(result.PrimaryChange.PreviousValue, Is.Zero);
            Assert.That(result.PrimaryChange.RequestedDelta, Is.EqualTo(int.MaxValue));
            Assert.That(result.PrimaryChange.AppliedDelta, Is.EqualTo(100));
            Assert.That(result.SecondaryChange.AppliedDelta, Is.EqualTo(100));
        }

        [Test]
        public void ReapplyingAtCapIsIdempotentAndStillProducesAnObservableResult()
        {
            PsychologyState state = new PsychologyState();
            DealerRelationshipChange maximumGreed = new DealerRelationshipChange(
                DealerRelationshipChangeCause.AllInConfirmed,
                100);
            state.ApplyDealerRelationship(maximumGreed);

            DealerRelationshipChangeResult result = state.ApplyDealerRelationship(
                DealerRelationshipChange.DefaultFor(DealerRelationshipChangeCause.AllInConfirmed));

            Assert.That(state.DealerRelationship.Greed, Is.EqualTo(100));
            Assert.That(result.ChangeCount, Is.EqualTo(1));
            Assert.That(result.PrimaryChange.PreviousValue, Is.EqualTo(100));
            Assert.That(result.PrimaryChange.RequestedDelta, Is.EqualTo(10));
            Assert.That(result.PrimaryChange.AppliedDelta, Is.Zero);
            Assert.That(result.PrimaryChange.ChangedValue, Is.False);
        }

        [Test]
        public void RelationshipResultsRemainSnapshotsAfterLaterChanges()
        {
            PsychologyState state = new PsychologyState();
            DealerRelationshipChanged firstEvent = state.ApplyDealerRelationship(
                    DealerRelationshipChange.DefaultFor(
                        DealerRelationshipChangeCause.It01MagnifyingGlassUsed))
                .PrimaryChange;

            state.ApplyDealerRelationship(DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.Sc05DealerDealRejected));

            Assert.That(firstEvent.PreviousValue, Is.Zero);
            Assert.That(firstEvent.FinalValue, Is.EqualTo(10));
            Assert.That(state.DealerRelationship.Distrust, Is.EqualTo(20));
        }

        [Test]
        public void RelationshipValueObjectValidatesEveryAxisAndSupportsExactLookup()
        {
            DealerRelationship relationship = new DealerRelationship(1, 2, 3, 4);

            Assert.That(relationship.GetValue(DealerRelationshipAxis.Respect), Is.EqualTo(1));
            Assert.That(relationship.GetValue(DealerRelationshipAxis.Greed), Is.EqualTo(2));
            Assert.That(relationship.GetValue(DealerRelationshipAxis.Dependence), Is.EqualTo(3));
            Assert.That(relationship.GetValue(DealerRelationshipAxis.Distrust), Is.EqualTo(4));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => relationship.GetValue((DealerRelationshipAxis)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DealerRelationship(-1, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DealerRelationship(0, 101, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DealerRelationship(0, 0, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DealerRelationship(0, 0, 0, 101));
        }

        [Test]
        public void RelationshipRequestsRejectInvalidShapesAndValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DealerRelationshipChange((DealerRelationshipChangeCause)999, 10));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DealerRelationshipChange(
                    DealerRelationshipChangeCause.AllInConfirmed,
                    -1));
            Assert.Throws<ArgumentException>(
                () => new DealerRelationshipChange(
                    DealerRelationshipChangeCause.AllInConfirmed,
                    10,
                    5));
            Assert.Throws<ArgumentException>(
                () => new DealerRelationshipChange(
                    DealerRelationshipChangeCause.Sc05DealerDealAccepted,
                    25));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DealerRelationshipChange(
                    DealerRelationshipChangeCause.Sc05DealerDealAccepted,
                    25,
                    -1));
        }

        [Test]
        public void InvalidResultIndexAndNullChangeAreRejectedWithoutMutation()
        {
            PsychologyState state = new PsychologyState();
            DealerRelationshipChangeResult result = state.ApplyDealerRelationship(
                DealerRelationshipChange.DefaultFor(DealerRelationshipChangeCause.AllInConfirmed));

            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetChange(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetChange(1));
            Assert.Throws<ArgumentNullException>(() => state.ApplyDealerRelationship(null));
            Assert.That(state.DealerRelationship.Greed, Is.EqualTo(10));
            Assert.That(state.DealerRelationship.Respect, Is.Zero);
            Assert.That(state.DealerRelationship.Dependence, Is.Zero);
            Assert.That(state.DealerRelationship.Distrust, Is.Zero);
        }
    }
}
