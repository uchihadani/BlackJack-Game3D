using System;
using NUnit.Framework;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Tests.EditMode.Psychology
{
    public sealed class PsychologyStateTests
    {
        [Test]
        public void NewRunUsesEveryConfiguredInitialPsychologyValue()
        {
            PsychologyState state = new PsychologyState();

            Assert.That(state.Pressure, Is.Zero);
            Assert.That(state.PressureBand, Is.EqualTo(PressureBand.Control));
            Assert.That(state.PressureRuptureTriggered, Is.False);
            Assert.That(state.Lucidity, Is.EqualTo(100));
            Assert.That(state.LucidityMaximum, Is.EqualTo(100));
            Assert.That(state.LucidityBand, Is.EqualTo(LucidityBand.Clear));
            Assert.That(state.DealerRelationship, Is.EqualTo(DealerRelationship.Empty));
        }

        [Test]
        public void SnapshotRoundTripPreservesAllPersistentPsychologyState()
        {
            PsychologyState source = new PsychologyState();
            source.AdjustLucidity(LucidityDelta.DefaultFor(
                LucidityAdjustmentCause.It04CigaretteBoxUsed));
            source.ReduceLucidityMaximum(LucidityMaximumReduction.SurrenderedEyeDefault());
            source.ApplyPressure(new PressureDelta(
                PressureChangeCause.Sc05DealerDealAccepted,
                100));
            source.ApplyPressure(PressureDelta.DefaultFor(
                PressureChangeCause.It04CigaretteBoxUsed));
            source.ApplyDealerRelationship(DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.Sc05DealerDealAccepted));

            PsychologySnapshot snapshot = source.CreateSnapshot();
            PsychologyState restored = PsychologyState.FromSnapshot(snapshot);

            Assert.That(restored.Pressure, Is.EqualTo(85));
            Assert.That(restored.PressureBand, Is.EqualTo(PressureBand.Collapse));
            Assert.That(restored.PressureRuptureTriggered, Is.True);
            Assert.That(restored.Lucidity, Is.EqualTo(45));
            Assert.That(restored.LucidityMaximum, Is.EqualTo(70));
            Assert.That(restored.LucidityBand, Is.EqualTo(LucidityBand.Unstable));
            Assert.That(restored.DealerRelationship.Dependence, Is.EqualTo(25));
            Assert.That(restored.DealerRelationship.Greed, Is.EqualTo(15));
            Assert.That(restored.CreateSnapshot().DealerRelationship, Is.EqualTo(snapshot.DealerRelationship));
        }

        [Test]
        public void RestoredRuptureFlagKeepsThePenaltyIdempotent()
        {
            PsychologySnapshot snapshot = new PsychologySnapshot(
                85,
                true,
                55,
                70,
                new DealerRelationship(0, 15, 25, 10));
            PsychologyState state = PsychologyState.FromSnapshot(snapshot);

            PressureChangeResult result = state.ApplyPressure(
                PressureDelta.DefaultFor(PressureChangeCause.AllInConfirmed));

            Assert.That(state.Pressure, Is.EqualTo(100));
            Assert.That(state.Lucidity, Is.EqualTo(55));
            Assert.That(state.PressureRuptureTriggered, Is.True);
            Assert.That(result.DidTriggerRupture, Is.False);
        }

        [Test]
        public void SnapshotRejectsImpossibleStateCombinations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PsychologySnapshot(-1, false, 100, 100, DealerRelationship.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PsychologySnapshot(101, true, 100, 100, DealerRelationship.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PsychologySnapshot(0, false, -1, 100, DealerRelationship.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PsychologySnapshot(0, false, 71, 70, DealerRelationship.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PsychologySnapshot(0, false, 0, -1, DealerRelationship.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PsychologySnapshot(0, false, 100, 101, DealerRelationship.Empty));
            Assert.Throws<ArgumentException>(
                () => new PsychologySnapshot(100, false, 75, 100, DealerRelationship.Empty));
        }

        [Test]
        public void NullSnapshotIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => PsychologyState.FromSnapshot(null));
        }
    }
}
