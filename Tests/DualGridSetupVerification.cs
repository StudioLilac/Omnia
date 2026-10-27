var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var otherScene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var terrain = UnityEngine.ScriptableObject.CreateInstance<DualGridTerrainTile>();
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
    Reject(() => DualGridTilemapSetup.SetupDualGrid(scene, null), "Missing terrain accepted");
    Check(scene.rootCount == 0, "Invalid setup changed scene");
    var grid = DualGridTilemapSetup.SetupDualGrid(scene, terrain);
    var data = grid.data;
    var visual = grid.visual;
    grid.name = "Renamed owner";
    data.name = "Renamed source";
    visual.name = "Renamed output";
    grid.gameObject.SetActive(false);
    data.SetTile(UnityEngine.Vector3Int.zero, ordinary);
    data.SetTile(UnityEngine.Vector3Int.right, otherTerrain);
    Check(DualGridTilemapSetup.SetupDualGrid(scene, terrain) == grid, "Renamed inactive grid was duplicated");
    Check(grid.data == data && grid.visual == visual, "Renamed maps were duplicated");
    Check(data.GetTile(UnityEngine.Vector3Int.zero) == terrain, "Ordinary tile was not upgraded");
    Check(data.GetTile(UnityEngine.Vector3Int.right) == otherTerrain, "Existing terrain was overwritten");
    Check(data.GetComponent<UnityEngine.Rigidbody2D>().bodyType == UnityEngine.RigidbodyType2D.Static, "Collision body");
    Check(data.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>().compositeOperation == UnityEngine.Collider2D.CompositeOperation.Merge, "Composite collision");
    Check(visual.transform.localPosition == new UnityEngine.Vector3(-0.5f, -0.5f, 0), "Visual offset");
    Check(grid.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true).Count(m => m.transform.parent == grid.transform) == 2, "Repeated setup added maps");
    var unrelated = new UnityEngine.GameObject("Unrelated");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(unrelated, scene);
    var second = unrelated.AddComponent<DualGridTilemap>();
    Reject(() => DualGridTilemapSetup.SetupDualGrid(scene, terrain), "Ambiguous grid accepted");
    Check(DualGridTilemapSetup.SetupDualGrid(scene, terrain, second) == second, "Explicit grid selection failed");
    Reject(() => DualGridTilemapSetup.SetupDualGrid(otherScene, terrain, grid), "Cross-scene target accepted");
    Check(otherScene.rootCount == 0, "Wrong scene modified");
    second.visual = second.data;
    Reject(() => DualGridTilemapSetup.SetupDualGrid(scene, terrain, second), "Shared Data/Visual accepted");
    // The checked-in settings and all artwork resolve by serialized reference.
    var settingsGuids = UnityEditor.AssetDatabase.FindAssets("t:DualGridSetupSettings");
    Check(settingsGuids.Length == 1, "Expected one project default settings asset");
    var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<DualGridSetupSettings>(UnityEditor.AssetDatabase.GUIDToAssetPath(settingsGuids[0]));
    Check(settings.terrain != null && settings.terrain.sprite != null, "Default terrain reference missing");
    Check(settings.terrain.tileSets[0].tiles.Count(tile => tile != null) == 15, "Default artwork missing");
    return "PASS: setup validation, rename/inactive/repeat safety, terrain migration, explicit/ambiguous targets, scene isolation, authored references; " + checks + " assertions.";
} finally {
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(otherScene);
    UnityEngine.Object.DestroyImmediate(terrain);
    UnityEngine.Object.DestroyImmediate(otherTerrain);
    UnityEngine.Object.DestroyImmediate(ordinary);
}
