using System.Collections;
using UnityEngine;

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
    

    void Start()
    {
        ch = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }


    void LateUpdate()
    {
        Vector3 rawInput = Vector3.zero;
        bool isMoving = false;
        

//керування персонажем
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


    // NEW: Корутина, выполняющая сам рывок
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
    
}