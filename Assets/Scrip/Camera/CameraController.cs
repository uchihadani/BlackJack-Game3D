using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float Sensitivity = 100;
    [SerializeField] private Transform PlayerBody;

    private float ValorX;
    private float ValorY;
    private float RotacionX;
    private float RotacionY;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        ValorX = Input.GetAxis("Mouse X") * Sensitivity * Time.deltaTime;
        ValorY = Input.GetAxis("Mouse Y") * Sensitivity * Time.deltaTime;

        RotacionX += ValorX;
        RotacionY -= ValorY;

        transform.localRotation = Quaternion.Euler(RotacionY, 0, 0);

        PlayerBody.Rotate(Vector3.up * ValorX);
        RotacionY = Mathf.Clamp(RotacionY, -90, 90);
    }
}
