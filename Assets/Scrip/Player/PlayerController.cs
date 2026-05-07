using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float movementSpeed;

    Vector3 moveInput = Vector3.zero;
    CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        move();
    }

    private void move()
    {
        moveInput = new Vector3(Input.GetAxis("Horizontal"),0f, Input.GetAxis("Vertical"));
        moveInput = transform.TransformDirection(moveInput) * movementSpeed;

        characterController.Move(moveInput * Time.deltaTime);
    }
}

