using UnityEngine;



public class PlayerController : MonoBehaviour
{
    [HideInInspector] public float movementSpeed;
    [SerializeField] public Transform PuntoJugador;
    [SerializeField] public int dineroJugador = 1000; 
    [SerializeField] ManoController ManoController;
    [HideInInspector] public int cartasJugadorCount = 0;
    public int Puntajejugador = 0;

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

    public void JugadorPideCarta()
    {
        // 1. Seguridad: No hacer nada si el juego no ha empezado o si el jugador ya perdió
        if (!ManoController.gamestarted || Puntajejugador > 23) 
        {
            Debug.LogWarning("No puedes pedir carta en este momento.");
            return;
        }

        // 2. Le damos la carta al jugador usando tu método existente
        ManoController.DarCarta(PuntoJugador, true);

        // 3. Verificamos inmediatamente si con esta carta el jugador se pasó del límite
        ManoController.VerificarLimite();
    }
}

