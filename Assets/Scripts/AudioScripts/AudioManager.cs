using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Utils;
using STOP_MODE = FMOD.Studio.STOP_MODE; // FMODUnity also defines STOP_MODE, so pick one

// These are now FMOD event paths (as shown in Studio's event browser), not clip names.
// Rename them to match whatever you actually call the events.
public static class AudioTracks {
    // BGM
    public const string LullabyForAScrapyard = "event:/Music/LullabyForAScrapyard";
    public const string CityOfMold = "event:/Music/CityOfMold";
    public const string SunkBeneath = "event:/Music/SunkBeneath";
    public const string JamiesTheme = "event:/Music/Jamie_sTheme";
    public const string TrialBySteel = "event:/Music/TrialBySteel";
    public const string UnclesTheme = "event:/Music/Uncle_sTheme";
    public const string CaveSpeak = "event:/Music/CaveSpeak";
    public const string FloraExMachina = "event:/Music/FloraExMachina";
    public const string IntoTheWind = "event:/Music/IntoTheWind";
    public const string Undersound = "event:/Music/Undersound";

    // SFX
    public const string Scrapgun = "event:/SFX/Scrapgun";
    public const string ScrapgunSpecial = "event:/SFX/ScrapgunSpecial";
    public const string Reload = "event:/SFX/Reload";
    public const string HarpoonHit = "event:/SFX/HarpoonHit";
    public const string HarpoonLaunch = "event:/SFX/HarpoonLaunch";
    public const string HarpoonRetract = "event:/SFX/HarpoonRetract";
    public const string JamieLand = "event:/SFX/JamieLand";
    public const string JamieSlide = "event:/SFX/JamieSlide";      // merge Slide + Slide_2 into one random-playlist event
    public const string JamieHurt = "event:/SFX/JamieHurt";        // one event, multi-instrument random playlist of the 4 hurt clips
    public const string MachineBreakdown = "event:/SFX/MachineBreakdown";
    public const string MachineHitGround = "event:/SFX/MachineHitGround";
    public const string MachineMalfunction = "event:/SFX/MachineMalfunction";
    public const string PlantShoot = "event:/SFX/PlantShoot";
    public const string Rumble = "event:/SFX/Rumble";
    public const string TaDa = "event:/SFX/TaDa";
    public const string WasteMove = "event:/SFX/WasteMove";        // merge Waste_Move_1 + _2
    public const string ArmadilloAttack = "event:/SFX/ArmadilloAttack";
    public const string ArmadilloClose = "event:/SFX/ArmadilloClose";
    public const string ArmadilloOpen = "event:/SFX/ArmadilloOpen";
    public const string ButtonPress = "event:/SFX/ButtonPress";
    public const string ClockStrikes = "event:/SFX/ClockStrikes";
    public const string CrabHurt = "event:/SFX/CrabHurt";
    public const string CrabSpawn = "event:/SFX/CrabSpawn";
    public const string Dinky = "event:/SFX/Dinky";                // merge Dinky_1..3
    public const string DinkyMutate = "event:/SFX/DinkyMutate";
    public const string DinkyScream = "event:/SFX/DinkyScream";
    public const string DummyFall = "event:/SFX/DummyFall";
    public const string FlyBoom = "event:/SFX/FlyBoom";
    public const string GateOpen = "event:/SFX/GateOpen";
    public const string GlassBreak = "event:/SFX/GlassBreak";

    // AMBIENT
}

public class AudioManager : PersistentSingleton<AudioManager>
{
    // Bus paths in the FMOD Studio mixer
    private const string MusicBusPath = "bus:/Music";
    private const string SfxBusPath = "bus:/SFX";
    private const string AmbientBusPath = "bus:/Ambience";

    private Bus musicBus, sfxBus, ambientBus;
    private EventInstance bgmInstance, ambientInstance;
    private string currentBgm;

    // Buses are fetched lazily so we never ask for them before banks are loaded.
    private Bus Music => GetBus(ref musicBus, MusicBusPath);
    private Bus Sfx => GetBus(ref sfxBus, SfxBusPath);
    private Bus Ambient => GetBus(ref ambientBus, AmbientBusPath);

    private static Bus GetBus(ref Bus bus, string path) {
        if (!bus.isValid()) {
            bus = RuntimeManager.GetBus(path);
        }
        return bus;
    }

    public void OnEnable() {
        LevelManager.OnLevelLoaded += OnSceneChange;
    }

    public void OnDisable() {
        LevelManager.OnLevelLoaded -= OnSceneChange;
    }

    protected override void OnAwake() {
        // Nothing to load: FMOD banks are loaded by the integration (see FMOD settings).
    }

