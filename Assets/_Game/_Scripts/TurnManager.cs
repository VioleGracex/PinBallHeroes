using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Zenject;

public class TurnManager : MonoBehaviour
{
    #region Fields and Game State
    [Inject]
    Player player;
    public List<EnemyParent> enemies = new List<EnemyParent>();
    public float turnDelay = 1.0f;
    public bool autoStart = true;
    [SerializeField]
    private ParallaxController parallaxController;
    [Header("Camera")]
    [SerializeField]
    private CameraController cameraController;
    [SerializeField]
    private WaveSpawner waveSpawner;
    [SerializeField]
    private CardsManager cardsManager;
    [SerializeField]
    private TurnIndicatorUI turnIndicatorUI;
    [Header("Currency Collection")]
    [SerializeField]
    private CannonManager cannonManager;
    [SerializeField]
    private PinballManager pinballManager;
    [Header("Pause Menu")]
    [SerializeField]
    private GameObject pauseMenu;
    private enum GameMode { Combat, Pinball, CardSelect }
    private GameMode currentMode = GameMode.Combat;
    private int enemiesFinishedCount = 0;
   
    #endregion

    #region Unity Lifecycle
    private IEnumerator Start()
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
        if (waveSpawner != null)
            waveSpawner.OnWaveCleared += OnWaveCleared;

        yield return new WaitForSeconds(2f);            
        if (autoStart)
            StartCoroutine(StartCombatMode());
    }

    private void OnDestroy()
    {
        if (waveSpawner != null)
            waveSpawner.OnWaveCleared -= OnWaveCleared;
    }
    #endregion

    #region Combat Mode
    private IEnumerator StartCombatMode()
    {
        currentMode = GameMode.Combat;
        // Move camera to combat position and wait
        if (cameraController != null)
            yield return StartCoroutine(cameraController.MoveToCombatPosition());

        if (parallaxController != null)
        {
            parallaxController.MoveParallax(2f);
            Debug.Log("[TurnManager] Parallax moving for 2 seconds.");
            yield return new WaitForSeconds(4f);
        }
        // Only start a new wave if there are no enemies
        if (enemies.Count == 0 && waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
        yield return StartCoroutine(TurnCycle());
    }

    private IEnumerator TurnCycle()
    {
        while (player != null && player.CurrentHP > 0 && enemies.Count > 0)
        {
            yield return StartCoroutine(PlayerTurn());
            yield return new WaitForSeconds(turnDelay);
            if (turnIndicatorUI != null) turnIndicatorUI.SetEnemyTurn();
            yield return StartCoroutine(EnemiesTurn());
            yield return new WaitForSeconds(turnDelay);
        }
        EndCombat();
    }

    private void EndCombat()
    {
        if (turnIndicatorUI != null) turnIndicatorUI.Hide();
        Debug.Log(GetCombatEndReason());
        CheckCombatEnd();
    }

    private void CheckCombatEnd()
    {
        if (player == null || player.CurrentHP <= 0)
            return;
        if (cannonManager != null)
        {
            cannonManager.OnReturnedToOriginalPosition += OnCannonReturned;
            StartCoroutine(cannonManager.CollectAllCurrencyToCannon());
        }
        else
        {
            StartPinballMode();
        }
    }

    private void OnCannonReturned()
    {
        if (cannonManager != null)
            cannonManager.OnReturnedToOriginalPosition -= OnCannonReturned;
        StartPinballMode();
    }
    #endregion

    #region Pinball Mode

    private IEnumerator PinballModeRoutine()
    {
        currentMode = GameMode.Pinball;
        // Move camera to pinball position and wait
        if (cameraController != null)
            yield return StartCoroutine(cameraController.MoveToPinballPosition());
        if (pinballManager != null && cannonManager != null)
        {
            pinballManager.StartPinballMode();
        }   
    }
    private void StartPinballMode()
    {
        if (pinballManager != null)
        {
            pinballManager.OnPinballModeEnd -= OnPinballModeEndHandler; // Ensure no duplicate
            pinballManager.OnPinballModeEnd += OnPinballModeEndHandler;
        }
        StartCoroutine(PinballModeRoutine());
    }

    private void OnPinballModeEndHandler()
    {
        if (pinballManager != null)
            pinballManager.OnPinballModeEnd -= OnPinballModeEndHandler;
        StartCardMode();
    }

    #endregion

    #region Card Select Mode
 
    private void StartCardMode()
    {
        StartCoroutine(CardModeRoutine());
    }

    private IEnumerator CardModeRoutine()
    {
        currentMode = GameMode.CardSelect;
        // Move camera to card select position and wait
        if (cameraController != null)
            yield return StartCoroutine(cameraController.MoveToCardSelectPosition());
        if (cardsManager != null)
        {
            cardsManager.OnCardSelectionEnded += OnCardSelectionEndedHandler;
            cardsManager.SpawnCards();
        }
    }

    private void OnCardSelectionEndedHandler()
    {
        if (cardsManager != null)
            cardsManager.OnCardSelectionEnded -= OnCardSelectionEndedHandler;
        Debug.Log("[TurnManager] Card select mode ended. Returning to combat.");
        StartCoroutine(StartCombatMode());
    }
    #endregion

    #region Turn Flow Utility Methods
    private IEnumerator PlayerTurn()
    {
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
        while (!playerFinished && player != null && player.CurrentHP > 0) yield return null;
    }

    private IEnumerator EnemiesTurn()
    {
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
            while (!enemyDidAnything && enemy != null)
                yield return null;
            if (enemyDidAnything)
                Debug.Log($"[TurnManager] {enemy.gameObject.name} finished turn and performed actions.");
            else
                Debug.Log($"[TurnManager] {enemy.gameObject.name} finished turn but did nothing.");
        }
        while (enemiesFinishedCount < livingEnemies)
        {
            if (player == null || player.CurrentHP <= 0) yield break;
            if (enemies.Count(e => e != null) == 0) yield break;
            yield return null;
        }
    }
    #endregion

    #region Enemy Registration
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

    private void OnEnemyFinishedActions(EnemyParent enemy)
    {
        enemiesFinishedCount++;
    }

    private void OnEnemyDeath(EnemyParent enemy)
    {
        UnregisterEnemy(enemy);
    }
    #endregion

    #region Wave Events
    private void OnWaveCleared()
    {
        StopAllCoroutines();
        EndCombat();
    }
    #endregion

    #region Utility
    private string GetCombatEndReason()
    {
        string reason = "[TurnManager] Combat ended: ";
        if (player == null)
            reason += "Player object is null (destroyed or not found).";
        else if (player.CurrentHP <= 0)
            reason += "Player defeated (HP <= 0).";
        else if (enemies.Count == 0 || enemies.All(e => e == null))
            reason += "All enemies defeated or removed.";
        else
            reason += "Unknown reason (possible bug).";
        return reason;
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }
        Debug.Log("[TurnManager] Game Over! Game paused and pause menu shown.");
    }
    #endregion
}