using UnityEngine;

public class CameraControlls : MonoBehaviour
{
    private Quaternion fixedRotation;
    public GameObject target;
    public Vector3 offset;
    void Start()
    { 
        offset = new Vector3(0,20,-10);
      fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        transform.position = target.transform.position+offset;
        transform.rotation = fixedRotation;
    }
}
