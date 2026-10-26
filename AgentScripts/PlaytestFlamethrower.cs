using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Enemies;
using Omnia.Fire;
using Omnia.State;
using Players;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class FlameDamageProbe : Enemy {
    public float Received;
    public override void Start() { }
    public override void Update() { }
    public override void FixedUpdate() { }
    protected override void UseAnimation(StateMachine stateMachine) { }
    public override void Hurt(float damage, bool stagger = true, bool crit = false) { Received += damage; }
}

public static class PlaytestFlamethrower {
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Field(object obj, string name, object value) => obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(obj,value);
    private static FlameDamageProbe Target(Vector2 point, float width = 0.4f) {
        var obj=new GameObject("World flame damage probe"); obj.layer=6; obj.transform.position=point;
        obj.AddComponent<BoxCollider2D>().size=new Vector2(width,0.4f);
        return obj.AddComponent<FlameDamageProbe>();
    }
    private static async Task Ticks(FlameEmitter emitter,int count) {
        int goal=emitter.Sim.StepIndex+count;
        var clock=Stopwatch.StartNew();
        while(emitter.Sim.StepIndex<goal) {
            if(clock.ElapsedMilliseconds>10000) throw new Exception("Player loop did not advance");
            await Task.Delay(10);
        }
    }
    private static async Task Frames(int count) {
        int goal=Time.frameCount+count;
        var clock=Stopwatch.StartNew();
        while(Time.frameCount<goal) {
            if(clock.ElapsedMilliseconds>10000) throw new Exception("Player frames did not advance");
            await Task.Delay(10);
        }
    }
    private static int Core(FlameSim sim,int maxAge=10) {
        int selected=-1, youngest=maxAge+1;
        for(int i=0;i<sim.Capacity;i++) {
            var p=sim.ParticleAt(i);
            if(p.kind==FlameParticleKind.Core && p.age<youngest && p.dist>=5) { selected=i; youngest=p.age; }
        }
        if(selected<0) throw new Exception("No live core particle found");
        return selected;
    }
    private static void Capture(FlameEmitter emitter,string path) {
        var obj=new GameObject("World fire capture"); var camera=obj.AddComponent<Camera>();
        camera.enabled=false; camera.orthographic=true; camera.orthographicSize=2;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(25,29,39,255);
        camera.transform.position=(Vector3)emitter.Origin+new Vector3(0.3f,0.9f,-10);
        var target=new RenderTexture(768,512,24); var texture=new Texture2D(768,512,TextureFormat.RGBA32,false);
        var old=RenderTexture.active;
        try {
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,768,512),0,0); texture.Apply();
            Directory.CreateDirectory("Temp/Flamethrower"); File.WriteAllBytes(path,texture.EncodeToPNG());
        } finally { RenderTexture.active=old; camera.targetTexture=null; Object.Destroy(obj); Object.Destroy(target); Object.Destroy(texture); }
    }

    public static async Task<string> Run() {
        Require(Application.isPlaying,"Run in Play mode");
        Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="CopyableScene","Run this test in CopyableScene");
        Application.runInBackground=true;
        foreach(var item in Object.FindObjectsByType<Transform>())
            if(item.name=="World flame damage probe" || item.name=="One-pixel flame test wall") Object.Destroy(item.gameObject);
        await Frames(2);
        var player=Object.FindAnyObjectByType<Player>(); Require(player!=null,"No player");
        var weapon=player.GetComponentInChildren<Flamethrower>(true); Require(weapon!=null,"No flamethrower");
        foreach(var input in player.GetComponentsInChildren<Players.Mixin.UseInput>(true)) input.enabled=false;
        player.enabled=false; Field(player,"invulnerable",true);
        var rb=player.GetComponent<Rigidbody2D>(); if(rb!=null) rb.simulated=false;
        Field(player,"facing",Vector2.right);
        weapon.SetSpriteActive(true); weapon.CurrentAmmo=100;
        var emitter=weapon.Emitter; emitter.Clear(); weapon.SetTriggerHeld(true);
        await Ticks(emitter,3);
        int tracked=Core(emitter.Sim);
        var initial=emitter.Sim.ParticleAt(tracked);
        var ahead=(initial.pos+initial.vel*3)*(1/emitter.pixelsPerUnit);
        var oldTrail=Target(new Vector2(ahead.x,ahead.y),0.65f);
        var outside=Target(emitter.Origin+Vector2.down*2);
        int sweepStart=emitter.Sim.StepIndex, trajectoryChecks=0;
        while(emitter.Sim.StepIndex-sweepStart<10) {
            float angle=Mathf.PI*Mathf.Min(1,(emitter.Sim.StepIndex-sweepStart+1)/10f);
            Field(player,"facing",new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)));
            await Ticks(emitter,1);
            var oldCore=emitter.Sim.ParticleAt(tracked);
            int expectedAge=initial.age+emitter.Sim.StepIndex-sweepStart;
            if(expectedAge<initial.life && oldCore.kind==FlameParticleKind.Core && oldCore.age==expectedAge) {
                Require(oldCore.vel.x>0 && oldCore.pos.x>initial.pos.x+1,"A turning gun redirected existing fire");
                trajectoryChecks++;
            }
        }
        Field(player,"facing",Vector2.left);
        Require(trajectoryChecks>0,"No trajectory observations before natural core expiry");
        var after=emitter.Sim.ParticleAt(tracked);
        Require(oldTrail.Received>0,"A target in the old rightward trail took no damage during the sweep");
        Require(outside.Received==0,"Outside target took damage");
        Require(emitter.Renderer.transform.parent==null && emitter.Renderer.transform.rotation==Quaternion.identity,"Fire renderer follows the gun transform");
        Capture(emitter,"Temp/Flamethrower/copyable-sweep.png");

        tracked=Core(emitter.Sim,8); initial=emitter.Sim.ParticleAt(tracked);
        int beforeStep=emitter.Sim.StepIndex;
        player.transform.position+=Vector3.up*0.5f;
        await Ticks(emitter,1);
        after=emitter.Sim.ParticleAt(tracked);
        Require(after.kind==FlameParticleKind.Core,"Movement test particle expired too early");
        float predictedY=initial.pos.y+initial.vel.y*(emitter.Sim.StepIndex-beforeStep);
        Require(Mathf.Abs(after.pos.y-predictedY)<3,"Moving the gun dragged existing flame sideways");

        tracked=Core(emitter.Sim,8); initial=emitter.Sim.ParticleAt(tracked);
        ahead=(initial.pos+initial.vel*2)*(1/emitter.pixelsPerUnit);
        var lingering=Target(new Vector2(ahead.x,ahead.y),0.6f);
        int liveCount=emitter.Sim.ParticleCount;
        weapon.SetSpriteActive(false); weapon.SetTriggerHeld(false);
        Require(emitter.Sim.ParticleCount==liveCount,"Weapon swap cleared lingering fire");
        await Ticks(emitter,3);
        Require(lingering.Received>0,"Hiding the weapon disabled damage from lingering fire");
        await Ticks(emitter,22);
        Require(emitter.Sim.ParticleCount==0 && emitter.Renderer.VisibleCount==0,"Release left particles or pixels behind");

        weapon.SetSpriteActive(true); weapon.CurrentAmmo=100; Field(player,"facing",Vector2.right);
        await Frames(2); emitter.Clear(); await Ticks(emitter,1);
        Vector2 origin=emitter.Origin;
        var wall=new GameObject("One-pixel flame test wall"); wall.layer=3;
        wall.transform.position=origin+Vector2.right*0.75f;
        wall.AddComponent<BoxCollider2D>().size=new Vector2(1/emitter.pixelsPerUnit,2);
        var near=Target(origin+Vector2.right*0.25f);
        var beyond=Target(origin+Vector2.right*1.2f);
        Physics2D.SyncTransforms(); weapon.SetTriggerHeld(true);
        await Ticks(emitter,25);
        Require(near.Received>0,"Core did not damage before the wall");
        Require(beyond.Received==0,"Particles tunnelled through the one-pixel wall");
        for(int i=0;i<emitter.Renderer.VisibleCount;i++) {
            int pixel=emitter.Renderer.VisibleIndex(i);
            Require(emitter.Renderer.PixelWorld(pixel).x<origin.x+0.75f,"Raster leaked behind the wall");
        }
        weapon.SetTriggerHeld(false);
        float trailDamage=oldTrail.Received, lingerDamage=lingering.Received;
        Object.Destroy(oldTrail.gameObject); Object.Destroy(outside.gameObject); Object.Destroy(lingering.gameObject);
        Object.Destroy(near.gameObject); Object.Destroy(beyond.gameObject); Object.Destroy(wall);
        return "CopyableScene: 180-degree sweep preserves old particle trajectories; player translation does not drag fire; old-trail damage="+trailDamage.ToString("F2")+", after-swap lingering damage="+lingerDamage.ToString("F2")+"; outside damage=0; release clears within 1.1 s; one-pixel wall blocks particles, pixels and damage. Capture: Temp/Flamethrower/copyable-sweep.png";
    }
}
