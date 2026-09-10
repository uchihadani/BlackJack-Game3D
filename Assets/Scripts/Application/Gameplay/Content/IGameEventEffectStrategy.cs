using TwentyThree.Domain.Events;

namespace TwentyThree.Application.Gameplay.Content
{
    internal interface IGameEventEffectStrategy
    {
        GameEventId Id { get; }

        bool RequiresDecision { get; }

        void Accept(IGameEventEffectHost host);

        void Reject(IGameEventEffectHost host);
    }
}
