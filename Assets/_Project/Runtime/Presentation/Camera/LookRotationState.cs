using UnityEngine;

namespace TwentyThree.Presentation.Camera
{
    public readonly struct LookRotationStep
    {
        public LookRotationStep(float yawDelta, float pitch)
        {
            YawDelta = yawDelta;
            Pitch = pitch;
        }

        public float YawDelta { get; }

        public float Pitch { get; }
    }

    /// <summary>
    /// Stateful, deterministic look calculation. It retains pitch only; yaw is always a
    /// delta produced by the current input sample, which prevents residual rotation.
    /// </summary>
    public sealed class LookRotationState
    {
        private readonly float _minimumPitch;
        private readonly float _maximumPitch;

        public LookRotationState(float initialPitch, float minimumPitch, float maximumPitch)
        {
            if (minimumPitch > maximumPitch)
            {
                throw new System.ArgumentException("Minimum pitch cannot exceed maximum pitch.");
            }

            _minimumPitch = minimumPitch;
            _maximumPitch = maximumPitch;
            Pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
        }

        public float Pitch { get; private set; }

        public LookRotationStep Evaluate(
            Vector2 lookInput,
            float horizontalScale,
            float verticalScale,
            bool invertVertical)
        {
            float yawDelta = lookInput.x * horizontalScale;
            float pitchDirection = invertVertical ? 1f : -1f;
            float unclampedPitch = Pitch + lookInput.y * verticalScale * pitchDirection;
            Pitch = Mathf.Clamp(unclampedPitch, _minimumPitch, _maximumPitch);

            return new LookRotationStep(yawDelta, Pitch);
        }
    }
}
