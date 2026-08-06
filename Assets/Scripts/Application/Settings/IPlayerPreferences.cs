using System;

namespace TwentyThree.Application.Settings
{
    public interface IPlayerPreferences
    {
        float MouseLookSensitivity { get; set; }

        float GamepadLookSpeed { get; set; }

        bool InvertVerticalLook { get; set; }

        event Action Changed;

        void Save();
    }
}
