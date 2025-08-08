using UnityEngine;

public class EnemyRanged : EnemyParent
{
    public GameObject projectilePrefab;
    public Transform firePoint;

    protected override void Start()
    {
        MaxHP = 20;
        CurrentHP = MaxHP;
        AttackDamage = 2;
        base.Start();
    }

    // Idle/turn-based: Called by TurnManager
    public override void TakeTurn()
    {
        int shots = GetAttacksPerTurn();
        for (int i = 0; i < shots; i++)
        {
            ShootProjectile();
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab && firePoint)
        {
            Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Debug.Log($"{gameObject.name} shoots a projectile!");
        }
    }
}