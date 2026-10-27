using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Omnia.Fire;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ValidateFlameOptimizations {
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static string Run(float radiansPerTick = 0) {
        Require(Application.isPlaying, "Run in Play mode");
        var preset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
        var output = new StringBuilder();
        for (int mode = 0; mode < 3; mode++) {
            var obj = new GameObject("Flame optimization validation"); obj.SetActive(false);
            obj.transform.position = new Vector3(1000, 1000, 0);
            var emitter = obj.AddComponent<FlameEmitter>();
            foreach (var field in typeof(FlameEmitter).GetFields(BindingFlags.Instance | BindingFlags.Public))
                field.SetValue(emitter, field.GetValue(preset));
            emitter.nozzle = obj.transform;
            emitter.solidLayers = mode == 2 ? 1 << 3 : 0;
            GameObject wall = null;
            try {
                if (mode == 2) {
                    wall = new GameObject("Flame optimization wall"); wall.layer = 3;
                    wall.transform.position = obj.transform.position + Vector3.right * (55f / emitter.pixelsPerUnit);
                    wall.AddComponent<BoxCollider2D>().size = new Vector2(1 / emitter.pixelsPerUnit, 4);
                    Physics2D.SyncTransforms();
                }
                obj.SetActive(true); emitter.Renderer.enabled = false;
                emitter.IsFiring = mode != 0;
                float dt = 1 / emitter.simHz;
                for (int tick = 0; tick < 80; tick++) {
                    obj.transform.rotation = Quaternion.Euler(0, 0, tick * radiansPerTick * Mathf.Rad2Deg);
                    emitter.Renderer.Advance(dt);
                }
                var timer = new Stopwatch();
                long before = GC.GetAllocatedBytesForCurrentThread();
                timer.Start();
                for (int tick = 80; tick < 200; tick++) {
                    obj.transform.rotation = Quaternion.Euler(0, 0, tick * radiansPerTick * Mathf.Rad2Deg);
                    emitter.Renderer.Advance(dt);
                }
                timer.Stop();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Require(allocated == 0, "Emitter allocated " + allocated);
                output.AppendLine(new[] { "idle", radiansPerTick == 0 ? "steady full tick" : "rotating full tick", "wall full tick" }[mode] + ": "
                    + (timer.Elapsed.TotalMilliseconds / 120).ToString("F4") + " ms; allocated=" + allocated);
                int callbacks = 0;
                emitter.SimulationTick += _ => callbacks++;
                if (mode == 0) {
                    int step = emitter.Sim.StepIndex;
                    emitter.Renderer.Advance(10);
                    Require(emitter.Sim.StepIndex == step, "Empty emitter continued ticking");
                    obj.transform.position += Vector3.up * 10;
                    emitter.Renderer.Advance(dt);
                    emitter.IsFiring = true;
                    emitter.Renderer.Advance(dt);
                    Require(emitter.Sim.ParticleCount > 0, "Idle emitter did not resume");
                    Require(emitter.SampleHeat(obj.transform.position + obj.transform.right * 0.1f) > 0, "Resumed fire used a stale nozzle pose");
                } else {
                    emitter.Renderer.Advance(10);
                    Require(callbacks == 4, "Catch-up was not capped at four damage ticks");
                    var colors = emitter.Renderer.Display.sprite.texture.GetPixels32();
                    for (int i = 0; i < colors.Length; i++)
                        Require((colors[i].a != 0) == (emitter.Renderer.PixelHeat(i) > 0), "Deferred upload diverged from damage pixels");
                    emitter.IsFiring = false;
                    for (int tick = 0; tick < 100; tick++) emitter.Renderer.Advance(dt);
                    Require(emitter.Sim.ParticleCount == 0 && !emitter.Sim.Flow.HasHeat && emitter.Renderer.VisibleCount == 0, "Release failed to extinguish");
                    int step = emitter.Sim.StepIndex;
                    emitter.Renderer.Advance(1);
                    Require(emitter.Sim.StepIndex == step, "Extinguished emitter did not become idle");
                }
            } finally {
                Object.Destroy(obj);
                if (wall != null) Object.Destroy(wall);
            }
        }
        return "Angular speed=" + radiansPerTick + " radians/tick. " + output + "Idle/resume, release, four-tick catch-up, deferred texture/query agreement passed. Full ticks include physics and texture upload; no enemy callbacks or GPU execution time.";
    }
}
