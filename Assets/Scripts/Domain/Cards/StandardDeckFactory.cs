using System;
using System.Collections.Generic;

namespace TwentyThree.Domain.Cards
{
    public sealed class StandardDeckFactory
    {
        public const int SuitCount = 4;
        public const int RanksPerSuit = 13;
        public const int CardCount = SuitCount * RanksPerSuit;

        public IReadOnlyList<NumericCard> Create()
        {
            NumericCard[] cards = new NumericCard[CardCount];
            int index = 0;

            for (int suitValue = 0; suitValue < SuitCount; suitValue++)
            {
                for (int rankValue = 1; rankValue <= RanksPerSuit; rankValue++)
                {
                    cards[index++] = new NumericCard(
                        (CardSuit)suitValue,
                        (CardRank)rankValue);
                }
            }

            return Array.AsReadOnly(cards);
        }
    }
}
