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
        if (pinballAmount <= 0) return;
        if (collision.gameObject.GetComponent<Currency>())
        {
            if (Time.time - lastTriggerTime < triggerCooldown)
                return;
            lastTriggerTime = Time.time;
            PinballManager mgr = FindFirstObjectByType<PinballManager>();
            if (pinballAmount > 0 && mgr != null)
            {
                mgr.SpawnPinball(collision.transform.position, Quaternion.identity, false);
            }
            pinballAmount--;
            UpdateAmountText();
        }
    }
}