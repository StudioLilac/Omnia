using Enemies;
using Enemies.Spawnball;
using Puzzle;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tank : MonoBehaviour {
    private enum TankState { Off, On, Deactivated, Actived, Broken }
    [SerializeField] private Sprite crackedTank;
    [SerializeField] private Sprite normalTank;
    [SerializeField] private Sprite brokenTank;
    [SerializeField] private Sprite deactivatedTank;
    [SerializeField] private Sprite crackedDeactivatedTank;
    [SerializeField] private SpriteRenderer containedSpecimen;
    [SerializeField] private SpriteRenderer transparentOverlay;
    [SerializeField] private SpriteRenderer crossFadeRenderer;
    [SerializeField] private CrossFade crossFade;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private InterfaceReference<IProgress> lever;
    [SerializeField] private Spawnball spawnball;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private List<TransformInfo> spawnLocations;
    private IProgress progressLever;
    private ScreenShakeManager ScreenShakeManager => this.Inject<ScreenShakeManager>();


#nullable enable
    private TankState State { get; set; } = TankState.Deactivated;
    public static event TankDelegate? TankDeactivated;
    public static event TankDelegate? TankActivated;
    public delegate void TankDelegate(Tank thank);

    private void Awake() {
        progressLever = lever.Value;
        spawner.SetMaxSpawn(spawnLocations.Count);
        spawner.IsActive = false;
    }

    private void Start() {
        spawner.GetSpawn = GetConfiguredSpawnball;
        spawner.GetOwnedEnemiesCount = () => spawnLocations.FindAll(it => it.occupied).Count;
    }

    private void OnEnable() {
        progressLever.ProgressEvent += HandleShutoff;
    }

    private void OnDisable() {
        progressLever.ProgressEvent -= HandleShutoff;
    }

    public void Activate() {
        Shake();
        PerformTransition(TankState.Actived);
        spawner.IsActive = true;
        spawner.TrySpawn();
        TankActivated?.Invoke(this);
    }

    public void Break() {
        Shake(3, 1f);
        PerformTransition(TankState.Broken);
        spawner.IsActive = false;
        containedSpecimen.gameObject.SetActive(false);
        KillAllChildren();
    }

    public void TurnOff() {
        PerformTransition(TankState.Off);
        spawner.IsActive = false;
    }

    public void FadeOn() {
        PerformTransition(TankState.On, doCrossFade: true);
        spawner.IsActive = false;
    }

    public void Deactivate() {
        Shake();
        PerformTransition(TankState.Deactivated);
        spawner.IsActive = false;
        TankDeactivated?.Invoke(this);
    }

    public bool IsInactive() => State == TankState.Deactivated;

    private void KillAllChildren() {
        spawner.KillAllChildren();
        spawnLocations.ForEach(it => it.occupiedEnemy?.Die());
    }

    private void HandleShutoff(IProgress progress) {
        if (progress != null && Mathf.Approximately(progress.Progress, 1f) && State == TankState.Actived) {
            Deactivate();
        }
    }

    // Spawnball factory for EnemySpawner that then uses events to modify Tank state
    private GameObject GetConfiguredSpawnball() {
        Spawnball instance = Instantiate(spawnball);
        TransformInfo info = GetRandomSpawnLocationInfo();

        info.occupied = true;
        instance.spawn = enemyPrefab;
        instance.target = info.transform;
        instance.NotifyOnSpawn += HandleEnemySpawn;

        //First register callbacks on spawnball, then on its spawned enemy
        void HandleEnemySpawn(Enemy enemy) {
            if (info.occupiedEnemy != null) 
                Debug.LogWarning($"Something went terrible wrong to be overriding the ownership of an existing enemy {info.occupiedEnemy.name} with ${enemy.name}");

            info.occupiedEnemy = enemy;
            instance.NotifyOnSpawn -= HandleEnemySpawn;
            enemy.OnDeath += HandleEnemyDeath;

            void HandleEnemyDeath(Enemy e) {
                info.occupiedEnemy = null;
                info.occupied = false;
                enemy.OnDeath -= HandleEnemyDeath;
            }
        }

        return instance.gameObject;
    }

    private List<TransformInfo> GetUnoccupiedTransformInfos() {
        return spawnLocations.FindAll(it => !it.occupied);
    }

    private TransformInfo GetRandomSpawnLocationInfo() {
        List<TransformInfo> inactiveSpawnLocations = GetUnoccupiedTransformInfos();
        if (inactiveSpawnLocations.Count == 0) {
            Debug.LogWarning($"Something Went terrible wrong for tank {gameObject.name} to try and spawn when all locations are occupied");
            return new TransformInfo(this.gameObject.transform, null);
        }
        return inactiveSpawnLocations[UnityEngine.Random.Range(0, inactiveSpawnLocations.Count)];
    }

    private void PerformTransition(TankState state, bool doCrossFade = false) {
        State = state;
        StartCoroutine(SetSprites(GetSpriteForState(state), doCrossFade));
    }

    private Sprite GetSpriteForState(TankState state) {
        return state switch {
            TankState.On => normalTank,
            TankState.Off => deactivatedTank,
            TankState.Actived => crackedTank,
            TankState.Deactivated => crackedDeactivatedTank,
            TankState.Broken => brokenTank,
            _ => normalTank,
        };
    }

    private IEnumerator SetSprites(Sprite sprite, bool doCrossFade = false) {
        if (doCrossFade) {
            crossFade.StartCrossFadeBackground(new CrossFade.CrossFadeRenderers(frontRenderer: new() { spriteRenderer }, backRenderer: crossFadeRenderer), sprite);
        } else {
            spriteRenderer.sprite = sprite;
            transparentOverlay.sprite = sprite;
        }

        yield return null;
    }

    // Helper to reduce clutter for screenShakeManager find references
    private void Shake(float intensity = 1.0f, float duration = 0.5f) {
        ScreenShakeManager.Shake(intensity, duration);
    }


    [Serializable]
    private class TransformInfo {
        public Transform transform;
        public Enemy? occupiedEnemy;
        // Necessary field to prevent race condition where occupied enemy is not spanwed in yet (in spawnball), but this location is chosen for occupency again.
        public bool occupied = false;

        public TransformInfo(Transform transform, Enemy? enemy) {
            this.transform = transform;
            this.occupiedEnemy = enemy;
        }
    }
}
