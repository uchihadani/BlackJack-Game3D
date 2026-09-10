using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TwentyThree.Application.Gameplay;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Dealer;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Randomness;
using TwentyThree.Domain.Rules;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Tests.EditMode.Gameplay
{
    public sealed class GameSessionPhaseThreeTests
    {
        [Test]
        public void SpecialCardSuspendsAndResumesTheOriginalNumericDraw()
        {
            GameSession session = CreateSession(Entries(
                DeckEntry.Special(SpecialCardId.PanicAttack),
                StandardHandEntries()));

            Assert.That(session.TryConfirmBet(Money.FromCoins(10)).Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.SpecialCardDecision));
            Assert.That(session.PendingContentDecision.TechnicalCode, Is.EqualTo("SC-02"));
            Assert.That(session.PlayerCards, Is.Empty);

            Assert.That(
                session.TryResolveContentDecision(ContentDecisionOption.Accept).Succeeded,
                Is.True);

            Assert.That(session.Phase, Is.EqualTo(GamePhase.PlayerTurn));
            Assert.That(session.PlayerCards, Has.Count.EqualTo(3));
            Assert.That(session.DealerCardCount, Is.EqualTo(3));
            Assert.That(session.Pressure, Is.EqualTo(20));
            Assert.That(session.ItemsBlockedForCurrentRound, Is.True);
            Assert.That(session.CurrentRoundDrawCount, Is.EqualTo(6));
        }

        [Test]
        public void DealerDealDoublesLiquidWealthAndAppliesPersistentConsequences()
        {
            GameSession session = CreateSession(Entries(
                DeckEntry.Special(SpecialCardId.WeHaveADeal),
                StandardHandEntries()));

            session.TryConfirmBet(Money.FromCoins(10));
            Assert.That(
                session.TryResolveContentDecision(ContentDecisionOption.Accept).Succeeded,
                Is.True);

            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(90)));
            Assert.That(session.EyeSurrendered, Is.True);
            Assert.That(session.LucidityMaximum, Is.EqualTo(70));
            Assert.That(session.Lucidity, Is.EqualTo(70));
            Assert.That(session.Pressure, Is.EqualTo(50));
            Assert.That(session.DealerRelationship.Dependence, Is.EqualTo(25));
            Assert.That(session.DealerRelationship.Greed, Is.EqualTo(15));
        }

        [Test]
        public void BeginnersLuckReshufflesOnlyTheRemainingPileWithItsOwnStream()
        {
            ScriptedRandomStreamFactory random = new ScriptedRandomStreamFactory();
            random.Set(RandomStreamKeys.Sc01Reshuffle, 123);
            GameSession session = CreateSession(
                Entries(
                    DeckEntry.Special(SpecialCardId.BeginnersLuck),
                    StandardHandEntries()),
                random,
                null,
                new SecondCallReverseEntryShuffler());

            session.TryConfirmBet(Money.FromCoins(5));
            session.TryResolveContentDecision(ContentDecisionOption.Accept);

            Assert.That(session.PlayerCards[0].Rank, Is.EqualTo(CardRank.Two));
            Assert.That(
                session.InternalHistory.Any(entry =>
                    entry.TechnicalCode == "SC-01/RESHUFFLE" &&
                    entry.StreamStateBefore.Value.Key == RandomStreamKeys.Sc01Reshuffle),
                Is.True);
        }

        [Test]
        public void BlackoutSanitizesEveryCardUntilResolutionBegins()
        {
            GameSession session = CreateSession(Entries(
                DeckEntry.Special(SpecialCardId.Blackout),
                StandardHandEntries()));

            session.TryConfirmBet(Money.FromCoins(5));
            session.TryResolveContentDecision(ContentDecisionOption.Accept);

            Assert.That(session.BlackoutActive, Is.True);
            Assert.That(session.ReadModel.PlayerCards.All(card => card.IsHidden), Is.True);
            Assert.That(session.ReadModel.DealerCards.All(card => card.IsHidden), Is.True);

            session.TryStand();

            Assert.That(session.BlackoutActive, Is.False);
            Assert.That(session.ReadModel.PlayerCards.All(card => !card.IsHidden), Is.True);
            Assert.That(session.ReadModel.DealerCards.All(card => !card.IsHidden), Is.True);
        }

        [Test]
        public void UnfundedDealerDealRejectionNeverMakesThePactMandatory()
        {
            GameSession session = CreateSession(Entries(
                DeckEntry.Special(SpecialCardId.WeHaveADeal),
                StandardHandEntries()));

            session.TryConfirmAllIn();
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.Zero));

            Assert.That(
                session.TryResolveContentDecision(ContentDecisionOption.Reject).Succeeded,
                Is.True);

            Assert.That(session.EyeSurrendered, Is.False);
            Assert.That(session.Pressure, Is.EqualTo(31));
            Assert.That(session.DealerRelationship.Greed, Is.EqualTo(10));
            Assert.That(session.DealerRelationship.Distrust, Is.EqualTo(10));
        }

        [Test]
        public void ThirdEyeChoosesAmongNumericCandidatesWithoutRemovingIntermediateSpecials()
        {
            GameSession session = CreateSession(new[]
            {
                DeckEntry.Special(SpecialCardId.ThirdEye),
                Entry(CardSuit.Hearts, CardRank.Two),
                DeckEntry.Special(SpecialCardId.PanicAttack),
                Entry(CardSuit.Clubs, CardRank.Three),
                Entry(CardSuit.Hearts, CardRank.Five),
                Entry(CardSuit.Clubs, CardRank.Ten),
                Entry(CardSuit.Diamonds, CardRank.Six),
                Entry(CardSuit.Spades, CardRank.Eight),
                Entry(CardSuit.Diamonds, CardRank.Seven),
                Entry(CardSuit.Spades, CardRank.Four)
            });

            session.TryConfirmBet(Money.FromCoins(5));
            session.TryResolveContentDecision(ContentDecisionOption.Accept);

            Assert.That(session.Phase, Is.EqualTo(GamePhase.SpecialCardChoice));
            Assert.That(
                session.TryResolveContentDecision(ContentDecisionOption.ChoiceB).Succeeded,
                Is.True);

            Assert.That(session.PlayerCards[0].Rank, Is.EqualTo(CardRank.Three));
            Assert.That(session.DealerVisibleCards[0].Rank, Is.EqualTo(CardRank.Two));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.SpecialCardDecision));
            Assert.That(session.PendingContentDecision.TechnicalCode, Is.EqualTo("SC-02"));
            Assert.That(session.Pressure, Is.EqualTo(40));
        }

        [Test]
        public void MeowUsesItsOwnStreamsAndAppliesARealOverride()
        {
            ScriptedRandomStreamFactory random = new ScriptedRandomStreamFactory();
            random.Set(RandomStreamKeys.Ev02Trigger, 0);
            random.Set(RandomStreamKeys.Ev02Target, 0);
            random.Set(RandomStreamKeys.Ev02Value, 0);
            GameSession session = CreateSession(StandardHandEntries(), random);

            session.TryConfirmBet(Money.FromCoins(5));

            Assert.That(session.Phase, Is.EqualTo(GamePhase.PlayerTurn));
            Assert.That(session.Lucidity, Is.EqualTo(75));
            Assert.That(session.ReadModel.PlayerCards[0].HasMechanicalOverride, Is.True);
            Assert.That(session.ReadModel.PlayerCards[0].DisplayedValue, Is.EqualTo(1));
        }

        [Test]
        public void DistractedSuspendsBeforeShowingP3AndBuildsSanitizedReadModel()
        {
            ScriptedRandomStreamFactory random = new ScriptedRandomStreamFactory();
            random.Set(RandomStreamKeys.Ev03Trigger, 0);
            GameSession session = CreateSession(StandardHandEntries(), random);

            session.TryConfirmBet(Money.FromCoins(5));

            Assert.That(session.Phase, Is.EqualTo(GamePhase.EventDecision));
            Assert.That(session.DealerCardCount, Is.EqualTo(2));
            Assert.That(session.PendingContentDecision.TechnicalCode, Is.EqualTo("EV-03"));

            session.TryResolveContentDecision(ContentDecisionOption.Accept);

            Assert.That(session.Phase, Is.EqualTo(GamePhase.PlayerTurn));
            Assert.That(session.DistractedActive, Is.True);
            Assert.That(session.Lucidity, Is.EqualTo(70));
            Assert.That(session.ReadModel.PlayerCards[2].IsHidden, Is.True);
            Assert.That(session.ReadModel.PlayerCards[2].CardId, Is.Null);
            Assert.That(session.ReadModel.VisiblePlayerTotal, Is.Null);
        }

        [Test]
        public void VoicesUsesIndependentTruthAndSignalStreamsThenReducesPressure()
        {
            ScriptedRandomStreamFactory random = new ScriptedRandomStreamFactory();
            random.Set(RandomStreamKeys.Ev01Trigger, 0);
            random.Set(RandomStreamKeys.Ev01Truth, 0);
            random.Set(RandomStreamKeys.Ev01Signal, 9999);
            GameSession session = CreateSession(
                Entries(
                    DeckEntry.Special(SpecialCardId.WeHaveADeal),
                    StandardHandEntries()),
                random);

            session.TryConfirmBet(Money.FromCoins(5));
            session.TryResolveContentDecision(ContentDecisionOption.Accept);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.EventDecision));

            session.TryResolveContentDecision(ContentDecisionOption.Accept);

            Assert.That(session.Phase, Is.EqualTo(GamePhase.PlayerTurn));
            Assert.That(session.Pressure, Is.EqualTo(35));
            Assert.That(session.CurrentVoicesMessage.HasValue, Is.True);
            Assert.That(
                session.CurrentVoicesMessage.Value.DealerHasTwentyThreeMessage,
                Is.False);
            Assert.That(session.CurrentVoicesMessage.Value.SuspicionSignal, Is.False);
        }

        [Test]
        public void MagnifyingGlassPreviewRevealsTheRealHoleCardOnlyOnConfirmation()
        {
            GameSession session = CreateSession(StandardHandEntries());
            session.TryPurchaseItem(ItemId.IT01, InventoryLocation.Table);
            session.TryConfirmBet(Money.FromCoins(5));

            Assert.That(session.DealerHoleCardRevealed, Is.False);
            session.TryUseItem(ItemId.IT01);
            Assert.That(session.DealerHoleCardRevealed, Is.False);

            session.TryResolveContentDecision(ContentDecisionOption.Confirm);

            Assert.That(session.DealerHoleCardRevealed, Is.True);
            Assert.That(session.ReadModel.DealerCards[2].IsHidden, Is.False);
            Assert.That(session.DealerRelationship.Distrust, Is.EqualTo(10));
            Assert.That(session.TableItems.Any(item => item.Id == ItemId.IT01), Is.False);
        }

        [Test]
        public void VoluntaryHitAfterAPreviousLossUsesFifteenTotalPressure()
        {
            GameSession session = CreateSession(StandardHandEntries()
                .Concat(new[]
                {
                    Entry(CardSuit.Clubs, CardRank.Five),
                    Entry(CardSuit.Hearts, CardRank.Ten),
                    Entry(CardSuit.Clubs, CardRank.Six),
                    Entry(CardSuit.Hearts, CardRank.Eight),
                    Entry(CardSuit.Diamonds, CardRank.Seven),
                    Entry(CardSuit.Hearts, CardRank.Two)
                })
                .Concat(new[] { Entry(CardSuit.Diamonds, CardRank.Three) })
                .ToArray());

            session.TryConfirmBet(Money.FromCoins(5));
            session.TryStand();
            Assert.That(session.LastResolution.Value.Outcome, Is.EqualTo(HandOutcome.Loss));

            session.TryContinue();
            session.TryConfirmBet(Money.FromCoins(5));
            session.TryHit();

            Assert.That(session.Pressure, Is.EqualTo(15));
        }

        [Test]
        public void AutomaticProtectionsUseLuckyCoinBeforeLastBreath()
        {
            ScriptedRandomStreamFactory random = new ScriptedRandomStreamFactory();
            random.Set(new RandomStreamKey("IT-05/1/0"), 0);
            GameSession luckyCoin = CreateSession(TwentyFourEntries(), random);
            luckyCoin.TryPurchaseItem(ItemId.IT05, InventoryLocation.Table);

            luckyCoin.TryConfirmBet(Money.FromCoins(5));

            Assert.That(luckyCoin.LastResolution.Value.Outcome, Is.EqualTo(HandOutcome.NormalWin));
            Assert.That(luckyCoin.PlayerCards, Has.Count.EqualTo(3));
            Assert.That(luckyCoin.TableItems.Any(item => item.Id == ItemId.IT05), Is.False);

            GameSession lastBreath = CreateSession(TwentyFourEntries());
            lastBreath.TryPurchaseItem(ItemId.IT02, InventoryLocation.Table);

            lastBreath.TryConfirmBet(Money.FromCoins(5));

            Assert.That(lastBreath.LastResolution.Value.Outcome, Is.EqualTo(HandOutcome.ProtectedDraw));
            Assert.That(lastBreath.LastBreathPriceDoubled, Is.True);
            Assert.That(lastBreath.TableItems.Any(item => item.Id == ItemId.IT02), Is.False);
        }

        [Test]
        public void CigaretteBoxAndProtectedFundsRespectPreparationRules()
        {
            GameSession session = CreateSession(StandardHandEntries());

            Assert.That(session.TryDepositProtected(Money.FromCoins(20)).Succeeded, Is.True);
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(30)));
            Assert.That(session.ProtectedMoney, Is.EqualTo(Money.FromCoins(20)));
            Assert.That(
                session.TryDepositProtected(Money.FromCoins(1)).Failure,
                Is.EqualTo(GameCommandFailure.ProtectedFundsTransferRejected));
            Assert.That(session.TryWithdrawProtected(Money.FromCoins(5)).Succeeded, Is.True);

            session.TryConfirmAllIn();
            Assert.That(session.Pressure, Is.EqualTo(15));
            Assert.That(session.TryUseItem(ItemId.IT04).Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.ItemDecision));
            Assert.That(
                session.TryResolveContentDecision(ContentDecisionOption.Confirm).Succeeded,
                Is.True);
            Assert.That(session.Pressure, Is.Zero);
            Assert.That(session.Lucidity, Is.EqualTo(70));
            Assert.That(session.BlurredVisionActive, Is.True);
            Assert.That(
                session.TableItems.Single(item => item.Id == ItemId.IT04).ChargesRemaining,
                Is.EqualTo(2));
        }

        [Test]
        public void CancellingManualItemPreviewDoesNotConsumeOrApplyIt()
        {
            GameSession session = CreateSession(StandardHandEntries());
            session.TryConfirmAllIn();

            Assert.That(session.TryUseItem(ItemId.IT04).Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.ItemDecision));
            Assert.That(
                session.PendingContentDecision.SourceKind,
                Is.EqualTo(ContentSourceKind.Item));

            Assert.That(
                session.TryResolveContentDecision(ContentDecisionOption.Cancel).Succeeded,
                Is.True);

            Assert.That(session.Phase, Is.EqualTo(GamePhase.PlayerTurn));
            Assert.That(session.Pressure, Is.EqualTo(15));
            Assert.That(session.Lucidity, Is.EqualTo(100));
            Assert.That(session.BlurredVisionActive, Is.False);
            Assert.That(
                session.TableItems.Single(item => item.Id == ItemId.IT04).ChargesRemaining,
                Is.EqualTo(3));
        }

        [Test]
        public void InternalSnapshotCapturesPendingFlowSecretsAndPersistentState()
        {
            GameSession session = CreateSession(Entries(
                DeckEntry.Special(SpecialCardId.WeHaveADeal),
                StandardHandEntries()));

            session.TryConfirmBet(Money.FromCoins(10));
            var snapshot = session.CreateSnapshot();

            Assert.That(snapshot.Phase, Is.EqualTo(GamePhase.SpecialCardDecision));
            Assert.That(snapshot.PendingFlow.SpecialCardId, Is.EqualTo(SpecialCardId.WeHaveADeal));
            Assert.That(snapshot.PendingFlow.NumericDrawPending, Is.True);
            Assert.That(snapshot.PendingFlow.DrawRecipient, Is.EqualTo(CardRecipient.Player));
            Assert.That(snapshot.Deck.InitialOrder.Count, Is.EqualTo(7));
            Assert.That(snapshot.Deck.DrawHistory[0].SpecialCardId, Is.EqualTo(SpecialCardId.WeHaveADeal));
            Assert.That(snapshot.Psychology.Pressure, Is.Zero);
            Assert.That(snapshot.Items.Single().ChargesRemaining, Is.EqualTo(3));
            Assert.That(snapshot.RandomStreams.Count, Is.EqualTo(9));
            Assert.That(
                snapshot.InternalHistory.Any(entry =>
                    entry.TechnicalCode == "SC-05" &&
                    entry.Kind == TwentyThree.Application.Gameplay.Diagnostics.InternalHistoryKind.ContentEncountered),
                Is.True);
        }

        [Test]
        public void PublicSessionInterfaceDoesNotExposeSecretReplayState()
        {
            Assert.That(typeof(IGameSession).GetProperty("PlayerCards"), Is.Null);
            Assert.That(typeof(IGameSession).GetProperty("CompletedRoundHistory"), Is.Null);
            Assert.That(typeof(IGameSession).GetProperty("RunSeed"), Is.Null);
            Assert.That(typeof(IGameSession).GetProperty("ActiveDistortions"), Is.Null);
            Assert.That(typeof(IGameSession).GetEvent("CardDrawn"), Is.Null);
            Assert.That(typeof(IGameSession).GetEvent("GameEventEncountered"), Is.Null);
            Assert.That(typeof(IGameSession).GetEvent("CardPerceptionEvaluated"), Is.Null);
            Assert.That(typeof(IGameSession).GetEvent("RunCompleted"), Is.Null);
            Assert.That(typeof(IGameSession).GetProperty("ReadModel"), Is.Not.Null);
        }

        [Test]
        public void TimedPerceptionDistortionExpiresOnlyThroughItsExplicitCommand()
        {
            ScriptedRandomStreamFactory random = new ScriptedRandomStreamFactory();
            random.Set(RandomStreamKeys.CardPerception, 0, 0, 9999, 9999, 9999, 9999);
            GameSession session = CreateSession(
                Entries(
                    DeckEntry.Special(SpecialCardId.WeHaveADeal),
                    StandardHandEntries()),
                random);

            session.TryConfirmBet(Money.FromCoins(5));
            session.TryResolveContentDecision(ContentDecisionOption.Accept);

            CardReadModel distorted = session.ReadModel.PlayerCards.First(card => card.IsDistorted);
            Assert.That(
                session.TryExpireTimedCardDistortion(distorted.CardId.Value).Succeeded,
                Is.True);
            Assert.That(
                session.ReadModel.PlayerCards.Single(card => card.CardId == distorted.CardId).IsDistorted,
                Is.False);
            Assert.That(
                session.TryExpireTimedCardDistortion(distorted.CardId.Value).Failure,
                Is.EqualTo(GameCommandFailure.DistortionUnavailable));
        }

        [Test]
        public void SecondChanceCreatesTheSingleExtraordinaryFifthRound()
        {
            GameRules rules = CreateRules(Money.FromCoins(500));
            GameSession session = CreateSession(StandardHandEntries(), null, rules);
            Assert.That(
                session.TryPurchaseItem(ItemId.IT03, InventoryLocation.Table).Succeeded,
                Is.True);

            for (int round = 1; round <= 4; round++)
            {
                session.TryConfirmBet(Money.FromCoins(5));
                session.TryStand();
                Assert.That(session.TryAbandonRound().Succeeded, Is.True);
            }

            Assert.That(
                session.RunStatus,
                Is.EqualTo(TwentyThree.Domain.Progression.RunStatus.Active));
            Assert.That(session.RuleRoundIndex, Is.EqualTo(4));
            Assert.That(session.RoundOrdinal, Is.EqualTo(5));
            Assert.That(session.IsExtraordinaryRound, Is.True);
            Assert.That(session.SecondChanceUsed, Is.True);
            Assert.That(session.Lucidity, Is.EqualTo(40));
            Assert.That(session.DealerRelationship.Dependence, Is.EqualTo(20));
            Assert.That(session.TableItems.Any(item => item.Id == ItemId.IT03), Is.False);

            session.TryConfirmBet(Money.FromCoins(5));
            session.TryStand();
            session.TryAbandonRound();

            Assert.That(session.RunStatus, Is.EqualTo(
                TwentyThree.Domain.Progression.RunStatus.DebtDeadlineMissed));
            Assert.That(session.FinalResult.PhaseThree.SecondChanceUsed, Is.True);
            Assert.That(session.FinalResult.PhaseThree.RuleRoundIndex, Is.EqualTo(4));
            Assert.That(session.FinalResult.PhaseThree.RoundOrdinal, Is.EqualTo(5));
            Assert.That(session.FinalResult.PhaseThree.IsExtraordinaryRound, Is.True);
            Assert.That(
                session.FinalResult.PhaseThree.Psychology.DealerRelationship.Dependence,
                Is.EqualTo(20));
        }

        private static GameSession CreateSession(
            IReadOnlyList<DeckEntry> entries,
            ScriptedRandomStreamFactory random = null,
            GameRules rules = null,
            IDeckEntryShuffler shuffler = null)
        {
            GameRules resolvedRules = rules ?? CreateRules(Money.FromCoins(50));
            return new GameSession(
                resolvedRules,
                17,
                new FixedMixedDeckFactory(entries, shuffler),
                new TwentyThreeHandEvaluator(),
                new ThresholdDealerStrategy(resolvedRules.DealerStandThreshold),
                new BetPlacementService(),
                new PayoutCalculator(),
                new HandOutcomeResolver(),
                random ?? new ScriptedRandomStreamFactory());
        }

        private static GameRules CreateRules(Money initialMoney)
        {
            return new GameRules(
                initialMoney,
                new[]
                {
                    Money.FromCoins(200),
                    Money.FromCoins(400),
                    Money.FromCoins(800),
                    Money.FromCoins(1600)
                },
                Money.FromCoins(5),
                new[]
                {
                    Money.FromCoins(20),
                    Money.FromCoins(25),
                    Money.FromCoins(30),
                    Money.FromCoins(35)
                },
                new BasisPoints(1500),
                new PayoutRules(
                    new BasisPoints(9200),
                    new BasisPoints(11000),
                    new BasisPoints(12000),
                    new BasisPoints(15000)),
                5,
                4,
                17,
                6);
        }

        private static IReadOnlyList<DeckEntry> StandardHandEntries()
        {
            return new[]
            {
                Entry(CardSuit.Hearts, CardRank.Five),
                Entry(CardSuit.Clubs, CardRank.Ten),
                Entry(CardSuit.Diamonds, CardRank.Six),
                Entry(CardSuit.Spades, CardRank.Eight),
                Entry(CardSuit.Hearts, CardRank.Seven),
                Entry(CardSuit.Clubs, CardRank.Two)
            };
        }

        private static IReadOnlyList<DeckEntry> TwentyFourEntries()
        {
            return new[]
            {
                Entry(CardSuit.Hearts, CardRank.Ten),
                Entry(CardSuit.Clubs, CardRank.Ten),
                Entry(CardSuit.Diamonds, CardRank.Nine),
                Entry(CardSuit.Spades, CardRank.Eight),
                Entry(CardSuit.Hearts, CardRank.Five),
                Entry(CardSuit.Clubs, CardRank.Two)
            };
        }

        private static IReadOnlyList<DeckEntry> Entries(
            DeckEntry first,
            IReadOnlyList<DeckEntry> remaining)
        {
            return new[] { first }.Concat(remaining).ToArray();
        }

        private static DeckEntry Entry(CardSuit suit, CardRank rank)
        {
            return DeckEntry.Numeric(new NumericCard(suit, rank));
        }

        private sealed class FixedMixedDeckFactory : IRoundDeckFactory
        {
            private readonly IReadOnlyList<DeckEntry> _entries;
            private readonly IDeckEntryShuffler _shuffler;

            public FixedMixedDeckFactory(
                IReadOnlyList<DeckEntry> entries,
                IDeckEntryShuffler shuffler = null)
            {
                _entries = entries;
                _shuffler = shuffler ?? new IdentityEntryShuffler();
            }

            public RoundDeck Create(int seed, int minimumCardsToStartHand)
            {
                return new RoundDeck(
                    _entries,
                    seed,
                    _shuffler,
                    minimumCardsToStartHand);
            }
        }

        private sealed class IdentityEntryShuffler : IDeckEntryShuffler
        {
            public IReadOnlyList<DeckEntry> ShuffleEntries(
                IReadOnlyList<DeckEntry> entries,
                int seed,
                int streamIndex)
            {
                return entries.ToArray();
            }
        }

        private sealed class SecondCallReverseEntryShuffler : IDeckEntryShuffler
        {
            private int _callCount;

            public IReadOnlyList<DeckEntry> ShuffleEntries(
                IReadOnlyList<DeckEntry> entries,
                int seed,
                int streamIndex)
            {
                _callCount++;
                return _callCount == 1
                    ? entries.ToArray()
                    : entries.Reverse().ToArray();
            }
        }

        private sealed class ScriptedRandomStreamFactory : IRandomStreamFactory
        {
            private readonly Dictionary<RandomStreamKey, Queue<int>> _values =
                new Dictionary<RandomStreamKey, Queue<int>>();

            public void Set(RandomStreamKey key, params int[] values)
            {
                _values[key] = new Queue<int>(values);
            }

            public IRandomStream Create(int seed, RandomStreamKey key)
            {
                if (!_values.TryGetValue(key, out Queue<int> values))
                {
                    values = new Queue<int>();
                    _values.Add(key, values);
                }

                return new ScriptedRandomStream(seed, key, values);
            }

            public IRandomStream Restore(RandomStreamState state)
            {
                return Create(state.Seed, state.Key);
            }
        }

        private sealed class ScriptedRandomStream : IRandomStream
        {
            private readonly Queue<int> _values;

            public ScriptedRandomStream(
                int seed,
                RandomStreamKey key,
                Queue<int> values)
            {
                Seed = seed;
                Key = key;
                _values = values;
            }

            public int Seed { get; }

            public RandomStreamKey Key { get; }

            public long Position { get; private set; }

            public int NextInt(int maximumExclusive)
            {
                int value = _values.Count == 0
                    ? maximumExclusive - 1
                    : _values.Dequeue();
                Position++;
                return value % maximumExclusive;
            }

            public RandomStreamState CaptureState()
            {
                return new RandomStreamState(Seed, Key, Position);
            }
        }
    }
}
