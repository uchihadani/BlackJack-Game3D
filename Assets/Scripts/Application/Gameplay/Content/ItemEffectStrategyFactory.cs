using System;
using System.Collections.Generic;
using TwentyThree.Domain.Items;

namespace TwentyThree.Application.Gameplay.Content
{
    internal sealed class ItemEffectStrategyFactory
    {
        private readonly Dictionary<ItemId, IItemEffectStrategy> _strategies;

        public ItemEffectStrategyFactory()
        {
            IItemEffectStrategy[] strategies =
            {
                new MagnifyingGlassStrategy(),
                new CigaretteBoxStrategy()
            };
            _strategies = new Dictionary<ItemId, IItemEffectStrategy>(strategies.Length);
            foreach (IItemEffectStrategy strategy in strategies)
            {
                _strategies.Add(strategy.Id, strategy);
            }
        }

        public bool TryCreate(ItemId id, out IItemEffectStrategy strategy)
        {
            if (!Enum.IsDefined(typeof(ItemId), id))
            {
                strategy = null;
                return false;
            }

            return _strategies.TryGetValue(id, out strategy);
        }
    }
}
