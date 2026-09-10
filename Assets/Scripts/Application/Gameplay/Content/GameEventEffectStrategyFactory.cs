using System;
using System.Collections.Generic;
using TwentyThree.Domain.Events;

namespace TwentyThree.Application.Gameplay.Content
{
    internal sealed class GameEventEffectStrategyFactory
    {
        private readonly Dictionary<GameEventId, IGameEventEffectStrategy> _strategies;

        public GameEventEffectStrategyFactory()
        {
            IGameEventEffectStrategy[] strategies =
            {
                new VoicesFromBeyondStrategy(),
                new MeowStrategy(),
                new DistractedStrategy()
            };
            _strategies = new Dictionary<GameEventId, IGameEventEffectStrategy>(
                strategies.Length);
            foreach (IGameEventEffectStrategy strategy in strategies)
            {
                _strategies.Add(strategy.Id, strategy);
            }
        }

        public IGameEventEffectStrategy Create(GameEventId id)
        {
            if (!_strategies.TryGetValue(id, out IGameEventEffectStrategy strategy))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return strategy;
        }
    }
}
