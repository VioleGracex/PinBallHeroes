using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using System.Linq;

[System.Serializable]
public class WaveUnit
{
    public GameObject prefab;
    [Range(0f, 1f)] public float spawnChance = 0.5f;
}

public class WaveSpawner : MonoBehaviour
{
    public enum WaveMode { Infinite, Level }

    [Header("Wave Settings")]
    public WaveMode mode = WaveMode.Infinite;

    public List<WaveUnit> units;
    [Tooltip("Spawner to use for enemy placement")] public EnemySpawner enemySpawner;

    private List<GameObject> aliveEnemies = new List<GameObject>();
    private int currentWave = 0;

    private void Start()
    {
        if (mode == WaveMode.Infinite)
            StartNextWave();
    }

    [Button("Start Next Wave")]
    public void StartNextWave()
    {
        currentWave++;
        aliveEnemies.Clear();
        SpawnWave();
    }

    #region Wave Spawning
    private void SpawnWave()
    {
        if (enemySpawner == null || units == null || units.Count == 0)
        {
            Debug.LogWarning("[WaveSpawner] No enemySpawner or units to spawn!");
            return;
        }
        int numToSpawn = 1;
        if (currentWave >= 4 && currentWave <= 6) numToSpawn = 2;
        else if (currentWave >= 7) numToSpawn = 3;
        Debug.Log($"[WaveSpawner] Spawning wave {currentWave} with {numToSpawn} units.");

        for (int i = 0; i < numToSpawn; i++)
        {
            // Pick a random unit from the list, respecting spawnChance
            var candidates = units.Where(u => u.prefab != null && Random.value <= u.spawnChance).ToList();
            if (candidates.Count == 0) candidates = units.Where(u => u.prefab != null).ToList();
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[WaveSpawner] No valid unit to spawn for slot {i + 1} in wave {currentWave}.");
                continue;
            }
            var unit = candidates[Random.Range(0, candidates.Count)];
            Debug.Log($"[WaveSpawner] Spawning unit: {unit.prefab.name} (chance: {unit.spawnChance})");
            var go = enemySpawner.SpawnEnemy(unit.prefab);
            if (go != null)
            {
                aliveEnemies.Add(go);
                var enemyParent = go.GetComponent<EnemyParent>();
                if (enemyParent != null)
                {
                    enemyParent.OnDeath += OnEnemyDeath;
                    Debug.Log($"[WaveSpawner] Registered OnDeath for {go.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[WaveSpawner] Failed to spawn enemy prefab: {unit.prefab.name}");
            }
        }
    }
    #endregion

    private void OnEnemyDeath(EnemyParent enemy)
    {
        aliveEnemies.Remove(enemy.gameObject);
        if (aliveEnemies.Count == 0)
        {
            // All enemies defeated: call pinball game, card choose, then next wave (not implemented)
            Debug.Log("All enemies defeated! (Pinball game, card choose, next wave TODO)");
            if (mode == WaveMode.Infinite)
                StartNextWave();
        }
    }

    // Helper for NaughtyAttributes
    private bool IsInfiniteMode() => mode == WaveMode.Infinite;
}
