using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string MainMenuScene = "LobbyScene";
    [SerializeField] private NetworkManager NetworkManager;

    private void Start()
    {
        // Одразу після запуску гри завантажуємо сцену меню.
        // Заміни "MenuScene" на точну назву твоєї сцени з меню!
        DontDestroyOnLoad(NetworkManager.gameObject);
        SceneManager.LoadScene(MainMenuScene);
    }
}