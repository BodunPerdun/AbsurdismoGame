using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class MouseRotation : NetworkBehaviour
{
    public bool isActive = true;

    [Header("Налаштування обертання")]
    private float forcedRotationSpeed = 100f;
    public float max_angle = 60f;

    [Header("Посилання")]
    public Camera playerCamera;
    public Transform playerRoot;
    private WeaponsSwitching weaponsSwitching;

    // Ми НЕ створюємо тут new PlayerControls().
    // Ми зберігаємо посилання на той, що в PlayerMovement.
    private PlayerControls controls;

    void Start()
    {
        if (!isOwned) return;

        if (playerRoot == null) playerRoot = transform.root;
        if (playerCamera == null) playerCamera = Camera.main;

        weaponsSwitching = playerRoot.GetComponent<WeaponsSwitching>();
        if (weaponsSwitching == null) Debug.LogError("Не знайдено скрипт WeaponsSwitching!");

        // --- ГОЛОВНА ЗМІНА ---
        // Знаходимо сусідній скрипт PlayerMovement
        var movement = GetComponent<PlayerMovement>();

        if (movement != null)
        {
            // Беремо вже створений контролер
            controls = movement.Controls;
        }
        else
        {
            Debug.LogError("CRITICAL ERROR: MouseRotation не знайшов PlayerMovement на цьому об'єкті!");
        }


    }

    void LateUpdate()
    {
  
        // Додаємо перевірку controls != null, щоб уникнути помилок, якщо старт не вдався
        if (!isOwned || !isActive || controls == null) return;

        if (playerCamera == null)
        {
            FindCamera();
            if (playerCamera == null) return;
        }

        Vector3 targetPoint = GetMousePoint();
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            RotateUpBody(direction);
            CheckAndForceBodyRotation(direction);
        }

        HandleShooting(direction);
    }

    void HandleShooting(Vector3 direction)
    {
        if (weaponsSwitching == null) return;

        // Використовуємо спільний контролер
        if (controls.Player.Fire.WasPressedThisFrame())
        {
            weaponsSwitching.Fire(direction);
        }
    }

    // --- Допоміжні методи (без змін) ---
    void FindCamera()
    {
        playerCamera = Camera.main;
        if (!playerCamera) playerCamera = FindFirstObjectByType<Camera>();
    }

    Vector3 GetMousePoint()
    {
        if (Mouse.current == null) return transform.position;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        // Математично піднімаємо площину, якщо треба (зміщення точки створення Plane не впливає на висоту, 
        // треба зміщувати саму площину по нормалі, але для Raycast це зазвичай не критично в top-down)
        float hitDist;
        if (groundPlane.Raycast(ray, out hitDist))
        {
            return ray.GetPoint(hitDist);
        }
        return transform.position;
    }

    void RotateUpBody(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void CheckAndForceBodyRotation(Vector3 aimDirection)
    {
        Vector3 playerForward = playerRoot.forward;
        float angleOffset = Vector3.SignedAngle(playerForward, aimDirection, Vector3.up);
        if (Mathf.Abs(angleOffset) > max_angle)
        {
            float excessAngle = angleOffset - Mathf.Sign(angleOffset) * max_angle;
            playerRoot.Rotate(Vector3.up, excessAngle * Time.deltaTime * (forcedRotationSpeed / 10f));
        }
    }
}