using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
public static class CaptureFlamethrower {
    public static async Task<string> Run() {
        var weapon = Object.FindAnyObjectByType<Flamethrower>();
        weapon.SetTriggerHeld(true);
        await Task.Delay(1800);
        var obj = new GameObject("Flame capture camera");
        var camera = obj.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 1.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(25,29,39,255);
        camera.transform.position = weapon.transform.position + new Vector3(1.8f, 0.125f, -10);
        var target = new RenderTexture(768,384,24);
        var texture = new Texture2D(768,384,TextureFormat.RGBA32,false);
        var prior = RenderTexture.active;
        try {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0,0,768,384),0,0);
            texture.Apply();
            Directory.CreateDirectory("Temp/Flamethrower");
            File.WriteAllBytes("Temp/Flamethrower/live.png",texture.EncodeToPNG());
            return "Temp/Flamethrower/live.png";
        } finally {
            RenderTexture.active = prior;
            camera.targetTexture = null;
            Object.Destroy(obj); Object.Destroy(texture); Object.Destroy(target);
            weapon.SetTriggerHeld(false);
        }
    }
}
