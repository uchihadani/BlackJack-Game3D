using UnityEngine;

[CreateAssetMenu(fileName = "NuevaCarta", menuName = "JuegoCartas/Carta Data")]
public class CartaData : ScriptableObject
{
    [Header("Datos Básicos")]
    public string nombreCarta;
    public int valor;

    [Header("Efectos Demoníacos")]
    public TipoEfecto efecto;
    public int modificadorDinero;
    
    [TextArea]
    public string descripcionEfecto; // Para saber qué hace desde el editor
}