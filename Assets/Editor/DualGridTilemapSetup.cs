using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class DualGridTilemapSetup
{
    private const string RootName = "Dual Grid";
    private const string TileFolder = "Assets/Tiles/DualGrid";
    private const string PlaceholderTexturePath = TileFolder + "/DataPlaceholder.png";
    private const string PlaceholderTilePath = TileFolder + "/DataPlaceholder.asset";
    private const string ExampleTileFolder = "Assets/Tiles/Underground/Example";

    [MenuItem("Tools/Setup Dual Grid")]
    public static void SetupDualGrid()
    {
        var root = GameObject.Find(RootName);
        if (root == null)
            root = new GameObject(RootName);

        var grid = root.GetComponent<Grid>();
        if (grid == null)
            grid = root.AddComponent<Grid>();

        var data = GetOrCreateTilemap(root.transform, "Data");
        var visual = GetOrCreateTilemap(root.transform, "Visual");
        data.transform.localPosition = Vector3.zero;
        visual.transform.localPosition = new Vector3(-grid.cellSize.x * 0.5f, -grid.cellSize.y * 0.5f, 0f);

        var dataRenderer = data.GetComponent<TilemapRenderer>();
        dataRenderer.sortingOrder = -1;
        var visualRenderer = visual.GetComponent<TilemapRenderer>();
        visualRenderer.sortingOrder = 0;

        var tilemapCollider = GetOrAdd<TilemapCollider2D>(data.gameObject);
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        GetOrAdd<CompositeCollider2D>(data.gameObject);
        var body = GetOrAdd<Rigidbody2D>(data.gameObject);
        body.bodyType = RigidbodyType2D.Static;

        var dualGrid = GetOrAdd<DualGridTilemap>(root);
        dualGrid.data = data;
        dualGrid.visual = visual;
        dualGrid.tiles = LoadTilesInSheetOrder();

        // Ensure the Data palette tile exists and carries full-cell Grid collision.
        CreateOrUpdatePlaceholderTile();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(dualGrid);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dual Grid setup complete. Paint Data with Assets/Tiles/DualGrid/DataPlaceholder.asset.");
    }

    private static Tilemap GetOrCreateTilemap(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child == null)
        {
            var childObject = new GameObject(childName, typeof(Tilemap), typeof(TilemapRenderer));
            childObject.transform.SetParent(parent, false);
            return childObject.GetComponent<Tilemap>();
        }

        var tilemap = child.GetComponent<Tilemap>();
        if (tilemap == null)
            tilemap = child.gameObject.AddComponent<Tilemap>();
        if (child.GetComponent<TilemapRenderer>() == null)
            child.gameObject.AddComponent<TilemapRenderer>();
        return tilemap;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        return component == null ? gameObject.AddComponent<T>() : component;
    }

    private static TileBase[] LoadTilesInSheetOrder()
    {
        var result = new TileBase[16];
        foreach (var guid in AssetDatabase.FindAssets("t:Tile", new[] { ExampleTileFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null || tile.sprite == null)
                continue;

            var rect = tile.sprite.rect;
            var column = Mathf.RoundToInt(rect.x / rect.width);
            var rowFromBottom = Mathf.RoundToInt(rect.y / rect.height);
            var sheetSlot = column + (3 - rowFromBottom) * 4;
            if (sheetSlot >= 0 && sheetSlot < result.Length)
                result[sheetSlot] = tile;
        }

        if (result[12] != null)
            Debug.LogWarning("Expected ExamplePalette sheet slot 12 to be blank, but found a tile there.");
        for (var i = 0; i < result.Length; i++)
        {
            if (i != 12 && result[i] == null)
                Debug.LogWarning($"Missing ExamplePalette tile for sheet slot {i}.");
        }
        return result;
    }

    private static void CreateOrUpdatePlaceholderTile()
    {
        EnsureFolder("Assets/Tiles", "DualGrid");
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderTexturePath) == null)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[16 * 16];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color(1f, 0f, 1f, 0.32f);
            texture.SetPixels(pixels);
            texture.Apply();
            System.IO.File.WriteAllBytes(PlaceholderTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PlaceholderTexturePath, ImportAssetOptions.ForceUpdate);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderTexturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        var tile = AssetDatabase.LoadAssetAtPath<Tile>(PlaceholderTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, PlaceholderTilePath);
        }
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderTexturePath);
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }
}
