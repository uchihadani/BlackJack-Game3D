namespace TwentyThree.Domain.Cards
{
    public interface IHandEvaluator
    {
        HandScore Evaluate(Hand hand);
    }
}
