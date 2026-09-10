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
        InsufficientFundsForMinimumBet = 7,
        ContentDecisionUnavailable = 8,
        InvalidContentChoice = 9,
        ContentRejectionUnavailable = 10,
        PreparationUnavailable = 11,
        ItemUnavailable = 12,
        ItemBlocked = 13,
        InventoryRejected = 14,
        ProtectedFundsTransferRejected = 15,
        DistortionUnavailable = 16
    }
}
