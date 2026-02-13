using System;
using UnityEngine;

public class Pistol : BaseWeapon
{
    public override GameObject PerformShot(Vector3 direction, GameObject owner)
    {
        
        // Пістолет просто повертає одну створену кулю
        return SpawnBullet(direction, owner);

    }

}