using System.IO;
using UnityEditor;
using UnityEngine;
using Omnia.Fire;
using Object = UnityEngine.Object;

public static class PreviewFlameRibbon {
    public static string Run() {
        const int size=224, zoom=2, width=size*zoom*2, height=size*zoom*3;
        var preset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
        var settings=preset.Settings(0.05f);
        var pixels=new Color32[width*height];
        for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(25,29,39,255);
        var output=new Texture2D(width,height,TextureFormat.RGBA32,false);
        try {
            for(int mode=0;mode<2;mode++) {
                settings.coreRibbon=mode==1;
                var sim=new FlameSim(512,size,1);
                var obj=new GameObject("Ribbon comparison"); var renderer=obj.AddComponent<FlameRenderer>();
                renderer.Initialize(size,32,null,preset.flameMaterial);
                var previous=new FlamePose(0,0,0);
                try {
                    for(int tick=0;tick<12;tick++) sim.Step(true,previous,previous,settings);
                    for(int frame=0;frame<3;frame++) {
                        for(int tick=0;tick<4;tick++) {
                            float angle=(frame*4+tick+1)*Mathf.PI*2/12;
                            var current=new FlamePose(0,0,angle);
                            sim.Step(true,previous,current,settings); previous=current;
                        }
                        renderer.Draw(sim,preset.palette,preset.cutoff,preset.ditherEdges);
                        var colors=renderer.Display.sprite.texture.GetPixels32();
                        for(int i=0;i<renderer.VisibleCount;i++) {
                            int index=renderer.VisibleIndex(i); Vector2 world=renderer.PixelWorld(index)*32;
                            int px=Mathf.FloorToInt(world.x)+size/2, py=Mathf.FloorToInt(world.y)+size/2;
                            if(px<0||py<0||px>=size||py>=size) continue;
                            int x=(mode*size+px)*zoom, y=((2-frame)*size+py)*zoom;
                            for(int dy=0;dy<zoom;dy++) for(int dx=0;dx<zoom;dx++) pixels[(y+dy)*width+x+dx]=colors[index];
                        }
                    }
                } finally { Object.DestroyImmediate(obj); }
            }
            output.SetPixels32(pixels); output.Apply();
            Directory.CreateDirectory("Temp/Flamethrower");
            File.WriteAllBytes("Temp/Flamethrower/ribbon-comparison.png",output.EncodeToPNG());
            return "Temp/Flamethrower/ribbon-comparison.png: particle streaks left, connected ribbon right; three stages of a fast full spin.";
        } finally { Object.DestroyImmediate(output); }
    }
}
