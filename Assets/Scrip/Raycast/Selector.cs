using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using Unity.UI;

public class Selector : MonoBehaviour
{
    [SerializeField] float distance = 2f;
    [SerializeField] TMPro.TextMeshProUGUI text;
    [SerializeField] Transform Silla;
    [SerializeField] PlayerController player;
    [SerializeField] Canvas start;
    [SerializeField] Canvas puntero;
    [SerializeField] startButton startButton;

    [HideInInspector] public bool EstaSentado = false;

    private Camera _mainCamera;
    private Vector3 PosicionAntesdeSentarse;

      
    void Start()
    {
        _mainCamera = Camera.main;
        PosicionAntesdeSentarse = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
       if(EstaSentado && Input.GetKeyDown(KeyCode.Q) && startButton.EmpezarJuego == false) 
        {
            Levantarse();
        }

       Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
       RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distance) && hit.collider.CompareTag("ObjetoInteractivo"))
        {
            if (!EstaSentado) text.text = "Presiona E para sentarte";

            if (Input.GetKeyDown(KeyCode.E) && !EstaSentado)
            {
                Sentarse();
            }
        }
        else
        {
            text.text = "";
        }

    }

    void Levantarse()
    {
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;
                
        player.transform.position = PosicionAntesdeSentarse;

        if (cc != null) cc.enabled = true;

        EstaSentado = false;    
        player.movementSpeed = 5; 
        text.text = "Quieres iniciar el juego?";
        start.gameObject.SetActive(false); 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        puntero.gameObject.SetActive(true);


    }

    void Sentarse()
    {
        CharacterController cc = player.GetComponent<CharacterController>();

        PosicionAntesdeSentarse = player.transform.position;
                
        if (cc != null) cc.enabled = false;
                
        player.transform.position = Silla.position;

        if (cc != null) cc.enabled = true;
        
        EstaSentado = true; 
        player.movementSpeed = 0; 
        start.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        puntero.gameObject.SetActive(false);

    }

}
