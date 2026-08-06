using System;
using TwentyThree.Application.Settings;
using UnityEngine;

namespace TwentyThree.Infrastructure.Settings
{
    public sealed class PlayerPreferences : IPlayerPreferences
    {
        public const float DefaultMouseLookSensitivity = 0.08f;
        public const float DefaultGamepadLookSpeed = 160f;

        private const string MouseSensitivityKey = "settings.look.mouseSensitivity";
        private const string GamepadSpeedKey = "settings.look.gamepadSpeed";
        private const string InvertVerticalKey = "settings.look.invertVertical";

        private float _mouseLookSensitivity;
        private float _gamepadLookSpeed;
        private bool _invertVerticalLook;
        private bool _hasPendingChanges;

        public PlayerPreferences()
        {
            float storedMouseSensitivity = PlayerPrefs.GetFloat(
                MouseSensitivityKey,
                DefaultMouseLookSensitivity);
            _mouseLookSensitivity = SanitizeFloat(
                storedMouseSensitivity,
                0.01f,
                0.5f,
                DefaultMouseLookSensitivity);

            float storedGamepadSpeed = PlayerPrefs.GetFloat(
                GamepadSpeedKey,
                DefaultGamepadLookSpeed);
            _gamepadLookSpeed = SanitizeFloat(
                storedGamepadSpeed,
                30f,
                360f,
                DefaultGamepadLookSpeed);
            _invertVerticalLook = PlayerPrefs.GetInt(InvertVerticalKey, 0) != 0;

            if (!Mathf.Approximately(storedMouseSensitivity, _mouseLookSensitivity))
            {
                PlayerPrefs.SetFloat(MouseSensitivityKey, _mouseLookSensitivity);
                _hasPendingChanges = true;
            }

            if (!Mathf.Approximately(storedGamepadSpeed, _gamepadLookSpeed))
            {
                PlayerPrefs.SetFloat(GamepadSpeedKey, _gamepadLookSpeed);
                _hasPendingChanges = true;
            }
        }

        public event Action Changed;

        public float MouseLookSensitivity
        {
            get => _mouseLookSensitivity;
            set
            {
                float sanitized = Mathf.Clamp(value, 0.01f, 0.5f);
                if (Mathf.Approximately(_mouseLookSensitivity, sanitized))
                {
                    return;
                }

                _mouseLookSensitivity = sanitized;
                PlayerPrefs.SetFloat(MouseSensitivityKey, sanitized);
                _hasPendingChanges = true;
                NotifyChanged();
            }
        }

        public float GamepadLookSpeed
        {
            get => _gamepadLookSpeed;
            set
            {
                float sanitized = Mathf.Clamp(value, 30f, 360f);
                if (Mathf.Approximately(_gamepadLookSpeed, sanitized))
                {
                    return;
                }

                _gamepadLookSpeed = sanitized;
                PlayerPrefs.SetFloat(GamepadSpeedKey, sanitized);
                _hasPendingChanges = true;
                NotifyChanged();
            }
        }

        public bool InvertVerticalLook
        {
            get => _invertVerticalLook;
            set
            {
                if (_invertVerticalLook == value)
                {
                    return;
                }

                _invertVerticalLook = value;
                PlayerPrefs.SetInt(InvertVerticalKey, value ? 1 : 0);
                _hasPendingChanges = true;
                NotifyChanged();
            }
        }

        public void Save()
        {
            if (!_hasPendingChanges)
            {
                return;
            }

            PlayerPrefs.Save();
            _hasPendingChanges = false;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private static float SanitizeFloat(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Clamp(value, minimum, maximum);
        }
    }
}
