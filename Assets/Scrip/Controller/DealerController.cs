using UnityEngine;
using System.Collections.Generic;

public class DealerController : MonoBehaviour
{
    [SerializeField] private startButton start;
    

    [SerializeField] private GameObject mazo;
    [SerializeField] private GameObject cartaPrefab;
    [SerializeField] private GameObject UIopciones;

    [SerializeField] public Transform PuntoDealer;
    [SerializeField] public Transform PuntoJugador;
    [SerializeField] private ManoController ManoController; 
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Deck deck;

    public int puntajeJugador = 0;
    public int puntajeDealer = 0;

    // Update is called once per frame
    void Update()
    {
        if (start.EmpezarJuego && !ManoController.gamestarted)
        {
            StartGame();
            ManoController.gamestarted = true;
        }
    }

    void StartGame()
    {
        Debug.Log("Dealer inicia la ronda");
        
        mazo.SetActive(true);
        playerController.cartasJugadorCount = 0;

        deck.GenerarMazo();
        deck.MezclarMazo();

        ManoController.DarCarta(PuntoDealer, false);
        ManoController.DarCarta(PuntoDealer, false);

        ManoController.DarCarta(PuntoJugador, true);
        ManoController.DarCarta(PuntoJugador, true);

        UIopciones.SetActive(true);
    }

    public void TurnoDealer()
    {
        Debug.Log("Turno del Dealer");
        int limiteSeguroDealer = 22;

        while (puntajeDealer < limiteSeguroDealer)
        {
            Debug.Log("El Dealer necesita cartas porque tiene " + puntajeDealer + ". Robando...");
            ManoController.DarCarta(PuntoDealer, false);
        }
        VerResultado();
    }

    public void VerResultado()
    {
        Debug.Log("Resultado Final");

        if(puntajeDealer > 23)
        {
            Debug.Log("<color=green>Dealer se paso, gana el jugador</color>");
        }
        else if (puntajeDealer > puntajeJugador)
        {
            Debug.Log("<color=red>Gana el Dealer</color>");
        }
        else if (puntajeDealer < puntajeJugador)
        {
            Debug.Log("<color=blue>Gana el Jugador</color>");
        }
        else
        {
            Debug.Log("<color=yellow>Empate</color>");
        }
    }

}
