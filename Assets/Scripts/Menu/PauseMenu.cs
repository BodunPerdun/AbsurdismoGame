using Edgegap;
using Mirror;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Елементи")]
    public GameObject pauseMenuUI; // Перетягни сюди панель паузи (Canvas або Panel)
    public GameObject blockageOverlayMenuReference; // Посилання на оверлей, який блокує інтерфейс, коли відкрито меню

    // Змінна для відстеження стану меню
    private bool isPaused = false;

    void Start()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.GetComponent<SteamLobby>().blockageOverlay = blockageOverlayMenuReference;
        }
        else
        {
            Debug.LogError("PauseMenu: Не вдалося знайти SteamLobby на NetworkManager.singleton!");
        }
        // На старті гри меню має бути вимкнене, а курсор - заблокований
        ResumeGame();
    }

    void Update()
    {
        // Перевіряємо натискання клавіші Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // --- ЛОГІКА МЕНЮ ---

    private void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        isPaused = true;

        // Вмикаємо курсор та відкріплюємо його від центру екрана
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        isPaused = false;

        // Ховаємо курсор і блокуємо його в центрі екрана (для керування камерою)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --- КНОПКИ МЕНЮ ---

    // Цей метод вішаємо на кнопку "Disconnect" або "Main Menu" у твоєму меню паузи
    public void UI_Disconnect()
    {
        // Оскільки ми повертаємося в головне меню, курсор має бути вільним
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Викликаємо метод виходу через наш Сінглтон!
        if (SteamLobby.instance != null)
        {
            SteamLobby.instance.LeaveLobby();
        }
        else
        {
            Debug.LogError("PauseMenu: Не знайдено SteamLobby.instance! Переконайся, що гра запущена через стартову сцену.");
        }
    }

    // Цей метод можна повісити на кнопку "Resume"
    public void UI_Resume()
    {
        ResumeGame();
    }
    public void UI_QuitGame()
    {
        // Вихід з гри
        Application.Quit();
    }

}