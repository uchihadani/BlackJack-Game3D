using System;
using TwentyThree.Application.Input;
using TwentyThree.Presentation.Camera;
using TwentyThree.Presentation.Input;
using TwentyThree.Presentation.Movement;
using UnityEngine;

namespace TwentyThree.Presentation.Player
{
    public interface IPlayerModeController
    {
        bool IsExploring { get; }

        bool TryEnterSeat(Transform seatPose, Transform exitPose);
    }

    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerModeController : MonoBehaviour, IPlayerModeController
    {
        [SerializeField] private InputContextController input;
        [SerializeField] private CursorStateController cursorState;
        [SerializeField] private FirstPersonMovementController movement;
        [SerializeField] private FirstPersonLookController look;
        [SerializeField] private Transform viewPivot;
        [SerializeField, Min(0f)] private float standingEyeHeight = 2.4f;
        [SerializeField, Min(0f)] private float seatedEyeHeight = 1.75f;

        private CharacterController _characterController;
        private Vector3 _returnPosition;
        private Quaternion _returnRotation;
        private Transform _explicitExitPose;
        private PlayerControlMode _modeBeforeUserInterface;

        public event Action<PlayerControlMode> ModeChanged;

        public PlayerControlMode CurrentMode { get; private set; }

        public bool IsExploring => CurrentMode == PlayerControlMode.Exploration;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (viewPivot == null)
            {
                viewPivot = transform.Find("CameraPivot");
            }

            if (input == null || cursorState == null || movement == null || look == null || viewPivot == null)
            {
                Debug.LogError("PlayerModeController is missing a required reference.", this);
                enabled = false;
                return;
            }

            ApplyMode(PlayerControlMode.Exploration, true);
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.LeaveTableRequested += ExitSeat;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.LeaveTableRequested -= ExitSeat;
            }

            if (cursorState != null)
            {
                cursorState.SetPointerMode();
            }
        }

        public bool TryEnterSeat(Transform seatPose, Transform exitPose)
        {
            if (!IsExploring || seatPose == null)
            {
                return false;
            }

            _returnPosition = transform.position;
            _returnRotation = transform.rotation;
            _explicitExitPose = exitPose;

            Teleport(seatPose.position, seatPose.rotation);
            ApplyMode(PlayerControlMode.SeatedAtTable);
            return true;
        }

        public void ExitSeat()
        {
            if (CurrentMode != PlayerControlMode.SeatedAtTable)
            {
                return;
            }

            Vector3 exitPosition = _explicitExitPose != null
                ? _explicitExitPose.position
                : _returnPosition;
            Quaternion exitRotation = _explicitExitPose != null
                ? _explicitExitPose.rotation
                : _returnRotation;

            Teleport(exitPosition, exitRotation);
            _explicitExitPose = null;
            ApplyMode(PlayerControlMode.Exploration);
        }

        public void OpenUserInterface()
        {
            if (CurrentMode == PlayerControlMode.UserInterface)
            {
                return;
            }

            _modeBeforeUserInterface = CurrentMode;
            ApplyMode(PlayerControlMode.UserInterface);
        }

        public void CloseUserInterface()
        {
            if (CurrentMode != PlayerControlMode.UserInterface)
            {
                return;
            }

            ApplyMode(_modeBeforeUserInterface);
        }

        private void ApplyMode(PlayerControlMode mode, bool force = false)
        {
            if (!force && CurrentMode == mode)
            {
                return;
            }

            CurrentMode = mode;
            switch (mode)
            {
                case PlayerControlMode.Exploration:
                    SetEyeHeight(standingEyeHeight);
                    input.SetContext(ControlContext.Exploration);
                    movement.SetMovementEnabled(true);
                    look.SetLookEnabled(true);
                    cursorState.SetExplorationMode();
                    break;
                case PlayerControlMode.SeatedAtTable:
                    SetEyeHeight(seatedEyeHeight);
                    input.SetContext(ControlContext.Table);
                    movement.SetMovementEnabled(false);
                    look.SetLookEnabled(false);
                    cursorState.SetPointerMode();
                    break;
                case PlayerControlMode.UserInterface:
                    input.SetContext(ControlContext.UserInterface);
                    movement.SetMovementEnabled(false);
                    look.SetLookEnabled(false);
                    cursorState.SetPointerMode();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown player mode.");
            }

            ModeChanged?.Invoke(mode);
        }

        private void Teleport(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = _characterController.enabled;
            _characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _characterController.enabled = wasEnabled;
        }

        private void SetEyeHeight(float height)
        {
            Vector3 localPosition = viewPivot.localPosition;
            localPosition.y = height;
            viewPivot.localPosition = localPosition;
        }
    }
}
