using System;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Dealer;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Rules;
using TwentyThree.Domain.Randomness;

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
        private readonly IRandomStreamFactory _randomStreamFactory;

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

        public GameSessionFactory(
            GameRules rules,
            ICardShuffler shuffler,
            IRandomStreamFactory randomStreamFactory)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (shuffler is not IDeckEntryShuffler entryShuffler)
            {
                throw new ArgumentException(
                    "The demo shuffler must support mixed deck entries.",
                    nameof(shuffler));
            }

            _roundDeckFactory = new DemoRoundDeckFactory(
                new DemoDeckFactory(),
                entryShuffler);
            _randomStreamFactory = randomStreamFactory ??
                throw new ArgumentNullException(nameof(randomStreamFactory));
            _handEvaluator = new TwentyThreeHandEvaluator();
            _dealerStrategy = new ThresholdDealerStrategy(rules.DealerStandThreshold);
            _betPlacement = new BetPlacementService();
            _payoutCalculator = new PayoutCalculator();
            _outcomeResolver = new HandOutcomeResolver();
        }

        public IGameSession Create(int seed)
        {
            return _randomStreamFactory == null
                ? new GameSession(
                    _rules,
                    seed,
                    _roundDeckFactory,
                    _handEvaluator,
                    _dealerStrategy,
                    _betPlacement,
                    _payoutCalculator,
                    _outcomeResolver)
                : new GameSession(
                    _rules,
                    seed,
                    _roundDeckFactory,
                    _handEvaluator,
                    _dealerStrategy,
                    _betPlacement,
                    _payoutCalculator,
                    _outcomeResolver,
                    _randomStreamFactory);
        }
    }
}
