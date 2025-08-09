using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    #region Fields
    [Header("Enemy")]
    public GameObject enemyPrefab;
    [Tooltip("If zero, will use camera right edge")] public float enemySpawnX = 0f; // X position for all enemy lanes (right side)
    private Camera mainCamera;
    public float enemyLanesYSpacing = 2.5f; // Distance between lanes
    public float enemyLanesYOffset = 0f; // Center offset for lanes
    [ReadOnly]
    public List<List<GameObject>> enemyInstances = new List<List<GameObject>>()
    {
        new List<GameObject>(), // middle
        new List<GameObject>(), // top
        new List<GameObject>()  // bottom
    };
    [Header("Max Enemies Per Lane")]
    [Min(1)]
    public int maxEnemiesPerLane = 1;

    [Header("Parent")]
    [Tooltip("Optional parent for all spawned enemies")]
    public Transform parent;
    #endregion

    #region Gizmos
    void OnDrawGizmos()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float rightX = mainCamera.transform.position.x + worldScreenWidth / 2f;
        float spawnX = (enemySpawnX == 0f) ? rightX : rightX + enemySpawnX;

        float baseY = transform.position.y + enemyLanesYOffset;
        float[] yLanes = new float[3];
        yLanes[0] = baseY; // middle
        yLanes[1] = baseY + enemyLanesYSpacing; // top
        yLanes[2] = baseY - enemyLanesYSpacing; // bottom

        Gizmos.color = Color.red;
        for (int i = 0; i < 3; i++)
        {
            Gizmos.DrawWireSphere(new Vector3(spawnX, yLanes[i], 0f), 0.1f); //enemy spawn point
        }
    }
    #endregion

    

    #region Spawning
    public GameObject SpawnEnemy(GameObject prefab)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float rightX = mainCamera.transform.position.x + worldScreenWidth / 2f;
        float spawnX = (enemySpawnX == 0f) ? rightX : rightX + enemySpawnX;
        float baseY = transform.position.y + enemyLanesYOffset;
        float[] yLanes = new float[3];
        yLanes[0] = baseY; // middle
        yLanes[1] = baseY + enemyLanesYSpacing; // top
        yLanes[2] = baseY - enemyLanesYSpacing; // bottom

        // Try to spawn in first available lane: middle, top, bottom
        for (int i = 0; i < 3; i++)
        {
            int laneIdx = i; // 0:middle, 1:top, 2:bottom
            enemyInstances[laneIdx].RemoveAll(x => x == null);
            if (enemyInstances[laneIdx].Count < maxEnemiesPerLane)
            {
                if (prefab != null)
                {
                    Vector3 pos = new Vector3(spawnX, yLanes[laneIdx], 0f);
                    var go = Instantiate(prefab, pos, Quaternion.identity, parent);
                    // Set enemy sort order: top=0, middle=10, bottom=15
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr)
                    {
                        if (laneIdx == 0) sr.sortingOrder = 10; // middle
                        else if (laneIdx == 1) sr.sortingOrder = 0; // top
                        else if (laneIdx == 2) sr.sortingOrder = 15; // bottom
                    }
                    enemyInstances[laneIdx].Add(go);
                    go.name = $"Enemy_Lane{laneIdx}_Idx{enemyInstances[laneIdx].Count - 1}";
                    Debug.Log($"[EnemySpawner] Spawned {go.name} at lane {laneIdx}, index {enemyInstances[laneIdx].Count - 1}, position {pos}");
                    // Register with TurnManager
                    var enemyParent = go.GetComponent<EnemyParent>();
                    var turnManager = FindFirstObjectByType<TurnManager>();
                    if (enemyParent != null && turnManager != null)
                    {
                        turnManager.RegisterEnemy(enemyParent);
                        Debug.Log($"[EnemySpawner] Registered {go.name} with TurnManager.");
                    }
                    return go;
                }
            }
        }
        return null;
    }

    [Button("Spawn Default Enemy")]
    public GameObject SpawnDefaultEnemy()
    {
        return SpawnEnemy(enemyPrefab);
    }
    #endregion
}