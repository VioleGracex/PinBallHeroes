using UnityEngine;

public class Currency : MonoBehaviour
{
    public bool collected = false;
    private Vector3 collectTarget;
    private float collectSpeed = 10f;
    private System.Action<Currency> onCollected;

    public void Collect(Vector3 target, System.Action<Currency> onCollectedCallback)
    {
        collected = true;
        collectTarget = target;
        onCollected = onCollectedCallback;
    }

    public void SetTravelSpeed(float speed)
    {
        collectSpeed = speed;
    }

    void Update()
    {
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
}
