using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.UnifiedRayTracing;

public class WeaponsSwitching : MonoBehaviour
{
    public GameObject pistol;
    Animator animator;
    private bool isPistol = false;
    
    void Start()
    {
        pistol.SetActive(false);
        animator = GetComponent<Animator>();
    }


    void FixedUpdate()
    {
        if(Input.GetKey(KeyCode.Alpha1))
        {
            {
                pistol.SetActive(false);
                animator.SetBool("isPistol", false);
                isPistol = false;
            }
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
          
                pistol.SetActive(true);
                animator.SetBool("isPistol", true);
                isPistol = true;
        }

    }
}
