using System.IO;
using UnityEditor;
using UnityEngine;
using Omnia.Fire;
using Object = UnityEngine.Object;

public static class PreviewFlameRules {
    public static string Run() {
        const int size = 224;
        var sim = new FlameSim(512,size,1);
        var settings = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>().Settings(0.05f);
        var palette = AssetDatabase.LoadAssetAtPath<FlamePalette>("Assets/Scripts/Weapon/Flamethrower/Fire.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Weapon/Flamethrower/PixelFlame.mat");
        var obj = new GameObject("World-space particle preview");
        var renderer = obj.AddComponent<FlameRenderer>(); renderer.Initialize(size,32,null,material);
        var output = new Texture2D(size*3,size*2,TextureFormat.RGBA32,false);
        var pixels = new Color32[size*3*size*2];
        for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(25,29,39,255);
        var previous = new FlamePose(0,0,0);
        try {
            var steady = new FlameSim(512,size,1);
            for(int tick=0;tick<40;tick++) steady.Step(true,previous,previous,settings);
            int gap=0, longestGap=0;
            for(int x=0;x<(int)settings.maxRange-16;x++) {
                bool hot=false;
                for(int y=-16;y<=16;y++) if(steady.SampleWorld(x+0.5f,y+0.5f)>0.08f) hot=true;
                gap=hot?0:gap+1; longestGap=System.Math.Max(longestGap,gap);
            }
            if(longestGap!=0) throw new System.Exception("Continuous core has a gap of "+longestGap+" pixels");
            for(int frame=0;frame<6;frame++) {
                for(int tick=0;tick<4;tick++) {
                    int step=frame*4+tick;
                    float angle=step<8 ? 0 : Mathf.Min(Mathf.PI,(step-8)*Mathf.PI/10);
                    var current = new FlamePose(0,0,angle);
                    sim.Step(step<20,previous,current,settings); previous=current;
                }
                renderer.Draw(sim,palette,0.08f,true);
                var colors=renderer.Display.sprite.texture.GetPixels32();
                for(int i=0;i<colors.Length;i++) if(colors[i].a!=0) {
                    int x=(frame%3)*size+i%size, y=(1-frame/3)*size+i/size;
                    pixels[y*size*3+x]=colors[i];
                }
            }
            output.SetPixels32(pixels); output.Apply();
            Directory.CreateDirectory("Temp/Flamethrower");
            File.WriteAllBytes("Temp/Flamethrower/world-space-sweep.png",output.EncodeToPNG());
            return "Temp/Flamethrower/world-space-sweep.png: continuous core verified with no gaps through the first 80 pixels; 180-degree sweep, then lingering flame.";
        } finally { Object.DestroyImmediate(obj); Object.DestroyImmediate(output); }
    }
}
