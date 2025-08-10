using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PinballManager : MonoBehaviour
{
#region Inspector Fields
    [Header("Pinball Settings")]
    public CannonManager cannonManager;
    public GameObject pinballPrefab; // Should be Currency prefab
    public int pinballLayer = 8; // Set to Pinball layer
    public float fireRatePercent = 0.2f; // 20% per second
    public float pinballForce = 60f;
    public float stuckVelocityThreshold = 0.1f;
    public float stuckTime = 2f;
    public float modeTimeout = 120f;
    public Camera pinballCamera;
    [Header("Flipper UI Buttons")]
    public GameObject leftFlipperButton;
    public GameObject rightFlipperButton;
#endregion

#region State
    private List<Currency> activePinballs = new List<Currency>();
    private bool isPinballMode = false;
    private float pinballModeTimer = 0f;
    private Dictionary<Currency, float> stuckTimers = new Dictionary<Currency, float>();
    public System.Action OnPinballModeEnd;
#endregion


#region Pinball Logic
    // Called by CannonManager to spawn a pinball
    public void SpawnPinball(Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[PinballManager] Spawning pinball at {position} rot {rotation.eulerAngles}");
        if (pinballPrefab == null) { Debug.LogWarning("[PinballManager] pinballPrefab is null!"); return; }
        GameObject go = Instantiate(pinballPrefab, position, rotation);
        Currency currency = go.GetComponent<Currency>();
        if (currency != null)
        {
            currency.EnablePhysics();
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.down * pinballForce;
                rb.angularVelocity = 0f;
                Debug.Log($"[PinballManager] Set pinball velocity to {rb.linearVelocity}");
            }
            activePinballs.Add(currency);
            stuckTimers[currency] = 0f;
            Debug.Log($"[PinballManager] Pinball added. Active count: {activePinballs.Count}");
        }
        else
        {
            Debug.LogWarning("[PinballManager] Spawned object has no Currency component!");
        }
    }
#endregion


#region Pinball Mode
    public void StartPinballMode()
    {
        Debug.Log("[PinballManager] Starting pinball mode");
        if (isPinballMode) return;
        isPinballMode = true;
        pinballModeTimer = 0f;
        activePinballs.Clear();
        stuckTimers.Clear();
        if (cannonManager != null)
        {
            cannonManager.StartPinballFiring();
        }
        // Show flipper buttons
        if (leftFlipperButton != null) leftFlipperButton.SetActive(true);
        if (rightFlipperButton != null) rightFlipperButton.SetActive(true);
        StartCoroutine(PinballModeTimer());
    }


    private IEnumerator PinballModeTimer()
    {
        Debug.Log("[PinballManager] PinballModeTimer started");
        while (isPinballMode)
        {
            pinballModeTimer += Time.deltaTime;
            if (pinballModeTimer >= modeTimeout)
            {
                Debug.Log("[PinballManager] Pinball mode timed out.");
                EndPinballMode();
                yield break;
            }
            CheckPinballsOutOfBounds();
            CheckPinballsStuck();
            yield return null;
        }
        Debug.Log("[PinballManager] PinballModeTimer ended");
    }
#endregion


#region Utility
    private void CheckPinballsOutOfBounds()
    {
        for (int i = activePinballs.Count - 1; i >= 0; i--)
        {
            Currency c = activePinballs[i];
            if (c == null) { activePinballs.RemoveAt(i); continue; }
            if (!IsInCameraView(c.transform.position))
            {
                Debug.Log($"[PinballManager] Pinball out of bounds at {c.transform.position}, removing.");
                CurrencyManager.Instance.AddPinballs(1);
                Destroy(c.gameObject);
                activePinballs.RemoveAt(i);
                stuckTimers.Remove(c);
            }
        }
        // End pinball mode only if cannon is empty and no active pinballs
        if (activePinballs.Count == 0 && cannonManager != null && cannonManager.ammo == 0)
        {
            Debug.Log("[PinballManager] No active pinballs and cannon is empty. Ending mode.");
            EndPinballMode();
        }
        else if (activePinballs.Count == 0)
        {
            Debug.Log($"[PinballManager] No active pinballs, but cannon still has ammo: {cannonManager?.ammo}");
        }
    }

    private void CheckPinballsStuck()
    {
        foreach (var c in new List<Currency>(activePinballs))
        {
            Rigidbody2D rb = c.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (rb.linearVelocity.magnitude < stuckVelocityThreshold)
                {
                    stuckTimers[c] += Time.deltaTime;
                    if (stuckTimers[c] > stuckTime)
                    {
                        Debug.Log("[PinballManager] Pinball stuck, removing.");
                        CurrencyManager.Instance.AddPinballs(1);
                        Destroy(c.gameObject);
                        activePinballs.Remove(c);
                        stuckTimers.Remove(c);
                    }
                }
                else
                {
                    stuckTimers[c] = 0f;
                }
            }
        }
    }


    private bool IsInCameraView(Vector3 pos)
    {
        if (pinballCamera == null) return true;
        Vector3 viewport = pinballCamera.WorldToViewportPoint(pos);
        bool inView = viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
        if (!inView) Debug.Log($"[PinballManager] Pinball at {pos} is out of camera view");
        return inView;
    }
#endregion


#region Flipper UI
    private void EndPinballMode()
    {
        if (!isPinballMode) return;
        isPinballMode = false;
        Debug.Log("[PinballManager] Pinball mode ended.");
        foreach (var c in activePinballs)
        {
            if (c != null) Destroy(c.gameObject);
        }
        activePinballs.Clear();
        stuckTimers.Clear();
        if (cannonManager != null)
        {
            cannonManager.transform.rotation = Quaternion.identity;
            cannonManager.DisablePinballAiming(); // Hide/disable shoot button
        }
        // Hide flipper buttons
        if (leftFlipperButton != null) leftFlipperButton.SetActive(false);
        if (rightFlipperButton != null) rightFlipperButton.SetActive(false);
        OnPinballModeEnd?.Invoke();
        Debug.Log("[PinballManager] Pinball mode cleanup complete");
    }
#endregion
}