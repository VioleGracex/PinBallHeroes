using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

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

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    [Header("Projectile Fire Point Offset (local)")]
    public Vector2 firePointOffset = new Vector2(0.5f, 0.2f);

    public event System.Action<Player> OnFinishedActions;

    private Vector3? _originalPosition = null;
    public int behaviorPattern = 1; // For future expansion

    private bool _hasSpawned = false;

    private IEnumerator Start()
    {
        _currentHP = _maxHP;
        if (!_hasSpawned)
        {
            Vector3 target = transform.position + Vector3.right * 0.7f;
            yield return StartCoroutine(MoveToPosition(target, 0.01f));
            _hasSpawned = true;
        }
    }

    // Returns how many attacks the player can do this turn
    public int GetAttacksPerTurn()
    {
        return Mathf.FloorToInt(_attackSpeed);
    }

    // Call this for each attack (from turn logic)
    public void Attack(EnemyParent target)
    {
        if (target != null && projectilePrefab != null)
        {
            Vector3 firePoint = transform.position + (Vector3)firePointOffset;
            StartCoroutine(ShootProjectileAtTarget(target, false, firePoint));
        }
        else if (target != null)
        {
            // fallback: instant damage
            target.TakeDamage(_attackDamage);
        }
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

    // Called by TurnManager to start the player's turn
    public IEnumerator PlayTurn(List<EnemyParent> enemies)
    {
        if (enemies == null || enemies.Count == 0 || enemies.All(e => e == null))
        {
            Debug.Log("[Player] No enemies alive, ending turn immediately.");
            OnFinishedActions?.Invoke(this);
            yield break;
        }
        int attacks = GetAttacksPerTurn();
        for (int i = 0; i < attacks; i++)
        {
            EnemyParent target = GetLowestHPEnemy(enemies);
            if (target != null)
            {
                if (behaviorPattern == 1)
                {
                    if (!_originalPosition.HasValue) _originalPosition = transform.position;
                    Vector3 backPos = transform.position + Vector3.left * 2f;
                    yield return StartCoroutine(MoveToPosition(backPos, 0.01f));
                    bool arc = target.transform.position.y > transform.position.y;
                    Vector3 firePoint = transform.position + (Vector3)firePointOffset;
                    yield return StartCoroutine(ShootProjectileAtTarget(target, arc, firePoint));
                    yield return StartCoroutine(MoveToPosition(_originalPosition.Value, 0.01f));
                }
                else
                {
                    Attack(target);
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
        OnFinishedActions?.Invoke(this);
    }

    private EnemyParent GetLowestHPEnemy(List<EnemyParent> enemies)
    {
        if (enemies == null || enemies.Count == 0) return null;
        return enemies.Where(e => e != null).OrderBy(e => e.CurrentHP).FirstOrDefault();
    }

    private IEnumerator MoveToPosition(Vector3 target, float stopDistance)
    {
        while (Vector3.Distance(transform.position, target) > stopDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, 10f * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator ShootProjectileAtTarget(EnemyParent target, bool arc, Vector3 firePoint)
    {
        if (projectilePrefab == null || target == null) yield break;
        Vector3 start = firePoint;
        Vector3 end = target.transform.position;
        float duration = 0.3f;
        float t = 0f;
        GameObject proj = Instantiate(projectilePrefab, start, Quaternion.identity);
        if (arc)
        {
            Vector3 peak = (start + end) / 2f + Vector3.up * 2f;
            while (t < 1f && proj != null && target != null)
            {
                t += Time.deltaTime / duration;
                Vector3 a = Vector3.Lerp(start, peak, t);
                Vector3 b = Vector3.Lerp(peak, end, t);
                proj.transform.position = Vector3.Lerp(a, b, t);
                yield return null;
            }
        }
        else
        {
            while (t < 1f && proj != null && target != null)
            {
                t += Time.deltaTime / duration;
                proj.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }
        if (proj != null) Destroy(proj);
        if (target != null) target.TakeDamage(_attackDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 firePoint = transform.position + (Vector3)firePointOffset;
        Gizmos.DrawWireSphere(firePoint, 0.05f);
    }
}