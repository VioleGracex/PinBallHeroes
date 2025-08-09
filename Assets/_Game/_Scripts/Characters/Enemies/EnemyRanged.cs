using UnityEngine;
using System.Collections;

public class EnemyRanged : EnemyParent
{
    public GameObject projectilePrefab;
    [Header("Projectile Fire Point Offset (local)")]
    public Vector2 firePointOffset = new Vector2(0.5f, 0.2f);
    [Header("Projectile Travel Time (seconds)")]
    [SerializeField]
    private float projectileTravelTime = 1f;

    private bool readyToPlayTurn = false;

    protected override void Start()
    {
        base.Start();
        readyToPlayTurn = false;
        StartCoroutine(MoveTowardPlayerOnSpawn());
    }

    private IEnumerator MoveTowardPlayerOnSpawn()
    {
        if (player == null) yield break;
        Vector3 dir = (player.transform.position - transform.position).normalized;
        Vector3 start = transform.position;
        Vector3 target = start + dir * 12f;
        float duration = 0.4f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float easedT = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t; // EaseInOutQuad
            transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(easedT));
            yield return null;
        }
        transform.position = target;
        readyToPlayTurn = true;
    }

    // Idle/turn-based: Called by TurnManager
    public override void TakeTurn()
    {
        StartCoroutine(WaitAndTakeTurn());
    }

    private IEnumerator WaitAndTakeTurn()
    {
        while (!readyToPlayTurn)
            yield return null;
        int shots = GetAttacksPerTurn();
        for (int i = 0; i < shots; i++)
        {
            // Wait for each projectile to finish before next or ending turn
            yield return StartCoroutine(ShootProjectileAtPlayer(player, transform.position + (Vector3)firePointOffset, player.transform.position.y > (transform.position + (Vector3)firePointOffset).y));
        }
        SetFinishedActions();
    }

    public override void Attack(Player target)
    {
        if (target != null && projectilePrefab != null)
        {
            Vector3 firePoint = transform.position + (Vector3)firePointOffset;
            Debug.Log($"[EnemyRanged] {gameObject.name} attacks player with projectile from {firePoint} to {target.transform.position}");
            bool arc = target.transform.position.y > firePoint.y;
            // Only start coroutine if not already handled in WaitAndTakeTurn
            // StartCoroutine(ShootProjectileAtPlayer(target, firePoint, arc));
        }
        else
        {
            // fallback: instant damage
            Debug.Log($"[EnemyRanged] {gameObject.name} attacks player instantly for {AttackDamage} damage.");
            base.Attack(target);
            SetFinishedActions();
        }
    }

    private IEnumerator ShootProjectileAtPlayer(Player target, Vector3 firePoint, bool arc)
    {
        Vector3 start = firePoint;
        Vector3 end = target.transform.position;
        float duration = projectileTravelTime;
        float t = 0f;
        GameObject proj = Instantiate(projectilePrefab, start, Quaternion.identity);
        Debug.Log($"[EnemyRanged] {gameObject.name} shot projectile at player from {start} to {end} (arc: {arc})");
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
            Debug.Log($"[EnemyRanged] {gameObject.name} projectile reached player at {target.transform.position}");
            target.TakeDamage(AttackDamage);
            Debug.Log($"{gameObject.name} attacks player for {AttackDamage} damage.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 firePoint = transform.position + (Vector3)firePointOffset;
        Gizmos.DrawWireSphere(firePoint, 1f);
    }
}