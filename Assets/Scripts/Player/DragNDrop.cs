using System;
using Unity.VisualScripting;
using UnityEngine;

public class DragNDrop : MonoBehaviour
{
    private const string TagOfDraggableItems = "Draggable";
    private const float speedOfDrag = 25f;
    [SerializeField] private int MaxRayDistance = 7;
    [SerializeField] private Transform _playerCamera;
    [SerializeField] private LayerMask _defaultLayerMask;
    [SerializeField] private GameObject _positionOfPickUp;
    private GameObject _draggableObject; 
    private Rigidbody _rbOfDraggableObject;

    private Outline _lastOutlineObject; // Зберігаємо останній об'єкт з підсвічуванням

    void Update()
    {
        // Візуалізація променя для налагодження
        //Debug.DrawRay(_playerCamera.position, _playerCamera.forward * MaxRayDistance, Color.red);

        // 1. Кидаємо промінь тільки якщо ми ще нічого не тримаємо
        if (_draggableObject == null)
        {
            RaycastHit hit;
            if (Physics.Raycast(_playerCamera.position, _playerCamera.forward, out hit, MaxRayDistance, _defaultLayerMask))
            {
                if (hit.collider.CompareTag(TagOfDraggableItems))
                {
                    if (_lastOutlineObject != null)
                    {
                        _lastOutlineObject.enabled = false; // Вимикаємо підсвічування для попереднього об'єкта
                    }

                    _lastOutlineObject = hit.transform.GetComponent<Outline>();
                    _lastOutlineObject.enabled = true;

                    if (Input.GetMouseButtonDown(0))
                    {
                        PrepareForDrag(hit);
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
            _lastOutlineObject = null; // Очищуємо посилання
        }
    }

    private void FixedUpdate()
    {
        // 3. Якщо об'єкт вибрано — рухаємо його (GetMouseButton - поки тримаємо)
        if (_draggableObject != null)
        {
            Drag();
        }

    }

    private void Drag()
    {
        // Розраховуємо дистанцію до цілі
        Vector3 dragDirection = _positionOfPickUp.transform.position - _draggableObject.transform.position;

        // Встановлюємо швидкість у напрямку цілі
        _rbOfDraggableObject.linearVelocity = dragDirection * speedOfDrag;
    }

    private void Drop()
    {
        // Повертаємо гравітацію та очищуємо посилання
        _rbOfDraggableObject.GetComponent<DraggableItems>().PrepareForDrop();
        _draggableObject = null;
        _rbOfDraggableObject = null;
    }
    private void PrepareForDrag(RaycastHit hit)
    {
        _draggableObject = hit.transform.gameObject;
        _rbOfDraggableObject = _draggableObject.GetComponent<Rigidbody>();

        _draggableObject.GetComponent<DraggableItems>().PrepareForDrag();

    }
}
