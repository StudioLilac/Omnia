using System;
using System.Diagnostics;
using UnityEngine;
using Omnia.Fire;
using Object = UnityEngine.Object;

public static class ProfileFlamethrower {
    public static string Run() {
        if(!Application.isPlaying) throw new Exception("Run in Play mode");
        var source=Object.FindAnyObjectByType<FlameEmitter>();
        string result="";
        for(int mode=0;mode<2;mode++) {
            var obj=new GameObject("Flame CPU profiling"); obj.SetActive(false);
            obj.transform.position=source.Origin+Vector2.up*0.25f;
            var emitter=obj.AddComponent<FlameEmitter>();
            emitter.palette=source.palette; emitter.flameMaterial=source.flameMaterial;
            emitter.solidLayers=source.solidLayers;
            emitter.windowSize=mode==0?160:224; emitter.maxRange=mode==0?48:96; emitter.jetSpeed=mode==0?2.5f:5;
            obj.SetActive(true); emitter.Renderer.enabled=false; emitter.IsFiring=true;
            for(int i=0;i<100;i++) emitter.Renderer.Advance(0.05f);
            var clock=new Stopwatch(); long before=GC.GetAllocatedBytesForCurrentThread();
            clock.Start(); for(int i=0;i<2000;i++) emitter.Renderer.Advance(0.05f); clock.Stop();
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            if(allocated!=0) throw new Exception("Full tick allocated "+allocated);
            result+=(mode==0?"Doc defaults":"Project prefab")+": "+(clock.Elapsed.TotalMilliseconds/2000).ToString("F4")+" ms/tick including physics, raster, palette and texture upload; "+allocated+" bytes allocated. ";
            Object.Destroy(obj);
        }
        return result;
    }
}
