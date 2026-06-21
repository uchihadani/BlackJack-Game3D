using UnityEngine;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
    [Header("Base de Datos (Pon aquí todas tus cartas creadas)")]
    // Aquí arrastras desde el editor todas las cartas que crees (malditas y normales)
    [SerializeField] private List<CartaData> todasLasCartasDisponibles = new List<CartaData>();

    [Header("Mazo en Juego (Se llena automáticamente)")]
    // Esta lista será tu mazo real balanceado con las probabilidades
    [SerializeField] private List<CartaData> mazoDeCartas = new List<CartaData>();
    
    private int indiceActual = 0;

    public void GenerarMazo()
    {
        mazoDeCartas.Clear();
        indiceActual = 0;

        // CORRECCIÓN: Cambiar 'en' por 'in'
        foreach (CartaData carta in todasLasCartasDisponibles)
        {
            if (carta.valor <= 4)
            {
                mazoDeCartas.Add(carta);
                mazoDeCartas.Add(carta);
                mazoDeCartas.Add(carta);
                mazoDeCartas.Add(carta);
            }
            else
            {
                mazoDeCartas.Add(carta);
            }
        }

        Debug.Log("Mazo generado con " + mazoDeCartas.Count + " cartas.");
    }

    public void MezclarMazo()
    {
        // Tu algoritmo de intercambio (Fisher-Yates) adaptado a objetos CartaData
        for (int i = 0; i < mazoDeCartas.Count; i++)
        {
            int r = Random.Range(0, mazoDeCartas.Count);

            CartaData temp = mazoDeCartas[i];
            mazoDeCartas[i] = mazoDeCartas[r];
            mazoDeCartas[r] = temp;
        }
        
        Debug.Log("Mazo mezclado exitosamente.");
    }

    // Ahora retorna correctamente el ScriptableObject 'CartaData'
    public CartaData SacarCarta()
    {
        if (mazoDeCartas.Count == 0)
        {
            Debug.LogError("¡El mazo está vacío! Asegúrate de llamar a GenerarMazo antes.");
            return null;
        }

        // Sacamos la carta actual
        CartaData cartaRobada = mazoDeCartas[indiceActual];
        
        // Avanzamos el índice de forma circular
        indiceActual = (indiceActual + 1) % mazoDeCartas.Count; 

        return cartaRobada;
    }
}