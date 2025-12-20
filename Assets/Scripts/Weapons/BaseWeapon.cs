using UnityEngine;
using Mirror;

public abstract class BaseWeapon : NetworkBehaviour
{
    [Header("Type")]
    public WeaponsTypes.WeaponType weaponType; // Тип зброї для аніматора

    [Header("Stats")]
    public float damage = 10f;
    public float fireRate = 10f;
    public float bulletSpeed = 20f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Effects")]
    public AudioClip shootSound;
    public AudioSource audioSource;

    [Tooltip("Min/Max Pitch")]
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Tooltip("Min/Max Volume")]
    public float minVolume = 0.9f;
    public float maxVolume = 1.0f;

    protected float nextTimetoFire = 0f;

    private bool isSelected = false;

    public virtual void TryShoot(Vector3 direction)
    {
        if (Time.time >= nextTimetoFire && this.CheckSelectedStatus())
        {
            nextTimetoFire = Time.time + 1f / fireRate;

            // Якщо є CameraManager, викликаємо тряску
            if (CameraManager.Instance != null)
                CameraManager.Instance.CameraShake(1.3f);

            PerformShot(direction.normalized);
            PlayShootSound();
        }else { return; }
    }

    // Абстрактний метод: кожна зброя сама вирішує, як саме стріляти (одна куля, черга, дроб)
    protected abstract void PerformShot(Vector3 direction);

    // Базовий метод спавну кулі, приймає напрямок
    [Server] // Гарантуємо, що спавнить тільки сервер
    protected void SpawnBullet(Vector3 direction)
    {
        if (bulletPrefab == null || firePoint == null) return;

        // ЗАМІСТЬ Instantiate: Беремо з пулу
        // Переконайся, що BulletPool існує на сцені
        if (BulletPool.Instance == null)
        {
            Debug.LogError("BulletPool не знайдено на сцені!");
            return;
        }

        GameObject bulletGO = BulletPool.Instance.GetBullet(firePoint.position, Quaternion.LookRotation(direction));

        // Налаштовуємо скрипт кулі
        ScriptedBullet bulletScript = bulletGO.GetComponent<ScriptedBullet>();
        if (bulletScript != null)
        {
            // Скидаємо старі параметри (важливо для пулу!)
            bulletScript.ResetBullet();

            bulletScript.SetDirection(direction);
            bulletScript.SetSpeed(bulletSpeed);
            bulletScript.SetDamage(damage);
        }

        // КРИТИЧНО ДЛЯ MIRROR:
        // Кажемо клієнтам: "Гей, цей об'єкт тепер активний, покажіть його у себе!"
        NetworkServer.Spawn(bulletGO);
    }

    protected void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.clip = shootSound;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.volume = Random.Range(minVolume, maxVolume);
            audioSource.PlayOneShot(shootSound); // PlayOneShot краще для стрільби, щоб звуки не переривали один одного
        }
    }

    public void SetSelectStatus(bool isSelected)
    {
        this.isSelected = isSelected;
    }

    protected bool CheckSelectedStatus()
    {
        return isSelected;
    }
}