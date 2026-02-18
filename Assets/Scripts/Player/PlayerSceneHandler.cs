using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PlayerSceneHandler : NetworkBehaviour
{
    [Header("Налаштування Сцен")]
    public string menuSceneName = "LobbyScene"; // Назва сцени меню
    public string gameSceneName = "SampleScene"; // Назва ігрової сцени (рівня)

    [Header("Компоненти для керування")]
    [Tooltip("Скрипти, які треба вимкнути в лобі (Movement, Shooting тощо)")]
    public MonoBehaviour[] scriptsToControl;

    [Tooltip("Об'єкт камери гравця (FPS/TPS), який треба вмикати в грі")]
    public GameObject playerCameraObject;

    [Tooltip("UI або графіка, яку треба ховати/показувати")]
    public GameObject[] lobbyVisuals; // Наприклад, моделька для меню
    public GameObject[] gameVisuals;  // Наприклад, руки зі зброєю

    public override void OnStartLocalPlayer()
    {
        // Цей метод викликається тільки для локального гравця
        CheckSceneState();

        // Підписуємось на подію зміни сцени (якщо ви переходите між сценами не знищуючи гравця)
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isLocalPlayer)
        {
            CheckSceneState();
        }
    }

    private void CheckSceneState()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == menuSceneName)
        {
            SetupLobbyState();
        }
        else
        {
            // Вважаємо, що будь-яка інша сцена (або конкретна gameSceneName) - це гра
            SetupGameState();
        }
    }

    private void SetupLobbyState()
    {
        // 1. Розблокуємо курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Вимикаємо керування
        SetScriptsEnabled(false);

        // 3. Вимикаємо бойову камеру
        if (playerCameraObject != null) playerCameraObject.SetActive(false);

        // 4. Візуал
        ToggleVisuals(lobbyVisuals, true);
        ToggleVisuals(gameVisuals, false);
    }

    private void SetupGameState()
    {
        // 1. Блокуємо курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 2. Вмикаємо керування (Movement, Shooting)
        SetScriptsEnabled(true);

        // 3. Вмикаємо камеру
        if (playerCameraObject != null)
        {
            playerCameraObject.SetActive(true);

            // Реєструємо камеру в менеджері (код перенесено з Movement)
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.RegisterCamera(playerCameraObject.GetComponent<CinemachineCamera>());
            }
        }

        // 4. Візуал
        ToggleVisuals(lobbyVisuals, false);
        ToggleVisuals(gameVisuals, true);
    }

    private void SetScriptsEnabled(bool state)
    {
        foreach (var script in scriptsToControl)
        {
            if (script != null) script.enabled = state;
        }
    }

    private void ToggleVisuals(GameObject[] visuals, bool state)
    {
        if (visuals == null) return;
        foreach (var obj in visuals)
        {
            if (obj != null) obj.SetActive(state);
        }
    }
}