using UnityEngine;
using Mirror;

public abstract class BaseWeapon : MonoBehaviour
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

            // Ефекти (тряска камери) - це спрацює тільки локально, якщо викликати на клієнті,
            // але оскільки ми стріляємо через сервер, тряску треба робити окремо (Rpc), 
            // або ігнорувати, якщо сервер - це не гравець.
            // Для простоти поки залишимо так, але пам'ятай про це.

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
    // Базовий метод спавну кулі
    protected void SpawnBullet(Vector3 direction)
    {
        if (bulletPrefab == null || firePoint == null) return;
        if (BulletPool.Instance == null)
        {
            Debug.LogError("BulletPool не знайдено!");
            return;
        }

        GameObject bulletGO = BulletPool.Instance.GetBullet(firePoint.position, Quaternion.LookRotation(direction));

        ScriptedBullet bulletScript = bulletGO.GetComponent<ScriptedBullet>();
        if (bulletScript != null)
        {
            bulletScript.ResetBullet();
            bulletScript.SetDirection(direction);
            bulletScript.SetSpeed(bulletSpeed);
            bulletScript.SetDamage(damage);

            // --- ДОДАНО ---
            // Шукаємо гравця, який тримає цю зброю.
            // GetComponentInParent<NetworkIdentity>() знайде головний об'єкт гравця,
            // навіть якщо зброя знаходиться глибоко в ієрархії (наприклад: Player -> Hands -> WeaponHolder -> Gun)
            NetworkIdentity playerIdentity = GetComponentInParent<NetworkIdentity>();

            if (playerIdentity != null)
            {
                bulletScript.SetOwner(playerIdentity.gameObject);
            }
            else
            {
                // Якщо раптом зброя не прикріплена до гравця з NetworkIdentity
                Debug.LogWarning("Зброя не знайшла власника (NetworkIdentity)!");
            }
            // --------------
        }

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