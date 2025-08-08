using UnityEngine;
using System.Collections;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ParallaxController : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform[] tiles; // Assign two Transforms per layer in Inspector
        public float parallaxSpeed = 0.5f;
    }

    [Header("Background (fills camera)")]
    public Transform background; // Assign a Transform with a SpriteRenderer
    public float backgroundDepth = 10f; // Z position for background

    [Header("Scaling")]
    public bool keepAspect = true; // If true, keep aspect ratio for ALL layers and background

    [Header("Parallax Layers")]
    public ParallaxLayer[] layers;
    public float scrollSpeed = 2f;
    private Coroutine parallaxCoroutine;
    public Camera mainCamera;

    [Header("Top Area Percentage")]
    [Range(0f, 1f)]
    public float topAreaPercent = 0.46f; // 46% of the screen from the top

    [Header("Left Offset (World Units)")]
    public float leftOffset = -5.0f; // How much to move all tiles/background to the left from the camera's left edge

    void Start()
    {
        AdaptBackgroundToScreen();
        AdaptTilesToScreen();
        MoveAllToLeft();
    }

    [Button("Adapt Background & Tiles To Screen")]
    public void AdaptAllToScreen()
    {
        AdaptBackgroundToScreen();
        AdaptTilesToScreen();
        MoveAllToLeft();
    }

    [Button("Adapt Background Only")]
    public void AdaptBackgroundToScreen()
    {
        if (!background) return;
        if (!mainCamera) mainCamera = Camera.main;

        SpriteRenderer sr = background.GetComponent<SpriteRenderer>();
        if (!sr) return;

        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float worldScreenHeight = 2f * mainCamera.orthographicSize;

        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y;

        float scaleX = worldScreenWidth / spriteWidth;
        float scaleY = worldScreenHeight / spriteHeight;

        if (keepAspect)
        {
            float scale = Mathf.Max(scaleX, scaleY);
            background.localScale = new Vector3(scale, scale, background.localScale.z);
        }
        else
        {
            background.localScale = new Vector3(scaleX, scaleY, background.localScale.z);
        }

        // Center background behind the camera at specified depth
        Vector3 bgPos = mainCamera.transform.position;
        bgPos.z = backgroundDepth;
        bgPos.y = mainCamera.transform.position.y; // <-- This is the fix!
        background.position = bgPos;
    }

    [Button("Adapt Parallax Tiles Only")]
    public void AdaptTilesToScreen()
    {
        if (!mainCamera) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float worldScreenHeight = 2f * mainCamera.orthographicSize;

        float layerTargetHeight = worldScreenHeight * topAreaPercent;

        foreach (var layer in layers)
        {
            for (int i = 0; i < layer.tiles.Length; i++)
            {
                var tile = layer.tiles[i];
                if (!tile) continue;

                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (!sr) continue;

                float spriteWidth = sr.sprite.bounds.size.x;
                float spriteHeight = sr.sprite.bounds.size.y;

                float scaleY = layerTargetHeight / spriteHeight;
                float scaleX = worldScreenWidth / spriteWidth;

                if (keepAspect)
                {
                    // Only use height scaling for both axes, so tile never exceeds the target height
                    float scale = scaleY;
                    tile.localScale = new Vector3(scale, scale, tile.localScale.z);
                }
                else
                {
                    tile.localScale = new Vector3(scaleX, scaleY, tile.localScale.z);
                }

                // Position the tile so its top edge aligns with the top edge of the camera view
                float tileWorldHeight = sr.bounds.size.y * tile.localScale.y;
                float cameraTopY = mainCamera.transform.position.y + worldScreenHeight / 2f;
                float tileTopY = tile.position.y + tileWorldHeight / 2f;
                float deltaY = cameraTopY - tileTopY;
                tile.position += new Vector3(0, deltaY, 0);
            }

            // Position tiles side by side
            if (layer.tiles.Length == 2 && layer.tiles[0] && layer.tiles[1])
            {
                SpriteRenderer sr0 = layer.tiles[0].GetComponent<SpriteRenderer>();
                float width = sr0.bounds.size.x * layer.tiles[0].localScale.x;
                Vector3 basePos = layer.tiles[0].position;
                layer.tiles[1].position = new Vector3(basePos.x + width, basePos.y, basePos.z);
            }
        }
    }

    [Button("Move All To Left")]
    public void MoveAllToLeft()
    {
        if (!mainCamera) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float cameraLeftX = mainCamera.transform.position.x - (worldScreenWidth / 2f);

        // Move background
        if (background)
        {
            Vector3 bgPos = background.position;
            float bgWidth = 0f;
            var sr = background.GetComponent<SpriteRenderer>();
            if (sr) bgWidth = sr.bounds.size.x;
            // Move so that the background's right edge is at camera left + leftOffset
            bgPos.x = cameraLeftX + leftOffset + bgWidth / 2f;
            background.position = bgPos;
        }

        // Move parallax tiles
        foreach (var layer in layers)
        {
            foreach (var tile in layer.tiles)
            {
                if (!tile) continue;
                var sr = tile.GetComponent<SpriteRenderer>();
                float width = sr ? sr.bounds.size.x : 0f;
                Vector3 pos = tile.position;
                // Move so that tile's right edge is at camera left + leftOffset
                pos.x = cameraLeftX + leftOffset + width / 2f;
                tile.position = pos;
            }
            // Re-calculate side-by-side for 2-tile layers
            if (layer.tiles.Length == 2 && layer.tiles[0] && layer.tiles[1])
            {
                var sr0 = layer.tiles[0].GetComponent<SpriteRenderer>();
                float width = sr0 ? sr0.bounds.size.x : 0f;
                Vector3 basePos = layer.tiles[0].position;
                layer.tiles[1].position = new Vector3(basePos.x + width, basePos.y, basePos.z);
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
        if (!mainCamera) mainCamera = Camera.main;

        while (timer < duration)
        {
            foreach (var layer in layers)
            {
                foreach (var tile in layer.tiles)
                {
                    if (tile != null)
                    {
                        Vector3 pos = tile.position;
                        pos.x -= layer.parallaxSpeed * scrollSpeed * Time.deltaTime;
                        tile.position = pos;
                    }
                }
                // Wrapping logic: assumes tiles[0] and tiles[1] are horizontally aligned, same width
                if (layer.tiles.Length == 2 && layer.tiles[0] && layer.tiles[1])
                {
                    SpriteRenderer sr0 = layer.tiles[0].GetComponent<SpriteRenderer>();
                    float tileWidth = sr0.bounds.size.x * layer.tiles[0].localScale.x;

                    for (int i = 0; i < layer.tiles.Length; i++)
                    {
                        Transform tile = layer.tiles[i];
                        Transform other = layer.tiles[(i + 1) % layer.tiles.Length];

                        // If tile fully left of the camera, move it to the right of the other tile
                        if (tile.position.x < mainCamera.transform.position.x - tileWidth)
                        {
                            tile.position = new Vector3(
                                other.position.x + tileWidth,
                                tile.position.y,
                                tile.position.z
                            );
                        }
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
        if (!mainCamera) mainCamera = Camera.main;
        MoveParallax(2f);
    }

#if UNITY_EDITOR
    // So inspector buttons work in edit mode
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            // Only run on inspector changes, not at runtime
            if (mainCamera == null) mainCamera = Camera.main;
        }
    }
#endif
}