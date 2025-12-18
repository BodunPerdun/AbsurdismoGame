using UnityEngine;
using Mirror; // 1. Додаємо Mirror

public class EnemySpawner : NetworkBehaviour // 2. Успадковуємо від NetworkBehaviour
{
    [Header("Налаштування Спавна")]
    public int enemiesToSpawn = 50; // Для тесту краще почати з меншого числа
    public float spawnRadius = 50f;
    private EnemyPooler pooler;

    // 3. Використовуємо OnStartServer замість Start
    // Це гарантує, що код виконається тільки коли сервер готовий
    public override void OnStartServer()
    {
        pooler = FindFirstObjectByType<EnemyPooler>(); // FindObjectOfType застаріло в нових Unity

        if (pooler == null)
        {
            Debug.LogError("EnemyPooler не знайдено!");
            return;
        }

        SpawnEnemies();
    }

    [Server] // 4. Гарантія, що метод викличе тільки сервер
    void SpawnEnemies()
    {
        int successfullySpawned = 0;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            GameObject enemy = pooler.GetPooledEnemy();

            if (enemy != null)
            {
                Vector3 randomPosition = transform.position + Random.insideUnitSphere * spawnRadius;
                randomPosition.y = transform.position.y;

                enemy.transform.position = randomPosition;
                enemy.transform.rotation = Quaternion.identity; // Бажано скинути поворот

                // 5. Порядок дій для Mirror:
                enemy.SetActive(true); // Спочатку вмикаємо фізично на сервері
                NetworkServer.Spawn(enemy); // Потім кажемо мережі "Заспавни це у всіх клієнтів"

                successfullySpawned++;
            }
            else
            {
                Debug.LogWarning("Пул вичерпано!");
                break;
            }
        }
        Debug.Log($"Заспавнено {successfullySpawned} ворогів через мережу.");
    }

    // Приклад обробки смерті ворога (можна додати в EnemyBase.cs)
    //[Server] // Обробка смерті тільки на сервері
    //public void Die()
    //{
    //    // 1. Повідомляємо клієнтам, що об'єкт зникає
    //    NetworkServer.UnSpawn(gameObject);

    //    // 2. Вимикаємо його фізично на сервері (повертаємо в пулл)
    //    gameObject.SetActive(false);

    //    // Опціонально: Скинути здоров'я на максимум для наступного використання
    //    currentHealth = maxHealth;
    //}
}