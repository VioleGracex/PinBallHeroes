using UnityEngine;
using System.Collections;

public class EnemyMelee : EnemyParent
{
    public float attackRange = 1.5f;
    public float moveSpeed = 12f;
    private Vector2 originalPosition;
    public GameObject slashEffectPrefab;

    private bool readyToPlayTurn = false;

    protected override void Start()
    {
        base.Start();
        originalPosition = transform.position;
        readyToPlayTurn = false;
        StartCoroutine(MoveTowardPlayerOnSpawn());
    }

    private IEnumerator MoveTowardPlayerOnSpawn()
    {
        if (player == null) yield break;
        Vector3 dir = (player.transform.position - transform.position).normalized;
        Vector3 start = transform.position;
        Vector3 target = start + dir * moveSpeed/2f;
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
        Debug.Log($"[EnemyMelee] {gameObject.name} begins turn.");
        StartCoroutine(ActMeleeTurn());
    }

    private IEnumerator ActMeleeTurn()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.transform.position);
        if (distToPlayer <= attackRange)
        {
            Debug.Log($"[EnemyMelee] {gameObject.name} is in attack range. Moving to max range position.");
            // Move to max range position (just at attackRange from player)
            Vector2 dir = (player.transform.position - transform.position).normalized;
            Vector2 maxRangePos = (Vector2)player.transform.position - dir * attackRange;
            yield return StartCoroutine(MoveToPosition(maxRangePos, 0.01f));
            // Attack with slash effect
            int attacks = GetAttacksPerTurn();
            for (int i = 0; i < attacks; i++)
            {
                if (slashEffectPrefab != null)
                {
                    GameObject slash = Instantiate(slashEffectPrefab, player.transform.position, Quaternion.identity);
                    Destroy(slash, 0.3f);
                }
                Debug.Log($"[EnemyMelee] {gameObject.name} attacks player (attack {i+1}/{attacks}).");
                Attack(player);
                yield return new WaitForSeconds(0.2f);
            }
            Debug.Log($"[EnemyMelee] {gameObject.name} moving back to original position.");
            yield return StartCoroutine(MoveToPosition(originalPosition, 0.01f));
        }
        else
        {
            Debug.Log($"[EnemyMelee] {gameObject.name} is too far. Moving toward player.");
            // Move as much as possible toward player
            float moveDist = moveSpeed * Time.deltaTime * 60f; // moveSpeed per second, estimate for 1 turn
            Vector2 dir = (player.transform.position - transform.position).normalized;
            Vector2 targetPos = (Vector2)transform.position + dir * Mathf.Min(moveDist, distToPlayer - attackRange);
            yield return StartCoroutine(MoveToPosition(targetPos, 0.01f));
        }
        Debug.Log($"[EnemyMelee] {gameObject.name} finished turn.");
        SetFinishedActions();
    }

    private IEnumerator MoveToPosition(Vector2 target, float stopDistance)
    {
        Vector2 start = transform.position;
        float totalDist = Vector2.Distance(start, target);
        float duration = Mathf.Max(0.1f, totalDist / Mathf.Max(0.01f, moveSpeed * 2f)); // scale duration to distance
        float t = 0f;
        while (Vector2.Distance(transform.position, target) > stopDistance)
        {
            t += Time.deltaTime / duration;
            float easedT = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
            transform.position = Vector2.Lerp(start, target, Mathf.Clamp01(easedT));
            yield return null;
        }
        transform.position = target;
    }

    public override void Attack(Player target)
    {
        Debug.Log($"[EnemyMelee] {gameObject.name} attacks player for {AttackDamage} damage.");
        base.Attack(target);
        // Add melee-specific effects if needed
    }
}