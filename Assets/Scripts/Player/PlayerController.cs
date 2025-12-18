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
    private Vector3 currentMoveDirection;
    private float acceleration = 100f;
    private Vector3 targetVelocity;
    private Vector3 currentVelocity;

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
    [SerializeField] private CinemachineCamera _checkBaseCamera; // Можна залишити пустим, знайде саме

    [Header("Input Settings")]
    [SerializeField] private KeyCode keyToSwitchCamera = KeyCode.Q;
    private bool isPlayerCamera = true; // true = Main, false = Second

    public AudioSource audioSource;
    public AudioClip dashSound;

    void Start()
    {
        // У Mirror краще ініціалізувати компоненти тут, а логіку мережі в OnStartLocalPlayer
        ch = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
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
        if (!isOwned) return;

        // --- ЛОГІКА ПЕРЕМИКАННЯ КАМЕРИ ---
        if (Input.GetKeyDown(keyToSwitchCamera))
        {
            SwitchToCamera();
        }

        // Керування офсетом працює ТІЛЬКИ якщо активна основна камера
        if (isPlayerCamera)
        {
            cameraTargetTracking(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));
        }

        // --- РУХ ПЕРСОНАЖА ---
        HandleMovement();
    }

    // Виніс рух в окремий метод для чистоти коду
    void HandleMovement()
    {
        Vector3 rawInput = Vector3.zero;
        bool isMoving = false;

        if (Input.GetKey(KeyCode.W)) { rawInput += Vector3.forward; isMoving = true; }
        if (Input.GetKey(KeyCode.S)) { rawInput += Vector3.back; isMoving = true; }
        if (Input.GetKey(KeyCode.D)) { rawInput += Vector3.right; isMoving = true; }
        if (Input.GetKey(KeyCode.A)) { rawInput += Vector3.left; isMoving = true; }

        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D)) { isMoving = false; }
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S)) { isMoving = false; }

        if (isMoving)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift)) Dash();
            if (isDashing) return;
        }

        animator.SetBool("isMoving", isMoving);
        Vector3 desiredDirection = rawInput.normalized;

        if (isMoving)
        {
            targetVelocity = desiredDirection * move_speed;
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotation_speed * Time.deltaTime);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

            animator.SetFloat("SpeedInput", currentVelocity.magnitude);
            ch.Move(currentVelocity * Time.deltaTime);
        }
        else
        {
            targetVelocity = Vector3.zero;
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
}