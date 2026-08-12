namespace TwentyThree.Application.Gameplay
{
    public enum GameCommandFailure
    {
        None = 0,
        InvalidPhase = 1,
        BetRejected = 2,
        PaymentRejected = 3,
        DeckUnavailable = 4,
        ActionInProgress = 5,
        RunFinished = 6,
        InsufficientFundsForMinimumBet = 7
    }
}
