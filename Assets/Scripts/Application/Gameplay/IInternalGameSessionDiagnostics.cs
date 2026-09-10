using System;
using System.Collections.Generic;
using TwentyThree.Application.Gameplay.Snapshots;
using TwentyThree.Application.Gameplay.Diagnostics;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Application.Gameplay
{
    public interface IInternalGameSessionDiagnostics
    {
        int RunSeed { get; }

        int CurrentRoundSeed { get; }

        IReadOnlyList<NumericCard> PlayerCards { get; }

        IReadOnlyList<NumericCard> DealerVisibleCards { get; }

        int DealerCardCount { get; }

        IReadOnlyList<int> RoundSeeds { get; }

        IReadOnlyList<RoundHistoryEntry> CompletedRoundHistory { get; }

        int CurrentRoundDrawCount { get; }

        HandResolution? LastResolution { get; }

        RunResultSnapshot FinalResult { get; }

        IReadOnlyList<InternalRunHistoryEntry> InternalHistory { get; }

        IReadOnlyCollection<CardPerceptionRecord> ActiveDistortions { get; }

        event Action<CardDrawnEvent> CardDrawn;

        event Action<NumericCard> DealerHoleCardRevealedEvent;

        event Action<HandResolution> HandResolved;

        event Action<SpecialCardEncounteredEvent> SpecialCardEncountered;

        event Action<GameEventEncounteredEvent> GameEventEncountered;

        event Action<CardPerceptionRecord> CardPerceptionEvaluated;

        GameSessionSnapshot CreateSnapshot();
    }
}
