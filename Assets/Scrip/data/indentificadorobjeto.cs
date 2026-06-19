using UnityEngine;

public class IdentificadorObjeto : MonoBehaviour
{
    // Aquí arrastras el archivo ScriptableObject que creaste en el editor
    [SerializeField] private DatosObjeto datos;

    void Start()
    {
        if (datos != null)
        {
            VisualizarInformacion();
        }
    }

    private void VisualizarInformacion()
    {
        // Ejemplo de cómo leer los datos guardados "sin código"
        Debug.Log($"Cargando objeto: {datos.nombreObjeto} de tipo: {datos.tipo}");

        // Puedes actuar según el tipo utilizando un switch
        switch (datos.tipo)
        {
            case TipoObjeto.Escena:
                // Lógica para objetos estáticos
                break;
            case TipoObjeto.Interactuable:
                // Habilitar prompts de interacción
                break;
            case TipoObjeto.Daeler:
                // Configurar IA con datos.vidaMax o datos.velocidad
                break;
        }
    }
}