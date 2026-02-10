using UnityEngine;
using Mirror;

public class PlayerWallet : NetworkBehaviour
{
    [SyncVar]
    private int coins = 0;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip lootPickupSound;

    [Server]
    public void AddCoins(int amount)
    {
        coins += amount;
        TargetPlaySound();

        Debug.Log($"[PlayerWallet] Додано: {amount}. Всього: {coins}");
    }

    [Server]
    public bool TrySpendCoins(int amount)
    {
        if (coins >= amount)
        {
            coins -= amount;

            Debug.Log($"[PlayerWallet] Знято: {amount}. Всього: {coins}");
            return true;
        }
        return false;
    }

    [TargetRpc]
    private void TargetPlaySound()
    {
        if (audioSource != null && lootPickupSound != null)
        {

            audioSource.pitch = Random.Range(0.9f, 1.1f);
            float randomVolume = Random.Range(0.8f, 1.0f);

            audioSource.PlayOneShot(lootPickupSound, randomVolume);
        }
    }
}