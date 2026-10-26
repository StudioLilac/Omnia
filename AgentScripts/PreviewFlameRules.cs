using System.IO;
using UnityEditor;
using UnityEngine;
using Omnia.Fire;
using Object = UnityEngine.Object;

public static class PreviewFlameRules {
    public static string Run() {
        const int size = 224;
        var sim = new FlameSim(512,size,1);
        var settings = FlameSimSettings.Default; settings.maxRange = 96; settings.jetSpeed = 5;
        var palette = AssetDatabase.LoadAssetAtPath<FlamePalette>("Assets/Scripts/Weapon/Flamethrower/Fire.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Weapon/Flamethrower/PixelFlame.mat");
        var obj = new GameObject("World-space particle preview");
        var renderer = obj.AddComponent<FlameRenderer>(); renderer.Initialize(size,32,null,material);
        var output = new Texture2D(size*3,size*2,TextureFormat.RGBA32,false);
        var pixels = new Color32[size*3*size*2];
        for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(25,29,39,255);
        var previous = new FlamePose(0,0,0);
        try {
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
            return "Temp/Flamethrower/world-space-sweep.png: pulsed jet, 180-degree sweep, then lingering flame.";
        } finally { Object.DestroyImmediate(obj); Object.DestroyImmediate(output); }
    }
}
