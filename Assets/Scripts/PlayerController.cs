using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    public float move_speed;
    public float rotation_speed;
  
    private CharacterController ch;
    private float verticalVelocity;           
    private Vector3 currentMoveDirection; 
    

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


        Vector3 desiredDirection = rawInput.normalized;
        //обертання ходьби персонажа
        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotation_speed * Time.deltaTime);
        }
        {
            currentMoveDirection = desiredDirection * move_speed;
            float currentspeed = currentMoveDirection.magnitude;
            animator.SetFloat("SpeedInput", currentspeed);
            ch.Move(currentMoveDirection * Time.deltaTime);
        }
        
        
    }

    
}