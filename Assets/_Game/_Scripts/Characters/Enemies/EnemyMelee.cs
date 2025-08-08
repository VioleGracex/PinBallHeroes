using UnityEngine;
using System.Collections;

public class EnemyMelee : EnemyParent
{
    public float attackRange = 1.5f;
    public float moveSpeed = 4f;
    private Vector2 originalPosition;

    protected override void Start()
    {
        base.Start();
        originalPosition = transform.position;
    }

    // Idle/turn-based: Called by TurnManager
    public override void TakeTurn()
    {
        StartCoroutine(ActMeleeTurn());
    }

    private IEnumerator ActMeleeTurn()
    {
        // Move towards player
        yield return StartCoroutine(MoveToPosition(player.transform.position, attackRange));
        // Attack
        int attacks = GetAttacksPerTurn();
        for (int i = 0; i < attacks; i++)
        {
            Attack(player);
            yield return new WaitForSeconds(0.2f); // slight delay between attacks
        }
        // Move back to original position
        yield return StartCoroutine(MoveToPosition(originalPosition, 0.01f));
    }

    private IEnumerator MoveToPosition(Vector2 target, float stopDistance)
    {
        while (Vector2.Distance(transform.position, target) > stopDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public override void Attack(Player target)
    {
        base.Attack(target);
        // Add melee-specific effects if needed
    }
}