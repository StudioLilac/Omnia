using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Creates the tile assets and representative terrain asset used by <see cref="DualGridTilemap"/>.
/// Select one or more 4 x 4 sliced textures in the Project window, then use the Assets context menu.
/// </summary>
public static class DualGridTerrainTileGenerator
{
    private const string SourceRoot = "Assets/Sprites/Tiles/";
    private const string OutputRoot = "Assets/Tiles/";
    private const int GridSize = 4;
    private const int SlotCount = GridSize * GridSize;
    private const int PreviewSlot = 6;

    [MenuItem("Assets/Generate Dual-Grid Terrain Tile", true)]
    private static bool ValidateGenerate()
    {
        if (Selection.objects.Length == 0) return false;

        foreach (var selected in Selection.objects)
        {
            if (!(selected is Texture2D)) return false;
            if (AssetDatabase.GetAssetPath(selected).Length == 0) return false;
        }

        return true;
    }

    [MenuItem("Assets/Generate Dual-Grid Terrain Tile")]
    private static void Generate()
    {
        var generated = new List<UnityEngine.Object>();
        var failures = new List<string>();

        foreach (var selected in Selection.objects)
        {
            var texture = selected as Texture2D;
            if (texture == null) continue;

            try
            {
                generated.Add(Generate(texture));
            }
            catch (Exception exception)
            {
                failures.Add(texture.name + ": " + exception.Message);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (generated.Count > 0)
        {
            Selection.objects = generated.ToArray();
            EditorGUIUtility.PingObject(generated[0]);
        }

        if (failures.Count > 0)
        {
            Debug.LogError("Dual-grid terrain generation failed:\n- " + string.Join("\n- ", failures));
        }
        else
        {
            Debug.Log("Created or updated " + generated.Count + " dual-grid terrain tile"
                + (generated.Count == 1 ? "." : "s."));
        }
    }

    private static DualGridTerrainTile Generate(Texture2D texture)
    {
        var sourcePath = AssetDatabase.GetAssetPath(texture);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
        var slots = BuildSlots(sourcePath, sprites);
        var outputFolder = GetOutputFolder(sourcePath, texture.name);
        var slotsFolder = outputFolder + "/Slots";
        EnsureFolder(slotsFolder);

        var tileSet = new DualGridTerrainTile.TileSet { tiles = new TileBase[SlotCount] };
        for (var slot = 0; slot < SlotCount; slot++)
        {
            var sprite = slots[slot];
            if (sprite == null) continue;

            var tilePath = slotsFolder + "/" + texture.name + "_slot" + slot + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(tilePath) != null)
                    throw new InvalidOperationException("Cannot create a Tile because '" + tilePath + "' already contains another asset type.");

                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            tileSet.tiles[slot] = tile;
        }

        var terrainPath = outputFolder + "/" + texture.name + ".asset";
        var terrain = AssetDatabase.LoadAssetAtPath<DualGridTerrainTile>(terrainPath);
        if (terrain == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(terrainPath) != null)
                throw new InvalidOperationException("Cannot create a representative tile because '" + terrainPath + "' already contains another asset type.");

            terrain = ScriptableObject.CreateInstance<DualGridTerrainTile>();
            AssetDatabase.CreateAsset(terrain, terrainPath);
        }

        terrain.name = texture.name;
        terrain.sprite = slots[PreviewSlot] ?? FirstSprite(slots);
        terrain.colliderType = Tile.ColliderType.Grid;
        terrain.tileSets = new[] { tileSet };
        EditorUtility.SetDirty(terrain);
        return terrain;
    }

    private static Sprite[] BuildSlots(string sourcePath, UnityEngine.Object[] assets)
    {
        var sprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            var sprite = asset as Sprite;
            if (sprite != null) sprites.Add(sprite);
        }

        if (sprites.Count == 0)
            throw new InvalidOperationException("This texture has no sliced sprites. Slice it as a 4 x 4 grid first.");
        if (sprites.Count < SlotCount - 1 || sprites.Count > SlotCount)
            throw new InvalidOperationException("'" + sourcePath + "' must contain 15 or 16 equally sized slices for a 4 x 4 dual-grid tile set.");

        var cellWidth = sprites[0].rect.width;
        var cellHeight = sprites[0].rect.height;
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        foreach (var sprite in sprites)
        {
            if (!Mathf.Approximately(sprite.rect.width, cellWidth) || !Mathf.Approximately(sprite.rect.height, cellHeight))
                throw new InvalidOperationException("All slices must have the same dimensions.");

            minX = Mathf.Min(minX, sprite.rect.x);
            minY = Mathf.Min(minY, sprite.rect.y);
        }

        var result = new Sprite[SlotCount];
        foreach (var sprite in sprites)
        {
            var column = Mathf.RoundToInt((sprite.rect.x - minX) / cellWidth);
            var rowFromBottom = Mathf.RoundToInt((sprite.rect.y - minY) / cellHeight);
            if (column < 0 || column >= GridSize || rowFromBottom < 0 || rowFromBottom >= GridSize)
                throw new InvalidOperationException("Slices must occupy a 4 x 4 grid.");

            var slot = (GridSize - 1 - rowFromBottom) * GridSize + column;
            if (result[slot] != null)
                throw new InvalidOperationException("More than one slice occupies slot " + slot + ".");

            result[slot] = sprite;
        }

        return result;
    }

    private static Sprite FirstSprite(Sprite[] sprites)
    {
        foreach (var sprite in sprites)
            if (sprite != null) return sprite;
        return null;
    }

    private static string GetOutputFolder(string sourcePath, string textureName)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath).Replace('\\', '/');
        var relativeDirectory = sourceDirectory.StartsWith(SourceRoot, StringComparison.OrdinalIgnoreCase)
            ? sourceDirectory.Substring(SourceRoot.Length)
            : "Generated";
        return OutputRoot + relativeDirectory + "/" + SanitizeFileName(textureName);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            name = name.Replace(invalidCharacter, '_');
        return name;
    }

    private static void EnsureFolder(string folder)
    {
        var current = "Assets";
        var segments = folder.Substring("Assets/".Length).Split('/');
        foreach (var segment in segments)
        {
            var next = current + "/" + segment;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segment);
            current = next;
        }
    }
}
