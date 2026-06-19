using UnityEngine;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
    [SerializeField] private List<int> mazo = new List<int>();
    [SerializeField] private List<int> maldiciones = new List<int>();
   
    public void GenerarMazo()
    {
        mazo.Clear();
        for (int valor = 0; valor <= 11; valor++)
        {
            if (valor <= 4)
            {
                mazo.Add(valor); mazo.Add(valor); mazo.Add(valor); mazo.Add(valor);  
            }
            else
            {
                mazo.Add(valor);
            }
        }
    }

    public void MezclarMazo()
    {
        for(int i = 0; i < mazo.Count; i++)
        {
            int r = Random.Range(0, mazo.Count);

            int temp = mazo[i];
            mazo[i] = mazo[r];
            mazo[r] = temp;
        }
    }

    public int SacarCarta()
    {
        if(mazo.Count == 0)
        {
            GenerarMazo();
            MezclarMazo();
        }
        int valor = mazo[0];
        mazo.RemoveAt(0);

        return valor;
    }
}

