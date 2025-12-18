using System;
using UnityEngine;

public class Pistol : BaseWeapon
{
    protected override void PerformShot(Vector3 direction)
    {
        // Пістолет стріляє просто однією кулею прямо
        SpawnBullet(direction);
    }
}