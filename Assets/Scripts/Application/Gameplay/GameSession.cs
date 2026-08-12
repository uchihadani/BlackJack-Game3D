using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Dealer;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Progression;
using TwentyThree.Domain.Rules;

namespace TwentyThree.Application.Gameplay
{
    public sealed class GameSession : IGameSession
    {
        private readonly IRoundDeckFactory _roundDeckFactory;
        private readonly IHandEvaluator _handEvaluator;
        private readonly IDealerStrategy _dealerStrategy;
        private readonly BetPlacementService _betPlacement;
        private readonly PayoutCalculator _payoutCalculator;
        private readonly HandOutcomeResolver _outcomeResolver;
        private readonly Wallet _wallet;
        private readonly RunProgression _progression;
        private readonly Hand _playerHand;
        private readonly Hand _dealerHand;
        private readonly List<NumericCard> _visibleDealerCards;
        private readonly ReadOnlyCollection<NumericCard> _readOnlyVisibleDealerCards;
        private readonly List<int> _roundSeeds;
        private readonly ReadOnlyCollection<int> _readOnlyRoundSeeds;
        private readonly List<RoundHistoryEntry> _roundHistory;
        private readonly ReadOnlyCollection<RoundHistoryEntry> _readOnlyRoundHistory;
        private readonly ObserverDispatcher _observerDispatcher;
        private RoundDeck _deck;
        private LockedBet _lockedBet;
        private int _roundSeedState;
        private bool _actionInProgress;

        public GameSession(
            GameRules rules,
            int runSeed,
            IRoundDeckFactory roundDeckFactory,
            IHandEvaluator handEvaluator,
            IDealerStrategy dealerStrategy,
            BetPlacementService betPlacement,
            PayoutCalculator payoutCalculator,
            HandOutcomeResolver outcomeResolver)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            RunSeed = runSeed;
            _roundDeckFactory = roundDeckFactory ?? throw new ArgumentNullException(nameof(roundDeckFactory));
            _handEvaluator = handEvaluator ?? throw new ArgumentNullException(nameof(handEvaluator));
            _dealerStrategy = dealerStrategy ?? throw new ArgumentNullException(nameof(dealerStrategy));
            _betPlacement = betPlacement ?? throw new ArgumentNullException(nameof(betPlacement));
            _payoutCalculator = payoutCalculator ?? throw new ArgumentNullException(nameof(payoutCalculator));
            _outcomeResolver = outcomeResolver ?? throw new ArgumentNullException(nameof(outcomeResolver));
            _wallet = new Wallet(rules.InitialMoney);
            _progression = new RunProgression(rules);
            _playerHand = new Hand();
            _dealerHand = new Hand();
            _visibleDealerCards = new List<NumericCard>();
            _readOnlyVisibleDealerCards = _visibleDealerCards.AsReadOnly();
            _roundSeeds = new List<int>();
            _readOnlyRoundSeeds = _roundSeeds.AsReadOnly();
            _roundHistory = new List<RoundHistoryEntry>();
            _readOnlyRoundHistory = _roundHistory.AsReadOnly();
            _observerDispatcher = new ObserverDispatcher();
            _roundSeedState = runSeed;
            Phase = GamePhase.Betting;
            CreateRoundDeck();
        }

        public int RunSeed { get; }

        public int CurrentRoundSeed => _deck.Seed;

        public GameRules Rules { get; }

        public GamePhase Phase { get; private set; }

        public RunStatus RunStatus => _progression.Status;

        public int CurrentCycleNumber => _progression.CurrentCycleNumber;

        public int CurrentRoundNumber => _progression.CurrentRoundNumber;

        public int CurrentHandNumber => _progression.CurrentHandNumber;

        public Money AvailableMoney => _wallet.Available;

        public Money ProtectedMoney => _wallet.Protected;

        public Money RemainingDebt => _progression.RemainingDebt;

        public Money CurrentMaximumBet => _progression.CurrentMaximumBet;

        public bool CanAffordMinimumBet => _wallet.Available >= Rules.MinimumBet;

        public bool HasLockedBet => _lockedBet != null && !_lockedBet.IsSettled;

        public Money LockedBetAmount => HasLockedBet ? _lockedBet.Amount : Money.Zero;

        public Money LastBetAmount => _lockedBet?.Amount ?? Money.Zero;

