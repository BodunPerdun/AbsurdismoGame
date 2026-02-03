using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [Range(0, 100)]
    public float chance;
}

public class CasinoManager : MonoBehaviour
{
    [Header("Компоненты")]
    public SlotReel[] reels; 
    public Button spinButton;
    public Animator machineAnimator; // Ссылка на аниматор корпуса

    [Header("Звуки автомата")]
    public AudioSource machineAudio; 
    public AudioClip startSound;     
    public AudioClip winSound;       

    [Header("Настройка шансов (%)")]
    public List<SymbolChance> symbolChances = new List<SymbolChance>();

    private int[] results = new int[3];
    private int completedReels = 0;

    void Start()
    {
        if (spinButton != null) spinButton.onClick.AddListener(StartSpinning);
        if (machineAudio == null) machineAudio = GetComponent<AudioSource>();
        if (machineAnimator == null) machineAnimator = GetComponent<Animator>();
    }

    void StartSpinning()
    {
        // 1. ЗАПУСКАЕМ АНИМАЦИЮ СТАРТА
        if (machineAnimator != null)
        {
            machineAnimator.SetTrigger("StartSpin");
        }

        // 2. ЗВУК СТАРТА
        if (machineAudio != null && startSound != null)
            machineAudio.PlayOneShot(startSound);

        StartCoroutine(SpinFlow());
    }

    IEnumerator SpinFlow()
    {
        spinButton.interactable = false;
        completedReels = 0;

        yield return new WaitForSeconds(0.2f); 

        for (int i = 0; i < reels.Length; i++)
        {
            int targetSymbol = GetRandomSymbol();
            int reelIndex = i;

            StartCoroutine(reels[i].Spin(2.5f + i * 0.5f, targetSymbol, (res) => {
                results[reelIndex] = res;
                completedReels++;
            }));

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitUntil(() => completedReels == reels.Length);
        CheckResult();
    }

    void CheckResult()
    {
        spinButton.interactable = true;

        // Проверка на три шестерки (индекс 4) или любой другой джекпот
        if (results[0] == results[1] && results[1] == results[2])
        {
            // ЗАПУСКАЕМ АНИМАЦИЮ ВЫИГРЫША
            if (machineAnimator != null)
            {
                machineAnimator.SetTrigger("WinJackpot");
            }

            if (machineAudio != null && winSound != null)
                machineAudio.PlayOneShot(winSound);

            if (results[0] == 4) TriggerDevilEvent();
        }
    }

    int GetRandomSymbol()
    {
        float totalWeight = symbolChances.Sum(s => s.chance);
        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var symbol in symbolChances)
        {
            currentWeight += symbol.chance;
            if (randomValue <= currentWeight) return symbol.symbolIndex;
        }
        return 0;
    }

    void TriggerDevilEvent()
    {
        Debug.Log("666: АДСКИЙ ДЖЕКПОТ!");
        RenderSettings.ambientLight = Color.red;
    }
}