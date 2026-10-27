using System;
using System.Diagnostics;
using Omnia.Fire;
using Unity.Burst;
using UnityEditor;
using UnityEngine;

// Actual prefab, stationary nozzle, sustained rotation at 40 Hz. Mode 0 measures
// everything; modes 1/2 isolate the ribbon with/without temperature variation.
public static class ProfileFlameSpin {
    public static string Run(float radiansPerTick = 0.6f, int mode = 0) {
        if (mode < 0 || mode > 2) throw new ArgumentOutOfRangeException(nameof(mode));
        var preset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
        var settings = preset.Settings(1 / preset.simHz);
        if (mode > 0) { settings.cellularFlow = false; settings.shedChance = 0; }
        if (mode == 2) settings.coreHeatVariation = 0;
        var sim = new FlameSim(preset.maxParticles, preset.windowSize, preset.seed, preset.flowCellSize);
        sim.Flow.ProfileTiming = true;
        var previous = new FlamePose();
        for (int tick = 0; tick < 40; tick++) {
            var pose = new FlamePose(0, 0, tick * radiansPerTick);
            sim.Step(true, previous, pose, settings); previous = pose;
        }
        var timer = new Stopwatch();
        long sourceTicks = 0, velocityTicks = 0, heatTicks = 0;
        long before = GC.GetAllocatedBytesForCurrentThread();
        timer.Start();
        for (int tick = 40; tick < 80; tick++) {
            var pose = new FlamePose(0, 0, tick * radiansPerTick);
            sim.Step(true, previous, pose, settings); previous = pose;
            sourceTicks += sim.Flow.SourceTicks; velocityTicks += sim.Flow.VelocityTicks; heatTicks += sim.Flow.HeatTicks;
        }
        timer.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        int hot = 0;
        for (int cell = 0; cell < sim.WindowSize * sim.WindowSize; cell++) if (sim.Flow.CellHeat(cell) > 0) hot++;
        int area = sim.Flow.HasHeat ? (sim.Flow.MaxWorldX - sim.Flow.MinWorldX + 1) * (sim.Flow.MaxWorldY - sim.Flow.MinWorldY + 1) : 0;
        double scale = 1000.0 / Stopwatch.Frequency / 40;
        return new[] { "complete", "core + temperature / no fluid", "core only / no temperature" }[mode]
            + ": " + (timer.Elapsed.TotalMilliseconds / 40).ToString("F4") + " ms/tick; radians/tick=" + radiansPerTick
            + "; Burst=" + BurstCompiler.IsEnabled + "; particles=" + sim.ParticleCount + "; hot cells=" + hot
            + "; heat bounds area=" + area + "; allocations=" + allocated
            + "; sources=" + (sourceTicks * scale).ToString("F3")
            + "; velocity=" + (velocityTicks * scale).ToString("F3") + "; heat=" + (heatTicks * scale).ToString("F3");
    }
}
