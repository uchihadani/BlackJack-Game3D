using TwentyThree.Domain.Events;

namespace TwentyThree.Application.Gameplay.Content
{
    internal sealed class VoicesFromBeyondStrategy : IGameEventEffectStrategy
    {
        public GameEventId Id => GameEventId.VoicesFromBeyond;

        public bool RequiresDecision => true;

        public void Accept(IGameEventEffectHost host)
        {
            host.ResolveVoicesFromBeyond();
        }

        public void Reject(IGameEventEffectHost host)
        {
        }
    }

    internal sealed class MeowStrategy : IGameEventEffectStrategy
    {
        public GameEventId Id => GameEventId.Meow;

        public bool RequiresDecision => false;

        public void Accept(IGameEventEffectHost host)
        {
            host.ResolveMeow();
        }

        public void Reject(IGameEventEffectHost host)
        {
        }
    }

    internal sealed class DistractedStrategy : IGameEventEffectStrategy
    {
        public GameEventId Id => GameEventId.Distracted;

        public bool RequiresDecision => true;

        public void Accept(IGameEventEffectHost host)
        {
            host.ActivateDistracted();
        }

        public void Reject(IGameEventEffectHost host)
        {
        }
    }
}
