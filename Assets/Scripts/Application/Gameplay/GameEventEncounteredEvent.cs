using TwentyThree.Domain.Events;

namespace TwentyThree.Application.Gameplay
{
    public readonly struct GameEventEncounteredEvent
    {
        public GameEventEncounteredEvent(
            GameEventId id,
            int triggerRoll,
            int effectiveProbability)
        {
            Id = id;
            TriggerRoll = triggerRoll;
            EffectiveProbability = effectiveProbability;
        }

        public GameEventId Id { get; }

        public int TriggerRoll { get; }

        public int EffectiveProbability { get; }
    }
}
