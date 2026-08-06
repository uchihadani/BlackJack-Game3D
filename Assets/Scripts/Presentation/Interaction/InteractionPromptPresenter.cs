using TMPro;
using UnityEngine;

namespace TwentyThree.Presentation.Interaction
{
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField] private InteractionScanner scanner;
        [SerializeField] private TMP_Text promptLabel;

        private void Awake()
        {
            if (scanner == null || promptLabel == null)
            {
                Debug.LogError("InteractionPromptPresenter is missing a required reference.", this);
                enabled = false;
                return;
            }

            SetPrompt(scanner.CurrentPrompt);
        }

        private void OnEnable()
        {
            if (scanner != null)
            {
                scanner.PromptChanged += SetPrompt;
            }
        }

        private void OnDisable()
        {
            if (scanner != null)
            {
                scanner.PromptChanged -= SetPrompt;
            }
        }

        private void SetPrompt(string prompt)
        {
            promptLabel.text = prompt;
            promptLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(prompt));
        }
    }
}
