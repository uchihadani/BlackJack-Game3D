namespace TwentyThree.Application.Gameplay.Content
{
    internal enum ItemActivationFailure
    {
        None,
        InvalidPhase,
        ItemUnavailable,
        ItemBlocked,
        PreconditionsNotMet
    }

    internal readonly struct ItemActivationValidation
    {
        public ItemActivationValidation(ItemActivationFailure failure)
        {
            Failure = failure;
        }

        public ItemActivationFailure Failure { get; }

        public bool Succeeded => Failure == ItemActivationFailure.None;

        public static ItemActivationValidation Success()
        {
            return new ItemActivationValidation(ItemActivationFailure.None);
        }

        public static ItemActivationValidation Failed(ItemActivationFailure failure)
        {
            return new ItemActivationValidation(failure);
        }
    }
}
