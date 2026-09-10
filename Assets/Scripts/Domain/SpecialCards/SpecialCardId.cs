namespace TwentyThree.Domain.SpecialCards
{
    public enum SpecialCardId
    {
        BeginnersLuck = 1,
        PanicAttack = 2,
        Blackout = 3,
        ThirdEye = 4,
        WeHaveADeal = 5
    }

    public static class SpecialCardIdExtensions
    {
        public static string ToTechnicalCode(this SpecialCardId id)
        {
            return id switch
            {
                SpecialCardId.BeginnersLuck => "SC-01",
                SpecialCardId.PanicAttack => "SC-02",
                SpecialCardId.Blackout => "SC-03",
                SpecialCardId.ThirdEye => "SC-04",
                SpecialCardId.WeHaveADeal => "SC-05",
                _ => throw new System.ArgumentOutOfRangeException(nameof(id))
            };
        }
    }
}
