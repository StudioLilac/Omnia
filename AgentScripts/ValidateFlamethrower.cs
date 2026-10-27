using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Omnia.Fire;
using Object = UnityEngine.Object;

public static class ValidateFlamethrower {
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static string Run() {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var sim = new FlameSim(512, 224, 1);
        var settings = FlameSimSettings.Default; settings.maxRange = 96; settings.jetSpeed = 5;
        var palette = AssetDatabase.LoadAssetAtPath<FlamePalette>("Assets/Scripts/Weapon/Flamethrower/Fire.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Weapon/Flamethrower/PixelFlame.mat");
        var obj = new GameObject("World flame validation");
        var renderer = obj.AddComponent<FlameRenderer>();
        renderer.Initialize(224,32,null,material);
        try {
            var previous = new FlamePose(0.4f,0.6f,0);
            for (int step = 0; step < 60; step++) {
                var current = new FlamePose(step * 0.2f, 0.6f, step * 0.12f);
                sim.Step(true,previous,current,settings); previous = current;
                renderer.Draw(sim,palette,0.08f,true);
                Require(obj.transform.parent == null && obj.transform.rotation == Quaternion.identity, "Renderer followed gun rotation");
                Require(Mathf.Abs(obj.transform.position.x * 32 - Mathf.Round(obj.transform.position.x * 32)) < 0.001f, "X alignment");
                Require(Mathf.Abs(obj.transform.position.y * 32 - Mathf.Round(obj.transform.position.y * 32)) < 0.001f, "Y alignment");
                var tex = renderer.Display.sprite.texture;
                Require(tex.filterMode == FilterMode.Point && tex.mipmapCount == 1, "Texture filtering");
                var colors = tex.GetPixels32();
                for (int i = 0; i < colors.Length; i++) {
                    Require(colors[i].a == 0 || colors[i].a == 255, "Fractional alpha");
                    Require((renderer.SampleHeat(renderer.PixelWorld(i)) > 0) == (colors[i].a == 255), "Visual/query mismatch");
                }
            }
            for (int i = 0; i < 100; i++) sim.Step(true,previous,previous,settings);
            var timer = new Stopwatch();
            long before = GC.GetAllocatedBytesForCurrentThread();
            timer.Start();
            for (int i = 0; i < 500; i++) sim.Step(true,previous,previous,settings);
            timer.Stop();
            long allocation = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(allocation == 0, "Simulation allocated " + allocation);
            double extendedTime = timer.Elapsed.TotalMilliseconds / 500;
            // The design budget is for its 48-pixel/160-cell defaults. Report the
            // larger reference independently instead of treating it as that default.
            var defaults = FlameSimSettings.Default;
            var defaultSim = new FlameSim(512,160,1);
            for(int i=0;i<100;i++) defaultSim.Step(true,previous,previous,defaults);
            before = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for(int i=0;i<500;i++) defaultSim.Step(true,previous,previous,defaults);
            timer.Stop();
            long defaultAllocation = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(defaultAllocation == 0, "Default simulation allocated " + defaultAllocation);
            double defaultTime = timer.Elapsed.TotalMilliseconds / 500;
            bool withinOriginalBudget = defaultTime < 0.2;
            var preset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
            var project = new FlameSim(preset.maxParticles, preset.windowSize, preset.seed, preset.flowCellSize);
            var projectRules = preset.Settings(1 / preset.simHz);
            for (int i = 0; i < 100; i++) project.Step(true, previous, previous, projectRules);
            before = GC.GetAllocatedBytesForCurrentThread(); timer.Restart();
            for (int i = 0; i < 500; i++) project.Step(true, previous, previous, projectRules);
            timer.Stop();
            long projectAllocation = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(projectAllocation == 0, "Project preset allocated " + projectAllocation);
            double projectTime = timer.Elapsed.TotalMilliseconds / 500;
            return "60 sweeping poses: world alignment, binary alpha, query agreement passed. Default 48-pixel jet: "
                + defaultTime.ToString("F4") + " ms/tick; 96-pixel reference: " + extendedTime.ToString("F4")
                + " ms/tick; actual project preset: " + projectTime.ToString("F4")
                + " ms/tick. All 0 bytes allocated. Original 0.2 ms budget met: " + withinOriginalBudget;
        } finally { Object.DestroyImmediate(obj); Application.runInBackground = background; }
    }
}
