using System;
using System.Diagnostics;
using System.Text;
using Omnia.Fire;
using UnityEditor;
using UnityEngine;

// Run before and after changes in the same Editor. No physics, rendering, or damage
// is included in the simulation rows. Checksums cover every measured raster.
public static class BenchmarkFlameOptimizations {
    private sealed class Wall : IFlameCollision {
        public long Cells, Sweeps;
        public bool CellSolid(int x, int y) { Cells++; return x == 55; }
        public bool HitsSolid(FlameVector a, FlameVector b) { Sweeps++; return Math.Min(a.x,b.x) <= 56 && Math.Max(a.x,b.x) >= 55; }
    }
    public static string Run(int mode = 0) {
        var preset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
        var settings = preset.Settings(1 / preset.simHz);
        var result = new StringBuilder();
        {
            var sim = new FlameSim(preset.maxParticles, preset.windowSize, preset.seed, preset.flowCellSize);
            var wall = mode == 2 ? new Wall() : null;
            var previous = new FlamePose();
            for (int t=0;t<100;t++) { var pose=Pose(mode,t); sim.Step(mode!=3,previous,pose,settings,wall); previous=pose; }
            var timer = new Stopwatch(); long allocated=0; uint checksum=2166136261;
            for (int t=100;t<300;t++) {
                var pose=Pose(mode,t);
                long before=GC.GetAllocatedBytesForCurrentThread();
                timer.Start(); sim.Step(mode!=3,previous,pose,settings,wall); timer.Stop();
                allocated+=GC.GetAllocatedBytesForCurrentThread()-before; previous=pose;
                unchecked {
                    checksum=(checksum^(uint)sim.OriginX)*16777619;
                    checksum=(checksum^(uint)sim.OriginY)*16777619;
                    for(int i=0;i<sim.WindowSize*sim.WindowSize;i++) checksum=(checksum^(uint)sim.CellHeat(i).GetHashCode())*16777619;
                }
            }
            result.AppendLine(new[]{"steady","sweep","wall","empty"}[mode]+": "+(timer.Elapsed.TotalMilliseconds/200).ToString("F4")+" ms/tick; allocations="+allocated+"; raster checksum="+checksum+(wall==null?"":"; cell queries="+wall.Cells+"; sweeps="+wall.Sweeps));
        }
        return result.ToString();
    }
    private static FlamePose Pose(int mode,int tick) => mode==1 ? new FlamePose(tick*0.2f,0,tick*0.07f) : default;
}
