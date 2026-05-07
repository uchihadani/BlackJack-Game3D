using UnityEngine;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
    [SerializeField] private List<int> mazo = new List<int>();
   
    public void GenerarMazo()
    {
        mazo.Clear();
        for (int i = 1; i <= 13; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int valorCarta = i;
                if(valorCarta > 10)
                {
                    valorCarta = 10;
                }
                mazo.Add(valorCarta);
            }
        }
    }

    public void MezclarMazo()
    {
        for(int i=0; i < mazo.Count; i++)
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
