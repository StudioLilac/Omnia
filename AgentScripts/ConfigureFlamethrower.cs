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
    private static void Wispy(FlameEmitter emitter) {
        emitter.shedChance = 0.55f;
        emitter.flapWaveNumber = 0.22f;
        emitter.flapFreq = 0.7f;
        emitter.wingOffsetMax = 4;
        emitter.wingForward = 0.35f;
        emitter.wingLift = 0.25f;
        emitter.wingSpread = 0.16f;
        emitter.wingSpreadSteps = 5;
        emitter.backDrag = 0.75f;
        emitter.liftAccel = 0.065f;
        emitter.wingLifeMin = 9;
        emitter.wingLifeMax = 15;
        emitter.wingHeat = 0.62f;
        emitter.wingRadius = 0.85f;
    }
    public static string SetWispy() {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            Wispy(root.GetComponent<FlameEmitter>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        if (Application.isPlaying)
            foreach (var emitter in Object.FindObjectsByType<FlameEmitter>()) Wispy(emitter);
        return "Wispy fire: thinner, longer-lived wings, stronger backward drag/lift, broader and slower eddies. Current core speed and continuous emission preserved.";
    }
    private static void Ribbon(FlameEmitter emitter) {
        emitter.coreRibbon = true;
        emitter.ribbonSmoothness = 0.85f;
        emitter.ribbonWidthScale = 1.75f;
        emitter.ribbonTipTaper = 0.2f;
        emitter.wingTrail = 1;
    }
    public static string SetRibbon() {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            Ribbon(root.GetComponent<FlameEmitter>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        if (Application.isPlaying)
            foreach (var emitter in Object.FindObjectsByType<FlameEmitter>()) Ribbon(emitter);
        return "Connected world-space core ribbon, smooth age/heat interpolation, tapered tip, and wing motion trails enabled.";
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
                Wispy(emitter); Ribbon(emitter);
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
