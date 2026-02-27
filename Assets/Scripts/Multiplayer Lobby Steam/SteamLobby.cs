using Mirror;
using Steamworks;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby instance;

    [Header("References")]
    public MenuManager menuManager; // Посилання на ваш скрипт меню
    public GameObject blockageOverlay; // UI елемент, який блокує інтерфейс, коли оверлей відкритий
    public NetworkManager networkManager;

    // Callbacks
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<GameOverlayActivated_t>  gameOverlayActivated; // Для відстеження, коли оверлей відкривається

    

    private CSteamID currentLobbyID;

    private const string HostAddressKey = "HostAddress";
    private const string MainLobbyScene = "LobbyScene";
    private const string LevelScene = "SampleScene";

    // Ініціалізація сінглтона
    private void Awake()
    {
        Debug.Log($"🟢 [Awake] Завантажується SteamLobby. Сцена: {gameObject.scene.name}, ID: {gameObject.GetInstanceID()}");

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"👑 [Awake] Цей об'єкт став ГОЛОВНИМ сінглтоном. ID: {gameObject.GetInstanceID()}");
        }
        else if (instance != this)
        {
            Debug.LogError($"❌ [Awake] Знайдено ДУБЛІКАТ! Видаляємо дублікат з ID: {gameObject.GetInstanceID()}");
            Destroy(gameObject);
        }
    }

    private void Start()
    {

        // 1. Знаходимо NetworkManager (він зазвичай на цьому ж об'єкті)
        if (networkManager == null)
        {
            networkManager = GetComponent<NetworkManager>();
        }

        //// Перевірка, чи ми дійсно все знайшли
        //if (menuManager == null) Debug.LogError("SteamLobby: Не знайдено MenuManager на сцені!");
        //if (networkManager == null) Debug.LogError("SteamLobby: Не знайдено NetworkManager!");

        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        gameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
    }

    // 1. Створення лобі (Викликається з кнопки "Host Game" в MenuManager)
    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);
    }

    // 2. Старт гри (Викликається з кнопки "Start Game" в лобі)
    public void StartGame()
    {
        // Переконайтеся, що сцена додана в Build Settings
        networkManager.ServerChangeScene(LevelScene);
    }

    // 3. Відкриття оверлею друзів (Кнопка "Invite Friends")
    public void OpenInviteOverlay()
    {
        if (SteamManager.Initialized)
        {
            SteamFriends.ActivateGameOverlay("invite");
        }
    }

    private void OnLobbyCreated(LobbyCreated_t result)
    {
        if (result.m_eResult == EResult.k_EResultOK)
        {
            Debug.Log("Лобі створено!");
            networkManager.StartHost();
            // Зберігаємо ID нашого лобі
            currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(currentLobbyID, HostAddressKey, SteamUser.GetSteamID().ToString());

            // Кажемо меню переїхати камерою в лобі
            if (menuManager != null) menuManager.UI_GoToLobby();
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t result)
    {
        SteamMatchmaking.JoinLobby(result.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);

        if (NetworkServer.active) return; // Якщо ми хост, ми вже все зробили

        string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, HostAddressKey);
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        // Якщо це клієнт (друг), він теж переходить в лобі візуально
        if (menuManager != null) menuManager.UI_GoToLobby();
    }

    private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
    {

        if (pCallback.m_bActive != 0)
        {
            // Оверлей відкритий
            blockageOverlay.SetActive(true); // Вимикаємо EventSystem, щоб не було конфліктів з оверлеєм
            Debug.Log("Steam Overlay відкритий, EventSystem вимкнено.");
        }
        else
        {
            // Оверлей закритий
            blockageOverlay.SetActive(false); // Вмикаємо EventSystem назад
            Debug.Log("Steam Overlay закритий, EventSystem увімкнено.");
        }
    }

    public void LeaveLobby()
    {
        // 1. Спочатку виходимо з лобі Steam (якщо ми взагалі в ньому є)
        if (currentLobbyID.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = CSteamID.Nil; // Очищаємо змінну
            Debug.Log("Вийшли зі Steam-лобі");
        }

        // 2. Відключаємо Mirror
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // Якщо ми Хост (Сервер + Клієнт)
            networkManager.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            // Якщо ми просто Клієнт
            networkManager.StopClient();
        }

        // 3. НОВЕ: Вручну повертаємося на сцену меню!
        // Заміни "MenuScene" на точну назву твоєї сцени з меню.
        SceneManager.LoadScene(MainLobbyScene);
    }
}