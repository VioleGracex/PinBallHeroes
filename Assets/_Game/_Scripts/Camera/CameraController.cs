using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public float moveDuration = 1.0f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Y Positions")]
    public float combatY = 0f;
    public float pinballY = 10f;
    public float cardSelectY = -10f;

    private Coroutine moveCoroutine;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
    }

    public IEnumerator MoveToCombatPosition()
    {
        yield return MoveToY(combatY);
    }

    public IEnumerator MoveToPinballPosition()
    {
        yield return MoveToY(pinballY);
    }

    public IEnumerator MoveToCardSelectPosition()
    {
        yield return MoveToY(cardSelectY);
    }

    public void SetInstantPosition(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }

    private IEnumerator MoveToY(float targetY)
    {
        Vector3 start = transform.position;
        Vector3 end = new Vector3(start.x, targetY, start.z);
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float eased = easeCurve.Evaluate(t);
            transform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }
        transform.position = end;
    }
}
