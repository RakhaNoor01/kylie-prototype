using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Tilemaps;

struct UnbakeEntry
{
    public Vector3Int position;
    public TileBase autoTile;
}

public static class UnbakeSelectedTiles
{
    private static readonly Dictionary<string, TileBase> autoTileCache = new();

    [MenuItem("Tools/Unbake Selected AutoTiles")]
    private static void Unbake()
    {
        if (GridSelection.target == null)
        {
            Debug.LogError("Select tiles on a Tilemap first");
            return;
        }

        Tilemap tilemap = GridSelection.target.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Grid selection target is not a Tilemap");
            return;
        }

        BoundsInt bounds = GridSelection.position;

        Undo.RegisterCompleteObjectUndo(tilemap, "Unbake Tiles");

        List<UnbakeEntry> entries = new();

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            Tile tile = tilemap.GetTile<Tile>(pos);

            if (tile == null)
                continue;

            string tileName = tile.name;

            if (!tileName.EndsWith("_baked"))
                continue;

            string autoTileName = tileName.Substring(0, tileName.Length - "_baked".Length);

            if (string.IsNullOrEmpty(autoTileName))
                continue;

            if (!autoTileCache.TryGetValue(autoTileName, out TileBase autoTile))
            {
                string[] guids = AssetDatabase.FindAssets($"{autoTileName} t:ScriptableObject");

                List<TileBase> matches = new();

                // Find AutoTile asset
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    TileBase asset = AssetDatabase.LoadAssetAtPath<TileBase>(path);

                    if (asset != null && asset.GetType().Name == "AutoTile" && asset.name == autoTileName)
                    {
                        matches.Add(asset);
                    }
                }

                // Only continue if exactly 1 asset has the same name
                if (matches.Count == 0)
                {
                    Debug.LogWarning($"Couldn't find AutoTile '{autoTileName}'. Skipping");
                    autoTileCache[autoTileName] = null;
                    continue;
                }
                else if (matches.Count > 1)
                {
                    Debug.LogWarning($"Found {matches.Count} AutoTiles named '{autoTileName}'");
                    autoTileCache[autoTileName] = null;
                    continue;
                }
                else
                {
                    autoTile = matches[0];
                }

                autoTileCache[autoTileName] = autoTile;
            }

            if (autoTile == null)
                continue;

            entries.Add(new UnbakeEntry
            {
                position = pos,
                autoTile = autoTile
            });
        }

        foreach (var entry in entries)
        {
            tilemap.SetTile(entry.position, entry.autoTile);
        }

        EditorUtility.SetDirty(tilemap);

        Debug.Log($"Unbaked {entries.Count} tiles");
    }
}