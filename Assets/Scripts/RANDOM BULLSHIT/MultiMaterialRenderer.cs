using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Applies multiple materials to a SpriteRenderer or TilemapRenderer by spawning
/// one child GameObject per additional material, each offset by sortingOrder.
///
/// List order = layer order:
///   index 0  → this renderer (base layer, original sortingOrder)
///   index 1  → child #1      (sortingOrder + 1)
///   index 2  → child #2      (sortingOrder + 2)
///   …
///
/// Supports:
///   • SpriteRenderer
///   • TilemapRenderer  (requires a Tilemap component on the same GameObject)
/// </summary>
[ExecuteAlways]
public class MultiMaterialRenderer : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Tooltip("Materials in layer order. Index 0 = base (this renderer). " +
             "Each subsequent entry spawns a child with sortingOrder + n.")]
    public List<Material> materials = new();

    [Tooltip("Name prefix used for the generated child GameObjects.")]
    public string childPrefix = "_MatLayer_";

    // ── Internal ─────────────────────────────────────────────────────────────
    readonly List<GameObject> _children = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void OnEnable() => Rebuild();
    void OnDisable() => DestroyChildren();

#if UNITY_EDITOR
    // Re-build whenever the inspector changes in Edit mode.
    void OnValidate() => UnityEditor.EditorApplication.delayCall += () =>
    {
        if (this) Rebuild();
    };
#endif

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Force a full rebuild of all child render layers.</summary>
    public void Rebuild()
    {
        DestroyChildren();

        if (materials == null || materials.Count == 0) return;

        // Identify what kind of renderer we're working with.
        var sr = GetComponent<SpriteRenderer>();
        var tmr = GetComponent<TilemapRenderer>();

        if (sr == null && tmr == null)
        {
            Debug.LogWarning($"[MultiMaterialRenderer] No SpriteRenderer or " +
                             $"TilemapRenderer found on '{name}'.", this);
            return;
        }

        // Apply the first material directly to this renderer (base layer).
        if (materials[0] != null)
        {
            if (sr != null) sr.material = materials[0];
            if (tmr != null) tmr.material = materials[0];
        }

        int baseSortingLayerID = sr != null ? sr.sortingLayerID : tmr.sortingLayerID;
        int baseSortingOrder = sr != null ? sr.sortingOrder : tmr.sortingOrder;

        // Spawn one child per additional material.
        for (int i = 1; i < materials.Count; i++)
        {
            if (materials[i] == null) continue;

            GameObject child = CreateChild(i, baseSortingLayerID, baseSortingOrder + i, sr, tmr);
            _children.Add(child);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    GameObject CreateChild(int index, int sortingLayerID, int sortingOrder,
                           SpriteRenderer sourceSR, TilemapRenderer sourceTMR)
    {
        var child = new GameObject($"{childPrefix}{index}")
        {
            hideFlags = HideFlags.DontSave   // shown in hierarchy but not saved
        };

        child.transform.SetParent(transform, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        Material mat = materials[index];

        // ── SpriteRenderer branch ────────────────────────────────────────────
        if (sourceSR != null)
        {
            var childSR = child.AddComponent<SpriteRenderer>();
            childSR.sprite = sourceSR.sprite;
            childSR.color = new Color(1f, 1f, 1f, 1f); // let shader handle alpha
            childSR.flipX = sourceSR.flipX;
            childSR.flipY = sourceSR.flipY;
            childSR.drawMode = sourceSR.drawMode;
            childSR.size = sourceSR.size;
            childSR.sortingLayerID = sortingLayerID;
            childSR.sortingOrder = sortingOrder;
            childSR.material = mat;
            return child;
        }

        // ── TilemapRenderer branch ───────────────────────────────────────────
        if (sourceTMR != null)
        {
            // The child needs its own Tilemap + TilemapRenderer.
            // We copy the Tilemap data reference so it renders the same tiles.
            var sourceTilemap = GetComponent<Tilemap>();

            var childTilemap = child.AddComponent<Tilemap>();
            var childTMR = child.AddComponent<TilemapRenderer>();

            if (sourceTilemap != null)
            {
                // Mirror the tilemap bounds / tile data via CopyAllTiles.
                childTilemap.ClearAllTiles();
                BoundsInt bounds = sourceTilemap.cellBounds;
                TileBase[] allTiles = sourceTilemap.GetTilesBlock(bounds);
                childTilemap.SetTilesBlock(bounds, allTiles);
                childTilemap.color = sourceTilemap.color;
                childTilemap.tileAnchor = sourceTilemap.tileAnchor;
                childTilemap.animationFrameRate = sourceTilemap.animationFrameRate;
            }

            childTMR.sortingLayerID = sortingLayerID;
            childTMR.sortingOrder = sortingOrder;
            childTMR.mode = sourceTMR.mode;
            childTMR.detectChunkCullingBounds = sourceTMR.detectChunkCullingBounds;
            childTMR.material = mat;
            return child;
        }

        return child; // unreachable
    }

    void DestroyChildren()
    {
        foreach (var child in _children)
        {
            if (child == null) continue;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
#endif
                Destroy(child);
        }
        _children.Clear();

        // Also clean up any stale children left from a previous session
        // (e.g. after a domain reload in the editor).
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var t = transform.GetChild(i);
            if (t.name.StartsWith(childPrefix))
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(t.gameObject);
                else
#endif
                    Destroy(t.gameObject);
            }
        }
    }
}