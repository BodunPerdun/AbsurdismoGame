using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using DG.Tweening;

public class MenuManager : MonoBehaviour
{
    [Header("Ссылки")]
    public CustomizationManager cust;
    public CinemachineBrain brain;
    public CinemachineCamera mainCam, secondCam, loudautCam;

    [Header("UI & Animation")]
    public Animator doorAnim;
    public AudioSource audioSource;
    public Text titleText;
    public CanvasGroup custPanel;
    public Button playButton;

    [Header("Данные")]
    public GameObject[] hats, beards, brows;
    public ColorPreset[] skinColors, hairColors;

    private bool isInLoadout;
    private bool isGameStarted = false;

    void Start()
    {
        custPanel.gameObject.SetActive(false);
        custPanel.alpha = 0;

        cust.GenerateUI(hats, beards, brows, skinColors, hairColors, HandleItemSelection, HandleColorSelection);
        
        LoadCharacterPrefs(); // Загружаем то, что было сохранено
    }

    public void PlayButtonHit()
    {
        if (isGameStarted) return;
        isGameStarted = true;

        audioSource?.PlayDelayed(1f);
        doorAnim?.SetTrigger("isStarted");
        if (playButton != null) playButton.interactable = false;

        brain.DefaultBlend.Time = 6f;
        titleText?.DOFade(0, 0.8f).OnComplete(() => titleText.gameObject.SetActive(false));

        SetCameras(0, 10, 0);
    }

    public void GoToLoadout()
    {
        brain.DefaultBlend.Time = 2f;
        SetCameras(0, 0, 20);
        isInLoadout = true;
        DOVirtual.DelayedCall(2f, () => {
            if (isInLoadout) {
                custPanel.gameObject.SetActive(true);
                custPanel.DOFade(1, 0.5f);
            }
        });
    }

    public void BackToSecondCam()
    {
        custPanel.DOFade(0, 0.3f).OnComplete(() => custPanel.gameObject.SetActive(false));
        SetCameras(0, 10, 0);
        isInLoadout = false;
    }

    private void HandleItemSelection(string type, int index)
    {
        if (type == "Hat") ToggleItems(hats, index);
        if (type == "Beard") ToggleItems(beards, index);
        if (type == "Brow") ToggleItems(brows, index);

        PlayerPrefs.SetInt("Selected" + type, index);
        PlayerPrefs.Save();
    }

    private void HandleColorSelection(Color c, bool isSkin)
    {
        string key = isSkin ? "SkinColor" : "HairColor";
        PlayerPrefs.SetString(key, "#" + ColorUtility.ToHtmlStringRGBA(c));
        
        if (isSkin) cust.ApplyColor(cust.bodyRenderer, c);
        else {
            foreach (var b in beards) cust.ApplyColor(b?.GetComponent<Renderer>(), c);
            foreach (var br in brows) cust.ApplyColor(br?.GetComponent<Renderer>(), c);
        }
        PlayerPrefs.Save();
    }

    private void LoadCharacterPrefs()
    {
        int h = PlayerPrefs.GetInt("SelectedHat", -1);
        int b = PlayerPrefs.GetInt("SelectedBeard", -1);
        int br = PlayerPrefs.GetInt("SelectedBrow", -1);
  
        
     

        HandleItemSelection("Hat", h);
        cust.HighlightButtonByIndex("Hat", h);

        HandleItemSelection("Beard", b);
        cust.HighlightButtonByIndex("Beard", b);

        HandleItemSelection("Brow", br);
        cust.HighlightButtonByIndex("Brow", br);

       
    }

    private void ToggleItems(GameObject[] arr, int idx)
    {
        for (int i = 0; i < arr.Length; i++) {
            if (arr[i] != null) arr[i].SetActive(i == idx);
        }
    }

    private void SetCameras(int m, int s, int l)
    {
        mainCam.Priority = m; secondCam.Priority = s; loudautCam.Priority = l;
    }

    void Update() { if (isInLoadout && Input.GetKeyDown(KeyCode.Escape)) BackToSecondCam(); }
}