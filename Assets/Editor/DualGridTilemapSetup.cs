using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class DualGridTilemapSetup
{
    [MenuItem("Tools/Setup Dual Grid")]
    public static void SetupDualGrid()
    {
        try
        {
            var settings = Selection.activeObject as DualGridSetupSettings;
            if (settings == null)
            {
                var candidates = AssetDatabase.FindAssets($"t:{nameof(DualGridSetupSettings)}");
                if (candidates.Length != 1)
                    throw new InvalidOperationException("Create one Tiles > Dual Grid Setup Settings asset, "
                        + "or select the settings asset to use when several exist.");
                settings = AssetDatabase.LoadAssetAtPath<DualGridSetupSettings>(
                    AssetDatabase.GUIDToAssetPath(candidates[0]));
            }

            var selected = Selection.activeGameObject;
            var target = selected == null ? null : selected.GetComponentInParent<DualGridTilemap>(true);
            var scene = target == null ? SceneManager.GetActiveScene() : target.gameObject.scene;
            var dualGrid = SetupDualGrid(scene, settings.terrain, target);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Dual Grid setup complete. Paint the data tilemap with {settings.terrain.name}.", dualGrid);
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError(exception.Message);
        }
    }

    // Scene and asset identity come from references; names below are only initial display labels.
    public static DualGridTilemap SetupDualGrid(Scene scene, DualGridTerrainTile terrain,
        DualGridTilemap target = null)
    {
        if (terrain == null)
            throw new InvalidOperationException("Assign a terrain asset in Dual Grid Setup Settings before running setup.");
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Open a scene before running Dual Grid setup.");
        if (target != null && target.gameObject.scene != scene)
            throw new InvalidOperationException("The selected grid must belong to the scene being configured.");

        if (target == null)
        {
            var candidates = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DualGridTilemap>(true)).ToArray();
            if (candidates.Length > 1)
                throw new InvalidOperationException("This scene has several dual grids. Select the grid to configure.");
            target = candidates.SingleOrDefault();
        }
        if (target != null && target.data != null && target.data == target.visual)
            throw new InvalidOperationException("Data and Visual must reference different tilemaps.");

        if (target == null)
        {
            var root = new GameObject("Dual Grid", typeof(Grid));
            SceneManager.MoveGameObjectToScene(root, scene);
            target = root.AddComponent<DualGridTilemap>();
        }

        var grid = GetOrAdd<Grid>(target.gameObject);
        var data = target.data != null ? target.data : CreateTilemap(target.transform, "Data");
        var visual = target.visual != null ? target.visual : CreateTilemap(target.transform, "Visual");
        data.transform.localPosition = Vector3.zero;
        visual.transform.localPosition = new Vector3(-grid.cellSize.x * 0.5f, -grid.cellSize.y * 0.5f, 0f);
        GetOrAdd<TilemapRenderer>(data.gameObject).sortingOrder = -1;
        GetOrAdd<TilemapRenderer>(visual.gameObject).sortingOrder = 0;

        var tilemapCollider = GetOrAdd<TilemapCollider2D>(data.gameObject);
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        GetOrAdd<CompositeCollider2D>(data.gameObject);
        GetOrAdd<Rigidbody2D>(data.gameObject).bodyType = RigidbodyType2D.Static;

        target.data = data;
        target.visual = visual;
        // Upgrade ordinary painted tiles using the explicitly configured terrain.
        var paintedTiles = new TileBase[data.GetUsedTilesCount()];
        data.GetUsedTilesNonAlloc(paintedTiles);
        foreach (var paintedTile in paintedTiles)
            if (!(paintedTile is DualGridTerrainTile)) data.SwapTile(paintedTile, terrain);
        target.RebuildAll();

        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(visual);
        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(scene);
        return target;
    }

    private static Tilemap CreateTilemap(Transform parent, string displayName)
    {
        var child = new GameObject(displayName, typeof(Tilemap), typeof(TilemapRenderer));
        child.transform.SetParent(parent, false);
        return child.GetComponent<Tilemap>();
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        return component == null ? gameObject.AddComponent<T>() : component;
    }
}
