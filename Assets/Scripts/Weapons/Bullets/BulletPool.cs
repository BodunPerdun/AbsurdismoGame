using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BulletPool : NetworkBehaviour
{
    public static BulletPool Instance; // Сінґлтон для легкого доступу

    [Header("Налаштування")]
    public GameObject bulletPrefab;
    public int poolSize = 50;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        InitializePool();
    }

    // Створюємо кулі заздалегідь, але ховаємо їх
    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    [Server]
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        if (pool.Count == 0)
        {
            // Якщо кулі закінчилися, створюємо нову (розширюємо пул)
            GameObject newBullet = Instantiate(bulletPrefab);
            newBullet.transform.position = position;
            newBullet.transform.rotation = rotation;
            return newBullet;
        }

        GameObject bullet = pool.Dequeue();

        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        return bullet;
    }

    [Server]
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        // Важливо: Mirror вимагає UnSpawn, щоб клієнти перестали бачити об'єкт,
        // але якщо ми просто деактивуємо об'єкт і використаємо Spawn пізніше,
        // це теж спрацює, якщо використовувати NetworkServer.Spawn при пострілі.
        NetworkServer.UnSpawn(bullet);

        pool.Enqueue(bullet);
    }
}