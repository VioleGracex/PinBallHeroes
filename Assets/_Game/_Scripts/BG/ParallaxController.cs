    #region Fields
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
        [Tooltip("Prefab to spawn for this layer")]
        public GameObject tilePrefab;
        [HideInInspector]
        public Transform[] tiles;
        public float parallaxSpeed = 0.5f;
    }

    #endregion

    #region Inspector Buttons & Spawning

    [Button("Spawn Parallax Tiles")]
    public void SpawnParallaxTiles()
    {
        if (!mainCamera) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;

        // Destroy all old tiles and parents
        foreach (var layer in layers)
        {
            if (layer.tiles != null)
            {
                foreach (var t in layer.tiles)
                {
                    if (t != null)
                    {
                        #if UNITY_EDITOR
                        if (!Application.isPlaying)
                            DestroyImmediate(t.gameObject);
                        else
                            Destroy(t.gameObject);
                        #else
                        Destroy(t.gameObject);
                        #endif
                    }
                }
            }
            layer.tiles = null;
        }
        // Destroy old collection parents
        var oldCollections = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("ParallaxCollection_"))
                oldCollections.Add(child.gameObject);
        }
        #if UNITY_EDITOR
        foreach (var go in oldCollections)
        {
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
                Destroy(go);
        }
        #else
        foreach (var go in oldCollections)
            Destroy(go);
        #endif

        // Get max prefab width (for spacing)
        float maxPrefabWidth = 0f;
        foreach (var layer in layers)
        {
            if (layer.tilePrefab == null) continue;
            var sr = layer.tilePrefab.GetComponent<SpriteRenderer>();
            if (!sr) continue;
            float prefabWidth = sr.sprite.bounds.size.x * layer.tilePrefab.transform.localScale.x;
            if (prefabWidth > maxPrefabWidth) maxPrefabWidth = prefabWidth;
        }
        if (maxPrefabWidth == 0f) return;

        // How many collections needed to fill screen + 1 extra
        int needed = Mathf.Max(2, Mathf.CeilToInt(worldScreenWidth / maxPrefabWidth) + 1);

        // Each collection contains one tile from each layer
        for (int i = 0; i < needed; i++)
        {
            GameObject collection = new GameObject($"ParallaxCollection_{i}");
            collection.transform.SetParent(this.transform);
            float x = mainCamera.transform.position.x - worldScreenWidth / 2f + maxPrefabWidth * i + maxPrefabWidth / 2f;
            // For each layer, spawn one tile in this collection
            for (int l = 0; l < layers.Length; l++)
            {
                var layer = layers[l];
                if (layer.tilePrefab == null) continue;
                GameObject go = null;
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                    go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(layer.tilePrefab);
                else
                    go = Instantiate(layer.tilePrefab);
                #else
                go = Instantiate(layer.tilePrefab);
                #endif
                go.name = layer.tilePrefab.name + "_Tile_" + i;
                go.transform.SetParent(collection.transform);
                // Place horizontally at x, keep y/z from prefab
                Vector3 pos = go.transform.position;
                pos.x = x;
                go.transform.position = pos;
                // Track tiles for each layer
                if (layer.tiles == null)
                {
                    layer.tiles = new Transform[needed];
                }
                layer.tiles[i] = go.transform;
            }
        }
        // After spawning, adapt and align
        AdaptTilesToScreen();
    }

    #endregion

    #region Fields (Serialized)
    [Header("Background (fills camera)")]
    public Transform background; // Assign a Transform with a SpriteRenderer
    float backgroundDepth = 10f; // Z position for background

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
    #endregion

    #region Unity Methods
    void Start()
    {
        SpawnParallaxTiles();
        AdaptBackgroundToScreen();
    }
    #endregion

    #region Adapt & Align

    [Button("Adapt Background & Tiles To Screen")]
    public void AdaptAllToScreen()
    {
        AdaptBackgroundToScreen();
        AdaptTilesToScreen();
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
        bgPos.y = mainCamera.transform.position.y;
        bgPos.x = mainCamera.transform.position.x; // Always center horizontally
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

    #endregion

    // No longer needed: background is always centered, collections are placed by spawner


    #region Parallax Logic
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
    #endregion

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