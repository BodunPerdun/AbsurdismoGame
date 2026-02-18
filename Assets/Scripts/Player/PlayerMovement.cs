using Mirror;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerHealth))]
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController ch;
    private PlayerHealth health;
    private Animator animator;

    // Прибрали MouseRotation, бо він для top-down
    public PlayerControls Controls { get; private set; }

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2.5f; // Висота стрибка
    public float gravityMultiplier = 1.0f; // Щоб налаштувати "важкість" падіння

    [Header("First Person Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraRoot;
    private float xRotation = 0f;
    public GameObject playerCameraObject;
    public CinemachineCamera thirdPersonCamera;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    private bool isDashing = false;
    public TrailRenderer trail1;
    public TrailRenderer trail2;

    [Header("Audio")]
    public AudioClip dashSound;
    public AudioSource audioSource;

    // Змінна для вертикальної швидкості (стрибки/гравітація)
    private float _verticalVelocity;

    void Awake()
    {
        ch = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        Controls = new PlayerControls();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Реєструємо себе в менеджері спостерігача (навіть якщо це чужий гравець)
        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.RegisterPlayersCamera(thirdPersonCamera);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        // Видаляємо себе при виході
        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.UnregisterPlayerCamera(thirdPersonCamera);
        }
    }

    //public override void OnStartLocalPlayer()
    //{
    //    // 1. Вмикаємо камеру ТІЛЬКИ якщо це наш гравець
    //    if (playerCameraObject != null)
    //    {
    //        playerCameraObject.SetActive(true);
    //    }

    //    // Курсор
    //    Cursor.lockState = CursorLockMode.Locked;
    //    Cursor.visible = false;

    //    // Реєструємося в менеджері
    //    if (CameraManager.Instance != null && playerCameraObject != null)
    //    {
    //        CameraManager.Instance.RegisterCamera(playerCameraObject.GetComponent<CinemachineCamera>());
    //    }
    //}

    //void Start()
    //{
    //    // Для безпеки: якщо це НЕ мій гравець, вимикаємо камеру примусово
    //    if (!isOwned && playerCameraObject != null)
    //    {
    //        playerCameraObject.SetActive(false);
    //    }
    //}

    void OnEnable() => Controls.Player.Enable();
    void OnDisable() => Controls.Player.Disable();

    void Update()
    {
        if (!isOwned || health.IsDead) return;

        HandleRotation();
        HandleMovementAndJump();
        HandleShooting();
    }

    void HandleRotation()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime * 5f;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime * 5f;

        // Обертаємо тіло гравця по горизонталі
        transform.Rotate(Vector3.up * mouseX);

        // Обертаємо камеру по вертикалі
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraRoot)
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleMovementAndJump()
    {
        if (isDashing) return;

        // 1. Земля та гравітація
        bool isGrounded = ch.isGrounded;

        // Якщо ми на землі, скидаємо вертикальну швидкість (залишаємо маленький мінус, щоб "притискало")
        if (isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = -2f;
        }

        // 2. Отримання вводу руху
        Vector2 input = Controls.Player.Move.ReadValue<Vector2>();

        // Рух завжди відносно погляду (FPS)
        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;

        // 3. Логіка стрибка
        // Переконайтесь, що в Input System є Action "Jump" (Space)
        if (Controls.Player.Jump.WasPressedThisFrame() && isGrounded)
        {
            // Формула фізичного стрибка: v = sqrt(h * -2 * g)
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y * gravityMultiplier);
        }

        // 4. Застосування гравітації до вертикальної швидкості
        _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

        // 5. Фінальне переміщення
        // Горизонтальна швидкість + Вертикальна швидкість
        Vector3 finalMove = moveDir * moveSpeed;
        finalMove.y = _verticalVelocity;

        ch.Move(finalMove * Time.deltaTime);

        // Анімація
        bool isMoving = input.sqrMagnitude > 0.01f;
        animator.SetBool("isMoving", isMoving);

        // Dash
        if (Controls.Player.Dash.WasPressedThisFrame() && isMoving)
        {
            StartCoroutine(PerformDashLocally(moveDir.normalized)); // Деш туди, куди дивимось/йдемо
            CmdDash();
        }
    }

    void HandleShooting()
    {
        var weapons = GetComponent<WeaponsSwitching>();

        if (weapons && Controls.Player.Fire.WasPressedThisFrame())
        {
            // 1. Знаходимо точку, куди дивиться гравець
            Ray ray = new Ray(cameraRoot.position, cameraRoot.forward);
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(100f);
            }

            // DEBUG: Малюємо лінію погляду (Зелена)
            Debug.DrawLine(cameraRoot.position, targetPoint, Color.green, 2f);

            // 2. Передаємо в зброю ТОЧКУ, а не напрямок. 
            // Нехай зброя сама вирішує, як туди стріляти від дула.
            weapons.FireAtTarget(targetPoint);
        }
    }

    // --- DASH (Локальний) ---
    IEnumerator PerformDashLocally(Vector3 dir)
    {
        isDashing = true;
        animator.SetBool("isDashing", true);
        ToggleTrails(true);
        if (audioSource && dashSound) audioSource.PlayOneShot(dashSound);

        Vector3 dashDirection = dir;
        // Якщо стоїмо на місці - деш вперед
        if (dashDirection.magnitude < 0.1f) dashDirection = transform.forward;

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            if (health.IsDead) break;
            // Під час деша ігноруємо гравітацію (або можна додати _verticalVelocity, якщо хочете падати в деші)
            ch.Move(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        ToggleTrails(false);
        animator.SetBool("isDashing", false);
        isDashing = false;
    }

    // --- МЕРЕЖА ---
    [Command] void CmdDash() { RpcDashEffects(); }
    [ClientRpc] void RpcDashEffects() { if (!isOwned) StartCoroutine(ShowDashEffectsRoutine()); }

    IEnumerator ShowDashEffectsRoutine()
    {
        animator.SetBool("isDashing", true);
        if (audioSource && dashSound) audioSource.PlayOneShot(dashSound);
        ToggleTrails(true);
        yield return new WaitForSeconds(dashDuration);
        ToggleTrails(false);
        animator.SetBool("isDashing", false);
    }

    void ToggleTrails(bool state)
    {
        if (trail1) trail1.emitting = state;
        if (trail2) trail2.emitting = state;
    }

    // --- Чисто для камера/спостерігача, щоб знати, що цей гравець помер ---
    public CinemachineCamera GetPlayerCamera() { return thirdPersonCamera; }
}