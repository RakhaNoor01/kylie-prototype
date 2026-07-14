// Assets/Editor/BakeSelectedAutoTiles.cs

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

struct BakeEntry
{
    public Vector3Int position;
    public Tile tile;
}

public static class BakeSelectedAutoTiles
{
    private static readonly Dictionary<Sprite, Tile> tileCache = new();
    private static string thing = "[BakeAutoTiles]";

    [MenuItem("Tools/Bake Selected AutoTiles")]
    private static void Bake()
    {
        if (GridSelection.target == null)
        {
            Debug.LogError($"{thing} Select tiles on a Tilemap first");
            return;
        }

        Tilemap tilemap = GridSelection.target.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError($"{thing} Grid selection target is not a Tilemap");
            return;
        }

        BoundsInt bounds = GridSelection.position;

        Undo.RegisterCompleteObjectUndo(tilemap, "Bake AutoTiles");

        List<BakeEntry> bakeEntries = new();

        // Evaluate AutoTile data
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            TileBase tileBase = tilemap.GetTile(pos);

            if (tileBase == null || tileBase.GetType().Name != "AutoTile")
                continue;

            TileData data = default;
            tileBase.GetTileData(pos, tilemap, ref data);

            if (data.sprite == null)
                continue;

            if (!tileCache.TryGetValue(data.sprite, out Tile tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = data.sprite;
                tile.colliderType = data.colliderType;
                tile.flags = TileFlags.LockColor;
                tile.color = Color.white;

                tileCache.Add(data.sprite, tile);
            }

            bakeEntries.Add(new BakeEntry
            {
                position = pos,
                tile = tile
            });
        }

        // Go through recorded AutoTile data and replace each one with a normal tile
        foreach (var entry in bakeEntries)
        {
            tilemap.SetTile(entry.position, entry.tile);
        }

        EditorUtility.SetDirty(tilemap);

        Debug.Log($"{thing} Baked {bakeEntries.Count} AutoTiles");
    }
}