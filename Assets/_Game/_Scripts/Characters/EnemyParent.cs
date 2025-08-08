using UnityEngine;

public class EnemyParent : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP = 100;
    public int attackDamage = 8;
    public float attackSpeed = 1.0f; // attacks per turn
    protected Player player;

    protected virtual void Start()
    {
        currentHP = maxHP;
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
        return Mathf.FloorToInt(attackSpeed);
    }

    public virtual void Attack(Player target)
    {
        if (target != null)
        {
            target.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} attacks player for {attackDamage} damage.");
        }
    }

    public virtual void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
            Die();
    }

    public virtual void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP)
            currentHP = maxHP;
    }

    protected virtual void Die()
    {
        Debug.Log($"{gameObject.name} defeated!");
        Destroy(gameObject);
    }
}