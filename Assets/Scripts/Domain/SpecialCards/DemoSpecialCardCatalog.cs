using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Content;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.SpecialCards
{
    public sealed class DemoSpecialCardCatalog : ISpecialCardCatalog
    {
        private readonly Dictionary<SpecialCardId, SpecialCardDefinition> _byId;
        private readonly ReadOnlyCollection<SpecialCardDefinition> _definitions;

        public DemoSpecialCardCatalog(
            Money beginnersLuckRejectionCost,
            Money panicAttackRejectionCost,
            Money blackoutRejectionCost,
            Money thirdEyeRejectionCost,
            Money dealRejectionCost,
            int panicAttackPressure,
            int blackoutPressure,
            int thirdEyePressure,
            int dealPressure)
        {
            SpecialCardDefinition[] definitions =
            {
                new SpecialCardDefinition(
                    SpecialCardId.BeginnersLuck,
                    "Suerte de principiante",
                    SpecialCardCategory.Positive,
                    SpecialCardActivationType.OptionalChoice,
                    ContentTargetKind.RemainingDrawPile,
                    beginnersLuckRejectionCost,
                    0,
                    ContentDuration.Instant,
                    EffectCategory.DeckOrder),
                new SpecialCardDefinition(
                    SpecialCardId.PanicAttack,
                    "Ataque de pánico",
                    SpecialCardCategory.Negative,
                    SpecialCardActivationType.OptionalChoice,
                    ContentTargetKind.ItemActivationSystem,
                    panicAttackRejectionCost,
                    RequireNonNegative(panicAttackPressure, nameof(panicAttackPressure)),
                    ContentDuration.CurrentRound,
                    EffectCategory.ItemActivationBlock),
                new SpecialCardDefinition(
                    SpecialCardId.Blackout,
                    "Apagón",
                    SpecialCardCategory.Event,
                    SpecialCardActivationType.OptionalChoice,
                    ContentTargetKind.VisibleInformation,
                    blackoutRejectionCost,
                    RequireNonNegative(blackoutPressure, nameof(blackoutPressure)),
                    ContentDuration.CurrentHand,
                    EffectCategory.GlobalCardConcealment),
                new SpecialCardDefinition(
                    SpecialCardId.ThirdEye,
                    "Tercer ojo",
                    SpecialCardCategory.Cursed,
                    SpecialCardActivationType.OptionalChoice,
                    ContentTargetKind.PendingNumericReplacement,
                    thirdEyeRejectionCost,
                    RequireNonNegative(thirdEyePressure, nameof(thirdEyePressure)),
                    ContentDuration.Instant,
                    EffectCategory.CardChoice),
                new SpecialCardDefinition(
                    SpecialCardId.WeHaveADeal,
                    "Tenemos un trato",
                    SpecialCardCategory.Cursed,
                    SpecialCardActivationType.DealerOffer,
                    ContentTargetKind.CurrentRun,
                    dealRejectionCost,
                    RequireNonNegative(dealPressure, nameof(dealPressure)),
                    ContentDuration.CurrentRun,
                    EffectCategory.PersistentSacrifice)
            };

            _definitions = Array.AsReadOnly(definitions);
            _byId = new Dictionary<SpecialCardId, SpecialCardDefinition>(definitions.Length);
            foreach (SpecialCardDefinition definition in definitions)
            {
                _byId.Add(definition.Id, definition);
            }
        }

        public IReadOnlyList<SpecialCardDefinition> Definitions => _definitions;

        public SpecialCardDefinition Get(SpecialCardId id)
        {
            if (!_byId.TryGetValue(id, out SpecialCardDefinition definition))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return definition;
        }

        public static DemoSpecialCardCatalog CreateDefault()
        {
            return new DemoSpecialCardCatalog(
                Money.FromCoins(10),
                Money.FromCoins(25),
                Money.FromCoins(30),
                Money.FromCoins(25),
                Money.FromCoins(16),
                20,
                25,
                40,
                50);
        }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return value;
        }
    }
}
