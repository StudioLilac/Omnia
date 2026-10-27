using System.IO;
using UnityEditor;
using UnityEngine;
using Omnia.Fire;
using Object = UnityEngine.Object;

public static class PreviewRisingFire {
    public static string Run() {
        const int window = 288, cropWidth = 124, cropHeight = 72, zoom = 4;
        const int width = cropWidth * zoom * 2, height = cropHeight * zoom * 3;
        var preset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Shotgun/Shotgun.prefab").GetComponent<FlameEmitter>();
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(25,29,39,255);
        var output = new Texture2D(width,height,TextureFormat.RGBA32,false);
        try {
            for (int mode = 0; mode < 2; mode++) {
                float fps = preset.simHz;
                var settings = preset.Settings(1 / fps);
                if (mode == 0) {
                    settings.flowUpperBias = 0; settings.flowBuoyancy = 0.035f;
                }
                var sim = new FlameSim(512,window,1,preset.flowCellSize);
                var obj = EditorUtility.CreateGameObjectWithHideFlags("Rising fire comparison",HideFlags.HideAndDontSave);
                var renderer = obj.AddComponent<FlameRenderer>(); renderer.Initialize(window,32,null,preset.flameMaterial);
                var pose = new FlamePose();
                try {
                    for (int tick = 0; tick < Mathf.RoundToInt(fps); tick++) sim.Step(true,pose,pose,settings);
                    for (int frame = 0; frame < 3; frame++) {
                        for (int tick = 0; tick < Mathf.RoundToInt(fps * 0.1f); tick++) sim.Step(true,pose,pose,settings);
                        renderer.Draw(sim,preset.palette,preset.cutoff,preset.ditherEdges);
                        var colors = renderer.Display.sprite.texture.GetPixels32();
                        for (int i = 0; i < renderer.VisibleCount; i++) {
                            int index = renderer.VisibleIndex(i); Vector2 world = renderer.PixelWorld(index) * 32;
                            int px = Mathf.FloorToInt(world.x) + 12, py = Mathf.FloorToInt(world.y) + 32;
                            if (px < 0 || py < 0 || px >= cropWidth || py >= cropHeight) continue;
                            int x = (mode * cropWidth + px) * zoom, y = ((2-frame) * cropHeight + py) * zoom;
                            for (int dy = 0; dy < zoom; dy++) for (int dx = 0; dx < zoom; dx++) pixels[(y+dy)*width+x+dx] = colors[index];
                        }
                    }
                } finally { Object.DestroyImmediate(obj); }
            }
            output.SetPixels32(pixels); output.Apply(false);
            Directory.CreateDirectory("Temp/Flamethrower");
            File.WriteAllBytes("Temp/Flamethrower/rising-fire-comparison.png",output.EncodeToPNG());
            return "Previous balanced plume left; stronger upper eddies and restrained underside right.";
        } finally { Object.DestroyImmediate(output); }
    }
}
