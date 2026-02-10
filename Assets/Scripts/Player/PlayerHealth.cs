using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar] private float currentHealth;
    [SerializeField] private float maxHealth = 100f;
    private bool isDead = false;

    // Посилання на компоненти, які треба вимикати при смерті
    [SerializeField] private CharacterController ch;
    [SerializeField] private Renderer[] renderers; // Призначити в інспекторі або знайти в Awake

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
    }

    [Server]
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    [Server]
    private void Die()
    {
        isDead = true;
        RpcOnDeath();
        StartCoroutine(RespawnRoutine());
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        // Вимкнути візуал та рух
        if (ch) ch.enabled = false;
        foreach (var r in renderers) r.enabled = false;
    }

    [Server]
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(5f);
        currentHealth = maxHealth;
        isDead = false;

        Transform startPos = NetworkManager.singleton.GetStartPosition();
        RpcOnRespawn(startPos != null ? startPos.position : Vector3.zero);
    }

    [ClientRpc]
    private void RpcOnRespawn(Vector3 pos)
    {
        transform.position = pos;
        if (ch) ch.enabled = true;
        foreach (var r in renderers) r.enabled = true;
    }

    public bool IsDead => isDead; // Властивість для перевірки з інших скриптів
}