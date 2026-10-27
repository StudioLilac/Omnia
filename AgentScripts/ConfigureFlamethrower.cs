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
    private static void Eddies(FlameEmitter emitter) {
        emitter.cellularFlow = true; emitter.flowCellSize = 2;
        emitter.flowSpeed = 3.5f; emitter.flowVorticity = 1.8f;
        emitter.flowCooling = 0.022f; emitter.flowBuoyancy = 0.035f;
        emitter.flowPerturbation = 0.35f; emitter.flowHeat = 0.8f;
        emitter.flowPressureIterations = 8;
        emitter.wingLifeMin = 9; emitter.wingLifeMax = 15;
        emitter.windowSize = 288;
    }
    public static string SetEddies() {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            Eddies(root.GetComponent<FlameEmitter>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        if (Application.isPlaying)
            foreach (var emitter in Object.FindObjectsByType<FlameEmitter>()) Eddies(emitter);
        return "World-space cellular heat/velocity flow enabled; existing core beam preserved.";
    }
    private static void BarrelPlume(FlameEmitter emitter) {
        emitter.simHz = 40;
        emitter.coreRadius = 2.5f; emitter.ribbonWidthScale = 2.4f;
        emitter.coreNozzleScale = 0.18f; emitter.coreBloomPixels = 14;
        emitter.coreFadeStart = 0.35f; emitter.coreFadeEnd = 0.85f;
        emitter.radialFalloff = 0.65f; emitter.colorFalloff = 0.8f;
        emitter.flowJetStability = 0.8f;
        emitter.flowCooling = 0.032f; emitter.flowHeat = 0.85f;
        emitter.flowPressureIterations = 6;
        emitter.emberLifeMin = 2; emitter.emberLifeMax = 4;
        emitter.emberHeat = 0.55f; emitter.emberRadius = 0.55f; emitter.maxEmbers = 32;
    }
    public static string SetBarrelPlume() {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            BarrelPlume(root.GetComponent<FlameEmitter>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        if (Application.isPlaying)
            foreach (var emitter in Object.FindObjectsByType<FlameEmitter>()) BarrelPlume(emitter);
        return "40 FPS barrel plume: narrow nozzle opening into a wider hot jet, smooth core handing off to cellular fire, 50–100 ms embers.";
    }
    public static string SetWidePlume() {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            var emitter = root.GetComponent<FlameEmitter>();
            emitter.coreRadius = 2.5f; emitter.coreNozzleScale = 0.18f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        if (Application.isPlaying)
            foreach (var emitter in Object.FindObjectsByType<FlameEmitter>()) {
                emitter.coreRadius = 2.5f; emitter.coreNozzleScale = 0.18f;
            }
        return "Core body widened by 67%, preserving the nozzle opening, 40 FPS and other tuning.";
    }
    public static string SetUnlimitedRisingFire() {
        const string path = "Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try {
            root.GetComponent<Flamethrower>().useFuel = false;
            var emitter = root.GetComponent<FlameEmitter>();
            emitter.flowUpperBias = 0.8f; emitter.flowBuoyancy = 0.12f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        if (Application.isPlaying) {
            foreach (var weapon in Object.FindObjectsByType<Flamethrower>()) weapon.useFuel = false;
            foreach (var emitter in Object.FindObjectsByType<FlameEmitter>()) {
                emitter.flowUpperBias = 0.8f; emitter.flowBuoyancy = 0.12f;
            }
        }
        return "Fuel disabled; buoyant upper eddies amplified and underside circulation reduced in world space.";
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
                Wispy(emitter); Ribbon(emitter); Eddies(emitter);
                emitter.simHz = 20; emitter.windowSize = 256; emitter.maxParticles = 512;
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
