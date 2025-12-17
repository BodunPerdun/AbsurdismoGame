using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance;

    public Vector3 MoveInput { get; private set; }
    public Vector2 MousePosition { get; private set; }
    public bool IsDashDown { get; private set; }
    public bool IsFireHeld { get; private set; }
    public int WeaponSlotPressed { get; private set; } // 1, 2...

    void Awake() => Instance = this;

    void Update()
    {
        MoveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        MousePosition = Input.mousePosition;
        IsDashDown = Input.GetKeyDown(KeyCode.LeftShift);
        IsFireHeld = Input.GetButton("Fire1");

        if (Input.GetKeyDown(KeyCode.Alpha1)) WeaponSlotPressed = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) WeaponSlotPressed = 2;
        else WeaponSlotPressed = 0;
    }
}