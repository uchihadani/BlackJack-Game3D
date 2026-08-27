using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _initialScreen;
    private VisualElement _menuScreen;
    private Button _btnStart;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        _initialScreen = root.Q<VisualElement>("InitialVE");
        _menuScreen = root.Q<VisualElement>("MenuVE");
        _btnStart = root.Q<Button>("btn-start");

        // Evento botón inicio
        _btnStart.RegisterCallback<ClickEvent>(OnStartClicked);

        // Buscamos las cartas por su nombre exacto
        VisualElement carta1 = root.Q<VisualElement>("Carta1VE");
        VisualElement carta2 = root.Q<VisualElement>("Carta2VE");
        VisualElement carta3 = root.Q<VisualElement>("Carta3VE");

        // Registramos evento usando expresiones lambda para depurar directo
        carta1?.RegisterCallback<ClickEvent>(OnCardClicked);
        carta2?.RegisterCallback<ClickEvent>(OnCardClicked);
        carta3?.RegisterCallback<ClickEvent>(OnCardClicked);
    }

    private void OnStartClicked(ClickEvent evt)
    {
        _initialScreen.style.display = DisplayStyle.None;
        _menuScreen.style.display = DisplayStyle.Flex;
    }

    private void OnCardClicked(ClickEvent evt)
    {
        VisualElement cartaClickeada = evt.currentTarget as VisualElement;
        if (cartaClickeada == null) return;

        // Desactivar para evitar múltiples clics
        cartaClickeada.SetEnabled(false);

        // Iniciar la corrutina de Giro 3D y Caída por 2 segundos
        StartCoroutine(AnimateRotationAndFall(cartaClickeada, 2.0f));
    }

    private IEnumerator AnimateRotationAndFall(VisualElement carta, float duracion)
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = tiempo / duracion;

            // 1. Giro 3D en Y (de 0 a 720 grados)
            float anguloY = Mathf.Lerp(0f, 720f, progreso);
            carta.style.rotate = new Rotate(new Angle(anguloY, AngleUnit.Degree), Vector3.up);

            // 2. Caída hacia abajo (de 0px a 600px en el eje Y)
            float desplazamientoY = Mathf.Lerp(0f, 600f, progreso);
            carta.style.translate = new Translate(0, desplazamientoY, 0);

            // 3. Desvanecimiento (Opacidad de 1 a 0)
            carta.style.opacity = Mathf.Lerp(1f, 0f, progreso);

            yield return null; // Esperar al siguiente frame
        }

        // Asegurar opacidad final y ejecutar acción
        carta.style.opacity = 0f;
        ExecuteCardAction(carta.name);
    }

    private void ExecuteCardAction(string nombreCarta)
    {
        switch (nombreCarta)
        {
            case "Carta1VE":
                Debug.Log("Jugar: Cargando escena...");
                SceneManager.LoadScene("10_GameRoom");
                break;

            case "Carta2VE":
                Debug.Log("Opciones: Abriendo menú...");
                break;

            case "Carta3VE":
                Debug.Log("Salir: Cerrando el juego...");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                break;
        }
    }
}
