var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var otherScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var ordinary = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
var otherTerrain = UnityEngine.ScriptableObject.CreateInstance<DualGridTerrainTile>();
var checks = 0;
void Check(bool condition, string message) {
    checks++;
    if (!condition) throw new System.Exception(message);
}
void Reject(System.Action action, string message) {
    bool rejected = false;
    try { action(); } catch (System.InvalidOperationException) { rejected = true; }
    Check(rejected, message);
}
try {
    var grid = DualGridTilemapSetup.SetupDualGrid(scene);
    var data = grid.data;
    var visual = grid.visual;
    grid.name = "Renamed owner";
    data.name = "Renamed source";
    visual.name = "Renamed output";
    grid.gameObject.SetActive(false);
    data.SetTile(UnityEngine.Vector3Int.zero, ordinary);
    data.SetTile(UnityEngine.Vector3Int.right, otherTerrain);
    Check(DualGridTilemapSetup.SetupDualGrid(scene) == grid, "Renamed inactive grid was duplicated");
    Check(grid.data == data && grid.visual == visual, "Renamed maps were duplicated");
    Check(data.GetTile(UnityEngine.Vector3Int.zero) == ordinary, "Ordinary tile was changed");
    Check(data.GetTile(UnityEngine.Vector3Int.right) == otherTerrain, "Existing terrain was overwritten");
    Check(data.GetComponent<UnityEngine.Rigidbody2D>().bodyType == UnityEngine.RigidbodyType2D.Static, "Collision body");
    Check(data.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>().compositeOperation == UnityEngine.Collider2D.CompositeOperation.Merge, "Composite collision");
    Check(visual.transform.localPosition == new UnityEngine.Vector3(-0.5f, -0.5f, 0), "Visual offset");
    Check(grid.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true).Count(m => m.transform.parent == grid.transform) == 2, "Repeated setup added maps");
    var unrelated = new UnityEngine.GameObject("Unrelated");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(unrelated, scene);
    var second = unrelated.AddComponent<DualGridTilemap>();
    Reject(() => DualGridTilemapSetup.SetupDualGrid(scene), "Ambiguous grid accepted");
    Check(DualGridTilemapSetup.SetupDualGrid(scene, second) == second, "Explicit grid selection failed");
    Reject(() => DualGridTilemapSetup.SetupDualGrid(otherScene, grid), "Cross-scene target accepted");
    Check(otherScene.rootCount == 0, "Wrong scene modified");
    second.visual = second.data;
    Reject(() => DualGridTilemapSetup.SetupDualGrid(scene, second), "Shared Data/Visual accepted");
    return "PASS: setup validation, rename/inactive/repeat safety, unchanged painted tiles, explicit/ambiguous targets and scene isolation; " + checks + " assertions.";
} finally {
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(otherScene);
    UnityEngine.Object.DestroyImmediate(otherTerrain);
    UnityEngine.Object.DestroyImmediate(ordinary);
}
