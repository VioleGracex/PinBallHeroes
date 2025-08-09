using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Zenject;

public class TurnManager : MonoBehaviour
{
    [Inject]
    Player player;
    public List<EnemyParent> enemies = new List<EnemyParent>();
    public float turnDelay = 1.0f;
    public bool autoStart = true;
    [SerializeField]
    private ParallaxController parallaxController;
    [SerializeField]
    private WaveSpawner waveSpawner;
    [SerializeField]
    private TurnIndicatorUI turnIndicatorUI;

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
                Debug.LogWarning("[TurnManager] Player reference is still null!");
            else
                Debug.Log("[TurnManager] Player reference found via FindFirstObjectByType.");
        }
        Debug.Log("[TurnManager] Initialized with player: " + (player != null ? player.name : "null"));
        if (autoStart)
            StartCoroutine(RunTurns());
    }

    public void RegisterEnemy(EnemyParent enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
        enemy.OnDeath += OnEnemyDeath;
        enemy.OnFinishedActions += OnEnemyFinishedActions;
    }

    public void UnregisterEnemy(EnemyParent enemy)
    {
        enemies.Remove(enemy);
        enemy.OnDeath -= OnEnemyDeath;
        enemy.OnFinishedActions -= OnEnemyFinishedActions;
    }

    private int enemiesFinishedCount = 0;
    private void OnEnemyFinishedActions(EnemyParent enemy)
    {
        enemiesFinishedCount++;
    }

    private void OnEnemyDeath(EnemyParent enemy)
    {
        UnregisterEnemy(enemy);
    }

    public IEnumerator RunTurns()
    {
        while (player != null && player.CurrentHP > 0 && enemies.Count > 0)
        {
            // Player turn
            if (turnIndicatorUI != null) turnIndicatorUI.SetPlayerTurn();
            Debug.Log("[TurnManager] Player's turn begins.");
            bool playerFinished = false;
            System.Action<Player> onPlayerFinished = null;
            onPlayerFinished = (p) => { playerFinished = true; player.OnFinishedActions -= onPlayerFinished; };
            player.OnFinishedActions += onPlayerFinished;
            yield return StartCoroutine(player.PlayTurn(enemies));
            if (playerFinished)
                Debug.Log("[TurnManager] Player finished turn and performed actions.");
            else
                Debug.Log("[TurnManager] Player finished turn but did nothing.");
            // Wait until player signals finished
            while (!playerFinished && player != null && player.CurrentHP > 0) yield return null;
            yield return new WaitForSeconds(turnDelay);
            // Enemy turn
            if (turnIndicatorUI != null) turnIndicatorUI.SetEnemyTurn();
            yield return StartCoroutine(EnemiesTurn());
            yield return new WaitForSeconds(turnDelay);
        }
        if (turnIndicatorUI != null) turnIndicatorUI.Hide();
        string reason = "[TurnManager] Combat ended: ";
        if (player == null)
            reason += "Player object is null (destroyed or not found).";
        else if (player.CurrentHP <= 0)
            reason += "Player defeated (HP <= 0).";
        else if (enemies.Count == 0 || enemies.All(e => e == null))
            reason += "All enemies defeated or removed.";
        else
            reason += "Unknown reason (possible bug).";
        Debug.Log(reason);
    }

    private EnemyParent GetLowestHPEnemy()
    {
        if (enemies.Count == 0) return null;
        return enemies.OrderBy(e => e.CurrentHP).FirstOrDefault();
    }

    private IEnumerator EnemiesTurn()
    {
        // If no enemies are alive, trigger next wave and post-wave logic
        if (enemies == null || enemies.Count == 0 || enemies.All(e => e == null))
        {
            Debug.Log("[TurnManager] No enemies alive at start of enemy turn. Spawning next wave.");

            if (waveSpawner != null)
            {
                waveSpawner.StartNextWave();
                Debug.Log("[TurnManager] Called StartNextWave on WaveSpawner.");
                // Wait a frame for new enemies to spawn
                yield return null;
                // Move parallax for 2 seconds
                if (parallaxController != null)
                {
                    parallaxController.MoveParallax(2f);
                    Debug.Log("[TurnManager] Parallax moving for 2 seconds.");
                    yield return new WaitForSeconds(2f);
                }
                // Pinball game placeholder
                Debug.Log("[TurnManager] Pinball game (not implemented)");
                // Power card choose placeholder
                Debug.Log("[TurnManager] Power card choose (not implemented)");
            }
            else
            {
                Debug.LogWarning("[TurnManager] No WaveSpawner found in scene!");
            }
            yield break;
        }
        enemiesFinishedCount = 0;
        int livingEnemies = enemies.Count(e => e != null);
        foreach (var enemy in enemies.ToList())
        {
            if (enemy == null) continue;
            enemy.ResetTurnState();
            Debug.Log($"[TurnManager] Enemy turn: {enemy.gameObject.name}");
            bool enemyDidAnything = false;
            System.Action<EnemyParent> onEnemyFinished = null;
            onEnemyFinished = (e) => { enemyDidAnything = true; enemy.OnFinishedActions -= onEnemyFinished; };
            enemy.OnFinishedActions += onEnemyFinished;
            enemy.TakeTurn();
            // Wait for enemy to finish (event-driven)
            while (!enemyDidAnything && enemy != null)
                yield return null;
            if (enemyDidAnything)
                Debug.Log($"[TurnManager] {enemy.gameObject.name} finished turn and performed actions.");
            else
                Debug.Log($"[TurnManager] {enemy.gameObject.name} finished turn but did nothing.");
        }
        // Wait until all living enemies have finished their actions or are destroyed
        while (enemiesFinishedCount < livingEnemies)
        {
            if (player == null || player.CurrentHP <= 0) yield break;
            if (enemies.Count(e => e != null) == 0) yield break;
            yield return null;
        }
    }
}
