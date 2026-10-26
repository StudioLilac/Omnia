using System;
using UnityEngine;

namespace Omnia.Fire {
    public sealed class FlameRenderer : MonoBehaviour {
        private Texture2D texture;
        private Sprite sprite;
        private SpriteRenderer display;
        private Material material;
        private Color32[] pixels;
        private float[] visibleHeat;
        private int[] visible;
        private int side;
        private float ppu;
        private Vector2 lowerLeft;
        public Action<float> Advance;
        public SpriteRenderer Display => display;
        public int PixelCount => visibleHeat.Length;
        public int VisibleCount { get; private set; }
        public int VisibleIndex(int index) => visible[index];
        public float PixelHeat(int index) => visibleHeat[index];
        public Vector2 PixelWorld(int index) => lowerLeft + new Vector2(index % side + 0.5f, index / side + 0.5f) / ppu;
        private void LateUpdate() { Advance?.Invoke(Time.deltaTime); }

        public void Initialize(int size, float pixelsPerUnit, SpriteRenderer sortingSource, Material flameMaterial) {
            ppu = pixelsPerUnit;
            side = size;
            pixels = new Color32[side * side];
            visibleHeat = new float[pixels.Length];
            visible = new int[pixels.Length];
            texture = new Texture2D(side, side, TextureFormat.RGBA32, false) {
                name = "World-space Flame", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels); texture.Apply(false, false);
            sprite = Sprite.Create(texture, new Rect(0, 0, side, side), Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
            display = gameObject.AddComponent<SpriteRenderer>();
            display.sprite = sprite;
            material = flameMaterial != null ? new Material(flameMaterial) : new Material(Shader.Find("Omnia/Pixel Flame"));
            display.sharedMaterial = material;
            if (sortingSource != null) {
                display.sortingLayerID = sortingSource.sortingLayerID;
                display.sortingOrder = sortingSource.sortingOrder + 1;
            }
        }

        public float SampleHeat(Vector2 world) {
            Vector2 pixel = (world - lowerLeft) * ppu;
            int x = Mathf.FloorToInt(pixel.x), y = Mathf.FloorToInt(pixel.y);
            return x < 0 || y < 0 || x >= side || y >= side ? 0 : visibleHeat[y * side + x];
        }

        public static bool CellVisible(float heat, float cutoff, bool dither, int worldX, int worldY, int step) {
            if (heat <= 0 || heat < cutoff) return false;
            if (!dither || heat >= cutoff + 0.15f) return true;
            // Window motion cannot change the dither: only absolute pixel and sim tick matter.
            uint hash;
            unchecked {
                hash = (uint)worldX * 73856093u ^ (uint)worldY * 19349663u ^ (uint)step * 83492791u;
                hash ^= hash >> 13; hash *= 1274126177u; hash ^= hash >> 16;
            }
            return (hash & 0xffffff) * (1f / 16777216f) < (heat - cutoff) / 0.15f;
        }

        public void Draw(FlameSim sim, FlamePalette palette, float cutoff, bool dither) {
            for (int i = 0; i < VisibleCount; i++) {
                int index = visible[i]; pixels[index] = default; visibleHeat[index] = 0;
            }
            VisibleCount = 0;
            lowerLeft = new Vector2(sim.OriginX, sim.OriginY) / ppu;
            // This is the only placement update. Between ticks the sprite stays put.
            transform.SetPositionAndRotation(new Vector3(lowerLeft.x, lowerLeft.y, 0), Quaternion.identity);
            transform.localScale = Vector3.one;
            for (int i = 0; i < sim.OccupiedCount; i++) {
                int index = sim.OccupiedIndex(i);
                float value = sim.CellHeat(index);
                if (!CellVisible(value, cutoff, dither, sim.OriginX + index % side, sim.OriginY + index / side, sim.StepIndex)) continue;
                visible[VisibleCount++] = index;
                visibleHeat[index] = value;
                pixels[index] = palette.Evaluate(value);
            }
            display.flipX = display.flipY = false;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private void OnDestroy() { Release(sprite); Release(texture); Release(material); }
        private static void Release(UnityEngine.Object resource) {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }
    }
}
