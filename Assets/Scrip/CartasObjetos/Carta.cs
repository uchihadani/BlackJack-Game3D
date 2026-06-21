using UnityEngine;

public class Carta : MonoBehaviour
{
    public CartaData data; // Aquí se guarda toda la info de la carta

    public void ConfigurarCarta(CartaData nuevaData)
    {
        this.data = nuevaData;

        // Si necesitas el valor para el puntaje del Blackjack:
        // int miValor = data.valor;

        if (data.efecto == TipoEfecto.MaldicionDinero)
        {
            GetComponent<SpriteRenderer>().color = Color.red; // Visual demoníaco
        }
    }
}
