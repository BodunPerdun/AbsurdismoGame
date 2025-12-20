using UnityEngine;
using Mirror;

public class ScriptedBullet : NetworkBehaviour
{
    private float speed;
    private float damage;
    private Vector3 direction;

    [Header("Life Time")]
    public float lifeTime = 5f;

    // Таймер для автоматичного повернення в пул, якщо нікуди не влучив
    public override void OnStartServer()
    {
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    // Цей метод викликаємо з BaseWeapon, щоб очистити стару інерцію
    public void ResetBullet()
    {
        CancelInvoke(nameof(ReturnToPool)); // Скасовуємо попередній таймер смерті
        Invoke(nameof(ReturnToPool), lifeTime); // Ставимо новий

        // Якщо є Rigidbody, обов'язково обнуляємо його!
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // (у нових Unity) або rb.velocity
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetDirection(Vector3 dir) { this.direction = dir; }
    public void SetSpeed(float spd) { this.speed = spd; }
    public void SetDamage(float dmg) { this.damage = dmg; }

    public float GetDamage() { return this.damage; }
    public Vector3 GetDirection() { return this.direction; }
    public float GetSpeed() { return this.speed; }

    [ServerCallback]
    void Update()
    {
        // Простий рух (якщо не використовуєш Rigidbody)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    //[ServerCallback]
    //void OnTriggerEnter(Collider other)
    //{
    //    // Логіка влучання (як ми писали раніше)
    //    if (other.CompareTag("Enemy"))
    //    {
    //        // Нанести урон...
    //        EnemyBase enemy = other.GetComponent<EnemyBase>();
    //        if (enemy != null) enemy.TakeDamage(damage);

    //        // Повернути в пул замість Destroy
    //        ReturnToPool();
    //    }
    //    else if (!other.CompareTag("Player") && !other.CompareTag("Bullet")) // Щоб не влучати в себе
    //    {
    //        // Влучив у стіну
    //        ReturnToPool();
    //    }
    //}

    [Server]
    public void ReturnToPool()
    {
        // Перевірка на випадок, якщо об'єкт вже вимкнено
        if (gameObject.activeSelf)
        {
            BulletPool.Instance.ReturnBullet(gameObject);
        }
    }
}