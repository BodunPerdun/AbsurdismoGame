using UnityEngine;
using UnityEngine.EventSystems;

public class MouseRotation : MonoBehaviour
{
  
    // --- НАСТРОЙКИ ПОВОРОТА ТОРСА И КОРНЯ ---
    private float forcedRotationSpeed = 100f;
    public float max_angle; 
    
    // public GameObject hips; // Удалено (не используется)
    // private PlayerController playerController; // Удалено (не используется)
    
    public Camera playerCamera;
    public Transform playerRoot;
    
    // --- НАСТРОЙКИ СТРЕЛЬБЫ ---
    public BaseWeapon activeWeapon;
    
    [Header("Точка выстрела (Для расчёта не используется, только для спавна)")]
    [Tooltip("Эта переменная нужна для BaseWeapon.cs, но не для расчёта направления aimDirection в этом скрипте.")]
    public Transform shootOrigin; 
    
    
    void Start()
    {
       if (playerRoot == null)
       {
           playerRoot = transform.root;
       }
       if (playerCamera == null)
       {
           playerCamera = Camera.main;
       }
    }

   
    void LateUpdate()
    {
        Vector3 targetPoint = GetMousePoint();
        
        // РАСЧЕТ НАПРАВЛЕНИЯ ОТ ТОРСА (transform.position)
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0; // Игнорируем высоту
        
        if (direction.sqrMagnitude > 0.01f)
        {
            RotateUpBody(direction);
            CheckAndForceBodyRotation(direction);
        }
        
        HandleShooting(direction);
    }
    
    
    // функция для считування позиції мишки
    Vector3 GetMousePoint()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 planeCenter = transform.position;
        planeCenter.y = 0.31f;
        // ПЛОСКОСТЬ ЗЕМЛИ: Привязана к Y-координате ТОРСА
        Plane groundPlane = new Plane(Vector3.up, transform.position); 
        
        
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
        if (Input.GetButtonDown("Fire1") && activeWeapon != null)
        {
            
            activeWeapon.TryShoot(direction);
        }
    }
}