using System;

namespace TwentyThree.Domain.Items
{
    public enum ItemId
    {
        IT01 = 1,
        IT02 = 2,
        IT03 = 3,
        IT04 = 4,
        IT05 = 5
    }

    public static class ItemIdExtensions
    {
        public static bool IsDemoItem(this ItemId itemId)
        {
            return itemId >= ItemId.IT01 && itemId <= ItemId.IT05;
        }

        public static string ToTechnicalCode(this ItemId itemId)
        {
            switch (itemId)
            {
                case ItemId.IT01:
                    return "IT-01";
                case ItemId.IT02:
                    return "IT-02";
                case ItemId.IT03:
                    return "IT-03";
                case ItemId.IT04:
                    return "IT-04";
                case ItemId.IT05:
                    return "IT-05";
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemId));
            }
        }
    }
}
