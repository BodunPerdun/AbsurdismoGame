using UnityEngine;
using Mirror;

public class EnemyBase : NetworkBehaviour // 2. Успадковуємо від NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    [Header("Damage Inflicted")]
    private float damageInflicted = 10f;

    [Header("Reward Settings")]
    [SerializeField] private float totalReward = 100f; // Загальна нагорода
    [Range(0, 1)]
    [SerializeField] private float directDepositPercent = 0.3f; // 30% на рахунок, 70% на землю

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
    public void TakeDamage(float damageAmount, GameObject attacker)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        // Debug.Log(gameObject.name + " отримав урон, HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die(attacker);
        }
    }

    [Server] // Обробка смерті тільки на сервері
    public void Die(GameObject attacker)
    {
        float directMoney = totalReward * directDepositPercent; // Гроші на рахунок
        float lootMoney = totalReward - directMoney; // Гроші, що випадають

        if (attacker != null)
        {
            PlayerController player = attacker.GetComponent<PlayerController>();
            if (player != null)
            {
                player.AddCoins(directMoney);
            }
        }

        if (lootMoney > 0)
        {
            // Звертаємось до нашого Singleton пулу
            if (LootPool.Instance != null)
            {
                // Беремо готовий об'єкт з пулу (він ставиться в позицію і вмикається всередині методу GetLoot)
                GameObject lootObj = LootPool.Instance.GetLoot(transform.position + Vector3.up, Quaternion.identity);

                // Налаштовуємо значення
                LootPickup lootItem = lootObj.GetComponent<LootPickup>();
                if (lootItem != null)
                {
                    lootItem.SetValue(lootMoney);
                }

                // ВАЖЛИВО: Кажемо Mirror, що цей об'єкт треба показати всім клієнтам.
                // Навіть якщо об'єкт був "UnSpawned" раніше, Spawn поверне його в мережу.
                NetworkServer.Spawn(lootObj);
            }
            else
            {
                Debug.LogError("LootPool Instance не знайдено на сцені!");
            }
        }

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
            // Kуля має скрипт ScriptedBullet з інформацією про урон
            ScriptedBullet bullet = other.GetComponent<ScriptedBullet>();
            if (bullet != null)
            {
                TakeDamage(bullet.GetDamage(), bullet.GetOwner());
                // Після нанесення урону можна знищити кулю
                bullet.ReturnToPool();
            }
        }
    }

    public float GetEnemyDamage()
    {
        return damageInflicted;
    }
}