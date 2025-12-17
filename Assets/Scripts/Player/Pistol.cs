using UnityEngine;

public class Pistol : BaseWeapon
{
    private void Start()
    {
        weaponType = WeaponsTypes.WeaponType.Pistol;
    }

    protected override void PerformShot(Vector3 direction)
    {
        // Пистолет просто спавнит одну пулю
        SpawnBullet(direction);
    }
}