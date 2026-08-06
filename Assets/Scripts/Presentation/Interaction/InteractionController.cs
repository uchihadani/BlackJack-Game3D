using TwentyThree.Presentation.Input;
using UnityEngine;

namespace TwentyThree.Presentation.Interaction
{
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] private InputContextController input;
        [SerializeField] private InteractionScanner scanner;

        private void Awake()
        {
            if (input == null || scanner == null)
            {
                Debug.LogError("InteractionController is missing a required reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.InteractRequested += OnInteractRequested;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.InteractRequested -= OnInteractRequested;
            }
        }

        private void OnInteractRequested()
        {
            scanner.TryInteract();
        }
    }
}
