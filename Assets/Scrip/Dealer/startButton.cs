using UnityEngine;

public class startButton : MonoBehaviour
{
    [SerializeField] private Selector player;
    [SerializeField] private Canvas startCanvas;
    [SerializeField] private Canvas UIopciones;

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
        dealer.DarCarta(dealer.PuntoJugador, true);
        dealer.VerificarLimiteJ();
    }

    public void Quedarse()
    {
        Debug.Log("Jugador se queda");

        UIopciones.gameObject.SetActive(false);

        dealer.TurnoDealer();
    }


}
