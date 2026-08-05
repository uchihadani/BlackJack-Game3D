using System;
using TwentyThree.Presentation.Player;
using UnityEngine;

namespace TwentyThree.Presentation.Interaction
{
    public sealed class InteractionScanner : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera viewCamera;
        [SerializeField] private PlayerModeController playerModes;
        [SerializeField, Min(0.1f)] private float maximumDistance = 2.5f;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private LayerMask interactableLayers = ~0;

        private IInteractable _focusedInteractable;
        private string _focusedPrompt = string.Empty;

        public event Action<string> PromptChanged;

        public string CurrentPrompt => _focusedPrompt;

        public bool HasFocus => _focusedInteractable != null;

        private void Awake()
        {
            if (viewCamera == null || playerModes == null)
            {
                Debug.LogError("InteractionScanner is missing a required reference.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            RefreshFocus();
        }

        private void OnDisable()
        {
            ClearFocus();
        }

        public void RefreshFocus()
        {
            if (!playerModes.IsExploring)
            {
                ClearFocus();
                return;
            }

            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    maximumDistance,
                    raycastMask,
                    QueryTriggerInteraction.Collide))
            {
                ClearFocus();
                return;
            }

            int hitLayerMask = 1 << hit.collider.gameObject.layer;
            if ((interactableLayers.value & hitLayerMask) == 0)
            {
                ClearFocus();
                return;
            }

            IInteractable interactable = FindInteractable(hit.collider);
            InteractionContext context = CreateContext();
            if (interactable == null || !interactable.CanInteract(context))
            {
                ClearFocus();
                return;
            }

            SetFocus(interactable, interactable.GetPrompt(context));
        }

        public bool TryInteract()
        {
            if (_focusedInteractable == null || !playerModes.IsExploring)
            {
                return false;
            }

            InteractionContext context = CreateContext();
            if (!_focusedInteractable.CanInteract(context))
            {
                ClearFocus();
                return false;
            }

            _focusedInteractable.Interact(context);
            RefreshFocus();
            return true;
        }

        private InteractionContext CreateContext()
        {
            return new InteractionContext(playerModes.gameObject, playerModes);
        }

        private void SetFocus(IInteractable interactable, string prompt)
        {
            string safePrompt = prompt ?? string.Empty;
            if (ReferenceEquals(_focusedInteractable, interactable) && _focusedPrompt == safePrompt)
            {
                return;
            }

            _focusedInteractable = interactable;
            _focusedPrompt = safePrompt;
            PromptChanged?.Invoke(_focusedPrompt);
        }

        private void ClearFocus()
        {
            if (_focusedInteractable == null && string.IsNullOrEmpty(_focusedPrompt))
            {
                return;
            }

            _focusedInteractable = null;
            _focusedPrompt = string.Empty;
            PromptChanged?.Invoke(string.Empty);
        }

        private static IInteractable FindInteractable(Collider collider)
        {
            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }
    }
}
