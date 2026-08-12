using System;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Dealer;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Rules;

namespace TwentyThree.Application.Gameplay
{
    public sealed class GameSessionFactory : IGameSessionFactory
    {
        private readonly GameRules _rules;
        private readonly IRoundDeckFactory _roundDeckFactory;
        private readonly IHandEvaluator _handEvaluator;
        private readonly IDealerStrategy _dealerStrategy;
        private readonly BetPlacementService _betPlacement;
        private readonly PayoutCalculator _payoutCalculator;
        private readonly HandOutcomeResolver _outcomeResolver;

        public GameSessionFactory(GameRules rules, ICardShuffler shuffler)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _roundDeckFactory = new StandardRoundDeckFactory(
                new StandardDeckFactory(),
                shuffler ?? throw new ArgumentNullException(nameof(shuffler)));
            _handEvaluator = new TwentyThreeHandEvaluator();
            _dealerStrategy = new ThresholdDealerStrategy(rules.DealerStandThreshold);
            _betPlacement = new BetPlacementService();
            _payoutCalculator = new PayoutCalculator();
            _outcomeResolver = new HandOutcomeResolver();
        }

        public IGameSession Create(int seed)
        {
            return new GameSession(
                _rules,
                seed,
                _roundDeckFactory,
                _handEvaluator,
                _dealerStrategy,
                _betPlacement,
                _payoutCalculator,
                _outcomeResolver);
        }
    }
}
