using System.Linq;
using UnityEditor;

public static class ReserializeAssets
{
    [MenuItem("Tools/Reserialize Scenes and Prefabs")]
    static void Run()
    {
        var paths = AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();
        AssetDatabase.ForceReserializeAssets(paths);
    }
}
