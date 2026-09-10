using System.Collections.Generic;

namespace TwentyThree.Domain.Items
{
    public interface IItemCatalog
    {
        IReadOnlyList<ItemDefinition> Definitions { get; }

        bool TryGetDefinition(ItemId itemId, out ItemDefinition definition);
    }

    public interface IItemCatalogFactory
    {
        IItemCatalog Create();
    }
}
