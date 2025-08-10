using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Flipper : MonoBehaviour
{
    public Rigidbody2D rb;
    public HingeJoint2D hinge;

    [Header("Torque Settings")]
    [Tooltip("Torque applied when flipping (impulse)")]
    public float flipImpulse = 1000f;
    [Tooltip("Torque to hold flipper up")]
    public float holdTorque = 20000f;
    [Tooltip("Torque to return flipper down")]
    public float releaseTorque = -30000f;

    [Header("Limit Angles")]
    public float minAngle = -45f; // Down
    public float maxAngle = 45f;  // Up

    [Header("UI Button to control this flipper")]
    public Button flipperButton; // Assign in Inspector

    private bool flipping = false;

    void Awake()
    {
        if (hinge == null) hinge = GetComponent<HingeJoint2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Setup hinge limits
        if (hinge != null)
        {
            hinge.useLimits = true;
            JointAngleLimits2D limits = hinge.limits;
            limits.min = minAngle;
            limits.max = maxAngle;
            hinge.limits = limits;
        }

        // Add event listeners for button press/release
        if (flipperButton != null)
        {
            EventTrigger trigger = flipperButton.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = flipperButton.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((eventData) => { flipping = true; });
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((eventData) => { flipping = false; });
            trigger.triggers.Add(pointerUp);
        }
    }

    void FixedUpdate()
    {
        // If the button is inactive/hidden, make sure flipping is false!
        if (flipperButton != null && !flipperButton.gameObject.activeInHierarchy)
            flipping = false;

        if (hinge == null || rb == null) return;

        float flipperAngle = hinge.jointAngle; // In degrees

        if (flipping)
        {
            // Snap up with impulse if not already at max angle
            if (flipperAngle < maxAngle - 2f)
            {
                rb.AddTorque(flipImpulse, ForceMode2D.Impulse);
            }
            else
            {
                // Hold at top
                rb.AddTorque(holdTorque * Time.fixedDeltaTime, ForceMode2D.Force);
            }
        }
        else
        {
            // Snap down with strong force if not already at min angle
            if (flipperAngle > minAngle + 2f)
            {
                rb.AddTorque(releaseTorque * Time.fixedDeltaTime, ForceMode2D.Force);
            }
        }
    }
}