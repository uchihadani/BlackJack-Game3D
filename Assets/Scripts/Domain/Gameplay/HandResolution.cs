using TwentyThree.Domain.Cards;

namespace TwentyThree.Domain.Gameplay
{
    public readonly struct HandResolution
    {
        public HandResolution(
            HandOutcome outcome,
            HandScore playerScore,
            HandScore dealerScore)
        {
            Outcome = outcome;
            PlayerScore = playerScore;
            DealerScore = dealerScore;
        }

        public HandOutcome Outcome { get; }

        public HandScore PlayerScore { get; }

        public HandScore DealerScore { get; }
    }
}
