using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    private Animator animator;
    public float move_speed;
    public float rotation_speed;

    private CharacterController ch;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private float acceleration = 100f;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SyncVar]
    private float currentHealth = 100f;
    private bool isDead = false;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    private bool isDashing = false;
    public TrailRenderer trail1;
    public TrailRenderer trail2;

    [Header("Main Camera Settings")]
    public string mainCameraTag = "FollowCamera"; // Тег основної камери
    private CinemachineCamera _targetCamera;
    public float forwardOffset = 5f;
    public float backwardOffset = 0f;
    private CinemachineRotationComposer _rotationComposer;

    [Header("Second Camera Settings")]
    public string switchCameraTag = "SwitchCamera"; // Тег другої камери (додайте цей тег в Unity)
    [SerializeField] private CinemachineCamera _checkBaseCamera; // Можна залишити пустим, знайде 

    private PlayerControls controls;

    private bool isPlayerCamera = true; // true = Main, false = Second

    public AudioSource audioSource;
    public AudioClip dashSound;

    // ================
    [SyncVar]
    private float coins = 0f;
    //=================

    void Awake()
    {
        // Створюємо екземпляр керування при появі об'єкта
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        // Вмикаємо керування
        controls.Player.Enable();
    }

    void OnDisable()
    {
        // Вимикаємо керування
        controls.Player.Disable();
    }

    void Start()
    {
        // У Mirror краще ініціалізувати компоненти тут, а логіку мережі в OnStartLocalPlayer
        ch = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if(isServer)
        {
            currentHealth = this.maxHealth;
        }
    }

    // Викликається тільки для локального гравця
    public override void OnStartLocalPlayer()
    {
        // 1. Налаштування ОСНОВНОЇ камери
        if (_targetCamera == null)
        {
            GameObject mainCamObj = GameObject.FindGameObjectWithTag(mainCameraTag);
            if (mainCamObj != null)
            {
                _targetCamera = mainCamObj.GetComponent<CinemachineCamera>();
                if (_targetCamera != null)
                {
                    _targetCamera.Follow = transform;
                    _targetCamera.LookAt = transform;
                    _rotationComposer = _targetCamera.GetComponent<CinemachineRotationComposer>();

                    // Активуємо основну камеру при старті
                    CameraManager.Instance.SwitchToCamera(_targetCamera);
                }
            }
            else
            {
                Debug.LogError($"Основну камеру з тегом '{mainCameraTag}' не знайдено!");
            }
        }

        // 2. Налаштування ДРУГОЇ камери (пошук за тегом)
        if (_checkBaseCamera == null)
        {
            GameObject switchCamObj = GameObject.FindGameObjectWithTag(switchCameraTag);
            if (switchCamObj != null)
            {
                _checkBaseCamera = switchCamObj.GetComponent<CinemachineCamera>();
                // Опціонально: Якщо друга камера теж має слідкувати за гравцем, розкоментуйте:
                // _checkBaseCamera.Follow = transform;
                // _checkBaseCamera.LookAt = transform;
            }
            else
            {
                // Це не помилка, можливо другої камери просто немає на рівні
                Debug.LogWarning($"Другу камеру з тегом '{switchCameraTag}' не знайдено.");
            }
        }
    }

    void LateUpdate()
    {
        if (!isOwned || isDead) return;

        // --- ЛОГІКА ПЕРЕМИКАННЯ КАМЕРИ ---
        // WasPressedThisFrame() замінює Input.GetKeyDown()
        if (controls.Player.SwitchCamera.WasPressedThisFrame())
        {
            SwitchToCamera();
        }

        // Читаємо вектор руху (x, y) одразу. Це замінює Input.GetAxis або перевірку WASD
        Vector2 inputVector = controls.Player.Move.ReadValue<Vector2>();

        // Керування офсетом
        if (isPlayerCamera)
        {
            // Перевіряємо напрямок по Y (W = >0, S = <0)
            bool pressingForward = inputVector.y > 0.1f;
            bool pressingBack = inputVector.y < -0.1f;
            cameraTargetTracking(pressingForward, pressingBack);
        }

        // --- РУХ ПЕРСОНАЖА ---
        HandleMovement(inputVector);
    }

    // Виніс рух в окремий метод для чистоти коду
    void HandleMovement(Vector2 input)
    {
        if(isDead) return;

        // Конвертуємо 2D інпут (X, Y) у 3D вектор руху (X, 0, Z)
        Vector3 rawInput = new Vector3(input.x, 0, input.y);

        // Перевіряємо, чи рухаємось ми (magnitude > 0)
        // Нова система сама обробляє протилежні клавіші (A+D дасть 0), тому ручні перевірки не треба
        bool isMoving = rawInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            // Перевірка на Dash (заміна Input.GetKeyDown(KeyCode.LeftShift))
            if (controls.Player.Dash.WasPressedThisFrame()) Dash();

            if (isDashing) return;
        }

        animator.SetBool("isMoving", isMoving);
        Vector3 desiredDirection = rawInput.normalized;

        if (isMoving)
        {
            targetVelocity = desiredDirection * move_speed;

            // Поворот персонажа
            if (desiredDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotation_speed * Time.deltaTime);
            }

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

            animator.SetFloat("SpeedInput", currentVelocity.magnitude);
            ch.Move(currentVelocity * Time.deltaTime);
        }
        else
        {
            targetVelocity = Vector3.zero;
            // Якщо треба плавно зупинятися, лишаємо Lerp, якщо миттєво - просто скидаємо
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, acceleration * Time.deltaTime);
        }
    }

    public bool GetPlayerIsDead()
    {
        return isDead;
    }

    void Dash()
    {
        if (isDashing) return;
        Vector3 dashDirection = transform.forward;
        if (audioSource && dashSound) audioSource.PlayOneShot(dashSound);
        StartCoroutine(PerformDash(dashDirection));
    }

    IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        animator.SetBool("isDashing", true);

        if (trail1) trail1.emitting = true;
        if (trail2) trail2.emitting = true;

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            if(isDead) break;

            ch.Move(direction * move_speed * Time.deltaTime * 3);
            yield return null;
        }

        isDashing = false;
        if (trail1) trail1.emitting = false;
        if (trail2) trail2.emitting = false;
        animator.SetBool("isDashing", false);
    }

    void cameraTargetTracking(bool isForward, bool isBackward)
    {
        // Перевірка на null обов'язкова, бо компонент може бути відсутній
        if (_rotationComposer == null) return;

        if (isForward)
        {
            _rotationComposer.TargetOffset.y = forwardOffset;
            _rotationComposer.TargetOffset.x = 0f;
        }
        else if (isBackward) // Використовуємо else if
        {
            _rotationComposer.TargetOffset.y = backwardOffset;
            _rotationComposer.TargetOffset.x = 0f;
        }
    }

    public void SwitchToCamera()
    {
        // Якщо другої камери немає - нічого не робимо
        if (_checkBaseCamera == null) return;

        // Перемикаємо логічний прапорець
        isPlayerCamera = !isPlayerCamera;

        if (isPlayerCamera)
        {
            // Вмикаємо основну камеру
            CameraManager.Instance.SwitchToCamera(_targetCamera);
        }
        else
        {
            // Вмикаємо другу камеру
            CameraManager.Instance.SwitchToCamera(_checkBaseCamera);
        }
    }

    public float GetHealth()
    {
        return maxHealth;
    }

    public void SetHealth(float health)
    {
        maxHealth = health;
    }

    // --- ЛОГІКА ЗДОРОВ'Я ТА СМЕРТІ (SERVER SIDE) ---
    [Server]
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount; 
        Debug.Log($"Player took damage. HP: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [Server]
    private void Die()
    {
        isDead = true;
        Debug.Log("Player has died.");
        // Тут можна додати логіку респавну або інші дії при смерті гравця
        RpcOnDeath();

        StartCoroutine(RespawnRountime());
    }

    [Server]
    private IEnumerator RespawnRountime()
    {
        for(int i = 5; i > 0; i--)
        {
            Debug.Log($"Respawning in {i} seconds...");
            yield return new WaitForSeconds(1f);
        }

        Respawn();
    }

    [Server]
    private void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;

        Transform startPos = NetworkManager.singleton.GetStartPosition();
        Vector3 spawnPoint = startPos != null ? startPos.position : Vector3.zero;

        RpcOnRespawn(spawnPoint);

        transform.position = spawnPoint;

        Debug.Log("Player has respawned.");
    }

    [ClientRpc]
    void RpcOnDeath()
    {
        isDead = true;

        //// Локальна логіка смерті (анімації, ефекти тощо)
        //if (isOwned)
        //{
        //    animator.SetTrigger("Die");
        //}

        // Краще вимкнути візуал:
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.enabled = false;
        }

        // Вимикаємо колізію, щоб вороги не стріляли в труп
        if (ch != null) ch.enabled = false;
    }

    [ClientRpc]
    void RpcOnRespawn(Vector3 spawnPoint)
    {
        //// Локальна логіка респавну (анімації, ефекти тощо)
        //if (isOwned)
        //{
        //    animator.SetTrigger("Respawn");
        //}
        // Вмикаємо колізію назад
        if (ch != null) ch.enabled = false;
        // Переміщаємо гравця на точку спавну
        transform.position = spawnPoint;
        if(ch != null) ch.enabled = true;

        isDead = false;

        // Вмикаємо візуал назад
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.enabled = true;
        }
    }

    [Server]
    public void AddCoins(float amount)
    {
        coins += amount;
        Debug.Log($"Гравець отримав {amount} монет. Баланс: {coins}");
    }
}