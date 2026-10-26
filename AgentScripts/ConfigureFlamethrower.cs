using System.Reflection;
using UnityEditor;
using UnityEngine;
using Omnia.Fire;

public static class ConfigureFlamethrower {
    public static string Run() => SetRules();
    public static string SetRange() => Configure(false);
    public static string SetRules() => Configure(true);
    public static string SetContinuous() {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            var emitter = root.GetComponent<FlameEmitter>();
            emitter.pulsePeriod = 1;
            emitter.slugLength = 6;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        return "Continuous emission: every tick, six overlapping core particles at 5 pixels/tick.";
    }
    private static string Configure(bool resetRules) {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            var emitter = root.GetComponent<FlameEmitter>();
            if (emitter == null) emitter = root.AddComponent<FlameEmitter>();
            if (resetRules) {
                var defaults = FlameSimSettings.Default;
                foreach (var field in typeof(FlameSimSettings).GetFields(BindingFlags.Instance | BindingFlags.Public)) {
                    var destination = typeof(FlameEmitter).GetField(field.Name);
                    if (destination != null) destination.SetValue(emitter, System.Convert.ChangeType(field.GetValue(defaults), destination.FieldType));
                }
                emitter.simHz = 20; emitter.windowSize = 224; emitter.maxParticles = 512;
                emitter.palette = AssetDatabase.LoadAssetAtPath<FlamePalette>("Assets/Scripts/Weapon/Flamethrower/Fire.asset");
                emitter.flameMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Weapon/Flamethrower/PixelFlame.mat");
                emitter.nozzle = root.transform.Find("Barrel Position");
                emitter.solidLayers = root.GetComponent<Flamethrower>().groundLayer;
            }
            // Preserve the longer reach requested for this game: 3 world units at 32 PPU.
            emitter.maxRange = 96; emitter.jetSpeed = 5;
            emitter.pulsePeriod = 1; emitter.slugLength = 6;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        return "World-space fire configured: 96-pixel range, 5 pixels/tick, 224-pixel raster, 20 FPS.";
    }
}
