using TwentyThree.Application.Navigation;
using TwentyThree.Presentation.Input;
using TwentyThree.Presentation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TwentyThree.Presentation.UI
{
    public sealed class GameRoomMenuPresenter : MonoBehaviour, IGameFlowConsumer
    {
        [SerializeField] private InputContextController input;
        [SerializeField] private PlayerModeController playerModes;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject eventSystemRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private IGameFlow _gameFlow;
        private bool _isOpen;

        private void Awake()
        {
            menuPanel.SetActive(false);
            eventSystemRoot.SetActive(false);
            resumeButton.onClick.AddListener(Close);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void OnEnable()
        {
            input.PauseRequested += Toggle;
        }

        private void OnDisable()
        {
            input.PauseRequested -= Toggle;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            resumeButton.onClick.RemoveListener(Close);
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        public void Configure(IGameFlow gameFlow)
        {
            _gameFlow = gameFlow;
        }

        public void Toggle()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;
            playerModes.OpenUserInterface();
            menuPanel.SetActive(true);
            eventSystemRoot.SetActive(true);
            Time.timeScale = 0f;
            resumeButton.Select();
        }

        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            eventSystemRoot.SetActive(false);
            menuPanel.SetActive(false);
            Time.timeScale = 1f;
            playerModes.CloseUserInterface();
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            if (_gameFlow != null && !_gameFlow.IsTransitioning)
            {
                _gameFlow.ShowMainMenu();
            }
        }
    }
}
