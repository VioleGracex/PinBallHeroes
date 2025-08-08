using UnityEngine;

public class Player : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP = 100;
    public int attackDamage = 10;
    public float attackSpeed = 1.5f; // Attacks per turn

    private void Start()
    {
        currentHP = maxHP;
    }

    // Returns how many attacks the player can do this turn
    public int GetAttacksPerTurn()
    {
        return Mathf.FloorToInt(attackSpeed);
    }

    // Call this for each attack (from turn logic)
    public void Attack(EnemyParent target)
    {
        if (target != null)
            target.TakeDamage(attackDamage);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP)
            currentHP = maxHP;
    }

    private void Die()
    {
        Debug.Log("Player defeated!");
        // Add defeat logic
    }
}