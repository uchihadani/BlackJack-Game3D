using TwentyThree.Domain.Events;

namespace TwentyThree.Application.Gameplay.Content
{
    internal interface IGameEventEffectHost
    {
        GameEventDefinition CurrentEventDefinition { get; }

        void ResolveMeow();

        void ResolveVoicesFromBeyond();

        void ActivateDistracted();
    }
}
