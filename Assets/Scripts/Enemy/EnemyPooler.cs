using UnityEngine;
using System.Collections.Generic;
using Mirror; // Додаємо Mirror

public class EnemyPooler : MonoBehaviour // Можна залишити MonoBehaviour
{
    public GameObject enemyPrefab;
    public int poolSize = 100;
    private List<GameObject> pooledEnemies;

    void Start()
    {
        InitializePool();
    }

    void InitializePool()
    {
        pooledEnemies = new List<GameObject>();
        GameObject temp;

        for (int i = 0; i < poolSize; i++)
        {
            temp = Instantiate(enemyPrefab);
            temp.transform.SetParent(this.transform);

            // ВАЖЛИВО: Одразу ховаємо, щоб Mirror не лаявся при старті сцени
            temp.SetActive(false);

            pooledEnemies.Add(temp);
        }
        Debug.Log($"Пул ініціалізовано. {poolSize} об'єктів.");
    }

    public GameObject GetPooledEnemy()
    {
        for (int i = 0; i < pooledEnemies.Count; i++)
        {
            if (!pooledEnemies[i].activeInHierarchy)
            {
                return pooledEnemies[i];
            }
        }
        return null;
    }
}