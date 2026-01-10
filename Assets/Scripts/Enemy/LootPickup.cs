using UnityEngine;
using Mirror;

public class LootPickup : NetworkBehaviour
{
    [SyncVar]
    private float coinsAmount;

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.AddCoins(coinsAmount);
                LootPool.Instance.ReturnLoot(gameObject);
            }
        }
    }

    [Server]
    public void SetValue(float amount)
    {
        coinsAmount = amount;
    } 
}
