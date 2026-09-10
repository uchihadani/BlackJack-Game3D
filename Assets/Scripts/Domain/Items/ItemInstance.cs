using System;

namespace TwentyThree.Domain.Items
{
    public sealed class ItemInstance
    {
        internal ItemInstance(ItemDefinition definition, int chargesRemaining)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));

            if (chargesRemaining < 0 || chargesRemaining > definition.InitialCharges)
            {
                throw new ArgumentOutOfRangeException(nameof(chargesRemaining));
            }

            ChargesRemaining = chargesRemaining;
        }

        public ItemDefinition Definition { get; }

        public ItemId Id => Definition.Id;

        public int ChargesRemaining { get; private set; }

        public bool IsDepleted => ChargesRemaining == 0;

        internal bool TryConsumeCharge()
        {
            if (ChargesRemaining == 0)
            {
                return false;
            }

            ChargesRemaining--;
            return true;
        }
    }
}
