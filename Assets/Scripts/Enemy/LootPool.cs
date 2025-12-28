using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class LootPool : NetworkBehaviour
{
    public static LootPool Instance; // Singleton for easy access

    [Header("Settings")]
    public GameObject lootPrefab;
    public int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewLootObject();
        }
    }

    private GameObject CreateNewLootObject()
    {
        GameObject loot = Instantiate(lootPrefab);
        loot.SetActive(false); // ќдразу ховаЇмо
        pool.Enqueue(loot);
        return loot;
    }

    [Server]
    public GameObject GetLoot(Vector3 position, Quaternion rotation)
    {
        GameObject loot;

        // якщо пул пустий Ч створюЇмо новий об'Їкт
        if (pool.Count == 0)
        {
            loot = Instantiate(lootPrefab);
        }
        else
        {
            loot = pool.Dequeue();
        }

        // ЌалаштовуЇмо позиц≥ю
        loot.transform.position = position;
        loot.transform.rotation = rotation;

        // ¬микаЇмо його, щоб в≥н був готовий до спавну
        loot.SetActive(true);

        return loot;
    }

    [Server]
    public void ReturnLoot(GameObject loot)
    {
        loot.SetActive(false);
        NetworkServer.UnSpawn(loot); // ¬≥дключаЇмо в≥д мереж≥
        pool.Enqueue(loot);
    }

}
