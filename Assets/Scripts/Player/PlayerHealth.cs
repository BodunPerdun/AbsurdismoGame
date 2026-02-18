using Mirror;
using Mirror.Examples.Basic;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private NetworkTransformReliable networkTransform;

    public CinemachineCamera mainCamera;
    public CinemachineCamera thirdPersonCamera;

    // Для ragdoll при смерті буде гравець падати
    private List<Rigidbody> _rigidbodies;
    private Animator animator;

    void Awake()
    {
        ch = GetComponent<CharacterController>();

        // Знаходимо NetworkTransform, якщо не задано в інспекторі
        if (networkTransform == null) networkTransform = GetComponent<NetworkTransformReliable>();

        // АВТОМАТИЧНИЙ ПОШУК КОМПОНЕНТІВ
        // Знаходимо всі меші (тіло, зброя, одяг) у цьому об'єкті та дочірніх
        renderers = GetComponentsInChildren<Renderer>();

        // Ініціалізуємо список Rigidbody для ragdoll та анімації
        _rigidbodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
        animator = GetComponent<Animator>();
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


    private void SetRagdollState(bool active)
    {
        // 1. Вмикаємо/вимикаємо аніматор (коли активний ragdoll - аніматор вимкнений)
        if (animator) animator.enabled = !active;

        // 2. Вмикаємо/вимикаємо CharacterController
        if (ch) ch.enabled = !active;

        // 3. ВАЖЛИВО: Вимикаємо NetworkTransform, щоб він не заважав фізиці падати
        if (networkTransform) networkTransform.enabled = !active;

        // 4. Налаштовуємо фізику кісток
        foreach (var rb in _rigidbodies)
        {
            rb.isKinematic = !active; // Якщо ragdoll активний, кінематика вимкнена (падає)
        }

        //foreach (var col in _colliders)
        //{
        //    col.enabled = active; // Вмикаємо коллайдери кісток при смерті
        //}
    }
    //========================================================================================

    [ClientRpc]
    private void RpcOnDeath()
    {
        isDead = true;
        /*
        // Вимкнути візуал та рух
        if (ch) ch.enabled = false;
        foreach (var r in renderers) r.enabled = false;
        */

        // Вимикаємо коллайдери та рендерери, вмикаємо фізику для ragdoll
        SetRagdollState(isDead);

        // Якщо це МІЙ локальний гравець помер -> вмикаємо режим спостерігача
        if (isOwned)
        {
            // Примусово вимикаємо СВОЮ головну камеру
            if (mainCamera)
            {
                mainCamera.gameObject.SetActive(false);
                mainCamera.Priority = 0;
            }

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
        /*
            if (ch) ch.enabled = true;
            foreach (var r in renderers) r.enabled = true;
        */
        isDead = false;

        SetRagdollState(isDead);


        // 1. Спочатку реєструємо камеру для інших спостерігачів.
        // Завдяки фіксу в SpectatorManager вона додасться ВИМКНЕНОЮ.
        if (SpectatorManager.Instance != null && thirdPersonCamera != null)
        {
            SpectatorManager.Instance.RegisterPlayersCamera(thirdPersonCamera);
        }

        // 2. Логіка для локального гравця (МЕНЕ)
        if (isOwned)
        {
            // Якщо ми були в режимі спостерігача — виходимо
            if (SpectatorManager.Instance != null)
            {
                SpectatorManager.Instance.DisableSpectatorMode();
            }

            // Примусово вмикаємо СВОЮ головну камеру
            if (mainCamera)
            {
                mainCamera.gameObject.SetActive(true);
                mainCamera.Priority = 100;
            }

            // ВАЖЛИВО: Переконуємось, що наша власна камера для спостереження (thirdPerson)
            // не активна для нас самих, щоб не перебивати MainCamera.
            if (thirdPersonCamera)
            {
                thirdPersonCamera.Priority = 0;
                thirdPersonCamera.gameObject.SetActive(false);
            }
        }
    }

    public bool IsDead => isDead; // Властивість для перевірки з інших скриптів
}