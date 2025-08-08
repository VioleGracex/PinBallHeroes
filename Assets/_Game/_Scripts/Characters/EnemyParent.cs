using UnityEngine;

public class EnemyParent : MonoBehaviour
{
    // Stats with properties
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private int _currentHP = 100;
    [SerializeField] private int _attackDamage = 8;
    [SerializeField] private float _attackSpeed = 1.0f; // attacks per turn
    protected Player player;

    public int MaxHP { get => _maxHP; set => _maxHP = value; }
    public int CurrentHP { get => _currentHP; set => _currentHP = value; }
    public int AttackDamage { get => _attackDamage; set => _attackDamage = value; }
    public float AttackSpeed { get => _attackSpeed; set => _attackSpeed = value; }

    // Event for death notification
    public event System.Action<EnemyParent> OnDeath;

    protected virtual void Start()
    {
        CurrentHP = MaxHP;
        player = FindFirstObjectByType<Player>();
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

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} defeated!");
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}