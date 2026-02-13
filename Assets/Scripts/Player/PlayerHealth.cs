using Mirror;
using Mirror.Examples.Basic;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar] private float currentHealth;
    [SerializeField] private float maxHealth = 100f;
    public float respawnTime = 5f;
    private bool isDead = false;

    // Посилання на компоненти, які треба вимикати при смерті
    [SerializeField] private CharacterController ch;
    [SerializeField] private Renderer[] renderers; // Призначити в інспекторі або знайти в Awake

    public CinemachineCamera mainCamera;
    public CinemachineCamera thirdPersonCamera;

    void Awake()
    {
        ch = GetComponent<CharacterController>();

        // АВТОМАТИЧНИЙ ПОШУК КОМПОНЕНТІВ
        // Знаходимо всі меші (тіло, зброя, одяг) у цьому об'єкті та дочірніх
        renderers = GetComponentsInChildren<Renderer>();

    }

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

        // Якщо це МІЙ локальний гравець помер -> вмикаємо режим спостерігача
        if (isOwned)
        {
            if (SpectatorManager.Instance != null)
            {
                SpectatorManager.Instance.EnableSpectatorMode();
            }
        }
        
        if (SpectatorManager.Instance != null && thirdPersonCamera != null)
        {
            SpectatorManager.Instance.UnregisterPlayerCamera(thirdPersonCamera);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] RpcOnDeath: Не вдалося знайти камеру для відключення від спостерігача.");
        }
    }

    [Server]
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
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

        // Якщо це МІЙ локальний гравець відродився -> вимикаємо режим спостерігача

        // 1. Повертаємо камеру в список спостереження (бо гравець ожив)
        if (SpectatorManager.Instance != null && thirdPersonCamera != null)
        {
            SpectatorManager.Instance.RegisterPlayersCamera(thirdPersonCamera);
        }

        // 2. Якщо це МІЙ гравець - вимикаю режим спостерігача
        if (isOwned)
        {
            if (SpectatorManager.Instance != null)
            {
                SpectatorManager.Instance.DisableSpectatorMode();
            }

            // Примусово вмикаємо свою камеру
            if (mainCamera)
            {
                mainCamera.gameObject.SetActive(true);
                mainCamera.Priority = 10;
            }
        }
    }

    public bool IsDead => isDead; // Властивість для перевірки з інших скриптів
}