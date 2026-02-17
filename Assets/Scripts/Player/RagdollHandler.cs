using UnityEngine;
using System.Collections.Generic;

public class RagdollHandler : MonoBehaviour
{
    [SerializeField]
    private List<Rigidbody> _rigidbody;

    public void Awake()
    {
        _rigidbody = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
   
        SetIsKinematicRagdoll(true); // Виключаємо ragdoll при ініціалізації, щоб персонаж був керованим анімацією
    }
    public void SetIsKinematicRagdoll(bool state)
    {
        foreach (var rb in _rigidbody)
        {
            rb.isKinematic = state;
        }
    }
}
