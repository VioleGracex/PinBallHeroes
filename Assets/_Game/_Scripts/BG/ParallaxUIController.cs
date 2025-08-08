using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using NaughtyAttributes;

public class ParallaxUIController : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public RectTransform[] tiles; // Assign two RectTransforms per layer in Inspector
        public float parallaxSpeed = 0.5f;
    }

    public ParallaxLayer[] layers;
    public float scrollSpeed = 2f;
    private Coroutine parallaxCoroutine;
    public RectTransform referenceRect; // Assign parent canvas or container rect (optional)

    void Start()
    {
        AdaptTilesToScreen();
    }

    [Button("Adapt Tiles To Screen")]
    public void AdaptTilesToScreen()
    {
        // Use referenceRect if supplied, otherwise use parent canvas rect
        RectTransform refRect = referenceRect ? referenceRect : GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (!refRect)
        {
            Debug.LogWarning("No reference RectTransform (canvas or container) found for UI parallax sizing.");
            return;
        }

        float width = refRect.rect.width;

        foreach (var layer in layers)
        {
            for (int i = 0; i < layer.tiles.Length; i++)
            {
                var tile = layer.tiles[i];
                if (!tile) continue;
                // Set each tile to fill screen width (height can be set in prefab or as you want)
                Vector2 size = tile.sizeDelta;
                size.x = width;
                tile.sizeDelta = size;
            }

            // Position tiles side by side
            if (layer.tiles.Length == 2 && layer.tiles[0] && layer.tiles[1])
            {
                float tileWidth = layer.tiles[0].rect.width;
                Vector3 basePos = layer.tiles[0].anchoredPosition;
                layer.tiles[0].anchoredPosition = new Vector2(0, basePos.y);
                layer.tiles[1].anchoredPosition = new Vector2(tileWidth, basePos.y);
            }
        }
    }

    public void MoveParallax(float duration = 2f)
    {
        if (parallaxCoroutine != null)
            StopCoroutine(parallaxCoroutine);
        parallaxCoroutine = StartCoroutine(ParallaxRoutine(duration));
    }

    private IEnumerator ParallaxRoutine(float duration)
    {
        float timer = 0f;
        RectTransform refRect = referenceRect ? referenceRect : GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (!refRect)
        {
            Debug.LogWarning("No reference RectTransform (canvas or container) found for UI parallax movement.");
            yield break;
        }
        float width = refRect.rect.width;

        while (timer < duration)
        {
            foreach (var layer in layers)
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile != null)
                    {
                        Vector2 pos = tile.anchoredPosition;
                        pos.x -= layer.parallaxSpeed * scrollSpeed * Time.deltaTime;
                        tile.anchoredPosition = pos;
                    }
                }
                // Wrapping logic for UI: assumes tiles[0] and tiles[1] are horizontally aligned, same width
                float tileWidth = layer.tiles[0].rect.width;

                for (int i = 0; i < layer.tiles.Length; i++)
                {
                    RectTransform tile = layer.tiles[i];
                    RectTransform other = layer.tiles[(i + 1) % layer.tiles.Length];

                    // If tile is fully left of the reference, move it to the right of the other tile
                    if (tile.anchoredPosition.x < -tileWidth)
                    {
                        tile.anchoredPosition = new Vector2(
                            other.anchoredPosition.x + tileWidth,
                            tile.anchoredPosition.y
                        );
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }

    [Button("Simulate Parallax (2s)")]
    public void SimulateParallax()
    {
        MoveParallax(2f);
    }
}