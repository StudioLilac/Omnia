var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root = new UnityEngine.GameObject("Dual grid verification", typeof(UnityEngine.Grid));
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
var objects = new System.Collections.Generic.List<UnityEngine.Object>();
var frameByMask = new[] { -1, 15, 8, 9, 0, 11, 14, 7, 13, 4, 1, 10, 3, 2, 5, 6 };
var checks = 0;
void Check(bool condition, string message) {
    checks++;
    if (!condition) throw new System.Exception(message);
}
UnityEngine.Tilemaps.Tilemap MakeMap(string name) {
    var go = new UnityEngine.GameObject(name, typeof(UnityEngine.Tilemaps.Tilemap), typeof(UnityEngine.Tilemaps.TilemapRenderer));
    go.transform.SetParent(root.transform, false);
    return go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
}
try {
    var data = MakeMap("Data");
    var visual = MakeMap("Visual");
    visual.transform.localPosition = new UnityEngine.Vector3(-0.5f, -0.5f, 0);
    var grid = root.AddComponent<DualGridTilemap>();
    grid.data = data;
    grid.visual = visual;
    var stone = UnityEngine.ScriptableObject.CreateInstance<DualGridTerrainTile>();
    var dirt = UnityEngine.ScriptableObject.CreateInstance<DualGridTerrainTile>();
    stone.name = "Stone";
    dirt.name = "Dirt";
    dirt.sortingOrder = 3;
    objects.Add(stone); objects.Add(dirt);
    foreach (var terrain in new[] { stone, dirt }) {
        for (int i = 0; i < 16; i++) {
            terrain.visualTiles[i] = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            objects.Add(terrain.visualTiles[i]);
        }
    }
    var positions = new[] {
        new UnityEngine.Vector3Int(-1, 0, 0), new UnityEngine.Vector3Int(0, 0, 0),
        new UnityEngine.Vector3Int(-1, -1, 0), new UnityEngine.Vector3Int(0, -1, 0)
    };
    void VerifyCell(int stoneMask, int dirtMask) {
        Check(visual.GetTile(UnityEngine.Vector3Int.zero) == null, "Unexpected artwork on the layer template");
        foreach (var terrain in new[] { stone, dirt }) {
            var mask = terrain == stone ? stoneMask : dirtMask;
            var map = visual.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>()
                .FirstOrDefault(m => m.name == terrain.name);
            var actual = map == null ? null : map.GetTile(UnityEngine.Vector3Int.zero);
            var expected = mask == 0 ? null : terrain.visualTiles[frameByMask[mask]];
            Check(actual == expected, terrain.name + " has wrong tile for mask " + mask);
            if (map != null) {
                Check(map.transform.localPosition == UnityEngine.Vector3.zero, "Layer offset changed");
                Check(map.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder == terrain.sortingOrder, "Layer sorting");
            }
        }
    }
    // Exhaust all 3^4 assignments. Successive patterns also replace existing terrain.
    for (int pattern = 0; pattern < 81; pattern++) {
        int value = pattern, stoneMask = 0, dirtMask = 0;
        for (int corner = 0; corner < 4; corner++) {
            int type = value % 3;
            value /= 3;
            var tile = type == 1 ? stone : type == 2 ? dirt : null;
            data.SetTile(positions[corner], tile);
            if (type == 1) stoneMask |= 1 << corner;
            if (type == 2) dirtMask |= 1 << corner;
            if (tile != null) Check(data.GetColliderType(positions[corner]) == UnityEngine.Tilemaps.Tile.ColliderType.Grid, "Terrain collision");
        }
        VerifyCell(stoneMask, dirtMask);
        grid.RebuildAll();
        VerifyCell(stoneMask, dirtMask);
    }
    data.ClearAllTiles();
    foreach (var map in visual.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>())
        Check(map.GetUsedTilesCount() == 0, "Stale artwork after erase");
    // Undo/redo must restore both the data identity and all derived layers.
    UnityEditor.Undo.RegisterCompleteObjectUndo(data, "Verify dual grid terrain");
    data.SetTile(UnityEngine.Vector3Int.zero, stone);
    UnityEditor.Undo.FlushUndoRecordObjects();
    UnityEditor.Undo.PerformUndo();
    Check(data.GetTile(UnityEngine.Vector3Int.zero) == null, "Undo data");
    VerifyCell(0, 0);
    UnityEditor.Undo.PerformRedo();
    Check(data.GetTile(UnityEngine.Vector3Int.zero) == stone, "Redo data");
    VerifyCell(2, 0);
    UnityEditor.Undo.ClearUndo(data);
    // A second owner cannot share the first owner's generated layers.
    var otherRoot = new UnityEngine.GameObject("Second grid", typeof(UnityEngine.Grid));
    otherRoot.transform.SetParent(root.transform, false);
    var other = otherRoot.AddComponent<DualGridTilemap>();
    var otherData = new UnityEngine.GameObject("Data", typeof(UnityEngine.Tilemaps.Tilemap));
    otherData.transform.SetParent(otherRoot.transform, false);
    other.data = otherData.GetComponent<UnityEngine.Tilemaps.Tilemap>();
    var otherVisual = new UnityEngine.GameObject("Visual", typeof(UnityEngine.Tilemaps.Tilemap), typeof(UnityEngine.Tilemaps.TilemapRenderer));
    otherVisual.transform.SetParent(otherRoot.transform, false);
    other.visual = otherVisual.GetComponent<UnityEngine.Tilemaps.Tilemap>();
    other.data.SetTile(UnityEngine.Vector3Int.zero, dirt);
    Check(other.visual.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Length == 2, "Second grid layer count");
    VerifyCell(2, 0);
    // Four identities can contribute separate corner pieces to the same cell.
    var four = new[] { stone, dirt,
        UnityEngine.ScriptableObject.CreateInstance<DualGridTerrainTile>(),
        UnityEngine.ScriptableObject.CreateInstance<DualGridTerrainTile>() };
    objects.Add(four[2]); objects.Add(four[3]);
    four[2].name = "Sand"; four[3].name = "Snow";
    four[2].visualTiles = stone.visualTiles;
    four[3].visualTiles = dirt.visualTiles;
    for (int i = 0; i < 4; i++) data.SetTile(positions[i], four[i]);
    for (int i = 0; i < 4; i++) {
        var layer = visual.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Single(m => m.name == four[i].name);
        Check(layer.GetTile(UnityEngine.Vector3Int.zero) == four[i].visualTiles[frameByMask[1 << i]], "Four-material corner");
    }
    grid.RebuildAll();
    Check(visual.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Length == 5, "Rebuild duplicated layers");
    // Ordinary tiles have no terrain identity and must not join a representative.
    var ordinary = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
    objects.Add(ordinary);
    data.ClearAllTiles();
    data.SetTile(positions[0], ordinary);
    data.SetTile(positions[1], stone);
    VerifyCell(2, 0);
    return "PASS: 81 mixed-terrain arrangements, incremental/rebuild parity, replacement, erasure, collision, sorting, undo/redo, multiple grids; " + checks + " assertions.";
} finally {
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
    foreach (var obj in objects) UnityEngine.Object.DestroyImmediate(obj);
}
