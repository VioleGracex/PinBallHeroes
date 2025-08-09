using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Currency")]
    [Tooltip("Chance (0-1) to drop currency on attack (e.g. 0.2 for 20%)")]
    [Range(0f,1f)]
    public float currencyDropChance = 0.2f;
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private int _currentHP = 100;
    [SerializeField] private int _attackDamage = 10;
    [SerializeField] private float _attackSpeed = 1.0f; // Attacks per turn
    [SerializeField] private int _armor = 0;

    public int MaxHP { get => _maxHP; set => _maxHP = value; }
    public int CurrentHP { get => _currentHP; set => _currentHP = value; }

    public void UpdateHealthBar()
    {
        if (CurrentHP > MaxHP)
                CurrentHP = MaxHP;
        if (healthBarUI != null)
        {
            healthBarUI.SetHP(_currentHP, _maxHP);
        }
    }
    public int AttackDamage { get => _attackDamage; set => _attackDamage = value; }
    public float AttackSpeed { get => _attackSpeed; set => _attackSpeed = value; }
    public int Armor { get => _armor; set => _armor = value; }

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    [Header("Projectile Fire Point Offset (local)")]
    public Vector2 firePointOffset = new Vector2(0.5f, 0.2f);
    [Header("Projectile Travel Time (seconds)")]
    [SerializeField]
    private float projectileTravelTime = 1f;

    [Header("UI")]
    [SerializeField]
    private HealthBarUI healthBarUI;

    public event System.Action<Player> OnFinishedActions;

    private Vector3? _originalPosition = null;
    public int behaviorPattern = 1; // For future expansion

    private bool _hasSpawned = false;
    private bool readyToPlayTurn = false;

    private IEnumerator Start()
    {
        _currentHP = _maxHP;
        readyToPlayTurn = false;
            // Ensure HealthBarUI is updated at start
            if (healthBarUI != null)
            {
                healthBarUI.SetHP(_currentHP, _maxHP);
            }
        if (!_hasSpawned)
        {
            Vector3 target = transform.position + Vector3.right * 22f;
            yield return StartCoroutine(MoveToPosition(target, 0.01f));
            _hasSpawned = true;
        }
        yield return new WaitForSeconds(1f);
        readyToPlayTurn = true;
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

        // Currency drop chance on attack
        if (target != null && Random.value < currencyDropChance)
        {
            // Drop currency at target position (arc drop behind enemy)
            // Animation and pooling logic to be implemented
            Debug.Log($"[Player] Currency dropped on attack at {target.transform.position} (Chance: {currencyDropChance*100})");
           target.SpawnCurrencyOnDamage();
        }
    }

    public void TakeDamage(int damage)
    {
        damage = Mathf.Max(0, damage - _armor); // Apply armor
        _currentHP -= damage;
        Debug.Log($"[Player] Took Damage. Current HP: {_currentHP}");
        if (healthBarUI != null)
        {
            healthBarUI.SetHP(_currentHP, _maxHP);
            healthBarUI.ShowDamage(damage);
        }
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
        if (healthBarUI != null)
        {
            healthBarUI.SetHP(_currentHP, _maxHP);
        }
    }

    private void Die()
    {
        Debug.Log("Player defeated!");
        // Add defeat logic
    }

    // Called by TurnManager to start the player's turn
    public IEnumerator PlayTurn(List<EnemyParent> enemies)
    {
        // Wait until ready to play turn (e.g. entrance animation finished)
        while (!readyToPlayTurn)
            yield return null;
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
                    Debug.Log($"[Player] behaviorPattern == 1 Attacking {target.gameObject.name} (HP: {target.CurrentHP}) with projectile.");
                    if (!_originalPosition.HasValue) _originalPosition = transform.position;
                    Vector3 backPos = transform.position + Vector3.left *  4f;
                    yield return StartCoroutine(MoveToPosition(backPos, 0.01f));
                    Debug.Log($"[Player] Shooting projectile at {target.gameObject.name} from {_originalPosition.Value}.");
                    bool arc = target.transform.position.y > transform.position.y;
                    Vector3 firePoint = transform.position + (Vector3)firePointOffset;
                    yield return StartCoroutine(ShootProjectileAtTarget(target, arc, firePoint));
                    Debug.Log($"[Player] Returning to original position {_originalPosition.Value} after attack.");
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
        float duration = projectileTravelTime;
        float t = 0f;
        GameObject proj = Instantiate(projectilePrefab, start, Quaternion.identity);
        Debug.Log($"[Player] Shot projectile at {target.gameObject.name} from {start} to {end} (arc: {arc})");
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
        if (target != null)
        {
            Debug.Log($"[Player] Projectile reached {target.gameObject.name} at {target.transform.position}");
            target.TakeDamage(_attackDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 firePoint = transform.position + (Vector3)firePointOffset;
        Gizmos.DrawWireSphere(firePoint, 1f);
    }
}