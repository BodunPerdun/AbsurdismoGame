using UnityEngine;
using Mirror;

public class DragNDrop : NetworkBehaviour // Змінено на NetworkBehaviour
{
    private const string TagOfDraggableItems = "Draggable";
    private const float speedOfDrag = 25f;
    [SerializeField] private int MaxRayDistance = 7;
    [SerializeField] private Transform _playerCamera;
    [SerializeField] private LayerMask _defaultLayerMask;
    [SerializeField] private GameObject _positionOfPickUp;

    private GameObject _draggableObject;
    private Rigidbody _rbOfDraggableObject;
    private Outline _lastOutlineObject;

    void Update()
    {
        // Виконуємо код тільки для свого гравця
        if (!isLocalPlayer) return;

        if (_draggableObject == null)
        {
            RaycastHit hit;
            if (Physics.Raycast(_playerCamera.position, _playerCamera.forward, out hit, MaxRayDistance, _defaultLayerMask))
            {
                if (hit.collider.CompareTag(TagOfDraggableItems))
                {
                    Outline currentOutline = hit.transform.GetComponent<Outline>();
                    if (_lastOutlineObject != currentOutline)
                    {
                        DisableOutline();
                        _lastOutlineObject = currentOutline;
                        if (_lastOutlineObject != null) _lastOutlineObject.enabled = true;
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        PrepareForDrag(hit.transform.gameObject);
                        DisableOutline();
                    }
                }
                else
                {
                    DisableOutline();
                }
            }
            else
            {
                DisableOutline();
            }
        }

        if (Input.GetMouseButtonUp(0) && _draggableObject != null)
        {
            Drop();
        }
    }

    private void DisableOutline()
    {
        if (_lastOutlineObject != null)
        {
            _lastOutlineObject.enabled = false;
            _lastOutlineObject = null;
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (_draggableObject != null && _rbOfDraggableObject != null)
        {
            Drag();
        }
    }

    private void Drag()
    {
        Vector3 dragDirection = _positionOfPickUp.transform.position - _draggableObject.transform.position;
        _rbOfDraggableObject.linearVelocity = dragDirection * speedOfDrag;
    }

    private void PrepareForDrag(GameObject hitObject)
    {
        _draggableObject = hitObject;
        _rbOfDraggableObject = _draggableObject.GetComponent<Rigidbody>();

        // Просимо сервер дати нам права на об'єкт і вимкнути йому гравітацію для всіх
        CmdPickup(_draggableObject);
    }

    private void Drop()
    {
        if (_draggableObject != null)
        {
            // Просимо сервер забрати права і ввімкнути гравітацію
            CmdDrop(_draggableObject);

            _draggableObject = null;
            _rbOfDraggableObject = null;
        }
    }

    // --- ЛОГІКА СЕРВЕРА ---

    // [Command] виконується ТІЛЬКИ на сервері, хоча викликається з клієнта
    [Command]
    private void CmdPickup(GameObject targetObject)
    {
        NetworkIdentity netId = targetObject.GetComponent<NetworkIdentity>();

        // Якщо предмет вже хтось тримає, забираємо в нього права
        if (netId.connectionToClient != null)
        {
            netId.RemoveClientAuthority();
        }

        // Даємо права клієнту, який викликав команду
        netId.AssignClientAuthority(connectionToClient);

        // Кажемо всім клієнтам вимкнути гравітацію та змінити шар
        targetObject.GetComponent<DraggableItems>().RpcPrepareForDrag();
    }

    [Command]
    private void CmdDrop(GameObject targetObject)
    {
        NetworkIdentity netId = targetObject.GetComponent<NetworkIdentity>();

        // Кажемо всім клієнтам ввімкнути гравітацію
        targetObject.GetComponent<DraggableItems>().RpcPrepareForDrop();

        // Забираємо права у клієнта, бо він відпустив предмет
        if (netId.connectionToClient == connectionToClient)
        {
            netId.RemoveClientAuthority();
        }
    }
}