using TwentyThree.Application.Input;
using TwentyThree.Application.Settings;
using TwentyThree.Presentation.Input;
using UnityEngine;

namespace TwentyThree.Presentation.Camera
{
    public sealed class FirstPersonLookController : MonoBehaviour, IPlayerPreferencesConsumer
    {
        [SerializeField] private InputContextController input;
        [SerializeField] private Transform body;
        [SerializeField] private Transform pitchPivot;
        [SerializeField, Range(0.01f, 0.5f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(30f, 360f)] private float gamepadDegreesPerSecond = 160f;
        [SerializeField] private bool invertVertical;
        [SerializeField, Range(-89f, 0f)] private float minimumPitch = -85f;
        [SerializeField, Range(0f, 89f)] private float maximumPitch = 85f;

        private LookRotationState _rotationState;
        private IPlayerPreferences _preferences;
        private bool _lookEnabled = true;

        public bool LookEnabled => _lookEnabled;

        private void Awake()
        {
            if (input == null || body == null || pitchPivot == null)
            {
                Debug.LogError("FirstPersonLookController is missing a required reference.", this);
                enabled = false;
                return;
            }

            float initialPitch = NormalizeSignedAngle(pitchPivot.localEulerAngles.x);
            _rotationState = new LookRotationState(initialPitch, minimumPitch, maximumPitch);
        }

        private void Update()
        {
            if (!_lookEnabled || input.CurrentContext != ControlContext.Exploration)
            {
                return;
            }

            Vector2 lookInput = input.Look;
            bool usesPointer = input.LookUsesPointer;
            float scale = usesPointer
                ? mouseSensitivity
                : gamepadDegreesPerSecond * Time.unscaledDeltaTime;

            LookRotationStep step = _rotationState.Evaluate(
                lookInput,
                scale,
                scale,
                invertVertical);

            if (!Mathf.Approximately(step.YawDelta, 0f))
            {
                body.Rotate(0f, step.YawDelta, 0f, Space.Self);
            }

            pitchPivot.localRotation = Quaternion.Euler(step.Pitch, 0f, 0f);
        }

        private void OnDestroy()
        {
            if (_preferences != null)
            {
                _preferences.Changed -= ApplyPreferences;
            }
        }

        public void Configure(IPlayerPreferences preferences)
        {
            if (_preferences != null)
            {
                _preferences.Changed -= ApplyPreferences;
            }

            _preferences = preferences;
            if (_preferences != null)
            {
                _preferences.Changed += ApplyPreferences;
                ApplyPreferences();
            }
        }

        public void SetLookEnabled(bool value)
        {
            _lookEnabled = value;
        }

        private void ApplyPreferences()
        {
            mouseSensitivity = _preferences.MouseLookSensitivity;
            gamepadDegreesPerSecond = _preferences.GamepadLookSpeed;
            invertVertical = _preferences.InvertVerticalLook;
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
