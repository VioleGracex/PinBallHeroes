using UnityEngine;
using System.Collections;
using TMPro;

public class CannonManager : MonoBehaviour
{
    [Header("Cannon Target Transform (where currency flies to)")]
    public Transform cannonMouth;
    [Header("Ammo Value")]
    public int ammo = 0;
    [Header("Cannon Move Settings")]
    public float moveSpeed = 10f;
    [Header("Pinball Collect Settings")]
    [Tooltip("Total time (seconds) to collect all pinballs")] public float collectDuration = 3f;
    [Tooltip("Minimum speed for pinballs")] public float minPinballSpeed = 10f;
    [Tooltip("Maximum speed for pinballs")] public float maxPinballSpeed = 50f;
    [Header("UI")]
    public TextMeshProUGUI ammoText;
    [Header("Gizmos")]
    [Tooltip("Collection position offset for pinball collection (relative to cannonMouth)")]
    public Vector3 collectionPositionOffset = Vector3.zero;

    private Vector3? originalPosition = null;
    private Coroutine ammoAnimCoroutine;
    private int displayedAmmo = 0;

    [SerializeField] private Camera mainCamera;

    public IEnumerator CollectAllCurrencyToCannon()
    {
        Currency[] allCurrency = FindObjectsByType<Currency>(FindObjectsSortMode.None);
        if (allCurrency.Length == 0 || cannonMouth == null)
            yield break;

        // Move cannon to center of camera
        if (!originalPosition.HasValue)
            originalPosition = transform.position;
        Vector3 center = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        center.z = transform.position.z;
        yield return StartCoroutine(MoveToPosition(center, moveSpeed));

        // Calculate pinball speed so all are collected in collectDuration
        float pinballSpeed = Mathf.Clamp((allCurrency.Length > 0 ? (Vector3.Distance(cannonMouth.position, allCurrency[0].transform.position) / (collectDuration / allCurrency.Length)) : minPinballSpeed), minPinballSpeed, maxPinballSpeed);

        int collected = 0;
        foreach (var c in allCurrency)
        {
            c.SetTravelSpeed(pinballSpeed); // Assumes Currency has SetTravelSpeed(float)
            c.Collect(cannonMouth.position, (currency) => { collected++; });
        }
        // Wait until all currency is collected or timeout
        float timeout = collectDuration + 1f;
        float timer = 0f;
        while (collected < allCurrency.Length && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        int prevAmmo = ammo;
        ammo += allCurrency.Length;
        AnimateAmmoText(prevAmmo, ammo);

        // Move cannon back to original position
        if (originalPosition.HasValue)
            yield return StartCoroutine(MoveToPosition(originalPosition.Value, moveSpeed));
    }

    private IEnumerator MoveToPosition(Vector3 target, float speed)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    private void AnimateAmmoText(int from, int to)
    {
        if (ammoAnimCoroutine != null)
            StopCoroutine(ammoAnimCoroutine);
        ammoAnimCoroutine = StartCoroutine(AnimateAmmoRoutine(from, to));
    }

    private IEnumerator AnimateAmmoRoutine(int from, int to)
    {
        float duration = 0.5f + 0.05f * Mathf.Abs(to - from); // Duration scales with amount
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            displayedAmmo = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
            UpdateAmmoText();
            yield return null;
        }
        displayedAmmo = to;
        UpdateAmmoText();
    }

    private void UpdateAmmoText()
    {
        if (ammoText != null)
            ammoText.text = $"{displayedAmmo}";
    }

    public void Shoot(int amount)
    {
        // TODO: Implement shooting logic (e.g., fire pinballs, play animation, etc.)
        Debug.Log($"[CannonManager] Shooting {amount} pinballs!");
        UpdateAmmoText();
    }

    private void Start()
    {
        displayedAmmo = ammo;
        UpdateAmmoText();
    }

    private void OnDrawGizmos()
    {
        // Draw gizmo for the collection position (center of camera + offset)
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        Vector3 collectionPos = Vector3.zero;
        if (cam != null)
        {
            collectionPos = cam.transform.position + collectionPositionOffset;
            collectionPos.z = transform.position.z; // Match cannon's Z for 2D
        }
        else
        {
            collectionPos = transform.position + collectionPositionOffset;
        }
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.7f); // Violet
        Gizmos.DrawWireSphere(collectionPos, 0.5f);
        DrawGizmoCircle(collectionPos, 5f);
    }

    private void DrawGizmoCircle(Vector3 center, float radius, int segments = 64)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), Mathf.Sin(0), 0) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

#if UNITY_EDITOR
    private Vector3 _lastCollectionPositionOffset;
    private void OnValidate()
    {
        // Only repaint if the offset value actually changed
        if (_lastCollectionPositionOffset != collectionPositionOffset)
        {
            _lastCollectionPositionOffset = collectionPositionOffset;
            UnityEditor.SceneView.RepaintAll();
        }
    }
#endif
}
