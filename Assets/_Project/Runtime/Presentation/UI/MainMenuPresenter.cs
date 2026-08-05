using TMPro;
using TwentyThree.Application.Navigation;
using TwentyThree.Application.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace TwentyThree.Presentation.UI
{
    public sealed class MainMenuPresenter : MonoBehaviour, IGameFlowConsumer, IPlayerPreferencesConsumer
    {
        [Header("Primary actions")]
        [SerializeField] private Button newRunButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        [Header("Options")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button closeOptionsButton;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Slider gamepadLookSpeedSlider;
        [SerializeField] private Toggle invertVerticalToggle;
        [SerializeField] private TMP_Text mouseSensitivityValue;
        [SerializeField] private TMP_Text gamepadLookSpeedValue;

        private IGameFlow _gameFlow;
        private IPlayerPreferences _preferences;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            optionsPanel.SetActive(false);
            continueButton.interactable = false;

            newRunButton.onClick.AddListener(OnNewRunClicked);
            optionsButton.onClick.AddListener(OpenOptions);
            closeOptionsButton.onClick.AddListener(CloseOptions);
            quitButton.onClick.AddListener(OnQuitClicked);
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            gamepadLookSpeedSlider.onValueChanged.AddListener(OnGamepadLookSpeedChanged);
            invertVerticalToggle.onValueChanged.AddListener(OnInvertVerticalChanged);
        }

        private void Start()
        {
            newRunButton.Select();
        }

        private void OnDestroy()
        {
            newRunButton.onClick.RemoveListener(OnNewRunClicked);
            optionsButton.onClick.RemoveListener(OpenOptions);
            closeOptionsButton.onClick.RemoveListener(CloseOptions);
            quitButton.onClick.RemoveListener(OnQuitClicked);
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
            gamepadLookSpeedSlider.onValueChanged.RemoveListener(OnGamepadLookSpeedChanged);
            invertVerticalToggle.onValueChanged.RemoveListener(OnInvertVerticalChanged);

            if (_preferences != null)
            {
                _preferences.Changed -= RefreshOptions;
                _preferences.Save();
            }
        }

        public void Configure(IGameFlow gameFlow)
        {
            _gameFlow = gameFlow;
        }

        public void Configure(IPlayerPreferences preferences)
        {
            if (_preferences != null)
            {
                _preferences.Changed -= RefreshOptions;
            }

            _preferences = preferences;
            if (_preferences != null)
            {
                _preferences.Changed += RefreshOptions;
                RefreshOptions();
            }
        }

        private void OnNewRunClicked()
        {
            if (_gameFlow != null && !_gameFlow.IsTransitioning)
            {
                _gameFlow.StartNewRun();
            }
        }

        private void OnQuitClicked()
        {
            _gameFlow?.Quit();
        }

        private void OpenOptions()
        {
            optionsPanel.SetActive(true);
            RefreshOptions();
            closeOptionsButton.Select();
        }

        private void CloseOptions()
        {
            _preferences?.Save();
            optionsPanel.SetActive(false);
            optionsButton.Select();
        }

        private void OnMouseSensitivityChanged(float value)
        {
            if (_preferences != null)
            {
                _preferences.MouseLookSensitivity = value;
            }

            mouseSensitivityValue.text = value.ToString("0.00");
        }

        private void OnGamepadLookSpeedChanged(float value)
        {
            if (_preferences != null)
            {
                _preferences.GamepadLookSpeed = value;
            }

            gamepadLookSpeedValue.text = Mathf.RoundToInt(value).ToString();
        }

        private void OnInvertVerticalChanged(bool value)
        {
            if (_preferences != null)
            {
                _preferences.InvertVerticalLook = value;
            }
        }

        private void RefreshOptions()
        {
            if (_preferences == null)
            {
                return;
            }

            mouseSensitivitySlider.SetValueWithoutNotify(_preferences.MouseLookSensitivity);
            gamepadLookSpeedSlider.SetValueWithoutNotify(_preferences.GamepadLookSpeed);
            invertVerticalToggle.SetIsOnWithoutNotify(_preferences.InvertVerticalLook);
            mouseSensitivityValue.text = _preferences.MouseLookSensitivity.ToString("0.00");
            gamepadLookSpeedValue.text = Mathf.RoundToInt(_preferences.GamepadLookSpeed).ToString();
        }
    }
}
