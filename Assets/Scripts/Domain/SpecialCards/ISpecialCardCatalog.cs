using System.Collections.Generic;

namespace TwentyThree.Domain.SpecialCards
{
    public interface ISpecialCardCatalog
    {
        IReadOnlyList<SpecialCardDefinition> Definitions { get; }

        SpecialCardDefinition Get(SpecialCardId id);
    }
}
