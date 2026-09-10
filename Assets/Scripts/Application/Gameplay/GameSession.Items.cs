using TwentyThree.Application.Gameplay.Content;
using TwentyThree.Application.Gameplay.Diagnostics;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        GamePhase IItemEffectHost.Phase => _pendingManualItem.HasValue
            ? _phaseBeforeManualItemDecision
            : Phase;

        bool IItemEffectHost.IsPreparationWindowOpen => _pendingManualItem.HasValue
            ? IsPreparationPhase(_phaseBeforeManualItemDecision)
            : IsPreparationWindowOpen;

        void IItemEffectHost.RevealDealerHoleCardWithMagnifyingGlass()
        {
            NumericCard holeCard = _dealerHand.Cards[2];
            DealerHoleCardRevealed = true;
            if (!_visibleDealerCards.Contains(holeCard))
            {
                _visibleDealerCards.Add(holeCard);
            }

            _activeDistortions.Remove(holeCard.Id);
            _perceptionEvaluatedCards.Add(holeCard.Id);
            ApplyRelationshipChange(DealerRelationshipChange.DefaultFor(
                DealerRelationshipChangeCause.It01MagnifyingGlassUsed));
            _observerDispatcher.Publish(DealerHoleCardRevealedEvent, holeCard);
        }

        void IItemEffectHost.ApplyCigaretteBox()
        {
            ApplyPressureChange(new PressureDelta(
                PressureChangeCause.It04CigaretteBoxUsed,
                -Rules.PhaseThree.CigarettePressureRelief));
            ApplyLucidityChange(new LucidityDelta(
                LucidityAdjustmentCause.It04CigaretteBoxUsed,
                -Rules.PhaseThree.CigaretteLucidityLoss));
            GamePhase activationPhase = _pendingManualItem.HasValue
                ? _phaseBeforeManualItemDecision
                : Phase;
            if (activationPhase == GamePhase.PlayerTurn)
            {
                _blurredVisionCurrentHand = true;
            }
            else
            {
                _blurredVisionNextHand = true;
            }
        }

        public bool IsPreparationWindowOpen =>
            _phaseThreeEnabled &&
            !HasLockedBet &&
            IsPreparationPhase(Phase);

        private static bool IsPreparationPhase(GamePhase phase)
        {
            return phase == GamePhase.Betting ||
                   phase == GamePhase.PostHand ||
                   phase == GamePhase.RoundSettlement ||
                   phase == GamePhase.FundingRequired;
        }

        public GameCommandResult TryUseItem(ItemId itemId)
        {
            if (!_phaseThreeEnabled)
            {
                return GameCommandResult.Reject(GameCommandFailure.ItemUnavailable);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            if (_pendingContentDecision != null)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            if (!_inventory.TryGetItem(
                    itemId,
                    out ItemInstance item,
                    out InventoryLocation location) ||
                location != InventoryLocation.Table ||
                item.IsDepleted ||
                !_itemStrategyFactory.TryCreate(itemId, out IItemEffectStrategy strategy))
            {
                return GameCommandResult.Reject(GameCommandFailure.ItemUnavailable);
            }

            ItemActivationValidation validation = strategy.Validate(this);
            if (!validation.Succeeded)
            {
                return GameCommandResult.Reject(
                    validation.Failure == ItemActivationFailure.ItemBlocked
                        ? GameCommandFailure.ItemBlocked
                        : GameCommandFailure.ItemUnavailable);
            }

            _pendingManualItem = itemId;
            _phaseBeforeManualItemDecision = Phase;
            _pendingContentDecision = new PendingContentDecision(
                ContentSourceKind.Item,
                item.Definition.TechnicalCode,
                item.Definition.DisplayName,
                true,
                true,
                Money.Zero,
                false,
                false);
            AppendInternalHistory(
                InternalHistoryKind.ItemPreview,
                item.Definition.TechnicalCode,
                Phase.ToString());
            SetPhase(GamePhase.ItemDecision);
            _observerDispatcher.Publish(ContentDecisionOpened, _pendingContentDecision);
            return GameCommandResult.Success();
        }

        private GameCommandResult ResolveManualItemDecision(
            ContentDecisionOption option)
        {
            if (!_pendingManualItem.HasValue)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.ContentDecisionUnavailable);
            }

            ItemId itemId = _pendingManualItem.Value;
            GamePhase returnPhase = _phaseBeforeManualItemDecision;
            InternalResourceState resourcesBefore = CaptureInternalResources();
            if (option == ContentDecisionOption.Cancel)
            {
                _pendingManualItem = null;
                _pendingContentDecision = null;
                AppendInternalHistory(
                    InternalHistoryKind.ItemResolved,
                    itemId.ToTechnicalCode(),
                    returnPhase.ToString(),
                    resourcesBefore,
                    choice: option,
                    succeeded: false);
                SetPhase(returnPhase);
                return GameCommandResult.Success();
            }

            if (!_inventory.TryGetItem(
                    itemId,
                    out ItemInstance item,
                    out InventoryLocation location) ||
                location != InventoryLocation.Table ||
                item.IsDepleted ||
                !_itemStrategyFactory.TryCreate(itemId, out IItemEffectStrategy strategy))
            {
                return GameCommandResult.Reject(GameCommandFailure.ItemUnavailable);
            }

            ItemActivationValidation validation = strategy.Validate(this);
            if (!validation.Succeeded)
            {
                return GameCommandResult.Reject(
                    validation.Failure == ItemActivationFailure.ItemBlocked
                        ? GameCommandFailure.ItemBlocked
                        : GameCommandFailure.ItemUnavailable);
            }

            if (returnPhase == GamePhase.PlayerTurn)
            {
                ExpireCommandBoundDistortions();
            }

            InventoryOperationResult consumed =
                _inventory.TryConsumeTableCharge(itemId);
            if (!consumed.Succeeded)
            {
                return GameCommandResult.Reject(GameCommandFailure.ItemUnavailable);
            }

            strategy.Apply(this);
            AppendInternalHistory(
                InternalHistoryKind.ItemResolved,
                itemId.ToTechnicalCode(),
                returnPhase.ToString(),
                resourcesBefore,
                choice: option,
                succeeded: true,
                effectExpiration: itemId == ItemId.IT01
                    ? "EndOfHand"
                    : "ConfiguredHand");
            _pendingManualItem = null;
            _pendingContentDecision = null;
            SetPhase(returnPhase);
            return GameCommandResult.Success();
        }

        public GameCommandResult TryPurchaseItem(
            ItemId itemId,
            InventoryLocation destination)
        {
            if (!IsPreparationWindowOpen)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.PreparationUnavailable);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            _actionInProgress = true;
            try
            {
                bool wasActivated = itemId == ItemId.IT02
                    ? _lastBreathPriceDoubled
                    : itemId == ItemId.IT03 && _progression.SecondChanceUsed;
                ItemPurchaseResult result = _itemCommerce.TryPurchase(
                    _wallet,
                    _inventory,
                    itemId,
                    destination,
                    new ItemPurchaseContext(wasActivated));
                if (!result.Succeeded)
                {
                    return GameCommandResult.Reject(GameCommandFailure.InventoryRejected);
                }

                ReevaluatePreparationFunding();
                PublishReadModel();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryMoveItem(ItemId itemId, InventoryLocation destination)
        {
            if (!IsPreparationWindowOpen)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.PreparationUnavailable);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            _actionInProgress = true;
            try
            {
                InventoryOperationResult result = _inventory.TryMove(itemId, destination);
                if (!result.Succeeded)
                {
                    return GameCommandResult.Reject(GameCommandFailure.InventoryRejected);
                }

                PublishReadModel();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TrySwapItems(ItemId movingItemId, ItemId destinationItemId)
        {
            if (!IsPreparationWindowOpen)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.PreparationUnavailable);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            _actionInProgress = true;
            try
            {
                InventoryOperationResult result =
                    _inventory.TrySwap(movingItemId, destinationItemId);
                if (!result.Succeeded)
                {
                    return GameCommandResult.Reject(GameCommandFailure.InventoryRejected);
                }

                PublishReadModel();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryDiscardItem(ItemId itemId)
        {
            if (!IsPreparationWindowOpen)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.PreparationUnavailable);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            _actionInProgress = true;
            try
            {
                InventoryOperationResult result =
                    _itemCommerce.TryDiscard(_wallet, _inventory, itemId);
                if (!result.Succeeded)
                {
                    return GameCommandResult.Reject(GameCommandFailure.InventoryRejected);
                }

                PublishReadModel();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryDepositProtected(Money amount)
        {
            if (!IsPreparationWindowOpen)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.PreparationUnavailable);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            _actionInProgress = true;
            try
            {
                ProtectedFundsTransferResult result =
                    _protectedFundsService.TryDeposit(_wallet, _inventory, amount);
                if (!result.Succeeded)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.ProtectedFundsTransferRejected);
                }

                ReevaluatePreparationFunding();
                PublishReadModel();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        public GameCommandResult TryWithdrawProtected(Money amount)
        {
            if (!IsPreparationWindowOpen)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.PreparationUnavailable);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            _actionInProgress = true;
            try
            {
                ProtectedFundsTransferResult result =
                    _protectedFundsService.TryWithdraw(_wallet, _inventory, amount);
                if (!result.Succeeded)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.ProtectedFundsTransferRejected);
                }

                ReevaluatePreparationFunding();
                PublishReadModel();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        private void ReevaluatePreparationFunding()
        {
            if (Phase != GamePhase.Betting && Phase != GamePhase.FundingRequired)
            {
                return;
            }

            SetPhase(CanAffordMinimumBet
                ? GamePhase.Betting
                : GamePhase.FundingRequired);
        }

        private bool HasUsableAutomaticItem(ItemId itemId)
        {
            return !_itemsBlockedForRound &&
                   _inventory.TryGetItem(
                       itemId,
                       out ItemInstance item,
                       out InventoryLocation location) &&
                   location == InventoryLocation.Table &&
                   !item.IsDepleted;
        }
    }
}
