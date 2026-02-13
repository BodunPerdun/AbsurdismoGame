using UnityEngine;
using Mirror;

public class GlobalBank : NetworkBehaviour
{
    public static GlobalBank Instance;

    [SyncVar(hook = nameof(OnGlobalMoneyChanged))]
    private float globalMoney = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Server]
    public void AddMoney(float amount)
    {
        globalMoney += amount;
        Debug.Log($"[GlobalBank] Додано: {amount}. Всього: {globalMoney}");
    }

    // Хук для оновлення UI у всіх гравців, коли міняється сума
    void OnGlobalMoneyChanged(float oldMoney, float newMoney)
    {
        // Тут виклик оновлення тексту на екрані, наприклад:
        // UIManager.Instance.UpdateGlobalMoneyText(newMoney);
    }
}