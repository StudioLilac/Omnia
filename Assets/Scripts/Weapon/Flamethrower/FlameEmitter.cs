using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Omnia.Fire {
    [DisallowMultipleComponent]
    public sealed class FlameEmitter : MonoBehaviour, IFlameCollision {
        [Header("World-space Raster")]
        [Tooltip("Expanded at initialization if necessary to fit twice Max Range plus a margin for the heat flow.")]
        [Min(32)] public int windowSize = 160;
        [Min(1)] public int maxParticles = 512;
        [Min(1)] public float pixelsPerUnit = 32;
        [InspectorName("Animation FPS"), Min(1)] public float simHz = 40;
        [Header("Core Stream (pixels per tick)")]
        [Tooltip("Rasterize a connected curve through the stream instead of individual particle streaks.")]
        public bool coreRibbon = true;
        [Range(0, 1)] public float ribbonSmoothness = 0.85f;
        [Range(0.25f, 4)] public float ribbonWidthScale = 1.75f;
        [Range(0, 1)] public float ribbonTipTaper = 0.2f;
        [Min(1)] public int maxRange = 48;
        [Min(0.1f)] public float jetSpeed = 2.5f;
        [Min(1)] public int pulsePeriod = 3;
        [Min(1)] public int slugLength = 3;
        [Range(0, 1)] public float inheritVelocity = 0.3f;
        [Min(0)] public float maxInheritedSpeed = 24;
        [Min(0)] public int coreWidthJitter;
        [Range(0, 1)] public float coreJitterChance = 0.1f;
        [Min(0)] public float coreDroop = 0.004f;
        [Range(0, 4)] public float coreRadius = 1;
        [Tooltip("Exponent of core heat over its lifetime. Higher values turn white into fiery colors sooner.")]
        [Range(0.1f, 3)] public float colorFalloff = 0.55f;
        [Range(0, 1)] public float radialFalloff = 0.3f;
        [Header("Barrel Plume")]
        [Range(0.1f, 1)] public float coreNozzleScale = 0.3f;
        [Min(0)] public float coreBloomPixels = 14;
        [Tooltip("Range fractions over which the smooth core gives way to the cellular fire. Heat still feeds the flow.")]
        [Range(0, 1)] public float coreFadeStart = 0.35f;
        [Range(0, 1)] public float coreFadeEnd = 0.85f;
        [Header("Core Temperature Pockets")]
        [Tooltip("Heat variation carried by emitted material. Zero restores a uniform core.")]
        [Range(0, 1)] public float coreHeatVariation = 0.5f;
        [Min(1)] public float coreHeatPatchLength = 10;
        [Min(1)] public float coreHeatPatchWidth = 3;
        [Header("Cellular Heat Flow")]
        [Tooltip("Advect heat through a persistent world-space velocity grid. The jet's shear generates eddies.")]
        public bool cellularFlow = true;
        [Tooltip("Velocity/pressure cell width in game pixels. Heat and damage remain one cell per game pixel. Set before Play.")]
        [Range(1, 4)] public int flowCellSize = 2;
        [Min(0)] public float flowSpeed = 3.5f;
        [Range(0, 2)] public float flowVorticity = 1.8f;
        [Min(0)] public float flowCooling = 0.022f;
        public float flowBuoyancy = 0.12f;
        [Tooltip("Amplify circulation on upward-facing heat edges and suppress it underneath, in world space.")]
        [Range(0, 1)] public float flowUpperBias = 0.8f;
        [Range(0, 1)] public float flowPerturbation = 0.35f;
        [Range(0, 1)] public float flowHeat = 0.8f;
        [Tooltip("Stabilize the hottest injected jet before turbulence develops downstream.")]
        [Range(0, 1)] public float flowJetStability = 0.8f;
        [Range(2, 24)] public int flowPressureIterations = 8;
        [Header("Legacy Wings (Cellular Flow disabled)")]
        [Range(0, 1)] public float shedChance = 0.25f;
        [Min(0)] public float flapWaveNumber = 0.35f;
        [Min(0)] public float flapFreq = 1.2f;
        [Min(1)] public int wingOffsetMax = 2;
        public float wingForward = 0.5f;
        public float wingLift = 0.15f;
        [Min(0)] public float wingSpread = 0.08f;
        [Min(0)] public int wingSpreadSteps = 3;
        [Min(0)] public float backDrag = 0.35f;
        public float liftAccel = 0.03f;
        [Min(1)] public int wingLifeMin = 4;
        [Min(1)] public int wingLifeMax = 9;
        [Range(0, 1)] public float wingHeat = 0.7f;
        [Range(0, 3)] public float wingRadius = 1;
        [Tooltip("Fraction of the last world-space movement drawn as a tapering wisp.")]
        [Range(0, 1)] public float wingTrail = 1;
        [HideInInspector] public int spread = 1;
        [Header("Tip Embers")]
        [Min(0)] public float emberGravity = 0.06f;
        [Min(0)] public int maxEmbers = 64;
        [Min(1)] public int emberLifeMin = 3;
        [Min(1)] public int emberLifeMax = 6;
        [Range(0, 1)] public float emberHeat = 0.75f;
        [Range(0, 3)] public float emberRadius = 0.75f;
        [Header("Raster and Damage")]
        [Range(0, 1)] public float cutoff = 0.08f;
        [Range(0, 1)] public float damageThreshold = 0.35f;
        [Range(0, 1)] public float coreHeatThreshold = 0.85f;
        [Min(0)] public float coreDamageMultiplier = 1;
        [Min(0)] public float wingDamageMultiplier = 0.6f;
        [Min(0)] public float releaseCooling = 1.5f;
        public FlamePalette palette;
        public Material flameMaterial;
        public bool ditherEdges = true;
        public uint seed = 1;
        public LayerMask solidLayers;
        public Transform nozzle;
        public bool IsFiring { get; set; }
        public event Action<float> SimulationTick;
        public FlameSim Sim { get; private set; }
        public FlameRenderer Renderer { get; private set; }
        public Vector2 Origin { get; private set; }
        public Vector2 Direction { get; private set; }
        public float range { get => maxRange / pixelsPerUnit; set => maxRange = Mathf.Max(1, Mathf.RoundToInt(value * pixelsPerUnit)); }
        public Vector2 Size => Vector2.one * (Sim.WindowSize / pixelsPerUnit);
        public Vector2 RasterCenter => new Vector2(Sim.OriginX + Sim.WindowSize * 0.5f, Sim.OriginY + Sim.WindowSize * 0.5f) / pixelsPerUnit;
        private FlamePose previousPose;
        private bool poseValid;
        private double accumulator;
        private FlamePalette fallbackPalette;
        private ContactFilter2D solidFilter;
        private readonly Collider2D[] overlaps = new Collider2D[1];
        private readonly RaycastHit2D[] rayHits = new RaycastHit2D[1];
        // Per-tick, fixed-size broadphase cache. Empty 8-pixel squares let most
        // particle/stamp queries skip narrowphase physics without retaining stale tiles.
        private readonly long[] chunkKeys = new long[2048];
        private readonly int[] chunkTicks = new int[2048];
        private readonly bool[] chunkSolid = new bool[2048];
        private int collisionTick;

        private void Awake() {
            pixelsPerUnit = Mathf.Max(1, pixelsPerUnit);
            int margin = cellularFlow ? 80 : 32;
            int size = Mathf.Max(32, windowSize, maxRange * 2 + margin);
            Sim = new FlameSim(Mathf.Max(1, maxParticles), size, seed, flowCellSize);
            var output = new GameObject("World-space Fire");
            // Deliberately detached: hiding, moving or rotating the gun cannot move its fire.
            SceneManager.MoveGameObjectToScene(output, gameObject.scene);
            Renderer = output.AddComponent<FlameRenderer>();
            Renderer.Initialize(size, pixelsPerUnit, GetComponentInChildren<SpriteRenderer>(), flameMaterial);
            Renderer.Advance = AdvanceSimulation;
            if (palette == null) fallbackPalette = ScriptableObject.CreateInstance<FlamePalette>();
        }

        public FlameSimSettings Settings(float seconds) => new FlameSimSettings {
            maxRange = Mathf.Max(1, maxRange), jetSpeed = jetSpeed, inheritVelocity = inheritVelocity,
            maxInheritedSpeed = maxInheritedSpeed, pulsePeriod = pulsePeriod, slugLength = slugLength,
            coreWidthJitter = coreWidthJitter, coreJitterChance = coreJitterChance, coreDroop = coreDroop,
            shedChance = shedChance, flapWaveNumber = flapWaveNumber, flapFreq = flapFreq,
            wingOffsetMax = wingOffsetMax, wingLifeMin = wingLifeMin, wingLifeMax = wingLifeMax,
            wingForward = wingForward, wingLift = wingLift, wingSpread = wingSpread * spread,
            wingSpreadSteps = wingSpreadSteps, backDrag = backDrag, liftAccel = liftAccel,
            wingHeat = wingHeat, emberGravity = emberGravity, maxEmbers = maxEmbers,
            emberHeat = emberHeat, emberLifeMin = emberLifeMin, emberLifeMax = emberLifeMax,
            coreRadius = coreRadius, wingRadius = wingRadius, emberRadius = emberRadius,
            radialFalloff = radialFalloff, colorFalloff = colorFalloff,
            releaseCooling = releaseCooling, stepSeconds = seconds,
            coreRibbon = coreRibbon, ribbonSmoothness = ribbonSmoothness, ribbonWidthScale = ribbonWidthScale,
            ribbonTipTaper = ribbonTipTaper, wingTrail = wingTrail,
            coreNozzleScale = coreNozzleScale, coreBloomPixels = coreBloomPixels,
            coreFadeStart = coreFadeStart, coreFadeEnd = coreFadeEnd,
            coreHeatVariation = coreHeatVariation, coreHeatPatchLength = coreHeatPatchLength, coreHeatPatchWidth = coreHeatPatchWidth,
            flowJetStability = flowJetStability,
            cellularFlow = cellularFlow, flowSpeed = flowSpeed, flowVorticity = flowVorticity,
            flowCooling = flowCooling, flowBuoyancy = flowBuoyancy, flowUpperBias = flowUpperBias, flowPerturbation = Mathf.Clamp01(flowPerturbation * spread),
            flowHeat = flowHeat, flowPressureIterations = flowPressureIterations
        };

        private void AdvanceSimulation(float elapsed) {
            if (this == null) return;
            accumulator += elapsed;
            double step = 1.0 / Mathf.Max(1, simHz);
            while (accumulator >= step) {
                accumulator -= step;
                // Pose input is captured once at the step boundary; the sim gets values only.
                Transform source = nozzle != null ? nozzle : transform;
                Origin = source.position;
                Vector2 direction = source.right;
                Direction = direction;
                var pose = new FlamePose(Origin.x * pixelsPerUnit, Origin.y * pixelsPerUnit, Mathf.Atan2(direction.y, direction.x));
                if (!poseValid) { previousPose = pose; poseValid = true; }
                solidFilter = new ContactFilter2D { useTriggers = false };
                solidFilter.SetLayerMask(solidLayers);
                unchecked { collisionTick++; }
                if (collisionTick == 0) { Array.Clear(chunkTicks, 0, chunkTicks.Length); collisionTick = 1; }
                Sim.Step(IsFiring && isActiveAndEnabled, previousPose, pose, Settings((float)step), solidLayers.value == 0 ? null : this);
                previousPose = pose;
                Renderer.Draw(Sim, palette != null ? palette : fallbackPalette, cutoff, ditherEdges);
                SimulationTick?.Invoke((float)step);
            }
        }

        public bool HitsSolid(FlameVector from, FlameVector to) {
            int minX = Mathf.FloorToInt(Mathf.Min(from.x, to.x) / 8), maxX = Mathf.FloorToInt(Mathf.Max(from.x, to.x) / 8);
            int minY = Mathf.FloorToInt(Mathf.Min(from.y, to.y) / 8), maxY = Mathf.FloorToInt(Mathf.Max(from.y, to.y) / 8);
            bool possible = false;
            if ((long)(maxX - minX + 1) * (maxY - minY + 1) > 64) possible = true;
            else for (int y = minY; y <= maxY && !possible; y++) for (int x = minX; x <= maxX; x++)
                if (ChunkSolid(x, y)) { possible = true; break; }
            if (!possible) return false;
            Vector2 start = new Vector2(from.x, from.y) / pixelsPerUnit;
            Vector2 end = new Vector2(to.x, to.y) / pixelsPerUnit;
            if (Physics2D.OverlapPoint(start, solidFilter, overlaps) > 0 || Physics2D.OverlapPoint(end, solidFilter, overlaps) > 0) return true;
            Vector2 delta = end - start;
            return delta.sqrMagnitude > 0 && Physics2D.Raycast(start, delta.normalized, solidFilter, rayHits, delta.magnitude) > 0;
        }
        private bool ChunkSolid(int x, int y) {
            long key = ((long)x << 32) | (uint)y;
            int index;
            unchecked { index = (int)((uint)x * 73856093u ^ (uint)y * 19349663u) & 2047; }
            if (chunkTicks[index] == collisionTick && chunkKeys[index] == key) return chunkSolid[index];
            bool solid = Physics2D.OverlapBox(new Vector2((x + 0.5f) * 8, (y + 0.5f) * 8) / pixelsPerUnit,
                Vector2.one * (8 / pixelsPerUnit), 0, solidFilter, overlaps) > 0;
            chunkTicks[index] = collisionTick; chunkKeys[index] = key; chunkSolid[index] = solid;
            return solid;
        }
        public bool CellSolid(int x, int y) => ChunkSolid(Mathf.FloorToInt(x / 8f), Mathf.FloorToInt(y / 8f))
            && Physics2D.OverlapPoint(new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit, solidFilter, overlaps) > 0;
        public float SampleHeat(Vector2 worldPos) => Renderer != null ? Renderer.SampleHeat(worldPos) : 0;
        public bool IsCoreHeat(float heat) => heat >= coreHeatThreshold;

        public void Clear() {
            IsFiring = false;
            accumulator = 0;
            poseValid = false;
            if (Sim != null) {
                Sim.Clear();
                if (Renderer != null) Renderer.Draw(Sim, palette != null ? palette : fallbackPalette, cutoff, ditherEdges);
            }
        }
        private void OnDisable() { IsFiring = false; }
        private void OnDestroy() {
            if (Renderer != null) { Renderer.Advance = null; Destroy(Renderer.gameObject); }
            if (fallbackPalette != null) Destroy(fallbackPalette);
        }
    }
}
