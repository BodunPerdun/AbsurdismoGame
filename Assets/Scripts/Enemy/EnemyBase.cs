using UnityEngine;
using Mirror; // 1. Додаємо Mirror

public class EnemyBase : NetworkBehaviour // 2. Успадковуємо від NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    // 3. SyncVar дозволяє автоматично передавати значення HP клієнтам (корисно для смужки здоров'я)
    [SyncVar]
    private float currentHealth;

    // Викликається на клієнті, коли об'єкт з'являється в мережі
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Примусово вмикаємо об'єкт, бо Mirror міг заспавнити його вимкненим
        gameObject.SetActive(true);
    }

    // 4. Використовуємо OnEnable для скидання здоров'я
    // Цей метод спрацьовує щоразу, коли об'єкт дістають з пулу (SetActive(true))
    void OnEnable()
    {
        currentHealth = maxHealth;
    }

    // 5. [Server] означає, що цей код виконається ТІЛЬКИ на сервері.
    // Клієнти не можуть самі собі нанести шкоду, це вирішує сервер.
    [Server]
    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        // Debug.Log(gameObject.name + " отримав урон, HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [Server] // Обробка смерті тільки на сервері
    public void Die()
    {
        // 1. Повідомляємо клієнтам, що об'єкт зникає
        NetworkServer.UnSpawn(gameObject);

        // 2. Вимикаємо його фізично на сервері (повертаємо в пулл)
        gameObject.SetActive(false);

        // Опціонально: Скинути здоров'я на максимум для наступного використання
        currentHealth = maxHealth;

        Debug.Log($"{gameObject.name} помер.");
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // Приклад: якщо ворог натрапляє на кулю гравця
        if (other.CompareTag("Bullet"))
        {
            // Припустимо, що куля має скрипт Bullet з інформацією про урон
            ScriptedBullet bullet = other.GetComponent<ScriptedBullet>();
            if (bullet != null)
            {
                TakeDamage(bullet.GetDamage());
                // Після нанесення урону можна знищити кулю
                bullet.ReturnToPool();
            }
        }
    }
}