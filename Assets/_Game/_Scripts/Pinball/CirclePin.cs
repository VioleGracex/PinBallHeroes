using UnityEngine;

public class CirclePin : MonoBehaviour
   
{
    private float lastTriggerTime = -10f;
    public float triggerCooldown = 0.4f;
    public TMPro.TextMeshProUGUI amountText;
    public int pinballAmount = 3;

    public void SetPinballAmount(int amount)
    {
        pinballAmount = amount;
        UpdateAmountText();
    }

    private void UpdateAmountText()
    {
        if (amountText != null)
            amountText.text = pinballAmount.ToString();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Apply bounce force to anything that touches the pin
        Rigidbody2D rb = collision.rigidbody;
        if (rb != null)
        {
            // Calculate bounce direction away from the pin
            Vector2 bounceDir = (rb.position - (Vector2)transform.position).normalized;
            float bounceForce = 10f;
            rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);
        }

        if (pinballAmount <= 0) return;
        var currency = collision.gameObject.GetComponent<Currency>();
        if (currency != null)
        {
            if (Time.time - lastTriggerTime < triggerCooldown)
                return;
            lastTriggerTime = Time.time;
            PinballManager mgr = FindFirstObjectByType<PinballManager>();
            if (pinballAmount > 0 && mgr != null)
            {
                // Spawn away from this collider (use contact point and normal)
                Vector3 spawnPos = collision.contacts.Length > 0
                    ? collision.contacts[0].point + (Vector2)collision.contacts[0].normal * 0.5f
                    : collision.transform.position + Vector3.up * 0.5f;
                mgr.SpawnPinball(spawnPos, Quaternion.identity, false);
            }
            pinballAmount--;
            UpdateAmountText();
        }
    }
}