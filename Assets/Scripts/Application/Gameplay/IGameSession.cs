using System;
using System.Collections.Generic;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Progression;
using TwentyThree.Domain.Rules;

namespace TwentyThree.Application.Gameplay
{
    public interface IGameSession
    {
        int RunSeed { get; }

        int CurrentRoundSeed { get; }

        GameRules Rules { get; }

        GamePhase Phase { get; }

        RunStatus RunStatus { get; }

        int CurrentCycleNumber { get; }

        int CurrentRoundNumber { get; }

        int CurrentHandNumber { get; }

        Money AvailableMoney { get; }

        Money ProtectedMoney { get; }

        Money RemainingDebt { get; }

        Money CurrentMaximumBet { get; }

        bool CanAffordMinimumBet { get; }

        bool HasLockedBet { get; }

        Money LockedBetAmount { get; }

        Money LastBetAmount { get; }

        bool DealerHoleCardRevealed { get; }

        IReadOnlyList<NumericCard> PlayerCards { get; }

        IReadOnlyList<NumericCard> DealerVisibleCards { get; }

        int DealerCardCount { get; }

        IReadOnlyList<int> RoundSeeds { get; }

        IReadOnlyList<RoundHistoryEntry> CompletedRoundHistory { get; }

        int CurrentRoundDrawCount { get; }

        HandResolution? LastResolution { get; }

        RunResultSnapshot FinalResult { get; }

        IReadOnlyList<Exception> ObserverFailures { get; }

        event Action<GamePhase> PhaseChanged;

        event Action<CardDrawnEvent> CardDrawn;

        event Action<NumericCard> DealerHoleCardRevealedEvent;

        event Action<HandResolution> HandResolved;

        event Action<RunResultSnapshot> RunCompleted;

        GameCommandResult TryConfirmBet(Money amount);

        GameCommandResult TryConfirmAllIn();

        GameCommandResult TryCancelBet();

        GameCommandResult TryHit();

        GameCommandResult TryStand();

        GameCommandResult TryPayDebt(Money amount, bool useProtectedFunds = false);

        GameCommandResult TryContinue();

        GameCommandResult TryCloseRound();

        GameCommandResult TryAbandonRound();
    }
}
