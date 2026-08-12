using System.Collections.Generic;

namespace TwentyThree.Domain.Cards
{
    public interface ICardShuffler
    {
        IReadOnlyList<NumericCard> Shuffle(
            IReadOnlyList<NumericCard> cards,
            int seed,
            int streamIndex);
    }
}
