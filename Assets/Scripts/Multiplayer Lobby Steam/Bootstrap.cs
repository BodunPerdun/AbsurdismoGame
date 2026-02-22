using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string MainMenuScene = "MenuScene";

    private void Start()
    {
        // Одразу після запуску гри завантажуємо сцену меню.
        // Заміни "MenuScene" на точну назву твоєї сцени з меню!
        SceneManager.LoadScene(MainMenuScene);
    }
}