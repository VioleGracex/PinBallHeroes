using UnityEngine;
using Zenject;
using System.Collections;

public class EnemyParent : MonoBehaviour
{
    [Header("Currency")]
    [Tooltip("How much currency this enemy drops on death.")]
    public int currencyOnKill = 1;
    // Stats with properties
    [SerializeField] private int _maxHP = 100;
    private int _currentHP = 100;
    [SerializeField] private int _attackDamage = 8;
    [SerializeField] private float _attackSpeed = 1.0f; // attacks per turn

    [Header("Currency Visuals")]
    [Tooltip("Prefab for the currency (pinball) to spawn on drop.")]
    public GameObject currencyPrefab;
    [Inject]
    protected Player player;

    public int MaxHP { get => _maxHP; set => _maxHP = value; }
    public int CurrentHP { get => _currentHP; set => _currentHP = value; }
    public int AttackDamage { get => _attackDamage; set => _attackDamage = value; }
    public float AttackSpeed { get => _attackSpeed; set => _attackSpeed = value; }

    // Event for death notification
    public event System.Action<EnemyParent> OnDeath;
    // Event for notifying TurnManager when this enemy finishes all actions
    public event System.Action<EnemyParent> OnFinishedActions;

    public bool ReadyToAttack { get; protected set; } = true;
    public bool FinishedActions { get; protected set; } = false;


    [Header("UI")]
    [SerializeField]
    private HealthBarUI healthBarUI;



    protected virtual void Start()
    {
        CurrentHP = MaxHP;
        // player is injected by Zenject
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
                Debug.LogWarning($"[EnemyParent] Player reference is still null on {gameObject.name}!");
            else
                Debug.Log($"[EnemyParent] Player reference found via FindFirstObjectByType on {gameObject.name}.");
        }
            // Ensure HealthBarUI is updated at start
            if (healthBarUI != null)
            {
                healthBarUI.SetHP(CurrentHP, MaxHP);
            }
    }

    // Used by TurnManager to process this enemy's turn
    public virtual void TakeTurn()
    {
        int attacks = GetAttacksPerTurn();
        for (int i = 0; i < attacks; i++)
        {
            Attack(player);
        }
    }

    public virtual int GetAttacksPerTurn()
    {
        return Mathf.FloorToInt(AttackSpeed);
    }

    public virtual void Attack(Player target)
    {
        if (target != null)
        {
            target.TakeDamage(AttackDamage);
            Debug.Log($"{gameObject.name} attacks player for {AttackDamage} damage.");
        }
    }

    public virtual void TakeDamage(int damage)
    {
        CurrentHP -= damage;
        if (healthBarUI != null)
        {
            healthBarUI.SetHP(CurrentHP, MaxHP);
            healthBarUI.ShowDamage(damage);
        }
        // Drop currency on hit (arc drop behind enemy)
        if (damage > 0)
        {
            SpawnCurrencyOnDamage(1);
        }
        if (CurrentHP <= 0)
            Die();
    }

    public virtual void Heal(int amount)
    {
        CurrentHP += amount;
        if (CurrentHP > MaxHP)
            CurrentHP = MaxHP;
        if (healthBarUI != null)
        {
            healthBarUI.SetHP(CurrentHP, MaxHP);
        }
    }

    // Call this when the enemy is ready to attack (e.g. after move/animation)
    public virtual void SetReadyToAttack()
    {
        ReadyToAttack = true;
    }

    // Call this when the enemy has finished all actions (e.g. after attack animation)
    public virtual void SetFinishedActions()
    {
        FinishedActions = true;
        OnFinishedActions?.Invoke(this);
    }

    // Call this to reset turn state at the start of a turn
    public void ResetTurnState()
    {
        ReadyToAttack = false;
        FinishedActions = false;
    }

    /// <summary>
    /// Spawns a currency drop at this enemy's position.
    /// </summary>
    /// <param name="type">Type of drop: OnDamage (behind), OnDeath (in place, bounce)</param>
    /// <param name="amount">How many currency to spawn (default 1)</param>



    private IEnumerator AnimateArcDrop(Transform obj, Vector3 start, Vector3 end, float arcHeight, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float height = Mathf.Sin(Mathf.PI * t) * arcHeight;
            obj.position = Vector3.Lerp(start, end, t) + Vector3.up * height;
            yield return null;
        }
        obj.position = end;
    }

    private IEnumerator AnimateBounceDrop(Transform obj, Vector3 start, float floorY, float bounceHeight, float duration)
    {
        float t = 0f;
        Vector3 end = new Vector3(start.x, floorY, start.z);
        bool bounced = false;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float y;
            if (!bounced && t < 0.5f)
            {
                // Fall down
                y = Mathf.Lerp(start.y, floorY + bounceHeight, t * 2f);
            }
            else
            {
                if (!bounced) { t = 0.5f; bounced = true; }
                // Bounce up and settle
                float bounceT = (t - 0.5f) * 2f;
                y = Mathf.Lerp(floorY + bounceHeight, floorY, bounceT);
            }
            obj.position = new Vector3(start.x, y, start.z);
            yield return null;
        }
        obj.position = end;
    }


    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} defeated!");
        // Drop currency on death (drop in place, bounce)
    SpawnCurrencyOnDeath(currencyOnKill);
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }

    public void SpawnCurrencyOnDamage(int amount = 1)
    {
        if (currencyPrefab == null)
        {
            Debug.LogWarning($"[EnemyParent] No currencyPrefab assigned!");
            return;
        }
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = transform.position;
            GameObject currency = Instantiate(currencyPrefab, spawnPos, Quaternion.identity);
            // Drop slightly behind the enemy (relative to facing direction, here -X)
            Vector3 targetPos = spawnPos + Vector3.left * 0.7f + Vector3.up * 0.2f;
            float arcHeight = 0.5f;
            float duration = 0.5f;
            StartCoroutine(AnimateArcDrop(currency.transform, spawnPos, targetPos, arcHeight, duration));
        }
    }

    public void SpawnCurrencyOnDeath(int amount = 1)
    {
        if (currencyPrefab == null)
        {
            Debug.LogWarning($"[EnemyParent] No currencyPrefab assigned!");
            return;
        }
        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = transform.position;
            GameObject currency = Instantiate(currencyPrefab, spawnPos, Quaternion.identity);
            // Drop at head, animate bounce to floor
            Vector3 headPos = spawnPos + Vector3.up * 1.0f;
            currency.transform.position = headPos;
            float floorY = spawnPos.y;
            float bounceHeight = 0.7f;
            float duration = 0.7f;
            StartCoroutine(AnimateBounceDrop(currency.transform, headPos, floorY, bounceHeight, duration));
        }
    }
}


    