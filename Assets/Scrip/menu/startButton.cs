using UnityEngine;

public class startButton : MonoBehaviour
{
    [SerializeField] private Selector player;
    [SerializeField] private Canvas startCanvas;
    [SerializeField] private Canvas UIopciones;
    [SerializeField] ManoController ManoController;

    [HideInInspector] public bool EmpezarJuego = false;

    [SerializeField] private DealerController dealer;

    public void StartGame()
    {
        if (player.EstaSentado == true)
        {
            Debug.Log("Empezar el juego");
            EmpezarJuego = true;
            startCanvas.gameObject.SetActive(false);
        }
        
    }

    //CONFIGURACION DE BOTONES PEDIR-DUPLICAR-QUEDARSE
    public void PedirCartaJugador()
    {
        ManoController.DarCarta(dealer.PuntoJugador, true);
        ManoController.VerificarLimite();
    }

    public void Quedarse()
    {
        Debug.Log("Jugador se queda");

        UIopciones.gameObject.SetActive(false);

        dealer.TurnoDealer();
    }


}
