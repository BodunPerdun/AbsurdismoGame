using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
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

    [Header("Camera Follow Settings")]
    [SerializeField] public CinemachineCamera _targetCamera;
    public float forwardOffset = 5f;
    public float backwardOffset = 0f;
    private CinemachineRotationComposer _rotationComposer; // Змінна для зберігання композера

    [Header("Camera Switch Settings")]
    // Вказуємо в інспекторі, на яку камеру перемикатися в цій зоні
    [SerializeField] private CinemachineCamera _checkBaseCamera;
    [SerializeField] private KeyCode keyToSwitchCamera = KeyCode.Q;
    private bool isPlayerCamera = true;

    void Start()
    {
        if (_targetCamera != null){_rotationComposer = _targetCamera.GetComponent<CinemachineRotationComposer>();}
        else{Debug.LogError("Камеру не призначено в інспекторі!");}

        ch = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }


    void LateUpdate()
    {
        Vector3 rawInput = Vector3.zero;
        bool isMoving = false;
        

        // Керування персонажем
        if (Input.GetKey(KeyCode.W))
        {
            rawInput += Vector3.forward;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            rawInput += Vector3.back;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            rawInput += Vector3.right;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            rawInput += Vector3.left;
            isMoving = true;
        }
        // Клік лівою кнопкою миші для тряски камери 
        if (Input.GetMouseButtonDown(0)) { CameraManager.Instance.CameraShake(1.5f); }

        
        if (Input.GetKeyDown(keyToSwitchCamera))
        { // ВИКЛИК ПЕРЕМИКАННЯ:
            SwitchToCamera();
            isPlayerCamera = !isPlayerCamera;
        }

        cameraTargetTracking(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));

        // Відміна руху при одночасному натисканні протилежних клавіш
        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D)) { isMoving = false; }
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S)) { isMoving = false; }

        if (isMoving)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                Dash();
            }
            
            if (isDashing)
            {
                return;
            }
        }
        animator.SetBool("isMoving", isMoving);
        Vector3 desiredDirection = rawInput.normalized;

        if (isMoving)
        {
            targetVelocity = desiredDirection * move_speed;
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotation_speed * Time.deltaTime);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
            float currentspeed = currentVelocity.magnitude;
            animator.SetFloat("SpeedInput", currentspeed);
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
        StartCoroutine(PerformDash(dashDirection));
    }

    IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        animator.SetBool("isDashing", true);
        float startTime = Time.time;
        if (isDashing)
        {
            trail1.emitting = true;
            trail2.emitting = true;
        }      
                
        while (Time.time < startTime + dashDuration)
        {
            
            ch.Move(direction * move_speed * Time.deltaTime * 3);
            yield return null;
        }

        isDashing = false;
        trail1.emitting = false;
        trail2.emitting = false;
        animator.SetBool("isDashing", false);
    }
 
    void cameraTargetTracking(bool isForward, bool isBackward)
    {
        if (isForward)
        {
            _rotationComposer.TargetOffset.y = forwardOffset;
            _rotationComposer.TargetOffset.x = 0f;

        }
        if (isBackward)
        {
            _rotationComposer.TargetOffset.y = backwardOffset;
            _rotationComposer.TargetOffset.x = 0f;
        }

    }

    public void SwitchToCamera()
    {
        if (isPlayerCamera)
        {
            CameraManager.Instance.SwitchToCamera(_checkBaseCamera);
        }else
        {
            CameraManager.Instance.SwitchToCamera(_targetCamera);
        }
    }

    public void TakeDamage(int amount)
    {
        // ... логіка здоров'я ...

        // ВИКЛИК ТРЯСКИ:
        // Просто один рядок. 0.5f - це сила тряски (можна більше або менше)
        CameraManager.Instance.CameraShake(0.5f);
    }
}