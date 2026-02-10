using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    [Header("Type")]
    public WeaponsTypes.WeaponType weaponType;

    [Header("Stats")]
    public float damage = 10f;
    public float fireRate = 5f;
    public float bulletSpeed = 20f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Effects")]
    public AudioClip shootSound;
    public AudioSource audioSource;
    public float cameraShakeForce = 1.0f;

    // --- ВИПРАВЛЕННЯ: Два різні таймери ---
    protected float nextFireTimeClient = 0f; // Таймер для візуалу (клієнт)
    protected float nextFireTimeServer = 0f; // Таймер для логіки (сервер)

    // --- ЛОГІКА КЛІЄНТА (Миттєвий візуал) ---
    public bool TryShootClient()
    {
        // Перевіряємо клієнтський таймер
        if (Time.time >= nextFireTimeClient)
        {
            // Оновлюємо ТІЛЬКИ клієнтський таймер
            nextFireTimeClient = Time.time + 1f / fireRate;

            // 1. Тряска камери
            if (CameraManager.Instance != null)
                CameraManager.Instance.CameraShake(cameraShakeForce);

            // 2. Звук
            PlayShootSound();

            return true;
        }
        return false;
    }

    public abstract GameObject PerformShot(Vector3 direction, GameObject owner);

    // --- ЛОГІКА СЕРВЕРА ---
    protected GameObject SpawnBullet(Vector3 direction, GameObject owner)
    {
        // Перевіряємо СЕРВЕРНИЙ таймер
        // Це важливо: навіть у Host-режимі це буде окрема змінна від клієнтської,
        // або ж, якщо це одна змінна, ми її ще не чіпали в TryShootClient.
        // Але краще мати дві змінні, щоб уникнути конфліктів.
        if (Time.time < nextFireTimeServer) return null;

        // Оновлюємо серверний таймер
        nextFireTimeServer = Time.time + 1f / fireRate;

        if (bulletPrefab == null || firePoint == null || BulletPool.Instance == null)
        {
            Debug.LogError("[BaseWeapon] Помилка: Немає префабу, FirePoint або Пулу!");
            return null;
        }

        // 1. Створюємо фізичний об'єкт
        GameObject bulletGO = BulletPool.Instance.GetBullet(firePoint.position, Quaternion.LookRotation(direction));

        // 2. Налаштовуємо скрипт кулі
        ScriptedBullet bulletScript = bulletGO.GetComponent<ScriptedBullet>();
        if (bulletScript != null)
        {
            bulletScript.ResetBullet();
            bulletScript.SetDirection(direction);
            bulletScript.SetSpeed(bulletSpeed);
            bulletScript.SetDamage(damage);
            bulletScript.SetOwner(owner);
        }

        return bulletGO;
    }

    public void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(shootSound);
        }
    }

    public void SetSelectStatus(bool isSelected) { gameObject.SetActive(isSelected); }
}