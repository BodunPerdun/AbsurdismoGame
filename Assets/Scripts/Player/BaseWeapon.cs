using UnityEngine;

public abstract class BaseWeapon : WeaponsTypes
{
    public WeaponType weaponType;

    public float damage;
    public float fireRate = 10f;
    public float bulletSpeed;
    public GameObject bulletPrefab;
    public Transform firePoint;
    protected float nextTimetoFire = 0f;
    private Vector3 diseredDirection;
    
    public AudioSource audioSource;
    public AudioClip shootSound;
    [Tooltip("Минимальный коэффициент изменения высоты тона (Pitch)")]
    public float minPitch = 0.95f; 
    
    [Tooltip("Максимальный коэффициент изменения высоты тона (Pitch)")]
    public float maxPitch = 1.05f; 
    
    // Опционально: Рандомизация громкости
    [Tooltip("Минимальная громкость")]
    public float minVolume = 0.9f; 
    
    [Tooltip("Максимальная громкость")]
    public float maxVolume = 1.0f;

    public void TryShoot(Vector3 direction)
    {
        diseredDirection = direction.normalized;

        if (Time.time >= nextTimetoFire)
        {
            nextTimetoFire = Time.time + 1f / fireRate;
            CameraManager.Instance.CameraShake(1.3f);
            PerformShot(diseredDirection);
            PlayShootSound();
        }
    }

    protected void SpawnBullet()
    {
        GameObject bulletGO = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        ScriptedBullet bulletScript = bulletGO.GetComponent<ScriptedBullet>();

        if (bulletScript != null)
        {
            bulletScript.direction = diseredDirection;
            bulletScript.speed = bulletSpeed;
            bulletScript.damage = damage;
        }
    }
    protected void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            // 1. Установите клип (если вы используете один и тот же компонент для разных звуков)
            audioSource.clip = shootSound;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.volume = Random.Range(minVolume, maxVolume);
            // 2. Воспроизведение
            audioSource.Play();
        } 
        else if (audioSource == null)
        {
            // Очень полезно для отладки
            Debug.LogError($"На объекте {gameObject.name} отсутствует AudioSource!");
        }
    }

    protected abstract void PerformShot(Vector3 direction);
}