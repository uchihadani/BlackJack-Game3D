using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TwentyThree.Domain.Items
{
    public sealed class ItemCatalog : IItemCatalog
    {
        private readonly Dictionary<ItemId, ItemDefinition> definitionsById;
        private readonly ReadOnlyCollection<ItemDefinition> definitions;

        public ItemCatalog(IEnumerable<ItemDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            definitionsById = new Dictionary<ItemId, ItemDefinition>();
            List<ItemDefinition> orderedDefinitions = new List<ItemDefinition>();

            foreach (ItemDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Definitions cannot contain null entries.", nameof(definitions));
                }

                if (definitionsById.ContainsKey(definition.Id))
                {
                    throw new ArgumentException("Definitions cannot contain duplicate item IDs.", nameof(definitions));
                }

                definitionsById.Add(definition.Id, definition);
                orderedDefinitions.Add(definition);
            }

            this.definitions = new ReadOnlyCollection<ItemDefinition>(orderedDefinitions);
        }

        public IReadOnlyList<ItemDefinition> Definitions => definitions;

        public bool TryGetDefinition(ItemId itemId, out ItemDefinition definition)
        {
            return definitionsById.TryGetValue(itemId, out definition);
        }
    }
}
