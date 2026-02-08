using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public struct SymbolChance
{
    public string name;
    public int symbolIndex;
    [Range(0, 100)] public float chance;
}

public class CasinoManager : MonoBehaviour
{
    [Header("Ссылки")]
    public SlotReel[] reels; 
    public Button spinButton;
    public Animator machineAnimator;
    public GameObject hintUI; // UI текст "Нажми F"

    [Header("Звуки")]
    public AudioSource machineAudio; 
    public AudioClip startSound;     
    public AudioClip winSound;       

    [Header("Шансы")]
    public List<SymbolChance> symbolChances = new List<SymbolChance>();

    private int[] results = new int[3];
    private int completedReels = 0;
    private bool isPlayerInside = false;
    private bool isSpinning = false;

    void Start()
    {
        if (spinButton != null) spinButton.onClick.AddListener(StartSpinning);
        if (hintUI != null) hintUI.SetActive(false);
    }

    void Update()
    {
        // Проверка нажатия F
        if (isPlayerInside && !isSpinning && Input.GetKeyDown(KeyCode.F))
        {
            StartSpinning();
        }
    }

    // --- ТРИГГЕРЫ ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (hintUI != null && !isSpinning) hintUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (hintUI != null) hintUI.SetActive(false);
        }
    }

    // --- ИГРОВАЯ ЛОГИКА ---
    public void StartSpinning()
    {
        if (isSpinning) return;

        isSpinning = true;
        if (hintUI != null) hintUI.SetActive(false);
        if (spinButton != null) spinButton.interactable = false;

        if (machineAnimator != null) machineAnimator.SetTrigger("StartSpin");
        if (machineAudio != null && startSound != null) machineAudio.PlayOneShot(startSound);

        StartCoroutine(SpinFlow());
    }

    IEnumerator SpinFlow()
    {
        completedReels = 0;
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < reels.Length; i++)
        {
            int target = GetRandomSymbol();
            int index = i;

            StartCoroutine(reels[i].Spin(2.0f + i * 0.4f, target, (res) => {
                results[index] = res;
                completedReels++;
            }));
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitUntil(() => completedReels == reels.Length);
        
        CheckResult();
        
        isSpinning = false;
        if (isPlayerInside && hintUI != null) hintUI.SetActive(true);
        if (spinButton != null) spinButton.interactable = true;
    }

    int GetRandomSymbol()
    {
        float total = symbolChances.Sum(s => s.chance);
        float rand = Random.Range(0, total);
        float current = 0;
        foreach (var s in symbolChances)
        {
            current += s.chance;
            if (rand <= current) return s.symbolIndex;
        }
        return 0;
    }

    void CheckResult()
    {
        if (results[0] == results[1] && results[1] == results[2])
        {
            if (machineAnimator != null) machineAnimator.SetTrigger("WinJackpot");
            if (machineAudio != null && winSound != null) machineAudio.PlayOneShot(winSound);
        }
    }
}