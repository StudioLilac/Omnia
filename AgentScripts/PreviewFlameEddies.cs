using System.IO;
using UnityEditor;
using UnityEngine;
using Omnia.Fire;
using Object = UnityEngine.Object;

public static class PreviewFlameEddies {
    public static string Run() {
        const int window=288, cropWidth=124, cropHeight=72, zoom=4;
        const int width=cropWidth*zoom*2, height=cropHeight*zoom*3;
        var preset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
        var settings=preset.Settings(0.05f);
        var pixels=new Color32[width*height];
        for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(25,29,39,255);
        var output=new Texture2D(width,height,TextureFormat.RGBA32,false);
        try {
            for(int mode=0;mode<2;mode++) {
                settings.cellularFlow=mode==1;
                settings.wingLifeMin=mode==0?9:preset.wingLifeMin; settings.wingLifeMax=mode==0?15:preset.wingLifeMax;
                var sim=new FlameSim(512,window,1,preset.flowCellSize);
                var obj=EditorUtility.CreateGameObjectWithHideFlags("Rolling sheet comparison",HideFlags.HideAndDontSave);
                var renderer=obj.AddComponent<FlameRenderer>(); renderer.Initialize(window,32,null,preset.flameMaterial);
                var pose=new FlamePose(0,0,0);
                try {
                    for(int tick=0;tick<20;tick++) sim.Step(true,pose,pose,settings);
                    for(int frame=0;frame<3;frame++) {
                        for(int tick=0;tick<4;tick++) sim.Step(true,pose,pose,settings);
                        renderer.Draw(sim,preset.palette,preset.cutoff,preset.ditherEdges);
                        var colors=renderer.Display.sprite.texture.GetPixels32();
                        for(int i=0;i<renderer.VisibleCount;i++) {
                            int index=renderer.VisibleIndex(i); Vector2 world=renderer.PixelWorld(index)*32;
                            int px=Mathf.FloorToInt(world.x)+12, py=Mathf.FloorToInt(world.y)+32;
                            if(px<0||py<0||px>=cropWidth||py>=cropHeight) continue;
                            int x=(mode*cropWidth+px)*zoom, y=((2-frame)*cropHeight+py)*zoom;
                            for(int dy=0;dy<zoom;dy++) for(int dx=0;dx<zoom;dx++) pixels[(y+dy)*width+x+dx]=colors[index];
                        }
                    }
                } finally { Object.DestroyImmediate(obj); }
            }
            output.SetPixels32(pixels); output.Apply();
            Directory.CreateDirectory("Temp/Flamethrower");
            File.WriteAllBytes("Temp/Flamethrower/eddy-comparison.png",output.EncodeToPNG());
            return "Temp/Flamethrower/eddy-comparison.png: backwards particles left; advected cellular heat with shear-generated eddies right, at three successive times.";
        } finally { Object.DestroyImmediate(output); }
    }
    public static string Animate() {
        const int window = 288, width = 256, height = 160, zoom = 3;
        var preset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
        var settings = preset.Settings(0.05f);
        var sim = new FlameSim(512, window, preset.seed, preset.flowCellSize);
        var obj = EditorUtility.CreateGameObjectWithHideFlags("Cellular flame preview", HideFlags.HideAndDontSave);
        var renderer = obj.AddComponent<FlameRenderer>(); renderer.Initialize(window, 32, null, preset.flameMaterial);
        var output = new Texture2D(width * zoom, height * zoom, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height * zoom * zoom];
        var pose = new FlamePose();
        string folder = "Temp/Flamethrower/cellular-frames"; Directory.CreateDirectory(folder);
        try {
            for (int tick = 0; tick < 30; tick++) sim.Step(true, pose, pose, settings);
            for (int frame = 0; frame < 100; frame++) {
                var next = new FlamePose(0, 0, frame >= 40 && frame < 60 ? (frame - 40) * Mathf.PI * 2 / 19 : 0);
                sim.Step(frame < 80, pose, next, settings); pose = next;
                renderer.Draw(sim, preset.palette, preset.cutoff, preset.ditherEdges);
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(25, 29, 39, 255);
                var colors = renderer.Display.sprite.texture.GetPixels32();
                for (int i = 0; i < renderer.VisibleCount; i++) {
                    int cell = renderer.VisibleIndex(i); Vector2 world = renderer.PixelWorld(cell) * 32;
                    int x = Mathf.FloorToInt(world.x) + 120, y = Mathf.FloorToInt(world.y) + 80;
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    for (int dy = 0; dy < zoom; dy++) for (int dx = 0; dx < zoom; dx++)
                        pixels[(y * zoom + dy) * width * zoom + x * zoom + dx] = colors[cell];
                }
                output.SetPixels32(pixels); output.Apply(false);
                File.WriteAllBytes(folder + "/" + frame.ToString("D3") + ".png", output.EncodeToPNG());
            }
            return "100 frames at 20 Hz: steady jet, fast full turn, steady jet, release. " + folder;
        } finally { Object.DestroyImmediate(output); Object.DestroyImmediate(obj); }
    }

}
