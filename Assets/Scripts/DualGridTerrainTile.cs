using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>A paintable terrain identity and its dual-grid artwork.</summary>
[CreateAssetMenu(fileName = "Terrain", menuName = "Tiles/Dual Grid Terrain")]
public class DualGridTerrainTile : Tile
{
    [Tooltip("4x4 sheet order, left to right and top to bottom. Leave blank sheet slots empty.")]
    public TileBase[] visualTiles = new TileBase[16];

    [Tooltip("Higher values draw over other terrain at shared edges.")]
    public int sortingOrder;

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
    private void OnValidate()
    {
        colliderType = ColliderType.Grid;
        UnityEditor.EditorApplication.delayCall -= RefreshGrids;
        UnityEditor.EditorApplication.delayCall += RefreshGrids;
    }

    private void RefreshGrids()
    {
        if (this == null) return;
        foreach (var grid in Object.FindObjectsByType<DualGridTilemap>(FindObjectsSortMode.None))
            grid.RebuildAll();
    }
#endif
}
