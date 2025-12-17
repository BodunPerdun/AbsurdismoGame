using UnityEngine;
using Mirror;

public class PlayerAim : NetworkBehaviour
{
    public float max_angle = 60f;
    public Camera playerCamera;
    public Transform playerRoot; // Корень префаба (с NetworkTransform)

    public WeaponManager weaponManager; // Скрипт переключения оружия

    void LateUpdate()
    {
        if (!isOwned) return; // Крутим спину только у своего персонажа

        Vector3 targetPoint = GetMouseWorldPoint();
        Vector3 aimDir = (targetPoint - transform.position).normalized;
        aimDir.y = 0;

        // Вращаем кость Spine
        transform.rotation = Quaternion.LookRotation(aimDir);

        // Проверка стрельбы
        if (Input.GetButton("Fire1"))
        {
            weaponManager.CurrentWeapon?.TryShoot(aimDir);
        }
    }

    private Vector3 GetMouseWorldPoint()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float dist)) return ray.GetPoint(dist);
        return transform.position + transform.forward;
    }
}