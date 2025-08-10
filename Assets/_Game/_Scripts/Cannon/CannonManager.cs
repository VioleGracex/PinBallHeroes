using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;

public class CannonManager : MonoBehaviour
{
    // --- Enhanced Touch (New Input System) ---
    private bool canDragToRotate = false;
    private bool isDragging = false;
    private float lastPointerX;
    private float targetAngle = 0f;
    [SerializeField] PinballManager pinballManager;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
        if (shootButton != null)
            shootButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnShootButtonPressed);
    }

    private void OnDisable()
    {
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
        UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
        EnhancedTouchSupport.Disable();
        if (shootButton != null)
            shootButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(OnShootButtonPressed);
    }

    private void Start()
    {
        displayedAmmo = ammo;
        UpdateAmmoText();
        // Initialize targetAngle to current angle
        float angle = transform.eulerAngles.z;
        if (angle > 180f) angle -= 360f;
        targetAngle = angle;
    }

#region Drag & Rotation
    private void OnFingerDown(Finger finger)
    {
        if (!canDragToRotate) return;
        // Prevent rotation if pointer is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(finger.index)) return;
        // Only allow one drag at a time (first finger)
        if (finger.index == 0)
        {
            isDragging = true;
        }
    }

    private void OnFingerUp(Finger finger)
    {
        if (finger.index == 0)
            isDragging = false;
    }

    private void OnFingerMove(Finger finger)
    {
        if (!canDragToRotate || !isDragging || finger.index != 0)
            return;
        // Prevent rotation if pointer is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(finger.index)) return;

        // Map finger X position to angle range (left = minAngle, right = maxAngle)
        float screenX = finger.screenPosition.x;
        float screenWidth = Screen.width;
        float t = Mathf.Clamp01(screenX / screenWidth);
        // If inverted, swap minAngle and maxAngle in Lerp
        float angle = Mathf.Lerp(maxAngle, minAngle, t);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

#endregion

#region Shooting
    [Header("Pinball Mode")]
    public float minAngle = -50f;
    public float maxAngle = 50f;
    public float angleSpeed = 10f; // degrees per second
    private bool isFiring = false;
    private Coroutine firingRoutine;

    // Enable this when entering Pinball mode, disable on exit
    public void StartPinballFiring()
    {
        if (isFiring) return;
        EnablePinballAiming();
    }

    private IEnumerator PinballFiringRoutine()
    {
        isFiring = true;
        int totalAmmo = ammo;
        Debug.Log($"[CannonManager] Starting pinball firing with {totalAmmo} ammo");
        float fireDelay = 0.5f;
        while (ammo > 0)
        {
            int toFire = Mathf.CeilToInt(totalAmmo * 0.2f);
            toFire = Mathf.Clamp(toFire, 1, ammo);
            for (int i = 0; i < toFire; i++)
            {
                FirePinball(pinballManager);
                UpdateAmmoText();
                if (ammo <= 0) break;
            }
            yield return new WaitForSeconds(fireDelay);
        }
        // After firing, smoothly rotate back to 0
        yield return StartCoroutine(RotateToZero());
        isFiring = false;
        DisablePinballAiming();
        UpdateAmmoText(); 
    }

    private void FirePinball(PinballManager pinballManager)
    {
        if (pinballManager == null) return;
        Vector3 spawnPos = muzzleTransform != null ? muzzleTransform.position : (cannonMouth != null ? cannonMouth.position : transform.position);
        if (ammo <= 0) {
            Debug.LogWarning("[CannonManager] Tried to fire pinball with no ammo!");
            return;
        }
        pinballManager.SpawnPinball(spawnPos, transform.rotation);
        ammo--;
        UpdateAmmoText();
        Debug.Log($"[CannonManager] Fired pinball. Ammo left: {ammo}");
    }

    private IEnumerator RotateToZero()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float startAngle = transform.eulerAngles.z;
        if (startAngle > 180f) startAngle -= 360f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Lerp(startAngle, 0f, t);
            transform.rotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        transform.rotation = Quaternion.identity;
    }

    public bool IsFiringPinballs()
    {
        return isFiring;
    }
#endregion

#region Ammo & Collection
    public event System.Action OnReturnedToOriginalPosition;
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
        // Notify listeners that cannon returned
        OnReturnedToOriginalPosition?.Invoke();
    }

    private IEnumerator MoveToPosition(Vector3 target, float speed)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }
#endregion

#region Unity Events & UI
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
#endregion

#region Gizmos
    private void OnDrawGizmos()
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        Vector3 collectionPos = Vector3.zero;
        if (cam != null)
        {
            collectionPos = cam.transform.position + collectionPositionOffset;
            collectionPos.z = transform.position.z;
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
#endregion

#if UNITY_EDITOR
    private Vector3 _lastCollectionPositionOffset;
    private void OnValidate()
    {
        if (_lastCollectionPositionOffset != collectionPositionOffset)
        {
            _lastCollectionPositionOffset = collectionPositionOffset;
            UnityEditor.SceneView.RepaintAll();
        }
    }
#endif

    [Header("Pinball UI")]
    public GameObject shootButton; // Assign in inspector

    [Header("Pinball Muzzle")]
    public Transform muzzleTransform; // Assign in inspector for shoot location

    public void EnablePinballAiming()
    {
        canDragToRotate = true;
        SetShootButtonVisible(true);
    }

    public void DisablePinballAiming()
    {
        canDragToRotate = false;
        SetShootButtonVisible(false);
    }

    private void SetShootButtonVisible(bool visible)
    {
        if (shootButton != null)
            shootButton.SetActive(visible);
    }

    public void OnShootButtonPressed()
    {
        if (!canDragToRotate || isFiring || ammo <= 0) return;
        firingRoutine = StartCoroutine(PinballFiringRoutine());
        SetShootButtonVisible(false);
    }
}