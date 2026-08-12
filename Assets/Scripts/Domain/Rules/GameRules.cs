using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Rules
{
    public sealed class GameRules
    {
        private readonly ReadOnlyCollection<Money> _debtsByCycle;
        private readonly ReadOnlyCollection<Money> _maximumBetsByRound;

        public GameRules(
            Money initialMoney,
            IReadOnlyList<Money> debtsByCycle,
            Money minimumBet,
            IReadOnlyList<Money> maximumBetsByRound,
            BasisPoints interestPerRound,
            PayoutRules payouts,
            int handsPerRound,
            int roundsPerCycle,
            int dealerStandThreshold,
            int minimumCardsToStartHand)
        {
            RequirePositive(initialMoney, nameof(initialMoney));
            RequirePositive(minimumBet, nameof(minimumBet));

            if (initialMoney < minimumBet)
            {
                throw new ArgumentOutOfRangeException(nameof(initialMoney));
            }

            if (handsPerRound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(handsPerRound));
            }

            if (roundsPerCycle <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roundsPerCycle));
            }

            if (dealerStandThreshold <= 0 || dealerStandThreshold > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(dealerStandThreshold));
            }

            if (minimumCardsToStartHand < RoundDeck.DefaultMinimumCardsToStartHand ||
                minimumCardsToStartHand > StandardDeckFactory.CardCount)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumCardsToStartHand));
            }

            Money[] debtCopy = CopyRequiredList(debtsByCycle, nameof(debtsByCycle));
            Money[] maximumBetCopy = CopyRequiredList(maximumBetsByRound, nameof(maximumBetsByRound));

            if (maximumBetCopy.Length != roundsPerCycle)
            {
                throw new ArgumentException(
                    "The maximum bet schedule must contain one value per round.",
                    nameof(maximumBetsByRound));
            }

            foreach (Money debt in debtCopy)
            {
                RequirePositive(debt, nameof(debtsByCycle));
            }

            foreach (Money maximumBet in maximumBetCopy)
            {
                if (maximumBet < minimumBet)
                {
                    throw new ArgumentException(
                        "Every maximum bet must be greater than or equal to the minimum bet.",
                        nameof(maximumBetsByRound));
                }
            }

            InitialMoney = initialMoney;
            MinimumBet = minimumBet;
            InterestPerRound = interestPerRound;
            Payouts = payouts;
            HandsPerRound = handsPerRound;
            RoundsPerCycle = roundsPerCycle;
            DealerStandThreshold = dealerStandThreshold;
            MinimumCardsToStartHand = minimumCardsToStartHand;
            _debtsByCycle = Array.AsReadOnly(debtCopy);
            _maximumBetsByRound = Array.AsReadOnly(maximumBetCopy);
        }

        public Money InitialMoney { get; }

        public IReadOnlyList<Money> DebtsByCycle => _debtsByCycle;

        public Money MinimumBet { get; }

        public IReadOnlyList<Money> MaximumBetsByRound => _maximumBetsByRound;

        public BasisPoints InterestPerRound { get; }

        public PayoutRules Payouts { get; }

        public int HandsPerRound { get; }

        public int RoundsPerCycle { get; }

        public int DealerStandThreshold { get; }

        public int MinimumCardsToStartHand { get; }

        public int CycleCount => _debtsByCycle.Count;

        public Money GetDebtForCycle(int cycleNumber)
        {
            return GetOneBased(_debtsByCycle, cycleNumber, nameof(cycleNumber));
        }

        public Money GetMaximumBetForRound(int roundNumber)
        {
            return GetOneBased(_maximumBetsByRound, roundNumber, nameof(roundNumber));
        }

        private static Money[] CopyRequiredList(IReadOnlyList<Money> values, string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (values.Count == 0)
            {
                throw new ArgumentException("At least one value is required.", parameterName);
            }

            Money[] copy = new Money[values.Count];
            for (int index = 0; index < values.Count; index++)
            {
                copy[index] = values[index];
            }

            return copy;
        }

        private static Money GetOneBased(
            IReadOnlyList<Money> values,
            int oneBasedIndex,
            string parameterName)
        {
            if (oneBasedIndex <= 0 || oneBasedIndex > values.Count)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return values[oneBasedIndex - 1];
        }

        private static void RequirePositive(Money value, string parameterName)
        {
            if (value <= Money.Zero)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
