using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Items
{
    public sealed class DemoItemCatalogFactory : IItemCatalogFactory
    {
        private readonly Money _it01Price;
        private readonly Money _it02InitialPrice;
        private readonly Money _it02ActivatedPrice;
        private readonly Money _it03Price;
        private readonly Money _it04Price;
        private readonly Money _it05Price;

        public DemoItemCatalogFactory()
            : this(
                Money.FromCoins(40),
                Money.FromCoins(20),
                Money.FromCoins(40),
                Money.FromCoins(50),
                Money.FromCoins(20),
                Money.FromCoins(35))
        {
        }

        public DemoItemCatalogFactory(
            Money it01Price,
            Money it02InitialPrice,
            Money it02ActivatedPrice,
            Money it03Price,
            Money it04Price,
            Money it05Price)
        {
            _it01Price = it01Price;
            _it02InitialPrice = it02InitialPrice;
            _it02ActivatedPrice = it02ActivatedPrice;
            _it03Price = it03Price;
            _it04Price = it04Price;
            _it05Price = it05Price;
        }

        public IItemCatalog Create()
        {
            return new ItemCatalog(new[]
            {
                new ItemDefinition(
                    ItemId.IT01,
                    "Lupa de prestamista",
                    _it01Price,
                    _it01Price,
                    1,
                    ItemActivationMode.Manual,
                    false,
                    false,
                    true),
                new ItemDefinition(
                    ItemId.IT02,
                    "Último aliento",
                    _it02InitialPrice,
                    _it02ActivatedPrice,
                    1,
                    ItemActivationMode.Automatic,
                    false,
                    false,
                    true),
                new ItemDefinition(
                    ItemId.IT03,
                    "Segunda Oportunidad",
                    _it03Price,
                    _it03Price,
                    1,
                    ItemActivationMode.Automatic,
                    false,
                    false,
                    false),
                new ItemDefinition(
                    ItemId.IT04,
                    "Caja de Pucho",
                    _it04Price,
                    _it04Price,
                    3,
                    ItemActivationMode.Manual,
                    true,
                    true,
                    true),
                new ItemDefinition(
                    ItemId.IT05,
                    "Moneda de la suerte",
                    _it05Price,
                    _it05Price,
                    1,
                    ItemActivationMode.Automatic,
                    false,
                    false,
                    true)
            });
        }
    }
}
