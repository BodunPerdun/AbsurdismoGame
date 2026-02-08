using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using DG.Tweening;

public class MenuManager : MonoBehaviour
{
    [Header("Система")]
    public CustomizationManager cust;
    public CinemachineBrain brain;
    public CinemachineCamera mainCam, secondCam, loudautCam;

    [Header("Интерфейс Меню")]
    public Animator doorAnim;
    public AudioSource audioSource;
    public Text titleText;
    public Button playButton;
    public CanvasGroup customizationPanel;

    [Header("Вкладки")]
    public GameObject hatPanel; 
    public GameObject beardPanel; 
    public GameObject browPanel; 
    public GameObject bodyPanel;
    public Button tabHatBtn, tabBeardBtn, tabBrowBtn, tabBodyBtn;

    [Header("Данные Палитры")]
    public GameObject[] hats, beards, brows;
    public ColorPreset[] skinColors;
    public ColorPreset[] commonHairColors; // Единая палитра для головы
    public ColorPreset[] pantsColors;

    private bool isInLoadout;
    private bool isGameStarted = false;

    void Start()
    {
        customizationPanel.alpha = 0;
        customizationPanel.gameObject.SetActive(false);
        
        // Генерация UI
        cust.GenerateUI(hats, beards, brows, skinColors, commonHairColors, pantsColors, HandleItemSelection, HandleColorSelection);

        // Настройка вкладок
        tabHatBtn.onClick.AddListener(() => SwitchTab("Hat"));
        tabBeardBtn.onClick.AddListener(() => SwitchTab("Beard"));
        tabBrowBtn.onClick.AddListener(() => SwitchTab("Brow"));
        tabBodyBtn.onClick.AddListener(() => SwitchTab("Body"));

        LoadCharacterPrefs();
        SwitchTab("Hat");
    }

    public void PlayButtonHit()
    {
        if (isGameStarted) return;
        isGameStarted = true;

        // --- МУЗЫКА ---
        if (audioSource != null) audioSource.PlayDelayed(0.5f);

        // --- АНИМАЦИЯ UI ---
        playButton.interactable = false;
        titleText?.DOFade(0, 0.6f).OnComplete(() => titleText.gameObject.SetActive(false));
        playButton.image.DOFade(0, 0.6f);
        playButton.GetComponentInChildren<Text>()?.DOFade(0, 0.6f).OnComplete(() => playButton.gameObject.SetActive(false));

        doorAnim?.SetTrigger("isStarted");
        
        brain.DefaultBlend.Time = 6f;
        SetCameras(0, 10, 0); 
    }

    private void SwitchTab(string tag)
    {
        hatPanel.SetActive(tag == "Hat"); 
        beardPanel.SetActive(tag == "Beard");
        browPanel.SetActive(tag == "Brow"); 
        bodyPanel.SetActive(tag == "Body");

        // Подсветка кнопок
        tabHatBtn.image.color = tag == "Hat" ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        tabBeardBtn.image.color = tag == "Beard" ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        tabBrowBtn.image.color = tag == "Brow" ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        tabBodyBtn.image.color = tag == "Body" ? Color.white : new Color(0.6f, 0.6f, 0.6f);
    }

    public void GoToLoadout()
    {
        brain.DefaultBlend.Time = 2f;
        SetCameras(0, 0, 20);
        isInLoadout = true;
        DOVirtual.DelayedCall(2f, () => {
            if (isInLoadout) {
                customizationPanel.gameObject.SetActive(true);
                customizationPanel.DOFade(1, 0.5f);
            }
        });
    }

    public void BackToSecondCam()
    {
        customizationPanel.DOFade(0, 0.3f).OnComplete(() => customizationPanel.gameObject.SetActive(false));
        SetCameras(0, 10, 0);
        isInLoadout = false;
    }

    private void HandleItemSelection(string type, int index)
    {
        GameObject[] arr = type switch { "Hat" => hats, "Beard" => beards, "Brow" => brows, _ => null };
        if (arr != null) for (int i = 0; i < arr.Length; i++) if (arr[i]) arr[i].SetActive(i == index);
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
        
        // Красим через поиск во всех дочерних рендерерах
        if (type == "HatColor") foreach (var h in hats) cust.ApplyToAllRenderers(h, c);
        if (type == "BeardColor") foreach (var b in beards) cust.ApplyToAllRenderers(b, c);
        if (type == "BrowColor") foreach (var br in brows) cust.ApplyToAllRenderers(br, c);
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
        LoadColor("HatColor", Color.white); // Белый по умолчанию для текстурных шляп
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

    void Update() { if (isInLoadout && Input.GetKeyDown(KeyCode.Escape)) BackToSecondCam(); }
}