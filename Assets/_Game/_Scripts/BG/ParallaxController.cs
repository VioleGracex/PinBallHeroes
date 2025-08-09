using UnityEngine;
using System.Collections;
using NaughtyAttributes;
using System.Collections.Generic;

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
        public Transform[] tiles; // One tile per collection
        public float parallaxSpeed = 0.5f;
    }

    [Header("Background (fills camera)")]
    public Transform background;
    public float backgroundDepth = 10f;

    [Header("Scaling")]
    public bool keepAspect = true;

    [Header("Parallax Layers")]
    public ParallaxLayer[] layers;
    public float scrollSpeed = 2f;
    private Coroutine parallaxCoroutine;
    public Camera mainCamera;

    [Header("Top Area Percentage")]
    [Range(0f, 1f)]
    public float topAreaPercent = 0.46f;

    [Header("Left Offset (World Units)")]
    public float leftOffset = -5.0f;

    [HideInInspector]
    public GameObject[] collections; // Each collection holds one tile from each layer

    [Button("Spawn Parallax Tiles")]
    public void SpawnParallaxTiles()
    {
        if (!mainCamera) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;

        // Destroy old collections (and their children)
        if (collections != null)
        {
            foreach (var col in collections)
            {
                if (col != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(col);
                    else
                        Destroy(col);
#else
                    Destroy(col);
#endif
                }
            }
        }
        collections = null;

        // Determine max tile width for spacing collections (the widest prefab among all layers)
        float maxTileWidth = 0f;
        foreach (var layer in layers)
        {
            if (layer.tilePrefab == null) continue;
            var sr = layer.tilePrefab.GetComponent<SpriteRenderer>();
            if (!sr) continue;
            float prefabWidth = sr.sprite.bounds.size.x * layer.tilePrefab.transform.localScale.x;
            if (prefabWidth > maxTileWidth) maxTileWidth = prefabWidth;
        }
        if (maxTileWidth == 0f) return;

        // Calculate number of needed collections to fill the screen + 2 for seamlessness
        int needed = Mathf.Max(2, Mathf.CeilToInt(worldScreenWidth / maxTileWidth) + 2);

        collections = new GameObject[needed];
        foreach (var layer in layers)
        {
            layer.tiles = new Transform[needed];
        }

        for (int i = 0; i < needed; i++)
        {
            GameObject collection = new GameObject($"ParallaxCollection_{i}");
            collection.transform.SetParent(this.transform);
            float x = mainCamera.transform.position.x - worldScreenWidth / 2f + maxTileWidth * i + maxTileWidth / 2f + leftOffset;
            Vector3 basePos = new Vector3(x, 0f, 0f);

            // For each layer, spawn one tile as a child of this collection
            for (int l = 0; l < layers.Length; l++)
            {
                var layer = layers[l];
                if (layer.tilePrefab == null) continue;
                GameObject go = null;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    go = (GameObject)PrefabUtility.InstantiatePrefab(layer.tilePrefab);
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
                layer.tiles[i] = go.transform;
            }
            collections[i] = collection;
        }
        AdaptTilesToScreen();
    }

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
        bgPos.x = mainCamera.transform.position.x;
        background.position = bgPos;
    }

    [Button("Adapt Parallax Tiles Only")]
    public void AdaptTilesToScreen()
    {
        if (!mainCamera) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;
        float worldScreenHeight = 2f * mainCamera.orthographicSize;
        float layerTargetHeight = worldScreenHeight * topAreaPercent;

        for (int l = 0; l < layers.Length; l++)
        {
            var layer = layers[l];
            if (layer.tiles == null) continue;

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
                    float scale = scaleY;
                    tile.localScale = new Vector3(scale, scale, tile.localScale.z);
                }
                else
                {
                    tile.localScale = new Vector3(scaleX, scaleY, tile.localScale.z);
                }

                // Position tile so its top edge aligns with camera top
                float tileWorldHeight = sr.bounds.size.y * tile.localScale.y;
                float cameraTopY = mainCamera.transform.position.y + worldScreenHeight / 2f;
                float tileTopY = tile.position.y + tileWorldHeight / 2f;
                float deltaY = cameraTopY - tileTopY;
                tile.position += new Vector3(0, deltaY, 0);
            }
        }
    }

    void Start()
    {
        SpawnParallaxTiles();
        AdaptBackgroundToScreen();
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
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;

        // Determine max tile width for wrapping
        float maxTileWidth = 0f;
        foreach (var layer in layers)
        {
            if (layer.tilePrefab == null) continue;
            var sr = layer.tilePrefab.GetComponent<SpriteRenderer>();
            if (!sr) continue;
            float prefabWidth = sr.sprite.bounds.size.x * layer.tilePrefab.transform.localScale.x;
            if (prefabWidth > maxTileWidth) maxTileWidth = prefabWidth;
        }

        while (timer < duration)
        {
            for (int i = 0; i < collections.Length; i++)
            {
                var collection = collections[i];
                if (!collection) continue;
                Vector3 pos = collection.transform.position;
                pos.x -= scrollSpeed * Time.deltaTime;
                collection.transform.position = pos;
            }

            // Wrapping logic for collections
            for (int i = 0; i < collections.Length; i++)
            {
                var collection = collections[i];
                if (!collection) continue;
                float leftEdge = mainCamera.transform.position.x - worldScreenWidth / 2f - maxTileWidth / 2f;

                if (collection.transform.position.x < leftEdge)
                {
                    // Find rightmost collection
                    float maxX = float.MinValue;
                    for (int j = 0; j < collections.Length; j++)
                    {
                        if (collections[j] && collections[j].transform.position.x > maxX)
                            maxX = collections[j].transform.position.x;
                    }
                    collection.transform.position = new Vector3(maxX + maxTileWidth, collection.transform.position.y, collection.transform.position.z);
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
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }
    }
#endif
}