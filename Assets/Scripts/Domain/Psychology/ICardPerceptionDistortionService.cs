using TwentyThree.Domain.Randomness;

namespace TwentyThree.Domain.Psychology
{
    public interface ICardPerceptionDistortionService
    {
        CardPerceptionRecord Evaluate(
            CardPerceptionRequest request,
            IRandomStream perceptionStream);
    }
}
