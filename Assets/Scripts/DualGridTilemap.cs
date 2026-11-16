using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DualGridTilemap : MonoBehaviour
{
    public Tilemap data;
    public Tilemap visual;
    [Serializable]
    private class TerrainLayer
    {
        public DualGridTerrainTile terrain;
        public Tilemap tilemap;
    }

    [SerializeField, HideInInspector]
    private List<TerrainLayer> terrainLayers = new List<TerrainLayer>();

    // Index = mask, value = sheet slot, -1 = draw nothing.
    // Bits: TL=1, TR=2, BL=4, BR=8. Valid for the SpriteCook 4x4 layout.
    private static readonly int[] FrameByMask = { -1, 15, 8, 9, 0, 11, 14, 7, 13, 4, 1, 10, 3, 2, 5, 6 };

    private void OnEnable()
    {
        Tilemap.tilemapTileChanged += OnChanged;
#if UNITY_EDITOR
        Undo.undoRedoPerformed += RebuildAfterUndoRedo;
        EditorApplication.delayCall += RebuildAfterUndoRedo;
#else
        RebuildAll();
#endif
    }

    private void OnDisable()
    {
        Tilemap.tilemapTileChanged -= OnChanged;
#if UNITY_EDITOR
        Undo.undoRedoPerformed -= RebuildAfterUndoRedo;
        EditorApplication.delayCall -= RebuildAfterUndoRedo;
#endif
    }

#if UNITY_EDITOR
    // Tilemap tile-change events are not guaranteed to fire while Unity restores history.
    // Visual is derived data, so rebuild it after every editor Undo or Redo.
    private void RebuildAfterUndoRedo()
    {
        if (this != null && isActiveAndEnabled) RebuildAll();
    }
#endif

    private void OnChanged(Tilemap tilemap, Tilemap.SyncTile[] changes)
    {
        if (tilemap != data || visual == null || data == visual)
            return;

        foreach (var change in changes)
        {
            for (var dx = 0; dx <= 1; dx++)
            {
                for (var dy = 0; dy <= 1; dy++)
                    UpdateVisual(change.position.x + dx, change.position.y + dy);
            }
        }
    }

    private void UpdateVisual(int x, int y)
    {
        if (data == null || visual == null || data == visual)
            return;

        var position = new Vector3Int(x, y, 0);
        var tl = data.GetTile(new Vector3Int(x - 1, y, 0));
        var tr = data.GetTile(new Vector3Int(x, y, 0));
        var bl = data.GetTile(new Vector3Int(x - 1, y - 1, 0));
        var br = data.GetTile(new Vector3Int(x, y - 1, 0));

        // Clear every material at this cell, including the material just erased/replaced.
        visual.SetTile(position, null);
        foreach (var layer in terrainLayers)
            if (layer.tilemap != null) layer.tilemap.SetTile(position, null);

        DrawTerrain(tl as DualGridTerrainTile, position, tl, tr, bl, br);
        if (tr != tl) DrawTerrain(tr as DualGridTerrainTile, position, tl, tr, bl, br);
        if (bl != tl && bl != tr) DrawTerrain(bl as DualGridTerrainTile, position, tl, tr, bl, br);
        if (br != tl && br != tr && br != bl) DrawTerrain(br as DualGridTerrainTile, position, tl, tr, bl, br);

    }

    private void DrawTerrain(DualGridTerrainTile terrain, Vector3Int position,
        TileBase tl, TileBase tr, TileBase bl, TileBase br)
    {
        if (terrain == null)
            return;

        var mask = (tl == terrain ? 1 : 0) | (tr == terrain ? 2 : 0)
            | (bl == terrain ? 4 : 0) | (br == terrain ? 8 : 0);
        if (mask != 0)
            GetLayer(terrain).SetTile(position, terrain.GetVisualTile(FrameByMask[mask], position));
    }

    private Tilemap GetLayer(DualGridTerrainTile terrain)
    {
        foreach (var layer in terrainLayers)
        {
            if (layer.terrain != terrain || layer.tilemap == null) continue;
            return layer.tilemap;
        }

        var child = new GameObject(terrain.name, typeof(Tilemap), typeof(TilemapRenderer));
        child.transform.SetParent(visual.transform, false);
        child.layer = visual.gameObject.layer;
        var tilemap = child.GetComponent<Tilemap>();
        terrainLayers.Add(new TerrainLayer { terrain = terrain, tilemap = tilemap });
        ConfigureLayer(tilemap, terrain);
        return tilemap;
    }

    private void ConfigureLayer(Tilemap tilemap, DualGridTerrainTile terrain)
    {
        tilemap.tileAnchor = visual.tileAnchor;
        tilemap.color = visual.color;
        tilemap.orientation = visual.orientation;
        tilemap.orientationMatrix = visual.orientationMatrix;
        var source = visual.GetComponent<TilemapRenderer>();
        var renderer = tilemap.GetComponent<TilemapRenderer>();
        if (source == null || renderer == null) return;
        renderer.sharedMaterials = source.sharedMaterials;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + terrain.sortingOrder;
        renderer.mode = source.mode;
        renderer.sortOrder = source.sortOrder;
        renderer.maskInteraction = source.maskInteraction;
        renderer.enabled = source.enabled;
    }

    [ContextMenu("Rebuild All")]
    public void RebuildAll()
    {
        if (data == null || visual == null || data == visual)
            return;

        visual.ClearAllTiles();
        terrainLayers.RemoveAll(layer => layer.tilemap == null);
        foreach (var layer in terrainLayers)
        {
            layer.tilemap.ClearAllTiles();
            if (layer.terrain != null) ConfigureLayer(layer.tilemap, layer.terrain);
        }
        data.CompressBounds();
        var bounds = data.cellBounds;
        for (var x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (var y = bounds.yMin; y <= bounds.yMax; y++)
                UpdateVisual(x, y);
        }
    }
}
