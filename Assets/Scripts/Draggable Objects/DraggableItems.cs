using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Outline))]
public class DraggableItems : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Outline _outline;

    void Start()
    {
        this.gameObject.tag = "Draggable";
        _rigidbody = GetComponent<Rigidbody>();
        _outline = GetComponent<Outline>();
        _outline.enabled = false; // Вимкнено за замовчуванням
    }

    public void SetHighlight(bool active)
    {
        if (_outline != null) _outline.enabled = active;
    }

    public void PrepareForDrag()
    {
        this.gameObject.layer = 8; // Переконайся, що шар існує
        _rigidbody.useGravity = false;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        SetHighlight(false); // Про всяк випадок
    }

    public void PrepareForDrop()
    {
        this.gameObject.layer = 0;
        _rigidbody.useGravity = true;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }
}