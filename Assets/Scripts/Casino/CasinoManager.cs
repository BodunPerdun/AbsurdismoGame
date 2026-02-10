using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror; // Підключаємо Mirror

[System.Serializable]
public struct SymbolChance
{
    public string name;
    public int symbolIndex;
    [Range(0, 100)] public float chance;
}

public class CasinoManager : NetworkBehaviour
{
    [Header("Посилання")]
    public SlotReel[] reels;
    public Button spinButton;
    public Animator machineAnimator;
    public GameObject hintUI;

    [Header("Звуки")]
    public AudioSource machineAudio;
    public AudioClip startSound;
    public AudioClip winSound;

    [Header("Шанси")]
    public List<SymbolChance> symbolChances = new List<SymbolChance>();

    // SyncVar гарантує, що якщо хтось новий зайде на сервер, 
    // він знатиме, що автомат зайнятий (але для анімації використовуємо Rpc)
    [SyncVar]
    private bool isSpinning = false;

    private bool isLocalPlayerInside = false;

    void Start()
    {
        if (hintUI != null) hintUI.SetActive(false);
        // Кнопку прив'язуємо динамічно
        if (spinButton != null)
        {
            spinButton.onClick.RemoveAllListeners();
            spinButton.onClick.AddListener(OnSpinButtonPressed);
        }
    }

    void Update()
    {
        // Перевіряємо ввід тільки якщо ми локальний гравець біля автомата
        if (isLocalPlayerInside && !isSpinning && Input.GetKeyDown(KeyCode.F))
        {
            OnSpinButtonPressed();
        }
    }

    // --- ВЗАЄМОДІЯ (Локально) ---
    public void OnSpinButtonPressed()
    {
        if (isSpinning) return;

        // Відправляємо команду на сервер
        CmdSpin();
    }

    // --- ТРИГЕРИ ---
    // Це спрацьовує на кожному клієнті окремо
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
            // Перевіряємо, чи це саме НАШ гравець зайшов у зону
            if (netId != null && netId.isLocalPlayer)
            {
                isLocalPlayerInside = true;
                if (hintUI != null && !isSpinning) hintUI.SetActive(true);
                if (spinButton != null) spinButton.interactable = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
            if (netId != null && netId.isLocalPlayer)
            {
                isLocalPlayerInside = false;
                if (hintUI != null) hintUI.SetActive(false);
            }
        }
    }

    // --- ЛОГІКА СЕРВЕРА ---

    // requiresAuthority = false дозволяє викликати це будь-кому
    [Command(requiresAuthority = false)]
    private void CmdSpin()
    {
        if (isSpinning) return;

        isSpinning = true;

        // 1. Генеруємо результати на сервері (захист від чітів)
        int[] results = new int[reels.Length];
        for (int i = 0; i < reels.Length; i++)
        {
            results[i] = GetRandomSymbol();
        }

        // 2. Розсилаємо всім клієнтам команду почати анімацію
        RpcStartSpin(results);

        // 3. Сервер повинен розблокувати автомат через певний час
        // Час = затримка початку + (к-сть барабанів * затримка) + час кручення останнього + запас
        float maxDuration = 0.1f + (reels.Length * 0.2f) + (2.0f + (reels.Length - 1) * 0.4f) + 1.0f;
        StartCoroutine(ServerResetSpinState(maxDuration));
    }

    // --- ЛОГІКА КЛІЄНТІВ (Візуал) ---
    [ClientRpc]
    private void RpcStartSpin(int[] finalResults)
    {
        StartCoroutine(SpinFlow(finalResults));
    }

    IEnumerator SpinFlow(int[] targets)
    {
        // Вимикаємо UI для того, хто стоїть поруч
        if (hintUI != null) hintUI.SetActive(false);
        if (spinButton != null) spinButton.interactable = false;

        if (machineAnimator != null) machineAnimator.SetTrigger("StartSpin");
        if (machineAudio != null && startSound != null) machineAudio.PlayOneShot(startSound);

        yield return new WaitForSeconds(0.1f);

        int completedCount = 0;

        for (int i = 0; i < reels.Length; i++)
        {
            int targetSymbol = targets[i];

            // Запускаємо SlotReel (він залишається звичайним MonoBehaviour)
            StartCoroutine(reels[i].Spin(2.0f + i * 0.4f, targetSymbol, (res) => {
                completedCount++;
            }));

            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitUntil(() => completedCount == reels.Length);

        CheckResultVisuals(targets);

        // Повертаємо інтерфейс тільки локальному гравцю, якщо він все ще тут
        if (isLocalPlayerInside && hintUI != null) hintUI.SetActive(true);
        if (spinButton != null) spinButton.interactable = true;
    }

    IEnumerator ServerResetSpinState(float delay)
    {
        yield return new WaitForSeconds(delay);
        isSpinning = false;
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

    void CheckResultVisuals(int[] finalResults)
    {
        if (finalResults[0] == finalResults[1] && finalResults[1] == finalResults[2])
        {
            if (machineAnimator != null) machineAnimator.SetTrigger("WinJackpot");
            if (machineAudio != null && winSound != null) machineAudio.PlayOneShot(winSound);
        }
    }
}