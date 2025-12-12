using UnityEngine;
using UnityEngine.EventSystems;

public class MouseRotation : MonoBehaviour
{
  
    //public float rotation_speed;
    private float forcedRotationSpeed = 100f;
    public float max_angle; //максимальний відхил,після котрого ноги повертаються
    public GameObject hips;
    public Camera playerCamera;
    private PlayerController playerController;
    public Transform playerRoot; 
    void Start()
    {
       
       playerRoot = transform.root;
    }

   
    void LateUpdate()
    {
        Vector3 targetPoint = GetMousePoint();
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0;
        RotateUpBody(direction);
        CheckAndForceBodyRotation(direction);
    }
    
    
    //функція для считування позиції мишки
    Vector3 GetMousePoint()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        float hitDist;
        if (groundPlane.Raycast(ray, out hitDist))
        {
            return ray.GetPoint(hitDist);
        }
        return transform.position;
    }
    //функція для оберту верху тіла(торсу)
    void RotateUpBody(Vector3 direction){
                
        Quaternion diseredRotation = Quaternion.LookRotation(direction);
        transform.rotation = diseredRotation;
                
    }
    //функція перевірки та обертання ніг
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
