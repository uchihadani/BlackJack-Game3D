using TwentyThree.Application.Input;
using TwentyThree.Presentation.Input;
using UnityEngine;

namespace TwentyThree.Presentation.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonMovementController : MonoBehaviour
    {
        [SerializeField] private InputContextController input;
        [SerializeField, Min(0f)] private float movementSpeed = 4.5f;
        [SerializeField, Min(0f)] private float gravity = 24f;
        [SerializeField] private float groundedVerticalVelocity = -2f;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private bool _movementEnabled = true;

        public bool MovementEnabled => _movementEnabled;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (input == null)
            {
                Debug.LogError("FirstPersonMovementController requires an InputContextController.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_movementEnabled || input.CurrentContext != ControlContext.Exploration)
            {
                return;
            }

            Vector2 inputDirection = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 planarVelocity =
                (transform.right * inputDirection.x + transform.forward * inputDirection.y) * movementSpeed;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = groundedVerticalVelocity;
            }
            else
            {
                _verticalVelocity -= gravity * Time.deltaTime;
            }

            Vector3 velocity = planarVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
        }

        public void SetMovementEnabled(bool value)
        {
            _movementEnabled = value;
            if (!value)
            {
                _verticalVelocity = 0f;
            }
        }
    }
}
