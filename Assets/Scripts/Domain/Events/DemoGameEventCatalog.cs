using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Content;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Events
{
    public sealed class DemoGameEventCatalog : IGameEventCatalog
    {
        private readonly Dictionary<GameEventId, GameEventDefinition> _byId;
        private readonly ReadOnlyCollection<GameEventDefinition> _definitions;

        public DemoGameEventCatalog(
            BasisPoints voicesProbability,
            Money voicesRejectionCost,
            BasisPoints voicesTruthProbability,
            BasisPoints falseMessageSignalProbability,
            BasisPoints trueMessageSignalProbability,
            int voicesPressureRelief,
            BasisPoints meowProbability,
            int meowLucidityLoss,
            BasisPoints distractedProbability,
            Money distractedRejectionCost,
            int distractedLucidityLoss,
            BasisPoints distractedNetGainBonus)
        {
            if (voicesPressureRelief < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(voicesPressureRelief));
            }

            if (meowLucidityLoss < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(meowLucidityLoss));
            }

            if (distractedLucidityLoss < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distractedLucidityLoss));
            }

            RequireProbability(voicesProbability, nameof(voicesProbability));
            RequireProbability(voicesTruthProbability, nameof(voicesTruthProbability));
            RequireProbability(falseMessageSignalProbability, nameof(falseMessageSignalProbability));
            RequireProbability(trueMessageSignalProbability, nameof(trueMessageSignalProbability));
            RequireProbability(meowProbability, nameof(meowProbability));
            RequireProbability(distractedProbability, nameof(distractedProbability));
            RequireProbability(distractedNetGainBonus, nameof(distractedNetGainBonus));

            VoicesTruthProbability = voicesTruthProbability;
            FalseMessageSignalProbability = falseMessageSignalProbability;
            TrueMessageSignalProbability = trueMessageSignalProbability;
            DistractedNetGainBonus = distractedNetGainBonus;

            GameEventDefinition[] definitions =
            {
                new GameEventDefinition(
                    GameEventId.VoicesFromBeyond,
                    "¿Voces del más allá?",
                    voicesProbability,
                    true,
                    voicesRejectionCost,
                    -voicesPressureRelief,
                    0,
                    ContentDuration.CurrentHand,
                    new[] { ContentTargetKind.VisibleInformation },
                    new[] { EffectCategory.HoleCardInformation }),
                new GameEventDefinition(
                    GameEventId.Meow,
                    "Miau",
                    meowProbability,
                    false,
                    Money.Zero,
                    0,
                    -meowLucidityLoss,
                    ContentDuration.CurrentHand,
                    new[]
                    {
                        ContentTargetKind.PlayerCard,
                        ContentTargetKind.DealerCard
                    },
                    new[] { EffectCategory.MechanicalCardOverride }),
                new GameEventDefinition(
                    GameEventId.Distracted,
                    "Distraído",
                    distractedProbability,
                    true,
                    distractedRejectionCost,
                    0,
                    -distractedLucidityLoss,
                    ContentDuration.CurrentHand,
                    new[]
                    {
                        ContentTargetKind.PlayerCard,
                        ContentTargetKind.VisibleInformation,
                        ContentTargetKind.HandReward
                    },
                    new[]
                    {
                        EffectCategory.SpecificCardConcealment,
                        EffectCategory.NetGainBonus
                    })
            };

            _definitions = Array.AsReadOnly(definitions);
            _byId = new Dictionary<GameEventId, GameEventDefinition>(definitions.Length);
            foreach (GameEventDefinition definition in definitions)
            {
                _byId.Add(definition.Id, definition);
            }
        }

        public IReadOnlyList<GameEventDefinition> Definitions => _definitions;

        public BasisPoints VoicesTruthProbability { get; }

        public BasisPoints FalseMessageSignalProbability { get; }

        public BasisPoints TrueMessageSignalProbability { get; }

        public BasisPoints DistractedNetGainBonus { get; }

        public GameEventDefinition Get(GameEventId id)
        {
            if (!_byId.TryGetValue(id, out GameEventDefinition definition))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return definition;
        }

        public static DemoGameEventCatalog CreateDefault()
        {
            return new DemoGameEventCatalog(
                new BasisPoints(1500),
                Money.FromCoins(20),
                new BasisPoints(7000),
                new BasisPoints(8000),
                new BasisPoints(2000),
                15,
                new BasisPoints(1200),
                25,
                new BasisPoints(800),
                Money.FromCoins(30),
                30,
                new BasisPoints(2000));
        }

        private static void RequireProbability(BasisPoints value, string parameterName)
        {
            if (value.Value > BasisPoints.Scale)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
