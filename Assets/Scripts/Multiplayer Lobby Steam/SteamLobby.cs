using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    public GameObject hostButton = null;
    private NetworkManager networkManager;
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HostAddressKey = "HostAddress";

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();

        if (!SteamManager.Initialized)
        {
            Debug.LogError("SteamManager не ініціалізовано. Не можна створити лобі.");
            return;
        }


        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby()
    {
        hostButton.SetActive(false); // Сховуємо кнопку хоста після натискання

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);


    }


    private void OnLobbyCreated(LobbyCreated_t result)
    {
        if (result.m_eResult == EResult.k_EResultOK)
        {
            Debug.Log("Лобі створено успішно.");
            networkManager.StartHost();
        }
        else if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Немає з'єднання з Steam. Перевірте підключення до Інтернету та Steam.");
            hostButton.SetActive(true); // Показуємо кнопку хоста, якщо створення лобі не вдалося
            return;
        }
        else
        {
            Debug.LogError("Помилка при створенні лобі: " + result.m_eResult);
        }

        SteamMatchmaking.SetLobbyData(new CSteamID(result.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t result)
    {
        Debug.Log("Запит на приєднання до лобі отримано.");
        SteamMatchmaking.JoinLobby(result.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        Debug.Log("Успішно приєднано до лобі.");
        if (NetworkServer.active) return; // Якщо ми вже є хостом, не запускаємо клієнт

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(result.m_ulSteamIDLobby), HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        hostButton.SetActive(false); // Сховуємо кнопку хоста після приєднання до лобі
    }
}