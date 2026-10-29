using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>A paintable terrain identity and its dual-grid artwork.</summary>
[CreateAssetMenu(fileName = "Terrain", menuName = "Tiles/Dual Grid Terrain")]
public class DualGridTerrainTile : Tile, ISerializationCallbackReceiver
{
    [Serializable]
    public class TileSet
    {
        public TileBase[] tiles = new TileBase[16];
    }

    [Tooltip("First set is the base. Later sets supply optional alternatives in matching sheet slots.")]
    public TileSet[] tileSets = { new TileSet() };

    [Tooltip("Change this to reshuffle variations. Choices stay stable through repainting and rebuilds.")]
    public int variationSeed;

    // Upgrade assets authored before multiple sets were supported, without losing references.
    [SerializeField, HideInInspector, FormerlySerializedAs("visualTiles")]
    private TileBase[] legacyVisualTiles;

    [Tooltip("Higher values draw over other terrain at shared edges.")]
    public int sortingOrder;

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        if (legacyVisualTiles == null || legacyVisualTiles.Length == 0) return;
        tileSets = new[] { new TileSet { tiles = legacyVisualTiles } };
        legacyVisualTiles = null;
    }

    /// <summary>Uniform choice among the base and non-null alternatives for this slot.</summary>
    public TileBase GetVisualTile(int slot, Vector3Int position)
    {
        if (slot < 0 || slot >= 16 || tileSets == null || tileSets.Length == 0)
            return null;

        var baseTile = GetTile(tileSets[0], slot);
        // The base defines which slots draw anything, including intentional blank sheet cells.
        if (baseTile == null) return null;

        var count = 1;
        for (var i = 1; i < tileSets.Length; i++)
            if (GetTile(tileSets[i], slot) != null) count++;
        if (count == 1) return baseTile;

        // Stateless position hash: stable in editor/builds, and never consumes gameplay Random.
        uint hash;
        unchecked
        {
            hash = (uint)position.x * 0x9E3779B9u ^ (uint)position.y * 0x85EBCA6Bu
                ^ (uint)position.z * 0xC2B2AE35u ^ (uint)variationSeed;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;
        }
        var choice = (int)(((ulong)hash * (uint)count) >> 32);
        if (choice == 0) return baseTile;
        for (var i = 1; i < tileSets.Length; i++)
        {
            var alternative = GetTile(tileSets[i], slot);
            if (alternative != null && --choice == 0) return alternative;
        }
        return baseTile;
    }

    private static TileBase GetTile(TileSet set, int slot)
    {
        return set != null && set.tiles != null && slot < set.tiles.Length ? set.tiles[slot] : null;
    }

    private void Reset()
    {
        colliderType = ColliderType.Grid;
    }

    // Representative tiles always provide full-cell collision, independent of their sprite.
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);
        tileData.colliderType = ColliderType.Grid;
    }

#if UNITY_EDITOR
    public override void OnValidate()
    {
        colliderType = ColliderType.Grid;
        UnityEditor.EditorApplication.delayCall -= RefreshGrids;
        UnityEditor.EditorApplication.delayCall += RefreshGrids;
    }

    private void RefreshGrids()
    {
        if (this == null) return;
        foreach (var grid in UnityEngine.Object.FindObjectsByType<DualGridTilemap>())
            grid.RebuildAll();
    }
#endif
}
