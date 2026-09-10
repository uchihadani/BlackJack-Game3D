using System;
using System.Collections.Generic;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        public GameSessionReadModel ReadModel
        {
            get
            {
                IReadOnlyList<CardReadModel> playerViews = CreateCardViews(
                    _playerHand,
                    CardRecipient.Player);
                IReadOnlyList<CardReadModel> dealerViews = CreateCardViews(
                    _dealerHand,
                    CardRecipient.Dealer);
                return new GameSessionReadModel(
                    Phase,
                    AvailableMoney,
                    ProtectedMoney,
                    RemainingDebt,
                    LockedBetAmount,
                    Pressure,
                    PressureBand,
                    Lucidity,
                    LucidityMaximum,
                    playerViews,
                    dealerViews,
                    CalculateVisibleTotal(playerViews),
                    CalculateVisibleTotal(dealerViews),
                    CopyItems(TableItems),
                    CopyItems(StoredItems),
                    PendingContentDecision);
            }
        }

        public event Action<GameSessionReadModel> ReadModelChanged;

        public GameCommandResult TryExpireTimedCardDistortion(CardId cardId)
        {
            if (!_phaseThreeEnabled ||
                !_activeDistortions.TryGetValue(cardId, out CardPerceptionRecord record) ||
                record.Expiration != DistortionExpiration.Timed)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.DistortionUnavailable);
            }

            _activeDistortions.Remove(cardId);
            PublishReadModel();
            return GameCommandResult.Success();
        }

        private IReadOnlyList<CardReadModel> CreateCardViews(
            Hand hand,
            CardRecipient recipient)
        {
            CardReadModel[] views = new CardReadModel[hand.Count];
            for (int index = 0; index < hand.Count; index++)
            {
                NumericCard card = hand.Cards[index];
                bool isHoleCard = recipient == CardRecipient.Dealer && index == 2;
                bool hidden = _phaseThreeEnabled &&
                              (_blackoutActive ||
                               recipient == CardRecipient.Player &&
                               index == 2 &&
                               _distractedActive ||
                               isHoleCard && !DealerHoleCardRevealed);
                if (hidden)
                {
                    views[index] = new CardReadModel(
                        recipient,
                        index,
                        true,
                        null,
                        null,
                        null,
                        null,
                        false,
                        false);
                    continue;
                }

                CardPerceptionRecord perception = null;
                bool distorted = _phaseThreeEnabled &&
                                 _activeDistortions.TryGetValue(
                                     card.Id,
                                     out perception);
                bool hasOverride = hand.TryGetValueOverride(card.Id, out int overrideValue);
                CardRank displayedRank = distorted
                    ? perception.FalseRank.Value
                    : card.Rank;
                int displayedValue = distorted
                    ? perception.FalseVisualValue.Value
                    : hasOverride
                        ? overrideValue
                        : GetBaseMechanicalVisualValue(card);
                views[index] = new CardReadModel(
                    recipient,
                    index,
                    false,
                    card.Id,
                    displayedRank,
                    card.Suit,
                    displayedValue,
                    distorted,
                    hasOverride);
            }

            return Array.AsReadOnly(views);
        }

        private static int? CalculateVisibleTotal(IReadOnlyList<CardReadModel> cards)
        {
            int total = 0;
            int flexibleAces = 0;
            foreach (CardReadModel card in cards)
            {
                if (card.IsHidden || !card.DisplayedValue.HasValue)
                {
                    return null;
                }

                total += card.DisplayedValue.Value;
                if (!card.IsDistorted &&
                    !card.HasMechanicalOverride &&
                    card.Rank == CardRank.Ace)
                {
                    flexibleAces++;
                }
            }

            for (int index = 0; index < flexibleAces && total + 10 <= 23; index++)
            {
                total += 10;
            }

            return total;
        }

        private static IReadOnlyList<ItemInstance> CopyItems(
            IReadOnlyList<ItemInstance> source)
        {
            ItemInstance[] copy = new ItemInstance[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return Array.AsReadOnly(copy);
        }

        private void PublishReadModel()
        {
            if (_phaseThreeEnabled)
            {
                _observerDispatcher.Publish(ReadModelChanged, ReadModel);
            }
        }
    }
}
