using System.Linq;
using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Items;

namespace TwentyThree.Tests.EditMode.Items
{
    public sealed class ItemCatalogTests
    {
        [Test]
        public void DemoCatalogContainsTheFiveApprovedDefinitions()
        {
            IItemCatalog catalog = new DemoItemCatalogFactory().Create();

            Assert.That(catalog.Definitions.Select(definition => definition.Id), Is.EqualTo(new[]
            {
                ItemId.IT01,
                ItemId.IT02,
                ItemId.IT03,
                ItemId.IT04,
                ItemId.IT05
            }));

            AssertDefinition(catalog, ItemId.IT01, "IT-01", "Lupa de prestamista", 40, 40, 1, ItemActivationMode.Manual, false, false, true);
            AssertDefinition(catalog, ItemId.IT02, "IT-02", "Último aliento", 20, 40, 1, ItemActivationMode.Automatic, false, false, true);
            AssertDefinition(catalog, ItemId.IT03, "IT-03", "Segunda Oportunidad", 50, 50, 1, ItemActivationMode.Automatic, false, false, false);
            AssertDefinition(catalog, ItemId.IT04, "IT-04", "Caja de Pucho", 20, 20, 3, ItemActivationMode.Manual, true, true, true);
            AssertDefinition(catalog, ItemId.IT05, "IT-05", "Moneda de la suerte", 35, 35, 1, ItemActivationMode.Automatic, false, false, true);
        }

        [Test]
        public void CatalogRejectsDuplicateDefinitions()
        {
            ItemDefinition definition = GetDefinition(ItemId.IT01);

            Assert.Throws<System.ArgumentException>(() => new ItemCatalog(new[]
            {
                definition,
                definition
            }));
        }

        [Test]
        public void DefinitionRejectsAnUnknownDemoId()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ItemDefinition(
                (ItemId)99,
                "Invalid",
                Money.Zero,
                Money.Zero,
                1,
                ItemActivationMode.Manual,
                false,
                false,
                true));
        }

        private static void AssertDefinition(
            IItemCatalog catalog,
            ItemId id,
            string technicalCode,
            string name,
            long basePrice,
            long activatedPrice,
            int charges,
            ItemActivationMode activationMode,
            bool remainsWhenDepleted,
            bool grantedAtRunStart,
            bool canPurchaseAfterActivation)
        {
            Assert.That(catalog.TryGetDefinition(id, out ItemDefinition definition), Is.True);
            Assert.That(definition.TechnicalCode, Is.EqualTo(technicalCode));
            Assert.That(definition.DisplayName, Is.EqualTo(name));
            Assert.That(definition.BasePurchasePrice, Is.EqualTo(Money.FromCoins(basePrice)));
            Assert.That(definition.PurchasePriceAfterActivation, Is.EqualTo(Money.FromCoins(activatedPrice)));
            Assert.That(definition.InitialCharges, Is.EqualTo(charges));
            Assert.That(definition.ActivationMode, Is.EqualTo(activationMode));
            Assert.That(definition.RemainsWhenDepleted, Is.EqualTo(remainsWhenDepleted));
            Assert.That(definition.GrantedAtRunStart, Is.EqualTo(grantedAtRunStart));
            Assert.That(definition.CanPurchaseAfterActivation, Is.EqualTo(canPurchaseAfterActivation));
        }

        private static ItemDefinition GetDefinition(ItemId itemId)
        {
            IItemCatalog catalog = new DemoItemCatalogFactory().Create();
            Assert.That(catalog.TryGetDefinition(itemId, out ItemDefinition definition), Is.True);
            return definition;
        }
    }
}
