using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public class MouseRotation : NetworkBehaviour
{
    // --- НАСТРОЙКИ ПОВОРОТА ТОРСА И КОРНЯ ---
    [Header("Налаштування обертання")]
    private float forcedRotationSpeed = 100f;
    public float max_angle = 10f;

    [Header("Посилання")]
    public Camera playerCamera;
    public Transform playerRoot;

    // --- НАСТРОЙКИ СТРЕЛЬБЫ ---
    private WeaponsSwitching weaponsSwitching;


    void Start()
    {
        if (!isOwned) return;

        if (playerRoot == null){playerRoot = transform.root;}

        if (playerCamera == null){playerCamera = Camera.main;}

        // Знаходимо скрипт перемикання зброї на головному об'єкті гравця
        weaponsSwitching = playerRoot.GetComponent<WeaponsSwitching>();
        if (weaponsSwitching == null) { Debug.LogError("Не знайдено скрипт WeaponsSwitching на об'єкті гравця!"); }
    }

   
    void LateUpdate()
    {
        if (!isOwned) return;

        if (playerCamera == null)
        {
            FindCamera();
            if (playerCamera == null) return; // Якщо все ще немає - чекаємо наступного кадру
        }

        // --- ЛОГІКА ПОВОРОТУ ---
        Vector3 targetPoint = GetMousePoint();
        
        // РАСЧЕТ НАПРАВЛЕНИЯ ОТ ТОРСА (transform.position)
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0; // Игнорируем высоту
        
        if (direction.sqrMagnitude > 0.01f)
        {
            // Обертаємо Spine (верхню частину)
            RotateUpBody(direction);
            // Обертаємо ноги, якщо скрутилися занадто сильно
            CheckAndForceBodyRotation(direction);
        }
        
        HandleShooting(direction);
    }

    void FindCamera()
    {
        playerCamera = Camera.main;
        // Альтернатива, якщо Camera.main не працює (наприклад, Cinemachine Brain):
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<Camera>();
    }


    // функция для считування позиції мишки
    Vector3 GetMousePoint()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        // ПЛОСКОСТЬ ЗЕМЛИ: Привязана к Y-координате ТОРСА
        Plane groundPlane = new Plane(Vector3.up, transform.position); 

        Vector3 planeCenter = transform.position;
        planeCenter.y = 0.31f;
        
        
        float hitDist;
        if (groundPlane.Raycast(ray, out hitDist))
        {
            return ray.GetPoint(hitDist);
        }
        return transform.position;
    }
    
    // функция для оберту верху тіла(торсу)
    void RotateUpBody(Vector3 direction){
        Quaternion diseredRotation = Quaternion.LookRotation(direction);
        transform.rotation = diseredRotation;
    }
   
    // функция перевірки та обертання ніг
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

    void HandleShooting(Vector3 direction)
    {
        BaseWeapon activeWeapon = weaponsSwitching.GetActiveWeapon();

        if (activeWeapon == null || weaponsSwitching == null) return;

        // Для одиночної черги пострілів
        if (Input.GetButtonDown("Fire1") && activeWeapon != null)
        {
            weaponsSwitching.Fire(direction);
        }
       
    }
}