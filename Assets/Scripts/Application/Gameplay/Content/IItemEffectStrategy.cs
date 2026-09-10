using TwentyThree.Domain.Items;

namespace TwentyThree.Application.Gameplay.Content
{
    internal interface IItemEffectStrategy
    {
        ItemId Id { get; }

        ItemActivationValidation Validate(IItemEffectHost host);

        void Apply(IItemEffectHost host);
    }
}
