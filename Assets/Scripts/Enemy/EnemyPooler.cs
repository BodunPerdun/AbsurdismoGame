using UnityEngine;
using System.Collections.Generic;

public class EnemyPooler : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int poolSize = 100;

    // Ініціалізуємо список одразу, щоб він не був null
    private List<GameObject> pooledEnemies = new List<GameObject>();
    private bool isInitialized = false; // Прапорець, чи створили ми вже клонів

    void Start()
    {
        // Пробуємо ініціалізувати при старті (якщо ще не зробили це через запит спавнера)
        if (!isInitialized) InitializePool();
    }

    // Робимо метод публічним, щоб мати змогу викликати його примусово, якщо треба
    public void InitializePool()
    {
        if (isInitialized) return; // Якщо вже створено - виходимо

        pooledEnemies = new List<GameObject>(); // Очищаємо/створюємо список
        GameObject temp;

        for (int i = 0; i < poolSize; i++)
        {
            temp = Instantiate(enemyPrefab);
            temp.transform.SetParent(this.transform);
            temp.SetActive(false); // Одразу ховаємо
            pooledEnemies.Add(temp);
        }

        isInitialized = true;
        Debug.Log($"Пул ініціалізовано. {poolSize} об'єктів.");
    }

    public GameObject GetPooledEnemy()
    {
        // --- ГОЛОВНЕ ВИПРАВЛЕННЯ ТУТ ---
        // Якщо список пустий або ще не створений - створюємо його прямо зараз!
        if (!isInitialized || pooledEnemies == null)
        {
            InitializePool();
        }
        // -------------------------------

        for (int i = 0; i < pooledEnemies.Count; i++)
        {
            // Додаткова перевірка на null всередині списку (якщо об'єкт був видалений)
            if (pooledEnemies[i] != null && !pooledEnemies[i].activeInHierarchy)
            {
                return pooledEnemies[i];
            }
        }
        return null;
    }
}