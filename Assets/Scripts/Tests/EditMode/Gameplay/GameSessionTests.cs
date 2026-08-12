using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TwentyThree.Application.Gameplay;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Dealer;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Progression;
using TwentyThree.Domain.Rules;
using TwentyThree.Infrastructure.Random;

namespace TwentyThree.Tests.EditMode.Gameplay
{
    public sealed class GameSessionTests
    {
        [Test]
        public void NewSessionStartsWithConfiguredRunState()
        {
            GameSession session = CreateSession(NormalWinDeck(), 73);

            Assert.That(session.RunSeed, Is.EqualTo(73));
            Assert.That(session.CurrentRoundSeed, Is.EqualTo(73));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Betting));
            Assert.That(session.CurrentCycleNumber, Is.EqualTo(1));
            Assert.That(session.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(session.CurrentHandNumber, Is.EqualTo(1));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(session.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
            Assert.That(session.CurrentMaximumBet, Is.EqualTo(Money.FromCoins(20)));
        }

        [Test]
        public void CommandsOutsideTheirPhaseAndInvalidBetsAreAtomic()
        {
            GameSession session = CreateSession(NormalWinDeck());

            Assert.That(session.TryHit().Failure, Is.EqualTo(GameCommandFailure.InvalidPhase));
            Assert.That(session.TryConfirmBet(Money.FromCoins(4)).Failure, Is.EqualTo(GameCommandFailure.BetRejected));
            Assert.That(session.TryConfirmBet(Money.FromCoins(21)).Failure, Is.EqualTo(GameCommandFailure.BetRejected));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(session.PlayerCards, Is.Empty);
            Assert.That(session.DealerVisibleCards, Is.Empty);
            Assert.That(session.DealerCardCount, Is.Zero);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Betting));
        }

        [Test]
        public void NormalWinLocksBetRunsDealerAndPaysExactlyOnce()
        {
            GameSession session = CreateSession(NormalWinDeck());

            Assert.That(session.TryConfirmBet(Money.FromCoins(10)).Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PlayerTurn));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(40)));
            Assert.That(session.HasLockedBet, Is.True);
            Assert.That(session.LockedBetAmount, Is.EqualTo(Money.FromCoins(10)));
            Assert.That(session.PlayerCards, Has.Count.EqualTo(3));
            Assert.That(session.DealerVisibleCards, Has.Count.EqualTo(2));
            Assert.That(session.DealerCardCount, Is.EqualTo(3));
            Assert.That(session.DealerHoleCardRevealed, Is.False);

            Assert.That(session.TryStand().Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));
            Assert.That(session.DealerHoleCardRevealed, Is.True);
            Assert.That(session.DealerVisibleCards, Has.Count.EqualTo(3));
            Assert.That(session.LastResolution.Value.Outcome, Is.EqualTo(HandOutcome.NormalWin));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromMinorUnits(5920)));
            Assert.That(session.HasLockedBet, Is.False);
            Assert.That(session.LockedBetAmount, Is.EqualTo(Money.Zero));
            Assert.That(session.LastBetAmount, Is.EqualTo(Money.FromCoins(10)));

            Assert.That(session.TryStand().Failure, Is.EqualTo(GameCommandFailure.InvalidPhase));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromMinorUnits(5920)));
        }

        [Test]
        public void InitialTwentyThreeResolvesAndUsesSpecialPayout()
        {
            GameSession session = CreateSession(InitialTwentyThreeDeck());

            Assert.That(session.TryConfirmBet(Money.FromCoins(10)).Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));
            Assert.That(session.LastResolution.Value.Outcome, Is.EqualTo(HandOutcome.InitialTwentyThree));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(62)));
            Assert.That(session.DealerHoleCardRevealed, Is.False);
            Assert.That(session.CurrentRoundDrawCount, Is.EqualTo(6));
        }

        [Test]
        public void InitialTwentyThreeAllInUsesAllInPayout()
        {
            GameSession session = CreateSession(InitialTwentyThreeDeck());

            Assert.That(session.TryConfirmAllIn().Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));
            Assert.That(session.HasLockedBet, Is.False);
            Assert.That(session.LockedBetAmount, Is.EqualTo(Money.Zero));
            Assert.That(session.LastBetAmount, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(125)));
        }

        [Test]
        public void ContinueWithNoValidBetIsRejectedWithoutLeavingPostHand()
        {
            GameSession session = CreateSession(PlayerBustDeck());

            Assert.That(session.TryConfirmAllIn().Succeeded, Is.True);
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.Zero));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));

            GameCommandResult result = session.TryContinue();

            Assert.That(result.Failure, Is.EqualTo(GameCommandFailure.InsufficientFundsForMinimumBet));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.Zero));
            Assert.That(session.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(session.CurrentHandNumber, Is.EqualTo(2));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));
        }

        [Test]
        public void NewRoundWithoutMinimumFundsEntersFundingRequiredState()
        {
            GameSession session = CreateSession(PlayerBustDeck());
            session.TryConfirmAllIn();

            Assert.That(session.TryAbandonRound().Succeeded, Is.True);

            Assert.That(session.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(session.RemainingDebt, Is.EqualTo(Money.FromCoins(230)));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.Zero));
            Assert.That(session.CanAffordMinimumBet, Is.False);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.FundingRequired));
            Assert.That(session.TryConfirmAllIn().Failure, Is.EqualTo(GameCommandFailure.InvalidPhase));
        }

        [Test]
        public void PayingCycleDebtWithAllFundsEntersFundingRequiredInNextCycle()
        {
            GameRules rules = CreateRules(initialMoney: Money.FromCoins(210));
            GameSession session = CreateSession(PlayerBustDeck(), rules);
            session.TryConfirmBet(Money.FromCoins(10));

            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(200)));
            Assert.That(session.TryPayDebt(Money.FromCoins(200)).Succeeded, Is.True);

            Assert.That(session.CurrentCycleNumber, Is.EqualTo(2));
            Assert.That(session.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(session.RemainingDebt, Is.EqualTo(Money.FromCoins(400)));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.Zero));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.FundingRequired));
        }

        [Test]
        public void ReentrantHitWhileDrawingCannotDrawASecondCard()
        {
            GameSession session = CreateSession(HitDeck());
            GameCommandResult reentrantResult = default;
            int reentrantCalls = 0;

            session.CardDrawn += cardEvent =>
            {
                if (session.Phase == GamePhase.DrawingPlayerCard &&
                    cardEvent.Recipient == CardRecipient.Player)
                {
                    reentrantCalls++;
                    reentrantResult = session.TryHit();
                }
            };

            Assert.That(session.TryConfirmBet(Money.FromCoins(10)).Succeeded, Is.True);
            Assert.That(session.TryHit().Succeeded, Is.True);

            Assert.That(reentrantCalls, Is.EqualTo(1));
            Assert.That(reentrantResult.Succeeded, Is.False);
            Assert.That(reentrantResult.Failure, Is.EqualTo(GameCommandFailure.ActionInProgress));
            Assert.That(session.PlayerCards, Has.Count.EqualTo(4));
            Assert.That(session.CurrentRoundDrawCount, Is.EqualTo(7));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PlayerTurn));
        }

        [Test]
        public void FailingObserversAreRecordedWithoutBreakingTheCommittedHand()
        {
            GameSession session = CreateSession(PlayerBustDeck());
            GamePhase phaseSeenByResolutionObserver = GamePhase.Betting;
            session.CardDrawn += _ => throw new InvalidOperationException("draw observer");
            session.HandResolved += _ =>
            {
                phaseSeenByResolutionObserver = session.Phase;
                throw new InvalidOperationException("resolution observer");
            };

            GameCommandResult result = session.TryConfirmBet(Money.FromCoins(10));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));
            Assert.That(phaseSeenByResolutionObserver, Is.EqualTo(GamePhase.PostHand));
            Assert.That(session.CurrentHandNumber, Is.EqualTo(2));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(40)));
            Assert.That(session.ObserverFailures, Has.Count.EqualTo(7));
        }

        [Test]
        public void TwentyThreeAfterHitWinsImmediatelyWithNormalPayout()
        {
            GameSession session = CreateSession(FourthCardTwentyThreeDeck());

            Assert.That(session.TryConfirmBet(Money.FromCoins(10)).Succeeded, Is.True);
            Assert.That(session.TryHit().Succeeded, Is.True);

            Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));
            Assert.That(session.LastResolution.Value.Outcome, Is.EqualTo(HandOutcome.NormalWin));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromMinorUnits(5920)));
            Assert.That(session.DealerHoleCardRevealed, Is.False);
            Assert.That(session.CurrentRoundDrawCount, Is.EqualTo(7));
        }

        [Test]
        public void HiddenDealerCardIsNotExposedUntilReveal()
        {
            GameSession session = CreateSession(NormalWinDeck());
            List<CardDrawnEvent> draws = new List<CardDrawnEvent>();
            NumericCard? revealedCard = null;
            session.CardDrawn += draws.Add;
            session.DealerHoleCardRevealedEvent += card => revealedCard = card;

            session.TryConfirmBet(Money.FromCoins(10));

            CardDrawnEvent hiddenDraw = draws.Single(draw => draw.IsFaceDown);
            Assert.That(hiddenDraw.Recipient, Is.EqualTo(CardRecipient.Dealer));
            Assert.That(hiddenDraw.Card.HasValue, Is.False);
            Assert.That(draws.Where(draw => !draw.IsFaceDown).All(draw => draw.Card.HasValue), Is.True);
            Assert.That(session.DealerVisibleCards, Has.Count.EqualTo(2));
            Assert.That(revealedCard.HasValue, Is.False);

            session.TryStand();

            Assert.That(revealedCard.HasValue, Is.True);
            Assert.That(session.DealerVisibleCards, Does.Contain(revealedCard.Value));
        }

        [Test]
        public void ExhaustionWithoutDiscardProducesTechnicalDrawAndRoundSettlement()
        {
            GameSession session = CreateSession(HitDeck().Take(6).ToArray());

            Assert.That(session.TryConfirmBet(Money.FromCoins(10)).Succeeded, Is.True);
            Assert.That(session.TryHit().Succeeded, Is.True);

            Assert.That(session.LastResolution.Value.Outcome, Is.EqualTo(HandOutcome.TechnicalDraw));
            Assert.That(session.AvailableMoney, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.RoundSettlement));
            Assert.That(session.TryContinue().Failure, Is.EqualTo(GameCommandFailure.InvalidPhase));
        }

        [Test]
        public void LowDeckMovesToPaymentWindowBeforeInterestAndNextRound()
        {
            GameSession session = CreateSession(NormalWinDeck());
            session.TryConfirmBet(Money.FromCoins(10));
            session.TryStand();

            Assert.That(session.TryContinue().Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.RoundSettlement));
            Assert.That(session.TryPayDebt(Money.FromCoins(20)).Succeeded, Is.True);
            Assert.That(session.RemainingDebt, Is.EqualTo(Money.FromCoins(180)));

            Assert.That(session.TryCloseRound().Succeeded, Is.True);
            Assert.That(session.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(session.RemainingDebt, Is.EqualTo(Money.FromCoins(207)));
            Assert.That(session.CurrentMaximumBet, Is.EqualTo(Money.FromCoins(25)));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Betting));
            Assert.That(session.CompletedRoundHistory, Has.Count.EqualTo(1));
        }

        [Test]
        public void AbandoningAfterAHandConsumesRoundAndAppliesInterest()
        {
            GameSession session = CreateSession(NormalWinDeck());
            session.TryConfirmBet(Money.FromCoins(10));
            session.TryStand();

            Assert.That(session.TryAbandonRound().Succeeded, Is.True);
            Assert.That(session.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(session.RemainingDebt, Is.EqualTo(Money.FromCoins(230)));
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Betting));
        }

        [Test]
        public void FiveResolvedHandsOpenTheRoundSettlementWindow()
        {
            GameSession session = CreateSession(new StandardDeckFactory().Create());

            for (int hand = 1; hand <= 5; hand++)
            {
                Assert.That(session.Phase, Is.EqualTo(GamePhase.Betting));
                Assert.That(session.CurrentHandNumber, Is.EqualTo(hand));
                Assert.That(session.TryConfirmBet(Money.FromCoins(5)).Succeeded, Is.True);
                if (session.Phase == GamePhase.PlayerTurn)
                {
                    Assert.That(session.TryStand().Succeeded, Is.True);
                }

                if (hand < 5)
                {
                    Assert.That(session.Phase, Is.EqualTo(GamePhase.PostHand));
                    Assert.That(session.TryContinue().Succeeded, Is.True);
                }
            }

            Assert.That(session.Phase, Is.EqualTo(GamePhase.RoundSettlement));
            Assert.That(session.CurrentHandNumber, Is.EqualTo(5));
            Assert.That(session.TryCloseRound().Succeeded, Is.True);
            Assert.That(session.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(session.CompletedRoundHistory, Has.Count.EqualTo(1));
        }

        [Test]
        public void SameRunSeedProducesTheSameRecordedOrder()
        {
            GameRules rules = CreateRules();
            GameSessionFactory factory = new GameSessionFactory(rules, new DeterministicCardShuffler());
            IGameSession first = factory.Create(12345);
            IGameSession second = factory.Create(12345);

            first.TryConfirmBet(Money.FromCoins(5));
            second.TryConfirmBet(Money.FromCoins(5));

            Assert.That(first.CurrentRoundSeed, Is.EqualTo(12345));
            Assert.That(second.CurrentRoundSeed, Is.EqualTo(12345));

            ResolveAndArchiveCurrentRound(first);
            ResolveAndArchiveCurrentRound(second);

            Assert.That(first.CompletedRoundHistory.Single().Seed, Is.EqualTo(12345));
            Assert.That(second.CompletedRoundHistory.Single().Seed, Is.EqualTo(12345));
            Assert.That(
                first.CompletedRoundHistory.Single().DrawHistory.Select(card => card.Id.Value),
                Is.EqualTo(second.CompletedRoundHistory.Single().DrawHistory.Select(card => card.Id.Value)));
        }

        [Test]
        public void RunSessionControllerCreatesFreshSessionsWithFreshSeeds()
        {
            GameSessionFactory factory = new GameSessionFactory(
                CreateRules(),
                new DeterministicCardShuffler());
            IncrementingSeedProvider seeds = new IncrementingSeedProvider(40);
            RunSessionController controller = new RunSessionController(factory, seeds);
            int changes = 0;
            controller.SessionChanged += _ => changes++;

            IGameSession first = controller.StartNewRun();
            IGameSession second = controller.StartNewRun();

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.RunSeed, Is.EqualTo(41));
            Assert.That(second.RunSeed, Is.EqualTo(42));
            Assert.That(controller.Current, Is.SameAs(second));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void PayingFourthDebtCreatesImmutableFinalResultAndBlocksCommands()
        {
            GameRules rules = CreateRules(initialMoney: Money.FromCoins(4000));
            GameSession session = CreateSession(PlayerBustDeck(), rules, 125);
            RunResultSnapshot observedResult = null;
            int completionEvents = 0;
            session.RunCompleted += result =>
            {
                observedResult = result;
                completionEvents++;
            };

            for (int cycle = 1; cycle <= 4; cycle++)
            {
                Assert.That(session.CurrentCycleNumber, Is.EqualTo(cycle));
                Assert.That(session.TryConfirmBet(Money.FromCoins(5)).Succeeded, Is.True);
                Assert.That(session.TryPayDebt(session.RemainingDebt).Succeeded, Is.True);
            }

            Assert.That(session.Phase, Is.EqualTo(GamePhase.DemoCompleted));
            Assert.That(session.RunStatus, Is.EqualTo(RunStatus.DemoCompleted));
            Assert.That(session.RemainingDebt, Is.EqualTo(Money.Zero));
            Assert.That(session.FinalResult, Is.SameAs(observedResult));
            Assert.That(session.FinalResult.Status, Is.EqualTo(RunStatus.DemoCompleted));
            Assert.That(session.FinalResult.RunSeed, Is.EqualTo(125));
            Assert.That(session.FinalResult.RoundSeeds, Has.Count.EqualTo(4));
            Assert.That(session.FinalResult.RoundHistory, Has.Count.EqualTo(4));
            Assert.That(session.FinalResult.RoundHistory.All(entry => entry.DrawHistory.Count == 6), Is.True);
            Assert.That(completionEvents, Is.EqualTo(1));
            Assert.That(
                () => ((IList<int>)session.FinalResult.RoundSeeds)[0] = 999,
                Throws.TypeOf<NotSupportedException>());
            Assert.That(session.TryConfirmBet(Money.FromCoins(5)).Failure, Is.EqualTo(GameCommandFailure.RunFinished));
            Assert.That(session.TryPayDebt(Money.FromCoins(1)).Failure, Is.EqualTo(GameCommandFailure.RunFinished));
        }

        [Test]
        public void FourthAbandonedRoundCreatesDebtDeadlineFinalResult()
        {
            GameSession session = CreateSession(PlayerBustDeck(), 321);

            for (int round = 1; round <= 4; round++)
            {
                Assert.That(session.CurrentRoundNumber, Is.EqualTo(round));
                Assert.That(session.TryConfirmBet(Money.FromCoins(5)).Succeeded, Is.True);
                Assert.That(session.TryAbandonRound().Succeeded, Is.True);
            }

            Assert.That(session.Phase, Is.EqualTo(GamePhase.DebtDeadlineMissed));
            Assert.That(session.RunStatus, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(session.FinalResult, Is.Not.Null);
            Assert.That(session.FinalResult.Status, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(session.FinalResult.RoundHistory, Has.Count.EqualTo(4));
            Assert.That(session.TryContinue().Failure, Is.EqualTo(GameCommandFailure.RunFinished));
            Assert.That(session.TryAbandonRound().Failure, Is.EqualTo(GameCommandFailure.RunFinished));
        }

        [Test]
        public void RunSessionControllerPreservesCompletedResultWhenStartingAnotherRun()
        {
            GameRules rules = CreateRules(initialMoney: Money.FromCoins(4000));
            FixedGameSessionFactory factory = new FixedGameSessionFactory(rules, PlayerBustDeck());
            RunSessionController controller = new RunSessionController(
                factory,
                new IncrementingSeedProvider(800));
            int completionEvents = 0;
            controller.RunCompleted += _ => completionEvents++;
            IGameSession completedSession = controller.StartNewRun();

            for (int cycle = 1; cycle <= 4; cycle++)
            {
                completedSession.TryConfirmBet(Money.FromCoins(5));
                completedSession.TryPayDebt(completedSession.RemainingDebt);
            }

            IGameSession nextSession = controller.StartNewRun();

            Assert.That(controller.CompletedRuns, Has.Count.EqualTo(1));
            Assert.That(controller.CompletedRuns[0], Is.SameAs(completedSession.FinalResult));
            Assert.That(controller.CompletedRuns[0].Status, Is.EqualTo(RunStatus.DemoCompleted));
            Assert.That(completionEvents, Is.EqualTo(1));
            Assert.That(nextSession, Is.Not.SameAs(completedSession));
            Assert.That(controller.Current, Is.SameAs(nextSession));
        }

        private static GameSession CreateSession(
            IReadOnlyList<NumericCard> cards,
            int seed = 11)
        {
            return CreateSession(cards, CreateRules(), seed);
        }

        private static GameSession CreateSession(
            IReadOnlyList<NumericCard> cards,
            GameRules rules,
            int seed = 11)
        {
            return new GameSession(
                rules,
                seed,
                new FixedRoundDeckFactory(cards),
                new TwentyThreeHandEvaluator(),
                new ThresholdDealerStrategy(rules.DealerStandThreshold),
                new BetPlacementService(),
                new PayoutCalculator(),
                new HandOutcomeResolver());
        }

        private static GameRules CreateRules(Money? initialMoney = null)
        {
            return new GameRules(
                initialMoney ?? Money.FromCoins(50),
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

        private static NumericCard[] NormalWinDeck()
        {
            return new[]
            {
                Card(CardSuit.Hearts, CardRank.Ten),
                Card(CardSuit.Clubs, CardRank.Nine),
                Card(CardSuit.Hearts, CardRank.Five),
                Card(CardSuit.Clubs, CardRank.Seven),
                Card(CardSuit.Hearts, CardRank.Four),
                Card(CardSuit.Clubs, CardRank.Ace)
            };
        }

        private static NumericCard[] InitialTwentyThreeDeck()
        {
            return new[]
            {
                Card(CardSuit.Hearts, CardRank.Ten),
                Card(CardSuit.Clubs, CardRank.Ten),
                Card(CardSuit.Hearts, CardRank.Eight),
                Card(CardSuit.Clubs, CardRank.Eight),
                Card(CardSuit.Hearts, CardRank.Five),
                Card(CardSuit.Clubs, CardRank.Five)
            };
        }

        private static NumericCard[] FourthCardTwentyThreeDeck()
        {
            return new[]
            {
                Card(CardSuit.Hearts, CardRank.Ten),
                Card(CardSuit.Clubs, CardRank.Ten),
                Card(CardSuit.Hearts, CardRank.Four),
                Card(CardSuit.Clubs, CardRank.Eight),
                Card(CardSuit.Hearts, CardRank.Three),
                Card(CardSuit.Clubs, CardRank.Five),
                Card(CardSuit.Hearts, CardRank.Six)
            };
        }

        private static NumericCard[] HitDeck()
        {
            return new[]
            {
                Card(CardSuit.Hearts, CardRank.Ten),
                Card(CardSuit.Clubs, CardRank.Ten),
                Card(CardSuit.Hearts, CardRank.Four),
                Card(CardSuit.Clubs, CardRank.Seven),
                Card(CardSuit.Hearts, CardRank.Three),
                Card(CardSuit.Clubs, CardRank.Two),
                Card(CardSuit.Hearts, CardRank.Two)
            };
        }

        private static NumericCard[] PlayerBustDeck()
        {
            return new[]
            {
                Card(CardSuit.Hearts, CardRank.Ten),
                Card(CardSuit.Clubs, CardRank.Ten),
                Card(CardSuit.Hearts, CardRank.Nine),
                Card(CardSuit.Clubs, CardRank.Seven),
                Card(CardSuit.Hearts, CardRank.Five),
                Card(CardSuit.Clubs, CardRank.Two)
            };
        }

        private static NumericCard Card(CardSuit suit, CardRank rank)
        {
            return new NumericCard(suit, rank);
        }

        private static void ResolveAndArchiveCurrentRound(IGameSession session)
        {
            if (session.Phase == GamePhase.PlayerTurn)
            {
                session.TryStand();
            }

            if (session.Phase == GamePhase.PostHand)
            {
                session.TryAbandonRound();
            }
        }

        private sealed class FixedRoundDeckFactory : IRoundDeckFactory
        {
            private readonly IReadOnlyList<NumericCard> _cards;

            public FixedRoundDeckFactory(IReadOnlyList<NumericCard> cards)
            {
                _cards = cards;
            }

            public RoundDeck Create(int seed, int minimumCardsToStartHand)
            {
                return new RoundDeck(
                    _cards,
                    seed,
                    new IdentityShuffler(),
                    minimumCardsToStartHand);
            }
        }

        private sealed class IdentityShuffler : ICardShuffler
        {
            public IReadOnlyList<NumericCard> Shuffle(
                IReadOnlyList<NumericCard> cards,
                int seed,
                int streamIndex)
            {
                return cards.ToArray();
            }
        }

        private sealed class IncrementingSeedProvider : IRunSeedProvider
        {
            private int _seed;

            public IncrementingSeedProvider(int seed)
            {
                _seed = seed;
            }

            public int NextSeed()
            {
                return ++_seed;
            }
        }

        private sealed class FixedGameSessionFactory : IGameSessionFactory
        {
            private readonly GameRules _rules;
            private readonly IReadOnlyList<NumericCard> _cards;

            public FixedGameSessionFactory(
                GameRules rules,
                IReadOnlyList<NumericCard> cards)
            {
                _rules = rules;
                _cards = cards;
            }

            public IGameSession Create(int runSeed)
            {
                return CreateSession(_cards, _rules, runSeed);
            }
        }
    }
}
