using UnityEngine;
using Mirror;

public class ScriptedBullet : NetworkBehaviour
{
    [HideInInspector]
    private GameObject owner;

    // 1. [SyncVar] - Швидкість передається з сервера клієнтам при спавні
    [SyncVar]
    private float speed;

    private float damage;
    private Vector3 direction;

    [Header("Life Time")]
    public float lifeTime = 5f;

    public override void OnStartServer()
    {
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }

    public void ResetBullet()
    {
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), lifeTime);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetOwner(GameObject newOwner)
    {
        this.owner = newOwner;
    }

    public void SetDirection(Vector3 dir)
    {
        Debug.Log("Роблю постріл!");
        this.direction = dir;
        // Обертання кулі синхронізується автоматично через NetworkTransform (якщо він є),
        // або через початковий спавн (rotation у GetBullet).
        if (dir != Vector3.zero)
            transform.forward = dir;
    }

    // Цей метод викликається на сервері перед Spawn
    public void SetSpeed(float spd) { this.speed = spd; }
    public void SetDamage(float dmg) { this.damage = dmg; }

    public float GetDamage() { return this.damage; }
    public Vector3 GetDirection() { return this.direction; }
    public float GetSpeed() { return this.speed; }
    public GameObject GetOwner() { return this.owner; }

    // 2. ПРИБРАНО [ServerCallback]
    // Тепер Update працює і на Клієнті, і на Сервері.
    // Клієнт рухає кулю сам, використовуючи синхронізовану швидкість (SyncVar).
    void Update()
    {
        // Рухаємось вперед
        // Важливо: Оскільки куля повертається через transform.forward у SetDirection,
        // клієнт знатиме напрямок завдяки початковому обертанню префабу при спавні.
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    // Зіткнення обробляємо ТІЛЬКИ на сервері
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.gameObject == owner) return;
        if (other.CompareTag("Enemy")) return; // Ворог сам обробить влучання

        // Ігноруємо тригери (наприклад, зони видимості ворогів), реагуємо тільки на тверді тіла
        if (other.isTrigger) return;

        if (!other.CompareTag("Player") && !other.CompareTag("Bullet"))
        {
            ReturnToPool();
        }
    }

    [ServerCallback]
    public void ReturnToPool()
    {
        if (BulletPool.Instance != null && gameObject.activeSelf)
        {
            BulletPool.Instance.ReturnBullet(gameObject);
        }
        else if (gameObject.activeSelf)
        {
            NetworkServer.UnSpawn(gameObject);
            gameObject.SetActive(false);
        }

    }
}