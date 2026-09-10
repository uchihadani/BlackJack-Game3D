using System.Collections.Generic;

namespace TwentyThree.Domain.Cards
{
    public interface IDeckEntryShuffler
    {
        IReadOnlyList<DeckEntry> ShuffleEntries(
            IReadOnlyList<DeckEntry> entries,
            int seed,
            int streamIndex);
    }
}
