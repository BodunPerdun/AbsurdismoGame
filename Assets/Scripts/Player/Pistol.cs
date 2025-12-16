using System;
using UnityEngine;

public class Pistol : BaseWeapon
{
    private bool isPistol = true;
    private void Start()
    {
        weaponType = WeaponType.Pistol;
        
    }

    protected override void PerformShot(Vector3 direction)
    {
        SpawnBullet();
    }
}
