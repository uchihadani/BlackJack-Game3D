using System;
using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.SpecialCards;
using TwentyThree.Editor;
using TwentyThree.Infrastructure.Configuration;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TwentyThree.Tests.EditMode.Configuration
{
    public sealed class GameRulesConfigurationTests
    {
        [Test]
        public void ApprovedAssetContainsTheTechnicalSpecificationValues()
        {
            GameRulesConfiguration configuration =
                AssetDatabase.LoadAssetAtPath<GameRulesConfiguration>(
                    PhaseTwoConfigurationBuilder.GameRulesConfigurationPath);

            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.InitialMoneyMinorUnits, Is.EqualTo(5000));
            Assert.That(configuration.InitialDebtMinorUnits, Is.EqualTo(20000));
            Assert.That(configuration.LaterCycleDebtMinorUnits, Is.EqualTo(new long[] { 40000, 80000, 160000 }));
            Assert.That(configuration.MinimumBetMinorUnits, Is.EqualTo(500));
            Assert.That(configuration.MaximumBetByRoundMinorUnits, Is.EqualTo(new long[] { 2000, 2500, 3000, 3500 }));
            Assert.That(configuration.InterestPerRoundBasisPoints, Is.EqualTo(1500));
            Assert.That(configuration.NormalWinBasisPoints, Is.EqualTo(9200));
            Assert.That(configuration.AllInWinBasisPoints, Is.EqualTo(11000));
            Assert.That(configuration.InitialTwentyThreeBasisPoints, Is.EqualTo(12000));
            Assert.That(configuration.AllInInitialTwentyThreeBasisPoints, Is.EqualTo(15000));
            Assert.That(configuration.HandsPerRound, Is.EqualTo(5));
            Assert.That(configuration.RoundsPerCycle, Is.EqualTo(4));
            Assert.That(configuration.DealerStandThreshold, Is.EqualTo(17));
            Assert.That(configuration.MinimumCardsToStartHand, Is.EqualTo(6));

            var rules = configuration.CreateRules();
            Assert.That(rules.InitialMoney, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(rules.GetDebtForCycle(1), Is.EqualTo(Money.FromCoins(200)));
            Assert.That(rules.GetDebtForCycle(4), Is.EqualTo(Money.FromCoins(1600)));
            Assert.That(rules.GetMaximumBetForRound(4), Is.EqualTo(Money.FromCoins(35)));
            Assert.That(rules.PhaseThree.IsEnabled, Is.True);
            Assert.That(rules.PhaseThree.SpecialCards.Definitions.Count, Is.EqualTo(5));
            Assert.That(
                rules.PhaseThree.SpecialCards.Get(SpecialCardId.WeHaveADeal).RejectionCost,
                Is.EqualTo(Money.FromCoins(16)));
            Assert.That(
                rules.PhaseThree.SpecialCards.Get(SpecialCardId.ThirdEye).AppliedPressureDelta,
                Is.EqualTo(40));
            Assert.That(rules.PhaseThree.Events.Definitions.Count, Is.EqualTo(3));
            Assert.That(
                rules.PhaseThree.Events.Get(GameEventId.VoicesFromBeyond).BaseProbability,
                Is.EqualTo(new BasisPoints(1500)));
            Assert.That(
                rules.PhaseThree.Events.DistractedNetGainBonus,
                Is.EqualTo(new BasisPoints(2000)));
            Assert.That(rules.PhaseThree.Items.Definitions.Count, Is.EqualTo(5));
            Assert.That(
                rules.PhaseThree.Items.TryGetDefinition(ItemId.IT05, out ItemDefinition luckyCoin),
                Is.True);
            Assert.That(luckyCoin.BasePurchasePrice, Is.EqualTo(Money.FromCoins(35)));
            Assert.That(rules.PhaseThree.VoluntaryHitPressure, Is.EqualTo(10));
            Assert.That(rules.PhaseThree.VoluntaryHitAfterLossPressure, Is.EqualTo(15));
            Assert.That(rules.PhaseThree.ProtectedFundsCapacity, Is.EqualTo(Money.FromCoins(20)));
        }

        [Test]
        public void InvalidSerializedValuesAreRejectedWhenRulesAreCreated()
        {
            GameRulesConfiguration configuration = ScriptableObject.CreateInstance<GameRulesConfiguration>();
            try
            {
                SerializedObject serialized = new SerializedObject(configuration);
                serialized.FindProperty("initialMoneyMinorUnits").longValue = -1;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CreateRules());
            }
            finally
            {
                Object.DestroyImmediate(configuration);
            }
        }

        [Test]
        public void IncompleteRoundMaximumScheduleIsRejected()
        {
            GameRulesConfiguration configuration = ScriptableObject.CreateInstance<GameRulesConfiguration>();
            try
            {
                SerializedObject serialized = new SerializedObject(configuration);
                SerializedProperty maximums = serialized.FindProperty("maximumBetByRoundMinorUnits");
                maximums.arraySize = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.Throws<ArgumentException>(() => configuration.CreateRules());
            }
            finally
            {
                Object.DestroyImmediate(configuration);
            }
        }

        [Test]
        public void ImpossibleDeckThresholdIsRejected()
        {
            GameRulesConfiguration configuration = ScriptableObject.CreateInstance<GameRulesConfiguration>();
            try
            {
                SerializedObject serialized = new SerializedObject(configuration);
                serialized.FindProperty("minimumCardsToStartHand").intValue = 53;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CreateRules());
            }
            finally
            {
                Object.DestroyImmediate(configuration);
            }
        }
    }
}
