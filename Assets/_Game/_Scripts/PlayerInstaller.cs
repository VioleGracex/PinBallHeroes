using UnityEngine;
using Zenject;

public class PlayerInstaller : MonoInstaller
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;
    private Player _playerInstance;
    #region Player Spawn Info
    [Header("Player Spawn Settings")]
    [Tooltip("If zero, player spawns at camera left edge")] public float playerSpawnX = 0f;
    [Tooltip("If zero, player spawns at baseY")] public float playerSpawnY = 0f;
    private Camera mainCamera;

    [SerializeField] private Transform parent; // Optional parent for player instance

    public Vector3 GetPlayerSpawnPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float leftX = mainCamera.transform.position.x - worldScreenWidth / 2f;
        float baseY = transform.position.y;
        float px = leftX + playerSpawnX;
        float py = baseY + playerSpawnY;
        return new Vector3(px, py, 0f);
    }

    private void OnDrawGizmos()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        Vector3 spawnPos = GetPlayerSpawnPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPos, 4f);
    }
    #endregion

    public override void InstallBindings()
    {
        // Only bind, do not instantiate here to avoid Zenject warning
        Container.Bind<Player>().FromMethod(_ => _playerInstance).AsSingle();
    }

    private void Awake()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerInstaller: playerPrefab not assigned!");
            return;
        }
        Vector3 spawnPos = GetPlayerSpawnPosition();
        _playerInstance = Container.InstantiatePrefabForComponent<Player>(playerPrefab, spawnPos, Quaternion.identity, parent);
        if (_playerInstance == null)
        {
            Debug.LogError("PlayerInstaller: Player prefab does not have a Player component!");
            return;
        }
        // Set player sort order highest
        var sr = _playerInstance.GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = 20;
        
    }
}