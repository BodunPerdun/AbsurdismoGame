using UnityEngine;
using System.Collections.Generic;



public class DungeonPart : MonoBehaviour {
    public List<EntryPoint> entryPoints;
   
    [Header("Настройки генерации")]
    [Range(0, 100)] public float spawnWeight = 50f; 
    public int maxBranches = 2; 
    [Header("Настройки модуля")]
    public PartType type; // Теперь это не будет гореть красным
    
    [Header("Ссылки")]
    public BoxCollider roomCollider; 

    
    [HideInInspector] public int currentBranches = 0;

    public bool CanSpawnMore() => currentBranches < maxBranches;

    public bool HasAvailableEntryPoint(out EntryPoint foundPoint) {
        foundPoint = null;
        foreach (var point in entryPoints) {
            if (!point.isOccupied) {
                foundPoint = point;
                return true;
            }
        }
        return false;
    }
}