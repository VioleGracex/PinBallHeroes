using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

[System.Serializable]
public class WaveUnit
{
    public GameObject prefab;
    [Range(0f, 1f)] public float spawnChance = 1f;
}

public class WaveSpawner : MonoBehaviour
{
    public enum WaveMode { Infinite, Level }

    [Header("Wave Settings")]
    public WaveMode mode = WaveMode.Infinite;
    [ShowIf("IsInfiniteMode")]
    public bool infiniteMode = true;
    [ReorderableList]
    public List<WaveUnit> units = new List<WaveUnit>();
    [Tooltip("Spawner to use for enemy placement")] public CharactersSpawner charactersSpawner;

    private List<GameObject> aliveEnemies = new List<GameObject>();
    private int currentWave = 0;

    private void Start()
    {
        if (mode == WaveMode.Infinite || infiniteMode)
            StartNextWave();
    }

    [Button("Start Next Wave")]
    public void StartNextWave()
    {
        currentWave++;
        aliveEnemies.Clear();
        SpawnWave();
    }

    private void SpawnWave()
    {
        if (charactersSpawner == null || units.Count == 0) return;
        foreach (var unit in units)
        {
            if (unit.prefab == null) continue;
            if (Random.value <= unit.spawnChance)
            {
                var go = charactersSpawner.SpawnEnemy(unit.prefab);
                if (go != null)
                {
                    aliveEnemies.Add(go);
                    var enemyParent = go.GetComponent<EnemyParent>();
                    if (enemyParent != null)
                        enemyParent.OnDeath += OnEnemyDeath;
                }
            }
        }
    }

    private void OnEnemyDeath(EnemyParent enemy)
    {
        aliveEnemies.Remove(enemy.gameObject);
        if (aliveEnemies.Count == 0)
        {
            // All enemies defeated: call pinball game, card choose, then next wave (not implemented)
            Debug.Log("All enemies defeated! (Pinball game, card choose, next wave TODO)");
            if (mode == WaveMode.Infinite || infiniteMode)
                StartNextWave();
        }
    }

    // Helper for NaughtyAttributes
    private bool IsInfiniteMode() => mode == WaveMode.Infinite || infiniteMode;
}
