using UnityEngine;
using Mirror;

public class WeaponManager : NetworkBehaviour
{
    public BaseWeapon[] weapons;
    
    [SyncVar(hook = nameof(OnWeaponChanged))]
    private int activeWeaponIndex = 0;

    public BaseWeapon CurrentWeapon => weapons[activeWeaponIndex];

    void Update()
    {
        if (!isOwned) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) CmdSwitchWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CmdSwitchWeapon(1);
    }

    [Command]
    void CmdSwitchWeapon(int index)
    {
        if (index >= 0 && index < weapons.Length) activeWeaponIndex = index;
    }

    void OnWeaponChanged(int oldIdx, int newIdx)
    {
        weapons[oldIdx].gameObject.SetActive(false);
        weapons[newIdx].gameObject.SetActive(true);
        // Тут же можно менять анимацию в аниматоре
        GetComponent<Animator>().SetInteger("WeaponType", (int)weapons[newIdx].weaponType);
    }
}