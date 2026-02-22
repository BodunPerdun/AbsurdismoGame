using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformReliable))]
public class DraggableItems : NetworkBehaviour
{
    private Rigidbody _rigidbody;
    private Outline _outline;

    void Start()
    {
        this.gameObject.tag = "Draggable";
        _rigidbody = GetComponent<Rigidbody>();
        _outline = GetComponent<Outline>();
        _outline.enabled = false;

        // Встановлюємо світовий простір (World)
        GetComponent<NetworkTransformReliable>().coordinateSpace = CoordinateSpace.World;        
    }

    // Підсвітка залишається локальною, її не треба синхронізувати
    public void SetHighlight(bool active)
    {
        if (_outline != null) _outline.enabled = active;
    }

    // [ClientRpc] означає, що сервер викликає цю функцію, а виконується вона на ВСІХ клієнтах
    [ClientRpc]
    public void RpcPrepareForDrag()
    {
        this.gameObject.layer = 8;
        _rigidbody.useGravity = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        SetHighlight(false);
    }

    [ClientRpc]
    public void RpcPrepareForDrop()
    {
        this.gameObject.layer = 0;
        _rigidbody.useGravity = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }
}