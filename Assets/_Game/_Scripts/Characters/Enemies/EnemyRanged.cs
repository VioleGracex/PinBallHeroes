using UnityEngine;
using System.Collections;

public class EnemyRanged : EnemyParent
{
    public GameObject projectilePrefab;
    [Header("Projectile Fire Point Offset (local)")]
    public Vector2 firePointOffset = new Vector2(0.5f, 0.2f);

    protected override void Start()
    {
        MaxHP = 20;
        CurrentHP = MaxHP;
        AttackDamage = 2;
        base.Start();
        // player is injected by Zenject
        StartCoroutine(MoveTowardPlayerOnSpawn());
    }

    private IEnumerator MoveTowardPlayerOnSpawn()
    {
        if (player == null) yield break;
        Vector3 dir = (player.transform.position - transform.position).normalized;
        Vector3 start = transform.position;
        Vector3 target = start + dir * 1f;
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
    }

    // Idle/turn-based: Called by TurnManager
    public override void TakeTurn()
    {
        int shots = GetAttacksPerTurn();
        for (int i = 0; i < shots; i++)
        {
            Attack(player);
        }
    }

    public override void Attack(Player target)
    {
        if (target != null && projectilePrefab != null)
        {
            Vector3 firePoint = transform.position + (Vector3)firePointOffset;
            bool arc = target.transform.position.y > firePoint.y;
            StartCoroutine(ShootProjectileAtPlayer(target, firePoint, arc));
        }
        else
        {
            // fallback: instant damage
            base.Attack(target);
        }
    }

    private IEnumerator ShootProjectileAtPlayer(Player target, Vector3 firePoint, bool arc)
    {
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
                // Quadratic Bezier for arc
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
            target.TakeDamage(AttackDamage);
            Debug.Log($"{gameObject.name} attacks player for {AttackDamage} damage.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 firePoint = transform.position + (Vector3)firePointOffset;
        Gizmos.DrawWireSphere(firePoint, 0.05f);
    }
}