    private void OnDestroy() {
        StopInstance(ref bgmInstance);
        StopInstance(ref ambientInstance);
    }

    private void OnSceneChange(LevelData levelData) {
        var track = levelData.Type switch {
            LevelType.Normal => AudioTracks.CaveSpeak,
            LevelType.Elite => AudioTracks.FloraExMachina,
            LevelType.Secret => AudioTracks.Undersound,
            LevelType.Custom => levelData.SoundTrack, // must now be an FMOD event path
            _ => ""
        };

        if (!string.IsNullOrEmpty(track)) {
            SwitchBGM(track);
        }
    }

    // Stops an event instance with its fade-out, releases it, and clears the handle.
    private static void StopInstance(ref EventInstance instance) {
        if (!instance.isValid()) return;
        instance.stop(STOP_MODE.ALLOWFADEOUT);
        instance.release(); // FMOD frees it once it has finished stopping
        instance = default;
    }

    private static void SetBusVolume(Bus bus, float volume) {
        bus.setVolume(Mathf.Clamp01(volume));
    }

    private static void ToggleBusMute(Bus bus) {
        bus.getMute(out bool muted);
        bus.setMute(!muted);
    }

    private static bool IsBusMuted(Bus bus) {
        bus.getMute(out bool muted);
        return muted;
    }

    //////////////////////////////
    // Background Music Methods //
    //////////////////////////////

    // Starts the specified music event. Does nothing if it's already playing.
    // Fades come from the AHDSR modulator on each music event's master track in Studio.
    public void PlayBGM(string eventPath) {
        SwitchBGM(eventPath);
    }

    // Fades out the current track and starts the new one.
    // Fade-out/in lengths are authored in FMOD (AHDSR attack/release), not in code.
    public void SwitchBGM(string eventPath) {
        if (eventPath == currentBgm && bgmInstance.isValid()) return;

        StopInstance(ref bgmInstance);
        currentBgm = eventPath;
        bgmInstance = RuntimeManager.CreateInstance(eventPath);
        bgmInstance.start();
    }

    // Stops the current music, waiting for the fade-out to finish.
    // Kept as a coroutine so existing `yield return StartCoroutine(StopBGM())` callers still work.
    public IEnumerator StopBGM() {
        var instance = bgmInstance;
        bgmInstance = default;
        currentBgm = null;
        if (!instance.isValid()) yield break;

        instance.stop(STOP_MODE.ALLOWFADEOUT);
        PLAYBACK_STATE state;
        do {
            yield return null;
            if (!instance.isValid()) yield break;
            instance.getPlaybackState(out state);
        } while (state != PLAYBACK_STATE.STOPPED);
        instance.release();
    }

    public void PauseBGM() {
        Music.setPaused(true);
    }

    public void ResumeBGM() {
        Music.setPaused(false);
    }

    public void ToggleBGM() {
        ToggleBusMute(Music);
    }

    public void SetBGMVolume(float volume) {
        SetBusVolume(Music, volume);
    }

    ///////////////////////////
    // Sound Effects Methods //
    ///////////////////////////

    // Fire-and-forget SFX. Pitch randomization now lives on the event in Studio.
    public void PlaySFX(string eventPath) {
        RuntimeManager.PlayOneShot(eventPath);
    }

    // Positioned version, for events with a spatializer (enemies, world objects, etc.)
    public void PlaySFX(string eventPath, Vector3 position) {
        RuntimeManager.PlayOneShot(eventPath, position);
    }

    public void StopSFX() {
        Sfx.stopAllEvents(STOP_MODE.IMMEDIATE);
    }

    public void SetSFXVolume(float volume) {
        SetBusVolume(Sfx, volume);
    }

    public void ToggleSFX() {
        ToggleBusMute(Sfx);
    }

    public void PlayHurtSound() {
        // The random pick between the hurt clips is done by the event's playlist in Studio.
        PlaySFX(AudioTracks.JamieHurt);
    }

    /////////////////////
    // Ambient Methods //
    /////////////////////

    public void PlayAmbient(string eventPath) {
        StopInstance(ref ambientInstance);
        ambientInstance = RuntimeManager.CreateInstance(eventPath);
        ambientInstance.start();
    }

    public void StopAmbient() {
        StopInstance(ref ambientInstance);
    }

    public void SetAmbientVolume(float volume) {
        SetBusVolume(Ambient, volume);
    }

    public void ToggleAmbient() {
        ToggleBusMute(Ambient);
    }

    public void ToggleAudio() {
        ToggleBGM();
        ToggleSFX();
        ToggleAmbient();
    }

    public bool IsMuted() {
        return IsBusMuted(Sfx) && IsBusMuted(Ambient) && IsBusMuted(Music);
    }
}
