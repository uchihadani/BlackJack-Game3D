namespace TwentyThree.Application.Gameplay
{
    public enum GamePhase
    {
        Betting = 0,
        InitialDeal = 1,
        PlayerTurn = 2,
        DrawingPlayerCard = 3,
        DealerTurn = 4,
        Resolution = 5,
        PostHand = 6,
        RoundSettlement = 7,
        DemoCompleted = 8,
        DebtDeadlineMissed = 9,
        FundingRequired = 10,
        SpecialCardDecision = 11,
        SpecialCardChoice = 12,
        EventDecision = 13,
        PostCardEvaluation = 14,
        BeforeRunFailureCommitted = 15,
        ItemDecision = 16
    }
}
