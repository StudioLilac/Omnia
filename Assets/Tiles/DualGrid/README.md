# Dual-grid terrain

Paint the **Data** tilemap with `DualGridTerrainTile` assets. Each asset is one
terrain type and contains its own 16 visual tiles. The **Dual Grid** component
only needs Data and Visual references.

1. In the Project window, use **Create > Tiles > Dual Grid Terrain** (or duplicate
   `ExampleTerrain.asset`). Give the asset a material name such as Stone or Dirt.
2. Set its Sprite to a recognizable placeholder image for painting.
3. Assign its **Visual Tiles** in 4x4 sheet order: left to right, top to bottom.
   Keep all 16 slots, including empty sheet slots. This uses the same SpriteCook
   layout as the original renderer; slot 12 is blank in the example artwork.
4. Add the representative asset to a Tile Palette and paint it onto Data.

Cells connect only when they contain the **same representative asset**. Stone
and Dirt calculate their edges independently, treating each other as empty.
Two different representative assets remain distinct even if their visual tiles
are identical. Ordinary Unity Tiles have no terrain artwork and are ignored.

Visual is the renderer template and half-cell offset. The component creates a
child Tilemap for each encountered terrain, allowing several materials to draw
pieces at the same visual cell. These layers are derived from Data; edit Data
and use the component's **Rebuild All** menu if needed. Painting, erasing,
replacing, editor undo/redo, scene loading and editing a terrain asset update
the derived artwork. Each representative provides full-cell Grid collision;
the generated layers do not add colliders.

Use a terrain's **Sorting Order** to control overlap at shared edges: higher
values draw above lower ones, relative to Visual's sorting order. Give terrain
types distinct orders when a particular overlap order is required. Artwork
transparency still controls which underlying pixels are visible.

**Tools > Setup Dual Grid** sets up the example terrain. When upgrading an old
Data tilemap, it replaces ordinary painted tiles with the example representative,
preserving their occupied cells. Existing terrain representatives are preserved.

Regression check (requires a connected Unity Editor):

```powershell
unity command eval_file --file Tests/DualGridVerification.cs --format json
```

This runs in a temporary preview scene and checks all 81 four-corner arrangements
of empty/Stone/Dirt, incremental updates versus full rebuilds, replacement,
erasure, collision, sorting, undo/redo and independent grids.
