using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Система")]
    public CustomizationManager cust;
    public CinemachineBrain brain;
    public CinemachineCamera mainCam, secondCam, loudautCam;

    [Header("Интерфейс Меню")]
    public Animator doorAnim;
    public AudioSource musicSource; 
    public Text titleText;
    //public Button playButton;
    public CanvasGroup customizationPanel;
    public CanvasGroup menuButtons; // Панель с кнопками "Loadout", "Start Game" и т.д.

    [Header("Вкладки")]
    public GameObject hatPanel; 
    public GameObject beardPanel; 
    public GameObject browPanel; 
    public GameObject bodyPanel;
    public Button tabHatBtn, tabBeardBtn, tabBrowBtn, tabBodyBtn;

    [Header("Данные Предметов")]
    public CustomizationManager.CustomizationItem[] hats;
    public CustomizationManager.CustomizationItem[] beards;
    public CustomizationManager.CustomizationItem[] brows;

    [Header("Данные Палитры")]
    public ColorPreset[] skinColors;
    public ColorPreset[] commonHairColors;
    public ColorPreset[] pantsColors;

    private bool isInLoadout;
    // змінна для переходу до меню на початку гри
    private bool isGameStarted = false;

    void Start()
    {
        // Инициализация UI
        customizationPanel.alpha = 0;
        customizationPanel.gameObject.SetActive(false);
        
        menuButtons.alpha = 0;
        menuButtons.gameObject.SetActive(false);
        
        cust.GenerateUI(hats, beards, brows, skinColors, commonHairColors, pantsColors, HandleItemSelection, HandleColorSelection);

        tabHatBtn.onClick.AddListener(() => SwitchTab("Hat"));
        tabBeardBtn.onClick.AddListener(() => SwitchTab("Beard"));
        tabBrowBtn.onClick.AddListener(() => SwitchTab("Brow"));
        tabBodyBtn.onClick.AddListener(() => SwitchTab("Body"));

        LoadCharacterPrefs();
        SwitchTab("Hat");
    }

    public void GoToStartMenu()
    {
        if (isGameStarted) return;
        isGameStarted = true;
        
        if (musicSource != null) musicSource.PlayDelayed(0.5f);
        
        //playButton.interactable = false;
        
        // Исчезновение надписей начального экрана
        titleText?.DOFade(0, 0.6f).OnComplete(() => titleText.gameObject.SetActive(false));
        //playButton.image.DOFade(0, 0.6f);
        //playButton.GetComponentInChildren<Text>()?.DOFade(0, 0.6f).OnComplete(() => playButton.gameObject.SetActive(false));
        
        doorAnim?.SetTrigger("isStarted");
        
        // Камера едет к персонажу (SecondCam)
        brain.DefaultBlend.Time = 6f;
        SetCameras(0, 10, 0);

        // Появление кнопок меню (Loadout и т.д.) после того как камера подъедет
        DOVirtual.DelayedCall(6f, () => {
            menuButtons.gameObject.SetActive(true);
            menuButtons.DOFade(1, 1.5f);
        });
    }

    public void GoToLoadout()
    {
        // 1. Прячем кнопки меню
        menuButtons.DOFade(0, 0.3f).OnComplete(() => menuButtons.gameObject.SetActive(false));

        // 2. Переключаем камеру
        brain.DefaultBlend.Time = 2f;
        SetCameras(0, 0, 20);
        isInLoadout = true;

        // 3. Показываем панель кастомизации
        DOVirtual.DelayedCall(1.5f, () => {
            if (isInLoadout) {
                customizationPanel.gameObject.SetActive(true);
                customizationPanel.DOFade(1, 0.5f);
            }
        });
    }

    public void BackToSecondCam()
    {
        // 1. Прячем панель кастомизации
        customizationPanel.DOFade(0, 0.3f).OnComplete(() => customizationPanel.gameObject.SetActive(false));

        // 2. Возвращаем камеру
        SetCameras(0, 10, 0);
        isInLoadout = false;

        // 3. Возвращаем кнопки меню
        DOVirtual.DelayedCall(1.5f, () => {
            if (!isInLoadout) {
                menuButtons.gameObject.SetActive(true);
                menuButtons.DOFade(1, 0.5f);
            }
        });
    }

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

    // --- Остальные методы (HandleItem, HandleColor, LoadPrefs) остаются без изменений ---
    
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
    }

    private void HandleColorSelection(Color c, string type)
    {
        PlayerPrefs.SetString(type + "Color", "#" + ColorUtility.ToHtmlStringRGBA(c));
        ApplyColorByType(type, c);
    }

    private void ApplyColorByType(string type, Color c)
    {
        if (type == "Skin") cust.ApplyColor(cust.bodyRenderer, c);
        if (type == "Pants") cust.ApplyColor(cust.pantsRenderer, c);
        
        if (type == "HatColor") foreach (var h in hats) cust.ApplyToAllRenderers(h.model, c);
        if (type == "BeardColor") foreach (var b in beards) cust.ApplyToAllRenderers(b.model, c);
        if (type == "BrowColor") foreach (var br in brows) cust.ApplyToAllRenderers(br.model, c);
    }

    private void LoadCharacterPrefs()
    {
        string[] cats = { "Hat", "Beard", "Brow" };
        foreach (string cat in cats) {
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

    private void SetCameras(int m, int s, int l) { mainCam.Priority = m; secondCam.Priority = s; loudautCam.Priority = l; }

    void Update() 
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame && !isGameStarted)
        {
            GoToStartMenu();
            Debug.Log("Була натиснута якась клавіша на клавіатурі!");
        }

        if (isInLoadout && Input.GetKeyDown(KeyCode.Escape)) BackToSecondCam(); 
    }
}