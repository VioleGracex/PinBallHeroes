using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedurally generates a field of pins (spawner circles and multiplier rectangles) for the pinball area.
/// Attach to a GameObject in your pinball scene.
/// </summary>
public class PinFieldGenerator : MonoBehaviour
{
    [Header("Pin Parent")]
    public Transform pinParent; // Optional parent for spawned pins
    // ...existing code...
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw the spawn area rectangle
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Vector3 areaCenter = new Vector3(center.x, center.y, 0f);
        Vector3 areaSize = new Vector3(width, height, 0.1f);
        Gizmos.DrawCube(areaCenter, areaSize);

        // Draw margin area (inner rectangle)
        Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.5f);
        Vector3 marginCenter = areaCenter;
        Vector3 marginSize = new Vector3(width - 2 * margin, height - 2 * margin, 0.1f);
        Gizmos.DrawWireCube(marginCenter, marginSize);
    }
    #endif
    [Header("Pin Prefabs")]
    public GameObject circlePinPrefab;      // Pin that spawns N pinballs (should have CirclePin script)
    public GameObject rectanglePinPrefab;   // Pin that multiplies pinballs (should have RectanglePin script)

    [Header("Field Area")]
    public Vector2 center = Vector2.zero;   // Center of pin field (world space)
    public float width = 10f;               // Width of area (for rectangle)
    public float height = 16f;              // Height of area (for rectangle)
    public float margin = 1f;               // Margin to keep pins away from edges

    [Header("Pin Layout")]
    public int rows = 6;                    // Number of rows
    public int columns = 7;                 // Pins per row (rectangle)
    public int circleRows = 4;              // Rows of circle pins (top area)
    public int rectangleRows = 2;           // Rows of rectangle pins (bottom area)
    [Tooltip("Randomize pin numbers and multipliers.")]
    public bool randomize = true;

    [Header("Circle Pin Settings")]
    public int minSpawn = 1;
    public int maxSpawn = 5;

    [Header("Rectangle Pin Settings")]
    public int minMultiplier = 2;
    public int maxMultiplier = 5;

    public void Start()
    {
        GenerateField();
    }

    public void GenerateField()
    {
        float usableWidth = width - 2 * margin;
        float usableHeight = height - 2 * margin;
        float rowSpacing = usableHeight / (rows - 1);

        Transform parent = pinParent != null ? pinParent : this.transform;

        // Circle pins (top area)
        for (int row = 0; row < circleRows; row++)
        {
            // Randomly skip some rows (50% chance if randomize is on)
            if (randomize && Random.value < 0.5f) continue;
            float y = center.y + usableHeight / 2 - row * rowSpacing;
            int pinsInRow = 2; // Max 2 pins per row
            float xSpacing = pinsInRow > 1 ? usableWidth / (pinsInRow - 1) : 0f;
            for (int col = 0; col < pinsInRow; col++)
            {
                float x = center.x - usableWidth / 2 + (pinsInRow == 1 ? 0 : col * xSpacing);
                Vector2 pos = new Vector2(x, y);
                GameObject pin = Instantiate(circlePinPrefab, pos, Quaternion.identity, parent);
                int spawnAmount = randomize ? Random.Range(minSpawn, maxSpawn + 1) : maxSpawn;
                var pinScript = pin.GetComponent<CirclePin>();
                if (pinScript != null) pinScript.SetPinballAmount(spawnAmount);
            }
        }

        // Rectangle pins (bottom area)
        for (int row = 0; row < rectangleRows; row++)
        {
            // Randomly skip some rows (50% chance if randomize is on)
            if (randomize && Random.value < 0.5f) continue;
            float y = center.y - usableHeight / 2 + row * rowSpacing;
            int pinsInRow = 2; // Max 2 pins per row
            float xSpacing = pinsInRow > 1 ? usableWidth / (pinsInRow - 1) : 0f;
            for (int col = 0; col < pinsInRow; col++)
            {
                float x = center.x - usableWidth / 2 + (pinsInRow == 1 ? 0 : col * xSpacing);
                Vector2 pos = new Vector2(x, y);
                GameObject pin = Instantiate(rectanglePinPrefab, pos, Quaternion.identity, parent);
                int mult = randomize ? Random.Range(minMultiplier, maxMultiplier + 1) : maxMultiplier;
                var pinScript = pin.GetComponent<RectanglePin>();
                if (pinScript != null) pinScript.SetMultiplier(mult);
            }
        }

    }

    /// <summary>
    /// Destroys all children of pinParent (or this.transform if not set) and regenerates the field.
    /// </summary>
    public void RegenerateField()
    {
        Transform parent = pinParent != null ? pinParent : this.transform;
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in parent)
        {
            toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
        {
            if (go != null) DestroyImmediate(go);
        }
        GenerateField();
    }
}