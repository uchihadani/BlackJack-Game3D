using UnityEngine;
using UnityEngine.SceneManagement;

public class Mainmenu : MonoBehaviour
{

    void start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void Start()
    {
        // aca van las esenas
        SceneManager.LoadScene("guardadoautomatico no se como nombrarlo");
    }

    public void exit()
    {
        Application.Quit();
    }
}