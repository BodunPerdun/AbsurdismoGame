using DG.Tweening;
using Mirror; // Додано для Single Player логіки
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Зв'язок з мережею")]
    public SteamLobby steamLobby; // ПЕРЕТЯГНІТЬ СЮДИ ОБ'ЄКТ З NETWORK MANAGER

    [Header("Система")]
    public CustomizationManager cust;
    public CinemachineBrain brain;
    public CinemachineCamera mainCam, secondCam, loudautCam, lobbyCam;

    [Header("Інтерфейс Меню")]
    public Animator doorAnim;
    public AudioSource musicSource;
    public Text titleText;

    // UI Групи
    public CanvasGroup customizationPanel;
    public CanvasGroup menuButtons;
    public CanvasGroup lobbyButtons;

    [Header("Кнопки Лобі")]
    public Button hostGameButton; // щоб ховати її від клієнтів, які приєднуються до лобі
    public Button startGameButton; // Кнопка "Start Game", щоб ховати її від клієнтів
    public Button inviteButton;

    [Header("Вкладки")]
    public GameObject hatPanel;
    public GameObject beardPanel;
    public GameObject browPanel;
    public GameObject bodyPanel;
    public Button tabHatBtn, tabBeardBtn, tabBrowBtn, tabBodyBtn;

    [Header("Дані Предметів")]
    public GameObject bobyHairModel;
    public CustomizationManager.CustomizationItem[] hats;
    public CustomizationManager.CustomizationItem[] beards;
    public CustomizationManager.CustomizationItem[] brows;

    [Header("Дані Палітри")]
    public ColorPreset[] skinColors;
    public ColorPreset[] commonHairColors;
    public ColorPreset[] pantsColors;

    private bool isInLoadout;
    private bool isLobby;
    private bool isGameStarted = false;

    void Start()
    {
        // Ініціалізація UI
        customizationPanel.alpha = 0;
        customizationPanel.gameObject.SetActive(false);

        menuButtons.alpha = 0;
        menuButtons.gameObject.SetActive(false);

        lobbyButtons.alpha = 0;
        lobbyButtons.gameObject.SetActive(false); // Ховаємо лобі на старті

        cust.GenerateUI(hats, beards, brows, skinColors, commonHairColors, pantsColors, HandleItemSelection, HandleColorSelection);

        tabHatBtn.onClick.AddListener(() => SwitchTab("Hat"));
        tabBeardBtn.onClick.AddListener(() => SwitchTab("Beard"));
        tabBrowBtn.onClick.AddListener(() => SwitchTab("Brow"));
        tabBodyBtn.onClick.AddListener(() => SwitchTab("Body"));

        LoadCharacterPrefs();
        SwitchTab("Hat");
    }

    // --- ЛОГІКА КНОПОК (Прив'яжіть це в Inspector) ---

    // Кнопка "HOST GAME"
    public void UI_HostGame()
    {
        // Вимикаємо кнопки меню, щоб не натиснули двічі
        menuButtons.interactable = false;
        // Запускаємо створення лобі через SteamLobby
        steamLobby.HostLobby();
        // Примітка: GoToLobby() викличеться автоматично з SteamLobby, коли лобі створиться
    }

    // Кнопка "SINGLE PLAYER"
    public void UI_SinglePlayer()
    {
        // Для одиночної гри Steam лобі не потрібне
        // Просто запускаємо локальний хост і одразу міняємо сцену
        NetworkManager.singleton.maxConnections = 1;
        NetworkManager.singleton.StartHost();
        NetworkManager.singleton.ServerChangeScene("SampleScene"); // Вкажіть точну назву сцени
    }

    // Кнопка "INVITE FRIEND" (всередині лобі)
    public void UI_InviteFriends()
    {
        steamLobby.OpenInviteOverlay();
    }

    // Кнопка "START" (всередині лобі, тільки для хоста)
    public void UI_StartMatch()
    {
        steamLobby.StartGame();
    }

    // Кнопка "EXIT" (вихід з гри)
    public void UI_ExitGame()
    {
        Application.Quit();
    }

    // --------------------------------------------------

    public void GoToStartMenu()
    {
        if (isGameStarted) return;
        isGameStarted = true;

        if (musicSource != null) musicSource.PlayDelayed(0.5f);

        titleText?.DOFade(0, 0.6f).OnComplete(() => titleText.gameObject.SetActive(false));
        doorAnim?.SetTrigger("isStarted");

        brain.DefaultBlend.Time = 5f;
        SetCameras(0, 20, 0, 0);

        DOVirtual.DelayedCall(3f, () => {
            menuButtons.gameObject.SetActive(true);
            menuButtons.DOFade(1, 1.5f);
            menuButtons.interactable = true; // Вмикаємо взаємодію
        });
    }

    public void GoToLoadout()
    {
        menuButtons.DOFade(0, 0.3f).OnComplete(() => menuButtons.gameObject.SetActive(false));
        lobbyButtons.DOFade(0, 0.3f).OnComplete(() => lobbyButtons.gameObject.SetActive(false));

        brain.DefaultBlend.Time = 2f;
        SetCameras(0, 0, 20, 0);
        isInLoadout = true;

        DOVirtual.DelayedCall(1.5f, () => {
            if (isInLoadout)
            {
                customizationPanel.gameObject.SetActive(true);
                customizationPanel.DOFade(1, 0.5f);
            }
        });
    }

    public void BackToSecondCam()
    {
        if (isInLoadout && !isLobby)
        {
            customizationPanel.DOFade(0, 0.3f).OnComplete(() => customizationPanel.gameObject.SetActive(false));


            SetCameras(0, 20, 0, 0);
            isInLoadout = false;

            DOVirtual.DelayedCall(1.5f, () => {
                if (!isInLoadout)
                {
                    menuButtons.gameObject.SetActive(true);
                    menuButtons.DOFade(1, 0.5f);
                    menuButtons.interactable = true;
                }
            });
        }

        if (isLobby)
        {
            customizationPanel.DOFade(0, 0.3f).OnComplete(() => customizationPanel.gameObject.SetActive(false));

            SetCameras(0, 0, 0, 20);
            isInLoadout = false;

            DOVirtual.DelayedCall(1.5f, () => {
                if (!isInLoadout)
                {
                    lobbyButtons.gameObject.SetActive(true);
                    lobbyButtons.DOFade(1, 0.5f);
                    lobbyButtons.interactable = true;
                }
            });
        }
    }

    public void GoToLobby()
    {
        // 1. Ховаємо кнопки меню
        menuButtons.DOFade(0, 0.3f).OnComplete(() => menuButtons.gameObject.SetActive(false));
        
        isLobby = true; // Встановлюємо прапорець, що ми в лобі, щоб логіка в Update() могла реагувати на це

        // 2. Їдемо камерою до лобі
        brain.DefaultBlend.Time = 2f;
        SetCameras(0, 0, 0, 20);

        // 3. Показуємо кнопки лобі
        DOVirtual.DelayedCall(1.5f, () => {
            lobbyButtons.gameObject.SetActive(true);
            lobbyButtons.DOFade(1, 0.5f);

            // Якщо ми НЕ сервер (тобто ми приєдналися), ховаємо кнопку старту
            if (!NetworkServer.active)
            {
                if (startGameButton != null) startGameButton.gameObject.SetActive(false);
            }
            else
            {
                // Якщо ми хост - показуємо
                if (startGameButton != null) startGameButton.gameObject.SetActive(true);
            }
        });
    }

    private void SetCameras(int m, int s, int l, int lobby)
    {
        mainCam.Priority = m;
        secondCam.Priority = s;
        loudautCam.Priority = l;
        lobbyCam.Priority = lobby;
    }

    // --- Стандартні методи кастомизації ---

    private void SwitchTab(string tag)
    {
        hatPanel.SetActive(tag == "Hat");
        beardPanel.SetActive(tag == "Beard");
        browPanel.SetActive(tag == "Brow");
        bodyPanel.SetActive(tag == "Body");

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);

        tabHatBtn.image.color = tag == "Hat" ? activeColor : inactiveColor;
        tabBeardBtn.image.color = tag == "Beard" ? activeColor : inactiveColor;
        tabBrowBtn.image.color = tag == "Brow" ? activeColor : inactiveColor;
        tabBodyBtn.image.color = tag == "Body" ? activeColor : inactiveColor;
    }

    private void HandleItemSelection(string type, int index)
    {
        CustomizationManager.CustomizationItem[] arr = type switch { "Hat" => hats, "Beard" => beards, "Brow" => brows, _ => null };
        if (arr != null)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].model != null) arr[i].model.SetActive(i == index);
            }
        }
        PlayerPrefs.SetInt("Selected" + type, index);

        // --- НОВИЙ КОД: Оновлюємо реального гравця в мережі ---
        if (NetworkClient.localPlayer != null)
        {
            var netPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayerCustomization>();
            if (netPlayer != null) netPlayer.CmdUpdateItem(type, index);
        }
    }

    private void HandleColorSelection(Color c, string type)
    {
        PlayerPrefs.SetString(type + "Color", "#" + ColorUtility.ToHtmlStringRGBA(c));
        ApplyColorByType(type, c);

        // --- НОВИЙ КОД: Оновлюємо реального гравця в мережі ---
        if (NetworkClient.localPlayer != null)
        {
            var netPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayerCustomization>();
            if (netPlayer != null) netPlayer.CmdUpdateColor(type, c);
        }
    }
    private void ApplyColorByType(string type, Color c)
    {
        if (type == "Skin") cust.ApplyColor(cust.bodyRenderer, c);
        if (type == "Pants") cust.ApplyColor(cust.pantsRenderer, c);

        if (type == "HatColor") foreach (var h in hats) cust.ApplyToAllRenderers(h.model, c);
        if (type == "BeardColor")
        {
            foreach (var b in beards) cust.ApplyToAllRenderers(b.model, c);
            if (bobyHairModel != null) cust.ApplyToAllRenderers(bobyHairModel, c);
        }
        if (type == "BrowColor") foreach (var br in brows) cust.ApplyToAllRenderers(br.model, c);
    }

    private void LoadCharacterPrefs()
    {
        string[] cats = { "Hat", "Beard", "Brow" };
        foreach (string cat in cats)
        {
            int val = PlayerPrefs.GetInt("Selected" + cat, -1);
            HandleItemSelection(cat, val);
            cust.HighlightButtonByIndex(cat, val);
        }
        LoadColor("Skin", Color.white);
        LoadColor("Pants", Color.gray);
        LoadColor("HatColor", Color.white);
        LoadColor("BeardColor", Color.black);
        LoadColor("BrowColor", Color.black);
    }

    private void LoadColor(string type, Color defaultCol)
    {
        if (PlayerPrefs.HasKey(type + "Color") && ColorUtility.TryParseHtmlString(PlayerPrefs.GetString(type + "Color"), out Color savedCol))
            ApplyColorByType(type, savedCol);
        else ApplyColorByType(type, defaultCol);
    }

    void Update()
    {
        // Старт гри при натисканні будь-якої клавіші, якщо ми ще не почали
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame && !isGameStarted)
        {GoToStartMenu();}

        // Повернення назад з лоадаута до другого каму, якщо натиснути Esc
        if (isInLoadout && Input.GetKeyDown(KeyCode.Escape)) BackToSecondCam();


        // Ховання кнопки "Host Game" для клієнтів, які приєдналися до лобі (бо вони не можуть бути хостами)
        if (hostGameButton != null)
        {
            // NetworkClient.active повертає true, якщо ми підключені як клієнт АБО як хост
            hostGameButton.interactable = !NetworkClient.active;

            // Або якщо хочеш повністю ховати її, а не робити сірою:
            // hostGameButton.gameObject.SetActive(!NetworkClient.active);
        }
    }
}