        public bool DealerHoleCardRevealed { get; private set; }

        public IReadOnlyList<NumericCard> PlayerCards => _playerHand.Cards;

        public IReadOnlyList<NumericCard> DealerVisibleCards => _readOnlyVisibleDealerCards;

        public int DealerCardCount => _dealerHand.Count;

        public IReadOnlyList<int> RoundSeeds => _readOnlyRoundSeeds;

        public IReadOnlyList<RoundHistoryEntry> CompletedRoundHistory => _readOnlyRoundHistory;

        public int CurrentRoundDrawCount => _deck.DrawHistory.Count;

        public HandResolution? LastResolution { get; private set; }

        public RunResultSnapshot FinalResult { get; private set; }

        public IReadOnlyList<Exception> ObserverFailures => _observerDispatcher.Failures;

        public event Action<GamePhase> PhaseChanged;

        public event Action<CardDrawnEvent> CardDrawn;

        public event Action<NumericCard> DealerHoleCardRevealedEvent;

        public event Action<HandResolution> HandResolved;

        public event Action<RunResultSnapshot> RunCompleted;

        public GameCommandResult TryConfirmBet(Money amount)
        {
            GameCommandResult guard = BeginAction(GamePhase.Betting);
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                if (!CanAffordMinimumBet)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.InsufficientFundsForMinimumBet);
                }

                if (!EnsureDeckCanStartHand())
                {
                    return GameCommandResult.Reject(GameCommandFailure.DeckUnavailable);
                }

                BetRules betRules = new BetRules(Rules.MinimumBet, CurrentMaximumBet);
                if (!_betPlacement.TryPlace(_wallet, amount, betRules, out _lockedBet))
                {
                    return GameCommandResult.Reject(GameCommandFailure.BetRejected);
                }

                return DealInitialCards();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryConfirmAllIn()
        {
            GameCommandResult guard = BeginAction(GamePhase.Betting);
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                if (!CanAffordMinimumBet)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.InsufficientFundsForMinimumBet);
                }

                if (!EnsureDeckCanStartHand())
                {
                    return GameCommandResult.Reject(GameCommandFailure.DeckUnavailable);
                }

                BetRules betRules = new BetRules(Rules.MinimumBet, CurrentMaximumBet);
                if (!_betPlacement.TryPlaceAllIn(_wallet, betRules, out _lockedBet))
                {
                    return GameCommandResult.Reject(GameCommandFailure.BetRejected);
                }

                return DealInitialCards();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryCancelBet()
        {
            GameCommandResult guard = BeginAction(GamePhase.Betting);
            if (!guard.Succeeded)
            {
                return guard;
            }

            _actionInProgress = false;
            return GameCommandResult.Success();
        }

        public GameCommandResult TryHit()
        {
            GameCommandResult guard = BeginAction(GamePhase.PlayerTurn);
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                SetPhase(GamePhase.DrawingPlayerCard);
                if (!DrawCard(_playerHand, CardRecipient.Player, false))
                {
                    ResolveTechnicalDraw();
                    return GameCommandResult.Success();
                }

                HandScore playerScore = _handEvaluator.Evaluate(_playerHand);
                if (playerScore.IsBust)
                {
                    ResolveHand();
                }
                else if (playerScore.IsTwentyThree)
                {
                    ResolveHand();
                }
                else
                {
                    SetPhase(GamePhase.PlayerTurn);
                }

                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryStand()
        {
            GameCommandResult guard = BeginAction(GamePhase.PlayerTurn);
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                ExecuteDealerTurn();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryPayDebt(Money amount, bool useProtectedFunds = false)
        {
            GameCommandResult guard = BeginPostHandAction();
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                int previousCycle = _progression.CurrentCycleNumber;
                bool paid = useProtectedFunds
                    ? _progression.PayDebtFromProtected(_wallet, amount)
                    : _progression.PayDebt(_wallet, amount);
                if (!paid)
                {
                    return GameCommandResult.Reject(GameCommandFailure.PaymentRejected);
                }

                if (_progression.Status == RunStatus.DemoCompleted)
                {
                    CompleteRun(GamePhase.DemoCompleted);
                }
                else if (_progression.CurrentCycleNumber != previousCycle)
                {
                    ArchiveRound();
                    CreateRoundDeck();
                    ResetHands();
                    EnterBettingOrFundingRequired();
                }

                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryContinue()
        {
            GameCommandResult guard = BeginAction(GamePhase.PostHand);
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                if (!CanAffordMinimumBet)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.InsufficientFundsForMinimumBet);
                }

                if (_deck.CanStartHand)
                {
                    ResetHands();
                    SetPhase(GamePhase.Betting);
                }
                else
                {
                    _progression.RequestRoundClosure();
                    SetPhase(GamePhase.RoundSettlement);
                }

                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryCloseRound()
        {
            GameCommandResult guard = BeginAction(GamePhase.RoundSettlement);
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                _progression.CloseRound();
                CompleteRoundTransition();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryAbandonRound()
        {
            GameCommandResult guard = BeginAction(GamePhase.PostHand);
            if (!guard.Succeeded)
            {
                return guard;
            }

            try
            {
                _progression.AbandonRound();
                CompleteRoundTransition();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        private GameCommandResult DealInitialCards()
        {
            if (!_deck.TryStartHand())
            {
                throw new InvalidOperationException("The validated round deck could not start a hand.");
            }

            SetPhase(GamePhase.InitialDeal);
            for (int index = 0; index < 3; index++)
            {
                if (!DrawCard(_playerHand, CardRecipient.Player, false) ||
                    !DrawCard(_dealerHand, CardRecipient.Dealer, index == 2))
                {
                    ResolveTechnicalDraw();
                    return GameCommandResult.Success();
                }
            }

            HandScore playerScore = _handEvaluator.Evaluate(_playerHand);
            if (playerScore.IsBust)
            {
                ResolveHand();
            }
            else if (playerScore.IsTwentyThree)
            {
                ResolveHand();
            }
            else
            {
                SetPhase(GamePhase.PlayerTurn);
            }

            return GameCommandResult.Success();
        }

        private void ExecuteDealerTurn()
        {
            SetPhase(GamePhase.DealerTurn);
            if (!DealerHoleCardRevealed)
            {
                DealerHoleCardRevealed = true;
                NumericCard holeCard = _dealerHand.Cards[2];
                _visibleDealerCards.Add(holeCard);
                _observerDispatcher.Publish(DealerHoleCardRevealedEvent, holeCard);
            }

            while (true)
            {
                HandScore dealerScore = _handEvaluator.Evaluate(_dealerHand);
                DealerDecision decision = _dealerStrategy.Decide(dealerScore);
                if (decision != DealerDecision.Hit)
                {
                    break;
                }

                if (!DrawCard(_dealerHand, CardRecipient.Dealer, false))
                {
                    ResolveTechnicalDraw();
                    return;
                }
            }

            ResolveHand();
        }

        private void ResolveHand()
        {
            SetPhase(GamePhase.Resolution);
            HandResolution resolution = _outcomeResolver.Resolve(
                _handEvaluator.Evaluate(_playerHand),
                _handEvaluator.Evaluate(_dealerHand));
            CompleteHand(resolution, false);
        }

        private void ResolveTechnicalDraw()
        {
            SetPhase(GamePhase.Resolution);
            HandResolution resolution = _outcomeResolver.TechnicalDraw(
                _handEvaluator.Evaluate(_playerHand),
                _handEvaluator.Evaluate(_dealerHand));
            CompleteHand(resolution, true);
        }

        private void CompleteHand(HandResolution resolution, bool closeRound)
        {
            BetOutcome betOutcome = ConvertOutcome(resolution.Outcome);
            if (!_payoutCalculator.TrySettle(
                    _lockedBet,
                    betOutcome,
                    Rules.Payouts,
                    _wallet,
                    out _))
            {
                throw new InvalidOperationException("The locked bet was already settled.");
            }

            LastResolution = resolution;

            if (!_deck.CompleteHand())
            {
                throw new InvalidOperationException("The round deck could not complete the active hand.");
            }

            _progression.RecordCompletedHand();
            if (closeRound && _progression.Status == RunStatus.Active)
            {
                _progression.RequestRoundClosure();
            }

            SetPhase(_progression.Status == RunStatus.RoundClosurePending
                ? GamePhase.RoundSettlement
                : GamePhase.PostHand);
            _observerDispatcher.Publish(HandResolved, resolution);
        }

        private bool DrawCard(Hand hand, CardRecipient recipient, bool isFaceDown)
        {
            if (_deck.Draw(out NumericCard card) != DeckDrawStatus.Drawn)
            {
                return false;
            }

            hand.Add(card);
            if (recipient == CardRecipient.Dealer && !isFaceDown)
            {
                _visibleDealerCards.Add(card);
            }

            _observerDispatcher.Publish(
                CardDrawn,
                new CardDrawnEvent(card, recipient, isFaceDown));
            return true;
        }

        private bool EnsureDeckCanStartHand()
        {
            if (_progression.CanStartHand && _deck.CanStartHand)
            {
                return true;
            }

            if (_progression.CanRequestRoundClosure)
            {
                _progression.RequestRoundClosure();
                SetPhase(GamePhase.RoundSettlement);
            }

            return false;
        }

        private void CompleteRoundTransition()
        {
            ArchiveRound();
            if (_progression.Status == RunStatus.DebtDeadlineMissed)
            {
                CompleteRun(GamePhase.DebtDeadlineMissed);
                return;
            }

            CreateRoundDeck();
            ResetHands();
            EnterBettingOrFundingRequired();
        }

        private void CreateRoundDeck()
        {
            int seed = NextRoundSeed();
            _roundSeeds.Add(seed);
            _deck = _roundDeckFactory.Create(seed, Rules.MinimumCardsToStartHand);
        }

        private int NextRoundSeed()
        {
            if (_roundSeeds.Count == 0)
            {
                return RunSeed;
            }

            _roundSeedState = unchecked(_roundSeedState * 1664525 + 1013904223);
            return _roundSeedState;
        }

        private void ArchiveRound()
        {
            if (_deck != null &&
                (_roundHistory.Count == 0 || _roundHistory[_roundHistory.Count - 1].Seed != _deck.Seed))
            {
                _roundHistory.Add(new RoundHistoryEntry(_deck));
            }
        }

        private void ResetHands()
        {
            _playerHand.Clear();
            _dealerHand.Clear();
            _visibleDealerCards.Clear();
            _lockedBet = null;
            DealerHoleCardRevealed = false;
            LastResolution = null;
        }

        private void EnterBettingOrFundingRequired()
        {
            SetPhase(CanAffordMinimumBet
                ? GamePhase.Betting
                : GamePhase.FundingRequired);
        }

        private void CompleteRun(GamePhase terminalPhase)
        {
            ArchiveRound();
            FinalResult = new RunResultSnapshot(
                RunSeed,
                _progression.Status,
                _wallet.Available,
                _wallet.Protected,
                _progression.RemainingDebt,
                _progression.CurrentCycleNumber,
                _progression.CurrentRoundNumber,
                _progression.CurrentHandNumber,
                _roundSeeds,
                _roundHistory);
            SetPhase(terminalPhase);
            _observerDispatcher.Publish(RunCompleted, FinalResult);
        }

        private GameCommandResult BeginAction(GamePhase requiredPhase)
        {
            if (IsTerminal())
            {
                return GameCommandResult.Reject(GameCommandFailure.RunFinished);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            if (Phase != requiredPhase)
            {
                return GameCommandResult.Reject(GameCommandFailure.InvalidPhase);
            }

            _actionInProgress = true;
            return GameCommandResult.Success();
        }

        private GameCommandResult BeginPostHandAction()
        {
            if (IsTerminal())
            {
                return GameCommandResult.Reject(GameCommandFailure.RunFinished);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            if (Phase != GamePhase.PostHand && Phase != GamePhase.RoundSettlement)
            {
                return GameCommandResult.Reject(GameCommandFailure.InvalidPhase);
            }

            _actionInProgress = true;
            return GameCommandResult.Success();
        }

        private bool IsTerminal()
        {
            return Phase == GamePhase.DemoCompleted ||
                   Phase == GamePhase.DebtDeadlineMissed;
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            _observerDispatcher.Publish(PhaseChanged, phase);
        }

        private static BetOutcome ConvertOutcome(HandOutcome outcome)
        {
            switch (outcome)
            {
                case HandOutcome.Loss:
                    return BetOutcome.Loss;
                case HandOutcome.NormalWin:
                    return BetOutcome.NormalWin;
                case HandOutcome.InitialTwentyThree:
                    return BetOutcome.InitialTwentyThree;
                case HandOutcome.Draw:
                case HandOutcome.TechnicalDraw:
                    return BetOutcome.Draw;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
            }
        }
    }
}
