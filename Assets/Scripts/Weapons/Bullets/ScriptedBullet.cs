using UnityEngine;
using Mirror;

public class ScriptedBullet : NetworkBehaviour
{
    [HideInInspector] 
    private GameObject owner; // Хто вистрілив

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

    // Додатковий захист: якщо кулю вимкнули раніше часу, скасовуємо таймер
    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }

    // Цей метод викликаємо з BaseWeapon, щоб очистити стару інерцію
    public void ResetBullet()
    {
        CancelInvoke(nameof(ReturnToPool)); // Скасовуємо попередній таймер смерті
        Invoke(nameof(ReturnToPool), lifeTime); // Ставимо новий

        // Якщо є Rigidbody, обов'язково обнуляємо його!
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null && !rb.isKinematic)
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
    public GameObject GetOwner() { return this.owner; }

    [ServerCallback]
    void Update()
    {
        // Простий рух (якщо не використовуєш Rigidbody)
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }


    // Обробка зіткнень, щоб куля не летіла крізь об'єкти
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // Повернути в пул замість Destroy
        ReturnToPool();
    }

    [ServerCallback]
    public void ReturnToPool()
    {
        // Перевіряємо, чи є пул і чи активна куля, щоб не викликати помилок
        if (BulletPool.Instance != null && gameObject.activeSelf)
        {
            BulletPool.Instance.ReturnBullet(gameObject);
        }
        else if (gameObject.activeSelf)
        {
            // Якщо пулу немає (наприклад, при зупинці гри), просто вимикаємо
            NetworkServer.UnSpawn(gameObject);
            gameObject.SetActive(false);
        }
    }
}