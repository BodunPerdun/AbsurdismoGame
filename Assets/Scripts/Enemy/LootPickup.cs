using Mirror;
using UnityEngine;
using UnityEngine.Audio;

public class LootPickup : NetworkBehaviour
{
    [SyncVar]
    private float coinsAmount;

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            GlobalBank.Instance.AddMoney(coinsAmount);
            LootPool.Instance.ReturnLoot(gameObject);
        }
    }

    [Server]
    public void SetValue(float amount)
    {
        coinsAmount = amount;
    } 
}
