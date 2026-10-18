using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Utils;

public class DinkyBossFightManager : MonoBehaviour {
    [SerializeField] private BobbingBehaviour dinkyBoss;
    [SerializeField] private HealthBar bossHealth;
    [SerializeField] private DinkyBossFightTanks dinkyBossfightTanks;

    [SerializeField] Yarn.Unity.DialogueReference afterwordDialogue;
    [SerializeField] SpriteRenderer lighting;
    [SerializeField] SpriteRenderer uncle;

    [Serializable]
    public struct DinkyBossFightTanks {
        public const int MAX_TANK_COUNT = 4;
        public Tank armadilloTank;
        public Tank sundewTank;
        public Tank crabTank;
        public Tank birdTank;

#nullable enable
        public List<Tank> GetAllTanks() => new() { armadilloTank, sundewTank, crabTank, birdTank };
        public Tank GetRandomInactiveTank() {
            List<Tank> tanks = new() { armadilloTank, sundewTank, crabTank, birdTank };
            List<Tank> inactiveTanks = tanks.FindAll(t => t.IsInactive());
            if (inactiveTanks.Count == 0) {
                Debug.LogWarning($"Something went pretty wrong to have you call get random when no tanks are active.");
                return armadilloTank;
            }

            return inactiveTanks[UnityEngine.Random.Range(0, inactiveTanks.Count)];
        }
    }

    public struct TankInfo {
        public Tank Tank { get; }
        public float ActivationTime { get; }

        public TankInfo(Tank tank, float activationTime) {
            Tank = tank;
            ActivationTime = activationTime;
        }
    }

    private int CurrentProgress {
        get => _currentProgress;
        set {
            _currentProgress = value;
            bossHealth.UpdateBar(MaxProgress - _currentProgress, MaxProgress);
        }
    }

    private int _currentProgress = 0;
    private bool canSpawnNextWave = true;
    private List<Tank> activeTanks = new();
    private Wave currentWave = Wave.Get<DialogueWave>();
    public int MaxProgress => Wave.WaveValues.Aggregate(0, (acc, wave) => acc + wave.ActiveTanks(dinkyBossfightTanks).Count);

    private void OnEnable() {
        Tank.TankDeactivated += IncrementProgress;
        Tank.TankActivated += HandleActiveTank;
        FinalWave.FightCompleteEvent += HandleSceneEnd;
    }

    private void OnDisable() {
        Tank.TankDeactivated -= IncrementProgress;
        Tank.TankActivated -= HandleActiveTank;
        FinalWave.FightCompleteEvent -= HandleSceneEnd;
    }

    public void Start() {
        StartFight();
        AudioManager.Instance.PlayBGM(AudioTracks.TrialBySteel);
    }

    public void StartFight() {
        if (currentWave == Wave.Get<DialogueWave>()) {
            StartCoroutine(AnimateIntro());
        }
    }

    private IEnumerator AnimateIntro() {
        yield return StartCoroutine(bossHealth.FadeInAndFill());
        yield return new WaitForSeconds(0.5f);
        ProgressWave();
    }

    private void HandleActiveTank(Tank tank) {
        activeTanks.Add(tank);
    }

    private void HandleSceneEnd() {
        dinkyBossfightTanks.GetAllTanks().ForEach(tank => tank.Break());
        dinkyBoss.ShouldBob = false;
        bossHealth.AnimateFadeOut();
        StartCoroutine(StartAfterScene());
    }

    private IEnumerator StartAfterScene() {
        yield return new WaitForSeconds(2f);

        AudioManager.Instance.SwitchBGM(AudioTracks.UnclesTheme);
        StartCoroutine(FadeHelpers.FadeSpriteRenderer(lighting, lighting.color.a, 0, 1.5f));
        yield return StartCoroutine(FadeHelpers.FadeSpriteRendererColor(uncle, uncle.color, new Color(1,1,1), 1.5f));

        yield return new WaitForSeconds(1f);

        yield return DialogueManager.Instance.StartDialogue(afterwordDialogue);

        yield return new WaitForSeconds(0.5f);

        LevelManager.Instance.NextLevel();
    }

    private void IncrementProgress(Tank tank) {
        ++CurrentProgress;
        activeTanks.Remove(tank);
        ProgressWave();
    }

    public void ProgressWave() {
        if (canSpawnNextWave && currentWave.WaveEndCondition(activeTanks.Count)) {
            StartWave(currentWave.NextWave());
        }
    }

    private void StartWave(Wave newWave) {
        currentWave = newWave;
        StartCoroutine(ActivateTanks(newWave.ActiveTanks(dinkyBossfightTanks)));
    }

    private IEnumerator ActivateTanks(List<TankInfo> tankInfos) {
        canSpawnNextWave = false;
        foreach (var tankInfo in tankInfos) {
            yield return new WaitForSeconds(tankInfo.ActivationTime);
            if (activeTanks.Count == DinkyBossFightTanks.MAX_TANK_COUNT) {
                Debug.LogWarning($"Something went pretty wrong to try and activate a tank when all tanks are active {currentWave.GetType()}");
                ++CurrentProgress; //Attempt to escape softlock condition
                continue;
            }

            if (tankInfo.Tank.IsInactive()) {
                tankInfo.Tank.Activate();
            } else {
                dinkyBossfightTanks.GetRandomInactiveTank().Activate();
            }
        }
        canSpawnNextWave = true;
    }
}
