using System.Collections.Generic;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Application.Gameplay
{
    public sealed class GameSessionReadModel
    {
        public GameSessionReadModel(
            GamePhase phase,
            Money availableMoney,
            Money protectedMoney,
            Money remainingDebt,
            Money lockedBet,
            int pressure,
            PressureBand pressureBand,
            int lucidity,
            int lucidityMaximum,
            IReadOnlyList<CardReadModel> playerCards,
            IReadOnlyList<CardReadModel> dealerCards,
            int? visiblePlayerTotal,
            int? visibleDealerTotal,
            IReadOnlyList<ItemInstance> tableItems,
            IReadOnlyList<ItemInstance> storedItems,
            PendingContentDecision pendingDecision)
        {
            Phase = phase;
            AvailableMoney = availableMoney;
            ProtectedMoney = protectedMoney;
            RemainingDebt = remainingDebt;
            LockedBet = lockedBet;
            Pressure = pressure;
            PressureBand = pressureBand;
            Lucidity = lucidity;
            LucidityMaximum = lucidityMaximum;
            PlayerCards = playerCards;
            DealerCards = dealerCards;
            VisiblePlayerTotal = visiblePlayerTotal;
            VisibleDealerTotal = visibleDealerTotal;
            TableItems = tableItems;
            StoredItems = storedItems;
            PendingDecision = pendingDecision;
        }

        public GamePhase Phase { get; }

        public Money AvailableMoney { get; }

        public Money ProtectedMoney { get; }

        public Money RemainingDebt { get; }

        public Money LockedBet { get; }

        public int Pressure { get; }

        public PressureBand PressureBand { get; }

        public int Lucidity { get; }

        public int LucidityMaximum { get; }

        public IReadOnlyList<CardReadModel> PlayerCards { get; }

        public IReadOnlyList<CardReadModel> DealerCards { get; }

        public int? VisiblePlayerTotal { get; }

        public int? VisibleDealerTotal { get; }

        public IReadOnlyList<ItemInstance> TableItems { get; }

        public IReadOnlyList<ItemInstance> StoredItems { get; }

        public PendingContentDecision PendingDecision { get; }
    }
}
