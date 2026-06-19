using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DealerController : MonoBehaviour
{
    [SerializeField] private startButton start;
    [SerializeField] private Deck deck;

    [SerializeField] private GameObject mazo;
    [SerializeField] private GameObject cartaPrefab;
    [SerializeField] private GameObject UIopciones;

    [SerializeField] public Transform PuntoDealer;
    [SerializeField] public Transform PuntoJugador;

    [SerializeField] List<Carta> catraJugador = new List<Carta>();
    [SerializeField] List<Carta> catraDealer = new List<Carta>();

    private int puntajeJugador = 0;
    private int puntajeDealer = 0;

    [HideInInspector] public int cartasJugadorCount = 0;

    private bool gamestarted = false;

    // Update is called once per frame
    void Update()
    {
        if (start.EmpezarJuego && !gamestarted)
        {
            StartGame();
            gamestarted = true;
        }
    }

    void StartGame()
    {
        Debug.Log("Dealer inicia la ronda");
        
        mazo.SetActive(true);
        cartasJugadorCount = 0;

        deck.GenerarMazo();
        deck.MezclarMazo();

        DarCarta(PuntoDealer, false);
        DarCarta(PuntoDealer, false);

        DarCarta(PuntoJugador, true);
        DarCarta(PuntoJugador, true);

        UIopciones.SetActive(true);
    }

    public void DarCarta(Transform punto, bool esJugador)
    {
        int valor = deck.SacarCarta();

        GameObject nuevaCarta = Instantiate(cartaPrefab, punto.position, punto.rotation);

        Carta carta = nuevaCarta.GetComponent<Carta>();
        carta.valor = valor;
       
        if (esJugador)
        {
            catraJugador.Add(carta);
            puntajeJugador += valor;
            cartasJugadorCount++;

            ReacomodarCartas(catraJugador, PuntoJugador);

            Debug.Log("<color=blue>Carta para Jugador: </color>" + valor + " | Total Jugador: " + puntajeJugador);
        }
        else
        {
            catraDealer.Add(carta);
            puntajeDealer += valor;

            ReacomodarCartas(catraDealer, PuntoDealer);

            Debug.Log("<color=green>Carta para Dealer: </color>" + valor + " | Total Dealer: " + puntajeDealer);
        }

    }

    public void ReacomodarCartas(List<Carta> cartas, Transform punto)
    {
        float separacion = 1.1f;
        int total = cartas.Count;

        float offsetCentro = (total - 1) * separacion * 0.5f;

        for (int i = 0; i < total; i++)
        {
            Vector3 pos = punto.position + (punto.right * (i * separacion - offsetCentro));

            cartas[i].transform.position = pos;
        }
    }


    public void TurnoDealer()
    {
        Debug.Log("Turno del Dealer");
        int limiteDealer = 22;
       

        while (puntajeDealer < limiteDealer)
        {
            DarCarta(PuntoDealer, false);
            

            Debug.Log("Dealer roba carta, total: " + puntajeDealer);
        }

        VerResultado();

    }

    public void VerificarLimiteJ()
    {
        if (puntajeJugador > 23)
        {
            Debug.Log("<color=red>Te pasaste de 23, perdiste!</color>");
            UIopciones.SetActive(false);
        }
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
