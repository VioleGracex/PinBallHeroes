using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

public class CharactersSpawner : MonoBehaviour
{
    #region Fields
    [Header("Player")]
    public GameObject playerPrefab;
    [Tooltip("If zero, player spawns at camera left edge")] public float playerSpawnX = 0f;
    [Tooltip("If zero, player spawns at baseY")] public float playerSpawnY = 0f;
    [ReadOnly]
    public GameObject playerInstance;

    [Header("Enemy")]
    public GameObject enemyPrefab;
    [Tooltip("If zero, will use camera right edge")] public float enemySpawnX = 0f; // X position for all enemy lanes (right side)
    private Camera mainCamera;
    public float enemyLanesYSpacing = 2.5f; // Distance between lanes
    public float enemyLanesYOffset = 0f; // Center offset for lanes
    [ReadOnly]
    public List<List<GameObject>> enemyInstances = new List<List<GameObject>>()
    {
        new List<GameObject>(), // top
        new List<GameObject>(), // middle
        new List<GameObject>()  // bottom
    };
    [Header("Max Enemies Per Lane")]
    [Min(1)]
    public int maxEnemiesPerLane = 1;

    [Header("Parent")]
    [Tooltip("Optional parent for all spawned characters (player and enemies)")]
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
        yLanes[0] = baseY + enemyLanesYSpacing;
        yLanes[1] = baseY;
        yLanes[2] = baseY - enemyLanesYSpacing;

        Gizmos.color = Color.red;
        for (int i = 0; i < 3; i++)
        {
            Gizmos.DrawWireSphere(new Vector3(spawnX, yLanes[i], 0f), 0.1f); //enemy spawn point
        }
        // Player spawn point (left side)
        float leftX = mainCamera.transform.position.x - worldScreenWidth / 2f;
        float px = leftX + playerSpawnX;
        float py = baseY + playerSpawnY;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(px, py, 0f), 0.1f); //player spawn point
    }
    #endregion

    #region Spawning
    [Button("Spawn Player")]
    public void SpawnPlayer()
    {
        if (playerInstance != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(playerInstance);
            else
                Destroy(playerInstance);
#else
            Destroy(playerInstance);
#endif
        }
        if (playerPrefab != null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
            float leftX = mainCamera.transform.position.x - worldScreenWidth / 2f;
            float px = leftX + playerSpawnX;
            float baseY = transform.position.y + enemyLanesYOffset;
            float py = baseY + playerSpawnY;
            Vector3 spawnPos = new Vector3(px, py, 0f);
            playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity, parent);
            // Set player sort order highest
            var sr = playerInstance.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = 20;
        }
    }

    public GameObject SpawnEnemy(GameObject prefab)
    {
        // Calculate Y positions for 3 lanes and X based on camera
        if (mainCamera == null) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float rightX = mainCamera.transform.position.x + worldScreenWidth / 2f;
        float spawnX = (enemySpawnX == 0f) ? rightX : rightX + enemySpawnX;
        float baseY = transform.position.y + enemyLanesYOffset;
        float[] yLanes = new float[3];
        yLanes[0] = baseY; // middle
        yLanes[1] = baseY - enemyLanesYSpacing; // bottom
        yLanes[2] = baseY + enemyLanesYSpacing; // top

        // Try to spawn in first available lane: middle, bottom, top
        for (int i = 0; i < 3; i++)
        {
            int laneIdx = (i == 0) ? 0 : (i == 1 ? 1 : 2); // 0:middle, 1:bottom, 2:top
            // Remove nulls from lane
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
                        else if (laneIdx == 1) sr.sortingOrder = 15; // bottom
                        else if (laneIdx == 2) sr.sortingOrder = 0; // top
                    }
                    enemyInstances[laneIdx].Add(go);
                    return go;
                }
            }
        }
        // All lanes busy
        return null;
    }

    [Button("Spawn Default Enemy")]
    public GameObject SpawnDefaultEnemy()
    {
        // Calculate Y positions for 3 lanes and X based on camera
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
            // Remove nulls from lane
            enemyInstances[laneIdx].RemoveAll(x => x == null);
            if (enemyInstances[laneIdx].Count < maxEnemiesPerLane)
            {
                if (enemyPrefab != null)
                {
                    Vector3 pos = new Vector3(spawnX, yLanes[laneIdx], 0f);
                    var go = Instantiate(enemyPrefab, pos, Quaternion.identity, parent);
                    // Set enemy sort order: top=0, middle=10, bottom=15
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr)
                    {
                        if (laneIdx == 0) sr.sortingOrder = 10; // middle
                        else if (laneIdx == 1) sr.sortingOrder = 0; // top
                        else if (laneIdx == 2) sr.sortingOrder = 15; // bottom
                    }
                    enemyInstances[laneIdx].Add(go);
                    return go;
                }
            }
        }
        // All lanes busy
        return null;
    }
    #endregion
}
