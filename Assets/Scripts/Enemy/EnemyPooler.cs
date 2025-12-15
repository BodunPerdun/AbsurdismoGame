using UnityEngine;
using System.Collections.Generic;

public class EnemyPooler : MonoBehaviour
{
    // Префаб, который нужно пулить (ваш враг)
    public GameObject enemyPrefab;

    // Количество врагов для создания в пуле
    [Header("Настройки Пула")]
    public int poolSize = 100;

    // Список для хранения всех врагов в пуле
    private List<GameObject> pooledEnemies;

    void Awake()
    {
        // Выполняем предварительное создание всех врагов при старте сцены
        InitializePool();
    }

    void InitializePool()
    {
        pooledEnemies = new List<GameObject>();
        GameObject temp;

        for (int i = 0; i < poolSize; i++)
        {
            // 1. Создаем врага
            temp = Instantiate(enemyPrefab);
            
            // 2. Делаем его дочерним для этого менеджера (для чистоты сцены)
            temp.transform.SetParent(this.transform);

            // 3. Скрываем (деактивируем) врага
            temp.SetActive(false);

            // 4. Добавляем в список
            pooledEnemies.Add(temp);
        }
        Debug.Log($"Пул инициализирован. Создано {poolSize} врагов.");
    }

    // --- ПУБЛИЧНЫЙ МЕТОД ДЛЯ ЗАПРОСА ВРАГА ---
    public GameObject GetPooledEnemy()
    {
        // Ищем в списке первого неактивного врага
        for (int i = 0; i < pooledEnemies.Count; i++)
        {
            // Если объект неактивен, возвращаем его
            if (!pooledEnemies[i].activeInHierarchy)
            {
                return pooledEnemies[i];
            }
        }
        
        // ВНИМАНИЕ: Если все 1000 врагов уже активны,
        // этот метод вернет null. В реальной игре нужно расширять пул.
        return null;
    }
}