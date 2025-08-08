using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private int _currentHP = 100;
    [SerializeField] private int _attackDamage = 10;
    [SerializeField] private float _attackSpeed = 1.0f; // Attacks per turn

    public int MaxHP { get => _maxHP; set => _maxHP = value; }
    public int CurrentHP { get => _currentHP; set => _currentHP = value; }
    public int AttackDamage { get => _attackDamage; set => _attackDamage = value; }
    public float AttackSpeed { get => _attackSpeed; set => _attackSpeed = value; }

    private void Start()
    {
        _currentHP = _maxHP;
    }

    // Returns how many attacks the player can do this turn
    public int GetAttacksPerTurn()
    {
        return Mathf.FloorToInt(_attackSpeed);
    }

    // Call this for each attack (from turn logic)
    public void Attack(EnemyParent target)
    {
        if (target != null)
            target.TakeDamage(_attackDamage);
    }

    public void TakeDamage(int damage)
    {
        _currentHP -= damage;
        if (_currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        _currentHP += amount;
        if (_currentHP > _maxHP)
            _currentHP = _maxHP;
    }

    private void Die()
    {
        Debug.Log("Player defeated!");
        // Add defeat logic
    }
}