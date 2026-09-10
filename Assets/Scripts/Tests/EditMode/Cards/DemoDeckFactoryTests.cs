using System.Linq;
using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Tests.EditMode.Cards
{
    public sealed class DemoDeckFactoryTests
    {
        [Test]
        public void CreatesCanonicalFiftySevenEntryDeck()
        {
            var entries = new DemoDeckFactory().Create();

            Assert.That(entries.Count, Is.EqualTo(57));
            Assert.That(entries.Count(entry => entry.IsNumeric), Is.EqualTo(52));
            Assert.That(entries.Count(entry => entry.IsSpecial), Is.EqualTo(5));
            Assert.That(entries.Select(entry => entry.StableIndex).Distinct().Count(), Is.EqualTo(57));
            Assert.That(entries.Take(52).All(entry => entry.IsNumeric), Is.True);
            CollectionAssert.AreEqual(
                new[]
                {
                    SpecialCardId.BeginnersLuck,
                    SpecialCardId.PanicAttack,
                    SpecialCardId.Blackout,
                    SpecialCardId.ThirdEye,
                    SpecialCardId.WeHaveADeal
                },
                entries.Skip(52).Select(entry => entry.SpecialCardId));
        }
    }
}
