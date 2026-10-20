using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class DualGridTilemap : MonoBehaviour
{
    public Tilemap data;
    public Tilemap visual;
    public TileBase[] tiles = new TileBase[16]; // Sheet order, row-major. Blank sheet slots stay null.

    // Index = mask, value = sheet slot, -1 = draw nothing.
    // Bits: TL=1, TR=2, BL=4, BR=8. Valid for the SpriteCook 4x4 layout.
    private static readonly int[] FrameByMask = { -1, 15, 8, 9, 0, 11, 14, 7, 13, 4, 1, 10, 3, 2, 5, 6 };

    private void OnEnable()
    {
        Tilemap.tilemapTileChanged += OnChanged;
    }

    private void OnDisable()
    {
        Tilemap.tilemapTileChanged -= OnChanged;
    }

    private void OnChanged(Tilemap tilemap, Tilemap.SyncTile[] changes)
    {
        if (tilemap != data || visual == null)
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

    private bool Filled(int x, int y)
    {
        return data != null && data.HasTile(new Vector3Int(x, y, 0));
    }

    private void UpdateVisual(int x, int y)
    {
        if (visual == null || tiles == null || tiles.Length < 16)
            return;

        var mask = 0;
        if (Filled(x - 1, y)) mask |= 1;     // top-left (Unity y is up)
        if (Filled(x, y)) mask |= 2;         // top-right
        if (Filled(x - 1, y - 1)) mask |= 4; // bottom-left
        if (Filled(x, y - 1)) mask |= 8;     // bottom-right

        var frame = FrameByMask[mask];
        visual.SetTile(new Vector3Int(x, y, 0), frame < 0 ? null : tiles[frame]);
    }

    [ContextMenu("Rebuild All")]
    public void RebuildAll()
    {
        if (data == null || visual == null)
            return;

        visual.ClearAllTiles();
        data.CompressBounds();
        var bounds = data.cellBounds;
        for (var x = bounds.xMin; x <= bounds.xMax + 1; x++)
        {
            for (var y = bounds.yMin; y <= bounds.yMax + 1; y++)
                UpdateVisual(x, y);
        }
    }
}
