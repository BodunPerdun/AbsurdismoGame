using UnityEngine;
using Mirror;

public class ScriptedBullet : NetworkBehaviour
{
    [HideInInspector]
    private GameObject owner; // Тепер ми будемо це заповнювати

    private float speed;
    private float damage;
    private Vector3 direction; // Вектор польоту

    [Header("Life Time")]
    public float lifeTime = 5f;

    public override void OnStartServer()
    {
        // Запускаємо таймер життя
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

    // --- ГОЛОВНЕ ВИПРАВЛЕННЯ ---
    // Додаємо метод для встановлення власника
    public void SetOwner(GameObject newOwner)
    {
        this.owner = newOwner;
        // Debug.Log($"Куля: Власник встановлений - {newOwner.name}");
    }
    // ---------------------------

    public void SetDirection(Vector3 dir)
    {
        this.direction = dir;
        // Одразу повертаємо кулю в бік польоту, щоб Translate працював коректно
        if (dir != Vector3.zero)
            transform.forward = dir;
    }

    public void SetSpeed(float spd) { this.speed = spd; }
    public void SetDamage(float dmg) { this.damage = dmg; }

    public float GetDamage() { return this.damage; }
    public Vector3 GetDirection() { return this.direction; }
    public float GetSpeed() { return this.speed; }
    public GameObject GetOwner() { return this.owner; }

    [ServerCallback]
    void Update()
    {
        // Рухаємось вперед відносно повороту кулі
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // 1. Ігноруємо самого стрілка (щоб не вбити себе на бігу)
        if (owner != null && other.gameObject == owner) return;

        // 2. Ігноруємо ворога ТУТ, тому що логіка знищення кулі вже прописана в EnemyBase
        // Якщо ми повернемо кулю в пул тут, скрипт EnemyBase може не встигнути прочитати GetOwner()
        if (other.CompareTag("Enemy")) return;

        // 3. Якщо влучили у стіну чи перешкоду - зникаємо
        if (!other.CompareTag("Player") && !other.CompareTag("Bullet")) // Додайте інші теги, які треба ігнорувати
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