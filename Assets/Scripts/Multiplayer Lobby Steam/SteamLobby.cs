using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.EventSystems;
using System;

public class SteamLobby : MonoBehaviour
{
    [Header("References")]
    private MenuManager menuManager; // Посилання на ваш скрипт меню
    public NetworkManager networkManager;

    // Callbacks
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<GameOverlayActivated_t>  gameOverlayActivated; // Для відстеження, коли оверлей відкривається

    
    public GameObject blockageOverlay; // UI елемент, який блокує інтерфейс, коли оверлей відкритий

    private const string HostAddressKey = "HostAddress";

    private void Start()
    {
        // 1. Знаходимо NetworkManager (він зазвичай на цьому ж об'єкті)
        if (networkManager == null)
        {
            networkManager = GetComponent<NetworkManager>();
        }

        // 2. Знаходимо MenuManager (він на іншому об'єкті в сцені)
        // GetComponent тут не підходить, треба шукати по всій сцені
        if (menuManager == null)
        {
            // Для нових версій Unity (2023+):
            menuManager = FindFirstObjectByType<MenuManager>();
        }

        // Перевірка, чи ми дійсно все знайшли
        if (menuManager == null) Debug.LogError("SteamLobby: Не знайдено MenuManager на сцені!");
        if (networkManager == null) Debug.LogError("SteamLobby: Не знайдено NetworkManager!");

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
        networkManager.ServerChangeScene("SampleScene");
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

            SteamMatchmaking.SetLobbyData(new CSteamID(result.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());

            // ВАЖЛИВО: Кажемо меню переїхати камерою в лобі
            menuManager.GoToLobby();
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t result)
    {
        SteamMatchmaking.JoinLobby(result.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        if (NetworkServer.active) return; // Якщо ми хост, ми вже все зробили

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(result.m_ulSteamIDLobby), HostAddressKey);
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        // Якщо це клієнт (друг), він теж переходить в лобі візуально
        menuManager.GoToLobby();
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

}