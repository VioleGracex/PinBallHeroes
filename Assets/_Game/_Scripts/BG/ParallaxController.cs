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
        public Transform[] tiles; // Tiles for this layer
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

    [Button("Spawn Parallax Tiles")]
    public void SpawnParallaxTiles()
    {
        if (!mainCamera) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;

        // Destroy old tiles/children
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

        for (int l = 0; l < layers.Length; l++)
        {
            var layer = layers[l];
            if (layer.tilePrefab == null) continue;

            var sr = layer.tilePrefab.GetComponent<SpriteRenderer>();
            float prefabWidth = sr.sprite.bounds.size.x * layer.tilePrefab.transform.localScale.x;

            // Calculate needed tiles (+3 for seamless)
            int needed = Mathf.Max(3, Mathf.CeilToInt(worldScreenWidth / prefabWidth) + 2);
            layer.tiles = new Transform[needed];

            for (int i = 0; i < needed; i++)
            {
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
                go.transform.SetParent(this.transform);

                float x = mainCamera.transform.position.x - worldScreenWidth / 2f + prefabWidth * i + prefabWidth / 2f + leftOffset;
                Vector3 pos = go.transform.position;
                pos.x = x;
                go.transform.position = pos;
                layer.tiles[i] = go.transform;
            }
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

    // The corrected parallax routine: moves and wraps per-layer, honoring both scrollSpeed and parallaxSpeed
    private IEnumerator ParallaxRoutine(float duration)
    {
        float timer = 0f;
        if (!mainCamera) mainCamera = Camera.main;
        float worldScreenWidth = 2f * mainCamera.orthographicSize * mainCamera.aspect;

        while (timer < duration)
        {
            for (int l = 0; l < layers.Length; l++)
            {
                var layer = layers[l];
                if (layer.tiles == null) continue;
                if (layer.tilePrefab == null) continue;

                var sr = layer.tilePrefab.GetComponent<SpriteRenderer>();
                float tileWidth = sr.sprite.bounds.size.x * layer.tilePrefab.transform.localScale.x;

                // Move tiles
                for (int i = 0; i < layer.tiles.Length; i++)
                {
                    var tile = layer.tiles[i];
                    if (!tile) continue;
                    Vector3 pos = tile.position;
                    pos.x -= scrollSpeed * layer.parallaxSpeed * Time.deltaTime;
                    tile.position = pos;
                }

                // Wrap tiles for this layer (account for overshoot)
                for (int i = 0; i < layer.tiles.Length; i++)
                {
                    var tile = layer.tiles[i];
                    if (!tile) continue;
                    float leftEdge = mainCamera.transform.position.x - worldScreenWidth / 2f - tileWidth / 2f;

                    if (tile.position.x < leftEdge)
                    {
                        // Find rightmost tile in this layer
                        float maxX = float.MinValue;
                        for (int j = 0; j < layer.tiles.Length; j++)
                        {
                            if (layer.tiles[j] && layer.tiles[j].position.x > maxX)
                                maxX = layer.tiles[j].position.x;
                        }

                        float overshoot = leftEdge - tile.position.x;
                        tile.position = new Vector3(maxX + tileWidth - overshoot, tile.position.y, tile.position.z);
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
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }
    }
#endif
}