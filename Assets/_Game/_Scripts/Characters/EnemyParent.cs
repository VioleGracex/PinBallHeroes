using UnityEngine;
using Zenject;

public class EnemyParent : MonoBehaviour
{
    // Stats with properties
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private int _currentHP = 100;
    [SerializeField] private int _attackDamage = 8;
    [SerializeField] private float _attackSpeed = 1.0f; // attacks per turn
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
        if (CurrentHP <= 0)
            Die();
    }

    public virtual void Heal(int amount)
    {
        CurrentHP += amount;
        if (CurrentHP > MaxHP)
            CurrentHP = MaxHP;
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

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} defeated!");
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}