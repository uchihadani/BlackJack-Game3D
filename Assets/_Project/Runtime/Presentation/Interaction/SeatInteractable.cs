using UnityEngine;

namespace TwentyThree.Presentation.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class SeatInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform seatPose;
        [SerializeField] private Transform exitPose;
        [SerializeField] private string prompt = "Interactuar — Sentarse en la mesa";

        private void Awake()
        {
            if (seatPose == null || exitPose == null)
            {
                Debug.LogError("SeatInteractable requires explicit seat and exit poses.", this);
                enabled = false;
            }
        }

        public bool CanInteract(InteractionContext context)
        {
            return enabled && context.PlayerModes != null && context.PlayerModes.IsExploring;
        }

        public string GetPrompt(InteractionContext context)
        {
            return CanInteract(context) ? prompt : string.Empty;
        }

        public void Interact(InteractionContext context)
        {
            if (CanInteract(context))
            {
                context.PlayerModes.TryEnterSeat(seatPose, exitPose);
            }
        }
    }
}
