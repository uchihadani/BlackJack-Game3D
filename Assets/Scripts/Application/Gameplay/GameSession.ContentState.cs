using System;
using System.Collections.Generic;
using TwentyThree.Application.Gameplay.Content;
using TwentyThree.Application.Gameplay.Diagnostics;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        private enum SuspendedGameplayFlow
        {
            None,
            InitialDeal,
            PlayerHit,
            DealerTurn,
            EnterPlayerTurn
        }

        private bool _phaseThreeEnabled;
        private IRandomStreamFactory _randomStreamFactory;
        private Dictionary<RandomStreamKey, IRandomStream> _randomStreams;
        private PsychologyState _psychology;
        private ItemInventory _inventory;
        private ItemCommerceService _itemCommerce;
        private ProtectedFundsService _protectedFundsService;
        private ICardPerceptionDistortionService _perceptionService;
        private SpecialCardEffectStrategyFactory _specialStrategyFactory;
        private GameEventEffectStrategyFactory _eventStrategyFactory;
        private ItemEffectStrategyFactory _itemStrategyFactory;
        private HashSet<GameEventId> _activatedEventsThisRound;
        private HashSet<GameEventId> _checkedEventsThisHand;
        private HashSet<CardId> _perceptionEvaluatedCards;
        private Dictionary<CardId, CardPerceptionRecord> _activeDistortions;
        private SuspendedGameplayFlow _suspendedFlow;
        private int _initialDealSlot;
        private bool _pendingNumericDraw;
        private CardRecipient _pendingDrawRecipient;
        private bool _pendingDrawFaceDown;
        private SpecialCardDefinition _pendingSpecial;
        private GameEventDefinition _pendingEvent;
        private ItemId? _pendingManualItem;
        private GamePhase _phaseBeforeManualItemDecision;
        private bool _awaitingBlindNumericChoice;
        private NumericCard? _deferredPlayerThirdCard;
        private bool _itemsBlockedForRound;
        private bool _blackoutActive;
        private bool _distractedActive;
        private bool _distractedBonusActive;
        private bool _blurredVisionCurrentHand;
        private bool _blurredVisionNextHand;
        private bool _eyeSurrendered;
        private bool _lastBreathPriceDoubled;
        private bool _it05CheckedForCurrentDraw;
        private int _it05CheckCount;
        private VoicesMessage? _voicesMessage;
        private TwentyThree.Domain.Gameplay.HandOutcome? _previousHandOutcome;
        private PendingContentDecision _pendingContentDecision;
        private List<InternalRunHistoryEntry> _internalHistory;
        private IReadOnlyList<InternalRunHistoryEntry> _readOnlyInternalHistory;

        public GameSession(
            TwentyThree.Domain.Rules.GameRules rules,
            int runSeed,
            IRoundDeckFactory roundDeckFactory,
            IHandEvaluator handEvaluator,
            TwentyThree.Domain.Dealer.IDealerStrategy dealerStrategy,
            BetPlacementService betPlacement,
            PayoutCalculator payoutCalculator,
            TwentyThree.Domain.Gameplay.HandOutcomeResolver outcomeResolver,
            IRandomStreamFactory randomStreamFactory)
            : this(
                rules,
                runSeed,
                roundDeckFactory,
                handEvaluator,
                dealerStrategy,
                betPlacement,
                payoutCalculator,
                outcomeResolver)
        {
            InitializePhaseThree(randomStreamFactory);
        }

        public int Pressure => _psychology?.Pressure ?? 0;

        public PressureBand PressureBand => _psychology?.PressureBand ?? PressureBand.Control;

        public int Lucidity => _psychology?.Lucidity ?? PsychologyState.InitialLucidity;

        public int LucidityMaximum => _psychology?.LucidityMaximum ?? PsychologyState.InitialMaximumLucidity;

        public DealerRelationship DealerRelationship =>
            _psychology?.DealerRelationship ?? DealerRelationship.Empty;

        public IReadOnlyList<ItemInstance> TableItems =>
            _inventory?.TableItems ?? Array.Empty<ItemInstance>();

        public IReadOnlyList<ItemInstance> StoredItems =>
            _inventory?.StoredItems ?? Array.Empty<ItemInstance>();

        public Money ProtectedFundsCapacity =>
            _protectedFundsService?.GetCapacity(_inventory) ?? Money.Zero;

        public bool ItemsBlockedForCurrentRound => _itemsBlockedForRound;

        public bool BlackoutActive => _blackoutActive;

        public bool DistractedActive => _distractedActive;

        public bool BlurredVisionActive => _blurredVisionCurrentHand;

        public bool EyeSurrendered => _eyeSurrendered;

        public bool LastBreathPriceDoubled => _lastBreathPriceDoubled;

        public bool SecondChanceUsed => _progression.SecondChanceUsed;

        public int RuleRoundIndex => _progression.RuleRoundIndex;

        public int RoundOrdinal => _progression.RoundOrdinal;

        public long RoundInstanceId => _progression.RoundInstanceId;

        public bool IsExtraordinaryRound => _progression.IsExtraordinary;

        public PendingContentDecision PendingContentDecision => _pendingContentDecision;

        public VoicesMessage? CurrentVoicesMessage => _voicesMessage;

        public IReadOnlyCollection<CardPerceptionRecord> ActiveDistortions =>
            _activeDistortions?.Values ?? (IReadOnlyCollection<CardPerceptionRecord>)Array.Empty<CardPerceptionRecord>();

        public event Action<SpecialCardEncounteredEvent> SpecialCardEncountered;

        public event Action<GameEventEncounteredEvent> GameEventEncountered;

        public event Action<PendingContentDecision> ContentDecisionOpened;

        public event Action<CardPerceptionRecord> CardPerceptionEvaluated;

        public event Action<VoicesMessage> VoicesMessageCreated;

        public event Action<PressureChanged> PressureChanged;

        public event Action<LucidityChanged> LucidityChanged;

        public event Action<LucidityMaximumChanged> LucidityMaximumChanged;

        public event Action<DealerRelationshipChanged> DealerRelationshipChanged;

        private void InitializePhaseThree(IRandomStreamFactory randomStreamFactory)
        {
            _randomStreamFactory = randomStreamFactory ??
                throw new ArgumentNullException(nameof(randomStreamFactory));
            _phaseThreeEnabled = Rules.PhaseThree.IsEnabled;
            if (!_phaseThreeEnabled)
            {
                return;
            }

            _psychology = new PsychologyState();
            _inventory = new DemoItemInventoryFactory(Rules.PhaseThree.Items).Create();
            _itemCommerce = new ItemCommerceService(Rules.PhaseThree.Items);
            _protectedFundsService = new ProtectedFundsService(
                Rules.PhaseThree.ProtectedFundsCapacity);
            _perceptionService = new CardPerceptionDistortionService();
            _specialStrategyFactory = new SpecialCardEffectStrategyFactory();
            _eventStrategyFactory = new GameEventEffectStrategyFactory();
            _itemStrategyFactory = new ItemEffectStrategyFactory();
            _activatedEventsThisRound = new HashSet<GameEventId>();
            _checkedEventsThisHand = new HashSet<GameEventId>();
            _perceptionEvaluatedCards = new HashSet<CardId>();
            _activeDistortions = new Dictionary<CardId, CardPerceptionRecord>();
            _internalHistory = new List<InternalRunHistoryEntry>();
            _readOnlyInternalHistory = _internalHistory.AsReadOnly();
            InitializeRoundRandomStreams();
        }

        private void InitializeRoundRandomStreams()
        {
            if (!_phaseThreeEnabled)
            {
                return;
            }

            _randomStreams = new Dictionary<RandomStreamKey, IRandomStream>();
            AddRoundStream(RandomStreamKeys.CardPerception);
            AddRoundStream(RandomStreamKeys.Sc01Reshuffle);
            AddRoundStream(RandomStreamKeys.Ev01Trigger);
            AddRoundStream(RandomStreamKeys.Ev01Truth);
            AddRoundStream(RandomStreamKeys.Ev01Signal);
            AddRoundStream(RandomStreamKeys.Ev02Trigger);
            AddRoundStream(RandomStreamKeys.Ev02Target);
            AddRoundStream(RandomStreamKeys.Ev02Value);
            AddRoundStream(RandomStreamKeys.Ev03Trigger);
        }

        private void AddRoundStream(RandomStreamKey key)
        {
            _randomStreams.Add(key, _randomStreamFactory.Create(CurrentRoundSeed, key));
        }

        private IRandomStream GetRoundStream(RandomStreamKey key)
        {
            return _randomStreams[key];
        }
    }
}
