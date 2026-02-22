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
    [SerializeField] private NetworkTransformReliable networkTransform;

    public CinemachineCamera mainCamera;
    public CinemachineCamera thirdPersonCamera;

    // Для ragdoll при смерті буде гравець падати
    private PhysicalBodyPart[] _bodyParts;
    [SerializeField] private Rigidbody _hipsRigidbody;
    [SerializeField] private ConfigurableJoint _hipsConfigurableJoin;
    [SerializeField] private Animator animator;

    void Awake()
    {
        ch = GetComponent<CharacterController>();

        // Знаходимо NetworkTransform, якщо не задано в інспекторі
        if (networkTransform == null) networkTransform = GetComponent<NetworkTransformReliable>();

        // Ініціалізуємо список Rigidbody для ragdoll та анімації
        _bodyParts = GetComponentsInChildren<PhysicalBodyPart>();
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
        // Вмикаємо/вимикаємо аніматор (коли активний ragdoll - аніматор вимкнений)
        if (animator) animator.enabled = !active;

        // Вмикаємо/вимикаємо CharacterController
        if (ch) ch.enabled = !active;

        // Вимикаємо NetworkTransform, щоб він не заважав фізиці падати
        if (networkTransform) networkTransform.enabled = !active;

        _hipsRigidbody.isKinematic = !active; // Хіпс має бути кінематичним, щоб не провалюватися крізь землю при активації ragdoll
        
        // Отримуємо копію поточних налаштувань суглоба
        JointDrive drive = _hipsConfigurableJoin.slerpDrive;

        // Змінюємо значення в нашій копії
        ConfigurableJointMotion targetMotion = active ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;

        _hipsConfigurableJoin.xMotion = targetMotion;
        _hipsConfigurableJoin.yMotion = targetMotion;
        _hipsConfigurableJoin.zMotion = targetMotion;

        drive.positionSpring = active ? 1000 : 100000;
        drive.positionDamper = active ? 0 : 10000;

        // Повертаємо оновлену структуру назад у ConfigurableJoint
        _hipsConfigurableJoin.slerpDrive = drive;
    }
    //========================================================================================

    [ClientRpc]
    private void RpcOnDeath()
    {
        isDead = true;
 
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

        isDead = false;

        SetRagdollState(isDead);

        foreach (var part in _bodyParts)
        {
            part.ResetPose();
        }

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