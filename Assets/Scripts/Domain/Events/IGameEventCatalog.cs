using System.Collections.Generic;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Events
{
    public interface IGameEventCatalog
    {
        IReadOnlyList<GameEventDefinition> Definitions { get; }

        BasisPoints VoicesTruthProbability { get; }

        BasisPoints FalseMessageSignalProbability { get; }

        BasisPoints TrueMessageSignalProbability { get; }

        BasisPoints DistractedNetGainBonus { get; }

        GameEventDefinition Get(GameEventId id);
    }
}
