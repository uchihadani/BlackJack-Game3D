using System;
using System.Collections.Generic;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Application.Gameplay.Content
{
    internal sealed class SpecialCardEffectStrategyFactory
    {
        private readonly Dictionary<SpecialCardId, ISpecialCardEffectStrategy> _strategies;

        public SpecialCardEffectStrategyFactory()
        {
            ISpecialCardEffectStrategy[] strategies =
            {
                new BeginnersLuckStrategy(),
                new PanicAttackStrategy(),
                new BlackoutStrategy(),
                new ThirdEyeStrategy(),
                new WeHaveADealStrategy()
            };

            _strategies = new Dictionary<SpecialCardId, ISpecialCardEffectStrategy>(strategies.Length);
            foreach (ISpecialCardEffectStrategy strategy in strategies)
            {
                _strategies.Add(strategy.Id, strategy);
            }
        }

        public ISpecialCardEffectStrategy Create(SpecialCardId id)
        {
            if (!_strategies.TryGetValue(id, out ISpecialCardEffectStrategy strategy))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return strategy;
        }
    }
}
