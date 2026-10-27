# Dual-grid terrain

Paint the **Data** tilemap with `DualGridTerrainTile` assets. Each asset is one
terrain type and contains its own collection of 16-tile sets. The **Dual Grid** component
only needs Data and Visual references.

1. In the Project window, use **Create > Tiles > Dual Grid Terrain** (or duplicate
   `Stone.asset` or `Dirt.asset`). Give the asset a material name such as Stone or Dirt.
2. Set its Sprite to a recognizable placeholder image for painting.
3. Assign the first entry in **Tile Sets** (the base set) in 4x4 sheet order:
   left to right, top to bottom.
   Keep all 16 slots, including empty sheet slots. This uses the same SpriteCook
   layout as the original renderer; slot 12 is blank in the example artwork.
4. Add the representative asset to a Tile Palette and paint it onto Data. Existing/Example representative tiles are found under `Assets/Tiles/RepresentativeTiles`.

Add more entries to **Tile Sets** for variations. Fill only the matching slots
that have alternative artwork; the remaining slots can stay null. Each drawn
cell chooses uniformly among its base tile and the non-null alternatives for
that slot. Null entries are skipped, so one available alternative gets a 50/50
chance with the base, and two get a one-third chance each. A blank base slot
always stays blank. The first set defines the terrain's shape.

Choices use the visual cell position and **Variation Seed**, so repainting,
rebuilds, undo/redo, scene reloads and player builds preserve the same pattern.
Change the seed to reshuffle a terrain. This does not consume Unity's gameplay
random state. Existing single-set assets automatically become base-only sets.

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

**Tools > Setup Dual Grid** uses the terrain reference in the **Dual Grid Setup
Settings** asset. Edit that asset to choose a different terrain, or create one via
**Create > Tiles > Dual Grid Setup Settings**. The terrain supplies its authored
sprite and artwork; setup does not generate or overwrite terrain assets. Unity
references survive moves and renames of the settings, terrain and artwork.

Setup reuses the scene's `DualGridTilemap` component and its Data/Visual references,
including inactive or renamed objects. With several grids, select the grid (or a
child) first. With several settings assets, select the settings to use. A new grid
is created only when the scene has none. Object names are initial display labels,
not identifiers. When upgrading an old Data tilemap, setup replaces ordinary
painted tiles with the configured representative, preserving occupied cells and
existing terrain representatives.

Regression check (requires a connected Unity Editor):

```powershell
unity command eval_file --file Tests/DualGridVerification.cs --format json
unity command eval_file --file Tests/DualGridSetupVerification.cs --format json
```

This runs in a temporary preview scene and checks all 81 four-corner arrangements
of empty/Stone/Dirt, incremental updates versus full rebuilds, replacement,
erasure, collision, sorting, undo/redo and independent grids, plus sparse
variation fallback, distribution, seed changes, stable rebuilds and asset migration.
The setup check also covers renamed and inactive objects, repeat setup, terrain
conversion, ambiguous grid selection, scene isolation and default asset references.
