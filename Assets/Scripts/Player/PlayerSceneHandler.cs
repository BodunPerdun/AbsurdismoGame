using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PlayerSceneHandler : NetworkBehaviour
{
    [Header("Налаштування Сцен")]
    public string menuSceneName = "LobbyScene";
    public string gameSceneName = "SampleScene";

    [Header("Компоненти для керування")]
    [Tooltip("Скрипти, які треба вимкнути в лобі (Movement, Shooting тощо)")]
    public MonoBehaviour[] scriptsToControl;

    [Tooltip("Об'єкт камери гравця (FPS/TPS), який треба вмикати в грі")]
    public GameObject playerCameraObject;

    [Tooltip("UI або графіка, яку треба ховати/показувати")]
    public GameObject[] lobbyVisuals;
    public GameObject[] gameVisuals;

    // Додаємо змінну для AudioListener, щоб уникнути конфліктів звуку
    private AudioListener _audioListener;

    private void Awake()
    {
        // Кешуємо AudioListener, якщо він є на камері
        if (playerCameraObject != null)
        {
            _audioListener = playerCameraObject.GetComponent<AudioListener>();
            if (_audioListener == null)
                _audioListener = playerCameraObject.GetComponentInChildren<AudioListener>();
        }
    }

    // Start викликається для ВСІХ гравців (і мого, і чужих)
    private void Start()
    {
        // КРИТИЧНО ВАЖЛИВО:
        // Якщо цей об'єкт НЕ належить мені (це друг, який бігає поруч),
        // ми маємо ЗАЛІЗОБЕТОННО вимкнути його камеру.
        if (!isLocalPlayer)
        {
            if (playerCameraObject != null)
            {
                playerCameraObject.SetActive(false);
            }

            if (_audioListener != null)
            {
                _audioListener.enabled = false;
            }

            // Також можна вимкнути візуал "рук" для чужих гравців, якщо треба
            return;
        }

        // Якщо це МІЙ гравець - перевіряємо сцену і налаштовуємось
        CheckSceneState();
    }

    public override void OnStartLocalPlayer()
    {
        // Це спрацьовує при старті, але також дублюємо логіку в Start для надійності
        CheckSceneState();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (isLocalPlayer)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
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
        // Додатковий захист: ніколи не вмикаємо логіку для чужих гравців
        if (!isLocalPlayer) return;

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == menuSceneName)
        {
            SetupLobbyState();
        }
        else
        {
            SetupGameState();
        }
    }

    private void SetupLobbyState()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetScriptsEnabled(false);

        // В лобі камера гравця не потрібна (там камера меню)
        if (playerCameraObject != null) playerCameraObject.SetActive(false);
        if (_audioListener != null) _audioListener.enabled = false;

        ToggleVisuals(lobbyVisuals, true);
        ToggleVisuals(gameVisuals, false);
    }

    private void SetupGameState()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetScriptsEnabled(true);

        // В грі вмикаємо камеру
        if (playerCameraObject != null)
        {
            playerCameraObject.SetActive(true);
            if (_audioListener != null) _audioListener.enabled = true;

            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.RegisterCamera(playerCameraObject.GetComponent<CinemachineCamera>());
            }
        }

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