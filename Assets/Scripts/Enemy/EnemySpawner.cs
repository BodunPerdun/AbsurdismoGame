using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Настройки Спавна")]
    public int enemiesToSpawn = 1000;
    public float spawnRadius = 50f; 
    private EnemyPooler pooler;

    void Start()
    {
        pooler = FindObjectOfType<EnemyPooler>();

        if (pooler == null)
        {
            Debug.LogError("EnemyPooler не найден в сцене! Спавн невозможен.");
            return;
        }

        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        int successfullySpawned = 0;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // 1. Запрашиваем врага из пула
            GameObject enemy = pooler.GetPooledEnemy();

            if (enemy != null)
            {
                // 2. Генерируем случайную позицию в пределах радиуса
                Vector3 randomPosition = transform.position + Random.insideUnitSphere * spawnRadius;
                
                // Убедимся, что враг спавнится на уровне земли (Y = 0 или свой уровень)
                randomPosition.y = transform.position.y; 
                enemy.transform.position = randomPosition;
                enemy.SetActive(true);
                successfullySpawned++;

                // *Опционально: сбросить здоровье/настройки врага в скрипте врага*
            }
            else
            {
                Debug.LogWarning("Пул врагов исчерпан. Не удалось заспавнить всех.");
                break;
            }
        }
        Debug.Log($"Успешно заспавнено {successfullySpawned} врагов.");
    }
}