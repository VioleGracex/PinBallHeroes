using UnityEngine;

/// <summary>
/// Multiplier pin: When pinball passes all the way through, multiplies pinball and lets it through.
/// </summary>
public class RectanglePin : MonoBehaviour
{
    private float lastTriggerTime = -10f;
    public float triggerCooldown = 0.2f;

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
                    mgr.SpawnPinball(other.transform.position + offset, Quaternion.identity);
                }
            }
            // Let the original pinball pass through (do nothing)
        }
    }
}