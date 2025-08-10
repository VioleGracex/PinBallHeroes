using UnityEngine;

/// <summary>
/// Multiplier pin: When pinball passes all the way through, multiplies pinball and lets it through.
/// </summary>
public class RectanglePin : MonoBehaviour
{
    public int maxMultiplier = 5;
    private float lastTriggerTime = -10f;
    public float triggerCooldown = 0.4f;
    public TMPro.TextMeshProUGUI multiplierText;
    public int multiplier = 2;

    public void SetMultiplier(int mult)
    {
        multiplier = mult;
        UpdateMultiplierText();
    }

    private void UpdateMultiplierText()
    {
        if (multiplierText != null)
            multiplierText.text = "x" + multiplier.ToString();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Currency>())
        {
            // Only trigger if object is moving downward (from above)
            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null || rb.linearVelocity.y > -0.01f) // y should be negative for downward
                return;

            if (Time.time - lastTriggerTime < triggerCooldown)
                return;
            lastTriggerTime = Time.time;
            PinballManager mgr = FindFirstObjectByType<PinballManager>();
            if (mgr != null)
            {
                for (int i = 1; i < multiplier; i++)
                {
                    // Spawn extra pinballs at the same spot, slightly offset
                    Vector3 offset = Random.insideUnitCircle * 0.1f;
                    mgr.SpawnPinball(other.transform.position + offset, Quaternion.identity, false);
                }
            }
            // Destroy this pin if multiplier has reached or exceeded maxMultiplier
            if (multiplier >= maxMultiplier)
            {
                Destroy(gameObject);
            }
            // Let the original pinball pass through (do nothing)
        }
    }
}