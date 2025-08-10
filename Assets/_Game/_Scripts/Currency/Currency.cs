using UnityEngine;

public class Currency : MonoBehaviour
{
    [Header("Metamorphosis")]
    public bool canMetamorphosis = true;
    private float metamorphosisCooldown = 0.5f;
    private float spawnTime = -10f;
    public bool collected = false;

    [Header("Movement Settings")]
    [Tooltip("Maximum velocity for the Rigidbody2D when in physics mode.")]
    public float maxVelocity = 30f;

    private Vector3 collectTarget;
    private float collectSpeed = 10f;
    private System.Action<Currency> onCollected;

    private Rigidbody2D rb;

    void Awake()
    {
        spawnTime = Time.time;
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        SetPhysicsMode(false); // Default to kinematic
    }

    /// <summary>
    /// Switches the currency to physics (dynamic) mode.
    /// </summary>
    private void SetPhysicsMode(bool enablePhysics)
    {
        if (rb != null)
        {
            rb.bodyType = enablePhysics ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }
    }

    /// <summary>
    /// Switches the currency to physics (dynamic) mode.
    /// </summary>
    public void EnablePhysics()
    {
        SetPhysicsMode(true);
    }

    /// <summary>
    /// Switches the currency to non-physics (kinematic) mode.
    /// </summary>
    public void DisablePhysics()
    {
        SetPhysicsMode(false);
    }

    public void Collect(Vector3 target, System.Action<Currency> onCollectedCallback)
    {
        collected = true;
        collectTarget = target;
        onCollected = onCollectedCallback;
        DisablePhysics();
    }

    public void SetTravelSpeed(float speed)
    {
        collectSpeed = speed;
    }

    void Update()
    {
        // Update canMetamorphosis based on cooldown
        if (Time.time - spawnTime < metamorphosisCooldown)
        {
            canMetamorphosis = false;
        }
        else
        {
            canMetamorphosis = true;
        }

        if (collected)
        {
            transform.position = Vector3.MoveTowards(transform.position, collectTarget, collectSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, collectTarget) < 0.1f)
            {
                onCollected?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        // Limit velocity if in physics mode
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && rb.simulated)
        {
            if (rb.linearVelocity.magnitude > maxVelocity)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            }
        }
    }
}