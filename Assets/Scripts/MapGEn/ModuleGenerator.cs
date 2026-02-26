using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DungeonGenerator : MonoBehaviour {
    [Header("Префабы")]
    public GameObject startPrefab;
    public List<GameObject> canyonPrefabs;
    public List<GameObject> townPrefabs;
    public List<GameObject> fillerPrefabs;

    [Header("Лимиты")]
    public int canyonLength = 5;
    public int totalLength = 15;
    public LayerMask roomLayerMask;

    private List<DungeonPart> spawnedParts = new List<DungeonPart>();
    private GameObject lastPrefab; // Для исключения повторов

    void Start() => Generate();

    void Generate() {
        // 1. Ставим старт
        DungeonPart lastPart = SpawnPart(startPrefab, null);
        if (lastPart == null) return;

        int attempts = 0;
        int maxAttempts = 500; // Чтобы не зависнуть в бесконечном цикле

        // 2. Генерируем цепочку до тех пор, пока не достигнем нужного количества
        while (spawnedParts.Count < totalLength && attempts < maxAttempts) {
            attempts++;

            // Выбираем биом
            List<GameObject> currentPool = (spawnedParts.Count < canyonLength) ? canyonPrefabs : townPrefabs;
            
            // Исключаем последний префаб, чтобы не было повторов
            List<GameObject> filteredPool = currentPool.Where(p => p != lastPrefab).ToList();
            if (filteredPool.Count == 0) filteredPool = currentPool; // Если в пуле всего 1 префаб

            GameObject randomPrefab = filteredPool[Random.Range(0, filteredPool.Count)];
            
            // Пытаемся пристыковаться к последней удачной части
            DungeonPart nextPart = SpawnPart(randomPrefab, lastPart);
            
            if (nextPart != null) {
                lastPart = nextPart;
                lastPrefab = randomPrefab; // Запоминаем, что спавнили
            } else {
                // Если не влезло, пробуем найти ЛЮБОЙ другой свободный сокет в уже построенном городе
                lastPart = spawnedParts.OrderBy(x => Random.value).FirstOrDefault(p => p.entryPoints.Skip(1).Any(e => !e.isOccupied));
            }
        }

        // 3. Закрываем дыры (опционально)
        FillGaps();
        Debug.Log($"Генерация завершена. Всего модулей: {spawnedParts.Count}");
    }

    DungeonPart SpawnPart(GameObject prefab, DungeonPart parent) {
        GameObject newObj = Instantiate(prefab);
        DungeonPart newPart = newObj.GetComponent<DungeonPart>();
        EntryPoint entry = newPart.entryPoints[0]; 

        if (parent != null) {
            // Ищем свободный выход у родителя
            EntryPoint exit = parent.entryPoints.Skip(1).FirstOrDefault(e => !e.isOccupied);
            
            if (exit != null) {
                Align(exit.transform, entry.transform, newObj.transform);
                Physics.SyncTransforms();

                if (IsSpaceFree(newPart)) {
                    exit.isOccupied = true;
                    entry.isOccupied = true;
                    spawnedParts.Add(newPart);
                    return newPart;
                }
            }
            Destroy(newObj);
            return null;
        } else {
            newObj.transform.position = transform.position;
            newObj.transform.rotation = transform.rotation;
            spawnedParts.Add(newPart);
            return newPart;
        }
    }

    void Align(Transform exit, Transform entry, Transform target) {
        target.rotation = exit.rotation * Quaternion.Inverse(entry.localRotation) * Quaternion.Euler(0, 180, 0);
        target.position = exit.position - (entry.position - target.position);
    }

    bool IsSpaceFree(DungeonPart part) {
        // Уменьшаем бокс проверки на 5%, чтобы избежать ложных срабатываний на стыках
        Collider[] colliders = Physics.OverlapBox(
            part.roomCollider.bounds.center, 
            part.roomCollider.size * 0.45f, 
            part.transform.rotation, 
            roomLayerMask
        );

        foreach (var col in colliders) {
            if (col.gameObject.transform.root != part.transform.root) return false;
        }
        return true;
    }

    void FillGaps() {
        if (fillerPrefabs == null || fillerPrefabs.Count == 0) return;
        
        foreach (var part in spawnedParts) {
            foreach (var socket in part.entryPoints) {
                if (!socket.isOccupied) {
                    Instantiate(fillerPrefabs[Random.Range(0, fillerPrefabs.Count)], socket.transform.position, socket.transform.rotation);
                    socket.isOccupied = true;
                }
            }
        }
    }
}