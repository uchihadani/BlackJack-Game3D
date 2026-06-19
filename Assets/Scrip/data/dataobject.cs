using UnityEngine;

// el codigo esta hecho con IA, pero aja, preguntale a IA que hace
// Definimos las categorías que necesitas
public enum TipoObjeto
{
    Escena,
    Interactuable,
    Jugador,
    Daeler // enemigo
}

// Esto permite crear el archivo desde el menú Click Derecho -> Create -> BaseDeDatos -> NuevoObjeto
[CreateAssetMenu(fileName = "NuevoObjeto", menuName = "Base de Datos/Objeto")]
public class DatosObjeto : ScriptableObject
{
    [Header("Clasificación")]
    public TipoObjeto tipo; // yo lo pense y me di cuenta que me vale verga

    [Header("Información General")]
    public string nombreObjeto;
    [TextArea] public string descripcion;

    [Header("Atributos")]
    public float Cartas;
    public float Mazo;
}