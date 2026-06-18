using Unity.Mathematics;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float Sensitivity = 100;
    [SerializeField] private Transform PlayerBody;

    private float ValorX;
    private float ValorY;
    private float RotacionX; 
    private float RotacionY;
    private float Bodyrotation;
    private float Body;
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

        transform.localRotation = Quaternion.Euler(RotacionY, RotacionX, 0);
        if (Bodyrotation < ValorX || Bodyrotation < ValorY)
        {
            Body = Bodyrotation + 1;
        }
        PlayerBody.Rotate(Vector3.up * Body);
        RotacionY = Mathf.Clamp(RotacionY, -90, 90);
        RotacionX = Mathf.Clamp(RotacionX, -90, 90);
    }
}
