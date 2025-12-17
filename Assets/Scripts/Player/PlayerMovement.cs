using UnityEngine;
using Mirror;
using System.Collections;
using Unity.Cinemachine;

public class PlayerMovement : NetworkBehaviour
{
    public float move_speed = 7f;
    public float rotation_speed = 10f;
    private CharacterController ch;
    private Animator animator;

    [Header("Dash")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public TrailRenderer[] trails;
    private bool isDashing = false;
    private CinemachineCamera _targetCamera;

    void Start()
    {
        ch = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // Если это наш локальный игрок — настраиваем камеру
        if (isOwned)
        {
            CameraManager.Instance.SwitchToCamera(_targetCamera);
        }
    }

    void Update()
    {
        if (!isOwned) return; // Не даем управлять чужими игроками

        if (isDashing) return;

        HandleInput();
    }

    private void HandleInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = new Vector3(x, 0, z).normalized;

        bool isMoving = moveDir.sqrMagnitude > 0.01f;
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            // Поворот ног в сторону движения
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotation_speed * Time.deltaTime);
            ch.Move(moveDir * move_speed * Time.deltaTime);
            animator.SetFloat("SpeedInput", move_speed);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && isMoving)
        {
            StartCoroutine(PerformDash(moveDir));
        }
    }

    private IEnumerator PerformDash(Vector3 dir)
    {
        isDashing = true;
        animator.SetBool("isDashing", true);
        foreach(var t in trails) t.emitting = true;

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            ch.Move(dir * dashSpeed * Time.deltaTime);
            yield return null;
        }

        foreach(var t in trails) t.emitting = false;
        animator.SetBool("isDashing", false);
        isDashing = false;
    }
}