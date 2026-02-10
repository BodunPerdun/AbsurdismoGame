using Mirror;
using Unity.Cinemachine;
using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

// Обов'язково вимагаємо наявність Health, бо ми перевіряємо if(health.IsDead)
[RequireComponent(typeof(CharacterController), typeof(PlayerHealth))]
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController ch;
    private PlayerHealth health; // Посилання на наш новий скрипт
    private PlayerControls controls;
    private Animator animator;

    [Header("Stats")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Main Camera Settings")]
    public string mainCameraTag = "FollowCamera"; // Тег основної камери
    private CinemachineCamera _targetCamera;
    public float forwardOffset = 5f;
    public float backwardOffset = 0f;
    private CinemachineRotationComposer _rotationComposer;

    [Header("Second Camera Settings")]
    public string switchCameraTag = "SwitchCamera"; // Тег другої камери (додайте цей тег в Unity)
    [SerializeField] private CinemachineCamera _checkBaseCamera; // Можна залишити пустим, знайде 

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    private bool isDashing = false;
    public TrailRenderer trail1;
    public TrailRenderer trail2;

    [Header("Audion Effects")]
    public AudioClip dashSound;
    public AudioSource audioSource;

    // ... змінні для камер та інше ...

    void Awake()
    {
        ch = GetComponent<CharacterController>();
        health = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        controls = new PlayerControls();
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

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Update()
    {
        // Якщо це не наш гравець або він мертвий - не рухаємось
        if (!isOwned || health.IsDead) return;

        HandleInput();
    }

    void HandleInput()
    {
        Vector2 input = controls.Player.Move.ReadValue<Vector2>();
        Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;

        if (moveDir.magnitude >= 0.1f)
        {
            // Рух
            ch.Move(moveDir * moveSpeed * Time.deltaTime);

            // Поворот
            Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);

            animator.SetBool("isMoving", true);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }

        bool isMoving = moveDir.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            // Перевірка на Dash (заміна Input.GetKeyDown(KeyCode.LeftShift))
            if (controls.Player.Dash.WasPressedThisFrame()) Dash();

            if (isDashing) return;
        }
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
            if (health.IsDead) break;

            ch.Move(direction * moveSpeed * Time.deltaTime * 3);
            yield return null;
        }

        isDashing = false;
        if (trail1) trail1.emitting = false;
        if (trail2) trail2.emitting = false;
        animator.SetBool("isDashing", false);
    }
}