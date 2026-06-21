using UnityEngine;
using System.Collections.Generic;

public class ManoController : MonoBehaviour
{
    [SerializeField] private Deck deck;
    [SerializeField] private GameObject mazo;
    [SerializeField] private GameObject cartaPrefab;
    [SerializeField] private GameObject UIopciones;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DealerController DealerController;
    [SerializeField] List<Carta> catraJugador = new List<Carta>();
    [SerializeField] List<Carta> catraDealer = new List<Carta>();
    public bool gamestarted = false;
    

    public void DarCarta(Transform punto, bool esJugador)
    {
        // 1. CORRECCIÓN: El mazo ahora devuelve un ScriptableObject tipo 'CartaData', no 'Deck.Datos'
        CartaData datos = deck.SacarCarta(); 

        if (datos == null) return; // Seguridad por si acaso el mazo se queda vacío

        GameObject nuevaCarta = Instantiate(cartaPrefab, punto.position, punto.rotation);

        Carta carta = nuevaCarta.GetComponent<Carta>();
        
        // 2. CORRECCIÓN: Ahora le pasamos el ScriptableObject completo a la carta
        carta.ConfigurarCarta(datos);
    
        if (esJugador)
        {
            catraJugador.Add(carta);
            // 3. CORRECCIÓN: C# es sensible a mayúsculas. Cambiado 'carta.Valor' por 'datos.valor' (o 'carta.data.valor')
            playerController.Puntajejugador += datos.valor; 
            playerController.cartasJugadorCount++;

            // --- ¡AQUÍ SE APLICA LA MALDICIÓN! ---
            // 4. CORRECCIÓN: Accedemos al efecto y modificador a través del ScriptableObject 'datos'
            if (datos.efecto == TipoEfecto.MaldicionDinero)
            {
                playerController.dineroJugador += datos.modificadorDinero; // Se suma el número negativo, restando plata
                Debug.Log("<color=purple>¡MALDICIÓN! Perdiste " + Mathf.Abs(datos.modificadorDinero) + " de plata. Dinero actual: </color>" + playerController.dineroJugador);
            }

            ReacomodarCartas(catraJugador, playerController.PuntoJugador);
            Debug.Log("<color=blue>Carta para Jugador: </color>" + datos.valor + " | Total Jugador: " + playerController.Puntajejugador);
        }
        else
        {
            // Las maldiciones le dan bonificaciones al dealer
            catraDealer.Add(carta);
            DealerController.puntajeDealer += datos.valor;

            if (datos.efecto == TipoEfecto.MaldicionDinero)
            {
                // El modificadorDinero es negativo (ej. -150). 
                int robo = Mathf.Abs(datos.modificadorDinero);
                playerController.dineroJugador -= robo; 
                
                Debug.Log("<color=red>👿 ¡El Dealer demonio usó la maldición para robarte " + robo + " de plata directamente!</color>");
            }

            ReacomodarCartas(catraDealer, DealerController.PuntoDealer);
            Debug.Log("<color=green>Carta para Dealer: </color>" + datos.valor + " | Total Dealer: " + DealerController.puntajeDealer);
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

    public void VerificarLimite()
    {
        if (playerController.Puntajejugador > 23)
        {
            Debug.Log("<color=red>Te pasaste de 23, perdiste!</color>");
            UIopciones.SetActive(false);
        }
    }
}
