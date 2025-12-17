using UnityEngine;
using Mirror;

public abstract class BaseWeapon : NetworkBehaviour
{
    public WeaponsTypes.WeaponType weaponType;

    [Header("Stats")]
    public float damage = 10f;
    public float fireRate = 10f;
    public float bulletSpeed = 20f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Effects")]
    public AudioClip shootSound;
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    protected float nextTimetoFire = 0f;

    // Метод вызова выстрела (вызывает игрок локально)
    public void TryShoot(Vector3 direction)
    {
        if (Time.time >= nextTimetoFire)
        {
            nextTimetoFire = Time.time + 1f / fireRate;
            
            // Локальный эффект отдачи камеры
            CameraManager.Instance.CameraShake(1.3f);
            
            // Просим сервер произвести выстрел
            CmdPerformShoot(direction.normalized);
        }
    }

    [Command]
    private void CmdPerformShoot(Vector3 direction)
    {
        // Логика выстрела на сервере
        PerformShot(direction);
        // Рассылаем звук всем клиентам
        RpcPlayShootEffects();
    }

    protected void SpawnBullet(Vector3 direction)
    {
        GameObject bulletGO = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        
        // РЕГИСТРИРУЕМ ПУЛЮ В СЕТИ
        NetworkServer.Spawn(bulletGO);

        ScriptedBullet bulletScript = bulletGO.GetComponent<ScriptedBullet>();
        if (bulletScript != null)
        {
            bulletScript.direction = direction;
            bulletScript.speed = bulletSpeed;
            bulletScript.damage = damage;
        }
    }

    [ClientRpc]
    private void RpcPlayShootEffects()
    {
        AudioSource source = GetComponentInParent<AudioSource>();
        if (source != null && shootSound != null)
        {
            source.pitch = Random.Range(minPitch, maxPitch);
            source.PlayOneShot(shootSound);
        }
    }

    protected abstract void PerformShot(Vector3 direction);
}