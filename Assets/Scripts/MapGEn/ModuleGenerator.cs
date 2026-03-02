using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum PartType { Canyon, CityRoad, House, Start }

public class DungeonGenerator : MonoBehaviour {
    [Header("Префабы")]
    public GameObject startPrefab;
    public List<GameObject> canyonPrefabs;
    public List<GameObject> roadPrefabs;
    public List<GameObject> housePrefabs;
    public List<GameObject> fillerPrefabs;

    [Header("Настройки")]
    public int canyonLimit = 5;      
    public int totalRoads = 15;       
    public LayerMask roomLayerMask;   
    public bool spawnFillersOnRoadSides = false;

    private List<DungeonPart> roadPath = new List<DungeonPart>();

    void Start() => Generate();

    void Generate() {
        roadPath.Clear();
        DungeonPart start = SpawnRoadOnly(startPrefab, null);
        if (start == null) return;

        int safety = 0;
        while (roadPath.Count < totalRoads && safety < 1000) {
            safety++;
            List<GameObject> currentPool = (roadPath.Count <= canyonLimit) ? canyonPrefabs : roadPrefabs;
            
            // Берем последний модуль, у которого ЕСТЬ выход
            DungeonPart parentPart = roadPath.LastOrDefault(p => p.entryPoints.Count >= 2 && !p.entryPoints[1].isOccupied);
            if (parentPart == null) break;

            GameObject prefab = currentPool[Random.Range(0, currentPool.Count)];
            SpawnRoadOnly(prefab, parentPart);
        }

        SpawnHousesOnly();
        FillGaps();
    }

    DungeonPart SpawnRoadOnly(GameObject prefab, DungeonPart parent) {
        GameObject newObj = Instantiate(prefab);
        DungeonPart newPart = newObj.GetComponent<DungeonPart>();

        if (newPart.entryPoints.Count == 0) { Destroy(newObj); return null; }

        if (parent != null) {
            EntryPoint exit = parent.entryPoints[1];
            EntryPoint entry = newPart.entryPoints[0];
            
            Align(exit.transform, entry.transform, newObj.transform);
            Physics.SyncTransforms();

            // Если это каньон, мы позволяем ему пересекаться с соседями сильнее (чувствительность 0.1)
            float sensitivity = (newPart.type == PartType.CityRoad) ? 0.2f : 0.1f;

            if (IsSpaceFree(newPart, sensitivity)) {
                exit.isOccupied = true;
                entry.isOccupied = true;
                roadPath.Add(newPart);
                return newPart;
            }
            Destroy(newObj);
            return null;
        } else {
            roadPath.Add(newPart);
            return newPart;
        }
    }

    void SpawnHousesOnly() {
        foreach (var road in roadPath) {
            if (road.type != PartType.CityRoad) continue;
            for (int i = 2; i < road.entryPoints.Count; i++) {
                if (road.entryPoints[i].isOccupied) continue;
                
                GameObject hPrefab = housePrefabs[Random.Range(0, housePrefabs.Count)];
                GameObject hObj = Instantiate(hPrefab);
                DungeonPart hPart = hObj.GetComponent<DungeonPart>();
                
                Align(road.entryPoints[i].transform, hPart.entryPoints[0].transform, hObj.transform);
                Physics.SyncTransforms();

                if (IsSpaceFree(hPart, 0.1f)) {
                    road.entryPoints[i].isOccupied = true;
                    hPart.entryPoints[0].isOccupied = true;
                    hObj.transform.SetParent(road.transform);
                } else { Destroy(hObj); }
            }
        }
    }

    void FillGaps() {
        foreach (var p in roadPath) {
            for (int i = 0; i < p.entryPoints.Count; i++) {
                if (p.entryPoints[i].isOccupied) continue;
                if (p.type == PartType.CityRoad && i >= 2 && !spawnFillersOnRoadSides) continue;

                if (fillerPrefabs.Count > 0) {
                    GameObject filler = fillerPrefabs[Random.Range(0, fillerPrefabs.Count)];
                    Instantiate(filler, p.entryPoints[i].transform.position, p.entryPoints[i].transform.rotation, p.transform);
                    p.entryPoints[i].isOccupied = true;
                }
            }
        }
    }

    void Align(Transform exit, Transform entry, Transform target) {
        target.rotation = exit.rotation * Quaternion.Inverse(entry.localRotation) * Quaternion.Euler(0, 180, 0);
        target.position = exit.position - (entry.position - target.position);
    }

    bool IsSpaceFree(DungeonPart part, float sensitivity) {
        if (part.roomCollider == null) return true;

        // Размер бокса делаем очень тонким, чтобы он проверял только "центр" модуля
        Vector3 size = Vector3.Scale(part.roomCollider.size, part.transform.lossyScale) * sensitivity;
        Collider[] colliders = Physics.OverlapBox(part.roomCollider.bounds.center, size / 2f, part.transform.rotation, roomLayerMask);
        
        foreach (var col in colliders) {
            // ИГНОРИРУЕМ ВСЁ, что является частью нашего уже построенного подземелья
            DungeonPart colPart = col.transform.root.GetComponentInChildren<DungeonPart>();
            if (colPart != null && roadPath.Contains(colPart)) continue;
            
            // Если столкнулись с чем-то чужим - возвращаем false
            if (col.transform.root != part.transform.root) return false;
        }
        return true;
    }
}