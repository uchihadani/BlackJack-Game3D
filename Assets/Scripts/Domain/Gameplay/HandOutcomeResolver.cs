using TwentyThree.Domain.Cards;

namespace TwentyThree.Domain.Gameplay
{
    public sealed class HandOutcomeResolver
    {
        public HandResolution Resolve(HandScore playerScore, HandScore dealerScore)
        {
            if (playerScore.IsBust)
            {
                return new HandResolution(HandOutcome.Loss, playerScore, dealerScore);
            }

            if (playerScore.IsTwentyThree)
            {
                return new HandResolution(
                    GetWinningOutcome(playerScore),
                    playerScore,
                    dealerScore);
            }

            if (dealerScore.IsBust)
            {
                return new HandResolution(
                    GetWinningOutcome(playerScore),
                    playerScore,
                    dealerScore);
            }

            if (playerScore.Total == dealerScore.Total)
            {
                return new HandResolution(HandOutcome.Draw, playerScore, dealerScore);
            }

            return playerScore.Total > dealerScore.Total
                ? new HandResolution(GetWinningOutcome(playerScore), playerScore, dealerScore)
                : new HandResolution(HandOutcome.Loss, playerScore, dealerScore);
        }

        public HandResolution TechnicalDraw(HandScore playerScore, HandScore dealerScore)
        {
            return new HandResolution(
                HandOutcome.TechnicalDraw,
                playerScore,
                dealerScore);
        }

        private static HandOutcome GetWinningOutcome(HandScore playerScore)
        {
            return playerScore.IsInitialTwentyThree
                ? HandOutcome.InitialTwentyThree
                : HandOutcome.NormalWin;
        }
    }
}
