using System;
using TwentyThree.Application.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TwentyThree.Presentation.Input
{
    [DefaultExecutionOrder(-500)]
    public sealed class InputContextController : MonoBehaviour, IInputContextController
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private ControlContext initialContext = ControlContext.Exploration;

        private InputActionMap _globalMap;
        private InputActionMap _explorationMap;
        private InputActionMap _tableMap;
        private InputActionMap _uiMap;
        private InputAction _pauseAction;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _interactAction;
        private InputAction _leaveTableAction;
        private InputActionAsset _runtimeActions;
        private bool _hasContext;

        public event Action<ControlContext> ContextChanged;
        public event Action PauseRequested;
        public event Action InteractRequested;
        public event Action LeaveTableRequested;

        public ControlContext CurrentContext { get; private set; }

        public Vector2 Move => IsActionAvailable(_moveAction) ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

        public Vector2 Look => IsActionAvailable(_lookAction) ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

        public bool LookUsesPointer => _lookAction?.activeControl?.device is Pointer;

        public InputActionAsset Actions => _runtimeActions != null ? _runtimeActions : inputActions;

        private void Awake()
        {
            InputActionAsset sourceActions = inputActions != null ? inputActions : InputSystem.actions;
            if (sourceActions == null)
            {
                Debug.LogError("No project-wide InputActionAsset is configured.", this);
                enabled = false;
                return;
            }

            _runtimeActions = Instantiate(sourceActions);
            _runtimeActions.name = sourceActions.name + " (Player Runtime)";
            ResolveActions();
        }

        private void OnEnable()
        {
            if (_runtimeActions == null)
            {
                return;
            }

            _pauseAction.performed += OnPausePerformed;
            _interactAction.performed += OnInteractPerformed;
            _leaveTableAction.performed += OnLeaveTablePerformed;

            _runtimeActions.Disable();
            _globalMap.Enable();
            SetContext(initialContext);
        }

        private void OnDisable()
        {
            if (_runtimeActions == null || _pauseAction == null)
            {
                return;
            }

            _pauseAction.performed -= OnPausePerformed;
            _interactAction.performed -= OnInteractPerformed;
            _leaveTableAction.performed -= OnLeaveTablePerformed;
            _runtimeActions.Disable();
            _hasContext = false;
        }

        private void OnDestroy()
        {
            if (_runtimeActions != null)
            {
                Destroy(_runtimeActions);
                _runtimeActions = null;
            }
        }

        public void SetContext(ControlContext context)
        {
            if (!enabled || _runtimeActions == null)
            {
                return;
            }

            if (_hasContext && CurrentContext == context)
            {
                return;
            }

            _explorationMap.Disable();
            _tableMap.Disable();
            _uiMap.Disable();

            switch (context)
            {
                case ControlContext.Exploration:
                    _explorationMap.Enable();
                    break;
                case ControlContext.Table:
                    _tableMap.Enable();
                    break;
                case ControlContext.UserInterface:
                    _uiMap.Enable();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context), context, "Unknown control context.");
            }

            CurrentContext = context;
            _hasContext = true;
            ContextChanged?.Invoke(context);
        }

        private void ResolveActions()
        {
            _globalMap = _runtimeActions.FindActionMap(InputActionPaths.GlobalMap, true);
            _explorationMap = _runtimeActions.FindActionMap(InputActionPaths.ExplorationMap, true);
            _tableMap = _runtimeActions.FindActionMap(InputActionPaths.TableMap, true);
            _uiMap = _runtimeActions.FindActionMap(InputActionPaths.UiMap, true);

            _pauseAction = _runtimeActions.FindAction(InputActionPaths.Pause, true);
            _moveAction = _runtimeActions.FindAction(InputActionPaths.Move, true);
            _lookAction = _runtimeActions.FindAction(InputActionPaths.Look, true);
            _interactAction = _runtimeActions.FindAction(InputActionPaths.Interact, true);
            _leaveTableAction = _runtimeActions.FindAction(InputActionPaths.LeaveTable, true);
        }

        private static bool IsActionAvailable(InputAction action)
        {
            return action != null && action.enabled;
        }

        private void OnPausePerformed(InputAction.CallbackContext _)
        {
            PauseRequested?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext _)
        {
            InteractRequested?.Invoke();
        }

        private void OnLeaveTablePerformed(InputAction.CallbackContext _)
        {
            LeaveTableRequested?.Invoke();
        }
    }
}
